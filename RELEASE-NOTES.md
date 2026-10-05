<!-- sp-compat {"hamish.sprocket": "0.2.55.5", "bepinex.bepinex": "6.0.0-be.788"} -->

**Required dependency: [Sprocket Keybinds API 0.1.5](https://github.com/RoanWassink/SprocketKeybinds/releases/tag/v0.1.5). Hydropneumatic, Telescopic Mast, Thermal Sight and Smoke Launchers will not load without it. The full pack includes it: keep its DLL installed. For separate plugin downloads, install the API ZIP once, merging its BepInEx folder into your game folder.**

One configurable thermal sight with selectable, saved profiles and monochrome thermal display.

## What changes for you

Controls use the shared Mod keybinds menu and the rendering repair is retained. Thermal operation and placement are limited to Cold War; an imported earlier-era sight keeps its saved profile and physical mass/cost but uses normal sight view.

**Beta:** tested together in the Cold War pack. Armour-response values are bounded gameplay approximations, not exact historical protection or a guarantee against every shell.

## Requirements and update

Sprocket 0.2.55.5, Windows x64 and an already-working Sprocket Mod Loader / BepInEx 6 IL2CPP setup. **Loader not included. Quality of Life not required.**
- [Sprocket Keybinds 0.1.5](https://github.com/RoanWassink/SprocketKeybinds/releases/tag/v0.1.5), installed separately once. This is required: without a compatible API, BepInEx skips this mod. The full pack includes it.
- Cold War availability requires the [Cold War core/pack](https://github.com/RoanWassink/SprocketColdWarExpansionPack/releases/tag/v0.1.0). Install its core-only download if you do not want the full pack.

Close the game, back up matching files and saves, then merge the ZIP's folders into the game directory. Keep one DLL per plugin and preserve customized configs/catalogues/WAV overrides. See [README](https://github.com/RoanWassink/SprocketThermalSight#readme) for exact use, controls, limitations and uninstall instructions.

[Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6).

[Separate loader installation](https://github.com/Hans21223/Sprocket-Mod-Loader).

