# Current checkpoint, 2026-10-24

The PC must stay on. John revoked shutdown.

Arcade Aura UI, six new maps and 192 Ink & Motion effect frames are in the project. The saved scene was rebuilt. The first redesigned-scene regression passed 414 checks. Later corrections trim brush-panel padding, use clearer small text, add action captions, and improve eight-character guide descriptions. The final scene rebuild, label checks, repeat counterplay and Windows build are still pending.

Automatic approval review blocked a combined command to remove unused old fonts and run the final Unity checks/build. It reported only "blocked by policy". John approved the retry, but automatic review rejected that retry too. Do not retry again or work around the rejection. No shutdown command was issued after the user revoked it.

Current branch: feat/rune-rumble-combat-presentation. Presentation work is being saved as an unfinished checkpoint, without a release merge. Combat checkpoint c49b747 was already pushed.

Evidence: Documentation/DUEL-VALIDATION.json (414 first-pass checks), COUNTERPLAY-v9.json (16 earlier combat checks), FX-SLICING-v9.json (192 frames, no output-edge contacts), ASSET-CLEANUP-v9.json (817 unused assets, 78,794,446 bytes removed). The final corrections need new Unity evidence before release.

Backups of removed assets are outside the project in ../woodland-update-workbench/removed-v9. Cleanup verified the active scene/prefab asset references. Old four-character art, animations, menus, arena props and effects were removed. The old Python importer and its obsolete source manifest were also removed; the current roster importer now uses its template inside CharactersRework.

Art prompts: Documentation/ART-BATCH-v9.json. Sources: Documentation/Design-Samples/FX-*-v9.png. Runtime effects: Assets/Resources/Art/CombatV9. Repackage with Tools/prepare_combat_v9.py; optional Python dependencies are pinned in Tools/requirements.txt. It does not generate images.

After the command restriction is resolved, run final Unity verification and inspect fresh title/roster/HUD/guide captures at 1280x720, 1440x900 and 1920x800, fix actual failures, then commit and use the solo-repo fast-forward merge/push flow. Keep all CharactersRework and AnimationsRework catalogs. Do not apply the rejected workbench draft or old stash.
