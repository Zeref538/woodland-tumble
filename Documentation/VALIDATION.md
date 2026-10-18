# Tilt Rivals v7.1 HUD and controller validation

The final targeted fixture passed 213 assertions. It verifies the removed arena label, enlarged VS, exact meter centers, zoomed-out circular portraits, the rendered device-specific Menu hint, the real pause-menu Move List callback, all four characters' skill explanations and text fit, and returning to pause without resuming combat. Existing bottom diamond mouse actions and refusal checks remain included.

Sony's actual device-description matchers were exercised for PS4 product IDs 05C4 and 09CC, DualSense 0CE6 and DualSense Edge 0DF2. Two XInput layouts and generic Gamepad were checked for all required controls, correct family prompts and the device-specific move guide. Disconnection restores Esc in the actual footer text. This does not prove physical USB/Bluetooth transport or every third-party hardware variant.

The older 693-check combat report remains retained as the v7 baseline. HUD-centered-1280-v7.1.png and Move-list-1280-v7.1.png show the revised native scene. BUILD-v7.1.json records the final Windows build and runtime hashes.

# Tilt Rivals v7 validation

Unity 6000.5.10f1 passed 693 core Play Mode checks against the saved scene. A separate final HUD run passed 164 checks covering both bottom diamonds, 80-pixel targets, circular masked portraits without opaque fighter slabs, all sixteen character covers, PlayStation/Xbox labels and all eight real button callbacks. Accepted actions and refused cooldown/paused actions were checked for feedback and exact resource effects. See PLAYMODE-VALIDATION-v7.json and HUD-VALIDATION-v7.json.

The core run covers four ranged specials, six-second cooldowns, the exact 25-health ultimate threshold, once-per-round limits, guarding, misses, interruption, camera cleanup, victory animations, AI and virtual controller mappings. A separate 28-check melee run verifies a natural punch-punch-kick string without teleporting fighters. It dealt 48.8 damage and spent 22 stamina. Eight hit/finisher sequences use 48 new sprite frames.

The new animation and platform sources were retained. All 692 PNGs in the artwork pack decoded; manifests record output hashes and sizing. The game includes 160 new dash, backstep, victory and ultimate frames, eight new PVP platform images and four new jump covers. Original Balance geometry is restored on mode changes.

HUD-bottom-diamonds-1280-v7.png is the final HUD render. Other v7 screenshots document the earlier combat test stages and may retain the earlier top action layout. Physical USB/Bluetooth controller transport and subjective audio balance remain unverified; automated playback was muted.

# Tilt Rivals v6.2 character-cover validation

The current scene passed 526 Play Mode checks. The new coverage switches all four characters through both player slots and verifies all 32 action-cover assignments plus 32 unchanged input badges. All sixteen catalog sprites load from separate assets. The exported files also have sixteen distinct decoded RGBA hashes and transparent edge margins.

All sixteen images were visually inspected on Character-Action-Covers-v6.2.png. Four actual game captures, character-hud-{fern,ember,luna,bram}-v6.2.png, were inspected with both HUDs visible. They show Fern/Ember, Ember/Luna, Luna/Bram and Bram/Fern pairs. These exercise the current native scene and selected character objects, not mockup screens. BUILD-v6.2.json records the successful Windows build and current script hashes.

# Tilt Rivals v6.1 validation

The updated scene passed 460 Play Mode checks in Unity 6000.5.10f1. New presentation checks verify matching compact panel heights, circular covers at equal diamond endpoints, mirrored outside-edge portraits, generated VS artwork and absence of the sound-percentage popup. All existing combat and controller checks remain in the same passing run. BUILD-v6.1.json records the successful Windows build and current runtime script hashes.

The final HUD render is TiltRivals-HUD-v6.1.png. The controller family badges are native UI over separate pixel-art action covers. Text labels and shoulder hints have been removed from the action diamond; the full field guide retains the action names and shoulder mappings. No audio playback was heard during validation because the listener stayed muted.

## Earlier v6 baseline

Unity 6000.5.10f1 passed 454 Play Mode checks against the rebuilt scene, then passed the same 454 checks against the saved scene after the crystal-prop crop correction. PLAYMODE-VALIDATION-v6.json records the final assertions. Older version reports remain historical evidence.

The fixture clicks real menu buttons through solo/local play, Arena PVP/Balance, character selection, map selection, settings and pause navigation. It verifies four independent animation catalogs, all four elemental special behaviors, four matching arena sets, shared stamina costs, failed-action costs, regeneration, combos, guard/parry, scoring and six edge-collision cases. HUD checks measure portrait position, inside-bar values, text fit and top-corner bounds.

Two independent virtual gamepads exercise input slots, movement, exact face/shoulder mapping, jump/double jump, Backstep, Grab & Toss, specials, guard, unplug/reconnect and menu shortcuts. Temporary DualShock and XInput layouts verify the actual family-specific guides. The fixture removes its virtual devices and restores previously enabled hardware on completion.

The first expanded run stopped after 416 checks at a compound Backstep assertion. Diagnostics split its cooldown, stamina and speed checks. The next two complete runs passed, measuring stamina 88 and backward speed -7. No gameplay change was made for that assertion. Unity's Editor and runtime input buffers differ, so raw trigger values read from EditorApplication.update are not treated as evidence that runtime input failed.

The thirteen sound clips decode nonzero waveform samples and start on eight pooled AudioSources. Tests keep AudioListener volume at zero. Physical USB/Bluetooth controllers, physical keyboard play and subjective headphone balance remain unverified.

All 360 character PNGs decode. Each of 36 action folders has ten distinct file hashes. Ordinary frames are 256x256; all forty Special frames are 384x256 to retain body proportions. New frames, original source hashes and crop measurements are recorded in ROSTER-ART-v6.json. ARENA-ART-v6.json records the corrected crystal-prop crops.

Screenshots ending in -v6.png are actual 1280x720 Play Mode renders. The fixture temporarily renders its UI canvas through the capture camera with sorting order 1000; it restores the normal overlay canvas afterward. Menu screenshots show the saved native UI over the illustrated backgrounds.

Batch validation uses -silent-test -executeMethod GameValidation.Run to rebuild first, or GameValidation.RunSaved to check the saved scene. Preserve custom demo edits before rebuilding. BUILD-v6.json records the final Windows build and script hashes.
