using HarmonyLib;
using Il2CppInterop.Runtime;
using Sprocket.UI;
using Sprocket.VehicleDesigner.Modules;
using Sprocket.Vehicles;
using Sprocket.Vehicles.AttachedBehaviours;
using Sprocket.Vehicles.Cannons;
using UnityEngine;
using UnityEngine.Events;
using SerializationInfo = Il2CppSystem.Runtime.Serialization.SerializationInfo;

namespace SprocketThermalSight;

internal static class ConsoleLinks
{
    internal const string Id = "sightConsole";
    internal const string Guid = "ef0537bb-b57e-4e96-a65c-48f757a5644d";
    private const string Key = "thermalConsoleVuid";
    private sealed record Link(VehicleComponent Owner, int Target);
    private static readonly Dictionary<IntPtr, Link> links = new();
    private static readonly HashSet<IntPtr> routed = new();
    private static readonly Dictionary<IntPtr,int> reported = new();
    private static int logs;
    private static float scanAt;
    internal static bool IsConsole(VehicleComponent c) => c != null && c.ComponentID == Id && c.TryCast<VehicleObjectModel>() != null;
    internal static IEnumerable<VehicleComponent> Components(VehicleComponent owner)
    {
        var items=owner.Vehicle?.ObjectReader?.Items;if(items==null)yield break;
        int n=items.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleObject>>().Count;
        for(int i=0;i<n;i++)
        {
            var obj=items[i];if(obj==null || obj.Released)continue;
            var list=obj.Components;if(list==null)continue;
            int m=list.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleComponent>>().Count;
            for(int j=0;j<m;j++)if(list[j]!=null && list[j].Vehicle?.Pointer==owner.Vehicle?.Pointer)yield return list[j];
        }
    }
    internal static int Target(VehicleComponent sight) => links.TryGetValue(sight.Pointer,out var link) && link.Owner.Pointer==sight.Pointer ? link.Target : -1;
    internal static VehicleComponent? Resolve(VehicleComponent sight)
    {
        int target=Target(sight);if(target<0)return null;
        var found=Components(sight).Where(c=>IsConsole(c) && c.IsInstalled && c.VUID.Value==target && ThermalEraAccess.Allowed(c)).Take(2).ToArray();
        return found.Length==1 ? found[0] : null;
    }
    internal static bool Profile(GunnerSight sight,out ThermalProfile profile)
    {
        profile=null!;
        // Console supplies a remote eyepiece, never the sensor's imaging capability.
        if(Target(sight)>=0 && Resolve(sight)==null)return false;
        return Runtime.FindProfile(sight,out profile);
    }
    internal static void Select(GunnerSight sight,int target)
    {
        if(target<0)links.Remove(sight.Pointer);else links[sight.Pointer]=new(sight,target);
        Runtime.Deactivate();
        foreach(var c in Components(sight))if(c.TryCast<Cannon>() is {} cannon && cannon.AssignedSightVUID.Value==sight.VUID.Value){Apply(cannon);cannon.RequestRebuild();}
    }
    internal static void Tick()
    {
        if(Time.unscaledTime<scanAt)return;scanAt=Time.unscaledTime+.25f;
        foreach(var vehicle in UnityEngine.Object.FindObjectsOfType<Vehicle>())
        {
            if(vehicle==null || vehicle.Released)continue;
            var items=vehicle.ObjectReader.Items;
            int n=items.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleObject>>().Count;
            for(int i=0;i<n;i++)
            {
                var obj=items[i];if(obj==null || obj.Released)continue;
                var list=obj.Components;if(list==null)continue;
                int m=list.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleComponent>>().Count;
                for(int j=0;j<m;j++)if(list[j]?.TryCast<Cannon>() is {} cannon)Apply(cannon);
            }
        }
    }
    private static void Apply(Cannon cannon)
    {
        if(cannon.Operable==null || cannon.VehicleObject.Released)return;
        var candidates=Components(cannon).Where(c=>c.VUID.Value==cannon.AssignedSightVUID.Value && c.TryCast<GunnerSight>()!=null).Take(2).ToArray();
        if(candidates.Length!=1)return;
        var sight=candidates[0].TryCast<GunnerSight>()!;
        var console=Resolve(sight);
        if(console==null && !routed.Contains(cannon.Pointer))return;
        // Exactly the native sight-to-cannon coordinate conversion; only the endpoint changes.
        // All native eyes/hands/feet distances and operator assignment remain intact.
        var world=console!=null ? console.VehicleTransform.LocalToWorldSpace(new Vector3(0,0,.06f)) : sight.VehicleTransform.LocalToWorldSpace(sight.EyepieceLocalPosition);
        var local=cannon.VehicleTransform.ToLocalSpace(world);
        if((cannon.Operable.OperateLocalPosition-local).sqrMagnitude>1e-8f)
        {cannon.Operable.operateLocalPosition=local;cannon.Operable.MarkModified();}
        if(console!=null)routed.Add(cannon.Pointer);else routed.Remove(cannon.Pointer);
        int endpoint=console?.VUID.Value ?? -1;
        if(logs<8 && (!reported.TryGetValue(cannon.Pointer,out int previous) || previous!=endpoint))
        {
            reported[cannon.Pointer]=endpoint;logs++;
            Plugin.Instance.Log.LogInfo($"[Sight console] cannon={cannon.VUID.Value}; sight={sight.VUID.Value}; console={endpoint}; native reach={cannon.Operable.MinimumEyesDistance}; endpoint={local}");
        }
    }
    [HarmonyPostfix,HarmonyPatch(typeof(Cannon),nameof(Cannon.UpdateAssociatedSight))]
    private static void Associated(Cannon __instance){try{Apply(__instance);}catch(Exception ex){Runtime.Warn("Console endpoint: "+ex.Message);}}
    [HarmonyPostfix,HarmonyPatch(typeof(Cannon),nameof(Cannon.UpdateSightTransform))]
    private static void Moved(Cannon __instance){try{Apply(__instance);}catch(Exception ex){Runtime.Warn("Console movement: "+ex.Message);}}
    [HarmonyPostfix,HarmonyPatch(typeof(VehicleComponent),nameof(VehicleComponent.SaveData))]
    private static void Save(VehicleComponent __instance,SerializationInfo __0)
    {if(__instance.TryCast<GunnerSight>()==null)return;try{if(Target(__instance)>=0)__0.AddValue(Key,Target(__instance));}catch(Exception ex){Runtime.Warn("Console link save: "+ex.Message);}}
    [HarmonyPostfix,HarmonyPatch(typeof(VehicleComponent),nameof(VehicleComponent.LoadData))]
    private static void Load(VehicleComponent __instance,SerializationInfo __0)
    {
        if(__instance.TryCast<GunnerSight>()==null)return;
        try{links.Remove(__instance.Pointer);var entries=__0.GetEnumerator();while(entries.MoveNext())if(entries.Name==Key){int id=__0.GetInt32(Key);if(id>=0)links[__instance.Pointer]=new(__instance,id);break;}}
        catch(Exception ex){Runtime.Warn("Console link load: "+ex.Message);}
    }
    [HarmonyPostfix,HarmonyPatch(typeof(VehicleComponent),nameof(VehicleComponent.ReleaseInternal))]
    private static void Release(VehicleComponent __instance){links.Remove(__instance.Pointer);routed.Remove(__instance.Pointer);reported.Remove(__instance.Pointer);}
    internal static void Clear()
    {
        var owners=links.Values.Select(l=>l.Owner).ToArray();links.Clear();
        foreach(var owner in owners)
            try{if(!owner.VehicleObject.Released)foreach(var c in Components(owner))if(c.TryCast<Cannon>() is {} cannon)Apply(cannon);}
            catch(Exception ex){Runtime.Warn("Console unload restore: "+ex.Message);}
        routed.Clear();reported.Clear();
    }
    [HarmonyPostfix,HarmonyPatch(typeof(VehicleObjectEditor),nameof(VehicleObjectEditor.OnGUI))]
    private static void Inspector(VehicleObjectEditor __instance,IGUILayout __0)
    {
        try
        {
            var obj=__instance.target;if(obj==null)return;
            var ui=__0.TryCast<IGUIElementDrawer>();if(ui==null)return;
            var sight=obj.GetBehaviour<GunnerSight>();
            var model=obj.GetBehaviour<VehicleObjectModel>();
            if(sight!=null)
            {
                var consoles=Components(sight).Where(c=>IsConsole(c) && ThermalEraAccess.Allowed(c)).ToArray();
                var names=new Il2CppSystem.Collections.Generic.List<string>();names.Add("Direct eyepiece (no console)");
                foreach(var c in consoles)names.Add("FCS #"+c.VUID.Value);
                int index=Array.FindIndex(consoles,c=>c.VUID.Value==Target(sight))+1;
                ui.Dropdown("FCS",names.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<string>>(),index,
                    DelegateSupport.ConvertDelegate<UnityAction<int>>((Action<int>)(v=>{if(v<0 || v>consoles.Length || sight.VehicleObject.Released)return;Select(sight,v==0 ? -1 : consoles[v-1].VUID.Value);__instance.RequestRedraw();}))!,
                    "Crew operates this sight at the linked console. No console-to-sight distance limit. Assign this external sight to the cannon as usual.");
                if(Target(sight)>=0 && Resolve(sight)==null)ui.InfoField("Linked console missing: thermal disabled. Select a valid console or direct eyepiece.",2);
            }
            else if(model!=null && IsConsole(model))
            {
                ui.Header("FCS #"+model.VUID.Value);
                ui.InfoField("Link one or more external sights below. Place this console within the gunner's normal operating reach.",2);
                ui.InfoField("Thermal profiles are selected on each thermal sight. A daylight sight stays daylight when connected.",2);
                foreach(var c in Components(model).Select(c=>c.TryCast<GunnerSight>()).Where(c=>c!=null).ToArray())
                {
                    var entry=c!;
                    ui.ToggleField(entry.ComponentID+" #"+entry.VUID.Value,Target(entry)==model.VUID.Value,
                        DelegateSupport.ConvertDelegate<Il2CppSystem.Action<bool>>((Action<bool>)(v=>{if(entry.VehicleObject.Released)return;Select(entry,v ? model.VUID.Value : -1);__instance.RequestRedraw();}))!,"A sight has one console; linking it here replaces its previous console.");
                }
            }
        }
        catch(Exception ex){Runtime.Warn("Console inspector: "+ex.Message);}
    }
}
