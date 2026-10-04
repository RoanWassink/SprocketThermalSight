using System.Text.Json;
using System.Text.Json.Serialization;

namespace SprocketThermalSight;

public sealed class ThermalCatalog
{
    public int Version { get; set; } = 1;
    public List<ThermalProfile> Models { get; set; } = new();
    public void Validate()
    {
        if (Version != 1 || Models.Count == 0) throw new InvalidDataException("Unsupported or empty thermal catalog.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var guids = new HashSet<Guid>();
        foreach (var model in Models)
        {
            model.Validate();
            if (!ids.Add(model.ComponentId) || !guids.Add(Guid.Parse(model.PartGuid)))
                throw new InvalidDataException("Duplicate thermal part GUID/component ID.");
        }
    }
    public static ThermalCatalog Read(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        RejectUnknown(doc.RootElement, typeof(ThermalCatalog));
        foreach (var model in doc.RootElement.GetProperty("models").EnumerateArray()) RejectUnknown(model, typeof(ThermalProfile));
        var result = JsonSerializer.Deserialize<ThermalCatalog>(doc.RootElement.GetRawText(), JsonOptions)!;
        result.Validate();
        return result;
    }
    private static void RejectUnknown(JsonElement element, Type type)
    {
        var names = type.GetProperties().Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).ToHashSet(StringComparer.Ordinal);
        foreach (var p in element.EnumerateObject())
            if (!names.Contains(p.Name)) throw new InvalidDataException($"Unknown setting '{p.Name}' in {type.Name}.");
    }
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
}

public sealed class ThermalProfile
{
    public string PartGuid { get; set; } = "";
    public string ComponentId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Width { get; set; } = 320;
    public int Height { get; set; } = 240;
    public float RefreshHz { get; set; } = 15;
    public float Contrast { get; set; } = 1.4f;
    public float Brightness { get; set; } = 0;
    public float Gamma { get; set; } = 1;
    public float Noise { get; set; } = .025f;
    public float Blur { get; set; } = .25f;
    public int GrayLevels { get; set; } = 64;
    public bool BlackHot { get; set; }
    public string Palette { get; set; } = "whiteHot";
    public string[] CustomColors { get; set; } = new[] { "#000000", "#FFFFFF" };
    public bool SmoothPixels { get; set; }
    public bool FlipVertical { get; set; }
    public float BackgroundLevel { get; set; } = .3f;
    public float BackgroundDetail { get; set; } = .3f;
    public float BackgroundGamma { get; set; } = .6f;
    public float VehicleHeat { get; set; } = .8f;
    public float ExtraMassKg { get; set; } = 15;
    public float ExtraAssemblyCost { get; set; } = 300;

    public void Validate()
    {
        if (!Guid.TryParse(PartGuid, out _) || string.IsNullOrWhiteSpace(ComponentId) || string.IsNullOrWhiteSpace(DisplayName))
            throw new InvalidDataException("Model requires partGuid, componentId and displayName.");
        if (Width < 80 || Width > 1024 || Height < 60 || Height > 768 || Width * Height > 524288)
            throw new InvalidDataException("Sensor resolution outside 80x60..1024x768; maximum 524288 pixels.");
        Range(RefreshHz, 1, 60, nameof(RefreshHz)); Range(Contrast, .1f, 5, nameof(Contrast));
        Range(Brightness, -1, 1, nameof(Brightness)); Range(Gamma, .2f, 4, nameof(Gamma));
        Range(Noise, 0, .5f, nameof(Noise)); Range(Blur, 0, 1, nameof(Blur));
        Range(BackgroundLevel, 0, 1, nameof(BackgroundLevel)); Range(BackgroundDetail, 0, 1, nameof(BackgroundDetail));
        Range(BackgroundGamma, .2f, 4, nameof(BackgroundGamma));
        Range(VehicleHeat, 0, 1, nameof(VehicleHeat)); Range(ExtraMassKg, 0, 500, nameof(ExtraMassKg));
        Range(ExtraAssemblyCost, 0, 100000, nameof(ExtraAssemblyCost));
        if (GrayLevels < 2 || GrayLevels > 256) throw new InvalidDataException("grayLevels must be 2..256.");
        if (Palette is not ("whiteHot" or "blackHot" or "greenHot" or "amberHot" or "ironbow" or "rainbow" or "custom"))
            throw new InvalidDataException("Unknown palette. Use whiteHot, blackHot, greenHot, amberHot, ironbow, rainbow or custom.");
        if (CustomColors == null || CustomColors.Length < 2 || CustomColors.Length > 16 || CustomColors.Any(c => c == null || !System.Text.RegularExpressions.Regex.IsMatch(c, "^#[0-9A-Fa-f]{6}$")))
            throw new InvalidDataException("customColors requires 2..16 #RRGGBB colors from cold to hot.");
    }
    private static void Range(float x, float min, float max, string name)
    { if (!float.IsFinite(x) || x < min || x > max) throw new InvalidDataException($"Invalid {name}; expected {min}..{max}."); }
}

