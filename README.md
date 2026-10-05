# Sprocket ThermalSight

**Required dependency: [Sprocket Keybinds API 0.1.5](https://github.com/RoanWassink/SprocketKeybinds/releases/tag/v0.1.5). Hydropneumatic, Telescopic Mast, Thermal Sight and Smoke Launchers will not load without it. The full pack includes it: keep its DLL installed. For separate plugin downloads, install the API ZIP once, merging its BepInEx folder into your game folder.**

One configurable thermal sight with selectable, saved profiles and monochrome thermal display.

**v0.2.4 — beta.** Controls use the shared Mod keybinds menu and the rendering repair is retained. Thermal operation and placement are limited to Cold War; an imported earlier-era sight keeps its saved profile and physical mass/cost but uses normal sight view.

## Requirements

- Sprocket **0.2.55.5**, Windows x64, Unity 6000.3.21f1.
- A working **Sprocket Mod Loader / BepInEx 6 IL2CPP (6.0.0-be.788)** setup with its runtime and generated interop. Loader installation is separate. Stock BepInEx alone is not claimed equivalent to the tested Sprocket-specific setup.
- [Sprocket Keybinds 0.1.5](https://github.com/RoanWassink/SprocketKeybinds/releases/tag/v0.1.5), installed separately once. This is required: without a compatible API, BepInEx skips this mod. The full pack includes it.
- Cold War availability requires the [Cold War core/pack](https://github.com/RoanWassink/SprocketColdWarExpansionPack/releases/tag/v0.1.0). Install its core-only download if you do not want the full pack.
- Quality of Life is not required or included. Other game versions have not been verified.

## Install and update

1. Install a working Sprocket Mod Loader / BepInEx 6 IL2CPP setup, run Sprocket once, then close it. The loader is a separate prerequisite and is not included.
2. Download **SprocketThermalSight-v0.2.4.zip** from [this release](https://github.com/RoanWassink/SprocketThermalSight/releases/tag/v0.2.4).
3. In Steam, use Sprocket > Manage > Browse local files. Copy the ZIP's folders into the folder containing Sprocket.exe. Merge folders; keep the internal structure intact.
4. Keep one copy of each plugin. Back up matching mod files and vehicle saves before updating. Never replace the whole BepInEx folder.
5. Preserve existing BepInEx/config files, customized thermal-models.json and sound overrides. Install required dependencies separately. Restart the game.

## Usage, controls and settings

Place Thermal sight, right-click > Thermal sight > Profile, and select a profile. The choice is saved per sight. Thermal / Toggle and Thermal / Reload profiles are in Mod keybinds (N/F8 factory defaults). Profiles are kept in BepInEx/plugins/SprocketThermalSight/thermal-models.json; this is a **starter file**, never overwrite your customized copy. New profile componentId values must be unique and defaultProfileId must reference an existing one. Edit, then use Reload profiles or restart. [Custom profiles](CUSTOMIZATION.md) includes a complete starter catalogue and examples. Five monochrome defaults remain; custom palettes are optional. Thermal is a visual gameplay approximation, not real temperature sensing. Earlier-era/imported sights keep their profile and mass/cost but display ordinary scope view. Remove custom sight parts before uninstalling.

## Troubleshooting, saves and rollback

If the mod is absent, check BepInEx/LogOutput.log for the mod name, missing dependencies, duplicate plugin versions or invalid configuration. When this mod requires Keybinds, missing/incompatible Keybinds causes the mod to be skipped; old direct-key CFG entries do not replace that requirement. Preserve a malformed file for inspection instead of overwriting all your settings. Restart after repairs.

Restore your backed-up mod files and settings together for rollback. Do not delete an entire shared folder. Custom parts/materials may be referenced by vehicle saves: return affected vehicles to stock parts/materials and save before uninstalling. Keep save backups; installed mods and release archives do not back up every vehicle automatically.

## Credits and support

Made with AI assistance. Mod code is MIT licensed; native Sprocket meshes/icons are resolved from your installed game and are not bundled. Donation: [Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6).

## Where to get the separate loader

Use [Hans21223's Sprocket Mod Loader](https://github.com/Hans21223/Sprocket-Mod-Loader) and follow its [manual installation guide](https://github.com/Hans21223/Sprocket-Mod-Loader/blob/main/package/MANUAL-INSTALL.md) or its documented manager installation. That upstream project targets the tested Sprocket version and supplies the Sprocket-specific patch. These mod downloads do not install the loader. Follow one upstream loader method and its update/backup instructions; the creator's supplied ModManager archive is not redistributed here.
