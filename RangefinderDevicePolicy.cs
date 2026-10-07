namespace SprocketThermalSight;

public enum RangefinderKind { None, Optical, ManualLaser, AutomaticLaser }
public static class RangefinderDevicePolicy
{
    public static RangefinderKind Kind(string? id) => id switch
    {
        "laserRangefinderSight" => RangefinderKind.ManualLaser,
        "automaticLaserRangefinder" => RangefinderKind.AutomaticLaser,
        "opticalCoincidenceRangefinder" => RangefinderKind.Optical,
        _ => RangefinderKind.None
    };
    public static double OpticalDelay(double range) => Math.Clamp(5 + .001 * range, 5, 8);
    public static double Mass(double baseline) => 8 + 7 * baseline;
    public static double Cost(double baseline) => 100 + 50 * baseline;
    public static double OpticalStep(double baseline)
    {
        if (!double.IsFinite(baseline) || baseline <= 0) throw new ArgumentOutOfRangeException(nameof(baseline));
        return baseline >= 2.999 ? 25 : baseline >= 1.999 ? 50 : 100;
    }
    public static double OpticalReading(double range, double baseline)
    {
        if (!double.IsFinite(range) || range < 50 || range > 3000) throw new ArgumentOutOfRangeException(nameof(range));
        var step = OpticalStep(baseline);
        return Math.Round(range / step, MidpointRounding.AwayFromZero) * step;
    }
}
