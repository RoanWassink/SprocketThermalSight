using SprocketThermalSight;
using System.Text.Json;

int checks = 0;
void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
void Reject(Action action, string name) { bool rejected = false; try { action(); } catch { rejected = true; } Check(rejected, name); }
var catalog = ThermalCatalog.Read(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../thermal-models.json")));
Check(catalog.Models.Count == 5, "three original models and two monochrome palette parts");
Check(catalog.Models.Skip(3).Select(p => p.Palette).Distinct().Count() == 2, "white-hot and black-hot palette parts represented");
Check(catalog.Models[0].ComponentId != catalog.Models[1].ComponentId, "per-part identity");
var p = new ThermalProfile { PartGuid = Guid.NewGuid().ToString(), ComponentId = "test", DisplayName = "test", Width = 80, Height = 60, Noise = 0, Blur = 0, Contrast = 1, Gamma = 1, GrayLevels = 256 };
p.Validate();
int count = p.Width * p.Height;
var scene = new byte[count * 4]; var mask = new byte[count * 4];
for (int y = 0; y < p.Height; y++) for (int x = 0; x < p.Width; x++)
{ int k = (y * p.Width + x) * 4; scene[k + 3] = 255; if (x >= 40) mask[k] = mask[k + 1] = mask[k + 2] = 255; }
var white = SensorProcessor.Process(scene, mask, p, 1);
Check(white[4 * 50] > white[0], "vehicle brighter than background");
for (int i = 0; i < count; i++) Check(white[i * 4 + 3] == 255, "opaque image");
p.BlackHot = true; var black = SensorProcessor.Process(scene, mask, p, 1);
for (int i = 0; i < count; i++) Check(Math.Abs(white[i * 4] + black[i * 4] - 255) <= 1, "polarity inversion");
p.BlackHot = false; p.GrayLevels = 2; var binary = SensorProcessor.Process(scene, mask, p, 1);
Check(binary.Where((_, i) => i % 4 == 0).All(v => v == 0 || v == 255), "two gray levels");
p.GrayLevels = 256; p.Noise = .1f;
Check(SensorProcessor.Process(scene, mask, p, 7).SequenceEqual(SensorProcessor.Process(scene, mask, p, 7)), "repeatable noise seed");
Check(!SensorProcessor.Process(scene, mask, p, 7).SequenceEqual(SensorProcessor.Process(scene, mask, p, 8)), "changing sensor noise");
p.Noise = 0; p.Blur = 1; var blurred = SensorProcessor.Process(scene, mask, p, 1);
Check(blurred[39 * 4] > white[39 * 4] && blurred[40 * 4] < white[40 * 4], "edge softness");
p.Blur = 0; p.Brightness = 1; Check(SensorProcessor.Process(scene, mask, p, 1)[0] == 255, "brightness clamp");
p.Brightness = -1; Check(SensorProcessor.Process(scene, mask, p, 1)[50 * 4] == 0, "black clamp");
p.Brightness = 0; p.Gamma = float.NaN; Reject(p.Validate, "reject NaN");
p.Gamma = 1; p.Width = 2000; Reject(p.Validate, "reject huge allocation");
p.Width = 80; Reject(() => SensorProcessor.Process(new byte[4], mask, p, 1), "reject buffer mismatch");
var duplicate = new ThermalCatalog { Models = new() { p, p } }; Reject(duplicate.Validate, "reject duplicate ID");
p.Brightness = 1; p.Palette = "custom"; p.CustomColors = new[] { "#112233", "#AABBCC" };
var colored = SensorProcessor.Process(scene, mask, p, 1);
Check(colored[0] == 0xAA && colored[1] == 0xBB && colored[2] == 0xCC, "custom hot endpoint");
p.Brightness = -1; colored = SensorProcessor.Process(scene, mask, p, 1);
Check(colored[0] == 0x11 && colored[1] == 0x22 && colored[2] == 0x33, "custom cold endpoint");
p.Palette = "greenHot"; p.Brightness = 1; colored = SensorProcessor.Process(scene, mask, p, 1);
Check(colored[1] > colored[0] && colored[1] > colored[2], "green palette");
p.Palette = "blackHot"; Check(SensorProcessor.Process(scene, mask, p, 1)[0] == 0, "named blackhot inversion");
p.Palette = "invalid"; Reject(p.Validate, "unknown palette");
p.Palette = "custom"; p.CustomColors = new[] { "#XYZXYZ", "#FFFFFF" }; Reject(p.Validate, "invalid color");
p.Palette = "whiteHot"; p.CustomColors = new[] { "#000000", "#FFFFFF" }; p.Brightness = 0;
p.Contrast = 1.7f; p.BackgroundLevel = .3f; p.BackgroundDetail = .3f; p.BackgroundGamma = .6f;
var darkScene = new byte[count * 4]; var coldMask = new byte[count * 4];
for (int i = 0; i < count; i++) { int k = i * 4; byte value = (byte)(i % p.Width < 40 ? 5 : 35); darkScene[k] = darkScene[k + 1] = darkScene[k + 2] = value; }
var terrain = SensorProcessor.Process(darkScene, coldMask, p, 1);
Check(terrain[0] > 30, "dark terrain remains visible at Mk3 contrast");
Check(terrain[50 * 4] - terrain[0] > 15, "faint terrain differences survive contrast");
var terrainHot = SensorProcessor.Process(darkScene, mask, p, 1);
Check(terrainHot[50 * 4] > terrain[50 * 4] + 50, "vehicle remains distinct from lifted terrain");
Check(catalog.Resolve(null).ComponentId == catalog.DefaultProfileId, "JSON default");
Check(catalog.Resolve("thermalSightMk3BlackHot").Palette == "blackHot", "saved selection");
Check(catalog.Resolve("missing").ComponentId == catalog.DefaultProfileId, "missing profile fallback");
Console.WriteLine($"PASS: {checks} sensor/profile checks; native rendering, part loading and mass/cost require gameplay validation.");


// Before post processing, software DRS occupies only part of an oversized buffer.
var low = BufferLayout.Resolve(1920, 1080, 960, 540, true);
Check(low.ScaleX == .5f && low.ScaleY == .5f, "low resolution capture reads valid region");
// After upscaling, scope source is full resolution even while camera.actualWidth is 960.
var final = BufferLayout.Resolve(1920, 1080, 960, 540, false);
Check(final.ViewportWidth == 1920 && final.ViewportHeight == 1080, "fixed post-upscale scope fills entire source");
var upscaled = BufferLayout.Resolve(1920, 1080, 1920, 1080, true);
Check(upscaled.ScaleX == 1 && upscaled.ScaleY == 1, "custom upscaler handle uses its own full viewport");
var hardware = BufferLayout.Resolve(960, 540, 960, 540, true);
Check(hardware.ScaleX == 1 && hardware.ScaleY == 1, "hardware-scaled physical surface samples full UVs");
foreach (int vw in new[] { 1920, 1344, 960, 1152, 1920 })
{
    var frame = BufferLayout.Resolve(2560, 1440, vw, vw * 9 / 16, true);
    Check(frame.ViewportWidth == vw && Math.Abs(frame.ScaleX * 2560 - vw) < .001f, "viewport follows changing render scale without allocation resize");
}
var odd = BufferLayout.Resolve(1919, 1079, 959, 539, true);
Check(Math.Abs(odd.ScaleX * 1919 - 959) < .001f && Math.Abs(odd.ScaleY * 1079 - 539) < .001f, "odd/asymmetric dimensions preserve sample footprint");
Reject(() => BufferLayout.Resolve(0, 1080, 960, 540, true), "reject uninitialized render surface");
Console.WriteLine($"PASS: {checks} checks including DRS layouts; native DRS visuals remain unvalidated.");
Check(ThermalEraPolicy.Allows("Coldwar"), "native Coldwar era available");
foreach (var earlier in new string?[] { null, "", "WWI", "Interwar", "Earlywar", "Midwar", "Latewar", "unknown" })
    Check(!ThermalEraPolicy.Allows(earlier), "earlier/unknown era fails closed");
Console.WriteLine($"PASS: {checks} checks; native era/date transitions and UI need live validation.");
