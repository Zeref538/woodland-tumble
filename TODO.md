## Current verified checkpoint

GitHub CLI is connected as Zeref538. Unity 6000.5.10f1 imported and rebuilt the scene; the revised duel fixture passed 153 checks including the final enlargement/HUD pass. Sprite padding audit: 1,288 separate sprites, zero output-edge touches and zero source-body clipping flags. Latest visual steering: enlarge gameplay fighters 2× uniformly, match collision/feet to art, use tightly cropped selection portraits, and restore the original Tilt Rivals HUD ordering (five connected hit segments in the old top-meter position, stamina below, names underneath). Rebuild and 153-check duel fixture passed after this pass.

## Latest selection and arena direction

- [x] Build fighter selection with two large player showcases, eight portrait cards, player markers and lock-in states in the scene builder.
- [x] Build arena selection with one large selected-stage preview and six illustrated thumbnail cards in the scene builder.
- [x] Replace timeout draws with sudden death: holes are open from round start and expand inward after 99 seconds.
- [x] Verify the selection layouts, open-hole placement, and timed expansion in Unity 6000.5.10f1.

# Rune Rumble: current rework

User direction: retain 2D pixel art; remove Balance and add Tekken-inspired combat. LATEST PRIORITY: focus on sprites first and generate independent small sheets concurrently. Latest mechanics: five hit squares replace the health bar; the fifth confirmed hit causes temporary knockout, recovery refills five squares, and rivals can push a downed fighter off the edge. Defer further gameplay changes until the sprite phase is complete. Earlier health/timeout work below is an intermediate prototype and must be replaced. Latest scope: user approves Zephyr, Volt, Nyx and Corin designs; redesign all four original fighters in that same style; reuse the existing project systems as the foundation for a new game direction; add two maps and breakable edge hatches/grates for falls. User confirmed falling through loses the round by ring-out.

- [x] Read handoff, current task history, gameplay and asset architecture.
- [x] Confirm 2D combat direction with the user.
- [x] Check local editor: Unity 6000.2.2f1 installed; project requires 6000.5.10f1. Preserve ProjectVersion.txt.
- [x] Connect GitHub CLI as Zeref538 and restore repository history safely without replacing local files.
- [x] Record baseline file hashes before editing gameplay/assets.
- [x] Remove Balance selection and tilting-plank gameplay; keep solo AI and local multiplayer.
- [x] Implement first duel pass: automatic opponent facing, 99-second rounds, timeout and draw rules (Unity verification pending).
- [ ] Revise the first pass enclosed-edge behavior for the newly requested breakable fall hazards.
- [x] Remove health graphics from the HUD builder; preserve circular portraits, names and existing skill circles. Saved scene update and Unity visual check pending.
- [x] Set 99-second rounds and retain first-to-three / best-of-five scoring. Timeout draws restart without health comparison.
- [x] Rename visible game title, Unity product name and Windows output to Rune Rumble; preserve repository links.
- [x] Generate and integrate a new centered VS emblem.
- [x] Implement five confirmed-hit squares and 3-second temporary knockout/recovery in existing scripts; downed bodies can be pushed without extending the recovery timer. Unity verification pending.
- [ ] Replace health/damage-based round wins with temporary knockdown, recovery and pushing to ring-out; remove health bar and rewrite move guide. Deferred while prioritizing sprites.
- [ ] Generate assets in small reviewed batches: approve each character silhouette, then each action sequence, then UI/effects; reject large one-shot sheets.
- [x] Remap keyboard inputs and HUD: P1 WASD, Q/E dashes, T/Y/G/H actions; P2 arrows, U/P dashes, I/O/K/L actions; F/J grab. Runtime verification pending.
- [x] Create and receive user approval for Zephyr, Volt, Nyx and Corin character designs.
- [x] Redesign Fern, Ember, Luna and Bram to match the approved new visual style while retaining their identities.
- [x] Add two original maps with dedicated background and arena art.
- [ ] Finish breakable rune hatch/cracked grate artwork and test ring-outs. Initial durability/collider/reset implementation is in existing scripts.
- [x] Generate all eight animation sets in focused batches, at most four rows per image; inspect each before integration.
- [ ] Inventory original fighter animation states, frame counts, clips, portraits, action covers, guard icons and effects.
- [x] Create complete independent animation artwork for each new fighter with matching coverage and consistent foot pivots.
- [ ] Create four new portrait/action/guard artwork sets and melee, ranged and ultimate effects.
- [x] Integrate eight-character roster, navigation, native animation assets, skills and AI.
- [x] Update saved editable scene and builder together; preserve a scene backup.
- [x] Update existing validation fixtures for duel rules and all eight fighters.
- [ ] Run Unity 6000.5.10f1 checks and build; inspect gameplay and artwork visually.
- [ ] Update README, HANDOFF and current design documentation with verified results.
- [ ] Review intended diff and commit to a dedicated GitHub branch; report remaining device checks.

Completion requires real animation coverage and distinct skills/effects, not renamed copies or recolored substitutes. No new C# helper/editor files. Historical check counts below describe the previous release, not this rework.

# Agent Tasks

