# Rune Rumble: Mac lab handoff

## Active Rune Rumble rework

Use installed **Unity 6000.5.10f1**. The matching editor imported and rebuilt this project successfully. The new playable scene contains eight redesigned fighter catalogs, six grounded woodland stages, two-player character showcases, and a large-preview stage selector. `Documentation/RuneRumble-CharacterSelect.png` and `RuneRumble-StageSelect.png` are actual Unity captures; refresh them after layout changes.

Latest rules: five confirmed-hit squares, three-second temporary knockdown, pushing and ring-out wins, first to three rounds. Both mossy rune-stone holes are **open for the entire round**. At 99 seconds the fight continues and both holes expand inward, reducing the safe floor. The next round resets the holes. There is no Balance selection, health bar, floating platform, or health-based timeout result. Preserve the original circular skill controls and original four action icon sets.

Artwork: `Assets/Art/CharactersRework` contains 1,288 separate PNG sprites, with uniform per-sheet body scale and foot pivots; native catalogs/controllers live in `Assets/AnimationsRework`. Sources and calibration are in `Documentation/Roster-Rework`; source images are local workbench assets and must not be mistaken for runtime sheets. `Tools/prepare_roster_rework.py` extracts and validates catalogs. Never override generated sprite pivots with a common pivot when canvas heights differ.

Current keyboard mapping: P1 A/D move, W jump, S defend, Q/E backdash/dash, T punch, Y kick, G ranged, H ultimate, F grab. P2 arrows move/jump/defend, U/P backdash/dash, I punch, O kick, K ranged, L ultimate, J grab.

Current duel fixture: `GameValidation.RunDuelSaved` / `RunSaved`. The revised fixture passed 153 checks, including native catalogs, five-hit recovery, ring-out scoring and sudden-death floor shrinkage. The final larger-fighter/five-segment HUD pass also passed all 153 checks. Fighters use 80 pixels/unit (twice the initial rework size); standing colliders are 1.05 × 2.375 units, foot offsets 1.1875, and the grounded ray tracks that baseline. Do not cite old health/draw fixtures as validation of current rules. Git remote/history have been restored on `lab-duel-roster`; GitHub CLI is authenticated as Zeref538; the playable checkpoint is pushed to `lab-duel-roster`.

The older release notes below are historical where they conflict with this section.

## Goal and project

Continue editing Rune Rumble at the Mac lab, keep the working game intact, and bring the changes back through GitHub.

Repository: https://github.com/Zeref538/woodland-tumble
Artwork and original sheets: https://github.com/Zeref538/Woodland-Scouts/tree/main/tumble

## Unity version

Use **6000.5.10f1**, matching `ProjectSettings/ProjectVersion.txt`. This project uses Universal 2D / URP 2D and Input System 1.19.0.

The lab's **6000.2.2f1 is older**. Downgrade compatibility has not been verified. Prefer opening with 6000.5.10f1 through Unity Hub. If the lab only permits the older editor, use a separate clone and branch, keep the original ProjectVersion.txt, and test before saving converted files back to GitHub. Do not silently change the editor version on main.

For USB installation, exact Intel/Apple Silicon installer links, licence requirements and offline package preparation, read [OFFLINE-SETUP.md](OFFLINE-SETUP.md).

## Fast first download

The current tree contains the playable Unity project. Original source sheets are in the artwork repository, and old screenshots remain in historical tags/releases. A shallow clone skips those old Git objects.

Run in macOS Terminal:

```sh
cd ~/Desktop
git clone --depth 1 --single-branch https://github.com/Zeref538/woodland-tumble.git TiltRivals
cd TiltRivals
git switch -c lab-edit
```

In Unity Hub, choose **Add project from disk**, select `Desktop/TiltRivals`, and open `Assets/Scenes/WoodlandTumble.unity`. Let Unity finish importing before pressing Play. Library is rebuilt locally and is never uploaded.

## Current game

Four characters: Fern, Ember, Luna and Bram. Four maps support local multiplayer or solo AI. Arena PVP has one long fixed platform; Balance keeps its tilting plank and ball.

Controller controls during combat:

