# Customize and add thermal profiles

Back up `BepInEx/plugins/SprocketThermalSight/thermal-models.json`. Edit the existing profile object inside `models`, preserving its stable componentId. JSON uses decimal points, double quotes and commas between entries, with no trailing comma. Unknown fields are rejected. The root has `"version": 1`.

## Supported fields

Defaults below are the class fallback values for omitted fields, **not** all shipped presets. Explicit supplied values are listed in thermal-models.json. Avoid removing fields merely to change presets.

| Field | Fallback / allowed values | Meaning |
|---|---|---|
| partGuid | Optional legacy GUID | Retain for old part compatibility; new profiles can omit it. |
| componentId | Required, unique nonempty string | Stable per-sight saved profile ID. New profiles need no matching part fileID. Helper IDs use letters, numbers, _ or -. |
| displayName | Required, nonempty | Visible Profile dropdown label; physical part remains Thermal sight. |
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

All numeric fields must be finite. Duplicate profile IDs or nonempty legacy part GUIDs reject the catalog. The root defaultProfileId (default thermalSightModel3) must reference an existing componentId.

## Practical recipes

These are **fields to change in an existing object**, not a complete replacement catalog. Each placed sight selects one profile from the right-click menu.

**Reduce processing load:** start with width=320, height=240, refreshHz=15. Resolution and capture rate drive work; merely adding blur does not reduce the number of processed pixels.

**Less bright white-hot terrain:** the shipped Mk3 white-hot settings are backgroundLevel=0.24, backgroundDetail=0.18, backgroundGamma=0.85. Lower backgroundLevel slightly to reduce the baseline; preserve enough backgroundDetail to see terrain. Adjust one field at a time and compare the same scene.

**Keep terrain detail:** reduce overly high contrast or increase grayLevels. Lower backgroundGamma lifts dark native terrain differences, but can make the background too bright. Noise=0 and blur=0 yield cleaner edges without adding sensor resolution.

**Switch an existing model to black-hot:** set palette="blackHot", blackHot=false. This changes its display without making a new part. For a separate dropdown choice, copy the profile or use the helper below.

**Personal colored display:** set palette="custom" and customColors=["#001020", "#20A0B0", "#FFFFFF"], blackHot=false. Colored parts are not included in the default release. This is optional personal customization, not a historical accuracy claim.

After editing, enter an active thermal scope and press F8, then N. Invalid edits keep the previous catalog in memory. Restart for key-binding changes. JSON-only new profiles can reload with F8; reopen the designer dropdown afterward, or restart if it does not refresh. Rebuild/reload the tank after mass/cost changes; their complete native aggregation remains a validation limit.

## Add a dropdown profile by copying JSON

Append this complete object to the existing models array, with a comma between objects:

```json
{
  "componentId": "myThermalScout",
  "displayName": "Scout - low load",
  "width": 160,
  "height": 120,
  "refreshHz": 10,
  "contrast": 1.15,
  "brightness": 0,
  "gamma": 1,
  "noise": 0.025,
  "blur": 0.25,
  "grayLevels": 64,
  "blackHot": false,
  "palette": "whiteHot",
  "customColors": ["#000000", "#FFFFFF"],
  "smoothPixels": false,
  "flipVertical": false,
  "backgroundLevel": 0.3,
  "backgroundDetail": 0.3,
  "backgroundGamma": 0.6,
  "vehicleHeat": 0.8,
  "extraMassKg": 12,
  "extraAssemblyCost": 300
}
```

No partGuid, new part JSON or XML localization is required. Save, reload with F8 in an active thermal scope, then reopen the designer: right-click the placed sight → Thermal sight → Profile → Scout - low load. Restart if the menu has not refreshed. Choose N again after profile changes to re-enable thermal.

Keep componentId stable after saving vehicles. Editing this object changes all sights referencing it. For a separate black-hot choice, duplicate the object with a new ID/label and palette=blackHot. Keep blackHot=false for the named palette. Do not duplicate an ID.

To make this the initial choice on new sights, set root defaultProfileId to myThermalScout. Existing explicit selections do not switch automatically. Old catalogs without that root field default to thermalSightModel3; if you removed that profile, set the field to an existing ID before launching.

## Optional helper kit

Download SprocketThermalSight-v0.2.1-customization-kit.zip and extract it into a writable workspace. It is an optional authoring folder, not a folder to paste into the game.

1. Close Sprocket. Copy your installed thermal-models.json into the kit first if customized.
2. Open PowerShell in the kit and run:

```powershell
.\Add-ThermalModel.ps1 -Id myThermalScout -DisplayName "Scout - low load" -CopyFrom thermalSightModel2
```

The helper backs up the catalog, copies a profile, removes its legacy partGuid and gives it your ID/label. It creates no extra physical part.

3. Edit the new profile in the kit catalog.
4. Run Install.ps1 to install/merge missing profiles with backups:

```powershell
.\Install.ps1
```

For another library, use `-GameDir "D:\SteamLibrary\steamapps\common\Sprocket"`. The installer expects the supported game build and Sprocket closed. Existing profile values win: editing a kit entry already installed does not overwrite it on reinstall; edit the installed catalog for changes to that entry. It preserves the existing defaultProfileId, adding it only if absent. Invalid catalogs/legacy identity conflicts stop installation. It does not delete other mods or retired parts.

5. Reopen the profile dropdown after restarting and select your new profile.

If PowerShell policy prevents scripts, use the manual JSON workflow above; ordinary installation needs no policy change.

## Sharing, missing IDs and recovery

A vehicle stores the selected ID, not the entire profile. Share the required catalog entry with the tank. Removing/renaming a selected profile makes the sight use defaultProfileId with a warning, while retaining the missing saved ID. Restoring the same ID restores the reference; explicitly choosing another profile replaces it.

Back up the catalog and use valid JSON with no comments/trailing commas. F8 rejection preserves the previously accepted catalog only in memory. Restore/fix the file before restarting. Key bindings still need a restart. Keep all six supplied native part/name pairs for older tanks; adding a JSON-only profile does not require creating or removing native part files.
