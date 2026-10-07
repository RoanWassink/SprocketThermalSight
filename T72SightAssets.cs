using System.Globalization;
using HarmonyLib;
using Sprocket.Vehicles;
using Sprocket.Vehicles.AssetManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SprocketThermalSight;

internal static class T72SightAssets
{
    private static bool Own(VehicleComponent c)=>c.TryCast<VehicleObjectModel>()?.VehicleObject?.GetBehaviour<Sprocket.Vehicles.AttachedBehaviours.GunnerSight>()?.ComponentID=="t72StyleSight";
    private static Mesh? mesh;
    private static VehicleMaterial? glass;
    private static VehicleMaterial? housing;
    private static float scanAt;
    private static bool failed;
    private static int appliedLogs;
    private static string Asset(string file)=>Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!,"assets",file);
    [HarmonyPrefix,HarmonyPatch(typeof(VehicleObjectModel),nameof(VehicleObjectModel.Initiate))]
    private static void Initiating(VehicleObjectModel __instance)
    {
        if(!Own(__instance))return;
        // Keep the native exterior sight interaction category.

        __instance.staticRenderer=false;
        
    }
    [HarmonyPrefix,HarmonyPatch(typeof(VehicleObjectModel),nameof(VehicleObjectModel.Build))]
    private static void Building(VehicleObjectModel __instance)
    {if(Own(__instance))__instance.staticRenderer=false;}
    [HarmonyPostfix,HarmonyPatch(typeof(VehicleObjectModel),nameof(VehicleObjectModel.OnModelLoaded))]
    private static void Loaded(VehicleObjectModel __instance)
    {try{if(Own(__instance))Apply(__instance);}catch(Exception ex){Runtime.Warn("T72 sight model load: "+ex.Message);}}
    private static void EnsureAssets()
    {
        if(mesh!=null)return;
        var positions=new List<Vector3>();var texcoords=new List<Vector2>();var sourceNormals=new List<Vector3>();
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var normals=new List<Vector3>();
        var faces=new[]{new List<int>(),new List<int>()};int material=0;
        float F(string text)=>float.Parse(text,CultureInfo.InvariantCulture);
        foreach(var line in File.ReadLines(Asset("t72-style-sight.obj")))
        {
            var f=line.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(f.Length==0)continue;
            if(f[0]=="v" && f.Length==4)positions.Add(new Vector3(F(f[1]),F(f[2]),F(f[3])));
            else if(f[0]=="vt" && f.Length>=3)texcoords.Add(new Vector2(F(f[1]),F(f[2])));
            else if(f[0]=="vn" && f.Length==4)sourceNormals.Add(new Vector3(F(f[1]),F(f[2]),F(f[3])));
            else if(f[0]=="usemtl" && f.Length==2)material=f[1] switch{"Housing"=>0,"Glass"=>1,_=>throw new InvalidDataException("Unknown FCS surface")};
            else if(f[0]=="f")
            {
                if(f.Length!=4)throw new InvalidDataException("FCS must be triangulated");
                for(int i=1;i<4;i++)
                {
                    var p=f[i].Split('/');if(p.Length!=3)throw new InvalidDataException("FCS UV/normal tuple missing");
                    int a=int.Parse(p[0],CultureInfo.InvariantCulture)-1,b=int.Parse(p[1],CultureInfo.InvariantCulture)-1,c=int.Parse(p[2],CultureInfo.InvariantCulture)-1;
                    if(a<0 || a>=positions.Count || b<0 || b>=texcoords.Count || c<0 || c>=sourceNormals.Count)throw new InvalidDataException("FCS tuple out of bounds");
                    faces[material].Add(vertices.Count);vertices.Add(positions[a]);uv.Add(texcoords[b]);normals.Add(sourceNormals[c]);
                }
            }
        }
        if(positions.Count!=1623 || vertices.Count!=1014 || faces.Any(f=>f.Count==0) || vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)))throw new InvalidDataException("Unexpected FCS model geometry");
        mesh=new Mesh{name="T72 style sight",hideFlags=HideFlags.HideAndDontSave};mesh.vertices=vertices.ToArray();mesh.uv=uv.ToArray();mesh.normals=normals.ToArray();mesh.subMeshCount=2;
        for(int i=0;i<2;i++)mesh.SetTriangles(faces[i].ToArray(),i);mesh.RecalculateBounds();
        var shader=Shader.Find("HDRP/Lit");if(shader==null)throw new InvalidOperationException("FCS shader unavailable");
        var surface=new Material(shader){name="FCS eyepiece glass",hideFlags=HideFlags.HideAndDontSave};
        surface.SetColor("_BaseColor",new Color(.018f,.028f,.025f,1));surface.SetFloat("_Smoothness",.92f);
        // The supplied LENS export includes inward-facing housing faces. Render
        // both sides without changing the author's mesh or normals.
        surface.SetFloat("_DoubleSidedEnable",1);surface.SetFloat("_CullMode",0);surface.SetFloat("_CullModeForward",0);HDMaterial.ValidateMaterial(surface);
        glass=new VehicleMaterial(surface){Flags=VehicleMaterialFlags.Unpainted|VehicleMaterialFlags.DisableGrime|VehicleMaterialFlags.CustomShader};
        Plugin.Instance.Log.LogInfo("[T72 sight] User model loaded: 338 triangles; original dimensions retained; mounting frame aligned to +Z.");
    }
    private static void Apply(VehicleObjectModel model)
    {
        var native=model.Model;if(native==null)return;EnsureAssets();model.MarkDynamic();
        if(native.InteractionMesh!=mesh)
        {native.SetMesh(mesh!);native.SetShadowProxy(mesh!);native.SetInteractionMesh(mesh!);native.SetCollisionMesh(mesh!);}
        var materials=native.Materials;var paint=materials!=null && materials.Length>0 ? materials[0] : native.Material;
        if(paint!=null && housing==null)
        {
            // Signed volume is not a reliable reason to reverse an entire exported
            // object. Keep source winding/normals and avoid losing visible surfaces
            // to the native inherited material's one-sided culling.
            // The inherited vanilla VehicleLit maps do not fit this creator OBJ's
            // UVs. Use a clean opaque housing surface, without inherited masks,
            // normal maps or glass-like patches on structural walls.
            var shader=Shader.Find("HDRP/Lit");if(shader==null)throw new InvalidOperationException("T72 housing shader unavailable");
            var surface=new Material(shader){name="T72 sight housing",hideFlags=HideFlags.HideAndDontSave};
            surface.SetColor("_BaseColor",new Color(.24f,.27f,.18f,1));
            surface.SetFloat("_Smoothness",.25f);surface.SetFloat("_Metallic",0);
            HDMaterial.SetSurfaceType(surface,false);
            surface.SetFloat("_DoubleSidedEnable",1);surface.SetFloat("_DoubleSidedNormalMode",1);
            surface.SetFloat("_CullMode",0);surface.SetFloat("_CullModeForward",0);HDMaterial.ValidateMaterial(surface);
            housing=new VehicleMaterial(surface){Flags=VehicleMaterialFlags.Unpainted|VehicleMaterialFlags.DisableGrime|VehicleMaterialFlags.CustomShader};
            Plugin.Instance.Log.LogInfo("[T72 sight] Clean opaque housing; original front optical pane uses glass; shader="+surface.shader.name);
        }
        if(housing!=null && (materials==null || materials.Length!=2 || materials[0]?.Pointer!=housing.Pointer || materials[1]?.Pointer!=glass!.Pointer))
            native.SetMaterials(new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<VehicleMaterial>(new[]{housing,glass!}));
        if(appliedLogs<4){appliedLogs++;Plugin.Instance.Log.LogInfo($"[T72 sight] Native model applied; selection={model.PartType}; layer={model.InteractionCollider?.Layer}; meshMatch={native.InteractionMesh==mesh}");}
    }
    internal static void Tick()
    {
        if(failed || Time.unscaledTime<scanAt)return;scanAt=Time.unscaledTime+.5f;
        try
        {
            foreach(var vehicle in UnityEngine.Object.FindObjectsOfType<Vehicle>())
            {
                if(vehicle==null || vehicle.Released)continue;
                var list=vehicle.ObjectReader.Items;int n=list.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleObject>>().Count;
                for(int i=0;i<n;i++)
                {
                    var components=list[i]?.Components;if(components==null)continue;
                    int m=components.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleComponent>>().Count;
                    for(int j=0;j<m;j++)
                    {var model=components[j]?.TryCast<VehicleObjectModel>();if(model!=null && Own(model))Apply(model);}
                }
            }
        }
        catch(Exception ex){failed=true;Runtime.Warn("T72 sight visual unavailable: "+ex.Message);}
    }
}
