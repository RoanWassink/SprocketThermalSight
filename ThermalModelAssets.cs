using System.Globalization;
using HarmonyLib;
using Sprocket.Vehicles;
using Sprocket.Vehicles.AssetManagement;
using Sprocket.Vehicles.AttachedBehaviours;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SprocketThermalSight;

internal static class ThermalModelAssets
{
    private static readonly Dictionary<bool,Mesh> meshes=new();
    private static VehicleMaterial? dark, lens;
    private static float scanAt;
    private static bool failed;
    private static int appliedLogs;
    private static readonly HashSet<IntPtr> reportedModels=new();
    private static bool Own(VehicleObjectModel model) => model.VehicleObject?.GetBehaviour<GunnerSight>() is {} sight && Runtime.IsThermal(sight);
    [HarmonyPrefix,HarmonyPatch(typeof(VehicleObjectModel),nameof(VehicleObjectModel.Build))]
    private static void Building(VehicleObjectModel __instance)
    {
        if(!Own(__instance))return;
        __instance.staticRenderer=false;
    }
    [HarmonyPostfix,HarmonyPatch(typeof(VehicleObjectModel),nameof(VehicleObjectModel.OnModelLoaded))]
    private static void Loaded(VehicleObjectModel __instance)
    {
        try{if(Own(__instance))Apply(__instance);}catch(Exception ex){Runtime.Warn("Thermal loaded model: "+ex.Message);}
    }
    private static void Apply(VehicleObjectModel model)
    {
        // Model is the actual IVehicleModel returned by OnModelLoaded. The inherited
        // component models list can be empty when this visual belongs to a sight.
        var native=model.Model;if(native==null)return;
        var mesh=EnsureAssets(model.VehicleObject.GetBehaviour<GunnerSight>()?.ComponentID==Runtime.VerticalId);model.MarkDynamic();
        bool changed=native.InteractionMesh!=mesh;
        var group=native.TryCast<VehicleRendererGroup>();
        if(group?.lods!=null)
            foreach(var lod in group.lods)
            {
                var filter=lod?.Renderer?.GetComponent<MeshFilter>();
                if(filter!=null && filter.sharedMesh!=mesh){changed=true;break;}
            }
        if(changed)
        {
            native.SetMesh(mesh!);native.SetShadowProxy(mesh!);
            native.SetInteractionMesh(mesh!);native.SetCollisionMesh(mesh!);
        }
        var materials=native.Materials;
        var paint=materials!=null && materials.Length>0 ? materials[0] : native.Material;
        if(paint!=null && (materials==null || materials.Length!=3 || materials[1]?.Pointer!=dark!.Pointer || materials[2]?.Pointer!=lens!.Pointer))
            native.SetMaterials(new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<VehicleMaterial>(new[]{paint,dark!,lens!}));
        if(appliedLogs<6 && reportedModels.Add(native.Pointer))
        {
            appliedLogs++;
            Plugin.Instance.Log.LogInfo($"[Thermal model] Applied to active Model: object={model.VehicleObject.EntityID}; LODs={native.LODCount}; baked={native.FullyBaked}; interactionMatch={native.InteractionMesh==mesh}; collisionMatch={native.CollisionMesh==mesh}");
        }
    }
    private static Mesh EnsureAssets(bool vertical)
    {
        if(meshes.TryGetValue(vertical,out var cached))return cached;
        var positions=new List<Vector3>();var vertices=new List<Vector3>();var faces=new[]{new List<int>(),new List<int>(),new List<int>()};int material=0;
        string path=Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!,"assets",vertical ? "thermal-head-vertical.obj" : "thermal-head.obj");
        foreach(var line in File.ReadLines(path))
        {
            var fields=line.Split(' ',StringSplitOptions.RemoveEmptyEntries);
            if(fields.Length==4 && fields[0]=="v")positions.Add(new Vector3(float.Parse(fields[1],CultureInfo.InvariantCulture),float.Parse(fields[2],CultureInfo.InvariantCulture),float.Parse(fields[3],CultureInfo.InvariantCulture)));
            else if(fields.Length==2 && fields[0]=="usemtl")material=fields[1] switch{"Paint"=>0,"Dark"=>1,"Lens"=>2,_=>throw new InvalidDataException("Unknown thermal material")};
            else if(fields.Length==4 && fields[0]=="f")
                for(int i=1;i<=3;i++){int index=int.Parse(fields[i].Split('/')[0],CultureInfo.InvariantCulture)-1;if(index<0 || index>=positions.Count)throw new InvalidDataException("Thermal vertex index");faces[material].Add(vertices.Count);vertices.Add(positions[index]);}
        }
        if(positions.Count!=82 || vertices.Count!=324 || faces.Any(f=>f.Count==0) || vertices.Any(v=>!float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z)))throw new InvalidDataException("Unexpected thermal model geometry");
        var shader=Shader.Find("HDRP/Lit");if(shader==null)throw new InvalidOperationException("Thermal model shader unavailable");
        VehicleMaterial Surface(string name,Color color,float smoothness)
        {
            var m=new Material(shader){name=name,hideFlags=HideFlags.HideAndDontSave};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smoothness);HDMaterial.ValidateMaterial(m);
            return new VehicleMaterial(m){Flags=VehicleMaterialFlags.Unpainted|VehicleMaterialFlags.DisableGrime|VehicleMaterialFlags.CustomShader};
        }
        dark ??=Surface("Thermal aperture rim",new Color(.18f,.23f,.17f,1),.35f);
        lens ??=Surface("Thermal IR aperture",new Color(.04f,.12f,.14f,1),.85f);
        var mesh=new Mesh{name=vertical ? "Wall-mounted thermal head" : "Stepped thermal periscope head",hideFlags=HideFlags.HideAndDontSave};mesh.vertices=vertices.ToArray();mesh.subMeshCount=3;
        for(int i=0;i<3;i++)mesh.SetTriangles(faces[i].ToArray(),i);mesh.RecalculateNormals();mesh.RecalculateBounds();
        meshes.Add(vertical,mesh);
        Plugin.Instance.Log.LogInfo($"[Thermal model] {(vertical ? "Wall-mounted" : "Periscope")} head ready; optical axis retained at (0,0,0.0409).");
        return mesh;
    }
    internal static void Tick()
    {
        if(failed || Time.unscaledTime<scanAt)return;scanAt=Time.unscaledTime+.5f;
        try
        {
            foreach(var vehicle in UnityEngine.Object.FindObjectsOfType<Vehicle>())
            {
                if(vehicle==null || vehicle.Released)continue;
                var items=vehicle.ObjectReader.Items;int n=items.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleObject>>().Count;
                for(int i=0;i<n;i++)
                {
                    var obj=items[i];if(obj==null || obj.Released)continue;
                    var sight=obj.GetBehaviour<GunnerSight>();if(sight==null || !Runtime.IsThermal(sight))continue;
                    var model=obj.GetBehaviour<VehicleObjectModel>();if(model==null)continue;
                    Apply(model);
                }
            }
        }
        catch(Exception ex){failed=true;Runtime.Warn("Optional thermal model unavailable: "+ex.Message);}
    }
}
