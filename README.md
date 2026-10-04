# Sprocket Thermal Sight

Bring thermal vision to your gunner's sight! Install one of five standalone sight parts, enter its scope and press **N** to toggle a configurable thermal-style image.

**v0.1.5 is the first public release. The creator confirmed the mod works in-game.** It ships three sensor-quality models plus explicit Mk3 **White hot** and **Black hot** parts. The latest standard white-hot calibration reduces excessive terrain brightness; black-hot tuning is retained.

Built with AI assistance. This is a gameplay approximation: vehicles receive synthetic heat and terrain detail comes from the native camera image, not a physical temperature simulation.

## Requirements

- **Sprocket 0.2.55.5**, Windows x64.
- Working **BepInEx 6 IL2CPP** mod-loader setup with its .NET 6 runtime; verified loader **6.0.0-be.788**, the environment also used by Hans21223's *Sprocket Quality of Life*.
- Loader not included. Quality of Life, Shell Selector, Material Selector and other RoanWassink mods are optional.
- Other game builds are unverified. English part names are supplied.

## Install

1. Run the game once with the loader installed, then close it.
2. Download **SprocketThermalSight-v0.1.5.zip** from [Releases](https://github.com/RoanWassink/SprocketThermalSight/releases/latest) and extract it.
3. In Steam: **Sprocket → Manage → Browse local files**.
4. Copy the ZIP's **BepInEx** and **Sprocket_Data** folders into the folder containing **Sprocket.exe**. Merge folders, replacing matching mod files for a fresh installation.
5. Launch the game and choose a supplied Thermal sight in the gunner-sight parts list. Place and use it as a normal gunner sight.

The plugin and its `thermal-models.json` must be together:

```text
BepInEx/plugins/SprocketThermalSight/
  SprocketThermalSight.dll
  thermal-models.json
Sprocket_Data/StreamingAssets/Parts/
  thermalSightModel1Part.json
  thermalSightModel2Part.json
  thermalSightModel3Part.json
  thermalSightMk3WhiteHotPart.json
  thermalSightMk3BlackHotPart.json
Sprocket_Data/StreamingAssets/Localization/en-UK/Parts/
  [the five matching XML files]
```

A DLL alone is not a complete installation. Keep only one DLL copy. Back up your vehicle saves. Disable/remove a previous Teplovizor installation separately before using this mod to avoid competing thermal effects and N-key hooks; this installer does not remove other mods.

## Choose your sensor

| Sight | Image | Sensor refresh target | Noise / blur / levels | Added mass / assembly cost |
|---|---|---|---|---|
| Model 1 | 160×120 White hot | 10 Hz | 0.05 / 0.50 / 32 | 12 kg / 300 |
| Model 2 | 320×240 White hot | 20 Hz | 0.025 / 0.25 / 64 | 22 kg / 650 |
| Model 3 | 640×480 White hot | 30 Hz | 0.008 / 0.08 / 256 | 35 kg / 1200 |
| Mk3 - White hot | Model-3 quality | 30 Hz | Model-3 values | 35 kg / 1200 |
| Mk3 - Black hot | Model-3 quality, reversed tones | 30 Hz | Model-3 values | 35 kg / 1200 |

Model 1 offers a deliberately rougher, lighter/cheaper sensor; model 3 trades more processing work and added weight/cost for a cleaner image. These are gameplay presets, not measured historical devices. Added mass/cost are configured contributions above the native sight baseline; complete vehicle aggregation is not fully gameplay-validated.

## Use thermal

- Enter the **active thermal sight's scope** and press **N** to turn thermal on/off.
- Switching to another sight or leaving its scope turns the effect off; press N again in the thermal sight.
- **F8** reloads the profile catalog while using an active recognized thermal scope. Thermal turns off after a successful reload; press N again.
- Controls require an unpaused, focused game without an active text-input field.

Ordinary sights and third-person views do not gain thermal vision. Choose the black-hot part for darker vehicle silhouettes; the default other parts use white-hot. Terrain remains visible according to the profile's background settings.

## Configuration and custom sights

Two files have different purposes:

- `BepInEx/config/nl.roan.sprocket.thermalsight.cfg`: plugin enable flag and key bindings; created on first launch. Close the game to edit and restart.
- `BepInEx/plugins/SprocketThermalSight/thermal-models.json`: per-sight quality, tones, palettes and added mass/cost. Back up before editing; use F8 in a thermal scope then N, or restart. Rebuild/reload the vehicle after mass/cost changes rather than expecting F8 to refresh native caches.

```ini
[General]
Enabled = true

[Keys]
Toggle = N
ReloadProfiles = F8
```

Bindings are individual Unity Input System Key names, such as `N`, `T`, `F8` or `F9`. `None` disables a command. Avoid keys already used by the game or other mods. Keep Toggle and ReloadProfiles distinct.

See [CUSTOM-SIGHTS.md](CUSTOM-SIGHTS.md) for every supported field, ranges/defaults, practical tuning recipes and the **customization kit** that creates new part/profile identities for you. New parts require a game restart; F8 only reloads profiles.

## Updating, saves and rollback

Close the game and back up the plugin folder, profile catalog, `.cfg` and vehicle saves. Replace matching DLL and part/name files, retaining one DLL. **If you customized `thermal-models.json`, skip replacing it during manual copy-and-paste updates** and merge any new profiles/settings from the supplied catalog instead. The `.cfg` is not included in the ZIP and is preserved.

The optional customization kit's `Install.ps1` preserves existing profile values, adds missing supplied profiles and certain newly introduced fields, and backs up existing files. It also upgrades an older packaged background preset of 0.12/0.12. It does not remove retired colored parts or reset arbitrary customized values. Manual ZIP installation has no automatic merge or backups.

Supplied part GUIDs remain stable for existing test-build tanks. The standard set no longer supplies Green, Amber, Ironbow, Rainbow or Custom parts. If an earlier tank uses one, replace it with a retained sight before removing that old part, or restore its part/profile from your backup. Existing colored files are not automatically deleted by this package.

To roll back, close the game and restore the previous DLL, matching catalog/part files and configuration. To uninstall, replace thermal sights on saved tanks with stock sights and save first; then remove this mod's plugin folder, five part JSONs and five localization XMLs. Remove custom files only when you know their identity. Keep game folders and unrelated mods. Missing custom parts may prevent a tank from loading.

## FAQ

### Why does N do nothing?

Use the supplied sight, enter its scope and confirm it is the active sight on the controlled tank. Check Enabled, key bindings and the catalog path. A stock sight with a similar name is not thermal. Remove duplicate/competing thermal plugins and inspect the log.

### Can I see through hills or armour?

The vehicle mask uses the existing camera depth test; it is not designed to draw hidden vehicles through solid obstacles. This is not a physically modeled thermal sensor: foliage, smoke and other transparency depend on native rendering and are not guaranteed to behave like real infrared imaging.

### Why do vehicles look uniformly hot?

Vehicles receive one synthetic heat value. Separate engine, exhaust, barrel and cooling temperatures are not simulated. Increasing vehicleHeat changes the uniform value, not component heat detail. Friendly and enemy vehicles are not classified by heat.

### How do I improve FPS or make terrain less washed out?

Try model 1/2, or lower width/height and refreshHz in your profile. For bright terrain, reduce backgroundLevel/backgroundDetail gradually or lower brightness; excessive contrast can erase terrain differences. See the tuning examples. RefreshHz is a requested sensor update rate, not guaranteed game FPS.

### Can I create my own part or colored display?

Yes. The supplied default parts stay monochrome. The profile engine still supports named and custom palettes for personal configurations. The customization kit's helper creates a unique GUID, sight component ID, profile and English name; editing only displayName does not create a part or change its in-game localization.

## Limits and troubleshooting

Image capture uses asynchronous GPU readback, CPU processing and upload, adding latency and processing cost. High settings may reduce performance. Vehicle warmth and camera-derived terrain are gameplay approximations. Native scope rendering/depth are reused; world materials and lights are not changed. Long sessions, all native sight combinations and vehicle-level mass/cost aggregation have not been exhaustively tested.

- **Parts absent:** install all JSON/XML files and restart.
- **Thermal unavailable on startup:** check JSON syntax, unknown fields, duplicate identities and the log. The DLL does not generate a missing catalog.
- **F8 rejected:** your last accepted catalog remains active in memory. Fix the error and reload again; the broken file remains on disk, so restore it before restarting.
- **No effect after reload:** press N again in the recognized thermal scope.
- **Image orientation wrong:** use flipVertical on the affected profile, then reload.

Check `BepInEx/LogOutput.log` for `Sprocket Thermal Sight` and `[Thermal]`. [Report issues](https://github.com/RoanWassink/SprocketThermalSight/issues) with versions, sight name/component ID, reproduction steps, resolution/refresh settings and relevant log lines. Review personal paths before sharing logs.

## Build and credits

Install .NET SDK 8 and generate local BepInEx interop by starting the game once:

```powershell
dotnet build SprocketThermalSight.csproj -c Release
dotnet run --project tests/Sensor.Tests.csproj -c Release
```

Game/loader binaries are referenced locally and not distributed. Created by RoanWassink with AI assistance. [MIT license](LICENSE) covers this mod's code and original assets.

## Donations

Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods: [Donate via PayPal](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6).
