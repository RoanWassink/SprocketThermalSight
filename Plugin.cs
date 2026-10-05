using BepInEx;
using BepInEx.Configuration;
using SprocketKeybinds;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Sprocket;
using Sprocket.Vehicles;
using Sprocket.Vehicles.AttachedBehaviours;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using PlayerController = Sprocket.Gameplay.VehicleControl.VehicleController;
using NativeScope = Sprocket.Vehicles.Weapons.Scope;

namespace SprocketThermalSight;

[BepInPlugin("nl.roan.sprocket.thermalsight", "Sprocket Thermal Sight", "0.2.4")]
[BepInDependency(Keybinds.PluginGuid, ">=0.1.3 <0.2.0")]
public sealed class Plugin : BasePlugin
{
    internal static Plugin Instance = null!;
    private Harmony? harmony;
    private Harmony? iconHarmony;
    public override void Load()
    {
        Instance = this;
        try
        {
            Runtime.Configure(Config);
            harmony = new Harmony("nl.roan.sprocket.thermalsight");
            harmony.PatchAll(typeof(Hooks));
            harmony.PatchAll(typeof(ProfileStateHooks));
            AddComponent<ThermalDriver>();
            Log.LogInfo("Thermal sight v0.2.4 loaded; shared Settings keybinds and dynamic-resolution rendering supported.");
        }
        catch (Exception ex) { Runtime.Shutdown(); Runtime.ReleaseBindings(); harmony?.UnpatchSelf(); Log.LogError("Thermal disabled: " + ex); return; }
        try
        {
            iconHarmony = new Harmony("nl.roan.sprocket.thermalsight.icon");
            iconHarmony.PatchAll(typeof(IconHooks));
            iconHarmony.PatchAll(typeof(ProfileInspector));
        }
        catch (Exception ex) { iconHarmony?.UnpatchSelf(); Log.LogWarning("Optional thermal icon hook disabled: " + ex.Message); }
    }
    public override bool Unload()
    { Runtime.Shutdown(); Runtime.ReleaseBindings(); iconHarmony?.UnpatchSelf(); IconHooks.Cleanup(); harmony?.UnpatchSelf(); return true; }
}

public sealed class ThermalDriver : MonoBehaviour
{
    public ThermalDriver(IntPtr ptr) : base(ptr) { }
    public void LateUpdate()
    { try { Runtime.Tick(); } catch (Exception ex) { Runtime.Fail(ex); } }
    public void OnDestroy() => Runtime.Shutdown();
}

internal static class Runtime
{
    private static ConfigEntry<Key> toggle = null!, reload = null!;
    private static ModKeybind? toggleBinding, reloadBinding;
    private static bool legacyKeysImported;
    private static ConfigEntry<bool> enabled = null!;
    private static Dictionary<string, ThermalProfile> models = new(StringComparer.Ordinal);
    private static string defaultProfileId = "thermalSightModel3";
    internal const string ConfigurableId = "thermalSight";
    internal const string ConfigurableGuid = "c9f96b83-cd58-47d1-999b-83c5e71a981f";
    private static readonly HashSet<string> legacyIds = new(StringComparer.Ordinal) { "thermalSightModel1", "thermalSightModel2", "thermalSightModel3", "thermalSightMk3WhiteHot", "thermalSightMk3BlackHot" };
    private static readonly HashSet<string> legacyGuids = new(StringComparer.OrdinalIgnoreCase) { "e1e0a2bf-5316-416a-b45e-b8e70b21a151", "40b644a7-90ed-47c7-b78f-e2f7310b80a2", "39614761-b743-408d-b112-9d4c24673c03", "37d49a2d-57ab-598a-8622-e909c48b2ad9", "ae0c73ac-6831-5c43-abf4-118b5352914d" };
    internal static IReadOnlyList<ThermalProfile> Profiles => models.Values.ToArray();
    internal static bool IsThermal(VehicleComponent component) => component != null && component.TryCast<GunnerSight>() != null && (component.ComponentID == ConfigurableId || legacyIds.Contains(component.ComponentID));
    internal static bool IsLegacyGuid(string guid) => legacyGuids.Contains(guid);
    private static string catalogPath = "";
    private static bool active, inputAllowed, stopped;
    private static int playerFrame = -100, scopeFrame = -100, inputFrame = -1;
    private static IntPtr sightId;
    private static GunnerSight? activeSight;
    private static Camera? camera;
    private static PlayerController? player;
    private static GameTime? gameTime;
    private static bool driverReported, playerReported;
    private static float playerLookupAt;
    private static string scopeDescription = "No active scope observed";
    private static ThermalProfile? selected;
    private static DrawRenderersCustomPass? pass;
    private static Material? heatMaterial;
    private static int heatPass;
    private static SensorCapture? sensor;
    private static readonly List<SensorCapture> retired = new();
    private static readonly List<Renderer> targets = new();
    private static float refreshTargetsAt, captureAt;
    private static readonly HashSet<string> warnings = new();

