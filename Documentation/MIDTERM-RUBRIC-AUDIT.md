# Midterm lab exam audit

Verdict: **not ready for this rubric**. Rune Rumble runs and its current gameplay checks pass, but the exam asks for a different set of mechanics and presentation rules.

Audited game commit: `07dfa1b`. Deadline supplied with the assignment: Wednesday, October 28, 2026, 7:00 PM. This report does not assign instructor points.

## Fresh evidence

- `GameValidation.RunDuelSaved`: **686 checks passed**, covering roster confirmation, presentation, native attacks, five-hit knockdown, recovery, manhole falls and round scoring. [Report](DUEL-VALIDATION.json).
- The Windows executable was copied with its runtime files to another directory and launched there for **18 seconds**. It stayed alive, initialized its script domain and logged **zero script exceptions**. [Relocation report](MIDTERM-PORTABLE-AUDIT.json).
- The built gameplay assembly SHA-256 matches the previously verified v19 Windows manifest. [Build manifest](BUILD-WINDOWS-v19.json).
- Current title, roster and match screenshots were inspected. No production scripts, scene, packages or project settings were changed during this audit.

An automated game test passing means the implemented game works as tested. It does not mean the implementation satisfies a different assignment.

## UI and thematic authenticity: 40-point category

| Requirement | Result | Evidence and required correction |
|---|---|---|
| Title includes the required developer format and full name | FAIL | The current title has the logo and slogan, but no developer credit. `GameSetup.CreateMenus` does not create that credit. The exact full name must be supplied. |
| Clean character roster, background image and confirmation | PASS | Fresh scene checks and the roster screenshot show all eight choices, P1/P2 selection and lock-in controls. |
| Animated menu background | FAIL | `MenuBackdrop` creates a static image. `AnimateFrontEnd` animates selection portraits, stage cards and screen entrances, but not a continuously animated title background. |
| Selection sound effects | NEEDS JOHN | Character preview calls `PlayCue(12, .18f)` and the audio system has a playback path. Audit runs were deliberately muted, so audible output was not verified. |
| All UI in accurate pure Tagalog | FAIL | Visible labels include SINGLE PLAYER, LOCAL VERSUS, SETTINGS, EXIT, LOCK IN, STAMINA and English move descriptions. Generated text inside images also needs replacement. |
| Cohesive Philippine names and environments | FAIL | The current fantasy roster and canal/castle environments do not establish the requested Philippine setting. Renaming labels alone would not fix the environment requirement. |
| Portable executable works without installation | NEEDS JOHN | Relocated-folder startup passed on this PC. A clean Windows machine without Unity installed has not been tested. |

The assignment explicitly asks for a developer branding format while also requesting pure Tagalog UI. Preserve the required credit wording and confirm any intended exception with the instructor rather than silently omitting the credit.

## Gameplay mechanics and engine logic: 40-point category

| Requirement | Result | Evidence and required correction |
|---|---|---|
| Confirmation transitions directly into gameplay | FAIL | The current flow requires an additional arena-selection screen after character confirmation. A strict edition should start the match after the necessary character confirmations. |
| Horizontal movement only, no jumping in any state | FAIL | `Scout.TryJump` supports normal and double jumps. Keyboard and gamepad inputs expose them. Punch impacts can also apply upward force. |
| Straight punch is the only combat action | FAIL | Kick, guard, grab, dash, backstep, charged skills, ultimates and air attacks remain available. These are intentional Rune Rumble features, but conflict with this exam. |
| One fixed backward step per punch | FAIL | `Scout.ReceiveHit` applies physics impulses modified by damage buildup, combo state and attack power. That produces variable displacement rather than one fixed step. |
| Exactly five consecutive straight punches trigger distinct dizziness | FAIL | Any five confirmed attacks remove the target's hit squares and cause a timed prone knockdown. This is not a straight-punch-only consecutive-hit dizziness state. |
| No health bars or K.O. gameplay system | FAIL | The match HUD shows five hit segments and a stamina gauge. The fifth-hit behavior uses falling, downed and get-up states rather than the requested dizziness presentation. |
| Only falling into a manhole ends the fight | PASS for round ending | The fresh test confirms that the fifth hit does not end the round, while a manhole fall does. The current game uses a best-of-three match around these rounds. |

The current 99-second sudden-death floor shrink is an additional rule. A strict edition should avoid introducing a changing floor into the exam's fixed edge-manhole exercise unless the instructor permits it.

