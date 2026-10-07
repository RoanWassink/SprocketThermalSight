namespace SprocketThermalSight;

public static class ThermalEraPolicy
{
    // The caller supplies the current native part invention date. No mod-owned epoch.
    public static bool Allows(DateTime? vehicleDate, ReadOnlySpan<DateTime> eraStarts, DateTime floor)
    {
        if (vehicleDate == null || eraStarts.Length == 0 || eraStarts[^1].Date == DateTime.MaxValue.Date) return false;
        for (int i = 1; i < eraStarts.Length; i++)
            if (eraStarts[i] <= eraStarts[i - 1]) return false;
        if (vehicleDate.Value.Date == DateTime.MaxValue.Date)
            return true; // Native final-era selection is open-ended, including later dated parts.
        if (vehicleDate.Value.Date < floor || vehicleDate.Value.Date < eraStarts[0].Date) return false;
        // No invented technology horizon. Any valid finite date in a known
        // native timeline from the policy floor onwards is eligible.
        return true;
    }
}

