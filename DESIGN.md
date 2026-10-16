# Combat visual direction

Experience mode. Keep the established woodland artwork and pixel font. Effects communicate a rule: forward crescent for guard direction, expanding foot ring for an aerial jump, horizontal streaks for dash, converging sparks for charge and an outward burst for a confirmed hit.

Fern: mint #61FFAD and forest green. Ember: amber #FF7A2E with warm white highlights. Parry is a sharp white spark; guard break is red-orange. HUD panels remain deep forest #0C1F1D with brass #CCAA57 and readable pale text. Energy, cooldown and combo status must be visible without covering the fighters.

Three arena sets retain identical physical dimensions: Sunlit Hollow, Moonlit Lake and Ember Grove. Their backgrounds, plank and rune sphere change together. Sprite extraction uses transparent padding and uniform resizing; new platform variants share the original canvas dimensions so art changes cannot change collider size.

Each player has independent sprite clips. Block and aerial recovery get new poses; dash, charge, smash and combo clips reuse each character's own source poses with separately authored timing and rotation. The foot pivot stays fixed. Keep effects pooled and camera shake bounded.