| Action | PlayStation | Xbox |
|---|---|---|
| Move | Stick / D-pad | Stick / D-pad |
| Jump / double jump | Tap Up, release, tap Up again | Tap Up, release, tap Up again |
| Hold guard | Cross or L2 | A or LT |
| Punch | Square | X |
| Kick | Circle | B |
| Charged ranged skill | Triangle | Y |
| Backdash / forward dash | L1 / R1 | LB / RB |
| Low-health ultimate | L2 + R2 together | LT + RT together |
| Match menu | Options | Menu |
| Play Again after champion screen | Cross | A |

Keyboard player one uses A/D, W jump, S guard, G punch, F kick, H ranged, Q backdash, Shift dash and T ultimate. Player two uses arrows, Up jump, Down guard, L punch, K kick, J ranged, U backdash, Right Shift dash and I ultimate. R restarts and Esc opens the match menu.

Ranged skills cost 14 stamina and charge for 0.85 seconds before firing automatically. There is no ranged cooldown. Incoming hits cancel the wind-up. Ultimates unlock at 25 health or less, once per round. Damage is scaled to 85 percent of the earlier build. Four pixel shield emblems replace character pictures on guard buttons.

The paused **Move List** explains both selected fighters using their assigned controller labels. Tests use virtual devices; physical controller USB/Bluetooth behavior still needs a local check.

## Files to edit

| File or folder | Purpose |
|---|---|
| `Assets/Scenes/WoodlandTumble.unity` | Saved editable game scene |
| `Assets/Scripts/Scout.cs` | Movement, guard, damage, charge, combos and ultimates |
| `Assets/Scripts/WoodlandGame.cs` | Match flow, maps, score, controllers and replay |
| `Assets/Scripts/WoodlandHUD.cs` | Action buttons, adaptive prompts and move guide |
| `Assets/Scripts/WoodlandEffects.cs` | Effects, ghosts, cinematic camera and audio |
| `Assets/Editor/GameSetup.cs` | Existing scene/HUD builder and build command |
| `Assets/Editor/GameValidation.cs` | Existing Play Mode validation fixtures |
| `Assets/Art/Characters` | Character animation frames |
| `Assets/Art/UI/Actions` | Per-character action and guard artwork |
| `Documentation/BUILD-v8.1.json` | Editor, checks and build evidence |
| `TODO.md` | Remaining work and user-only checks |

**Woodland > Rebuild Editable Demo Scene overwrites custom scene edits.** Do not run it after arranging objects by hand unless you intend to restore the supplied layout. Editing the builder also requires rebuilding the saved scene before distributing that layout.

## Work rules

Read `TODO.md` before changing anything. Keep each assignment in its own project and repository. Preserve professor-supplied scripts exactly, and do not add C# helper/editor files. Modify only the existing authorized game scripts for this project.

Keep sprite pivots at the feet and preserve aspect ratios. Do not stretch sprites or normalize each frame from its effects bounding box. Do not add paid dependencies, API calls, secrets or attribution lines. Do not upload Library, Temp, Logs, UserSettings or build output.

Historical reports describe older controls and damage. Use the newest report for the version being edited rather than treating every old assertion as a current requirement.

## Check and save lab edits

Stop Play Mode and run **Woodland > Validate Editable Game**. Confirm no red C# errors, test the changed behavior yourself, and keep `TODO.md` honest about unverified hardware.

After closing Unity, inspect and commit:

```sh
git status --short
git diff --check
git add Assets Packages ProjectSettings Documentation README.md HANDOFF.md TODO.md
git commit -m "feat: describe the lab change"
git push -u origin lab-edit
```

Replace the commit description with the actual change. The branch push backs up your work; it does not overwrite main. If GitHub asks for sign-in, use your own account and do not put a token in this file or a shell command saved in the repository.

On a machine that already has the project, close Unity before updating:

```sh
cd ~/Desktop/TiltRivals
git status --short
git fetch origin
git pull --ff-only
```

If there are local changes or Git refuses the update, keep the work and resolve the difference. Do not use reset --hard or force-push. Solo repository changes can be merged with --ff-only after validation, then pushed normally.

## Handoff for the next editor

State the intended change, branch, editor version and exact files touched. Report what passed and what still needs a real device. Finish when the project opens, the changed behavior works, the move guide matches the controls, the diff contains only intended files, and the work is committed or its uncommitted state is clearly recorded.
