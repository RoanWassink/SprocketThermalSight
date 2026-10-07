using HarmonyLib;
using Sprocket.PartImporting;
using UnityEngine;

namespace SprocketThermalSight;

internal static class IconHooks
{
    private static readonly Dictionary<bool,Texture2D> textures=new();
    private static readonly Dictionary<bool,Sprite> sprites=new();
    private static readonly HashSet<bool> attempted=new();

    [HarmonyPostfix, HarmonyPatch(typeof(PartDefinitionCardFactory), nameof(PartDefinitionCardFactory.CreateCard))]
    private static void Card(PartDefinition __0, PartDisplayCard __result)
    {
        if (__0 == null || __result == null || !Runtime.OwnsPartGuid(__0.guid)) return;
        try
        {
            bool vertical=string.Equals(__0.guid,Runtime.VerticalGuid,StringComparison.OrdinalIgnoreCase);
            if (attempted.Add(vertical))
            {
                
                string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "assets", vertical ? "thermal-head-vertical-icon.png" : "thermal-head-icon.png");
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false)) throw new InvalidDataException("Thermal sight icon could not be decoded.");
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                textures[vertical]=texture;sprites[vertical]=sprite;
                Plugin.Instance.Log.LogInfo("[Thermal] Shared thermal sight icon loaded.");
            }
            if (sprites.TryGetValue(vertical,out var selected)) __result.Icon = selected;
        }
        catch (Exception ex) { Runtime.Warn("Optional part icon: " + ex.Message); }
    }
    internal static void Cleanup()
    {
        foreach(var sprite in sprites.Values) UnityEngine.Object.Destroy(sprite);
        foreach(var texture in textures.Values) UnityEngine.Object.Destroy(texture);
        sprites.Clear();textures.Clear();attempted.Clear();
    }
}
