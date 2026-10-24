# Rune Rumble

Use Unity 6000.5.10f1. The editable scene is Assets/Scenes/WoodlandTumble.unity. Existing scripts own gameplay, scene construction and validation.

Menu buttons are built in GameSetup, then bound in WoodlandHUD.Awake. UnityEvent.AddListener callbacks created while building an editor scene are not saved into that scene. The first v6 menu test caught this when the Local button did nothing after a scene reload. Keep runtime binding or use genuine persistent events when changing this flow.

GameSetup.Prepare rebuilds the scene. GameValidation.Run and GameValidation.RunSaved both test the saved scene. Pass -silent-test to keep speaker output muted. The native Windows build uses GameSetup.BuildWindows.

The current roster preparation tool uses Assets/Art/CharactersRework/Fern/Walk/01.png.meta as its import template. Keep this path current when cleaning legacy art. Offline art tools need checking as well as Unity scene dependencies.
