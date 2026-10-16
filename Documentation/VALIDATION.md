# Presentation and combat validation

Unity 6000.5.10f1 passed 228 Play Mode checks. PLAYMODE-VALIDATION-v4.json contains every assertion. Earlier reports remain as historical records.

The fixture exercises 160 character sprites and their native Animator clips, 16 elemental slash frames, three matching arena sets, damage, knockback, scoring, jump and landing, double jump, dash, frontal and rear guard, parry, guard break, charged smash, buffered combos and reset. Player two has an independent left-facing attack, Ember slash and dash check.

Six movement cases cover both flat edges, downhill edges at 22 degrees and inward pressure against either side face. Controlled placement isolates collision behavior; normal gameplay retains the dynamic hinged plank.

Presentation checks verify saved HUD references, health text fitting its rectangle, the F1 overlay and keyboard sound controls. Thirteen clips load, decode nonzero waveforms and start on eight pooled AudioSources. Invalid indices, volume clamping, mute, pause and resume are checked. All tests set AudioListener.volume to zero before playback; subjective sound balance is unverified.

Screenshots ending in -v4.png are actual 1280x720 Unity Play Mode renders. They are not concept images. The Windows build result and script hashes are in BUILD-v4.json. Physical-keyboard standalone play still needs a local check because earlier desktop-injected keys produced no observable game actions.

Unity logs an internal QuickSearch indexing exception at startup. It does not prevent the completed game assertions or Windows build.

The editor menu Woodland > Validate Editable Game tests the current editable scene. Batch execution uses -silent-test -executeMethod GameValidation.Run. That entry point rebuilds the demo scene before testing; use a clean copy when preserving custom scene edits.
