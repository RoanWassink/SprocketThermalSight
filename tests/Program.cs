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
var vanilla = new[] { new DateTime(1900,1,1), new DateTime(1918,1,1), new DateTime(1940,1,1), new DateTime(1945,9,3) };
Check(!ThermalEraPolicy.Allows(new DateTime(1945,9,2), vanilla, new DateTime(1945,9,3)), "WWII final day excluded");
Check(ThermalEraPolicy.Allows(new DateTime(1945,9,3), vanilla, new DateTime(1945,9,3)), "first postwar day inclusive");
Check(ThermalEraPolicy.Allows(new DateTime(1960,1,1), vanilla, new DateTime(1945,9,3)), "finite vanilla postwar");
Check(ThermalEraPolicy.Allows(DateTime.MaxValue, vanilla, new DateTime(1945,9,3)), "vanilla final-era sentinel");
var custom = new[] { new DateTime(1900,1,1), new DateTime(1945,9,3), new DateTime(1992,1,1), new DateTime(2050,1,1) };
foreach (var date in new[] { new DateTime(1945,9,3), new DateTime(1992,1,1), new DateTime(2050,1,1), new DateTime(2400,1,1), new DateTime(9999,12,30) })
    Check(ThermalEraPolicy.Allows(date, custom, new DateTime(1945,9,3)), "custom postwar/modern/future has no arbitrary upper horizon");
Check(ThermalEraPolicy.Allows(DateTime.MaxValue, custom, new DateTime(1945,9,3)), "custom future final-era sentinel, no name lookup");
var earlyOnly = new[] { new DateTime(1900,1,1), new DateTime(1939,9,1) };
Check(ThermalEraPolicy.Allows(DateTime.MaxValue, earlyOnly, new DateTime(1945,9,3)), "native final era is open-ended regardless its start");
Check(!ThermalEraPolicy.Allows(new DateTime(1944,6,1), custom, new DateTime(1945,9,3)), "date excludes earlier custom era regardless label");
Check(!ThermalEraPolicy.Allows(null, custom, new DateTime(1945,9,3)), "missing owner date fails closed");
Check(!ThermalEraPolicy.Allows(new DateTime(1950,1,1), Array.Empty<DateTime>(), new DateTime(1945,9,3)), "missing era metadata fails closed");
Check(!ThermalEraPolicy.Allows(new DateTime(1950,1,1), new[] {new DateTime(1945,9,3),new DateTime(1900,1,1)}, new DateTime(1945,9,3)), "unordered eras fail closed");
Check(!ThermalEraPolicy.Allows(DateTime.MaxValue, new[] {DateTime.MaxValue.Date}, new DateTime(1945,9,3)), "sentinel is not valid era start");
Check(!ThermalEraPolicy.Allows(new DateTime(1945,9,3), new[] {new DateTime(2000,1,1)}, new DateTime(1945,9,3)), "date before first registered era fails closed");
Console.WriteLine($"PASS: {checks} checks including postwar windows; native imports/save-load and custom-era visuals need live validation.");
Check(RangefinderPolicy.Nearest(new double[]{300,100,200},500)==100,"unsorted hits choose nearest surface");
Check(RangefinderPolicy.Nearest(new double[]{200,300,100},500)==100,"hit order does not alter distance");
Check(RangefinderPolicy.Nearest(new double[]{double.NaN,double.PositiveInfinity,-1,0,600},500)==null,"invalid and out-of-range returns rejected");
Check(RangefinderPolicy.Nearest(new double[]{500},500)==500,"maximum range inclusive");
Check(RangefinderPolicy.Nearest(Array.Empty<double>(),500)==null,"empty ray has no return");
Check(RangefinderPolicy.Nearest(new double[]{100},double.NaN)==null,"invalid configured limit rejected");
Check(RangefinderPolicy.CanMeasure(true,false,true,true,true,0),"active sight with fitted device can measure");
Check(RangefinderPolicy.CanMeasure(true,false,true,true,true,1),"one-frame callback ordering accepted");
Check(!RangefinderPolicy.CanMeasure(true,false,true,true,true,2),"stale controller rejected");
Check(!RangefinderPolicy.CanMeasure(true,false,true,true,true,-1),"future frame rejected");
Check(!RangefinderPolicy.CanMeasure(false,false,true,true,true,0),"unfocused cannot measure");
Check(!RangefinderPolicy.CanMeasure(true,true,true,true,true,0),"paused cannot measure");
Check(!RangefinderPolicy.CanMeasure(true,false,false,true,true,0),"disabled cannot measure");
Check(!RangefinderPolicy.CanMeasure(true,false,true,false,true,0),"unscoped cannot measure");
Check(!RangefinderPolicy.CanMeasure(true,false,true,true,false,0),"missing fitted device cannot measure");
Console.WriteLine($"PASS: {checks} checks including ranging policy; native ray/HUD/save-load require live testing.");

