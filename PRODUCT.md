# Rune Rumble

A 2D woodland ring-out fighting game built from the existing Unity project. Eight elemental fighters share a polished pixel-art style. Solo AI and two local players fight on six maps, with breakable rune covers at the edges.

Current user direction: five hit squares per fighter, not a health bar. Five confirmed hits cause a temporary knockout; rivals can push the downed fighter into an open edge hazard. Recovery restores five squares. Falling loses the round. Rounds last 99 seconds; first to three round wins takes the best-of-five match. Keep circular character portraits, names and the established skill-circle controls, with a new central VS emblem.

Prioritize centered, consistent-scale sprite catalogs, including downed and get-up animations. Generate small sheets concurrently, at most four rows per sheet, then inspect and calibrate them before integration. Project editor remains Unity 6000.5.10f1. See TODO.md for completion state; historical build reports do not validate this rework.
