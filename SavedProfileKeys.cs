namespace SprocketThermalSight;

internal static class SavedProfileKeys
{
    internal const string Canonical = "sprocketThermalProfile";
    internal const string Legacy = "roanThermalProfile";
    internal static string? Resolve(IEnumerable<string> names)
    {
        bool legacy = false;
        foreach (var name in names)
        {
            if (name == Canonical) return Canonical;
            if (name == Legacy) legacy = true;
        }
        return legacy ? Legacy : null;
    }
}
