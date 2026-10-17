# Controller and combat validation

Unity 6000.5.10f1 passed 265 Play Mode checks. PLAYMODE-VALIDATION-v5.json records each assertion. Earlier reports remain historical evidence.

The controller fixture uses two independent virtual Gamepads. It verifies assignment, simultaneous opposite movement, unplugged input release, replacement binding, punches, shoves, trigger guard, dash, jump and double jump, charge/release, pause/resume, help, arena cycling and rematch. Menu chords suppress their gameplay buttons. Holding a charge before unplug or menu entry verifies cancellation without an accidental smash.

Temporary DualShock HID and XInput layouts verify family names and the actual PlayStation/Xbox guide text. The fixture captures controller-help-1280-v5.png with those assigned devices. It removes virtual devices and re-enables previously enabled physical pads on success or failure. These checks do not validate physical USB/Bluetooth transport or two-person controller hardware.

Existing checks cover 160 character sprites, native Animator clips, 16 slash frames, three matching arena sets, damage, knockback, scoring, six edge-collision cases, guard/parry, charged smash, combos and reset. HUD references and health text fit are checked. All 13 sounds load, decode nonzero samples and start on eight pooled AudioSources, with the listener muted throughout testing.

Images ending in -v5.png are actual 1280x720 Play Mode renders. BUILD-v5.json records the successful Windows build and runtime script hashes. Physical keyboard play and subjective headphone balance remain unverified.

Unity logs an internal QuickSearch startup indexing exception separately from the completed game assertions. The first batch attempt was blocked because the project was already open. A separate test copy was started, then stopped after the main editor closed; the completed checks ran against the local project.

Woodland > Validate Editable Game tests the current scene. Batch execution uses -silent-test -executeMethod GameValidation.Run and rebuilds the demo first. Preserve custom scene edits before using that rebuilding entry point.
