# v7 artwork notes

Four action sheets provide Dash, Backstep, Victory and Ultimate poses for Fern, Ember, Luna and Bram. Each action has ten distinct PNGs. Source gutters and connected silhouettes define each crop, rather than equal grid cells.

The 160 output frames use 384x256 transparent canvases and a foot pivot at (192,24) measured from the bottom. A source-wide scale calibrates the neutral body to 200 pixels; no frame is independently stretched. Luna Ultimate uses a separate ten-pose replacement source and a 196.5-pixel neutral body so the entire aura fits. A few wide dust trails require a small horizontal placement adjustment to remain inside the canvas.

## Art specifications

- Four character action sheets: ten separate full-body poses per action, woodland pixel art, transparent background, consistent body scale, no text or borders.
- Luna Ultimate replacement: two rows of five complete moon-mage casting poses, contained blue effects, no effect-only cell.
- Four PVP themes: Forest timber and moss, Moonlit silver-blue stone, Ember volcanic rock, Skyward ivory ruins with cyan crystals. Each has a separate wide Base and smaller Ledge. Both are straight side views with flat walking surfaces.
- Jump covers: one upward leaping character per cover, transparent square composition and character-specific elemental trail. Bram uses stone gauntlets and a gem headband.

## Validation

`ROSTER-ART-v7.json` records 160 frames, dimensions, source references and hashes. Every action has ten distinct image hashes and every frame has at least four transparent pixels around its visible bounds.

`ARENA-ACTION-ART-v7.json` records eight PVP textures and four Jump covers. The platform top profile is mechanically aligned within 0.005 world units of +0.04 when displayed with the declared uniform width: 14 for bases and 4.2 for ledges. These are artwork checks; Unity collision and animation playback still need the project integration checks.
