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
    const string Active = "Woodland.ValidateV6";
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
        SessionState.SetBool("Woodland.RosterCaptured",false);
        SetStage(-1);
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
            if (stage == -1 && Elapsed > .25f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title && !game.playing && !game.Paused, "Boot opens the title menu without starting or pausing a match");
                Check(!game.plank.simulated && game.players.All(scout => !scout.Body.simulated), "Title menu freezes the plank and both players");
                Check(game.hud.menuRoot && game.hud.titleMenu && game.hud.mapMenu && game.hud.matchHud && game.hud.startButton && game.hud.backButton && game.hud.modeText && game.hud.mapDescription, "Menu roots, navigation buttons and descriptive labels are wired");
                Check(game.hud.modeButtons != null && game.hud.modeButtons.Length == 4 && game.hud.modeButtons.All(button => button != null) && game.hud.mapButtons != null && game.hud.mapButtons.Length == 4 && game.hud.mapButtons.All(button => button != null), "All four main menu buttons and four map buttons are wired");
                game.hud.RefreshMenu();Capture("Documentation/main-menu-1280-v6.png");
                Check(game.hud.ruleButtons.Length == 2 && game.hud.ruleButtons.All(button => button != null) && game.hud.characterButtons.Length == 4 && game.hud.characterButtons.All(button => button != null) && game.hud.confirmCharacterButton && game.hud.ruleBackButton && game.hud.characterBackButton && game.hud.settingsBackButton, "Rules, four-character roster and settings navigation are wired");
                SelectMenuRoster(game, false, true, 0);
                game.hud.startButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Match && !game.SoloMode, "Map start button enters the selected local match");
                SetStage(0);
            } else if (stage == 0 && game.playing && Time.timeSinceLevelLoad > 3) {
                Check(Camera.main != null && game.plank != null && game.hud != null, "Saved camera, platform and HUD references work");
                Check(a.transform.position.x < -2 && b.transform.position.x > 2, "Players spawn at separate editable spawn points");
                Check(a.transform.position.y > -2 && b.transform.position.y > -2, "Players land on the plank");
                foreach (var scout in game.players) foreach (string state in new[] { "Walk", "Jump", "Hurt", "Push", "Punch", "Knockout", "Block", "DoubleJump", "Kick" }) {
                    var frames = scout.animations.Get(state);
                    Check(frames.Length == 10 && frames.All(f => f != null && f.rect.width == 256 && f.rect.height == 256), scout.displayName + "/" + state + ": ten independent 256px sprites");
                    Check(scout.animator.HasState(0, Animator.StringToHash("Base Layer." + state)), scout.displayName + "/" + state + ": editable Animator state exists");
                }
                foreach (var scout in game.players) foreach (string state in new[] { "Dash", "Charge", "Smash", "Combo2", "Combo3", "Backstep", "Grab" }) {
                    var controller = scout.animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                    Check(controller != null && scout.animator.HasState(0, Animator.StringToHash("Base Layer." + state)), scout.displayName + "/" + state + ": real Animator state exists");
                    var node = controller.layers[0].stateMachine.states.FirstOrDefault(entry => entry.state.name == state).state;
                    var clip = node == null ? null : node.motion as AnimationClip;
                    Check(clip != null && AnimationUtility.GetObjectReferenceCurveBindings(clip).Any(binding => binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite" && AnimationUtility.GetObjectReferenceCurve(clip, binding).Any(key => key.value is Sprite)), scout.displayName + "/" + state + ": assigned clip contains real sprite keyframes");
                }
                ValidateRosterAssets();
                Check(game.hud.healthFill.All(i => i.sprite != null && i.type == Image.Type.Filled), "HUD health fills and panel sprites are serialized");
                Check(game.hud.shieldFill.Length == 2 && game.hud.shieldFill.All(image => image != null && image.type == Image.Type.Filled), "Both real stamina meters have wired filled images");
                Check(game.hud.actionDiscs.Length == 8 && game.hud.actionDiscs.All(image => image != null) && game.hud.actionIcons.Length == 8 && game.hud.actionIcons.All(image => image != null) && game.hud.actionGlyphs.Length == 8 && game.hud.actionGlyphs.All(text => text != null) && game.hud.actionCovers.Length == 8 && game.hud.actionCovers.All(image => image != null && image.sprite != null), "Both four-button action diamonds have complete discs, controller badges and illustrated action covers");
                Check(game.hud.helpPanel != null && game.hud.damageGhost.Length == 2 && game.hud.damageGhost.All(image => image != null) && game.hud.fighterPlates.Length == 2 && game.hud.fighterPlates.All(plate => plate != null), "Presentation HUD has wired help panel, two damage ghosts and two fighter plates");
                Check(game.hud.deviceText != null && game.hud.deviceText.Length == 2 && game.hud.deviceText.All(text => text != null) && game.hud.controlsText != null && game.hud.controlsText.Length == 2 && game.hud.controlsText.All(text => text != null), "Both player device labels and help control texts are wired");
                ValidateHudCorners(game);
                ValidateHudMeters(game);
                ValidateCharacterActionCovers(game);
                foreach(var label in game.hud.healthText)Check(label.preferredHeight<=label.rectTransform.rect.height,"HP digits fit their text rectangle without clipping");
                Check(WoodlandEffects.Instance.fernSlash.Length==8 && WoodlandEffects.Instance.emberSlash.Length==8 && WoodlandEffects.Instance.fernSlash.All(frame=>frame!=null) && WoodlandEffects.Instance.emberSlash.All(frame=>frame!=null),"Both elemental attack animations contain eight loaded VFX frames");
                if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-silent-test")>=0)Check(AudioListener.volume==0,"Silent test mode keeps speaker output muted");
                ValidateAudio(game);
                Capture("Documentation/GamePreview-v6.png");
                var originalCollider = game.plank.GetComponent<BoxCollider2D>().size;
                var originalScale = game.plank.transform.localScale;
                for (int arena = 0; arena < 4; arena++) {
                    game.SelectArena(arena);
                    Check(game.ArenaIndex == arena && game.backgroundArt.sprite == game.backgrounds[arena] && game.plankArt.sprite == game.planks[arena] && game.sphereArt.sprite == game.spheres[arena], "Arena " + arena + ": matching background, plank and sphere switch together");
                    Check(game.plank.GetComponent<BoxCollider2D>().size == originalCollider && game.plank.transform.localScale == originalScale, "Arena " + arena + ": visual changes preserve collider proportions");
                    Capture("Documentation/arena-" + arena + "-1280-v6.png");
                }
                game.SelectArena(1);
                game.fallBoundary = -100;
                game.plank.bodyType = RigidbodyType2D.Kinematic;
                Arrange(game);
                Check(a.TryAttack(true), "Punch action starts");
                Check(Mathf.Approximately(a.Stamina, a.StaminaCapacity - 8), "Accepted punch spends eight real stamina");
                Check(a.AnimationState == "Punch", "Punch uses its own animation");
                SetStage(10);
            } else if (stage == 10 && SimElapsed > .25f) {
                Check(Mathf.Approximately(b.Health, b.maxHealth - a.punchDamage), "Punch applies reduced damage (" + a.punchDamage + ")");
                Check(b.Body.linearVelocity.x > a.punchKnockback && b.Body.linearVelocity.x < 6, "Punch applies moderated knockback");
                Check(WoodlandEffects.Instance.ActiveMotes > 0 && WoodlandEffects.Instance.ActiveMotes <= 320, "Pooled attack VFX are active within capacity");
                Capture("Documentation/VFXPreview-v6.png");
                a.ResetScout();b.ResetScout();Arrange(game);
                Check(a.TryAttack(false), "Kick action starts");
                Check(Mathf.Approximately(a.Stamina, a.StaminaCapacity - 6), "Accepted kick spends six real stamina");
                Check(a.AnimationState == "Kick", "Kick uses its own animation");
                SetStage(1);
            } else if (stage == 1 && SimElapsed > .25f) {
                int count = SessionState.GetInt("Woodland.PushCount", 0);
                Check(Mathf.Approximately(b.Health, Mathf.Max(0, b.maxHealth - (count + 1) * a.pushDamage)), "Kick " + (count + 1) + " applies reduced damage (" + a.pushDamage + ")");
                float force = b.Body.linearVelocity.x;
                Check(force > SessionState.GetFloat("Woodland.LastForce", 0), "Kick knockback grows as health drops (" + force.ToString("F2") + ")");
                SessionState.SetFloat("Woodland.LastForce", force);
                SessionState.SetInt("Woodland.PushCount", count + 1);
                if (b.Health <= 0) {
                    Check(b.KnockedOut && b.AnimationState == "Knockout" && !game.playing, "Health depletion triggers dedicated knockout state and stops controls");
                    SetStage(3);
                } else SetStage(2);
            } else if (stage == 2 && SimElapsed > 1.1f) {
                Arrange(game);
                Check(a.TryAttack(false), "Kick recovers after cooldown");
                SetStage(1);
            } else if (stage == 3 && SimElapsed > .8f) {
                // Place the collapsed sprite on the board for a clear animation preview.
                b.Body.linearVelocity = Vector2.zero;b.Body.position = new Vector2(1.5f, .12f);b.transform.position = b.Body.position;
                Capture("Documentation/KnockoutPreview-v6.png");
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
                float jumpStamina = a.Stamina;
                Check(a.TryJump() && a.AirJumpsRemaining == 1, "Ground jump preserves the one aerial jump budget");
                Check(Mathf.Approximately(a.Stamina, jumpStamina - 4), "Ground jump spends four stamina");
                Check(a.TryJump() && a.AirJumpsRemaining == 0, "Double jump consumes exactly one aerial jump");
                Check(Mathf.Approximately(a.Stamina, jumpStamina - 10), "Aerial jump spends another six stamina");
                Check(!a.TryJump(), "Third jump is denied before landing");
                Check(Mathf.Approximately(a.Stamina, jumpStamina - 10), "Denied third jump spends no additional stamina");
                Check(a.AnimationState == "DoubleJump", "Aerial jump selects its dedicated animation");
                SetStage(20);
            } else if (stage == 20 && SimElapsed > .18f) {
                Check(WoodlandEffects.Instance.ActiveMotes > 0, "Double jump produces visible pooled effects");
                Capture("Documentation/combat-doublejump-1280-v6.png");
                a.ResetScout();b.ResetScout();Arrange(game);
                Check(a.TryDash(), "Dash starts when ready");
                Check(Mathf.Approximately(a.Stamina, a.StaminaCapacity - 10), "Accepted dash spends ten stamina");
                Check(a.AnimationState == "Dash" && a.Body.linearVelocity.x >= a.dashSpeed * .99f, "Dash uses its animation and intended launch speed");
                Check(!a.TryDash() && !a.TryAttack(true), "Dash denies a second dash and attacks during its burst");
                Check(Mathf.Approximately(a.Stamina, a.StaminaCapacity - 10), "Denied actions during a dash spend no stamina");
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
                Capture("Documentation/combat-guard-1280-v6.png");
                a.Body.position = b.Body.position + Vector2.right;
                a.transform.position = a.Body.position;Physics2D.SyncTransforms();
                Check(b.ReceiveHit(a, -1, true) && b.Health < b.maxHealth, "A rear hit bypasses the directional guard");
                PrepareGuard(game);
                Check(!b.ReceiveHit(a, 1, true), "A freshly raised guard parries a frontal hit");
                Check(a.Status == "STAGGERED" && !a.TryAttack(true) && !a.TryDash(), "Parry staggers the attacker and blocks immediate retaliation");
                Check(Mathf.Approximately(b.Health, b.maxHealth), "Parry preserves defender health");
                Capture("Documentation/combat-parry-1280-v6.png");
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
                float smashStamina = a.Stamina;
                Check(a.Charge > .5f && a.ReleaseSmash(), "Holding smash builds charge before release");
                Check(Mathf.Approximately(a.Stamina, smashStamina - 14), "Releasing a charged special spends fourteen stamina");
                Check(!a.Charging && a.AnimationState == "Smash", "Release changes from charge to smash animation");
                SetStage(29);
            } else if (stage == 29 && SimElapsed > .2f) {
                Check(b.maxHealth - b.Health > a.punchDamage * 1.5f, "Charged smash deals substantially more damage than one punch");
                Capture("Documentation/combat-smash-1280-v6.png");
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
                Capture("Documentation/combat-combo-1280-v6.png");
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
                Capture("Documentation/controls-1280-v6.png");
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
                Capture("Documentation/combat-ember-1280-v6.png");
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
                padStates[0] = new GamepadState { leftStick = Vector2.right * .8f }.WithButton(GamepadButton.North);
                padStates[1] = new GamepadState().WithButton(GamepadButton.DpadLeft);SetStage(42);
            } else if (stage == 42 && SimElapsed > .12f) {
                Check(b.MovementInput < -.9f && a.MovementInput > .5f, "Two controllers simultaneously move their own players in opposite directions");
                Check(a.Charging, "Held player one north button starts a special charge before disconnect");
                InputSystem.RemoveDevice(testPads[0]);testPads[0] = null;game.RefreshGamepads();SetStage(43);
            } else if (stage == 43 && SimElapsed > .12f) {
                Check(game.GetGamepad(0) == null && game.GetGamepad(1) == testPads[1], "Unplugging player one leaves player two in its original slot");
                Check(Mathf.Abs(a.MovementInput) < .01f && b.MovementInput < -.9f, "Disconnected controller releases movement while the other controller still works");
                Check(!a.Charging && a.AnimationState != "Smash" && b.Health == b.maxHealth, "Disconnecting a held special button cancels charge without an accidental smash");
                testPads[0] = InputSystem.AddDevice<Gamepad>();game.RefreshGamepads();
                Check(game.GetGamepad(0) == testPads[0] && game.GetGamepad(1) == testPads[1], "A replacement controller fills the empty slot without stealing player two");
                PreparePadCombat(game);padStates[0] = new GamepadState().WithButton(GamepadButton.West);SetStage(44);
            } else if (stage == 44 && SimElapsed > .25f) {
                Check(Mathf.Approximately(b.Health, b.maxHealth - a.punchDamage), "Player one west face button punches player two");
                PreparePadCombat(game);padStates[1] = new GamepadState().WithButton(GamepadButton.East);SetStage(45);
            } else if (stage == 45 && SimElapsed > .25f) {
                Check(Mathf.Approximately(a.Health, a.maxHealth - b.pushDamage), "Player two east face button kicks player one");
                PreparePadCombat(game);padStates[1] = new GamepadState().WithButton(GamepadButton.South);SetStage(46);
            } else if (stage == 46 && SimElapsed > .2f) {
                Check(b.Blocking && !a.Blocking && b.Shield < b.shieldCapacity, "Player two south face button holds and drains its own guard");
                PreparePadCombat(game);b.Body.simulated = false;padStates[0] = new GamepadState().WithButton(GamepadButton.RightShoulder);SetStage(47);
            } else if (stage == 47 && SimElapsed > .08f) {
                Check(a.DashReady < 1 && a.Body.linearVelocity.x > 7, "Player one right shoulder launches a right-facing dash");
                PreparePadCombat(game);padStates[1] = new GamepadState().WithButton(GamepadButton.North);SetStage(48);
            } else if (stage == 48 && SimElapsed > .4f) {
                Check(b.Charging && b.Charge > .3f && !a.Charging, "Player two north face button charges its own smash");
                padStates[1] = new GamepadState();SetStage(49);
            } else if (stage == 49 && SimElapsed > .23f) {
                Check(!b.Charging && a.Health < a.maxHealth, "Releasing player two special button executes its first left-facing flame-rush contact");
                Capture("Documentation/controller-combat-1280-v6.png");
                PreparePadCombat(game);StartJumpCase(game, false);SetStage(50);
            } else if (stage == 50 && SimElapsed > .2f) {
                Check(a.Grounded, "Controller jump test begins on the walking surface");
                SessionState.SetFloat("Woodland.PadJumpY", a.Body.position.y);
                padStates[0] = new GamepadState().WithButton(GamepadButton.LeftShoulder);SetStage(51);
            } else if (stage == 51 && SimElapsed > .12f) {
                Check(a.Body.position.y > SessionState.GetFloat("Woodland.PadJumpY", 0) + .3f && a.AirJumpsRemaining == 1, "Left shoulder jumps while preserving the extra aerial jump");
                padStates[0] = new GamepadState();SetStage(52);
            } else if (stage == 52 && SimElapsed > .06f) {
                padStates[0] = new GamepadState().WithButton(GamepadButton.LeftShoulder);SetStage(53);
            } else if (stage == 53 && SimElapsed > .1f) {
                Check(a.AirJumpsRemaining == 0 && a.AnimationState == "DoubleJump", "A second left shoulder press performs the dedicated aerial jump");
                PreparePadCombat(game);padStates[0] = new GamepadState().WithButton(GamepadButton.Start);SetStage(54);
            } else if (stage == 54 && Elapsed > .12f) {
                Check(game.Paused, "Controller start pauses the game");
                padStates[0] = new GamepadState();SetStage(55);
            } else if (stage == 55 && Elapsed > .1f) {
                padStates[0] = new GamepadState().WithButton(GamepadButton.Start);SetStage(56);
            } else if (stage == 56 && Elapsed > .12f) {
                Check(!game.Paused, "A second controller start press resumes the game");
                SessionState.SetBool("Woodland.PadHelp", game.hud.helpPanel.activeSelf);
                padStates[0] = new GamepadState().WithButton(GamepadButton.North);SetStage(62);
            } else if (stage == 62 && SimElapsed > .2f) {
                Check(a.Charging, "Controller north special button begins charging before entering the menu chord");
                SessionState.SetFloat("Woodland.MenuChargeHealth", b.Health);
                padStates[0] = new GamepadState().WithButton(GamepadButton.North).WithButton(GamepadButton.Select);SetStage(63);
            } else if (stage == 63 && SimElapsed > .25f) {
                Check(!a.Charging && a.AnimationState != "Smash" && b.Health == SessionState.GetFloat("Woodland.MenuChargeHealth", 0), "Select cancels an existing held-special charge without releasing a smash");
                padStates[0] = new GamepadState().WithButton(GamepadButton.Select);SetStage(57);
            } else if (stage == 57 && SimElapsed > .12f) {
                padStates[0] = new GamepadState();SetStage(58);
            } else if (stage == 58 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.PadHelp", false), "Releasing controller select toggles the help overlay");
                Check(!game.Paused, "Controller help leaves gameplay unpaused");
                SessionState.SetInt("Woodland.PadArena", game.ArenaIndex);
                padStates[0] = new GamepadState().WithButton(GamepadButton.Select).WithButton(GamepadButton.RightShoulder);SetStage(59);
            } else if (stage == 59 && SimElapsed > .12f) {
                Check(game.ArenaIndex == (SessionState.GetInt("Woodland.PadArena", 0) + 1) % 4, "Select plus right shoulder cycles to the next arena");
                Check(a.DashReady == 1, "Arena shortcut suppresses the shoulder dash action");
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.PadHelp", false), "Arena shortcut does not also toggle help");
                padStates[0] = new GamepadState();SetStage(60);
            } else if (stage == 60 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.PadHelp", false), "Releasing the arena shortcut does not accidentally toggle help");
                game.score[0] = 2;game.score[1] = 1;
                padStates[0] = new GamepadState().WithButton(GamepadButton.Select).WithButton(GamepadButton.South);SetStage(61);
            } else if (stage == 61 && SimElapsed > .12f) {
                Check(game.score[0] == 0 && game.score[1] == 0 && !game.playing, "Select plus south restarts the match into its countdown");
                Check(a.AirJumpsRemaining == 1 && !a.Blocking && a.Body.linearVelocity == Vector2.zero, "Rematch shortcut does not also block or move the player");
                game.hud.helpPanel.SetActive(SessionState.GetBool("Woodland.PadHelp", false));
                heldKey = Key.None;padStates[0] = padStates[1] = new GamepadState();
                game.ReturnToMenu();SetStage(70);
            } else if (stage == 70 && SimElapsed > .15f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title && !game.playing && !game.Paused && !game.plank.simulated && game.players.All(scout => !scout.Body.simulated), "Returning to title freezes gameplay and clears pause");
                Check(!a.TryAttack(true) && !a.TryJump() && !a.TryDash() && !a.BeginSmash(), "Title menu denies combat and movement actions");
                SelectMenuRoster(game, true, true, 0);
                Check(game.SoloMode && game.Screen == WoodlandGame.MenuScreen.Maps && !game.playing, "Solo rules and roster selection reach map selection without starting gameplay");
                for (int arena = 0; arena < 4; arena++) {
                    game.hud.mapButtons[arena].onClick.Invoke();
                    Check(game.PendingArena == arena && game.ArenaIndex == arena && !game.playing && !game.plank.simulated, "Map button " + arena + ": previews the matching arena without starting physics");
                }
                game.hud.backButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Characters && !game.playing, "Map Back button returns to character selection without starting a match");
                game.hud.characterBackButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Rules, "Character Back button returns to rule selection");
                game.hud.ruleBackButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Title, "Rules Back button returns to title");
                SelectMenuRoster(game, true, true, 2);
                game.hud.RefreshMenu();Capture("Documentation/map-menu-1280-v6.png");
                game.hud.startButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Match && game.SoloMode && game.BalanceMode && game.PendingArena == 2 && game.ArenaIndex == 2, "Start button preserves the chosen solo balance arena into the match");
                SetStage(71);
            } else if (stage == 71 && game.playing && SimElapsed > 2) {
                Check(!a.aiControlled && b.aiControlled, "Solo mode enables AI only for player two");
                PrepareAiCase(game, 4);
                SessionState.SetInt("Woodland.AIDecisions", b.AIDecisions);
                SessionState.SetFloat("Woodland.AIStartX", b.Body.position.x);
                padStates[1] = new GamepadState { leftStick = Vector2.right };
                SetStage(72);
            } else if (stage == 72 && SimElapsed > .65f) {
                Check(b.AIDecisions > SessionState.GetInt("Woodland.AIDecisions", 0), "Solo rival makes autonomous decisions without attack inputs");
                Check(Mathf.Abs(a.MovementInput) < .01f && b.Body.position.x < SessionState.GetFloat("Woodland.AIStartX", 0) - .2f, "Solo rival approaches the opponent instead of following its assigned pad's opposing stick");
                padStates[1] = new GamepadState();PrepareAiCase(game, 1.25f);
                SessionState.SetInt("Woodland.AIActions", b.AIActionRequests);SetStage(73);
            } else if (stage == 73 && SimElapsed > 2.2f) {
                Check(b.AIActionRequests > SessionState.GetInt("Woodland.AIActions", 0) && a.Health < a.maxHealth, "Autonomous rival performs real combat actions and damages the nearby opponent");
                Capture("Documentation/solo-combat-1280-v6.png");
                SessionState.SetInt("Woodland.AIDecisions", b.AIDecisions);
                SessionState.SetInt("Woodland.AIActions", b.AIActionRequests);
                SessionState.SetFloat("Woodland.AIFrozenX", b.Body.position.x);
                SessionState.SetFloat("Woodland.AIFrozenY", b.Body.position.y);
                game.TogglePause();SetStage(74);
            } else if (stage == 74 && Elapsed > .3f) {
                Check(game.Paused && b.AIDecisions == SessionState.GetInt("Woodland.AIDecisions", 0) && b.AIActionRequests == SessionState.GetInt("Woodland.AIActions", 0), "Pause freezes autonomous decisions and action counts");
                Check(Vector2.Distance(b.Body.position, new Vector2(SessionState.GetFloat("Woodland.AIFrozenX", 0), SessionState.GetFloat("Woodland.AIFrozenY", 0))) < .001f, "Pause freezes the solo rival's physical position");
                game.ReturnToMenu();
                SessionState.SetInt("Woodland.AIDecisions", b.AIDecisions);SessionState.SetInt("Woodland.AIActions", b.AIActionRequests);SetStage(75);
            } else if (stage == 75 && Elapsed > .3f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title && !game.Paused && !b.Body.simulated && b.AIDecisions == SessionState.GetInt("Woodland.AIDecisions", 0) && b.AIActionRequests == SessionState.GetInt("Woodland.AIActions", 0), "Returning from paused solo gameplay freezes AI and restores title navigation");
                heldKey = Key.RightArrow;DriveTestInput();SetStage(77);
            } else if (stage == 77 && SimElapsed > .12f) {
                heldKey = Key.Enter;DriveTestInput();SetStage(78);
            } else if (stage == 78 && SimElapsed > .12f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Rules && !game.SoloMode && !game.playing, "Keyboard direction and Enter choose local mode and open rules");
                heldKey = Key.Escape;DriveTestInput();SetStage(79);
            } else if (stage == 79 && SimElapsed > .12f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title, "Keyboard Escape backs out of rule selection");
                heldKey = Key.None;padStates[0] = new GamepadState().WithButton(GamepadButton.DpadDown);SetStage(80);
            } else if (stage == 80 && SimElapsed > .12f) {
                padStates[0] = new GamepadState().WithButton(GamepadButton.South);SetStage(81);
            } else if (stage == 81 && SimElapsed > .12f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Rules && !game.SoloMode && !game.playing, "Controller directional pad and south button open local rule selection");
                padStates[0] = new GamepadState().WithButton(GamepadButton.East);SetStage(82);
            } else if (stage == 82 && SimElapsed > .12f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title, "Controller east button backs out of rule selection");
                padStates[0] = new GamepadState();SelectMenuRoster(game, false, true, 1);
                heldKey = Key.RightArrow;DriveTestInput();SetStage(83);
            } else if (stage == 83 && SimElapsed > .12f) {
                Check(game.PendingArena == 2 && !game.playing, "Keyboard map navigation previews the next of four maps without starting");
                heldKey=Key.None;padStates[0]=new GamepadState().WithButton(GamepadButton.DpadRight);SetStage(84);
            } else if (stage == 84 && SimElapsed > .12f) {
                Check(game.PendingArena == 3 && !game.playing, "Controller map navigation reaches the fourth map without starting");
                padStates[0]=new GamepadState();game.hud.mapButtons[1].onClick.Invoke();game.hud.startButton.onClick.Invoke();SetStage(76);
            } else if (stage == 76 && game.playing && SimElapsed > 2) {
                Check(!game.SoloMode && !a.aiControlled && !b.aiControlled && game.ArenaIndex == 1, "Starting local mode after solo restores two human-controlled players on the chosen map");
                PreparePadCombat(game);
                SessionState.SetFloat("Woodland.TestCapacity", a.shieldCapacity);a.shieldCapacity = 8;a.ResetScout();Arrange(game);
                Check(a.TryAttack(true) && a.Stamina == 0, "An accepted punch can exhaust a small test stamina pool exactly");SetStage(90);
            } else if (stage == 90 && SimElapsed > .55f) {
                Check(a.Stamina == 0, "Spent stamina stays empty during its regeneration delay");
                Check(!a.TryAttack(false) && !a.TryJump() && !a.TryDash() && !a.TryBackstep() && !a.TryGrab() && !a.BeginSmash(), "Exhausted stamina denies kick, jump, dash, backstep, grab and special");
                Check(a.Stamina == 0, "Rejected exhausted actions do not create or spend stamina");SetStage(91);
            } else if (stage == 91 && SimElapsed > .2f) {
                Check(a.Stamina > 0 && a.Stamina <= 8, "Stamina begins regenerating after the spend delay and stays within capacity");
                a.shieldCapacity = SessionState.GetFloat("Woodland.TestCapacity", 100);PreparePadCombat(game);
                Check(a.TryBackstep() && a.Body.linearVelocity.x <= -6.9f && a.Stamina == a.StaminaCapacity - 12, "Backstep moves opposite facing and spends twelve stamina");
                float energy = a.Stamina;
                Check(!a.TryBackstep() && a.Stamina == energy, "Backstep cooldown rejection spends no additional stamina");
                Check(!a.TryDash() && a.Stamina == energy, "A forward dash cannot interrupt backstep or spend stamina during its burst");
                Check(!a.ReceiveHit(b, -1, true) && a.Health == a.maxHealth, "Backstep invulnerability rejects an immediate incoming hit");SetStage(92);
            } else if (stage == 92 && SimElapsed > .24f) {
                Check(a.ReceiveHit(b, -1, true) && a.Health < a.maxHealth, "Backstep invulnerability expires after its short evade window");SetStage(93);
            } else if (stage == 93 && SimElapsed > .8f) {
                Check(a.BackstepReady >= .999f && a.TryBackstep(), "Backstep becomes available again after its cooldown");
                PreparePadCombat(game);
                for (int i = 0; i < 2; i++) { var scout=game.players[i];scout.Body.position=new Vector2(i==0?-.45f:.45f,.15f);scout.transform.position=scout.Body.position; }
                Physics2D.SyncTransforms();padStates[1] = new GamepadState().WithButton(GamepadButton.South);SetStage(94);
            } else if (stage == 94 && SimElapsed > .2f) {
                Check(b.Blocking, "Grab test defender holds the south-button guard");
                padStates[0] = new GamepadState { rightTrigger = 1 };SetStage(95);
            } else if (stage == 95 && SimElapsed > .3f) {
                Check(Mathf.Approximately(a.Stamina, a.StaminaCapacity - 18) && a.GrabReady < 1, "Controller right trigger starts grab and spends eighteen stamina");
                Check(Mathf.Approximately(b.Health, b.maxHealth - a.pushDamage * .5f), "Close grab bypasses the held guard and damages the defender");
                Capture("Documentation/grab-toss-1280-v6.png");
                PreparePadCombat(game);a.Body.position=new Vector2(-2,.15f);b.Body.position=new Vector2(2,.15f);a.transform.position=a.Body.position;b.transform.position=b.Body.position;Physics2D.SyncTransforms();
                Check(a.TryGrab(), "Grab can start outside contact range");SetStage(96);
            } else if (stage == 96 && SimElapsed > .3f) {
                Check(b.Health == b.maxHealth && a.Stamina == a.StaminaCapacity - 18, "A missed out-of-range grab still costs stamina but deals no damage");
                float energy=a.Stamina;Check(!a.TryGrab() && a.Stamina == energy, "Grab recovery rejection spends no additional stamina");
                Capture("Documentation/health-stamina-1280-v6.png");
                PreparePadCombat(game);b.Body.simulated=false;
                SessionState.SetFloat("Woodland.BackstepStartX",a.Body.position.x);
                padStates[0]=new GamepadState { leftTrigger=1 };SetStage(97);
            } else if (stage == 97 && SimElapsed > .08f) {
                string diagnostic="elapsed="+SimElapsed.ToString("F3")+", evadeAge="+(.9f*a.BackstepReady).ToString("F3")+", ready="+a.BackstepReady.ToString("F3")+", stamina="+a.Stamina.ToString("F3")+"/"+a.StaminaCapacity+", velocity="+a.Body.linearVelocity+", displacement="+(a.Body.position.x-SessionState.GetFloat("Woodland.BackstepStartX",0)).ToString("F3")+", facing="+a.Facing+", grounded="+a.Grounded+", state="+a.AnimationState+", inputEnabled="+a.inputEnabled+", trigger="+testPads[0].leftTrigger.ReadValue()+", plank="+game.plank.bodyType+", angle="+game.plank.rotation.ToString("F2");
                Debug.Log("CONTROLLER_BACKSTEP_DIAGNOSTIC: "+diagnostic);
                Check(a.BackstepReady < 1,"Controller left trigger starts backstep cooldown ("+diagnostic+")");
                Check(a.Stamina == a.StaminaCapacity - 12,"Controller left trigger spends exactly twelve stamina ("+diagnostic+")");
                Check(a.Body.linearVelocity.x < -6,"Controller left trigger launches backward at its intended speed ("+diagnostic+")");
                padStates[0]=new GamepadState();
                Check(game.hud.pauseButtons.Length == 4 && game.hud.pauseButtons.All(button => button != null), "All four pause menu buttons are wired");
                game.TogglePause();game.hud.RefreshMenu();
                Check(game.hud.pauseMenu.activeSelf, "Pausing displays the interactive pause menu");
                game.hud.pauseButtons[0].onClick.Invoke();
                Check(!game.Paused && game.Screen == WoodlandGame.MenuScreen.Match, "Pause Resume button restores gameplay");
                game.TogglePause();game.hud.pauseButtons[2].onClick.Invoke();
                Check(!game.Paused && !game.playing && game.Screen != WoodlandGame.MenuScreen.Match, "Pause Change Game button safely leaves combat for configuration");
                game.ReturnToMenu();game.hud.modeButtons[2].onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Settings && !game.playing, "Settings main menu button opens settings without starting gameplay");
                ValidateSettings(game);game.hud.settingsBackButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Title, "Settings Back button returns to title");
                SelectMenuRoster(game, false, false, 3);game.hud.startButton.onClick.Invoke();SetStage(100);
            } else if (stage == 100 && game.playing && SimElapsed > 2) {
                Check(!game.BalanceMode && game.ArenaIndex == 3 && game.arenaPlatforms.Length == 3 && game.arenaPlatforms.All(platform => platform != null && platform.gameObject.activeInHierarchy), "Arena PvP starts the fourth map with all three fixed platforms active");
                Check(game.plank.bodyType == RigidbodyType2D.Kinematic && !game.plank.GetComponent<HingeJoint2D>().enabled && !game.sphereArt.gameObject.activeInHierarchy, "Arena PvP disables the balancing hinge and hides its central ball");
                Check(game.arenaPlatforms.All(platform => game.IsGroundBody(platform)), "All Arena PvP platforms are accepted by player ground detection");
                var platform = game.arenaPlatforms[0];var box = platform.GetComponent<BoxCollider2D>();
                Check(box != null, "Arena PvP base platform has an editable walking collider");
                a.ResetScout();b.ResetScout();b.Body.simulated=false;a.inputEnabled=false;
                float top=platform.transform.TransformPoint(box.offset+Vector2.up*box.size.y*.5f).y;
                Vector2 point=new Vector2(platform.position.x,top+.625f);a.Body.position=point;a.transform.position=point;a.Body.gravityScale=1;a.Body.simulated=true;Physics2D.SyncTransforms();SetStage(101);
            } else if (stage == 101 && SimElapsed > .25f) {
                Check(a.Grounded, "Player settles on the Arena PvP platform as valid ground");
                SessionState.SetFloat("Woodland.PvpJumpY",a.Body.position.y);
                Check(a.TryJump() && a.AirJumpsRemaining==1, "Arena PvP ground jump preserves its aerial jump budget");SetStage(102);
            } else if (stage == 102 && SimElapsed > .12f) {
                Check(a.Body.position.y>SessionState.GetFloat("Woodland.PvpJumpY",0)+.3f, "Arena PvP ground jump launches above its fixed platform");
                Capture("Documentation/arena-pvp-1280-v6.png");
                PrepareSpecialCase(game,0,1.2f);
                Check(a.SkillName=="VINE BIND" && a.BeginSmash() && a.ReleaseSmash(),"Fern starts its own vine-binding special");SetStage(103);
            } else if (stage == 103 && SimElapsed > .2f) {
                Check(b.Health<b.maxHealth && b.Rooted && b.Slowed,"Fern contact deals damage and applies both root and slow");
                float stamina=b.Stamina;Check(!b.TryDash() && b.Stamina==stamina,"A vine-bound rival cannot dash or spend stamina on the denied dash");
                Capture("Documentation/special-fern-1280-v6.png");
                PrepareSpecialCase(game,1,1.2f);
                Check(a.SkillName=="FLAME RUSH" && a.BeginSmash() && a.ReleaseSmash(),"Ember starts its own two-contact flame rush");SetStage(104);
            } else if (stage == 104 && SimElapsed > .2f) {
                Check(Mathf.Approximately(b.Health,b.maxHealth-a.punchDamage*.65f),"Ember flame rush applies its first independent contact");
                PositionSpecialPair(game,1.2f,-.2f);SetStage(105);
            } else if (stage == 105 && SimElapsed > .3f) {
                Check(Mathf.Approximately(b.Health,b.maxHealth-a.punchDamage*1.3f),"Ember flame rush connects a second hit after normal hit invulnerability expires");
                Capture("Documentation/special-ember-1280-v6.png");
                PrepareSpecialCase(game,2,3.4f);
                Check(a.SkillName=="FROST WAVE" && a.BeginSmash() && a.ReleaseSmash(),"Luna starts its own travelling frost wave");SetStage(106);
            } else if (stage == 106 && SimElapsed > .65f) {
                Check(Mathf.Approximately(b.Health,b.maxHealth-a.punchDamage*.85f) && b.Slowed && !b.Rooted,"Luna wave reaches beyond melee range, hits once and slows without rooting");
                Capture("Documentation/special-luna-1280-v6.png");
                PrepareSpecialCase(game,3,1.2f);Check(!a.BeginSmash(),"Bram cannot start its ground quake while airborne");
                var platform=game.arenaPlatforms[0];var box=platform.GetComponent<BoxCollider2D>();float top=platform.transform.TransformPoint(box.offset+Vector2.up*box.size.y*.5f).y;
                a.Body.position=new Vector2(platform.position.x,top+.625f);b.Body.position=new Vector2(platform.position.x-1.2f,top+.625f);
                foreach(var scout in game.players){scout.transform.position=scout.Body.position;scout.Body.gravityScale=1;scout.Body.linearVelocity=Vector2.zero;}
                Physics2D.SyncTransforms();SetStage(107);
            } else if (stage == 107 && SimElapsed > .25f) {
                Check(a.Grounded && a.Facing>0 && b.Body.position.x<a.Body.position.x,"Bram quake test is grounded with the rival behind its facing direction");
                Check(a.SkillName=="STONE QUAKE" && a.BeginSmash() && a.ReleaseSmash(),"Grounded Bram starts its radial stone quake");SetStage(108);
            } else if (stage == 108 && SimElapsed > .2f) {
                Check(Mathf.Approximately(b.Health,b.maxHealth-a.punchDamage) && b.Body.linearVelocity.y>4,"Bram quake damages a rival behind him and adds its distinct upward launch");
                Capture("Documentation/special-bram-1280-v6.png");
                Complete(true, "FOUR_ROSTER_FOUR_MAPS_RULES_SETTINGS_SOLO_AI_COMBAT_AND_CONTROLLERS_VALIDATED");
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
    static void ValidateHudCorners(WoodlandGame game)
    {
        Canvas.ForceUpdateCanvases();
        var canvas = (RectTransform)game.hud.transform;
        for (int i = 0; i < game.hud.fighterPlates.Length; i++) {
            var corners = new Vector3[4];game.hud.fighterPlates[i].GetWorldCorners(corners);
            var points = corners.Select(point => (Vector2)canvas.InverseTransformPoint(point)).ToArray();
            Check(points.All(point => point.x >= canvas.rect.xMin - 1 && point.x <= canvas.rect.xMax + 1 && point.y >= canvas.rect.yMin - 1 && point.y <= canvas.rect.yMax + 1), "Player " + (i + 1) + ": entire HUD plate fits inside the canvas");
            Check(canvas.rect.yMax - points.Max(point => point.y) < 90 && (i == 0 ? points.Min(point => point.x) - canvas.rect.xMin : canvas.rect.xMax - points.Max(point => point.x)) < 90, "Player " + (i + 1) + ": HUD plate sits near its top screen corner");
        }
    }
    static void ValidateHudMeters(WoodlandGame game)
    {
        game.hud.Refresh();Canvas.ForceUpdateCanvases();
        Check(game.hud.soundText==null,"No sound-percentage popup is created on the combat HUD");
        Check(game.hud.transform.Find("Match HUD/Versus Emblem")!=null,"Generated VS artwork is present between the two HUDs");
        for(int player=0;player<2;player++){
            var diamond=(RectTransform)game.hud.actionDiscs[player*4].transform.parent;
            Check(Mathf.Approximately(diamond.rect.height*diamond.lossyScale.y,game.hud.fighterPlates[player].rect.height*game.hud.fighterPlates[player].lossyScale.y),"Player "+(player+1)+": action diamond matches the compact fighter panel height");
            var centers=Enumerable.Range(0,4).Select(n=>{var r=game.hud.actionDiscs[player*4+n].rectTransform;return r.anchoredPosition+new Vector2(r.rect.width*.5f,-r.rect.height*.5f);}).ToArray();
            Check(Mathf.Approximately(centers[0].x,centers[3].x)&&Mathf.Approximately(centers[1].y,centers[2].y)&&Mathf.Approximately(Vector2.Distance(centers[0],centers[3]),Vector2.Distance(centers[1],centers[2])),"Player "+(player+1)+": four circles occupy the equal diagonal endpoints of a diamond");
        }
        Check(game.hud.fighterPortraits.Length==2 && game.hud.fighterPortraits.All(image=>image!=null && image.sprite!=null),"Both fighter portraits are wired with real character sprites");
        for(int i=0;i<2;i++){
            var health=game.hud.healthFill[i].rectTransform;var stamina=game.hud.shieldFill[i].rectTransform;
            var healthNumber=game.hud.healthText[i].rectTransform;var staminaNumber=game.hud.shieldText[i].rectTransform;
            Check(game.hud.healthText[i].text.Replace(" ","")=="100/100" && game.hud.shieldText[i].text.Replace(" ","")=="100/100","Player "+(i+1)+": health and stamina show readable whole-number current/max values");
            Check(health.rect.Contains(health.InverseTransformPoint(healthNumber.TransformPoint(healthNumber.rect.center))) && stamina.rect.Contains(stamina.InverseTransformPoint(staminaNumber.TransformPoint(staminaNumber.rect.center))),"Player "+(i+1)+": each numeric value is centered inside its own meter");
            Check(health.rect.height>stamina.rect.height,"Player "+(i+1)+": health meter is visually larger than the stamina meter");
            var portrait=game.hud.fighterPortraits[i].rectTransform;var name=game.hud.names[i].rectTransform;
            Vector3 portraitCenter=portrait.TransformPoint(portrait.rect.center);
            Check((i==0?portraitCenter.x<health.TransformPoint(health.rect.center).x:portraitCenter.x>health.TransformPoint(health.rect.center).x) && name.TransformPoint(name.rect.center).y<portraitCenter.y,"Player "+(i+1)+": portrait mirrors toward its outside screen edge with its name below");
        }
    }
    static void ValidateCharacterActionCovers(WoodlandGame game)
    {
        var hud=game.hud;
        Check(hud.characterActionCovers.Length==16 && hud.characterActionCovers.All(sprite=>sprite!=null),"All sixteen character-specific action covers are loaded");
        Check(hud.characterActionCovers.Select(sprite=>AssetDatabase.GetAssetPath(sprite)).Distinct().Count()==16,"Each character/action pair references a separate artwork asset");
        int[] previous=(int[])game.CharacterChoices.Clone();
        string[] badges=hud.actionGlyphs.Select(label=>label.text).ToArray();
        string[] actions={"Block","Punch","Kick","Special"};
        try {
            for(int character=0;character<4;character++) {
                for(int player=0;player<2;player++) {
                    int selected=(character+player)%4;game.CharacterChoices[player]=selected;
                    game.players[player].SetCharacter(selected,game.CharacterNames[selected],game.CharacterAnimationSets[selected],game.CharacterControllers[selected],game.CharacterColors[selected]);
                }
                hud.Refresh();Canvas.ForceUpdateCanvases();
                for(int player=0;player<2;player++)for(int action=0;action<4;action++) {
                    int selected=game.players[player].CharacterIndex;int slot=player*4+action;
                    string expected="Assets/Art/UI/Actions/"+game.CharacterNames[selected]+"/"+actions[action]+".png";
                    Check(AssetDatabase.GetAssetPath(hud.actionCovers[slot].sprite)==expected,"P"+(player+1)+" "+game.CharacterNames[selected]+" "+actions[action]+": live HUD shows its own artwork");
                    Check(hud.actionGlyphs[slot].text==badges[slot],"P"+(player+1)+" "+game.CharacterNames[selected]+" "+actions[action]+": changing character preserves its input badge");
                }
                Capture("Documentation/character-hud-"+game.CharacterNames[character].ToLowerInvariant()+"-v6.2.png");
            }
        } finally {
            for(int player=0;player<2;player++) {
                int selected=previous[player];game.CharacterChoices[player]=selected;
                game.players[player].SetCharacter(selected,game.CharacterNames[selected],game.CharacterAnimationSets[selected],game.CharacterControllers[selected],game.CharacterColors[selected]);
            }
            hud.Refresh();
        }
    }
    static void PrepareSpecialCase(WoodlandGame game,int skill,float separation)
    {
        heldKey=Key.None;padStates[0]=padStates[1]=new GamepadState();
        int[] choices={skill,skill==0?1:0};
        for(int i=0;i<2;i++){
            var scout=game.players[i];int index=choices[i];
            scout.SetCharacter(index,game.CharacterNames[index],game.CharacterAnimationSets[index],game.CharacterControllers[index],game.CharacterColors[index]);
            scout.ResetScout();scout.inputEnabled=false;scout.aiControlled=false;scout.Body.gravityScale=0;scout.Body.simulated=true;
        }
        PositionSpecialPair(game,separation,-.2f);
    }
    static void PositionSpecialPair(WoodlandGame game,float separation,float height)
    {
        for(int i=0;i<2;i++){
            var scout=game.players[i];scout.Body.position=new Vector2((i==0?-1:1)*separation*.5f,height);scout.transform.position=scout.Body.position;scout.Body.linearVelocity=Vector2.zero;
        }
        Physics2D.SyncTransforms();
    }
    static void SelectMenuRoster(WoodlandGame game, bool solo, bool balance, int arena)
    {
        game.hud.modeButtons[solo ? 0 : 1].onClick.Invoke();
        Check(game.Screen == WoodlandGame.MenuScreen.Rules && game.SoloMode == solo && !game.playing, "Mode button opens the rules screen with the selected player mode");
        game.hud.ruleButtons[balance ? 1 : 0].onClick.Invoke();
        Check(game.Screen == WoodlandGame.MenuScreen.Characters && game.BalanceMode == balance, "Rule button opens character selection with the selected arena rules");
        if(!SessionState.GetBool("Woodland.RosterCaptured",false)){game.hud.RefreshMenu();Capture("Documentation/roster-menu-1280-v6.png");SessionState.SetBool("Woodland.RosterCaptured",true);}
        game.hud.characterButtons[0].onClick.Invoke();game.hud.confirmCharacterButton.onClick.Invoke();
        if (!solo) {
            Check(game.Screen == WoodlandGame.MenuScreen.Characters && game.ChoosingPlayer == 1, "Local roster confirms player one before choosing player two");
            game.hud.characterButtons[1].onClick.Invoke();game.hud.confirmCharacterButton.onClick.Invoke();
        }
        Check(game.Screen == WoodlandGame.MenuScreen.Maps && game.CharacterChoices[0] == 0 && game.CharacterChoices[1] == 1, "Roster selection reaches maps with distinct Fern and Ember choices");
        game.hud.mapButtons[arena].onClick.Invoke();
    }
    static void ValidateRosterAssets()
    {
        var previous = new System.Collections.Generic.HashSet<string>();
        foreach (string name in new[] { "Fern", "Ember", "Luna", "Bram" }) {
            string directory = "Assets/Animations/" + name + "/";
            var catalog = AssetDatabase.LoadAssetAtPath<ScoutAnimations>(directory + "Frames.asset");
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(directory + name + ".controller");
            Check(catalog != null && controller != null && controller.animationClips.Length > 0, name + ": independent sprite catalog and editable controller exist");
            var own = new System.Collections.Generic.HashSet<string>();
            foreach (string state in new[] { "Walk", "Jump", "Hurt", "Punch", "Knockout", "Block", "DoubleJump", "Kick" }) {
                var frames = catalog.Get(state);
                Check(frames.Length == 10 && frames.All(frame => frame != null && frame.rect.width == 256 && frame.rect.height == 256 && AssetDatabase.GetAssetPath(frame).StartsWith("Assets/Art/Characters/" + name + "/", StringComparison.Ordinal)), name + "/" + state + ": ten aligned sprites belong exclusively to this character");
                foreach (var frame in frames) own.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(frame)));
            }
            var special = catalog.Get("Special");
            Check(special.Length == 10 && special.All(frame => frame != null && frame.rect.height == 256 && (frame.rect.width == 256 || frame.rect.width == 384) && AssetDatabase.GetAssetPath(frame).StartsWith("Assets/Art/Characters/" + name + "/", StringComparison.Ordinal)), name + "/Special: ten own sprites preserve supported full-body proportions");
            foreach (var frame in special) own.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(frame)));
            Check(!own.Overlaps(previous), name + ": source sprite GUIDs are not reused from another roster character");
            Check(controller.animationClips.All(clip => AssetDatabase.GetAssetPath(clip).StartsWith(directory, StringComparison.Ordinal)), name + ": all controller clips belong to its own animation folder");
            Check(controller.animationClips.All(clip => AnimationUtility.GetObjectReferenceCurveBindings(clip).Where(binding => binding.type == typeof(SpriteRenderer)).SelectMany(binding => AnimationUtility.GetObjectReferenceCurve(clip, binding)).Select(key => key.value as Sprite).Where(sprite => sprite != null).All(sprite => AssetDatabase.GetAssetPath(sprite).StartsWith("Assets/Art/Characters/" + name + "/", StringComparison.Ordinal))), name + ": native animation clips never reference another character's sprites");
            previous.UnionWith(own);
        }
    }
    static void ValidateSettings(WoodlandGame game)
    {
        var effects=WoodlandEffects.Instance;float volume=effects.SfxVolume;bool shake=effects.cameraShake;string quality=game.EffectsQualityName;
        try {
            effects.SetVolume(.5f,false);game.StepVolume(-.1f);
            Check(game.SettingsVolume<.5f && game.SettingsVolume>=0,"Settings volume down updates the real bounded audio level");
            float lower=game.SettingsVolume;game.StepVolume(.1f);
            Check(game.SettingsVolume>lower && game.SettingsVolume<=1,"Settings volume up updates the real bounded audio level");
            game.ToggleCameraShake();Check(effects.cameraShake!=shake,"Settings camera shake toggle changes the live effect setting");
            game.ToggleCameraShake();Check(effects.cameraShake==shake,"A second shake toggle restores the original setting");
            game.CycleEffectsQuality();Check(game.EffectsQualityName!=quality,"Settings effect quality cycles to a different preset");
            for(int i=0;i<4 && game.EffectsQualityName!=quality;i++)game.CycleEffectsQuality();
            Check(game.EffectsQualityName==quality,"Effect quality cycling can return to the original preset");
            Capture("Documentation/settings-1280-v6.png");
        } finally {
            effects.SetVolume(volume,false);
            if(effects.cameraShake!=shake)game.ToggleCameraShake();
            for(int i=0;i<4 && game.EffectsQualityName!=quality;i++)game.CycleEffectsQuality();
        }
    }
    static void PrepareAiCase(WoodlandGame game, float separation)
    {
        game.plank.rotation = 0;game.plank.transform.rotation = Quaternion.identity;
        game.plank.bodyType = RigidbodyType2D.Kinematic;game.plank.simulated = true;
        var box = game.plank.GetComponent<BoxCollider2D>();
        float top = game.plank.transform.TransformPoint(box.offset + Vector2.up * box.size.y * .5f).y + box.edgeRadius * game.plank.transform.lossyScale.x;
        for (int i = 0; i < 2; i++) {
            var scout = game.players[i];scout.ResetScout();
            Vector2 point = new Vector2((i == 0 ? -1 : 1) * separation * .5f, top + .625f);
            scout.transform.position = point;scout.Body.position = point;scout.Body.linearVelocity = Vector2.zero;
            scout.Body.gravityScale = 1;scout.Body.simulated = true;scout.inputEnabled = i == 1;
        }
        Physics2D.SyncTransforms();
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
            game.RefreshGamepads();game.hud.helpPanel.SetActive(false);game.hud.Refresh();
            Capture("Documentation/controller-diamond-1280-v6.png");
            game.hud.helpPanel.SetActive(true);game.hud.Refresh();
            Check(game.hud.deviceText[0].text.Contains("PLAYSTATION") && game.hud.deviceText[1].text.Contains("XBOX"), "Assigned PlayStation and Xbox layouts appear on their own player panels");
            Check(game.hud.controlsText[0].text.Contains("CROSS") && game.hud.controlsText[0].text.Contains("SQUARE") && game.hud.controlsText[1].text.ToUpperInvariant().Contains("BLOCK") && game.hud.controlsText[1].text.Contains("RB"), "Help displays real PlayStation and Xbox button names for the assigned virtual layouts");
            Capture("Documentation/controller-help-1280-v6.png");
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
        File.WriteAllText("Documentation/PLAYMODE-VALIDATION-v6.json", JsonUtility.ToJson(report, true));
        if (success) Debug.Log(status);else Debug.LogError(status);
        EditorApplication.isPlaying = false;
        if (SessionState.GetBool("Woodland.ValidationExit", false)) EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
    }
}