Check(RangefinderPolicy.Readout(RangefinderPolicy.Nearest(new double[]{20,200},4000),50)=="Too close","near obstruction cannot be filtered to range behind it");
Check(RangefinderPolicy.Readout(50,50)=="50 m","minimum inclusive");
Check(RangefinderPolicy.Readout(54,50)=="50 m","ten metre readout rounded down");
Check(RangefinderPolicy.Readout(55,50)=="60 m","ten metre midpoint rounded away from zero");
Check(RangefinderPolicy.Readout(null,50)=="No return","no synthetic maximum return");
Console.WriteLine($"PASS: {checks} checks including device readout and near obstruction.");
Check(RangefinderDevicePolicy.Kind("laserRangefinderSight")==RangefinderKind.ManualLaser,"legacy marker remains manual");
Check(RangefinderDevicePolicy.Kind("automaticLaserRangefinder")>RangefinderDevicePolicy.Kind("laserRangefinderSight"),"automatic priority");
Check(RangefinderDevicePolicy.Kind("sight")==RangefinderKind.None,"unrelated sight does not grant capability");
Check(RangefinderDevicePolicy.OpticalDelay(50)==5.05,"near target delay");
Check(RangefinderDevicePolicy.OpticalDelay(3000)==8,"far target delay limit");
Check(RangefinderDevicePolicy.Mass(2)==22 && RangefinderDevicePolicy.Cost(2)==200,"default optical resource calibration");
Check(RangefinderDevicePolicy.Cost(3)<400,"largest allowed baseline still cheaper than manual laser");
Reject(()=>RangefinderDevicePolicy.OpticalReading(1000,0),"zero baseline rejected");
Check(ThermalEraPolicy.Allows(new DateTime(1944,1,1), vanilla,new DateTime(1943,10,19)),"optical allowed in late WWII");
Check(!ThermalEraPolicy.Allows(new DateTime(1944,1,1),vanilla,new DateTime(1945,9,3)),"thermal postwar floor preserved");
Check(ThermalEraPolicy.Allows(DateTime.MaxValue,new[]{new DateTime(1900,1,1),new DateTime(1943,10,19)},new DateTime(1943,10,19)),"last-era sentinel also supports optical WWII floor");
Console.WriteLine($"PASS: {checks} including extended ranging math and independent optical era gate.");

Check(RangefinderDevicePolicy.OpticalReading(150,1)==200,"one metre user's midpoint example");
Check(RangefinderDevicePolicy.OpticalReading(149,1)==100,"one metre rounds down below midpoint");
Check(RangefinderDevicePolicy.OpticalReading(150,2)==150,"two metre resolves fifty metre steps");
Check(RangefinderDevicePolicy.OpticalReading(175,2)==200,"fifty metre midpoint rounds up");
Check(RangefinderDevicePolicy.OpticalReading(163,3)==175,"three metre resolves twenty five metre steps");
Check(RangefinderDevicePolicy.OpticalReading(50,1)==100,"near reading follows coarse step");
Check(RangefinderDevicePolicy.OpticalReading(3000,1)==3000,"upper limit maintained");
Check(RangefinderDevicePolicy.Mass(1)==15 && RangefinderDevicePolicy.Cost(1)==150,"one metre default mass and cost");
Reject(()=>RangefinderDevicePolicy.OpticalReading(double.NaN,1),"invalid distance rejected");
Reject(()=>RangefinderDevicePolicy.OpticalReading(40,1),"near obstruction never quantized as valid range");
Reject(()=>RangefinderDevicePolicy.OpticalReading(3001,1),"outside optical envelope rejected");
for(int range=50;range<=3000;range++) foreach(var baseline in new[]{1d,2d,3d}){
 var reading=RangefinderDevicePolicy.OpticalReading(range,baseline);
 var step=RangefinderDevicePolicy.OpticalStep(baseline);
 Check(Math.Abs(reading-range)<=step/2+.0001,"bounded quantization error");
 Check(reading%step==0,"readout is a real step multiple");
}
Console.WriteLine($"PASS: {checks} including optical quantization boundaries and user's 150-to-200 example.");

// Native Part dates are mutable data, not a permanent postwar gate.
foreach(var invention in new[]{new DateTime(1910,6,1),new DateTime(1943,10,19),new DateTime(1965,7,1),new DateTime(2100,1,1)})
{
 Check(!ThermalEraPolicy.Allows(invention.AddDays(-1),vanilla,invention),"before actual Part date");
 Check(ThermalEraPolicy.Allows(invention,vanilla,invention),"on actual Part date");
 Check(ThermalEraPolicy.Allows(invention.AddDays(1),vanilla,invention),"after actual Part date");
 Check(ThermalEraPolicy.Allows(DateTime.MaxValue,vanilla,invention),"final-era allows invention later than final start");
}
Console.WriteLine($"PASS: {checks} including native mutable Part dates and open final era.");

// Profile key migration uses presence precedence, preserving profile ID strings.
Check(SavedProfileKeys.Resolve(Array.Empty<string>()) == null, "no profile key");
Check(SavedProfileKeys.Resolve(new[]{SavedProfileKeys.Legacy}) == SavedProfileKeys.Legacy, "legacy profile read");
Check(SavedProfileKeys.Resolve(new[]{SavedProfileKeys.Canonical}) == SavedProfileKeys.Canonical, "canonical profile read");
Check(SavedProfileKeys.Resolve(new[]{SavedProfileKeys.Legacy,SavedProfileKeys.Canonical}) == SavedProfileKeys.Canonical, "canonical wins after legacy");
Check(SavedProfileKeys.Resolve(new[]{SavedProfileKeys.Canonical,SavedProfileKeys.Legacy}) == SavedProfileKeys.Canonical, "canonical wins before legacy");
foreach(var profileId in new[]{"thermalSightModel3","custom-user-profile"})
{
 var oldSave = new Dictionary<string,string>{{SavedProfileKeys.Legacy,profileId}};
 var selected = oldSave[SavedProfileKeys.Resolve(oldSave.Keys)!];
 var nextSave = new Dictionary<string,string>{{SavedProfileKeys.Canonical,selected}};
 Check(nextSave.Count==1 && !nextSave.ContainsKey(SavedProfileKeys.Legacy), "write canonical key only");
 Check(nextSave[SavedProfileKeys.Resolve(nextSave.Keys)!]==profileId, "profile ID survives old load new save reload");
 Check(oldSave[SavedProfileKeys.Legacy]==profileId, "old vehicle representation not mutated by load");
}
Console.WriteLine($"PASS: {checks} including legacy profile read/canonical write roundtrips.");
