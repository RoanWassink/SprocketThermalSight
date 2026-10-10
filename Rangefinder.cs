using BepInEx.Configuration;
using HarmonyLib;
using Sprocket;
using Sprocket.Vehicles;
using Sprocket.Vehicles.AttachedBehaviours;
using SprocketKeybinds;
using UnityEngine;
using PlayerController = Sprocket.Gameplay.VehicleControl.VehicleController;

namespace SprocketThermalSight;

internal static class Rangefinder
{
    internal const string ComponentId = "laserRangefinderSight";
    private const string Owner = "sprocket.thermalsight";
    private static ModKeybind? measure;
    private static ConfigEntry<bool> enabled = null!;
    private static ConfigEntry<float> maximum = null!, minimum = null!, cooldown = null!, duration = null!;
    private static PlayerController? player;
    private static GameTime? time;
    private static int frame = -10;
    private static IntPtr sightId;
    private static string? result;
    private static float expires;
    private static IntPtr resultVehicle;
    private static readonly Dictionary<IntPtr,float> nextByVehicle = new();
    private static bool ready, hudFailed, measurementFailed, warningLogged;
    private static int readingLogs;
    private static int eligibilityLogs;
    private static VehicleComponent? pendingDevice;
    private static double pendingDistance, pendingBaseline;
    private static float completesAt;
    private static Vector3 pendingDirection;
    private static RangefinderKind activeKind;
    private static float ballisticRange;
    internal static bool BallisticContext(out PlayerController controller, out GunnerSight sight, out float range)
    {
        controller = player!; sight = Current()!; range = ballisticRange;
        return ballisticRange >= 50 && sight != null && player != null && Application.isFocused && !Keybinds.IsConfiguring
            && time?.PauseState == PauseState.Unpaused && Time.frameCount-frame <= 1
            && RangefinderDevicePolicy.Kind(FindDevice(sight)?.ComponentID) == RangefinderKind.AutomaticLaser;
    }
    internal static double Baseline(VehicleComponent component)
    {
        var models = component.models;
        if (models != null && models.Count > 0 && models[0]?.lods?.Length > 0)
        {
            var transform = models[0].lods[0]?.Transform;
            if (transform != null) return Vector3.Distance(transform.TransformPoint(Vector3.right), transform.TransformPoint(-Vector3.right));
        }
        return 2 * Math.Abs(component.Scale.x);
    }

