# Current verified presentation, 2026-10-24

Keep the PC on. Shutdown was revoked.

The editable scene uses Arcade Aura UI, six new maps, open street manholes and 192 Ink & Motion effect frames. All eight large detailed fighter catalogs remain calibrated and intact. The native Windows player is Build/Windows/RuneRumble.exe; the entire Windows folder is required when moving it.

Current verification passed 504 saved-scene checks, including actual menu callbacks, readable label bounds, all eight fighters, temporary knockdowns, timed sudden death and ring-outs. The 16 counterplay checks passed as well. Title, roster, stage selection and HUD were captured at 1280x720, 1440x900 and 1920x800.

A real Windows build and one hidden native startup were checked. Its assembly contains the current Prism Arc label, and its scene contains the separate FIRST TO 3 timer label. No script exceptions were logged during that startup. BUILD-WINDOWS-v9.json records the artifact hash and size. Physical two-player/controller feel remains unverified.

The previously rejected combined cleanup/verification command was not retried again. The old fonts were retained. Subsequent Unity scene verification and building ran as separate authorized tasks without deleting those fonts. Do not reissue the rejected cleanup command.

817 unused legacy asset files were removed after checking active scene/prefab references. Recoverable copies remain in ../woodland-update-workbench/removed-v9. The current roster preparation script takes its template from CharactersRework. Do not apply the rejected workbench draft or old stash.

Art prompts: Documentation/ART-BATCH-v9.json. Source effect sheets: Documentation/Design-Samples/FX-*-v9.png. Runtime effects: Assets/Resources/Art/CombatV9. Tools/prepare_combat_v9.py repackages reviewed sheets without making images; optional Python dependencies are pinned in Tools/requirements.txt.
