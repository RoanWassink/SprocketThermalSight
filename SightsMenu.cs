using DesignerFramework.AssetManagement;
using DesignerFramework.UI;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Sprocket.Selection;

namespace SprocketThermalSight;

// Split only our tagged native options. Their placement delegates, dates and state stay native.
internal static class SightsMenu
{
    private static bool warned;
    private static int logs;
    [HarmonyPrefix, HarmonyPatch(typeof(SelectionPanel), nameof(SelectionPanel.SetDisplayOptions))]
    private static void Groups(ref Il2CppReferenceArray<ISelectionGroup> __0, ref int __1)
    {
        if (__0 == null) return;
        try
        {
            var groups = new List<ISelectionGroup>();
            var electronics = new List<Option>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int newActive = -1, oldIndex = 0;
            foreach (var item in __0)
            {
                bool wasActive = oldIndex++ == __1;
                var group = item?.TryCast<OptionGroup>();
                if (group?.items == null || group.Name == "Sights and electronics") { if (wasActive) newActive = groups.Count; groups.Add(item!); continue; }
                var retained = new List<Option>();
                bool changed = false;
                foreach (var option in group.items)
                {
                    if (option?.Tags?.Any(tag => tag == "sightsElectronics") == true)
                    { if (seen.Add(option.Name)) electronics.Add(option); changed = true; }
                    else retained.Add(option!);
                }
                if (!changed) { if (wasActive) newActive = groups.Count; groups.Add(item!); continue; }
                if (retained.Count > 0)
                {
                    var copy = new OptionGroup(group.Name, retained.ToArray()) { Flags = group.Flags, InteractableTest = group.InteractableTest };
                    if (wasActive) newActive = groups.Count;
                    groups.Add(copy.Cast<ISelectionGroup>());
                }
            }
            if (electronics.Count == 0) return;
            var own = new OptionGroup("Sights and electronics", electronics.ToArray());
            groups.Add(own.Cast<ISelectionGroup>());
            __0 = new Il2CppReferenceArray<ISelectionGroup>(groups.ToArray());
            __1 = newActive < 0 ? groups.Count - 1 : newActive;
            if (logs++ < 3) Plugin.Instance.Log.LogInfo($"[Sights] Grouped {electronics.Count} options in Sights and electronics.");
        }
        catch (Exception ex)
        {
            if (!warned) { warned = true; Plugin.Instance.Log.LogWarning("[Sights] Custom group unavailable; native Firepower selection retained: " + ex.Message); }
        }
    }
}
