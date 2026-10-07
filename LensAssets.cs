using UnityEngine;
using Sprocket.Vehicles;
using HarmonyLib;
using Sprocket.PartImporting;
using Sprocket.Vehicles.AssetManagement;
using UnityEngine.Rendering.HighDefinition;

namespace SprocketThermalSight;

internal static class LensAssets
{
    private const string ModelId = "thermalLensModel";
    private static Mesh? mesh;
    private static Material? glass;
    private static VehicleMaterial? nativeGlass;
    private static Material? rim;
    private static VehicleMaterial? nativeRim;
    private static Texture2D? texture;
    private static Sprite? icon;
    private static float scanAt;
    private static bool failed;
    private static bool iconFailed;
    private static Mesh? paneMesh;
    private static readonly Dictionary<string, VehicleMaterial> paneMaterials = new();
    private static readonly Dictionary<string, Sprite> paneIcons = new();
    private static Mesh? integratedMesh;
    private static Material? screenMaterial;
    internal static void IntegratedWindow(Transform parent,string name,bool visible,Vector3 position,Vector3 scale,bool screen)
    {
        var existing=parent.Find(name);
        if(existing!=null){existing.gameObject.SetActive(visible);return;}
        if(!visible)return;
        EnsureAssets();
        if(integratedMesh==null)
        {
            integratedMesh=new Mesh{name="Integrated optical face",hideFlags=HideFlags.HideAndDontSave};
            integratedMesh.vertices=new[]{new Vector3(-.06f,-.045f,0),new Vector3(.06f,-.045f,0),new Vector3(.06f,.045f,0),new Vector3(-.06f,.045f,0)};
            integratedMesh.triangles=new[]{0,1,2,0,2,3};integratedMesh.RecalculateNormals();integratedMesh.RecalculateBounds();
        }
        if(screen && screenMaterial==null)
        {
            screenMaterial=new Material(glass!){name="Console screen",hideFlags=HideFlags.HideAndDontSave};
            screenMaterial.SetTexture("_BaseColorMap",null);screenMaterial.SetColor("_BaseColor",new Color(.08f,.22f,.12f,1));
            HDMaterial.SetSurfaceType(screenMaterial,false);HDMaterial.ValidateMaterial(screenMaterial);
        }
        var go=new GameObject(name);go.hideFlags=HideFlags.DontSave;go.layer=parent.gameObject.layer;
        go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.identity;go.transform.localScale=scale;
        go.AddComponent<MeshFilter>().sharedMesh=integratedMesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=screen ? screenMaterial! : PaneMaterial("yellowWindowPane").Material;
    }
    private static bool IsPane(string id) => id == "clearWindowPane" || id == "tintedWindowPane" || id == "yellowWindowPane";
    private static Color PaneColor(string id) => id switch
    {
        "clearWindowPane" => new Color(.96f,.99f,1f,.12f),
        "tintedWindowPane" => new Color(.16f,.23f,.28f,.55f),
        _ => new Color(1f,.68f,.12f,.38f)
    };
    private static string? PaneId(string guid) => guid switch
    {
        "379c5275-72ad-4a2d-9e8c-ce183164ea70" => "clearWindowPane",
        "d6bc56a1-a1f9-4314-aed4-9a953031f8db" => "tintedWindowPane",
        "862d1452-9b48-4efc-bafd-a7eb8eac6117" => "yellowWindowPane",
        _ => null
    };
    private static VehicleMaterial PaneMaterial(string id)
    {
        if(paneMaterials.TryGetValue(id,out var existing))return existing;
        EnsureAssets();
        var material=new Material(glass!){name=id,hideFlags=HideFlags.HideAndDontSave};
        // A uniform tint leaves the scene visible; the old lens coating texture is not used.
        material.SetTexture("_BaseColorMap",null);
        material.SetColor("_BaseColor",PaneColor(id));
        HDMaterial.ValidateMaterial(material);
        var wrapper=new VehicleMaterial(material){Flags=VehicleMaterialFlags.Unpainted|VehicleMaterialFlags.DisableGrime|VehicleMaterialFlags.CustomShader};
        paneMaterials.Add(id,wrapper);
        if(paneMesh==null)
        {
            paneMesh=new Mesh{name="Frameless window pane",hideFlags=HideFlags.HideAndDontSave};
            var points=new[]{new Vector3(-.1f,-.1f,.003f),new Vector3(.1f,-.1f,.003f),new Vector3(.1f,.1f,.003f),new Vector3(-.1f,.1f,.003f),new Vector3(-.1f,-.1f,0),new Vector3(.1f,-.1f,0),new Vector3(.1f,.1f,0),new Vector3(-.1f,.1f,0)};
            paneMesh.vertices=points;
            paneMesh.triangles=new[]{0,1,2,0,2,3,7,6,5,7,5,4,4,5,1,4,1,0,5,6,2,5,2,1,6,7,3,6,3,2,7,4,0,7,0,3};
            paneMesh.RecalculateNormals();paneMesh.RecalculateBounds();
        }
        return wrapper;
    }
    private static string Asset(string name) => Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!,"assets",name);
    private static void EnsureAssets()
    {
        if (mesh != null) return;
        // Thin, bevelled optical window. No enclosing box; the player provides the housing.
        Vector3[] front = { new(-.06f,-.045f,.012f),new(.06f,-.045f,.012f),new(.06f,.045f,.012f),new(-.06f,.045f,.012f) };
        Vector3[] rear = { new(-.07f,-.055f,0),new(.07f,-.055f,0),new(.07f,.055f,0),new(-.07f,.055f,0) };
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var faces = new List<int>();
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int start=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});
            faces.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
        }
        Quad(front[0],front[1],front[2],front[3]);
        for(int i=0;i<4;i++) Quad(rear[i],rear[(i+1)%4],front[(i+1)%4],front[i]);
        Quad(rear[3],rear[2],rear[1],rear[0]);
        var shader=Shader.Find("HDRP/Lit");
        if(shader==null) throw new InvalidOperationException("HDRP lens shader unavailable");
        texture=new Texture2D(2,2,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave};
        if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(Asset("thermal-lens-coating.png")),false)) throw new InvalidDataException("Lens coating decode failed");
        glass=new Material(shader){name="Coated sight glass",hideFlags=HideFlags.HideAndDontSave};
        glass.SetColor("_BaseColor",new Color(.8f,.9f,1f,.35f));glass.SetTexture("_BaseColorMap",texture);
        glass.SetFloat("_Metallic",0f);glass.SetFloat("_Smoothness",.98f);
        glass.SetFloat("_CoatMask",1f);
        glass.SetFloat("_DoubleSidedEnable",1);glass.SetFloat("_DoubleSidedNormalMode",0);
        glass.SetVector("_DoubleSidedConstants",new Vector4(1,1,-1,0));
        HDMaterial.SetSurfaceType(glass,true);
        glass.SetFloat("_BlendMode",0);glass.SetFloat("_ZWrite",0);glass.SetFloat("_TransparentZWrite",0);
        glass.SetFloat("_CullMode",0);glass.SetFloat("_CullModeForward",0);
        glass.SetFloat("_SrcBlend",1);glass.SetFloat("_DstBlend",10);
        glass.renderQueue=3000;
        HDMaterial.ValidateMaterial(glass);
        rim=new Material(shader){name="Optical rim",hideFlags=HideFlags.HideAndDontSave};
        rim.SetColor("_BaseColor",new Color(.055f,.06f,.045f,1));rim.SetFloat("_Metallic",.5f);rim.SetFloat("_Smoothness",.6f);
        HDMaterial.ValidateMaterial(rim);
        // Native wrappers retain these materials through paint, LOD and renderer refreshes.
        nativeGlass=new VehicleMaterial(glass){Flags=VehicleMaterialFlags.Unpainted|VehicleMaterialFlags.DisableGrime|VehicleMaterialFlags.CustomShader};
        nativeRim=new VehicleMaterial(rim){Flags=VehicleMaterialFlags.Unpainted|VehicleMaterialFlags.DisableGrime|VehicleMaterialFlags.CustomShader};
        mesh=new Mesh{name="Unhoused optical window",hideFlags=HideFlags.HideAndDontSave};
        mesh.vertices=v.ToArray();mesh.uv=uv.ToArray();mesh.subMeshCount=2;
        // Front and back glass; the bevel uses an opaque frame material.
        mesh.SetTriangles(faces.Take(6).Concat(faces.Skip(30)).ToArray(),0);
        mesh.SetTriangles(faces.Skip(6).Take(24).ToArray(),1);
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        Plugin.Instance.Log.LogInfo($"[Optical glass] shader={shader.name}; surface={glass.GetFloat("_SurfaceType")}; queue={glass.renderQueue}; native unpainted material ready.");
    }
    [HarmonyPrefix,HarmonyPatch(typeof(VehicleObjectModel),nameof(VehicleObjectModel.Build))]
    private static void Building(VehicleObjectModel __instance)
    {
        if(__instance.ComponentID!=ModelId && !IsPane(__instance.ComponentID) && !ConsoleLinks.IsConsole(__instance))return;
        __instance.staticRenderer=false;__instance.autoApplyScale=true;__instance.autoFlipModel=true;
    }
    internal static void Tick()
    {
        if(failed || Time.unscaledTime<scanAt)return;scanAt=Time.unscaledTime+.5f;
        try
        {
            foreach(var vehicle in UnityEngine.Object.FindObjectsOfType<Vehicle>())
            {
                if(vehicle==null || vehicle.Released)continue;
                var objects=vehicle.ObjectReader.Items;
                int n=objects.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleObject>>().Count;
                for(int i=0;i<n;i++)
                {
                    var components=objects[i]?.Components;if(components==null)continue;
                    int count=components.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleComponent>>().Count;
                    for(int j=0;j<count;j++)
                    {
                        var c=components[j];if(c==null || (c.ComponentID!=ModelId && !IsPane(c.ComponentID)) || c.models==null)continue;
                        EnsureAssets();
                        bool pane=IsPane(c.ComponentID);
                        var material=pane ? PaneMaterial(c.ComponentID) : nativeGlass!;
                        var selectedMesh=pane ? paneMesh! : mesh!;
                        var materials=pane ? new[]{material} : new[]{material,nativeRim!};
                        foreach(var group in c.models)
                        {
                            if(group?.lods==null)continue;
                            foreach(var lod in group.lods)
                            {
                                var renderer=lod?.Renderer;var filter=renderer?.GetComponent<MeshFilter>();if(filter==null)continue;
                                if(filter.sharedMesh!=selectedMesh){group.SetMesh(selectedMesh);group.SetInteractionMesh(selectedMesh);group.SetCollisionMesh(selectedMesh);}
                                if(group.materials==null || group.materials.Length!=materials.Length || group.materials[0]?.Pointer!=material.Pointer)
                                    group.SetMaterials(new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<VehicleMaterial>(materials));
                            }
                        }
                    }
                }
            }
        }
        catch(Exception ex){failed=true;Plugin.Instance.Log.LogWarning("[Optical glass] Optional glass visuals failed: "+ex.Message);}
    }
    private static void Card(PartDefinition __0,PartDisplayCard __result)
    {
        if(__0==null || __result==null || iconFailed)return;
        string? pane=PaneId(__0.guid);
        if(__0.guid!=Runtime.LensGuid && pane==null)return;
        try
        {
            if(pane!=null)
            {
                if(!paneIcons.TryGetValue(pane,out var paneIcon))
                {
                    var image=new Texture2D(32,32,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave};
                    var color=PaneColor(pane);color.a=1;
                    var pixels=Enumerable.Range(0,1024).Select(i=>i%32<2 || i%32>29 || i/32<2 || i/32>29 ? new Color(.8f,.8f,.8f,1) : color).ToArray();
                    image.SetPixels(pixels);image.Apply();
                    paneIcon=Sprite.Create(image,new Rect(0,0,32,32),new Vector2(.5f,.5f),100);paneIcon.hideFlags=HideFlags.HideAndDontSave;
                    paneIcons.Add(pane,paneIcon);
                }
                __result.Icon=paneIcon;return;
            }
            if(icon==null){EnsureAssets();icon=Sprite.Create(texture!,new Rect(0,0,texture!.width,texture.height),new Vector2(.5f,.5f),100);icon.hideFlags=HideFlags.HideAndDontSave;}
            __result.Icon=icon;
        }
        catch(Exception ex){Plugin.Instance.Log.LogWarning("[Optical glass] Icon unavailable: "+ex.Message);iconFailed=true;}
    }
}
