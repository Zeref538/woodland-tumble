# Tilt Rivals Balance

This variant starts directly in a local two-player Balance match. Fern and Ember play on the forest plank and sphere. There is no title menu, character selection, map selection or pause-navigation menu.

Use Unity **6000.5.10f1**. Open `Assets/Scenes/BalanceOnly.unity` and press Play. The original `WoodlandTumble.unity` scene retains the full game.

## Windows version

The portable Windows build is copied to `D:/TiltRivals-Balance`. Open `TiltRivalsBalance.exe` there. Keep the executable beside its Data folder and DLLs. Unity is not required. This executable does not run on macOS.

## Controls

Existing combat and controller controls remain: Up jumps and double-jumps, Cross/A guards, Square/X punches, Circle/B kicks, Triangle/Y charges the ranged skill, L1/R1 or LB/RB dash, and both triggers activate the low-health ultimate.

Keyboard player one uses A/D to move, W to jump, S to guard, G to punch, F to kick and H for ranged. Player two uses arrows, Up to jump, Down to guard, L to punch, K to kick and J for ranged. See `HANDOFF.md` for the remaining actions.

Esc or controller Options/Menu toggles pause with a plain PAUSED banner. R replays. The champion screen also accepts Cross/A to replay. F1 opens the existing move reference. Map-switch shortcuts are disabled in this variant.

## Rebuild

The existing `GameSetup.PrepareBalanceOnly` copies the saved full scene to `BalanceOnly.unity`, enables direct-start mode and removes its selection and pause menu objects. It never overwrites the full scene. Custom edits in the Balance scene will be replaced if you run the builder again.

`GameSetup.BuildBalanceWindows` builds only `BalanceOnly.unity` to `Build/BalanceWindows/TiltRivalsBalance.exe`. `GameValidation.RunBalanceOnly` validates the variant; automated runs use `-silent-test`.
