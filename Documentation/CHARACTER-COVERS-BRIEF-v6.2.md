# Character-specific HUD artwork

Four original pixel-art medallions per character, arranged Punch/Kick above Block/Special in each source atlas. Keep a common dark forest and gold frame, stepped outlines, visible square color clusters, transparent gutters and readable central shapes. No lettering or controller glyphs are baked into the art. Each atlas was created separately and preserved unchanged in Assets/Art/Sources.

Fern: leaf-wrapped leather fist, ranger boot with a leaf trail, twisted-root shield, and thorny vines binding an amber rune. Ember: red-gloved flame punch, flaming heel kick, crossed red bracers under a fire ward, and two forward flame comets for Flame Rush.

Luna: a pale mage palm casting a crescent impact, a pointed moon boot with stars, a crescent and ice ward, and a travelling frost wave with crystals. Bram: a rock gauntlet punch, a stone-toed boot scattering chips, crossed stone gauntlets guarding, and a downward rock fist breaking the ground for Stone Quake.

Source atlases were mechanically sliced at their four cells, cropped to visible alpha and scaled uniformly with nearest-neighbor sampling into 256x256 images. Every output has at least four transparent pixels at its edges. CHARACTER-COVERS-v6.2.json records source rectangles and decoded image hashes. The contact sheet is a labelled comparison of the actual exported files.
