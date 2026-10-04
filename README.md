# Sprocket Thermal Sight

One sight, your choice of sensor! Place **Thermal sight**, right-click it and choose **Thermal sight → Profile**. Profiles come from JSON and the selected ID is saved separately on each sight, so two sights on the same tank can have different settings.

**v0.2.1** replaces the separate default palette/quality parts with one configurable sight and fixes the startup crash in the unpublished v0.2.0 test build. The creator confirmed the profile menu and saving/loading work in-game.

Built with AI assistance. Thermal imagery is a gameplay approximation: vehicles receive synthetic heat; terrain comes from the native camera image rather than a temperature map.

## Requirements

- Sprocket **0.2.55.5**, Windows x64.
- Working **BepInEx 6 IL2CPP** setup with its .NET 6 runtime; verified loader **6.0.0-be.788**. Loader not included.
- Other mods are optional. Other game builds are unverified. English part names are supplied.

## Install or update

1. Run the game once with the loader installed, then close it.
2. Download **SprocketThermalSight-v0.2.1.zip** from [Releases](https://github.com/RoanWassink/SprocketThermalSight/releases/latest) and extract it.
3. In Steam: **Sprocket → Manage → Browse local files**.
4. Copy **BepInEx** and **Sprocket_Data** into the folder containing **Sprocket.exe**, merging folders and replacing matching mod files.
5. Launch and place **Thermal sight** from the gunner-sight parts list.

**Customized catalog?** Back up and retain your installed `BepInEx/plugins/SprocketThermalSight/thermal-models.json` when manually updating. Copying the release catalog over it replaces your custom settings. The plugin CFG is not bundled and is preserved. Keep only one DLL copy; back up vehicle saves. Disable an older Teplovizor separately to avoid competing effects/N hooks.

The plugin folder contains the DLL, catalog and `assets/thermal-sight-icon.png`. The package installs six JSON part definitions and six matching English XML names: the new `thermalSight` pair and five legacy pairs. **Keep those legacy files** to load older tanks; they are hidden from the parts selector, not deleted from the game.

A DLL-only update from v0.1.5 is insufficient: install the new part/name files and icon too. No PowerShell or compiling is required for normal installation.

## Choose a profile

1. Place and configure the sight as a normal gunner sight.
2. Right-click it in the designer, open **Thermal sight**, then choose **Profile**.
3. The panel shows resolution, refresh target, palette and configured extra mass/cost.
4. Save the tank. In Play, enter that sight's scope and press **N** to toggle thermal.

| Profile | Resolution / refresh target | Added mass / assembly cost |
|---|---|---|
| Model 1, White hot | 160×120 / 10 Hz | 12 kg / 300 |
| Model 2, White hot | 320×240 / 20 Hz | 22 kg / 650 |
| Model 3, White hot | 640×480 / 30 Hz | 35 kg / 1200 |
| Mk3 - White hot | Model-3 quality | 35 kg / 1200 |
| Mk3 - Black hot | Model-3 quality, inverted tones | 35 kg / 1200 |

These are gameplay presets, not measured historical sensors. Higher resolution/rate adds processing work. Mass/cost are additions to the native sight baseline. Selection requests a rebuild and refreshes the mass cache; all native aggregation paths have not been exhaustively validated.

## Controls and configuration

- **N:** toggle thermal in the active thermal scope. Stock sights and third-person view do not gain thermal.
- **F8:** reload JSON profiles while in a recognized thermal scope, then press N again.
- Controls require an unpaused, focused game without an active text-input field.

Edit `BepInEx/config/nl.roan.sprocket.thermalsight.cfg` with the game closed and restart for keys:

```ini
[General]
Enabled = true

[Keys]
Toggle = N
ReloadProfiles = F8
```

Bindings are individual Unity Input System Key names such as N, T, F8 or F9. None disables a command. Avoid conflicting keys and keep the two bindings distinct.

Image profiles live in `BepInEx/plugins/SprocketThermalSight/thermal-models.json` next to the DLL. Back up before editing. `componentId` is now the stable **profile ID** and `displayName` is its dropdown label. New profiles need no partGuid or extra JSON/XML part assets. Keep existing IDs unchanged after saving vehicles.

Root `defaultProfileId` selects the initial profile for new sights; the release uses `thermalSightModel3`. It must name an existing profile. Old catalogs without this field use the same default; if you removed model 3, explicitly choose a remaining ID before starting.

See [CUSTOM-SIGHTS.md](CUSTOM-SIGHTS.md) for the full field/range table, copyable profiles, performance/terrain recipes and optional helper kit. Additional profiles can be loaded with F8; reopen the designer menu to select them. Restart if a menu does not refresh. After mass/cost edits rebuild/reload the tank.

## Saved tanks, fallback and updates

Legacy sights retain their original profile until changed in the menu. Their GUIDs remain installed. There is no need to replace old sights simply to upgrade.

If a saved profile ID is missing, the sight temporarily uses defaultProfileId and logs a warning while retaining the missing saved ID. Restore that profile under the same ID to recover its selection, or explicitly choose another profile. Profiles are not embedded in vehicle saves: share the matching catalog entries when sharing a custom-profile tank.

The manual ZIP does not merge or back up catalogs automatically. The optional customization kit installer preserves existing profile values, adds missing defaults/fields, and backs up replaced files. Existing profiles remain authoritative; editing a kit profile and reinstalling does not overwrite its installed values. The installer retains its prior terrain-preset upgrade behavior and does not remove retired colored parts.

To roll back, close Sprocket and restore the previous DLL with matching part files, catalog and CFG backups. v0.1.5 does not provide the new configurable part/menu; replace new sights with older/stock parts before rolling back if those vehicles must load. Do not roll back to the crashing v0.2.0 test build.

Before uninstalling, replace thermal sights with stock sights in affected tanks and save. Remove this mod's plugin folder and the six matching part/name pairs, keeping unrelated files. Missing custom parts can prevent tanks loading. Keep backups of profiles and CFG.

## FAQ and troubleshooting

**Why does N do nothing?** Enter the active thermal sight's scope, check Enabled/key bindings, and verify the catalog loads. Switching sights or leaving scope turns thermal off; press N again in the thermal scope.

**Can two sights use different settings?** Yes: each placed sight saves its selected profile ID. Editing that JSON profile affects every sight referencing it; copy the profile to get independently tuned presets.

**Can I add another palette or sensor?** Copy a profile, assign a unique componentId/displayName, then reload and select it in the menu. No new physical part is needed. The standard set stays monochrome; optional named/custom palettes remain supported.

**Why is a vehicle uniformly hot?** Vehicles receive one synthetic value; no separate engine, exhaust, barrel or cooling temperatures are simulated. Friendly/enemy classification is not thermal-coded. Native depth testing is used, not intended x-ray vision; smoke/foliage behavior is not a physical infrared model.

**Poor FPS or washed-out terrain?** Lower resolution/refreshHz; reduce backgroundLevel/backgroundDetail or excessive contrast. RefreshHz is a requested sensor rate, not game FPS. Capture/readback/CPU processing adds latency and load.

**Startup/menu issue?** Confirm the log reports 0.2.1 and there is only one plugin DLL. Install all part assets. Invalid JSON, unknown keys, duplicate IDs or a missing defaultProfileId target reject the catalog. F8 failures keep the previous catalog in memory, but leave the broken file on disk: repair or restore it before restart.

**Icon missing?** Keep assets/thermal-sight-icon.png next to the DLL. Optional icon failure should not disable thermal operation.

Check `BepInEx/LogOutput.log` for `Sprocket Thermal Sight` and `[Thermal]`. [Report issues](https://github.com/RoanWassink/SprocketThermalSight/issues) with versions, selected profile ID, reproduction steps and relevant logs. Review personal paths before sharing.

## Validation, building and credits

The creator confirmed the right-click menu/profile selection and persistence work in-game. Release build and sensor/profile regression checks pass. Long-session stability, every native sight combination and complete mass/cost aggregation are not exhaustively validated.

With .NET SDK 8 and locally generated BepInEx interop:

```powershell
dotnet build SprocketThermalSight.csproj -c Release
dotnet run --project tests/Sensor.Tests.csproj -c Release
```

Game/loader binaries are referenced locally and not distributed. Created by RoanWassink with AI assistance. [MIT license](LICENSE) covers this mod's code and original assets.

## Donations

Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods: [Donate via PayPal](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6).
