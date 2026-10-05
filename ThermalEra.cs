using Sprocket.TechTrees;
using Sprocket.Vehicles;

namespace SprocketThermalSight;
internal static class ThermalEraAccess
{
    private static DateTime Date(TechDate date) => new(date.Year, date.Month, date.Day);
    internal static bool Allowed(VehicleComponent? sight)
    {
        try
        {
            var design = sight?.Vehicle?.DesignInfo;
            var eras = VehicleClassifications.eras;
            if (design == null || eras == null || eras.Length == 0) return false;
            var starts = new DateTime[eras.Length];
            for (int i = 0; i < eras.Length; i++)
            {
                if (eras[i] == null) return false;
                starts[i] = Date(eras[i].StartDate);
            }
            return ThermalEraPolicy.Allows(Date(design.Date), starts);
        }
        catch (Exception ex) { Runtime.Warn("Thermal date unavailable; normal sight retained: " + ex.Message); return false; }
    }
}
