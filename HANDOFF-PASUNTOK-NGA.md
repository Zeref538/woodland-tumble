# Pasuntok Nga! coding handoff

Paste the following prompt into a coding-agent chat with access to this workspace.

## Submission package completed

The build and archive now exist. Use `Build/Submission/Pasuntok-Nga-John-Andrei-M-Martinez.zip` for portable distribution. The archive passed CRC validation and its extracted EXE passed an 18-second native launch with zero script exceptions. `Documentation/Filipino-v20/DELIVERY.json` records 501 checks across the Filipino and original-game fixtures. The later input and layout checkpoints below supersede the older failures in the original prompt. Remaining rubric exceptions and physical-device/clean-machine limits are recorded in DELIVERY.json. Nothing has been submitted to the assignment system.

## Latest checkpoint: input fixture repaired

The historical 220-check failure described below is resolved. The latest `Documentation/Filipino-v20/VALIDATION.json` reports **289 checks passed**. The fixture now clones the working focus settings, isolates its simulated gamepads from existing devices and accepts normalized diagonal D-pad input around 0.707 per axis. Both keyboard schemes, no-jump inputs, simultaneous gamepads, unplug/reconnect and keyboard takeover passed in software.

Latest log: `C:/Users/johna/OneDrive/Documents/Portfolio/filipino-input-final-v20.log`. Original failure evidence is retained in `Documentation/Filipino-v20/INPUT-BEFORE.json`, `INPUT-FOCUS-AFTER.json` and `INPUT-DIAGONAL-BEFORE.json`.

The blank first-frame roster portrait and long HUD captions are now fixed. Eighteen layout checks and 114 presentation checks passed; the refreshed Roster.png and Match-Layout.png were visually inspected. Small action captions read ANGAT and ABOT. Next work is original-game regression and the separate Windows build. The new EXE has still not been built. Physical controllers remain unverified. This checkpoint supersedes the older failure and next-step notes inside the original prompt below.