## Controller and input systems: 40-point category

| Requirement | Result | Evidence and remaining check |
|---|---|---|
| Independent P1/P2 controls without input bleeding | NEEDS JOHN | The source maps P1 to A/D and T, and P2 to arrows and I. The 686 presentation checks do not establish simultaneous physical-device isolation. |
| Keyboard/gamepad switching during a match | NEEDS JOHN | `RefreshGamepads` and `GetGamepad` support dynamic device assignment. Test a real keyboard and controllers on the final exam build. |
| Unplug/reconnect without stealing the other player's slot | NEEDS JOHN | The source preserves occupied slots and fills empty slots. Existing legacy fixtures contain simulated-device probes, but those older full suites were not rerun as proof for this audit. |

Do not claim the controller category's maximum rating from code inspection or software-created gamepads alone.

## Submission packaging

The current `RuneRumble.exe` is a Unity launcher, not a self-contained game file. Its working folder includes `UnityPlayer.dll`, `RuneRumble_Data`, `MonoBleedingEdge` and the other shipped runtime files.

Keep the required runtime files together. The relocation test excluded the `DoNotShip` debugging directory. If the submission form accepts an archive, package the portable game folder; confirm the expected format if it truly accepts only one `.exe` file.

No assignment was submitted or published by this audit.

## Decision before changes

The safe approach is a separate exam edition, preserving Rune Rumble's expanded fighting mechanics. No such edition has been created yet; the scope question is pending.

That edition needs the exact developer name, pure Tagalog text and illustrated labels, a Philippine setting, punch-only input paths, fixed horizontal recoil and a distinct fifth-punch dizziness state. It also needs its own build and acceptance checks.

## Additional engineering checks

These checks are separate from the instructor's 120-point rubric. FAIL below can mean not verified, rather than an observed runtime defect.

| Gate | Result | Evidence or limit |
|---|---|---|
| 1. Clean clone builds | FAIL | A clean clone with an empty Unity cache was not rebuilt in this audit. |
| 2. Empty-environment dependency install | FAIL | Packages and authoring dependencies are declared, but a clean dependency install was not performed. |
| 3. Simple startup | PASS | The relocated executable launched directly with its runtime folder. |
| 4. Repository history secret review | FAIL | No complete history scan was performed in this rubric audit. |
| 5. Missing external configuration | NOT APPLICABLE | The inspected runtime is an offline local game with no API-key configuration. |
| 6. README behavior matches implementation | PASS | Its expanded combat description matches the inspected source and current verified game behavior. |
| 7. Reported counts have evidence | PASS | The fresh 686-check report and prior v19 413-check delivery have saved reports. |
| 8. Limits documented | PASS | README identifies local play and unverified physical controller feel. |
| 9. All documentation references checked | FAIL | Seven README local links resolve; all historical documentation was not exhaustively checked. |
| 10. Every invalid input checked | FAIL | No exhaustive invalid-state/input audit was performed. |
| 11. Unhappy-path coverage | PASS | The v19 counterplay and charge fixtures cover misses, shields, cancellation and interrupted casts. |
| 12. Crash-safe persistent match data | NOT APPLICABLE | Matches have no persistent save records. Only disposable user settings are stored. |
| 13. External-call failures | NOT APPLICABLE | No runtime service calls were found in the inspected gameplay path. |
| 14. Public service isolation | NOT APPLICABLE | The game exposes no public service. |
| 15. Tests and documented command | PASS | README lists `GameValidation.RunDuelSaved`; its fresh run passed. |
| 16. Core gameplay tested | PASS | Actual hits, fifth-hit state, recovery and manhole scoring are covered by the saved-scene fixture. |
| 17. Continuous integration | FAIL | No workflow is tracked and `gh run list --limit 3` returned an empty list. This is not an exam rubric requirement. |
| 18. Distribution status documented | PASS | README identifies the local Windows build and Unity project workflow. |
| 19. Live website availability | NOT APPLICABLE | The deliverable is a native game. |
| 20. Phone layout | NOT APPLICABLE | The submitted platform is Windows. |
| 21. Keyboard and screen-reader access | FAIL | Keyboard navigation is implemented; screen-reader support was not verified. |
| 22. Picture of the project | PASS | README includes game screenshots, refreshed by the scene suite. |
