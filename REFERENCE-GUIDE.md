# Thermal sight v0.2.3

One configurable Thermal sight. Right-click it to choose a JSON profile, stored separately per sight.

Controls are in the existing Settings / keybind menu:
- Thermal / Toggle (default N)
- Thermal / Reload profiles (default F8)

SprocketKeybinds >=0.1.3 <0.2.0 is required, installed once as a shared plugin. This package does not include or modify its DLL. Existing API bindings win, including unbound/reset choices. Legacy CFG keys are imported once; use Settings for further edits.

Stable action IDs: nl.roan.sprocket.thermalsight/toggle and nl.roan.sprocket.thermalsight/reload-profiles.

Profiles remain in thermal-models.json. componentId is the stable profile ID; displayName is its menu label. New profiles need no part GUID/assets. defaultProfileId chooses the initial profile. Missing saved IDs fall back with a warning and retain their saved identity.

v0.2.2 dynamic-resolution rendering is preserved byte-for-byte in source. User confirmed it working. Shared bindings in this combined v0.2.3 build still require live validation.

Installer backs up DLL, legacy CFG, profile JSON and changed assets. API choices are not edited.
