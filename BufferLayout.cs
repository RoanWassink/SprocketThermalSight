namespace SprocketThermalSight;

public readonly record struct BufferLayout(int SurfaceWidth, int SurfaceHeight, int ViewportWidth, int ViewportHeight)
{
    public float ScaleX => (float)ViewportWidth / SurfaceWidth;
    public float ScaleY => (float)ViewportHeight / SurfaceHeight;
    public static BufferLayout Resolve(int width, int height, int viewportWidth, int viewportHeight, bool scaled)
    {
        if (width <= 0 || height <= 0 || (scaled && (viewportWidth <= 0 || viewportHeight <= 0)))
            throw new ArgumentOutOfRangeException(nameof(width));
        return new(width, height, scaled ? Math.Min(width, viewportWidth) : width, scaled ? Math.Min(height, viewportHeight) : height);
    }
}
