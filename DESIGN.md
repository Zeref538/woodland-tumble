# Approved direction: arcade interface, ink effects

Use Documentation/Design-Samples/Arcade-Aura.png for the main menu, HUD, lettering, bright color accents and fighter-card composition. Use Documentation/Design-Samples/Ink-and-Motion.png for sweeping brush attack arcs, contact splashes and their decay. These are approved concept boards, not implemented screenshots.

Keep the eight existing large detailed fighter designs and native animation catalogs. Do not replace them with the older four small scouts. Preserve the current five-hit knockdowns, 99-second sudden death, fixed grounded arenas and open edge manholes. No wood-frame, shard or rune decorative system. The game name remains Rune Rumble.

Build separate background, atmosphere, fixed floor, hazard, fighter, VFX and live-interface layers. Keep labels and meters editable and test their actual bounds. Generate independent assets concurrently and include the shared direction in every prompt. Check title, roster, stage selection and HUD at 1280x720, 1440x900 and 1920x800 before calling the design implemented.

Selection update: the active fighter stays on the left for both players, with four distinct high-resolution pixel-art breathing poses. The roster sits to the right. HUD circles are dedicated close-up face artwork. The map picker uses six tall slanted illustration panels and a subdued navy exposure; scenery brightness stays below fighter and effect brightness. Replace Corin's crystal identity with Marin, a navy-haired tide fighter with a teal headband and sea-blue coat.

Readability overrides the previous lettering choice: use normal upright sans-serif text, bold headings and strong white labels. Menu actions use solid dark surfaces with clear focus borders. HUD action discs use centered dedicated icon crops with captions below. Arena selection has a quiet dedicated background and selection lift, image zoom and a brief cyan glint.

Combat framing: use a closer camera for large fighters, preserving the physical body and sprite scale. Pull back to contain both players when they separate or jump. Hold the view during pause. Track the falling rival sideways during ring-out without following below the floor, and clamp camera travel to the background edges. The primary stage is Lantern Crossroads, a wet street-level plaza with elemental shop lights and deep perspective.
