<!-- sp-compat {"hamish.sprocket": "0.2.55.5", "bepinex.bepinex": "6.0.0-be.788"} -->

## What changes for you

Thermal placement and operation now use the owning vehicle design date from 3 September 1945 instead of an era name. Valid custom postwar and future eras are supported. Earlier designs keep their saved sight/profile and physical properties but use ordinary sight view.

**Beta.** 9,654 automatic checks passed. Native custom-era placement, switching and save/load testing remains pending.

The shared availability cutoff is **3 September 1945**, inclusive, without a finite future cutoff for valid registered eras. Earlier eras keep their supported features. Saved dates and customized settings are preserved. This does not change historical balance coefficients.

## Install or update

Requires Sprocket **0.2.55.5**, Windows x64 and a working **Sprocket Mod Loader / BepInEx 6 IL2CPP 6.0.0-be.788** setup. **Loader not included; Quality of Life not required.**

**Sprocket Keybinds API is required**; install [v0.1.5](https://github.com/RoanWassink/SprocketKeybinds/releases/tag/v0.1.5) separately if missing.

Close Sprocket and back up saves and matching mod files. Merge the ZIP's **BepInEx** and, where included, **Sprocket_Data** folders into the folder containing Sprocket.exe. Keep one DLL per plugin. **Preserve existing configs, custom Technology/material files, thermal-models.json and WAV overrides.**

See the [installation and customization guide](https://github.com/RoanWassink/SprocketThermalSight#readme) for requirements, examples and rollback.

[Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6).
