# Sprocket Thermal Sight 0.2.6

Compared with public 0.2.5, this update adds separate thermal heads, optical and laser rangefinders, an internal FCS linked to external sights and a T-72-style sight. Choose profiles on the installed sight; profiles remain defined in thermal-models.json and saved per sight.

Thermal heads have horizontal and vertical models, profile-dependent appearance and updated icons. The automatic laser sets aim range using shell ballistics, including APFSDS. Optical coincidence ranging shows Measuring... before a coarser reading. Sights are grouped in the native part menu. Legacy thermal parts retain their save identities and are hidden from new placement. Optical glass, lens/window rendering and the sight presentation are updated.

Availability uses each part's native date and the owning design's valid registered era timeline. Custom eras are supported by date rather than label. Existing saved profiles, physical values and thermal configuration remain compatible.

Configure Thermal / Toggle, Thermal / Reload profiles and Thermal / Measure range in Settings / keybinds. Measure range is unbound by default.

Requires a working Sprocket Mod Loader / BepInEx 6 IL2CPP setup and Sprocket Keybinds API 0.1.6. Loader, API and game assemblies are not included. Preserve customized thermal-models.json and keybind settings when updating. Thermal is a gameplay approximation.

<!-- sp-compat {"hamish.sprocket": "0.2.55.5", "bepinex.bepinex": "6.0.0-be.788"} -->


[Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods.](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6)
