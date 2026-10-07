using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Sprocket;
using Sprocket.AssetManagement;
using Sprocket.PartImporting;
using UnityEngine;

namespace SprocketThermalSight;

internal static class ElectronicsIcons
{
    private static readonly Dictionary<string,string> files=new(StringComparer.Ordinal);
    private static readonly Dictionary<string,Sprite> sprites=new(StringComparer.Ordinal);
    private static readonly Dictionary<string,Texture2D> textures=new(StringComparer.Ordinal);
    private static readonly HashSet<string> failures=new(StringComparer.Ordinal);
    private static int logs;
    private static string? File(string? key)
    {
        if(files.Count==0)
        {
            void Add(string guid,string id,string file){files[guid]=file;files[id]=file;}
            Add(Runtime.ConfigurableGuid,Runtime.ConfigurableId,"thermal-head-icon.png");
            Add(Runtime.VerticalGuid,Runtime.VerticalId,"thermal-head-vertical-icon.png");
            Add("e1e0a2bf-5316-416a-b45e-b8e70b21a151","thermalSightModel1","thermal-head-icon.png");
            Add("40b644a7-90ed-47c7-b78f-e2f7310b80a2","thermalSightModel2","thermal-head-icon.png");
            Add("39614761-b743-408d-b112-9d4c24673c03","thermalSightModel3","thermal-head-icon.png");
            Add("37d49a2d-57ab-598a-8622-e909c48b2ad9","thermalSightMk3WhiteHot","thermal-head-icon.png");
            Add("ae0c73ac-6831-5c43-abf4-118b5352914d","thermalSightMk3BlackHot","thermal-head-icon.png");
            Add(RangefinderAssets.Guid,"laserRangefinderSight","lrf-icon-concept.png");
            Add("af74a44d-65e0-44ae-8ac3-61198eeb1b30","automaticLaserRangefinder","automatic-lrf-icon.png");
            Add("b93efc74-6702-4d2e-9211-dfe313ffdab3","opticalCoincidenceRangefinder","optical-rangefinder-icon.png");
            Add("2879aef5-e6b0-4bdd-8b1e-e9803404a01e","t72StyleSight","t72-style-sight-icon.png");
            Add(ConsoleLinks.Guid,ConsoleLinks.Id,"fcs-console-icon.png");
            Add(Runtime.LensGuid,"thermalSightLens","optical-glass-icon.png");
            Add("379c5275-72ad-4a2d-9e8c-ce183164ea70","clearWindowPane","clear-window-icon.png");
            Add("d6bc56a1-a1f9-4314-aed4-9a953031f8db","tintedWindowPane","tinted-window-icon.png");
            Add("862d1452-9b48-4efc-bafd-a7eb8eac6117","yellowWindowPane","yellow-window-icon.png");
        }
        return key!=null && files.TryGetValue(key,out var f) ? f : null;
    }
    private static Sprite? Load(string? key)
    {
        var file=File(key);if(file==null || failures.Contains(file))return null;
        if(sprites.TryGetValue(file,out var cached))return cached;
        try
        {
            var path=Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!,"assets",file);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave};textures[file]=texture;
            if(!ImageConversion.LoadImage(texture,System.IO.File.ReadAllBytes(path),false))throw new InvalidDataException("Icon PNG decode failed");
            var icon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);icon.hideFlags=HideFlags.HideAndDontSave;sprites[file]=icon;
            if(logs++<12)Plugin.Instance.Log.LogInfo("[Electronics icon] Loaded "+file);return icon;
        }
        catch(Exception ex){failures.Add(file);Runtime.Warn("Electronics icon "+file+": "+ex.Message);return null;}
    }
    private static void Apply(PartDisplayCard? card)
    {if(card==null)return;var icon=Load(card.PartGuid) ?? Load(card.Identifier);if(icon!=null)card.Icon=icon;}
    [HarmonyPostfix,HarmonyPatch(typeof(GameIcons),nameof(GameIcons.GetOrDefault))]
    private static void Lookup(string __0,ref Sprite __result)
    {var icon=Load(__0);if(icon!=null)__result=icon;}
    [HarmonyPostfix,HarmonyPatch(typeof(PartDefinitionCardFactory),nameof(PartDefinitionCardFactory.CreateCard))]
    private static void Created(PartDisplayCard __result)=>Apply(__result);
    // Cover already-cached cards as well as newly created ones.
    [HarmonyPostfix,HarmonyPatch(typeof(PartDefinitionCardFactory),nameof(PartDefinitionCardFactory.GetAllWithTags),new[]{typeof(Il2CppStringArray)})]
    private static void Listed(Il2CppReferenceArray<IAssetInfo> __result)
    {if(__result!=null)foreach(var item in __result)Apply(item?.TryCast<PartDisplayCard>());}
    [HarmonyPostfix,HarmonyPatch(typeof(PartDefinitionCardFactory),nameof(PartDefinitionCardFactory.GetByIdentifier))]
    private static void Retrieved(IAssetInfo __result)=>Apply(__result?.TryCast<PartDisplayCard>());
    internal static void Cleanup()
    {foreach(var icon in sprites.Values)UnityEngine.Object.Destroy(icon);foreach(var texture in textures.Values)UnityEngine.Object.Destroy(texture);sprites.Clear();textures.Clear();failures.Clear();}
}
