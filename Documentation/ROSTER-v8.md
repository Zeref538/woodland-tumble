# Roster corrections v8

Luna and Bram now have dedicated planted idle poses. Their original Walk sprites were running poses and must no longer supply Idle clips or portraits.

Use `Assets/Art/Characters/Luna/Idle/01.png` and `Assets/Art/Characters/Bram/Idle/01.png`. Both images use a 256x256 canvas with a 200px tall subject, feet baseline232, and Unity feet pivot `(0.5, 24/256)`.

The first Bram draft looked too tall and thin. The selected replacement preserves his large head, chunky stone gloves and short legs. Both selected outputs were visually reviewed against the existing Walk sprites, then cropped and resized uniformly with nearest neighbour. Original transparent source files remain under `Assets/Art/Sources`.

## Size diagnosis

All four first Walk sprites measure 199 or 200px tall at the same current 160 pixels per unit. Ember's large beret and face occupy more width and height than Bram's small head and long-legged pose, so identical silhouette height does not make them feel equally large.

Do not resize each frame by its alpha rectangle. Projectiles, dust, raised fists, capes and ultimate auras change that rectangle without changing the character's body. The v6 and v7 manifests already preserve source-wide scale rather than independent frame normalization.

A modest optional display correction is 165 pixels per unit for Ember and 151 for Bram, retaining 160 for Fern and Luna. Apply one value to every state of each character. These are visual calibration recommendations, not measurements of an anatomical standard. The final scene should confirm the result before accepting them.

## Punch and kick inspection

The four characters have separate source artwork. Luna's punches show blue moon/frost energy and Bram's punches use stone gauntlets. Their kicks use different body poses. Fern and Ember share a scout costume silhouette but have their own green and orange designs.

The shared green overlay reported in combat should be corrected in the runtime effect selection. Replacing correctly distinct character sprites would not fix a procedural green slash drawn over every character.

## Evidence

`ROSTER-v8.json` records the two final image hashes, source crop rectangles, uniform scales and alpha bounds. It also records 320 existing frame bounds across Walk, Block, Punch, Kick, Special, Dash, Backstep and Ultimate. Bounds include effects and are labelled accordingly.

Both selected idle PNGs and both original sources were copied into the separate `Woodland-Scouts/tumble` artwork folder. No controller script, Unity scene, importer setting or animation clip was edited in this art track.

The v8 scene applies the recommended 165/151/160 pixel scales uniformly across states. Actual Play Mode captures were reviewed with those settings.