- [x] Add a Mac lab HANDOFF.md with editor version, controls and safe save/update commands.

- [x] Add controller Play Again at match end and reduce damage by 15 percent.
- [x] Generate four matching pixel-art shield icons for guard.
- [x] Slim the current GitHub tree, preserving original art and historical screenshots.
- [x] Validate replay, damage and guard art: 112 gameplay/replay checks and 28 combo checks passed; Windows build succeeded.
- [x] Publish and verify the 63,573,322-byte source ZIP, 79.27 percent smaller. Copy 1,786 files to D:/GameDev/TiltRivals with matching hashes.
- [x] Add exact Unity version and offline Mac installation instructions with verified installer links.

- [x] Remove upper PVP ledges and verify the Up jump / double-jump and Cross/A guard mappings.
- [x] Replace ranged cooldown with an interruptible charge, distinct projectiles and matching HUD guide.
- [x] Fix character-specific melee effects, remove streaks and add ghost-based ultimate transitions.
- [x] Match character body proportions and create true Luna/Bram idle portraits.
- [x] Validate the revised combat and inspect captures. 101 combat/presentation checks and 28 combo checks passed; Windows build succeeded.
- [x] Publish and verify the v8 source archive. CRC checked, GitHub digest and 306,673,591-byte size matched, public download returned HTTP 200.

- [x] Remove the arena label, enlarge VS, center meter values and zoom portraits out.
- [x] Add device-specific Menu hints and a paused move list for both selected characters.
- [x] Verify the revised HUD and guide, inspect screenshots and build the update. Final fixture passed 213 checks; Windows build succeeded.
- [x] Publish the v7.1 source update and verify its archive. CRC checked; GitHub digest and 285,240,784-byte size match; public download returns HTTP 200.

- [x] Connect dedicated PVP art slots, uniform platform sizing and original Balance geometry restoration. Unity passed 84 sizing checks across four map slots and three mode-switch cycles using generated test sprites. Production artwork is now integrated.
- [x] Verify a naturally connecting grounded punch-punch-kick combo; reduce early knockback so the finisher stays in range. Silent Play Mode passed 28 checks including stamina, whiffs, guard and parry.
- [x] Create 48 new character-specific combo-effect sprites, integrate confirmed-hit and finisher playback, and inspect the running effect preview.
- [x] Smooth melee input buffering and combat timing; remap shoulder dashes and trigger ultimate.
- [x] Add four unique ranged specials with cooldowns and low-health cinematic ultimates.
- [x] Create dedicated dash, backward dash, victory and ultimate animation art for every character.
- [x] Enlarge action circles to 80 pixels with adaptive prompts and accepted/refused press effects.
- [x] Create new larger PVP arena art and set the highest VFX quality as the default.
- [x] Move enlarged clickable action diamonds to the bottom corners, use circular portraits and remove the heavy fighter-panel backing. Silent Play Mode passed 164 targeted checks.
- [x] Verify combat, remapping, ultimates, HUD and win emotes; Windows build succeeded. Core 693 checks, final HUD 164 checks and natural melee 28 checks passed.
- [x] Publish the v7 source ZIP and verify its public download. GitHub digest and 283,523,395-byte size match; anonymous download returns HTTP 200.

- [x] Create sixteen distinct pixel-art action covers, four for each character.
- [x] Bind covers to the selected character and verify both player slots in the running game.
- [x] Inspect all four HUDs, build and publish the character-art update.

- [x] Align action diamonds with fighter HUD height, add illustrated action covers and button badges.
- [x] Rename the game Tilt Rivals and verify the updated scene.
- [x] Publish the HUD update and separate action artwork.

- [x] Generate and integrate illustrated menu and map-selection artwork.
- [x] Add Solo versus AI and local multiplayer menu flows.
- [x] Move fighter HUDs to the top corners.
- [x] Validate menus, AI and existing combat; build and publish the project.
- [x] Add four distinct character catalogs, portraits and elemental specials.
- [x] Add Arena PVP platforms and retain the Balance ruleset.
- [x] Add Skyward Ruins as the fourth illustrated map.
- [x] Match the portrait-left HUD reference with values inside both bars.
- [x] Map all face and shoulder buttons, including Backstep and Grab & Toss.
- [x] Add volume, fullscreen, camera shake and effects settings.
- [x] Update the game title and clone instructions.
- [x] Package the new artwork in the separate sprites repository.

- [x] Add independent controller controls and safe reconnect handling.
- [x] Refine HUD artwork, spacing and controller prompts.
- [x] Validate controllers and combat, build and publish the update.

- [x] Replace bulky HUD with compact fighter plates and cleaner typography.
- [x] Improve combat VFX layering, easing and camera feedback.
- [x] Add fitting licensed action sounds and audio controls.
- [x] Run combat regression plus presentation/audio checks.
- [x] Capture new running-game screenshots and publish source update.

# Zeref Tasks

- [ ] Check standalone controls with a physical keyboard. Desktop-injected keys were not observable in the previous smoke check.

- [ ] Check action sound balance with headphones. Automated playback checks ran silently.

- [ ] Connect real PS4/Xbox controllers and check both players, unplug and reconnect.
