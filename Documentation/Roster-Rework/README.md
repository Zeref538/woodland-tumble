# Rune Rumble roster artwork

Eight integrated fighters: Fern, Ember, Luna, Bram, Zephyr, Volt, Nyx and Corin. Each has Idle and ten distinct frames for sixteen actions: Walk, Jump, Hurt, Push, Punch, Kick, Knockout, Block, DoubleJump, Special, Dash, Backstep, Victory, Ultimate, Downed and GetUp.

Runtime PNGs are in `Assets/Art/CharactersRework`; native catalogs and clips are in `Assets/AnimationsRework`. Uniform scale is calibrated against a neutral standing pose in each small generated batch. Standing reference height is 190 pixels, imported at 80 pixels/unit. Frame canvases may expand for effects; pivots preserve the foot position and the body never shrinks to fit effects. Dedicated tightly cropped portraits are in `Assets/Art/PortraitsRework`.

`Tools/prepare_roster_rework.py` records source components, extraction scale, hashes, bounds and pivots. The padding audit covers all 1,288 runtime images and reports zero source-body edge cuts or output-edge touches. Catalog validation checks frame coverage and distinct frames. The Unity duel fixture checks native clip coverage and gameplay separately.

Generated using the built-in image tool in small inspected batches. Large initial sheets and floating-platform drafts were rejected. Source generation images and animated contact sheets remain local and are ignored by Git; final runtime images, calibration, extraction report and validation are versioned. Full regeneration from source sheets requires the local workbench.
