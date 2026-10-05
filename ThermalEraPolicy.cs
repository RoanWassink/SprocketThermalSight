namespace SprocketThermalSight;
public static class ThermalEraPolicy
{
    public static bool Allows(string? nativeEraName) =>
        string.Equals(nativeEraName, "Coldwar", StringComparison.OrdinalIgnoreCase);
}