    internal static void Configure(ConfigFile config)
    {
        enabled = config.Bind("Rangefinder", "Enabled", true, "Enable manual ranging through any active sight when this vehicle has a Laser rangefinder device fitted.");
        maximum = config.Bind("Rangefinder", "MaximumDistanceMetres", 4000f,
            new ConfigDescription("Maximum measured line-of-sight distance in metres. Gameplay limit, not a hardware specification.", new AcceptableValueRange<float>(100, 10000)));
        minimum = config.Bind("Rangefinder", "MinimumDistanceMetres", 50f,
            new ConfigDescription("Closest measurable solid surface, in metres; nearer obstructions produce Too close instead of seeing through them.", new AcceptableValueRange<float>(0, 1000)));
        cooldown = config.Bind("Rangefinder", "CooldownSeconds", 3f,
            new ConfigDescription("Minimum time between measurements, in seconds.", new AcceptableValueRange<float>(.1f, 30)));
        duration = config.Bind("Rangefinder", "DisplaySeconds", 5f,
            new ConfigDescription("How long a reading stays visible; it does not track the target.", new AcceptableValueRange<float>(1, 30)));
        Keybinds.TryMigrateBindingIdentity("nl.roan.sprocket.thermalsight", "rangefinder-measure", Owner, "rangefinder-measure");
        measure = Keybinds.RegisterButton(Owner, "Thermal", "rangefinder-measure", "Measure range", "");
        ready = true;
    }
    internal static bool Own(VehicleComponent? component) => RangefinderDevicePolicy.IsStandalone(component?.ComponentID);
    internal static void Observe(PlayerController controller, GameTime gameTime)
    { player = controller; time = gameTime; frame = Time.frameCount; }
    private static GunnerSight? Current()
    {
        if (player == null || !player.isActiveAndEnabled || player.ControlledVehicle == null) return null;
        var scope = player?.ScopeControl?.TryCast<ScopeController>();
        return scope?.Scoped == true && scope.HasActive
            ? scope.ControlledScope?.TryCast<GunnerSight>() : null;
    }
    // Resolve from the active player's own sight gateway, never from a global vehicle search.
    // Scan at use time rather than caching across edits, spawn boundaries or part removal.
    private static VehicleComponent? FindDevice(GunnerSight? sight)
    {
        if (sight == null || player?.ControlledVehicle == null) return null;
        var vehicle = sight.Vehicle;
        if (vehicle == null || player.ControlledVehicle.VehicleStructure?.Pointer != vehicle.Pointer) return null;
        var objects = vehicle?.ObjectReader?.Items;
        if (objects == null) return null;
        VehicleComponent? best = null;
        int count = objects.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleObject>>().Count;
        for (int i = 0; i < count; i++)
        {
            var components = objects[i]?.Components;
            if (components == null) continue;
            int componentCount = components.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleComponent>>().Count;
            for (int j = 0; j < componentCount; j++)
            {
                var component = components[j];
                var kind = RangefinderDevicePolicy.Kind(component?.ComponentID);
                if (kind == RangefinderKind.None || component == null || !component.IsInstalled || component.Vehicle?.Pointer != vehicle!.Pointer || !ThermalEraAccess.Allowed(component)) continue;
                if (kind == RangefinderKind.Optical && (!float.IsFinite(component.HealthFraction) || component.HealthFraction <= 0)) continue;
                if (best == null || (int)kind > (int)RangefinderDevicePolicy.Kind(best.ComponentID)) best = component;
            }
        }
        return best;
    }
    internal static void Tick()
    {
        if (!ready) return;
        var sight = Current();
        var paused = time == null || time.PauseState != PauseState.Unpaused || time.TimeScale <= 0 || time.DeltaTime <= 0;
        var device = FindDevice(sight);
        bool fitted = device != null;
        if (!paused && measure?.WasPressedThisFrame == true && eligibilityLogs++ < 5)
            Plugin.Instance.Log.LogInfo($"[Rangefinder] Request sight={sight?.ComponentID ?? "none"}, device={fitted}, sightOwner={sight?.Vehicle?.Pointer}, playerOwner={player?.ControlledVehicle?.VehicleStructure?.Pointer}.");
        if (!RangefinderPolicy.CanMeasure(Application.isFocused, paused, enabled.Value, sight != null, fitted, Time.frameCount-frame)
            || Keybinds.IsConfiguring)
        { Clear(); return; }
        var owner = sight!.Vehicle.Pointer;
        if (sightId != sight.Pointer || resultVehicle != owner) { Clear(); sightId = sight.Pointer; resultVehicle = owner; }
        var scope = player!.ScopeControl.TryCast<ScopeController>()!;
        var origin = sight.SightParent.TransformPoint(sight.ObjectiveLensLocalPosition);
        var direction = scope.ScopeAimPoint - origin;
        if(RangefinderDevicePolicy.Kind(device!.ComponentID)==RangefinderKind.AutomaticLaser)
            direction=player.AimRay.direction;
        if (pendingDevice != null)
        {
            if (pendingDevice.Pointer != device!.Pointer || direction.sqrMagnitude < .000001f || Vector3.Dot(direction.normalized, pendingDirection) < .99996f)
            { pendingDevice = null; Show("Measurement cancelled"); return; }
            if (Time.unscaledTime < completesAt) return;
            var estimate = RangefinderDevicePolicy.OpticalReading(pendingDistance, pendingBaseline);
            pendingDevice = null;
            Show($"{estimate:0} m");
            if (readingLogs++ < 10) Plugin.Instance.Log.LogInfo($"[Rangefinder] Optical true={pendingDistance:F1}m estimated={estimate:F1}m baseline={pendingBaseline:F2}m.");
            return;
        }
        if (measurementFailed || measure?.WasPressedThisFrame != true || (nextByVehicle.TryGetValue(owner, out var next) && Time.unscaledTime < next)) return;
        // Bounded session ledger: never clear it on sight changes, part duplication or removal.
        if (!nextByVehicle.ContainsKey(owner) && nextByVehicle.Count >= 128) { Show("Session rangefinder limit"); return; }
        activeKind = RangefinderDevicePolicy.Kind(device!.ComponentID);
        var max = activeKind == RangefinderKind.Optical ? 3000 : activeKind == RangefinderKind.AutomaticLaser ? 4000 : maximum.Value;
        var min = activeKind == RangefinderKind.ManualLaser ? minimum.Value : 50;
        nextByVehicle[owner] = Time.unscaledTime + (activeKind == RangefinderKind.ManualLaser ? cooldown.Value : 3);
        if (direction.sqrMagnitude < .000001f) { Show("No valid aiming ray"); return; }
        var hits = Physics.RaycastAll(new Ray(origin, direction.normalized), max, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        var distances = new List<double>();
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            // Exclude the entire controlled vehicle hierarchy, including its own hull and fitted equipment.
            var own = player.ControlledVehicleGameObject;
            if (own != null && hit.collider.transform.IsChildOf(own.transform)) continue;
            distances.Add(hit.distance);
        }
        var distance = RangefinderPolicy.Nearest(distances, max);
        if (activeKind == RangefinderKind.Optical && distance != null && distance >= min)
        {
            pendingDevice = device; pendingDistance = distance.Value; pendingBaseline = Baseline(device);
            if (!double.IsFinite(pendingBaseline) || pendingBaseline <= 0) { pendingDevice = null; Show("Invalid optical baseline"); return; }
            pendingDirection = direction.normalized;
            completesAt = Time.unscaledTime + (float)RangefinderDevicePolicy.OpticalDelay(distance.Value);
            nextByVehicle[owner] = completesAt + 2;
            Show($"Measuring... ({pendingBaseline:F2} m base)"); expires = completesAt + duration.Value;
            return;
        }
        Show(RangefinderPolicy.Readout(distance, min));
        ballisticRange = 0;
        if (activeKind == RangefinderKind.AutomaticLaser && distance != null && distance >= min)
        {
            if(RangefinderBallistics.Guided(RangefinderBallistics.LinkedCannon(sight)))
            {
                Show($"{distance.Value:0} m / guided missile: no elevation setting");
                return;
            }
            float setting = Mathf.Clamp((float)(Math.Round(distance.Value / 10, MidpointRounding.AwayFromZero) * 10), ScopeController.MinConvergance, ScopeController.MaxConvergance);
            try
            {
                scope.SetGunConvergance(setting);
                ballisticRange = (float)distance.Value;
                Show($"{distance.Value:0} m / set {scope.GunConverganceDistance:0} m");
            }
            catch (Exception ex)
            {
                Show($"{distance.Value:0} m / setting unavailable");
                if (!warningLogged) { warningLogged = true; Plugin.Instance.Log.LogWarning("[Rangefinder] Native range setting failed; manual readings retained: " + ex.Message); }
            }
        }
        if (readingLogs++ < 5) Plugin.Instance.Log.LogInfo($"[Rangefinder] Reading={result}; externalHits={distances.Count}; sight={sight.ComponentID}.");
    }
    private static void Show(string text) { result = text; expires = Time.unscaledTime + duration.Value; }
    internal static void Draw()
    {
        if (!ready || hudFailed || result == null || Time.unscaledTime > expires || Time.frameCount-frame > 1 || sightId == IntPtr.Zero
            || !Application.isFocused || Keybinds.IsConfiguring || FindDevice(Current()) == null || time == null || time.PauseState != PauseState.Unpaused) return;
        var label = (activeKind == RangefinderKind.Optical ? "OPTICAL  " : "LASER  ") + result;
        GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 60, 380, 35), label);
    }
    internal static void Fail(Exception exception, bool hud = false)
    {
        if (hud) hudFailed = true; else measurementFailed = true;
        Clear();
        if (!warningLogged) { warningLogged = true; Plugin.Instance.Log.LogWarning("[Rangefinder] Optional ranging disabled; native sights and thermals retained: " + exception.Message); }
    }
    internal static void Clear() { ballisticRange = 0; pendingDevice = null; sightId = IntPtr.Zero; resultVehicle = IntPtr.Zero; result = null; }
    internal static void Shutdown()
    {
        ready = false; Clear(); player = null; time = null; nextByVehicle.Clear();
        if (measure != null) { Keybinds.Unregister(Owner, "rangefinder-measure"); measure = null; }
    }
}

