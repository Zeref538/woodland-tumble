# Combat audio

Thirteen short Kenney OGG effects support the woodland arena. Original recordings are copied without editing. Imported effects are mono, preloaded and decompressed on load for short action latency. Unity normalizes on import; the suggested per-event gains below keep common movement quieter than combat.

## Sources and permission

Downloaded on 2026-10-16 from the official free pack links:

- [Impact Sounds](https://kenney.nl/assets/impact-sounds): [original ZIP](https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip)
- [RPG Audio](https://kenney.nl/assets/rpg-audio): [original ZIP](https://kenney.nl/media/pages/assets/rpg-audio/8e99002d76-1677590336/kenney_rpg-audio.zip)
- [Interface Sounds](https://kenney.nl/assets/interface-sounds): [original ZIP](https://kenney.nl/media/pages/assets/interface-sounds/fa43c1dd4d-1677589452/kenney_interface-sounds.zip)

All three official pages and included License.txt files identify [Creative Commons Zero (CC0)](https://creativecommons.org/publicdomain/zero/1.0/). Original licenses are retained in `Assets/Audio/Licenses/`. Credit: Kenney, https://kenney.nl. No paid or account-only audio is included.

## Event mapping

| File in Assets/Audio/Combat | Event | Duration | Bytes | Suggested gain | Original pack and filename |
|---|---|---:|---:|---:|---|
| `swing.ogg` | Attack wind-up cloth swish | 0.661 s | 16,508 | 0.38 | rpg: `cloth1.ogg` |
| `hit_light.ogg` | Normal punch contact | 0.431 s | 8,800 | 0.60 | impact: `impactPunch_medium_000.ogg` |
| `hit_heavy.ogg` | Combo finisher contact | 0.649 s | 11,617 | 0.70 | impact: `impactPunch_heavy_000.ogg` |
| `jump.ogg` | Ground jump cloth lift | 0.415 s | 11,787 | 0.28 | rpg: `cloth2.ogg` |
| `double_jump.ogg` | Air jump sparkle accent | 0.278 s | 6,103 | 0.38 | interface: `glass_001.ogg` |
| `dash.ogg` | Fast dodge swish | 0.600 s | 15,532 | 0.40 | rpg: `knifeSlice.ogg` |
| `land.ogg` | Soft plank landing | 0.250 s | 5,949 | 0.27 | impact: `footstep_wood_000.ogg` |
| `block.ogg` | Wood shield contact | 0.266 s | 6,343 | 0.48 | impact: `impactWood_light_000.ogg` |
| `parry.ogg` | Brief bright successful parry | 0.351 s | 7,651 | 0.55 | impact: `impactMetal_light_000.ogg` |
| `guard_break.ogg` | Shield break crack | 0.313 s | 6,233 | 0.68 | impact: `impactWood_heavy_000.ogg` |
| `knockout.ogg` | Low knockout thud | 0.505 s | 6,570 | 0.70 | impact: `impactSoft_heavy_000.ogg` |
| `victory.ogg` | Short match result cue | 0.539 s | 14,169 | 0.42 | interface: `confirmation_002.ogg` |
| `ui_click.ogg` | Menu selection | 0.100 s | 4,876 | 0.18 | interface: `click_001.ogg` |

Use subtle pitch variation for repeated swishes and hits, around 0.94 to 1.06. Keep the victory cue at its original pitch. Play a punch impact only on contact; a whiff plays only the swing. Block, parry and guard break replace the normal punch contact cue. Rate-limit land effects and do not play them every grounded frame. A double jump uses its sparkle cue instead of layering both jump sounds. A master SFX control should multiply these gains.

## Verification

All 13 files were checked for Ogg/Vorbis headers, consistent Ogg page boundaries, valid positive duration and exact byte equality with their official source. Durations use the final Ogg granule count divided by the Vorbis sample rate. This checks metadata and completeness, not decoded signal peaks. No listening check or gameplay loudness check was performed while selecting the files; the gains are starting values to verify in the running game.

The running-game fixture additionally decodes all 13 clips, checks nonzero waveform energy and confirms AudioSource playback. It verifies mute, volume clamping, keyboard M/minus/equals, invalid cues, pause and resume. The listener remains muted throughout testing. Subjective timbre and gameplay loudness still need a headphone check. Mute and volume persist between sessions.
