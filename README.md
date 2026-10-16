# Woodland Tumble

A local two-player woodland balance brawler for **Unity 6000.5.10f1, Universal 2D / URP 2D**.

Open `Assets/Scenes/WoodlandTumble.unity`. The camera, arena, player prefab instances and HUD are saved in this scene and are visible and editable outside Play Mode. If you already had the old scene open during the update, refresh the project and reopen the scene once.

## Play

| Action | Fern / Player 1 | Ember / Player 2 |
|---|---|---|
| Move | A / D | Left / Right |
| Jump | W | Up |
| Push | F | K |
| Punch | G | L |
| Pause | Escape | Escape |
| Restart match | R | R |

Face your rival and get close. Push deals **12 damage** with **4.8 base horizontal impulse**. Punch deals **16 damage** with **4 base horizontal impulse** and more upward launch. Damage makes subsequent knockback stronger, up to +30%. Nine successful pushes can deplete 100 health and knock out the opponent; being pushed off the plank also loses the round. A full collapse animation plays before the next round. First to three rounds wins.

## Edit the game

- **Arena:** expand `Environment - Edit These Objects`. Select the background, Mossbound Oak Plank or Runewood Balancing Sphere. Edit Transform, SpriteRenderer and the plank's BoxCollider2D/HingeJoint2D in the Inspector. The flat walking collider deliberately excludes decorative mushrooms and vines.
- **Starting positions:** move the two objects under `Spawn Points`. Their gizmos show each player's collision footprint. The Scout component's Spawn Point field connects each player to its marker.
- **Characters:** scene players are prefab instances of `Assets/Prefabs/Fern.prefab` and `Ember.prefab`. Their Character child contains SpriteRenderer and Animator. Keep character roots at scale 1 for the supplied collider proportions.
- **Animation:** select the Character child and open Unity's Animation window. Choose Walk, Jump, Hurt, Push, Punch or Knockout to edit sprite keyframes. Clips and controllers are in `Assets/Animations/Fern` and `Ember`. Each action contains ten separate frame sprites; knockout holds its final collapsed pose. Idle uses the first standing frame. The Frames asset is a catalog of source sprites; playback comes from the animation clips.
- **Sprites:** `Assets/Art/Characters/<Fern or Ember>/<Action>/01.png` through `10.png`. Each PNG imports as a **Single** sprite, **Full Rect**, 160 pixels per unit, Point filtering, no compression or mipmaps. All canvases are 256×256 with a common foot pivot (128, 24 pixels from the bottom). The complete outlines are extracted before alignment; the runtime does not cut a grid from a large sheet.
- **Combat tuning:** select a player's root and edit max health, push/punch damage, knockback, range and cooldown on Scout. Select Woodland Game to edit rounds to win, fall boundary and plank restoring force.
- **Text and panel design:** expand `HUD - Edit Text and Woodland Panels`. The title, subtitle and control text are ordinary editable UI Text objects. The nine-slice panel sprite is `Assets/Art/UI/WoodlandPanel.png`. The HUD component's Fern Color and Ember Color control the live player panel accents. Health, cooldowns, names and scores update from game state. The Press Start 2P font and OFL license are in `Assets/Fonts`.
- **Effects:** select `Effects - Trails and Splashes` to tune intensity, trail duration, shake, footsteps and particle gravity, or toggle ambient fireflies and camera shake. Effects include attack arcs, converging wind-up sparks, impact streaks and shockwaves, jump/landing dust, footstep leaves, knockout stars and victory bursts. A shared 320-sprite pool bounds active effects; pausing freezes their animation.

`Woodland > Rebuild Editable Demo Scene` restores the supplied demo layout, clips and prefabs; it overwrites custom demo edits, so use it only when you want to reset. Backups of the previous scene are under `Documentation/Backups`. `Woodland > Validate Editable Game` runs a temporary Play Mode check and returns to edit mode; it saves its report and previews under Documentation.

## Edge physics

Players use the zero-friction PlayerSlide physics material, and the plank has slightly rounded collision corners. Ground traction is applied along the plank's top surface and compensates for its tilt and motion. Side faces, corners facing sideways and the opposing player do not count as ground. Air steering does not stop falling or cancel horizontal momentum when movement keys are released. This avoids the old wall-friction hang at plank ends while keeping controlled movement on top.

## Art and validation

The new character animations and oak/rune arena sprites were generated with the built-in image generation tool, then mechanically separated, uniformly resized and padded into individual sprites. Original sources are retained under `Assets/Art/Sources`; generation prompts are in `Documentation/ART-PROMPTS-v2.json` and `ART-PUNCH-REVISION.txt`. Artwork was inspired by the supplied Woodland Scouts reference. No original repository artwork is included in the active game.

`Documentation/CharacterFrames-v2.png` is an overview only. The game uses the 120 independent PNG files. `SPRITE-VALIDATION.json` records every frame's size, alpha bounds and checksum. `Tools/prepare_art.py` reproduces the extraction with Pillow from the included source PNGs.

Public repository: [Zeref538/woodland-tumble](https://github.com/Zeref538/woodland-tumble). GitHub setup and lab-computer sign-out guidance are in [Documentation/GITHUB.md](Documentation/GITHUB.md).
