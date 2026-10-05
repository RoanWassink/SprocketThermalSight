namespace SprocketThermalSight;

public static class ThermalEraPolicy
{
    public static readonly DateTime MinimumDate = new(1945, 9, 3);
    // Native finite intervals are [start, next start). MaxValue means final-era
    // selection, not a literal future date. Names/playability are not date evidence.
    public static bool Allows(DateTime? vehicleDate, ReadOnlySpan<DateTime> eraStarts)
    {
        if (vehicleDate == null || eraStarts.Length == 0 || eraStarts[^1].Date == DateTime.MaxValue.Date) return false;
        for (int i = 1; i < eraStarts.Length; i++)
            if (eraStarts[i] <= eraStarts[i - 1]) return false;
        if (vehicleDate.Value.Date == DateTime.MaxValue.Date)
            return eraStarts[^1].Date >= MinimumDate;
        if (vehicleDate.Value.Date < MinimumDate || vehicleDate.Value.Date < eraStarts[0].Date) return false;
        // No invented technology horizon. Any valid finite date in a known
        // native timeline from the policy floor onwards is eligible.
        return true;
    }
}

