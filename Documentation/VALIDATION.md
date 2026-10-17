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