internal static class RangefinderHooks
{
    [HarmonyPrefix, HarmonyPatch(typeof(VehicleObjectModel), nameof(VehicleObjectModel.Build))]
    private static void Prepare(VehicleObjectModel __instance)
    {
        if (!Rangefinder.Own(__instance)) return;
        __instance.autoApplyScale = true; __instance.autoFlipModel = true;
        if (RangefinderDevicePolicy.Kind(__instance.ComponentID) == RangefinderKind.Optical) __instance.generateDamageColliders = true;
    }
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerController), nameof(PlayerController.UpdateControl))]
    private static void Input(PlayerController __instance, GameTime __0)
    { try { Rangefinder.Observe(__instance, __0); } catch (Exception ex) { Rangefinder.Fail(ex); } }
    [HarmonyPostfix, HarmonyPatch(typeof(VehicleObjectModel), nameof(VehicleObjectModel.Build))]
    private static void Build(VehicleObjectModel __instance)
    {
        if (!Rangefinder.Own(__instance)) return;
        try
        {
            // Native model adapters charge their mass to Armour; clear that resource first.
            __instance.SetMass(0, MassType.Armour);
            __instance.SetCost(0, MassType.Armour, CostType.Material);
            var kind = RangefinderDevicePolicy.Kind(__instance.ComponentID);
            var baseline = Rangefinder.Baseline(__instance);
            __instance.SetMass(kind == RangefinderKind.Optical ? (float)RangefinderDevicePolicy.Mass(baseline) : kind == RangefinderKind.AutomaticLaser ? 40 : 35, MassType.Mechanisms);
            __instance.SetCost(0, MassType.Mechanisms, CostType.Material);
            __instance.SetCost(kind == RangefinderKind.Optical ? (float)RangefinderDevicePolicy.Cost(baseline) : kind == RangefinderKind.AutomaticLaser ? 800 : 400, MassType.Mechanisms, CostType.Assembly);
            __instance.cachedMass = __instance.GetMass(MassType.Everything);
            __instance.RecalculateCenterOfMass();
        }
        catch (Exception ex) { Rangefinder.Fail(ex); }
    }
}






