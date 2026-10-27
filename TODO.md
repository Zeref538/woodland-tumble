# Rune Rumble: current delivery

## Midterm rubric audit

- [x] Re-run the saved scene suite: 686 checks passed on the current commit.
- [x] Launch the relocated Windows folder for 18 seconds without script exceptions.
- [x] Record rubric failures and submission dependencies in Documentation/MIDTERM-RUBRIC-AUDIT.md.
- [ ] Choose whether to prepare a separate strict exam edition. No gameplay changes made during this audit.
- [ ] Confirm the exact full developer name for the required title credit if preparing the exam edition.
- [ ] Verify simultaneous physical controllers, unplug/reconnect and clean-machine startup before submission.


## Comic feedback and aerial combat

- [x] Add ten rotating confirmed-hit words, comic guard text and an animated combo popup.
- [x] Add usable air specials and verify airborne punch and kick contacts.
- [x] Let punch and kick cancel a charge, and verify opponents can interrupt special startup.
- [x] Run combat regressions, rebuild Windows and push the verified changes.

## Idle size and charge feedback

- [x] Match breathing idle artwork to the existing combat body and head size for all eight fighters.
- [x] Show BLOCKED when a ranged attack contacts the front shield.
- [x] Allow a second special press or guard to cancel a charge before release.
- [x] Verify cancelled charges cannot fire later, inspect idle loops and rebuild Windows.

## Full animation proportion audit

- [x] Inventory every active animation frame and create fixed-scale comparison sheets for all eight fighters.
- [x] Inspect 392 representative poses across all eight fighters; record malformed frames and scale changes.
- [x] Repair 10 detached-effect/duplicate-body frames and calibrate 20 Luna/Bram hurt frames. Preserve clip timing and foot pivots.
- [x] Pass 92 Unity contact checks, rebuild Windows and verify an 18-second native launch after these sprite repairs.
- [x] Inspect every one of 1288 frames using 32 full comparison sheets. Repair two Ember ultimate frames, Nyx's reversed backstep and two neighboring-frame fragments.
- [x] Verify all 1288 clip bindings, pass 92 contact checks, 686 scene checks and 16 counterplay checks, then rebuild Windows and verify native startup.





## Button, animation and contact correction

- [x] Use one full-width illustrated surface per button, with no duplicate frame.
- [x] Match fall, downed and get-up artwork and scale across all eight fighters.
- [x] Match melee and projectile contacts to body bounds and real front shield bounds.
- [x] Render shields opaque, preserving attacks from the exposed back.
- [x] Remove the old slash-like block VFX and prevent walking through downed bodies.
- [x] Match the circular tap-recovery reference with device-specific input glyphs.
- [x] Navigate the roster in two dimensions using keyboard, D-pad and stick.
- [x] Verify all eight ultimate contact areas and Volt interruption cleanup.
- [x] Add a longer knockdown with capped X-tap recovery and an overhead press effect.
- [x] Regress front/back contacts and all eight recovery animations, inspect screens and rebuild Windows.

Current delivery: comic feedback, air attacks and special interruptions passed 413 combat checks across eight fighters. Windows rebuilt and passed an 18-second native launch with no logged script exceptions. Evidence: Documentation/DELIVERY-v19.json.

- [x] Confirm Windows build and verify native startup.
- [x] Raise the recovery glyph contrast on the dark key face and inspect the corrected prompt.
- [x] Commit and push the verified delivery.

## Midterm ink and combat revision

- [x] New ink-battle menu backdrop and illustrated FIGHT SMART. FALL LAST. slogan.
- [x] New generated artwork on every button, long in menus/pause and short in stage selection.
- [x] Remove navigation instructions along the bottom of every HUD.
- [x] Illustrated move cards with each fighter's name and skill imagery.
- [x] Lower guard hold and impact stamina costs without removing grabs or parries.
- [x] Generate and integrate six real elemental shield poses for each of eight fighters.
- [x] Fix Volt's committed Thunder Rush and verify it hits from its intended range.
- [x] Stop knockdown drift while allowing an opponent to push the body.
- [x] Animate the ultimate-ready HUD and extend all ranged attacks.
- [x] Run combat regressions, scene checks and a fresh Windows build within the 20 percent reserve.


Completed 2026-10-27: 686 scene checks, 38 combat checks and 16 counterplay checks passed. Windows built successfully and stayed alive for an 18-second startup sample without logged script exceptions. Usage reserve was 47 percent at the final build check. Physical controller feel remains open.

- [ ] Stop the completed daily heartbeat: the desktop automation tool returned Transport closed.

