# Balancer combat rework

Feature tier: expand the existing local two-player balance brawler. Keep the woodland arena, editable scene and two distinct characters. Unity remains 6000.5.10f1.

Fern uses leaf-green ward rings and agile spinning recovery. Ember uses amber sparks, heavier aerial kicks and fire-edged strikes. Preserve each character's existing independent sprite sequences; add dedicated authored transform-and-sprite clips for block, double jump, dash, charge and smash.

Combat rules: one extra airborne jump, reset only on landing; short dash with cooldown; front-facing shield drains while held, regenerates when released and breaks under pressure. The first 0.12 seconds of a fresh shield can parry; holding cannot repeatedly parry. Stun blocks attacks, jumps, shields and dashes. Charge commits the player and releases a stronger directional hit with capped charge. Keep health, increasing knockback and first-to-three rounds.

Implementation order: Scout state and input; pooled effects; animation clips and editable HUD; Play Mode checks for restrictions, shield direction, parry, break/recovery, air-jump budget, dash cooldown, charged damage and reset. Run existing edge-physics regression as well. Build a Windows player for a visual check. Publish source after checks pass.

No professor-supplied controller exists in this repository. Modify existing game scripts and existing setup/validation scripts only; add no helper MonoBehaviours.
