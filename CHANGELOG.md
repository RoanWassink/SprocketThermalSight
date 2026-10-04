# Changelog

## v0.2.1 — One sight, selectable profiles

- One Thermal sight part with a right-click Thermal sight → Profile dropdown.
- Store the chosen JSON profile ID separately on each sight in vehicle saves.
- New profiles need only a catalog entry; no new part GUID, JSON or localization files.
- Configurable defaultProfileId, retained legacy sight definitions and missing-ID fallback.
- Shared sight icon, rebuild on profile selection and mass-cache refresh.
- Fix startup failure in the unpublished v0.2.0 test build by replacing unsafe save/load hooks.

The creator confirmed the menu/profile choice and saving/loading work in-game. Existing profile values are preserved when using the optional merging installer; manual catalog replacement overwrites custom settings.

## v0.1.5 — First public release

- Five thermal sight parts: model 1, 2, 3, Mk3 White hot and Mk3 Black hot.
- Scope-only N toggle and F8 profile reload.
- Per-part quality, tones, optional palettes and added mass/cost settings.
- Latest white-hot terrain brightness tuning; black-hot settings retained.
- Default release restricted to monochrome parts. Optional custom palette processing remains available through JSON.
- Customization helper creates unique profile/part/name identities; optional installer preserves existing profiles and backs up replaced files.

The creator confirmed thermal operation in-game. Image processing is a gameplay approximation; full vehicle mass/cost aggregation and long-session stability are not exhaustively validated.