## Illustrated menu refresh

- [x] Generate and integrate eight labeled ink buttons and seven arena plaques.
- [x] Generate and integrate a battle backdrop with all eight active fighters.
- [x] Replace the primary arena with a restrained pixel-art Cinder Quay.
- [x] Remove footer shading and integrate the new artwork.
- [x] Inspect the final centered controls and pass the 18-second Windows native startup check in v14.


## Ink menu and dedicated ability art

- [x] Restore brush-edged ink menu controls with readable labels and focus feedback.
- [x] Redraw Marin gameplay and selection proportions, with a larger 210-pixel standing body.
- [x] Generate and integrate 32 dedicated illustrated ability icons, preserving the diamond layout.
- [x] Pass 668 current scene checks and 16 current counterplay checks.
- [x] Move the roster footer below the selection skill subtitle and prove the overlap check fails before the fix.
- [x] Build Windows and pass an 18-second native player startup check after the subtitle fix.

Subtitle delivery completed on 2026-10-27. The saved scene passed 668 checks. The Windows player passed its 18-second startup sample without logged script exceptions.


- [x] Recover and preserve the actual eight-fighter checkpoint.
- [x] Keep the larger detailed fighters, one uniform scale and calibrated foot pivots.
- [x] Add punishable missed/blocked attacks, persistent guard and confirmed punch-punch-kick routes.
- [x] Keep fixed arenas, five-hit temporary knockdowns, open manholes and first-to-three ring-out matches.
- [x] Build the approved Arcade Aura menu/HUD with Ink & Motion effects.
- [x] Generate related images in concurrent batches with the shared written art direction.
- [x] Refresh all six maps and the manhole illustration.
- [x] Integrate 192 character-colored slash, hit, finisher and guard frames.
- [x] Layer scenery, hazards, fighters, effects and live UI; add menu entrances and action feedback.
- [x] Remove 817 unused legacy asset files and preserve recoverable copies outside the project.
- [x] Fix the clipped timer, crowded input badge and inconsistent Corin skill name.
- [x] Inspect title, roster, arena selection and HUD at 1280x720, 1440x900 and 1920x800.
- [x] Pass 668 current scene/UI/combat checks and 16 counterplay checks.
- [x] Build Windows and check one native player launch with no logged script exceptions.

The remaining old font files were left untouched after the cleanup command was rejected. They are not required by the active scene.

## Manual verification still open

- [ ] Check two-person keyboard/controller feel on physical hardware. Software fixtures do not establish this.

Evidence: Documentation/DUEL-VALIDATION.json, COUNTERPLAY-v9.json, FX-SLICING-v9.json, ASSET-CLEANUP-v9.json and BUILD-WINDOWS-v9.json. Earlier reports in Git describe older rules and art.

## Selection refinement

- [x] Generate eight detailed pixel-art breathing sheets and eight dedicated circular HUD portraits.
- [x] Put the active P1/P2 fighter on the left and roster on the right, with switching transitions.
- [x] Darken arena scenery and replace stage selection with tall angled panels.
- [x] Verify the new layouts and animation, then rebuild Windows.


- [x] Replace Corin with Marin in the saved roster, including gameplay animation, effects and move labels.
- [ ] Remove the unused Corin files: the safety gate rejected the original command and the user-approved retry. No further removal attempt is authorized by the tool gate.

## Readability refinement

- [x] Replace slanted display text with a normal font and clear button labels.
- [x] Redesign action discs and center 32 skill icons by their visible artwork.
- [x] Add a dedicated quiet stage background and stronger selection motion.
- [x] Recheck label bounds, inspect the final screens and rebuild Windows.

- [x] Replace the main arena with Lantern Crossroads: a street-level pixel-art plaza with deep perspective.

- [x] Add closer dynamic fight framing, with automatic pullback for separation and jumps.

Current delivery evidence: Documentation/DUEL-VALIDATION.json (686 passed), COUNTERPLAY-v9.json (16 passed), MIDTERM-COMBAT-v14.json (38 passed), BUILD-WINDOWS-v14.json and NATIVE-WINDOWS-v14.json. The native player was closed after its 18-second startup check.

- [x] Verify padded selection poses retain their full hat and hood details, then refresh Windows delivery.

## Diamond HUD and typography

- [x] Restore diamond skill placement with readable captions and centered icons.
- [x] Place a new VS emblem above a compact live timer.
- [x] Lower fighter framing and show more surrounding architecture with less floor.
- [x] Create and integrate distinct custom header, subheader and body fonts.
- [x] Verify layouts and camera behavior, then rebuild Windows.
