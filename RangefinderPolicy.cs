namespace SprocketThermalSight;

public static class RangefinderPolicy
{
    public static string Readout(double? nearest, double minimum)
    {
        if (nearest == null) return "No return";
        if (nearest.Value < minimum) return "Too close";
        return $"{Math.Round(nearest.Value / 10, MidpointRounding.AwayFromZero) * 10:0} m";
    }
    public static double? Nearest(IEnumerable<double> distances, double maximum)
    {
        if (!double.IsFinite(maximum) || maximum <= 0) return null;
        double? nearest = null;
        foreach (var value in distances)
            if (double.IsFinite(value) && value > 0 && value <= maximum && (nearest == null || value < nearest)) nearest = value;
        return nearest;
    }
    public static bool CanMeasure(bool focused, bool paused, bool enabled, bool scoped, bool fittedDevice, int frameAge)
        => focused && !paused && enabled && scoped && fittedDevice && frameAge >= 0 && frameAge <= 1;
}
