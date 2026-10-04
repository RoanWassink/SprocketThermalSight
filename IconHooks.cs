using HarmonyLib;
using Sprocket.PartImporting;
using UnityEngine;

namespace SprocketThermalSight;

internal static class IconHooks
{
    private static Texture2D? texture;
    private static Sprite? sprite;
    private static bool attempted;

    [HarmonyPostfix, HarmonyPatch(typeof(PartDefinitionCardFactory), nameof(PartDefinitionCardFactory.CreateCard))]
    private static void Card(PartDefinition __0, PartDisplayCard __result)
    {
        if (__0 == null || __result == null || !Runtime.OwnsPartGuid(__0.guid)) return;
        try
        {
            if (!attempted)
            {
                attempted = true;
                string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "assets", "thermal-sight-icon.png");
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false)) throw new InvalidDataException("Thermal sight icon could not be decoded.");
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                Plugin.Instance.Log.LogInfo("[Thermal] Shared thermal sight icon loaded.");
            }
            if (sprite != null) __result.Icon = sprite;
        }
        catch (Exception ex) { Runtime.Warn("Optional part icon: " + ex.Message); }
    }
    internal static void Cleanup()
    {
        if (sprite != null) UnityEngine.Object.Destroy(sprite);
        if (texture != null) UnityEngine.Object.Destroy(texture);
        sprite = null; texture = null; attempted = false;
    }
}
