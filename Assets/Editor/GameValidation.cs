using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class GameValidation
{
    static Keyboard testKeyboard;
    static Key heldKey;
    static Gamepad[] testPads = new Gamepad[2];
    static GamepadState[] padStates = new GamepadState[2];
    static Gamepad[] disabledPads = new Gamepad[0];
    static InputSettings originalInputSettings, validationInputSettings;
    const string Active = "Woodland.ValidateV5";
    static GameValidation() { EditorApplication.update += Tick; }
    [Serializable] public class Report { public bool success; public string status; public string[] checks; }
    public static void InspectHUD()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/WoodlandTumble.unity");Canvas.ForceUpdateCanvases();
        var hud=UnityEngine.Object.FindAnyObjectByType<WoodlandHUD>();
        foreach(var text in hud.healthText)Debug.Log("HUD_METRICS: "+text.name+" text="+text.text+" preferred="+text.preferredWidth+"x"+text.preferredHeight+" rect="+text.rectTransform.rect.size+" font="+text.font.name+" size="+text.fontSize);
    }
    [MenuItem("Woodland/Validate Editable Game")]
    public static void RunInEditor() => Begin(false);
    public static void Run() { AudioListener.volume=0; GameSetup.Prepare(); Begin(true); }
    public static void RunSaved() { AudioListener.volume=0; Begin(true); }
    static void Begin(bool exit)
    {
        if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; EditorApplication.delayCall += () => Begin(exit); return; }
        EditorSceneManager.OpenScene("Assets/Scenes/WoodlandTumble.unity");
        SessionState.SetBool("Woodland.ValidationExit", exit);
        SessionState.SetString("Woodland.Checks", "");
        SessionState.SetBool(Active, true);
        SessionState.SetInt("Woodland.PushCount", 0);
        SessionState.SetFloat("Woodland.LastForce", 0);
        SetStage(0);
        EditorApplication.isPlaying = true;
    }
    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        SessionState.SetString("Woodland.Checks", SessionState.GetString("Woodland.Checks", "") + message + "\n");
    }
    static void SetStage(int stage) { SessionState.SetInt("Woodland.Stage", stage);SessionState.SetFloat("Woodland.StageTime", (float)EditorApplication.timeSinceStartup);SessionState.SetFloat("Woodland.StageGameTime", Time.time); }
    static float SimElapsed => Time.time - SessionState.GetFloat("Woodland.StageGameTime", 0);
    static float Elapsed => (float)EditorApplication.timeSinceStartup - SessionState.GetFloat("Woodland.StageTime", 0);
    static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying) return;
        try {
            if (Elapsed > 40) throw new Exception("Play Mode validation timed out.");
            var game = WoodlandGame.Instance;if (game == null) return;
            int stage = SessionState.GetInt("Woodland.Stage", 0);
            var a = game.players[0];var b = game.players[1];
            if (stage == 0 && game.playing && Time.timeSinceLevelLoad > 3) {
                Check(Camera.main != null && game.plank != null && game.hud != null, "Saved camera, platform and HUD references work");
                Check(a.transform.position.x < -2 && b.transform.position.x > 2, "Players spawn at separate editable spawn points");
                Check(a.transform.position.y > -2 && b.transform.position.y > -2, "Players land on the plank");
                foreach (var scout in game.players) foreach (string state in new[] { "Walk", "Jump", "Hurt", "Push", "Punch", "Knockout", "Block", "DoubleJump" }) {
                    var frames = scout.animations.Get(state);
                    Check(frames.Length == 10 && frames.All(f => f != null && f.rect.width == 256 && f.rect.height == 256), scout.displayName + "/" + state + ": ten independent 256px sprites");
                    Check(scout.animator.HasState(0, Animator.StringToHash("Base Layer." + state)), scout.displayName + "/" + state + ": editable Animator state exists");
                }
                foreach (var scout in game.players) foreach (string state in new[] { "Dash", "Charge", "Smash", "Combo2", "Combo3" }) {
                    var controller = scout.animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                    Check(controller != null && scout.animator.HasState(0, Animator.StringToHash("Base Layer." + state)), scout.displayName + "/" + state + ": real Animator state exists");
                    var node = controller.layers[0].stateMachine.states.FirstOrDefault(entry => entry.state.name == state).state;
                    var clip = node == null ? null : node.motion as AnimationClip;
                    Check(clip != null && AnimationUtility.GetObjectReferenceCurveBindings(clip).Any(binding => binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite" && AnimationUtility.GetObjectReferenceCurve(clip, binding).Any(key => key.value is Sprite)), scout.displayName + "/" + state + ": assigned clip contains real sprite keyframes");
                }
                Check(game.hud.healthFill.All(i => i.sprite != null && i.type == Image.Type.Filled), "HUD health fills and panel sprites are serialized");
                Check(game.hud.helpPanel != null && game.hud.damageGhost.Length == 2 && game.hud.damageGhost.All(image => image != null) && game.hud.fighterPlates.Length == 2 && game.hud.fighterPlates.All(plate => plate != null), "Presentation HUD has wired help panel, two damage ghosts and two fighter plates");
                Check(game.hud.deviceText != null && game.hud.deviceText.Length == 2 && game.hud.deviceText.All(text => text != null) && game.hud.controlsText != null && game.hud.controlsText.Length == 2 && game.hud.controlsText.All(text => text != null), "Both player device labels and help control texts are wired");
                foreach(var label in game.hud.healthText)Check(label.preferredHeight<=label.rectTransform.rect.height,"HP digits fit their text rectangle without clipping");
                Check(WoodlandEffects.Instance.fernSlash.Length==8 && WoodlandEffects.Instance.emberSlash.Length==8 && WoodlandEffects.Instance.fernSlash.All(frame=>frame!=null) && WoodlandEffects.Instance.emberSlash.All(frame=>frame!=null),"Both elemental attack animations contain eight loaded VFX frames");
                if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-silent-test")>=0)Check(AudioListener.volume==0,"Silent test mode keeps speaker output muted");
                ValidateAudio(game);
                Capture("Documentation/GamePreview-v5.png");
                var originalCollider = game.plank.GetComponent<BoxCollider2D>().size;
                var originalScale = game.plank.transform.localScale;
                for (int arena = 0; arena < 3; arena++) {
                    game.SelectArena(arena);
                    Check(game.ArenaIndex == arena && game.backgroundArt.sprite == game.backgrounds[arena] && game.plankArt.sprite == game.planks[arena] && game.sphereArt.sprite == game.spheres[arena], "Arena " + arena + ": matching background, plank and sphere switch together");
                    Check(game.plank.GetComponent<BoxCollider2D>().size == originalCollider && game.plank.transform.localScale == originalScale, "Arena " + arena + ": visual changes preserve collider proportions");
                    Capture("Documentation/arena-" + arena + "-1280-v5.png");
                }
                game.SelectArena(1);
                game.fallBoundary = -100;
                game.plank.bodyType = RigidbodyType2D.Kinematic;
                Arrange(game);
                Check(a.TryAttack(true), "Punch action starts");
                Check(a.AnimationState == "Punch", "Punch uses its own animation");
                SetStage(10);
            } else if (stage == 10 && SimElapsed > .25f) {
                Check(Mathf.Approximately(b.Health, b.maxHealth - a.punchDamage), "Punch applies reduced damage (" + a.punchDamage + ")");
                Check(b.Body.linearVelocity.x > a.punchKnockback && b.Body.linearVelocity.x < 6, "Punch applies moderated knockback");
                Check(WoodlandEffects.Instance.ActiveMotes > 0 && WoodlandEffects.Instance.ActiveMotes <= 320, "Pooled attack VFX are active within capacity");
                Capture("Documentation/VFXPreview-v5.png");
                a.ResetScout();b.ResetScout();Arrange(game);
                Check(a.TryAttack(false), "Push action starts");
                Check(a.AnimationState == "Push", "Push uses its own animation");
                SetStage(1);
            } else if (stage == 1 && SimElapsed > .25f) {
                int count = SessionState.GetInt("Woodland.PushCount", 0);
                Check(Mathf.Approximately(b.Health, Mathf.Max(0, b.maxHealth - (count + 1) * a.pushDamage)), "Push " + (count + 1) + " applies reduced damage (" + a.pushDamage + ")");
                float force = b.Body.linearVelocity.x;
                Check(force > SessionState.GetFloat("Woodland.LastForce", 0), "Push knockback grows as health drops (" + force.ToString("F2") + ")");
                SessionState.SetFloat("Woodland.LastForce", force);
                SessionState.SetInt("Woodland.PushCount", count + 1);
                if (b.Health <= 0) {
                    Check(b.KnockedOut && b.AnimationState == "Knockout" && !game.playing, "Health depletion triggers dedicated knockout state and stops controls");
                    SetStage(3);
                } else SetStage(2);
            } else if (stage == 2 && SimElapsed > 1.1f) {
                Arrange(game);
                Check(a.TryAttack(false), "Push recovers after cooldown");
                SetStage(1);
            } else if (stage == 3 && SimElapsed > .8f) {
                // Place the collapsed sprite on the board for a clear animation preview.
                b.Body.linearVelocity = Vector2.zero;b.Body.position = new Vector2(1.5f, .12f);b.transform.position = b.Body.position;
                Capture("Documentation/KnockoutPreview-v5.png");
                SetStage(4);
            } else if (stage == 4 && SimElapsed > .65f) {
                Check(game.score[0] == 1, "Knockout awards the opponent a round");
                game.StopAllCoroutines();game.playing = true;game.message = "";
                game.plank.GetComponent<HingeJoint2D>().enabled = false;
                game.plank.bodyType = RigidbodyType2D.Kinematic;
                originalInputSettings = InputSystem.settings;validationInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
                validationInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                validationInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings = validationInputSettings;
                testKeyboard = InputSystem.AddDevice<Keyboard>();testKeyboard.MakeCurrent();
                InputSystem.onBeforeUpdate += DriveTestInput;
                SessionState.SetInt("Woodland.EdgeCase", 0);
                StartEdgeCase(game, 0);
                SetStage(5);
            } else if (stage == 5 && SimElapsed > .95f) {
                int index = SessionState.GetInt("Woodland.EdgeCase", 0);
                float edgeY = SessionState.GetFloat("Woodland.EdgeY", 0);
                Check(Mathf.Abs(a.MovementInput) > .9f, "Edge case " + index + ": continuous input reaches the player controller");
                Check(a.Body.position.y < edgeY - 1.2f, "Edge case " + index + ": falls clear while input is held (position=" + a.Body.position + ", input=" + a.MovementInput + ")");
                Check(!a.Grounded, "Edge case " + index + ": side/corner is not ground");
                if (index < 5) { SessionState.SetInt("Woodland.EdgeCase", index + 1);StartEdgeCase(game, index + 1);SetStage(5); }
                else { StartJumpCase(game);SetStage(6); }
            } else if (stage == 6 && SimElapsed > .15f) {
                Check(a.Body.linearVelocity.y > 4 && a.Body.position.y > SessionState.GetFloat("Woodland.JumpY", 0) + .3f, "Jump still launches cleanly after ground-control changes");
                Check(WoodlandEffects.Instance.ActiveMotes > 0, "Jump and movement VFX emit successfully");
                SetStage(7);
            } else if (stage == 7 && SimElapsed > 1.2f) {
                Check(a.Grounded && Mathf.Abs(a.Body.linearVelocity.y) < .5f, "Player lands and settles on the plank");
                heldKey = Key.None;
                b.ResetScout();b.Body.simulated=true;b.Body.gravityScale=0;b.Body.position=new Vector2(2.2f,.15f);b.transform.position=b.Body.position;
                a.inputEnabled = false;
                Check(a.TryJump() && a.AirJumpsRemaining == 1, "Ground jump preserves the one aerial jump budget");
                Check(a.TryJump() && a.AirJumpsRemaining == 0, "Double jump consumes exactly one aerial jump");
                Check(!a.TryJump(), "Third jump is denied before landing");
                Check(a.AnimationState == "DoubleJump", "Aerial jump selects its dedicated animation");
                SetStage(20);
            } else if (stage == 20 && SimElapsed > .18f) {
                Check(WoodlandEffects.Instance.ActiveMotes > 0, "Double jump produces visible pooled effects");
                Capture("Documentation/combat-doublejump-1280-v5.png");
                a.ResetScout();b.ResetScout();Arrange(game);
                Check(a.TryDash(), "Dash starts when ready");
                Check(a.AnimationState == "Dash" && a.Body.linearVelocity.x >= a.dashSpeed * .99f, "Dash uses its animation and intended launch speed");
                Check(!a.TryDash() && !a.TryAttack(true), "Dash denies a second dash and attacks during its burst");
                SetStage(21);
            } else if (stage == 21 && SimElapsed > .22f) {
                Check(!a.TryDash() && a.DashReady < 1, "Dash remains unavailable after its burst until cooldown ends");
                SetStage(22);
            } else if (stage == 22 && SimElapsed > .9f) {
                Check(a.DashReady >= .999f && a.TryDash(), "Dash becomes available again after cooldown");
                PrepareGuard(game);
                SetStage(23);
            } else if (stage == 23 && SimElapsed > .22f) {
                Check(b.Blocking && b.Shield < b.shieldCapacity && b.Shield > 80, "Holding guard drains shield gradually");
                float shield = b.Shield;
                Check(!b.ReceiveHit(a, 1, true), "A frontal hit is absorbed after the parry window");
                Check(Mathf.Approximately(b.Health, b.maxHealth) && b.Shield < shield - a.punchDamage * 2, "Normal guard drains shield instead of health");
                Capture("Documentation/combat-guard-1280-v5.png");
                a.Body.position = b.Body.position + Vector2.right;
                a.transform.position = a.Body.position;Physics2D.SyncTransforms();
                Check(b.ReceiveHit(a, -1, true) && b.Health < b.maxHealth, "A rear hit bypasses the directional guard");
                PrepareGuard(game);
                Check(!b.ReceiveHit(a, 1, true), "A freshly raised guard parries a frontal hit");
                Check(a.Status == "STAGGERED" && !a.TryAttack(true) && !a.TryDash(), "Parry staggers the attacker and blocks immediate retaliation");
                Check(Mathf.Approximately(b.Health, b.maxHealth), "Parry preserves defender health");
                Capture("Documentation/combat-parry-1280-v5.png");
                SetStage(24);
            } else if (stage == 24 && SimElapsed > .45f) {
                Check(a.Status != "STAGGERED" && a.TryDash(), "Parry stagger expires and allows actions again");
                PrepareGuard(game);
                SetStage(25);
            } else if (stage == 25 && SimElapsed > .22f) {
                Check(!b.ReceiveHit(a, 1, true, 4), "A heavy frontal hit is initially absorbed by guard");
                Check(!b.Blocking && b.Shield == 0 && b.Status == "GUARD BROKEN", "Depleted guard enters its vulnerable recovery state");
                Check(!b.SetBlock(true) && !b.TryJump() && !b.TryDash() && !b.TryAttack(true) && !b.BeginSmash(), "Guard break denies defense, jump, dash and attacks during recovery");
                SetStage(26);
            } else if (stage == 26 && SimElapsed > .6f) {
                Check(b.Shield == 0 && !b.SetBlock(true), "Broken guard cannot instantly recharge or re-raise");
                SetStage(27);
            } else if (stage == 27 && SimElapsed > 1.5f) {
                Check(b.Status != "STAGGERED" && b.Shield > 0 && b.SetBlock(true), "Guard recovers only after its recovery delay");
                heldKey = Key.None;a.ResetScout();b.ResetScout();Arrange(game);
                Check(a.BeginSmash() && a.Charging && a.AnimationState == "Charge", "Smash enters its separate charge animation");
                Check(!a.TryAttack(true) && !a.TryDash(), "Charging prevents ordinary attacks and dashes");
                SetStage(28);
            } else if (stage == 28 && SimElapsed > .55f) {
                Check(a.Charge > .5f && a.ReleaseSmash(), "Holding smash builds charge before release");
                Check(!a.Charging && a.AnimationState == "Smash", "Release changes from charge to smash animation");
                SetStage(29);
            } else if (stage == 29 && SimElapsed > .2f) {
                Check(b.maxHealth - b.Health > a.punchDamage * 1.5f, "Charged smash deals substantially more damage than one punch");
                Capture("Documentation/combat-smash-1280-v5.png");
                a.ResetScout();b.ResetScout();
                foreach (var scout in game.players) {
                    Check(scout.Health == scout.maxHealth && scout.Shield == scout.shieldCapacity, scout.displayName + ": reset restores health and shield");
                    Check(!scout.Blocking && !scout.Charging && !scout.KnockedOut && scout.AirJumpsRemaining == 1 && scout.ComboCount == 0, scout.displayName + ": reset clears defense, charge, knockout, combo and aerial budget");
                    Check(scout.DashReady == 1 && scout.PunchReady == 1 && scout.PushReady == 1 && scout.AnimationState == "Idle", scout.displayName + ": reset clears cooldowns and restores idle");
                }
                Arrange(game);
                Check(a.TryAttack(true), "Combo opener starts with punch");
                SetStage(30);
            } else if (stage == 30 && SimElapsed > .25f) {
                Check(a.ComboCount == 1 && Mathf.Approximately(b.Health, b.maxHealth - a.punchDamage), "A confirmed opener starts the combo counter");
                Arrange(game);
                Check(!a.TryAttack(true), "A follow-up input buffers while the opening attack is recovering");
                SetStage(31);
            } else if (stage == 31 && SimElapsed > .5f) {
                Check(a.ComboCount == 2 && Mathf.Approximately(b.Health, b.maxHealth - 2 * a.punchDamage), "Buffered second hit connects before normal punch cooldown");
                Arrange(game);
                Check(!a.TryAttack(true), "Finisher input buffers during the second attack");
                SetStage(32);
            } else if (stage == 32 && SimElapsed > .5f) {
                Check(a.ComboCount == 3 && Mathf.Approximately(b.Health, b.maxHealth - 3.4f * a.punchDamage), "Three-hit combo connects with a stronger final hit");
                Capture("Documentation/combat-combo-1280-v5.png");
                SetStage(33);
            } else if (stage == 33 && SimElapsed > 1.1f) {
                Check(a.ComboCount == 0, "Combo counter expires when attacks stop connecting");
                a.ResetScout();b.ResetScout();
                SessionState.SetBool("Woodland.HelpWasVisible", game.hud.helpPanel.activeSelf);
                SessionState.SetBool("Woodland.WasPaused", game.Paused);
                heldKey = Key.F1;DriveTestInput();
                SetStage(34);
            } else if (stage == 34 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.HelpWasVisible", false), "F1 opens or closes the wired controls overlay");
                Check(game.Paused == SessionState.GetBool("Woodland.WasPaused", false), "Controls overlay does not change game pause state");
                Capture("Documentation/controls-1280-v5.png");
                heldKey = Key.None;DriveTestInput();SetStage(35);
            } else if (stage == 35 && SimElapsed > .1f) {
                heldKey = Key.F1;DriveTestInput();SetStage(36);
            } else if (stage == 36 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf == SessionState.GetBool("Woodland.HelpWasVisible", false), "A second F1 press restores the original overlay state");
                Check(game.Paused == SessionState.GetBool("Woodland.WasPaused", false), "Closing controls still leaves pause state unchanged");
                heldKey = Key.None;
                a.ResetScout();b.ResetScout();Arrange(game);
                Check(b.Facing < 0 && b.TryAttack(true), "Player two starts a left-facing punch");
                var effects = WoodlandEffects.Instance;
                SessionState.SetBool("Woodland.RestoreAudioKeys", true);
                SessionState.SetBool("Woodland.AudioKeysMuted", effects.Muted);
                SessionState.SetFloat("Woodland.AudioKeysVolume", effects.SfxVolume);
                effects.SetVolume(.5f, false);
                heldKey = Key.M;DriveTestInput();SetStage(37);
            } else if (stage == 37 && SimElapsed > .25f) {
                var effects = WoodlandEffects.Instance;
                Check(Mathf.Approximately(a.Health, a.maxHealth - b.punchDamage), "Player two left-facing punch damages player one");
                Check(effects.GetComponentsInChildren<SpriteRenderer>().Any(renderer => renderer.enabled && effects.emberSlash.Contains(renderer.sprite)), "Player two attack displays its own active Ember slash sprite");
                Capture("Documentation/combat-ember-1280-v5.png");
                Check(effects.Muted != SessionState.GetBool("Woodland.AudioKeysMuted", false), "Keyboard M toggles mute");
                heldKey = Key.Equals;DriveTestInput();SetStage(38);
            } else if (stage == 38 && SimElapsed > .1f) {
                Check(Mathf.Approximately(WoodlandEffects.Instance.SfxVolume, .6f), "Keyboard equals increases effect volume by one step");
                heldKey = Key.Minus;DriveTestInput();SetStage(39);
            } else if (stage == 39 && SimElapsed > .1f) {
                Check(Mathf.Approximately(WoodlandEffects.Instance.SfxVolume, .5f), "Keyboard minus decreases effect volume by one step");
                heldKey = Key.M;DriveTestInput();SetStage(40);
            } else if (stage == 40 && SimElapsed > .1f) {
                Check(WoodlandEffects.Instance.Muted == SessionState.GetBool("Woodland.AudioKeysMuted", false), "A second keyboard M press restores the original mute state");
                heldKey = Key.None;a.ResetScout();b.ResetScout();Arrange(game);
                Check(b.TryDash() && b.Body.linearVelocity.x <= -b.dashSpeed * .99f, "Player two dash launches left with its intended speed");
                BeginGamepadCases(game);SetStage(41);
            } else if (stage == 41 && SimElapsed > .12f) {
                Check(game.GetGamepad(0) == testPads[0] && game.GetGamepad(1) == testPads[1], "Two connected controllers occupy separate player slots");
                Check(a.MovementInput > .5f && Mathf.Abs(b.MovementInput) < .01f, "Player one stick moves only player one");
                padStates[0] = new GamepadState { leftStick = Vector2.right * .8f, rightTrigger = 1 };
                padStates[1] = new GamepadState().WithButton(GamepadButton.DpadLeft);SetStage(42);
            } else if (stage == 42 && SimElapsed > .12f) {
                Check(b.MovementInput < -.9f && a.MovementInput > .5f, "Two controllers simultaneously move their own players in opposite directions");
                Check(a.Charging, "Held player one right trigger starts a charge before disconnect");
                InputSystem.RemoveDevice(testPads[0]);testPads[0] = null;game.RefreshGamepads();SetStage(43);
            } else if (stage == 43 && SimElapsed > .12f) {
                Check(game.GetGamepad(0) == null && game.GetGamepad(1) == testPads[1], "Unplugging player one leaves player two in its original slot");
                Check(Mathf.Abs(a.MovementInput) < .01f && b.MovementInput < -.9f, "Disconnected controller releases movement while the other controller still works");
                Check(!a.Charging && a.AnimationState != "Smash" && b.Health == b.maxHealth, "Disconnecting a held trigger cancels charge without an accidental smash");
                testPads[0] = InputSystem.AddDevice<Gamepad>();game.RefreshGamepads();
                Check(game.GetGamepad(0) == testPads[0] && game.GetGamepad(1) == testPads[1], "A replacement controller fills the empty slot without stealing player two");
                PreparePadCombat(game);padStates[0] = new GamepadState().WithButton(GamepadButton.West);SetStage(44);
            } else if (stage == 44 && SimElapsed > .25f) {
                Check(Mathf.Approximately(b.Health, b.maxHealth - a.punchDamage), "Player one west face button punches player two");
                PreparePadCombat(game);padStates[1] = new GamepadState().WithButton(GamepadButton.East);SetStage(45);
            } else if (stage == 45 && SimElapsed > .25f) {
                Check(Mathf.Approximately(a.Health, a.maxHealth - b.pushDamage), "Player two east face button pushes player one");
                PreparePadCombat(game);padStates[1] = new GamepadState { leftTrigger = 1 };SetStage(46);
            } else if (stage == 46 && SimElapsed > .2f) {
                Check(b.Blocking && !a.Blocking && b.Shield < b.shieldCapacity, "Player two left trigger holds and drains its own guard");
                PreparePadCombat(game);b.Body.simulated = false;padStates[0] = new GamepadState().WithButton(GamepadButton.RightShoulder);SetStage(47);
            } else if (stage == 47 && SimElapsed > .08f) {
                Check(a.DashReady < 1 && a.Body.linearVelocity.x > 7, "Player one right shoulder launches a right-facing dash");
                PreparePadCombat(game);padStates[1] = new GamepadState().WithButton(GamepadButton.North);SetStage(48);
            } else if (stage == 48 && SimElapsed > .4f) {
                Check(b.Charging && b.Charge > .3f && !a.Charging, "Player two north face button charges its own smash");
                padStates[1] = new GamepadState();SetStage(49);
            } else if (stage == 49 && SimElapsed > .23f) {
                Check(!b.Charging && a.maxHealth - a.Health > b.punchDamage, "Releasing player two charge button executes a stronger left-facing smash");
                Capture("Documentation/controller-combat-1280-v5.png");
                PreparePadCombat(game);StartJumpCase(game, false);SetStage(50);
            } else if (stage == 50 && SimElapsed > .2f) {
                Check(a.Grounded, "Controller jump test begins on the walking surface");
                SessionState.SetFloat("Woodland.PadJumpY", a.Body.position.y);
                padStates[0] = new GamepadState().WithButton(GamepadButton.South);SetStage(51);
            } else if (stage == 51 && SimElapsed > .12f) {
                Check(a.Body.position.y > SessionState.GetFloat("Woodland.PadJumpY", 0) + .3f && a.AirJumpsRemaining == 1, "South face button jumps while preserving the extra aerial jump");
                padStates[0] = new GamepadState();SetStage(52);
            } else if (stage == 52 && SimElapsed > .06f) {
                padStates[0] = new GamepadState().WithButton(GamepadButton.South);SetStage(53);
            } else if (stage == 53 && SimElapsed > .1f) {
                Check(a.AirJumpsRemaining == 0 && a.AnimationState == "DoubleJump", "A second south press performs the dedicated aerial jump");
                PreparePadCombat(game);padStates[0] = new GamepadState().WithButton(GamepadButton.Start);SetStage(54);
            } else if (stage == 54 && Elapsed > .12f) {
                Check(game.Paused, "Controller start pauses the game");
                padStates[0] = new GamepadState();SetStage(55);
            } else if (stage == 55 && Elapsed > .1f) {
                padStates[0] = new GamepadState().WithButton(GamepadButton.Start);SetStage(56);
            } else if (stage == 56 && Elapsed > .12f) {
                Check(!game.Paused, "A second controller start press resumes the game");
                SessionState.SetBool("Woodland.PadHelp", game.hud.helpPanel.activeSelf);
                padStates[0] = new GamepadState { rightTrigger = 1 };SetStage(62);
            } else if (stage == 62 && SimElapsed > .2f) {
                Check(a.Charging, "Controller trigger begins charging before entering the menu chord");
                SessionState.SetFloat("Woodland.MenuChargeHealth", b.Health);
                padStates[0] = new GamepadState { rightTrigger = 1 }.WithButton(GamepadButton.Select);SetStage(63);
            } else if (stage == 63 && SimElapsed > .25f) {
                Check(!a.Charging && a.AnimationState != "Smash" && b.Health == SessionState.GetFloat("Woodland.MenuChargeHealth", 0), "Select cancels an existing held-trigger charge without releasing a smash");
                padStates[0] = new GamepadState().WithButton(GamepadButton.Select);SetStage(57);
            } else if (stage == 57 && SimElapsed > .12f) {
                padStates[0] = new GamepadState();SetStage(58);
            } else if (stage == 58 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.PadHelp", false), "Releasing controller select toggles the help overlay");
                Check(!game.Paused, "Controller help leaves gameplay unpaused");
                SessionState.SetInt("Woodland.PadArena", game.ArenaIndex);
                padStates[0] = new GamepadState().WithButton(GamepadButton.Select).WithButton(GamepadButton.RightShoulder);SetStage(59);
            } else if (stage == 59 && SimElapsed > .12f) {
                Check(game.ArenaIndex == (SessionState.GetInt("Woodland.PadArena", 0) + 1) % 3, "Select plus right shoulder cycles to the next arena");
                Check(a.DashReady == 1, "Arena shortcut suppresses the shoulder dash action");
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.PadHelp", false), "Arena shortcut does not also toggle help");
                padStates[0] = new GamepadState();SetStage(60);
            } else if (stage == 60 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.PadHelp", false), "Releasing the arena shortcut does not accidentally toggle help");
                game.score[0] = 2;game.score[1] = 1;
                padStates[0] = new GamepadState().WithButton(GamepadButton.Select).WithButton(GamepadButton.South);SetStage(61);
            } else if (stage == 61 && SimElapsed > .12f) {
                Check(game.score[0] == 0 && game.score[1] == 0 && !game.playing, "Select plus south restarts the match into its countdown");
                Check(a.AirJumpsRemaining == 1 && a.Body.linearVelocity == Vector2.zero, "Rematch shortcut does not also jump the player");
                game.hud.helpPanel.SetActive(SessionState.GetBool("Woodland.PadHelp", false));
                Complete(true, "PRESENTATION_AUDIO_COMBAT_CONTROLLERS_AND_EDGE_PHYSICS_VALIDATED");
            }
        } catch (Exception error) { Complete(false, error.ToString()); }
    }
    static void ValidateAudio(WoodlandGame game)
    {
        var effects = WoodlandEffects.Instance;
        Check(effects != null && effects.soundBank != null && effects.soundBank.Length == 13, "Thirteen action sound clips are wired into the effects bank");
        float volume = effects.SfxVolume;bool muted = effects.Muted;bool paused = game.Paused;
        try {
            if (paused) game.TogglePause();
            if (effects.Muted) effects.ToggleMute();
            effects.SetVolume(-1);Check(effects.SfxVolume == 0, "Sound volume clamps negative input to zero");
            Check(!effects.PlayCue(0), "Zero volume denies playback");
            effects.SetVolume(2);Check(effects.SfxVolume == 1, "Sound volume clamps excessive input to one");
            int sounds = effects.SoundsPlayed;
            for (int i = 0; i < effects.soundBank.Length; i++) {
                var clip = effects.soundBank[i];
                Check(clip != null && clip.loadState == AudioDataLoadState.Loaded && clip.length > .01f && clip.samples > 0 && clip.channels > 0, "Sound " + i + ": clip is loaded with positive duration and sample count");
                var samples = new float[Mathf.Min(clip.samples * clip.channels, 8192)];
                Check(clip.GetData(samples, 0) && samples.Sum(sample => (double)sample * sample) / samples.Length > 1e-10, "Sound " + i + ": decoded waveform contains audible data");
                Check(effects.PlayCue(i,.02f), "Sound " + i + ": playback accepts its valid cue");
            }
            Check(effects.SoundsPlayed == sounds + 13, "Accepted sound cues increment the actual playback counter thirteen times");
            var sources = effects.GetComponents<AudioSource>();
            Check(sources.Length == 8 && sources.Any(source => source.clip != null && source.isPlaying), "Eight pooled audio sources exist and at least one cue is playing");
            Check(!effects.PlayCue(-1) && !effects.PlayCue(13), "Invalid sound indices are rejected");
            effects.ToggleMute();sounds = effects.SoundsPlayed;
            Check(effects.Muted && !effects.PlayCue(0) && effects.SoundsPlayed == sounds, "Mute denies playback without advancing the sound counter");
            effects.ToggleMute();game.TogglePause();
            Check(!effects.PlayCue(0) && effects.SoundsPlayed == sounds, "Pause denies new sound cues");
            game.TogglePause();
            Check(effects.PlayCue(0) && effects.SoundsPlayed == sounds + 1, "Resuming permits sound playback again");
        } finally {
            if (game.Paused != paused) game.TogglePause();
            if (effects.Muted != muted) effects.ToggleMute();
            effects.SetVolume(volume,false);
        }
    }
    static void BeginGamepadCases(WoodlandGame game)
    {
        disabledPads = Gamepad.all.Where(pad => pad.enabled).ToArray();
        foreach (var pad in disabledPads) InputSystem.DisableDevice(pad);
        game.RefreshGamepads();
        var labelPads = new Gamepad[2];
        bool helpWasVisible = game.hud.helpPanel.activeSelf;
        try {
            string[] layouts = { "DualShock4GamepadHID", "XInputController" };
            for (int i = 0; i < layouts.Length; i++) {
                Check(InputSystem.ListLayouts().Contains(layouts[i]), layouts[i] + ": controller layout is available for prompt validation");
                labelPads[i] = InputSystem.AddDevice(layouts[i]) as Gamepad;
                Check(labelPads[i] != null && WoodlandGame.GamepadStyle(labelPads[i]) == (i == 0 ? "PLAYSTATION" : "XBOX"), layouts[i] + ": controller family maps to its expected button labels");
            }
            game.RefreshGamepads();game.hud.helpPanel.SetActive(true);game.hud.Refresh();
            Check(game.hud.deviceText[0].text.Contains("PLAYSTATION") && game.hud.deviceText[1].text.Contains("XBOX"), "Assigned PlayStation and Xbox layouts appear on their own player panels");
            Check(game.hud.controlsText[0].text.Contains("CROSS") && game.hud.controlsText[0].text.Contains("SQUARE") && game.hud.controlsText[1].text.Contains("A   Jump") && game.hud.controlsText[1].text.Contains("RB"), "Help displays real PlayStation and Xbox button names for the assigned virtual layouts");
            Capture("Documentation/controller-help-1280-v5.png");
        } finally {
            game.hud.helpPanel.SetActive(helpWasVisible);
            foreach (var pad in labelPads) if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            game.RefreshGamepads();
        }
        testPads[0] = InputSystem.AddDevice<Gamepad>();testPads[1] = InputSystem.AddDevice<Gamepad>();
        Check(WoodlandGame.GamepadStyle(testPads[0]) == "GAMEPAD", "Generic controller receives neutral button labels");
        game.RefreshGamepads();PreparePadCombat(game);
        padStates[0] = new GamepadState { leftStick = Vector2.right * .8f };
    }
    static void PreparePadCombat(WoodlandGame game)
    {
        heldKey = Key.None;padStates[0] = padStates[1] = new GamepadState();
        game.players[0].ResetScout();game.players[1].ResetScout();Arrange(game);
        foreach (var scout in game.players) scout.inputEnabled = true;
        DriveTestInput();
    }
    static void DriveTestInput()
    {
        if (testKeyboard != null) { testKeyboard.MakeCurrent();InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(heldKey)); }
        for (int i = 0; i < testPads.Length; i++) if (testPads[i] != null && testPads[i].added) InputSystem.QueueStateEvent(testPads[i], padStates[i]);
    }
    static void StartEdgeCase(WoodlandGame game, int index)
    {
        var scout = game.players[0];var plank = game.plank;
        scout.ResetScout();game.players[1].Body.simulated = false;
        scout.inputEnabled = true;scout.Body.gravityScale = 1;
        float direction = index % 2 == 0 ? 1 : -1;
        float angle = index == 2 ? -22 : index == 3 ? 22 : 0;
        plank.rotation = angle;plank.transform.rotation = Quaternion.Euler(0, 0, angle);
        plank.linearVelocity = Vector2.zero;plank.angularVelocity = 0;
        var box = plank.GetComponent<BoxCollider2D>();
        Vector2 corner = plank.transform.TransformPoint(box.offset + new Vector2(direction * box.size.x * .5f, box.size.y * .5f));
        bool pressSide = index >= 4;
        Vector2 point = corner + (pressSide ? new Vector2(direction * .25f, -.40f) : new Vector2(-direction * .35f, .68f));
        scout.transform.position = point;scout.Body.position = point;scout.Body.linearVelocity = Vector2.zero;scout.Body.simulated = true;
        SessionState.SetFloat("Woodland.EdgeY", corner.y);
        Key key = (pressSide ? -direction : direction) > 0 ? Key.D : Key.A;
        heldKey = key;DriveTestInput();Physics2D.SyncTransforms();
    }
    static void StartJumpCase(WoodlandGame game, bool driveKeyboardJump = true)
    {
        var scout = game.players[0];scout.ResetScout();scout.inputEnabled = true;
        game.plank.rotation = 0;game.plank.transform.rotation = Quaternion.identity;
        var box = game.plank.GetComponent<BoxCollider2D>();
        float top = game.plank.transform.TransformPoint(box.offset + Vector2.up * box.size.y * .5f).y + box.edgeRadius * game.plank.transform.lossyScale.x;
        Vector2 point = new Vector2(0, top + .625f);
        scout.transform.position = point;scout.Body.position = point;scout.Body.gravityScale = 1;scout.Body.linearVelocity = Vector2.zero;scout.Body.simulated = true;
        SessionState.SetFloat("Woodland.JumpY", point.y);
        heldKey = driveKeyboardJump ? Key.W : Key.None;DriveTestInput();Physics2D.SyncTransforms();
    }
    static void PrepareGuard(WoodlandGame game)
    {
        heldKey = Key.DownArrow;
        game.players[0].ResetScout();game.players[1].ResetScout();Arrange(game);
        game.players[1].inputEnabled = true;
        DriveTestInput();
        Check(game.players[1].SetBlock(true), "Player two can raise directional guard");
    }
    static void Arrange(WoodlandGame game)
    {
        for (int i = 0; i < 2; i++) {
            var scout = game.players[i];scout.inputEnabled = false;
            scout.Body.simulated = true;scout.Body.gravityScale = 0;
            var position = new Vector2(i == 0 ? -.7f : .7f, .15f);
            scout.transform.position = position;scout.Body.position = position;scout.Body.linearVelocity = Vector2.zero;
        }
        Physics2D.SyncTransforms();
    }
    static void Capture(string path)
    {
        WoodlandGame.Instance.hud.Refresh();
        var camera = Camera.main;
        var canvas = WoodlandGame.Instance.hud.GetComponent<Canvas>();
        var mode = canvas.renderMode;var previousCamera = canvas.worldCamera;float distance = canvas.planeDistance;int order=canvas.sortingOrder;
        var rt = new RenderTexture(1280, 720, 24);
        camera.targetTexture = rt;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;canvas.worldCamera = camera;canvas.planeDistance = 1;canvas.sortingOrder=1000;
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active = rt;
        var shot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);shot.Apply();File.WriteAllBytes(path, shot.EncodeToPNG());
        camera.targetTexture = null;RenderTexture.active = null;
        canvas.renderMode = mode;canvas.worldCamera = previousCamera;canvas.planeDistance = distance;canvas.sortingOrder=order;
        UnityEngine.Object.DestroyImmediate(shot);UnityEngine.Object.DestroyImmediate(rt);
    }
    static void Complete(bool success, string status)
    {
        SessionState.SetBool(Active, false);
        if (SessionState.GetBool("Woodland.RestoreAudioKeys", false) && WoodlandEffects.Instance != null) {
            var effects = WoodlandEffects.Instance;
            if (effects.Muted != SessionState.GetBool("Woodland.AudioKeysMuted", false)) effects.ToggleMute();
            effects.SetVolume(SessionState.GetFloat("Woodland.AudioKeysVolume", .5f), false);
            SessionState.SetBool("Woodland.RestoreAudioKeys", false);
        }
        InputSystem.onBeforeUpdate -= DriveTestInput;
        for (int i = 0; i < testPads.Length; i++) { if (testPads[i] != null && testPads[i].added) InputSystem.RemoveDevice(testPads[i]);testPads[i] = null;padStates[i] = new GamepadState(); }
        foreach (var pad in disabledPads) if (pad != null && pad.added) InputSystem.EnableDevice(pad);
        disabledPads = new Gamepad[0];
        if (testKeyboard != null) { InputSystem.RemoveDevice(testKeyboard);testKeyboard = null; }
        if (originalInputSettings != null) { InputSystem.settings = originalInputSettings;UnityEngine.Object.DestroyImmediate(validationInputSettings);originalInputSettings = null; }
        var report = new Report { success = success, status = status, checks = SessionState.GetString("Woodland.Checks", "").Split('\n').Where(s => s.Length > 0).ToArray() };
        File.WriteAllText("Documentation/PLAYMODE-VALIDATION-v5.json", JsonUtility.ToJson(report, true));
        if (success) Debug.Log(status);else Debug.LogError(status);
        EditorApplication.isPlaying = false;
        if (SessionState.GetBool("Woodland.ValidationExit", false)) EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
    }
}
