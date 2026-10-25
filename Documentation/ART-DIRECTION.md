# Shared art direction

The approved combination is Arcade Aura interface plus Ink & Motion effects. DESIGN.md governs the result; the two reference boards are in Documentation/Design-Samples.

Use midnight purple with vivid cyan, coral pink, amber and acid lime. Normal upright sans-serif lettering, bold headings and calm high-contrast label fields and confident illustrated bands define the interface. Attack effects use broad directional ink-brush sweeps, pale contact cores, teal or the fighter's own element color, and short controlled paint fragments. Keep the detailed existing fighters and preserve their proportions. Avoid wood ornaments, crystal shards, rune glyph decoration and glossy mobile-game bevels.

Every generation prompt must include these shared colors, shape rules, reference roles and exclusions, followed by the specific asset composition. Generate independent assets together. Keep source artwork, live labels and state feedback in separate layers. Inspect the actual Unity result, not just the generated images.

Import calibration: transparent brush panels are trimmed to their occupied canvas before use. Effect sheets use measured transparent gutters rather than assuming perfect generated grid alignment. Put each extracted frame on a common 352-pixel canvas, then uniformly downsample to 256 pixels. Do not independently enlarge each frame: that erases anticipation and decay. Use the original upright Rumble Display header face, Rumble Signal subheader and numeral face, and Rumble Read body face. Font outlines are maintained in Tools/create_rumble_fonts.py.

Selection refinement: preserve pixel art with deliberate visible pixel clusters, richer face and clothing detail, and a high-resolution selection-only breathing loop. Active player artwork stays on the left for both P1 and P2; roster sits on the right. Dedicated HUD circles contain close faces, not miniaturized bodies. Arena scenery and previews use a subdued night exposure so fighters and contact effects stay readable. Stage choices are tall angled picture panels.
