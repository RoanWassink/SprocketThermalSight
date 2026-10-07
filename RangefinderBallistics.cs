using HarmonyLib;
using Sprocket.Vehicles;
using Sprocket.Vehicles.Cannons;
using Sprocket.Vehicles.AttachedBehaviours;
using Sprocket.Vehicles.CrewSystems;
using UnityEngine;

namespace SprocketThermalSight;

// Read native current cannon speed; change only the active player's linked gun layer request.
// Native interception accounts for gravity, not projectile drag or motor-driven missiles.
internal static class RangefinderBallistics
{
    private static int logs;
    private static bool warned;
    private static System.Reflection.MethodInfo? shellProfile;
    private static bool profileLookup;
    internal static Cannon? LinkedCannon(GunnerSight sight)
    {
        if(sight.LinkedWeapon==null)return null;
        var objects=sight.Vehicle?.ObjectReader?.Items;if(objects==null)return null;
        int n=objects.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleObject>>().Count;
        Cannon? assigned=null;
        for(int i=0;i<n;i++)
        {
            var cannon=objects[i]?.GetBehaviour<Cannon>();
            if(cannon?.Behaviour==null || cannon.Vehicle?.Pointer!=sight.Vehicle!.Pointer)continue;
            if(cannon.Behaviour.Transform?.Pointer==sight.LinkedWeapon.Pointer)return cannon;
            if(cannon.AssignedSightVUID.Value==sight.VUID.Value)
            {if(assigned!=null)return null;assigned=cannon;}
        }
        return assigned;
    }
    internal static bool Guided(Cannon? cannon)
    {
        if(cannon==null)return false;
        if(!profileLookup)
        {
            profileLookup=true;
            shellProfile=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="SprocketShellSelector")?
                .GetType("SprocketShellSelector.RuntimeShellSelection")?.GetMethod("CannonProfile",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
        }
        var profile=shellProfile?.Invoke(null,new object[]{cannon});
        return (profile?.GetType().GetProperty("Behavior")?.GetValue(profile) as string)?.StartsWith("atgm",StringComparison.OrdinalIgnoreCase)==true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(GunLayer), nameof(GunLayer.AimAtPosition))]
    private static void Aim(GunLayer __instance, ref Vector3 __0)
    {
        try
        {
            if (!Rangefinder.BallisticContext(out var player, out var sight, out var range)
                || player.gunLayers?.Any(layer => layer?.Pointer == __instance.Pointer) != true
                || sight.LinkedWeapon == null) return;
            var linked=LinkedCannon(sight);
            if (linked?.Behaviour == null || linked.HealthFraction <= 0 || !float.IsFinite(linked.muzzleVelocity) || linked.muzzleVelocity < 50) return;
            // Soft read-only integration: preserve missile guidance; no dependency or rebuild of Shell Selector.
            if(Guided(linked))return;
            var mechanism = __instance.ControlTarget;
            if (mechanism == null || (linked.HorizontalAimingMechanism?.Pointer != mechanism.Pointer && linked.VerticalAimingMechanism?.Pointer != mechanism.Pointer)) return;
            // The sampled distance follows the current sight ray; it is not a target lock.
            var scope = player.ScopeControl.TryCast<ScopeController>();
            if (scope == null) return;
            var lens = sight.SightParent.TransformPoint(sight.ObjectiveLensLocalPosition);
            // ScopeAimPoint is a convergence-dependent point, not an independent
            // sight ray. Use the current native input direction for measurement
            // and compensation alike; never feed a derived convergence point back.
            var direction = player.AimRay.direction;
            if (direction.sqrMagnitude < .000001f || !float.IsFinite(direction.x) || !float.IsFinite(direction.y) || !float.IsFinite(direction.z)) return;
            var target = lens+direction.normalized*range;
            var muzzle = linked.Behaviour.ShellSpawnPosition;
            // Do not pretend a gravity-only solution supports a strongly moving firing platform.
            if (linked.Behaviour.InheritedVelocity.sqrMagnitude > 1) return;
            var correction = Sprocket.PhysicsRules.Ballistics.CalculateInterceptPoint(target, Vector3.zero, muzzle, linked.muzzleVelocity, 6);
            if (!float.IsFinite(correction.x) || !float.IsFinite(correction.y) || !float.IsFinite(correction.z)
                || Vector3.Distance(correction,target) > range*.25f) return;
            var input=__0;__0 = correction;
            if (logs++ < 8) Plugin.Instance.Log.LogInfo($"[Rangefinder FCS] Input-ray correction range={range:F1}m speed={linked.muzzleVelocity:F1}m/s lift={correction.y-target.y:F3}m scopeRayDelta={Vector3.Angle(scope.ScopeAimPoint-lens,direction):F3}deg targetChange={Vector3.Distance(input,target):F2}m lens={lens} muzzle={muzzle} target={target} cannon={linked.VUID.Value}.");
        }
        catch (Exception ex)
        {
            if (!warned) { warned = true; Plugin.Instance.Log.LogWarning("[Rangefinder FCS] Correction unavailable; native aiming retained: " + ex.Message); }
        }
    }
}