    internal static void Configure(ConfigFile config)
    {
        enabled = config.Bind("General", "Enabled", true, "Enable thermal only on recognized thermal sight parts.");
        toggle = config.Bind("Keys", "Toggle", Key.N, "Legacy import only; edit Thermal / Toggle in Settings / keybinds.");
        reload = config.Bind("Keys", "ReloadProfiles", Key.F8, "Legacy import only; edit Thermal / Reload profiles in Settings / keybinds. Invalid profiles keep the last accepted catalog.");
        catalogPath = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "thermal-models.json");
        Reload(); stopped = false;
        toggleBinding = Keybinds.RegisterButton("nl.roan.sprocket.thermalsight", "Thermal", "toggle", "Toggle", "<Keyboard>/n");
        reloadBinding = Keybinds.RegisterButton("nl.roan.sprocket.thermalsight", "Thermal", "reload-profiles", "Reload profiles", "<Keyboard>/f8");
        legacyKeysImported = false;
        TryImportLegacyKeys();
    }
    private static void TryImportLegacyKeys()
    {
        if (legacyKeysImported || Keybinds.IsConfiguring) return;
        var keyboard = Keyboard.current;
        if (keyboard == null && (toggle.Value != Key.None || reload.Value != Key.None)) return;
        Keybinds.TryImportLegacyBinding(toggleBinding!, toggle.Value == Key.None ? "" : "<Keyboard>/" + keyboard![toggle.Value].name);
        Keybinds.TryImportLegacyBinding(reloadBinding!, reload.Value == Key.None ? "" : "<Keyboard>/" + keyboard![reload.Value].name);
        legacyKeysImported = true;
        Plugin.Instance.Log.LogInfo("[Thermal] Shared keybinds ready; existing API choices preserved, legacy CFG keys imported only on first use.");
    }
    private static void Reload()
    {
        var catalog = ThermalCatalog.Read(catalogPath);
        var next = catalog.Models.ToDictionary(p => p.ComponentId, StringComparer.Ordinal);
        models = next; defaultProfileId = catalog.DefaultProfileId; Deactivate(); selected = null;
        Plugin.Instance.Log.LogInfo($"[Thermal] Accepted {models.Count} part profiles.");
    }
    internal static bool FindProfile(VehicleComponent component, out ThermalProfile p)
    {
        p = null!;
        // Component IDs are deliberately unique fileIDs on the supplied standalone parts.
        // VehicleObject.GUID identifies a placed object and is NOT the asset GUID.
        if (!IsThermal(component)) return false;
        var requested = ProfileStateHooks.Selection(component);
        var id = requested ?? (component.ComponentID == ConfigurableId ? defaultProfileId : component.ComponentID);
        if (models.TryGetValue(id, out p!)) return true;
        Warn($"Saved profile '{id}' is missing; using '{defaultProfileId}' until restored or changed in the inspector.");
        return models.TryGetValue(defaultProfileId, out p!);
    }
    internal static bool OwnsPartGuid(string guid) => string.Equals(guid, ConfigurableGuid, StringComparison.OrdinalIgnoreCase) || IsLegacyGuid(guid);
    internal static void PlayerInput(PlayerController controller, GameTime time, Camera view)
    {
        if (stopped) return;
        player = controller; gameTime = time;
        if (!playerReported) { playerReported = true; Plugin.Instance.Log.LogInfo("[Thermal] Player control hook reached."); }
        playerFrame = Time.frameCount; camera = view;
        inputAllowed = Application.isFocused && time.PauseState == PauseState.Unpaused && time.TimeScale > 0 && time.DeltaTime > 0;
        var go = EventSystem.current?.currentSelectedGameObject;
        if (go != null && ((go.GetComponent<TMP_InputField>()?.isFocused ?? false) || (go.GetComponent<UnityEngine.UI.InputField>()?.isFocused ?? false))) inputAllowed = false;
    }
    internal static void ScopeUpdate(ScopeController controller)
    {
        if (stopped) return;
        var sight = controller.HasActive && controller.Scoped ? controller.ControlledScope?.TryCast<GunnerSight>() : null;
        ThermalProfile? next = null;
        // The native controller already decides whether a sight is usable. Non-damageable
        // optical components need not have a positive HealthFraction.
        activeSight = sight;
        if (sight != null && ThermalEraAccess.Allowed(sight) && FindProfile(sight, out var p)) next = p;
        scopeDescription = $"scoped={controller.Scoped}, hasActive={controller.HasActive}, component={sight?.ComponentID ?? "none"}, health={sight?.HealthFraction.ToString() ?? "n/a"}, profile={next?.DisplayName ?? "none"}";
        IntPtr identity = sight?.Pointer ?? IntPtr.Zero;
        if (identity != sightId || !ReferenceEquals(next, selected))
        { Deactivate(); sightId = identity; selected = next; Plugin.Instance.Log.LogInfo("[Thermal] Sight: " + scopeDescription); }
        scopeFrame = Time.frameCount;
    }
    internal static void Tick()
    {
        for (int i = retired.Count - 1; i >= 0; i--)
            if (!retired[i].Pending) { retired[i].Dispose(); retired.RemoveAt(i); }
        if (stopped) return;
        if (!driverReported) { driverReported = true; Plugin.Instance.Log.LogInfo("[Thermal] Sensor driver ticking."); }
        TryImportLegacyKeys();
        bool inCombat = false;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == "VehicleControlUI") { inCombat = true; break; }
        if (inCombat && (player == null || !player.isActiveAndEnabled) && Time.unscaledTime >= playerLookupAt)
        {
            playerLookupAt = Time.unscaledTime + 1;
            player = UnityEngine.Object.FindObjectsOfType<PlayerController>().FirstOrDefault(p => p != null && p.isActiveAndEnabled && p.ControlledVehicle != null);
        }
        if (inCombat && player != null && player.isActiveAndEnabled && player.ControlledVehicle != null)
        {
            camera = player.ActiveCamera;
            var controller = player.ScopeControl?.TryCast<ScopeController>();
            if (controller != null) ScopeUpdate(controller);
        }
        inputAllowed = inCombat && Application.isFocused && gameTime != null && gameTime.PauseState == PauseState.Unpaused && gameTime.TimeScale > 0 && gameTime.DeltaTime > 0;
        var focused = EventSystem.current?.currentSelectedGameObject;
        if (focused != null && ((focused.GetComponent<TMP_InputField>()?.isFocused ?? false) || (focused.GetComponent<UnityEngine.UI.InputField>()?.isFocused ?? false))) inputAllowed = false;
        bool pressed = toggleBinding!.WasPressedThisFrame && inputFrame != Time.frameCount;
        if (pressed)
            Plugin.Instance.Log.LogInfo($"[Thermal] Toggle pressed: enabled={enabled.Value}, combat={inCombat}, inputAllowed={inputAllowed}, gameClock={gameTime != null}, playerHookAge={Time.frameCount-playerFrame}, scopeAge={Time.frameCount-scopeFrame}, camera={camera?.name ?? "none"}; {scopeDescription}");
        if (!enabled.Value || !Application.isFocused || !inCombat || Time.frameCount - scopeFrame > 1 || selected == null || camera == null)
        { Deactivate(); return; }
        if (inputAllowed && inputFrame != Time.frameCount)
        {
            inputFrame = Time.frameCount;
            if (reloadBinding!.WasPressedThisFrame)
            {
                try { Reload(); } catch (Exception ex) { Warn("Profile reload rejected; previous catalog retained: " + ex.Message); }
                return;
            }
            if (toggleBinding!.WasPressedThisFrame)
            {
                if (active) Deactivate(); else Activate();
            }
        }
        if (active && sensor != null) sensor.UploadIfReady();
    }
    private static void Activate()
    {
        if (selected == null || camera == null || !ThermalEraAccess.Allowed(activeSight)) return;
        if (!SystemInfo.supportsAsyncGPUReadback) throw new NotSupportedException("GPU does not support asynchronous sensor capture.");
        if (heatMaterial == null)
        {
            var shader = Shader.Find("HDRP/Unlit");
            if (shader == null) throw new NotSupportedException("HDRP/Unlit shader missing; native graphics retained.");
            heatMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            heatMaterial.SetColor("_UnlitColor", Color.white);
            heatMaterial.SetColor("_EmissiveColor", Color.black);
            heatMaterial.SetFloat("_ZWrite", 0);
            heatMaterial.SetFloat("_ZTestDepthEqualForOpaque", (float)CompareFunction.Equal);
            heatMaterial.SetFloat("_CullMode", (float)CullMode.Back);
            heatMaterial.SetFloat("_CullModeForward", (float)CullMode.Back);
            heatPass = heatMaterial.FindPass("ForwardOnly");
            if (heatPass < 0) throw new NotSupportedException("HDRP/Unlit ForwardOnly pass missing.");
        }
        pass ??= new DrawRenderersCustomPass
        {
            name = "Roan thermal sensor", layerMask = 0,
            overrideMaterial = heatMaterial, overrideMaterialPassName = "ForwardOnly", overrideMaterialPassIndex = heatPass,
            targetColorBuffer = CustomPass.TargetBuffer.Camera,
            targetDepthBuffer = CustomPass.TargetBuffer.Camera,
            clearFlags = ClearFlag.None
        };
        sensor = new SensorCapture(selected);
        // Use a native pass as the lifecycle host; its zero layerMask draws no stock geometry.
        // Only our instance's Execute postfix appends the heat-mask commands.
        CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforePostProcess, pass, 0);
        active = true; refreshTargetsAt = captureAt = 0;
        Plugin.Instance.Log.LogInfo("[Thermal] Active: " + selected.DisplayName + $" ({selected.Width}x{selected.Height}, {selected.RefreshHz} Hz)");
    }
    internal static void Deactivate()
    {
        bool wasActive = active; active = false;
        if (pass != null) CustomPassVolume.UnregisterGlobalCustomPass(CustomPassInjectionPoint.BeforePostProcess, pass);
        if (sensor != null) { retired.Add(sensor); sensor = null; }
        targets.Clear();
        if (wasActive) Plugin.Instance.Log.LogInfo("[Thermal] Normal sight restored.");
    }
    internal static void Render(DrawRenderersCustomPass instance, CustomPassContext ctx)
    {
        if (!ThermalEraAccess.Allowed(activeSight)) { Deactivate(); return; }
        if (!active || pass == null || instance.Pointer != pass.Pointer || sensor == null || selected == null || camera == null || !inputAllowed) return;
        if (ctx.hdCamera.camera.GetInstanceID() != camera.GetInstanceID()) return;
        if (ctx.hdCamera.viewCount != 1 || ctx.hdCamera.msaaEnabled)
            throw new NotSupportedException("Prototype sensor supports mono, non-MSAA cameras only.");
        if (sensor.Pending || sensor.AwaitingUpload || Time.unscaledTime < captureAt) return;
        if (Time.unscaledTime >= refreshTargetsAt)
        {
            targets.Clear(); var ids = new HashSet<int>();
            foreach (var v in UnityEngine.Object.FindObjectsOfType<Vehicle>())
            {
                if (v == null || v.Released || !v.BehaviourEnabled) continue;
                var register = v.rendererRegister?.renderers;
                if (register == null) continue;
                for (int i = 0; i < register.Count; i++)
                { var r = register[i]?.Renderer; if (r != null && ids.Add(r.GetInstanceID())) targets.Add(r); }
            }
            refreshTargetsAt = Time.unscaledTime + 1;
        }
        sensor.Capture(ctx, targets, heatMaterial!, heatPass);
        captureAt = Time.unscaledTime + 1 / selected.RefreshHz;
    }
    internal static void ReplaceScopeSource(CommandBuffer cmd, HDCamera hdCamera, ref RTHandle source)
    {
        if (!ThermalEraAccess.Allowed(activeSight)) { Deactivate(); return; }
        if (active && inputAllowed && sensor?.HasImage == true && camera != null && hdCamera.camera.GetInstanceID() == camera.GetInstanceID()) source = sensor.ScopeImage(cmd, hdCamera, source);
    }
    internal static void Fail(Exception ex) { Deactivate(); Warn(ex.GetType().Name + ": " + ex.Message); }
    internal static void Warn(string message)
    { if (warnings.Add(message)) Plugin.Instance.Log.LogWarning("[Thermal] " + message); }
    internal static void ReleaseBindings()
    {
        if (toggleBinding != null) { Keybinds.Unregister("nl.roan.sprocket.thermalsight", "toggle"); toggleBinding = null; }
        if (reloadBinding != null) { Keybinds.Unregister("nl.roan.sprocket.thermalsight", "reload-profiles"); reloadBinding = null; }
    }
    internal static void Shutdown()
    {
        if (stopped) return;
        Deactivate(); stopped = true;
        // Callback-owned buffers remain alive until both GPU requests complete.
        foreach (var s in retired) s.RetireForShutdown();
        retired.Clear();
        if (heatMaterial != null) { UnityEngine.Object.Destroy(heatMaterial); heatMaterial = null; }
    }
}

