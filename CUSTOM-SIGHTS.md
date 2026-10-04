# Customize image quality or add a thermal sight

Back up `BepInEx/plugins/SprocketThermalSight/thermal-models.json`. Edit the existing profile object inside `models`, preserving its partGuid and componentId. JSON uses decimal points, double quotes and commas between entries, with no trailing comma. Unknown fields are rejected. The root has `"version": 1`.

## Supported fields

Defaults below are the class fallback values for omitted fields, **not** all shipped presets. Explicit supplied values are listed in thermal-models.json. Avoid removing fields merely to change presets.

| Field | Fallback / allowed values | Meaning |
|---|---|---|
| partGuid | Required, unique GUID | Must match the part JSON guid. Keep stable after saving vehicles. |
| componentId | Required, unique nonempty string | Must match the sight component's fileID. Helper IDs use letters, numbers, _ or -. |
| displayName | Required, nonempty | Profile/log label; actual part name is in its XML localization. |
| width / height | 320 / 240; width 80–1024, height 60–768 | Sensor pixels; width × height must be ≤524288. |
| refreshHz | 15; 1–60 | Requested captures/second, subject to rendering and readback limits. |
| contrast | 1.4; 0.1–5 | Contrast about the signal midpoint; high values clip detail. |
| brightness | 0; -1–1 | Offset before final gamma/palette conversion. |
| gamma | 1; 0.2–4 | Final curve is value^(1/gamma); higher values lift intermediate tones before black-hot inversion. |
| noise | 0.025; 0–0.5 | Random signal variation. |
| blur | 0.25; 0–1 | Blend toward a 3×3 neighborhood average. |
| grayLevels | 64; integer 2–256 | Signal quantization levels, even with colored palettes. |
| palette | whiteHot | whiteHot, blackHot, greenHot, amberHot, ironbow, rainbow or custom; exact case. |
| blackHot | false; boolean | Legacy inversion flag. Either true OR palette=blackHot inverts once; use false with named palettes to avoid unintended inversion. |
| customColors | ["#000000", "#FFFFFF"] | 2–16 #RRGGBB stops; required to remain valid for any palette, used only by custom. Ordered from low to high signal before inversion. |
| smoothPixels | false; boolean | Bilinear display filtering instead of hard pixels. |
| flipVertical | false; boolean | Flip output rows for orientation correction. |
| backgroundLevel | 0.3; 0–1 | Terrain signal baseline. |
| backgroundDetail | 0.3; 0–1 | Strength of native camera luminance detail. |
| backgroundGamma | 0.6; 0.2–4 | Terrain curve: level + luminance^backgroundGamma × detail; lower values lift faint terrain tones. |
| vehicleHeat | 0.8; 0–1 | Uniform vehicle signal, not measured temperature. |
| extraMassKg | 15; 0–500 | Added mechanism mass in kg above the sight baseline. |
| extraAssemblyCost | 300; 0–100000 | Added assembly cost in native game units. |

All numeric fields must be finite. Duplicate part GUIDs or component IDs reject the catalog.

## Practical recipes

These are **fields to change in an existing object**, not a complete replacement catalog.

**Reduce processing load:** start with width=320, height=240, refreshHz=15. Resolution and capture rate drive work; merely adding blur does not reduce the number of processed pixels.

**Less bright white-hot terrain:** the shipped Mk3 white-hot settings are backgroundLevel=0.24, backgroundDetail=0.18, backgroundGamma=0.85. Lower backgroundLevel slightly to reduce the baseline; preserve enough backgroundDetail to see terrain. Adjust one field at a time and compare the same scene.

**Keep terrain detail:** reduce overly high contrast or increase grayLevels. Lower backgroundGamma lifts dark native terrain differences, but can make the background too bright. Noise=0 and blur=0 yield cleaner edges without adding sensor resolution.

**Switch an existing model to black-hot:** set palette="blackHot", blackHot=false. This changes its display without making a new part. For a separate choice in the parts list, use the helper below.

**Personal colored display:** set palette="custom" and customColors=["#001020", "#20A0B0", "#FFFFFF"], blackHot=false. Colored parts are not included in the default release. This is optional personal customization, not a historical accuracy claim.

After editing, enter an active thermal scope and press F8, then N. Invalid edits keep the previous catalog in memory. Restart for key-binding changes and newly installed parts. Rebuild/reload the tank after mass/cost changes; their complete native aggregation remains a validation limit.

## Add a new part with the customization kit

Download **SprocketThermalSight-v0.1.5-customization-kit.zip** from the release. It includes the helper, installer, current DLL and source templates; it is an optional authoring workspace, not a folder to paste into the game.

1. Extract the kit into a writable folder and close Sprocket.
2. If you already have custom profiles, copy your installed thermal-models.json into the kit first. Also copy the matching custom JSON/XML part files into its parts folder; the installer expects files for every catalog entry.
3. Open PowerShell in the extracted kit and run:

```powershell
.\Add-ThermalModel.ps1 -Id myThermalScout -DisplayName "Thermal sight - Scout" -CopyFrom thermalSightModel2
```

The helper backs up the catalog and creates a new unique part GUID, component ID, English XML name and copied profile. It refuses existing IDs/files. Do not regenerate an ID after saving tanks with it.

4. Edit the new myThermalScout object in the kit's catalog. For a lower-load scout sensor, try width=160, height=120, refreshHz=10. Adjust noise, tones and mass/cost to suit your intended gameplay tradeoff.
5. Run:

```powershell
.\Install.ps1
```

For another Steam library:

```powershell
.\Install.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Sprocket"
```

The installer checks the supported game binary, requires Sprocket closed, backs up replaced files and merges missing profiles into an existing catalog. **Existing profile values are preserved**: if you edit a profile already installed, update its installed catalog entry yourself; reinstalling will not overwrite it with kit values. It does not remove other mods or retired parts. An invalid existing catalog or identity conflict stops installation.

6. Restart Sprocket and place your named sight. F8 cannot register a new part with the native part importer.

If Windows execution policy blocks these scripts, you can create the files and copy them manually instead. No policy change is necessary for ordinary installation.

## Manual addition

Copy a supplied profile and its matching JSON/XML pair. Generate a new GUID, set the copied part's guid and profile's partGuid to it, then set a unique componentId and matching sight-component fileID. Set the part name to your ID, and the XML name to your visible label. Append the copied profile to models with correct JSON commas.

Install the JSON under Sprocket_Data/StreamingAssets/Parts, XML under Localization/en-UK/Parts, and the updated catalog next to the plugin DLL. Keep the model component and stock asset references from the template; do not change unrelated component IDs. Restart and verify the new sight is recognized in the log.

Changing only displayName does not create a new native part. Changing IDs on an existing part can break saved vehicles. Before removing a custom sight, replace it on affected saved tanks, then remove its matching files and catalog entry with the game closed.
