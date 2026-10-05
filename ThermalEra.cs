using Sprocket.Vehicles;

namespace SprocketThermalSight;

internal static class ThermalEraAccess
{
    internal static bool Allowed(VehicleComponent? sight)
    {
        try
        {
            var design = sight?.Vehicle?.DesignInfo;
            var era = design == null ? null : VehicleClassifications.GetEra(design.Date);
            return era != null && ThermalEraPolicy.Allows(era.Name);
        }
        catch (Exception ex) { Runtime.Warn("Thermal era unavailable; normal sight retained: " + ex.Message); return false; }
    }
}