internal static class Hooks
{
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateControl))]
    private static void Input(PlayerController __instance, GameTime __0, Camera __3) { try { Runtime.PlayerInput(__instance, __0, __3); } catch (Exception ex) { Runtime.Fail(ex); } }
    [HarmonyPostfix, HarmonyPatch(typeof(ScopeController), nameof(ScopeController.Update))]
    private static void Sight(ScopeController __instance) { try { Runtime.ScopeUpdate(__instance); } catch (Exception ex) { Runtime.Fail(ex); } }
    [HarmonyPrefix, HarmonyPatch(typeof(ScopeController), nameof(ScopeController.Dispose))]
    private static void Release() => Runtime.Deactivate();
    [HarmonyPostfix, HarmonyPatch(typeof(DrawRenderersCustomPass), nameof(DrawRenderersCustomPass.Execute), new[] { typeof(CustomPassContext) })]
    private static void Sensor(DrawRenderersCustomPass __instance, CustomPassContext __0) { try { Runtime.Render(__instance, __0); } catch (Exception ex) { Runtime.Fail(ex); } }
    [HarmonyPrefix, HarmonyPatch(typeof(NativeScope), nameof(NativeScope.Render))]
    private static void ScopeImage(CommandBuffer __0, HDCamera __1, ref RTHandle __2) { try { Runtime.ReplaceScopeSource(__0, __1, ref __2); } catch (Exception ex) { Runtime.Fail(ex); } }
    [HarmonyPostfix, HarmonyPatch(typeof(VehicleComponent), nameof(VehicleComponent.GetMass))]
    private static void Mass(VehicleComponent __instance, MassType __0, ref float __result)
    { if ((__0 & MassType.Mechanisms) != 0 && Runtime.FindProfile(__instance, out var p)) __result += p.ExtraMassKg; }
    [HarmonyPostfix, HarmonyPatch(typeof(VehicleComponent), nameof(VehicleComponent.GetCost))]
    private static void Cost(VehicleComponent __instance, MassType __0, CostType __1, ref float __result)
    { if ((__0 & MassType.Mechanisms) != 0 && (__1 & CostType.Assembly) != 0 && Runtime.FindProfile(__instance, out var p)) __result += p.ExtraAssemblyCost; }
}








