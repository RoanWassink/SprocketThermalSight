using HarmonyLib;
using Il2CppInterop.Runtime;
using Sprocket.UI;
using Sprocket.VehicleDesigner.Modules;
using Sprocket.Vehicles;
using Sprocket.Vehicles.AttachedBehaviours;
using UnityEngine.Events;
using SerializationInfo = Il2CppSystem.Runtime.Serialization.SerializationInfo;

namespace SprocketThermalSight;

internal static class ProfileStateHooks
{
    private const string Key = "roanThermalProfile";
    private sealed record Choice(VehicleComponent Component, string Id);
    private static readonly Dictionary<IntPtr, Choice> Choices = new();
    internal static string? Selection(VehicleComponent component) => Choices.TryGetValue(component.Pointer, out var choice) ? choice.Id : null;
    internal static void Select(VehicleComponent component, string id)
    {
        if (!ThermalEraAccess.Allowed(component)) return;
        Choices[component.Pointer] = new(component, id);
        component.RequestRebuild();
        Runtime.Deactivate();
        Plugin.Instance.Log.LogInfo($"[Thermal] Sight {component.VUID.Value} selected {id}.");
    }
    [HarmonyPostfix, HarmonyPatch(typeof(VehicleComponent), nameof(VehicleComponent.SaveData))]
    private static void Save(VehicleComponent __instance, SerializationInfo __0)
    {
        if (!Runtime.IsThermal(__instance)) return;
        try
        {
            var id = Selection(__instance);
            if (id == null && Runtime.FindProfile(__instance, out var profile)) id = profile.ComponentId;
            if (id != null) __0.AddValue(Key, id);
        }
        catch (Exception ex) { Runtime.Warn("Save profile: " + ex); }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(VehicleComponent), nameof(VehicleComponent.LoadData))]
    private static void Load(VehicleComponent __instance, SerializationInfo __0)
    {
        if (!Runtime.IsThermal(__instance)) return;
        try
        {
            Choices.Remove(__instance.Pointer);
            var entries = __0.GetEnumerator();
            while (entries.MoveNext())
                if (entries.Name == Key)
                {
                    var id = __0.GetString(Key);
                    if (!string.IsNullOrWhiteSpace(id)) Choices[__instance.Pointer] = new(__instance, id);
                    break;
                }
        }
        catch (Exception ex) { Runtime.Warn("Load profile: " + ex); }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(VehicleComponent), nameof(VehicleComponent.BuildInternal))]
    private static void MassCache(VehicleComponent __instance)
    {
        if (!Runtime.IsThermal(__instance)) return;
        try { __instance.cachedMass = __instance.GetMass(MassType.Everything); __instance.RecalculateCenterOfMass(); }
        catch (Exception ex) { Runtime.Warn("Profile mass refresh: " + ex); }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(VehicleComponent), nameof(VehicleComponent.ReleaseInternal))]
    private static void Release(VehicleComponent __instance) => Choices.Remove(__instance.Pointer);
}

internal static class ProfileInspector
{
    private static bool open = true;
    [HarmonyPostfix, HarmonyPatch(typeof(VehicleObjectEditor), nameof(VehicleObjectEditor.OnGUI))]
    private static void Draw(VehicleObjectEditor __instance, IGUILayout __0)
    {
        try
        {
            var sight = __instance.target?.GetBehaviour<GunnerSight>();
            if (sight == null || !Runtime.FindProfile(sight, out var current)) return;
            var ui = __0.TryCast<IGUIElementDrawer>();
            if (ui == null) return;
            if (!ThermalEraAccess.Allowed(sight))
            {
                ui.InfoField("Thermal available from 1945-09-03; saved profile retained, normal sight active.", 2);
                return;
            }
            var profiles = Runtime.Profiles.ToArray();
            var labels = new Il2CppSystem.Collections.Generic.List<string>();
            foreach (var p in profiles) labels.Add(p.DisplayName);
            __0.EndAllDropdowns();
            __0.BeginDropdown("Thermal sight", open, DelegateSupport.ConvertDelegate<Il2CppSystem.Action<bool>>((Action<bool>)(v => open = v))!);
            try
            {
                ui.Dropdown("Profile", labels.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<string>>(),
                    Array.FindIndex(profiles, p => p.ComponentId == current.ComponentId),
                    DelegateSupport.ConvertDelegate<UnityAction<int>>((Action<int>)(index =>
                    {
                        try
                        {
                            if (index < 0 || index >= profiles.Length || sight.VehicleObject.Released) return;
                            ProfileStateHooks.Select(sight, profiles[index].ComponentId);
                            __instance.RequestRedraw();
                        }
                        catch (Exception ex) { Runtime.Warn("Select profile: " + ex); }
                    }))!, "Saved separately on this sight. Profiles come from thermal-models.json.");
                ui.InfoField($"{current.Width} x {current.Height} | {current.RefreshHz:0.#} Hz | {current.Palette}", 2);
                ui.InfoField($"Electronics: +{current.ExtraMassKg:0.#} kg | +{current.ExtraAssemblyCost:0.#} assembly cost", 2);
                ui.InfoField("Controls: Settings / keybinds - Thermal / Toggle", 2);
            }
            finally { __0.EndAllDropdowns(); }
        }
        catch (Exception ex) { Runtime.Warn("Thermal inspector: " + ex); }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(Sprocket.PartImporting.PartDefinitionCardFactory), nameof(Sprocket.PartImporting.PartDefinitionCardFactory.GetAllWithTags), new[] { typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray) })]
    private static void HideLegacy(ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Sprocket.AssetManagement.IAssetInfo> __result)
    {
        if (__result == null) return;
        __result = new(__result.Where(card => card?.TryCast<Sprocket.PartImporting.PartDisplayCard>() is not {} part || !Runtime.IsLegacyGuid(part.PartGuid)).ToArray());
    }
}