```text
Goal: Finish, verify and build Pasuntok Nga!, a separate Filipino punch-focused edition of the existing Unity brawler, while preserving Rune Rumble.

Context:
- Local repository: C:/Users/johna/OneDrive/Documents/Portfolio/woodland-tumble
- GitHub: https://github.com/Zeref538/woodland-tumble
- Shell: PowerShell on Windows.
- Unity: C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Unity.exe
- Current branch: feature/filipino-punch-edition
- Branch base and latest committed main: 06a530f, docs: audit the current game against the midterm rubric.
- IMPORTANT: the Filipino edition is UNCOMMITTED work in this checkout, including many untracked generated assets and a new scene. Do not reset, clean, replace this checkout or recreate the work from main.
- Read C:/Users/johna/.codex/AGENTS.md, the repository CLAUDE.md and TODO.md. Follow the applicable engineering, imagegen and verify skills. No new C# files: extend the existing scripts.

User decisions:
- Confirmed title: Pasuntok Nga!
- Exact developer name: John Andrei M. Martinez
- Credit currently implemented as DEVELOPED BY followed by JOHN ANDREI M. MARTINEZ. The English credit phrase is retained for the assignment's requested branding format.
- Disable jumping everywhere.
- Replace the kick with a real, different punching animation, specifically a grounded uppercut.
- Replace elemental ranged skills with a long-range punch represented by a traveling fist.
- Replace every ultimate with an interruptible CLOSE-RANGE punch barrage, dealing at most three hit points through three contacts. No map-wide ultimate.
- All other visible interface wording should be Filipino and witty. The user specifically suggested WHIFF becoming BULAG.
- Give fighters strong Filipino first names with funny surnames.
- Keep the established detailed pixel characters, ink interface and diamond action-button arrangement.
- Do not shut down the PC. Previous shutdown requests were revoked.
- User previously requested preserving 20% remaining usage. Live quota tools were unavailable. Check live usage if possible; do not claim a remaining percentage from stale session logs.

Exam context:
- The assignment is due October 28, 2026 at 7:00 PM, user timezone Asia/Manila.
- The rubric literally requires horizontal-only movement, straight punches only, a fixed backward step per hit, dizziness after five consecutive straight punches, no health bars and Philippine theming with pure Tagalog UI.
- The user was explicitly told that uppercuts and long-range punches still conflict with straight-punch-only wording. They chose the custom punching variant anyway.
- Do not silently remove their requested uppercut, traveling punch or barrage to chase the rubric. Do not claim this variant satisfies every exam requirement or guarantees a grade.
- Existing rubric audit: Documentation/MIDTERM-RUBRIC-AUDIT.md and .json.
- No assignment has been submitted. Do not submit to the LMS without an explicit request.

Current state:
1. Rune Rumble is preserved:
   - Original scene: Assets/Scenes/WoodlandTumble.unity
   - Original Windows game: Build/Windows/RuneRumble.exe, with its full runtime folder.
   - Before the Filipino edits, the current original game passed 413 combat checks, a fresh 686-check saved-scene audit and an 18-second relocated native launch.
   - Original source baseline is 07dfa1b; 06a530f added only audit documentation and screenshots.

2. The new edition exists, but is unfinished:
   - New scene: Assets/Scenes/PasuntokNga.unity
   - WoodlandGame.punchEdition is true in that scene and defaults false in the original scene.
   - New animation catalogs/controllers: Assets/AnimationsFilipino/{original asset key}/
   - Original controller/catalog directory remains Assets/AnimationsRework/.
   - New build destination is configured as Build/PasuntokNga/PasuntokNga.exe.
   - That new EXE DOES NOT EXIST YET.
   - No Unity process was running at handoff time.

3. Fighter display names, in roster order:
   Fern -> Bagwis Talbos
   Ember -> Alab Tutong
   Luna -> Tala Ginawin
   Bram -> Dakila Batobato
   Zephyr -> Hagibis Ututin
   Volt -> Kidlat Pitik
   Nyx -> Lakan Antukin
   Marin -> Agos Sipunin
   Keep asset keys separate from display names. WoodlandGame.AssetNames holds the original eight keys; FilipinoNames holds display names.

4. Generated artwork is already complete and copied into the repository:
   - 18 source images: Documentation/Filipino-v20/Sources/
   - Prompt records: Documentation/Filipino-v20/prompts.json
   - Uppercut review sheet: Documentation/Filipino-v20/Uppercut-Review.png
   - Import records: Documentation/Filipino-v20/import.json
   - Runtime uppercuts: Assets/Art/FilipinoUppercuts/{asset key}/01.png through 10.png, 80 total.
   - Each uses six generated poses in ten animation keys, a shared per-fighter scale and a fixed foot anchor. No whole-body scaling or rotation is used to fake the uppercut.
   - Canvas: 384x384; feet at pixel y=352; pivot (.5, 1/12); PPU 80.
   - Runtime Filipino UI: Assets/Art/UI/FilipinoV20/
   - This includes the title, slogan, LABAN mark, 72 illustrated label variants, eight two-line fighter nameplates, six arena plaques and eight uppercut icons.
   - Traveling fists: Assets/Resources/Art/FilipinoPunches/{asset key}.png
   - Mechanical slicing tool: Tools/prepare_filipino_v20.py
   - The lettering core is preserved while side ink is widened. Do not stretch the lettering to remove padding.
   - All independent image requests were submitted concurrently and have finished. Do not generate them again unnecessarily.

5. Shared runtime changes are gated by punchEdition:
   - Scout.TryJump rejects the Filipino edition, including normal and double jumps. Idle/walk state selection avoids jumping animation in this edition.
   - The old kick slot uses the new uppercut sprites and a higher contact box.
   - Ranged cast uses a punching pose. SpecialProjectile deals a normal punch instead of elemental slow/root/lift effects.
   - WoodlandEffects.RangedProjectile loads the new traveling-fist sprites for this edition.
   - Scout.PunchBarrage handles every Filipino ultimate: 0.35-second tell, up to three contacts 0.26 seconds apart, fixed short-range area, then recovery. It remains interruptible and limited to once per round.
   - The original game's aerial attacks and elemental ultimates remain available when punchEdition is false.

6. Localization:
   - WoodlandGame.Text translates visible text using a longest-first, word-boundary regex over a translation dictionary.
   - The source translation record is Documentation/Filipino-v20/translations.json.
   - WoodlandHUD.LocalizeLabels applies it after live HUD updates and skips Developer Credit.
   - Filipino move instructions replace the old jump instructions.
   - WHIFF -> BULAG!, CLANG! -> KALANG!, with Filipino hit words and combo wording.
   - Generic controller directions/shapes are localized; real brands and physical key letters remain names/glyphs.
   - New illustrated button labels already contain Filipino text. Old English bitmap lettering is not used in the new menu.
   - Title background has a subtle continuous pan/scale animation in the new edition.

What was verified and what failed:
- GameSetup.PrepareFilipino compiled and completed successfully.
- Prepare log: C:/Users/johna/OneDrive/Documents/Portfolio/filipino-prepare-v20.log
- GameValidation.RunFilipino compiled and ran.
- Test log: C:/Users/johna/OneDrive/Documents/Portfolio/filipino-test-v20.log
- Current report: Documentation/Filipino-v20/VALIDATION.json
- It reports success=false with 220 completed checks, then fails at: P1 keyboard movement cannot move P2.
- All eight fighters completed the uppercut, 10.5-unit traveling punch, exact three-point close barrage, out-of-range barrage and interrupted-barrage cases before that failure. Idle direct calls to TryJump were rejected throughout these cases.
- Do NOT treat the input failure as a diagnosed production input-bleeding bug yet.
- Likely test-fixture issue: TickFilipino creates a synthetic Keyboard and Gamepads without copying the focus configuration used by the existing passing TickContacts fixture. That existing fixture clones InputSystem.settings, sets editorInputBehaviorInPlayMode to AllDeviceInputAlwaysGoesToGameView, sets backgroundBehavior to IgnoreFocus, and assigns the clone. Check this first and add diagnostics for both MovementInput values and playing/inputEnabled state.
- Input tests after that first keyboard check have NOT passed yet: P2 keyboard isolation, W/up rejection, simultaneous two-pad control, unplugging P1 without changing P2, keyboard takeover and controller replacement.
- Last Guide and Pause screenshots were not reached.

Known visual fixes still needed:
1. Roster opening frame shows a white rectangle instead of the large fighter.
   - See Documentation/Filipino-v20/Roster.png.
   - GameSetup creates Full Fighter Preview as an Image without an initial sprite. AnimateFrontEnd assigns the sprite on a later Update.
   - Initialize selectionPortraits[side].sprite from selectionIdleFrames[side*4] during scene creation, and verify no blank first frame. Do not reset its breathing pose every RefreshMenu call.
2. Small HUD captions can be too long for their 84x23 box.
   - Current translations KICK -> SUNTOK-ANGAT and SPECIAL -> ABOT-SUNTOK are too long for some small labels.
   - Prefer ANGAT and ABOT on the small Action Caption labels, while retaining the complete move names in descriptions and the move guide. Do not shrink/stretch the entire button artwork to solve text fitting.
3. Verify the move guide and pause screen visually after the input test advances. Check for clipped Filipino text and untranslated visible phrases.
4. The traveling-fist sprite uses a centered pivot while its solid fist lies toward the front of a wrist smear. Consider calibrating the pivot near (.75, .5) so the leading fist aligns with the swept collision box. Inspect before changing it, then test contact visually.
5. Arena-name source sheets include mini illustrations. They were retained in the plaques, so the current lower labels are smaller than plain text-only plaques. Inspect readability; no requirement to regenerate if acceptable.

Task:
1. Inspect git status and preserve all current edits and untracked assets.
2. Read TODO.md and this handoff, then inspect the three current screenshots in Documentation/Filipino-v20: Title.png, Roster.png and Arenas.png.
3. Fix the test input focus configuration, initializing the preview sprite and fitting the small captions. Do not edit C# while a Unity validation process is running.
4. Rebuild only the separate Filipino scene with GameSetup.PrepareFilipino.
5. Run GameValidation.RunFilipino again and fix actual failures until the report is successful.
6. Review the new Title, Roster, Arenas, Uppercut, Guide and Pause captures. The title and developer credit must be exact, and no blank portrait or clipped caption should remain.
7. Verify original-game compatibility with appropriate existing saved-scene tests, such as GameValidation.RunCounterplay and GameValidation.RunChargeFeedback. Do not rebuild the original scene unnecessarily.
8. Build GameSetup.BuildFilipinoWindows and inspect the actual build result in its log.
9. Launch the new exact EXE from Build/PasuntokNga for a measured native smoke test. Check script initialization and exceptions, then close only the process you started.
10. Package the portable game with its required UnityPlayer.dll, game Data directory, Mono runtime and other shipped runtime dependencies. An isolated Unity EXE is not a complete game. Keep DoNotShip debugging output out of a submission package.
11. Update TODO.md and a new delivery report with measured counts, native test results and remaining rubric exceptions. Do not claim the old 413 or 686 checks validate this new edition unless rerun as applicable.
12. Commit verified work using the repository's solo-repo rules. Preserve both scene/build paths, use no PR for this solo repo, and never skip hooks. Do not push an unfinished or unverified edition.

Useful commands, run from the repository:

PowerShell editor path:
$unity='C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Unity.exe'
$project=(Get-Location).Path

Prepare the Filipino scene (editor operation, use -quit):
Start-Process $unity -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','GameSetup.PrepareFilipino','-silent-test','-logFile',('"'+$project+'/../filipino-prepare-v20.log"')) -WindowStyle Hidden -Wait

Run the Filipino fixture (it exits Unity itself, omit -quit):
Start-Process $unity -ArgumentList @('-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod','GameValidation.RunFilipino','-silent-test','-logFile',('"'+$project+'/../filipino-test-v20.log"')) -WindowStyle Hidden -Wait

Build the separate game:
Start-Process $unity -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','GameSetup.BuildFilipinoWindows','-silent-test','-logFile',('"'+$project+'/../filipino-build-v20.log"')) -WindowStyle Hidden -Wait

For automation, Start-Process -PassThru and inspect that specific process rather than blocking the conversation for several minutes. Fixtures and builds must run sequentially against one Unity project. Inspect success fields, report counts and fresh timestamps, not only exit codes.

Important files:
- Assets/Scripts/Scout.cs: jump gate, uppercut bounds, traveling punch contact and PunchBarrage.
- Assets/Scripts/WoodlandGame.cs: edition flag, stable asset keys, funny display names and translations.
- Assets/Scripts/WoodlandHUD.cs: localized text, guide instructions and animated title background.
- Assets/Scripts/WoodlandEffects.cs: fist projectile rendering and punch-only swipe effects for the edition.
- Assets/Editor/GameSetup.cs: PrepareFilipino, isolated catalogs, illustrated labels, credit, new scene and separate Windows build.
- Assets/Editor/GameValidation.cs: RunFilipino/TickFilipino, locale scan, combat and input tests. Existing TickContacts shows the missing focus configuration.
- Tools/prepare_filipino_v20.py: deterministic asset extraction and protected lettering widths.

Constraints:
- No new C# files, no paid API calls, no API-key workarounds.
- Built-in imagegen only if a new raster asset or correction is actually needed. Inspect local references first. Submit independent requests together using Promise.allSettled and inspect results afterward.
- All 18 existing generated sources are already local. No new generation is running.
- Preserve character/head proportions, foot anchors and native sprite animation. Do not resize every pose independently or use whole-body stretching to fake movement.
- Preserve the original Rune Rumble scene, original assets/catalogs and original Windows build.
- Use reversible edits; do not delete legacy Corin assets or old fonts. Those cleanup attempts were blocked earlier, including an approved retry. They are unrelated to this task.
- No shutdown, no attribution lines, no --no-verify, no force push.
- Do not dump whole process command lines or session files; they can contain credentials.
- The Codex app MCP tools were returning Transport closed. Shell, image generation and local image viewing worked. Do not make a UI-only app tool a prerequisite for finishing the build.

Done when:
- PasuntokNga.unity opens with the correct title, credit and all eight renamed fighters.
- Original WoodlandTumble.unity remains a working separate edition.
- Jump attempts fail from the public API, both keyboard layouts and controller up inputs while characters remain grounded.
- The former kick visibly uses the generated grounded uppercut sprites.
- The special visibly sends a fist across its intended range.
- All eight close barrages remove exactly three hit points from a stationary unguarded nearby rival, cannot hit a distant rival and stop if interrupted.
- Filipino labels are readable and contained; WHIFF appears as BULAG!, with no white portrait rectangle or unintended visible English action instructions.
- The software input isolation/reconnect fixture passes. Physical two-person/controller feel is still labeled unverified unless actually tested with hardware.
- A successful new Windows build exists at Build/PasuntokNga/PasuntokNga.exe with its dependencies and has passed a real launch sample.
- A delivery report states what passed, sample counts, exact build path and honest remaining exam-rubric exceptions.

Output: concise summary of completed changes, verified counts, build/package location, any remaining limitation and the current git status. Link the actual local files using absolute paths.

If unsure: The title and developer name are already confirmed, so do not ask for them again. Preserve the user's requested punching variant and ask only if a new choice materially changes the game. Never label an unverified build or a failed fixture as finished.
```
