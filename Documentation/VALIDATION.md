# Combat, VFX and edge physics validation

Unity 6000.5.10f1 completed the final Play Mode run with exit code 0 and status `COMBAT_VFX_AND_EDGE_PHYSICS_VALIDATED`. Full results are in `PLAYMODE-VALIDATION-v2.json`.

- Verified saved camera, scene/prefab references, player spawns, all 120 individual frame assets, Animator states and editable HUD bindings.
- Actual punch applies 16 damage and moderated horizontal knockback; actual pushes apply 12 damage. Repeated pushes grow from 4.97 to 6.24 impulse-derived horizontal speed as health drops. Nine pushes deplete full health, play Knockout and award a round.
- Attack VFX emit within the 320-sprite pool capacity. Jump, movement, hit, knockout and victory effects run without game exceptions.
- Tested right and left edges on a flat plank, right and left downhill edges at 22 degrees, and pressing inward against either side from below the top. In all six cases, the controller receives continuous movement input, the player falls clear, and side/corner contact does not count as ground.
- Verified jumping still launches, followed by landing and settling on the plank.

The fixtures use a stationary plank, virtual keyboard input and controlled character placement to isolate edge collisions. Normal gameplay retains the dynamic hinged plank. This covers the reported edge hang and the supplied movement/collision setup, rather than every possible custom Inspector configuration.

`GamePreview-v2.png`, `KnockoutPreview-v2.png` and `VFXPreview.png` include the HUD. `SPRITE-VALIDATION.json` contains the independent sprite bounds/checksum checks.

Unity's batch editor still logs its internal QuickSearch startup indexing exception. The game checks complete successfully.

To rerun: `Woodland > Validate Editable Game`, or launch a separate editor process with `-batchmode -projectPath <folder> -executeMethod GameValidation.Run -logFile <log>`.
