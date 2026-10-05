# Custom thermal profiles

The live file is `BepInEx/plugins/SprocketThermalSight/thermal-models.json`. Back it up. Each `models` entry has a unique componentId. Append an entry to the existing models array; do not replace somebody else's catalogue or delete profiles referenced by saves. A new profile does not need a new part or GUID. Leave partGuid empty for a profile used through the configurable sight.

`examples/thermal-models-with-custom-profile.json` is a complete valid starter catalogue with the five defaults plus `myThermalProfile`. It is intended for a clean setup/reference, not to overwrite custom data. The new entry reduces noise to 0.02; select it through right-click > Thermal sight > Profile. For your own file, copy that entry and choose a new componentId. Keep defaultProfileId pointing to a present componentId. Changing displayName does not change a saved profile identity.

Edit while paused/closed, then use Thermal / Reload profiles (factory F8) or restart. Config and starter profile defaults are preserved on update. Missing saved IDs fall back to a valid default with a warning while retaining the original selection identity. Profiles cannot bypass the Cold War eligibility gate.

| Setting | Supported values / units | Practical effect |
|---|---|---|
| width / height | Width 80–1024, height 60–768; width × height <= 524288 | Lower values give a coarser sight and less processing. |
| refreshHz | 1–60 Hz | Higher values update more often; higher workload. |
| contrast | 0.1–5 | Stronger tonal separation. |
| brightness | -1–1 | Shift the display brightness. |
| gamma | 0.2–4 | Change middle tones. |
| noise | 0–0.5 | Grain; 0 removes generated noise. |
| blur | 0–1 | Blend towards local smoothing. |
| grayLevels | Integer 2–256 | Tone quantization. |
| backgroundLevel / backgroundDetail | 0–1 | Keep the landscape visible; lift its level/detail. |
| backgroundGamma | 0.2–4 | Landscape tone curve. |
| vehicleHeat | 0–1 | Vehicle-mask signal, not actual temperature. |
| extraMassKg / extraAssemblyCost | 0–500 kg / 0–100000 game cost units | Physical premium retained even when thermal is inactive in an earlier era. |
| palette | whiteHot, blackHot, greenHot, amberHot, ironbow, rainbow, custom | Standard set is monochrome; colored palettes are personal customization. |
| customColors | 2–16 #RRGGBB colors | Custom palette, cold to hot. |
| blackHot, smoothPixels, flipVertical | true/false | Polarity, scaling and vertical orientation. |

JSON uses dot decimals, no comments and no trailing commas. Unknown fields, duplicate IDs, invalid palettes/dimensions or a missing default can prevent catalogue loading. Preserve the broken file, restore a known-good backup, restart, and inspect BepInEx/LogOutput.log. No thermal temperature, smoke occlusion or generation-specific historical deployment is simulated.