public static class SensorProcessor
{
    // Each input is RGBA32. Heat is a depth-tested binary vehicle mask, not a physical temperature map.
    public static byte[] Process(byte[] scene, byte[] mask, ThermalProfile p, uint seed)
    {
        int count = checked(p.Width * p.Height);
        if (scene.Length != count * 4 || mask.Length != count * 4) throw new ArgumentException("Sensor buffer size mismatch.");
        var signal = new float[count];
        for (int i = 0; i < count; i++)
        {
            int k = i * 4;
            float luma = (.2126f * scene[k] + .7152f * scene[k + 1] + .0722f * scene[k + 2]) / 255;
            float coverage = Math.Clamp(Math.Max(mask[k], Math.Max(mask[k + 1], mask[k + 2])) / 255f, 0, 1);
            // Lift faint native landscape differences before contrast would crush them to black.
            // Vehicle heat remains independent of this landscape-only tonal adjustment.
            float background = Math.Clamp(p.BackgroundLevel + MathF.Pow(luma, p.BackgroundGamma) * p.BackgroundDetail, 0, 1);
            signal[i] = background + (p.VehicleHeat - background) * coverage;
        }
        var output = new byte[count * 4];
        var palette = BuildPalette(p);
        uint state = seed == 0 ? 1u : seed;
        for (int y = 0; y < p.Height; y++) for (int x = 0; x < p.Width; x++)
        {
            int i = y * p.Width + x;
            float sum = 0; int samples = 0;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            { int xx = Math.Clamp(x + dx, 0, p.Width - 1), yy = Math.Clamp(y + dy, 0, p.Height - 1); sum += signal[yy * p.Width + xx]; samples++; }
            float v = signal[i] + (sum / samples - signal[i]) * p.Blur;
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            v += ((state & 0xffff) / 65535f * 2 - 1) * p.Noise;
            v = Math.Clamp((v - .5f) * p.Contrast + .5f + p.Brightness, 0, 1);
            v = MathF.Pow(v, 1 / p.Gamma);
            v = MathF.Round(v * (p.GrayLevels - 1)) / (p.GrayLevels - 1);
            if (p.BlackHot || p.Palette == "blackHot") v = 1 - v;
            int target = ((p.FlipVertical ? p.Height - 1 - y : y) * p.Width + x) * 4;
            byte b = (byte)Math.Clamp((int)MathF.Round(v * 255), 0, 255);
            output[target] = palette[b * 3]; output[target + 1] = palette[b * 3 + 1]; output[target + 2] = palette[b * 3 + 2]; output[target + 3] = 255;
        }
        return output;
    }
    private static byte[] BuildPalette(ThermalProfile p)
    {
        string[] colors = p.Palette switch
        {
            "greenHot" => new[] { "#000000", "#00B840", "#DEFFE2" },
            "amberHot" => new[] { "#000000", "#BA6500", "#FFE3A0" },
            "ironbow" => new[] { "#000000", "#24054D", "#841B7B", "#D74637", "#FFAC25", "#FFFFD8" },
            "rainbow" => new[] { "#000020", "#2020A0", "#00A8DD", "#20D050", "#FFE020", "#F04020", "#FFFFFF" },
            "custom" => p.CustomColors,
            _ => new[] { "#000000", "#FFFFFF" }
        };
        var stops = colors.Select(c => new[] { Convert.ToInt32(c.Substring(1, 2), 16), Convert.ToInt32(c.Substring(3, 2), 16), Convert.ToInt32(c.Substring(5, 2), 16) }).ToArray();
        var lut = new byte[256 * 3];
        for (int i = 0; i < 256; i++)
        {
            float pos = i / 255f * (stops.Length - 1);
            int low = Math.Min((int)pos, stops.Length - 2); float weight = pos - low;
            for (int channel = 0; channel < 3; channel++) lut[i * 3 + channel] = (byte)Math.Clamp((int)MathF.Round(stops[low][channel] + (stops[low + 1][channel] - stops[low][channel]) * weight), 0, 255);
        }
        return lut;
    }
}
