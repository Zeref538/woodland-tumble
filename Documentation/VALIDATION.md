# Combat rework validation

Unity 6000.5.10f1 passed 170 Play Mode checks. The current machine-readable results are in PLAYMODE-VALIDATION-v3.json; the v2 report records the earlier build.

Checks cover 160 independent character sprites, their Animator bindings, matching background/plank/sphere changes across three arenas and unchanged collision proportions. Combat checks exercise real attack coroutines, damage, growing knockback, knockout scoring, jump/landing, double-jump budget, dash cooldown, shield drain, rear hits, parry stagger, guard break/recovery, charge/release, buffered three-hit chains and reset.

The six movement cases cover either edge on a flat plank, downhill edges at 22 degrees, and inward pressure against both side faces. The fixture uses virtual keyboard input and controlled placement on a stationary plank to isolate the collision behavior. Normal gameplay keeps the dynamic hinged plank.

Screenshots named arena-*-1280.png and combat-*-1280.png were captured from the current Play Mode run at 1280x720. They are running Unity renders, not concept pictures. Source art is checked separately in COMBAT-SPRITES-v3.json and ARENA-ART-v3.json.

Unity logs an internal QuickSearch startup indexing exception. The game checks still complete successfully. A StandaloneWindows64 build succeeded and launched at 1920x1080. The screenshot standalone-1920.png records the native player. Its log contains no game exception. Desktop automation key presses did not produce observable in-game responses, so physical-keyboard standalone play remains unverified; the Play Mode fixture verifies input using Unity virtual keyboard events.

To run the fixture inside Unity, choose Woodland > Validate Editable Game. Batch execution uses -executeMethod GameValidation.Run; that entry point rebuilds the demo scene before testing, so use a clean test copy if you have custom scene edits.
