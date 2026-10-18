# Grounded combo update

Square, Square, Circle performs punch, punch, kick. Xbox uses X, X, B. Keyboard player one uses G, G, F; player two uses L, L, K.

Press the next attack during recovery. One pending input is remembered for 0.4 seconds. Confirmed hits keep the string open for 0.8 seconds; the third hit deals 1.4 times its normal damage and launches the rival. Early grounded hits use reduced knockback so the string connects without moving either fighter artificially. Misses and guards give no combo credit. A timed parry staggers the attacker.

Each character has six new hit-effect frames and six separate finisher frames. Fern uses leaves, Ember uses flames, Luna uses moon ice, and Bram uses stone fragments. Effects play through the existing 320-sprite pool, only after a hit is confirmed. Artwork loads once at startup from Resources/Art/Combo; it does not load from disk during a hit.

The silent Unity 6000.5.10f1 Play Mode run passed 28 checks. The natural punch-punch-kick string dealt 48.8 damage and spent 22 stamina. The test also covered whiffs, stamina guard, timed parry, combo expiry, all 48 loaded sprites and playback of eight character effect sequences. See MELEE-VALIDATION-v7.json and combo-effects-1280-v7.png. The effect preview deliberately displays all eight sequences together for inspection.

This targeted run checks the melee slice against the saved scene. The full v7 core fixture separately passed 693 checks, and the final bottom-corner HUD passed 164 checks. Older effect-preview captures show the HUD at the time of that run; the final HUD image is HUD-bottom-diamonds-1280-v7.png.
