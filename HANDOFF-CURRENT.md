# Continue Rune Rumble

Workspace: C:/Users/johna/OneDrive/Documents/Portfolio/woodland-tumble. Branch: feat/rune-rumble-combat-presentation, based on origin/lab-duel-roster at 1a4d735. The older GameDev/woodland-tumble checkout has unrelated uncommitted work and was left untouched.

The user's final choice is Arcade Aura menu/HUD and Ink & Motion attack effects. Read DESIGN.md and Documentation/ART-DIRECTION.md; inspect both boards in Documentation/Design-Samples before generating anything. Preserve all eight CharactersRework fighters. Do not resume the rejected wood, shard/rune-frame or all-liquid experiments.

Combat changes are in Scout.cs, WoodlandEffects.cs and WoodlandHUD.cs. GameValidation.RunCounterplay reproduced the missed-punch recovery failure before the change and now passes 16 checks: whiff commitment, guard endurance, block punishment, repeated-punch route rejection, natural P-P-K finisher and stamina. Reports are Documentation/COUNTERPLAY-BEFORE-v9.json and COUNTERPLAY-v9.json. Full presentation/ring-out regression and standalone build remain pending. The editor also logged an unrelated Unity Search index exception; the controlled Play Mode checks completed successfully.

Next implement approved artwork in the existing GameSetup/HUD/effects files, rebuild the saved scene, run GameValidation.RunDuelSaved and RunCounterplay, inspect three aspect ratios and all eight fighters, then build Windows. Do not claim current concept boards are running-game screenshots. Keep TODO.md current.

Earlier default-branch work is retained in git stash. Rejected image drafts and an unapproved interface patch are outside the project in ../woodland-update-workbench. Do not apply that patch blindly. Generated originals remain under C:/Users/johna/.codex/generated_images/01a16970-d7f5-7dc2-906c-dd1f89b33816.

The user authorized PC shutdown at completion or 5 percent remaining usage. The 5 percent threshold was reached, so work was saved before shutdown. No shutdown automation was installed.
