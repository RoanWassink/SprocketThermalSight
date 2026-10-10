# Sprocket Thermal Sight

<!-- sp-compat {"hamish.sprocket": ">=0.2.55.5", "bepinex.bepinex": "6.0.0-be.788"} -->

## New in 0.2.7

- **TPD-K1 laser rangefinding gunner's sight:** the T72-style daylight sight now has blue-green and amber optical panes and an integrated manual laser rangefinder. Its tooltip identifies both functions.
- **Corrected sight and FCS models:** updated creator-supplied meshes fix facing/orientation and surface appearance.

Fit the TPD-K1 as your sight and use the shared **Measure range** action from Settings/keybinds. No separate laser device is needed for this sight. It remains a daylight optic; fitting it does not add thermal vision or automatic ballistic ranging. Other rangefinder and thermal functions remain available as before.

**Wolfosito** created the T72 gunner sight and FCS models.


Thermal sights, rangefinders and fire-control sights for Sprocket 0.2.55.5. Version 0.2.7 adds the TPD-K1 integrated manual rangefinder and corrected models. Separate optical/laser equipment, FCS links and thermal profiles remain available.

## Requirements

Windows x64, Sprocket 0.2.55.5, a working Sprocket Mod Loader / BepInEx 6 IL2CPP setup, and Sprocket Keybinds API 0.1.6. The loader and API are separate dependencies; a full Cold War pack supplies the API. Era availability follows native part dates and the registered era timeline.

## Install and update

Close Sprocket. Back up matching Thermal files and vehicle saves. Copy the package's BepInEx and Sprocket_Data folders into the folder containing Sprocket.exe, merge directories, and replace matching files. Keep exactly one Thermal plugin DLL. Do not replace the entire shared directories.

Preserve customized BepInEx/plugins/SprocketThermalSight/thermal-models.json. The default catalogue is a starter example, not an update replacement. Existing old nl.roan.sprocket.thermalsight.cfg is copied to sprocket.thermalsight.cfg only when the latter does not exist; the original remains available for rollback. An existing neutral configuration wins.

## Fire control and rangefinders

Place an internal **FCS** within the gunner's normal operating reach. Link one or more external sights using the FCS inspector or the sight's FCS dropdown, then assign the external sight to the cannon as usual. There is no FCS-to-sight distance limit. Each sight links to one FCS; its own thermal Profile selection determines the image. A daylight sight remains daylight when linked. Thermal mode uses a toggle.

A fitted rangefinder provides measurements through any active sight on the same vehicle. The optical coincidence model takes longer and rounds readings more coarsely; its length affects the reading precision. The manual laser gives a reading, while the automatic laser also sets the aim range using the selected shell's ballistics, including APFSDS. Gun-launched guided missiles do not need ballistic ranging. These are gameplay aids, not guaranteed accuracy for every weapon and moving target.

The **Sights and electronics** menu contains the FCS, thermal heads, rangefinders, **TPD-K1 gunner sight**, and decorative scalable clear, tinted and yellow window panes. The panes can be used independently; they do not add thermal or rangefinding functionality. Build your own outer structures around the generic sensor models.

## Use

Place a thermal sight/head and use its right-click Profile menu to select a JSON profile. The selection is stored per sight. Added valid profiles appear in that menu; componentId values must be unique and defaultProfileId must reference an existing profile. Use Reload profiles after edits.

Configure Thermal / Toggle, Thermal / Reload profiles and Thermal / Measure range in Settings / keybinds. Toggle and reload have factory defaults N/F8; measure is initially unbound. The new rangefinders and FCS parts provide the rangefinder/FCS options; available parts depend on the owning design date.

Older vehicle saves retain legacy part/profile identities. Thermal imagery is a gameplay approximation rather than real temperature sensing.

## Configuration and rollback

See CUSTOMIZATION.md for profile settings, quality and refresh-rate units. Invalid profile changes retain the last accepted catalogue. Back up customized catalogues and settings. To roll back, close the game and restore the matching DLL, assets and native part/localization files together. Keep save backups.

## Credits and assets

**Wolfosito** created the T72 gunner sight and FCS models.

Made with AI assistance. The generic automatic rangefinder housing and its icon are original procedural assets by Nero, distributed under this repository's MIT license. See ASSET-CREDITS.md.

Support: https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6


## Saved profiles and compatibility

Existing profile IDs and console-link VUIDs retain their meanings. Older vehicle files remain untouched until you save them. On the next save, the profile key becomes sprocketThermalProfile; older roanThermalProfile keys are read as a fallback. If both are present, the neutral key wins. Back up vehicle saves before updating. Older plugin versions may not read the neutral profile key; restore your pre-update vehicle backup when rolling back.

The shared API migrates toggle, reload-profiles and rangefinder-measure bindings to sprocket.thermalsight before registration. Existing neutral bindings win, including deliberately unbound actions. Legacy binding records remain for rollback. The full migration requires API 0.1.6 or later within the compatible 0.1.x range.


## Rangefinder settings

Edit `BepInEx/config/sprocket.thermalsight.cfg` with Sprocket closed, then restart. `[Rangefinder]` has `Enabled=true`; maximum distance defaults to 4000 m (100–10000), minimum to 50 m (0–1000), cooldown to 3 seconds (0.1–30), and display duration to 5 seconds (1–30). A reading is a snapshot. The closest solid obstruction controls the result; an obstruction within the minimum range produces **Too close**. Keep minimum below maximum.

## Troubleshooting and removal

If the mod is missing, check the loader log and install one compatible Keybinds API DLL. Blank icons or missing models usually indicate incomplete asset/native-part installation. A missing linked FCS disables the thermal connection: select an existing FCS or direct eyepiece. Preserve malformed custom profiles, restore a known-good catalogue and reload it.

Before uninstalling, remove addon parts from vehicles you intend to keep using and save backup copies. With the game closed, remove this plugin's DLL, owned assets and part/localization files; leave shared Keybinds and other mods intact. Restore pre-update vehicles when rolling back to a version that does not read the neutral saved-profile key.

[Sprocket Keybinds download](https://github.com/RoanWassink/SprocketKeybinds/releases/tag/v0.1.6). [Sprocket Mod Loader](https://github.com/Hans21223/Sprocket-Mod-Loader).

[Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods.](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6)