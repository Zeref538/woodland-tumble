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
    static InputSettings originalInputSettings, validationInputSettings;
    const string Active = "Woodland.ValidateV2";
    static GameValidation() { EditorApplication.update += Tick; }
    [Serializable] public class Report { public bool success; public string status; public string[] checks; }
    [MenuItem("Woodland/Validate Editable Game")]
    public static void RunInEditor() => Begin(false);
    public static void Run() { GameSetup.Prepare(); Begin(true); }
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
                foreach (var scout in game.players) foreach (string state in new[] { "Walk", "Jump", "Hurt", "Push", "Punch", "Knockout", "Block", "DoubleJump", "Block", "Dash", "DoubleJump", "Charge", "Smash", "Combo2", "Combo3" }) {
                    var frames = scout.animations.Get(state);
                    Check(frames.Length == 10 && frames.All(f => f != null && f.rect.width == 256 && f.rect.height == 256), scout.displayName + "/" + state + ": ten independent 256px sprites");
                    Check(scout.animator.HasState(0, Animator.StringToHash("Base Layer." + state)), scout.displayName + "/" + state + ": editable Animator state exists");
                }
                Check(game.hud.healthFill.All(i => i.sprite != null && i.type == Image.Type.Filled), "HUD health fills and panel sprites are serialized");
                Capture("Documentation/GamePreview-v3.png");
                var originalCollider = game.plank.GetComponent<BoxCollider2D>().size;
                var originalScale = game.plank.transform.localScale;
                for (int arena = 0; arena < 3; arena++) {
                    game.SelectArena(arena);
                    Check(game.ArenaIndex == arena && game.backgroundArt.sprite == game.backgrounds[arena] && game.plankArt.sprite == game.planks[arena] && game.sphereArt.sprite == game.spheres[arena], "Arena " + arena + ": matching background, plank and sphere switch together");
                    Check(game.plank.GetComponent<BoxCollider2D>().size == originalCollider && game.plank.transform.localScale == originalScale, "Arena " + arena + ": visual changes preserve collider proportions");
                    Capture("Documentation/arena-" + arena + "-1280.png");
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
                Capture("Documentation/VFXPreview-v3.png");
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
                Capture("Documentation/KnockoutPreview-v3.png");
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
                a.inputEnabled = false;
                Check(a.TryJump() && a.AirJumpsRemaining == 1, "Ground jump preserves the one aerial jump budget");
                Check(a.TryJump() && a.AirJumpsRemaining == 0, "Double jump consumes exactly one aerial jump");
                Check(!a.TryJump(), "Third jump is denied before landing");
                Check(a.AnimationState == "DoubleJump", "Aerial jump selects its dedicated animation");
                SetStage(20);
            } else if (stage == 20 && SimElapsed > .18f) {
                Check(WoodlandEffects.Instance.ActiveMotes > 0, "Double jump produces visible pooled effects");
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
                Capture("Documentation/combat-guard-1280.png");
                a.Body.position = b.Body.position + Vector2.right;
                a.transform.position = a.Body.position;Physics2D.SyncTransforms();
                Check(b.ReceiveHit(a, -1, true) && b.Health < b.maxHealth, "A rear hit bypasses the directional guard");
                PrepareGuard(game);
                Check(!b.ReceiveHit(a, 1, true), "A freshly raised guard parries a frontal hit");
                Check(a.Status == "STAGGERED" && !a.TryAttack(true) && !a.TryDash(), "Parry staggers the attacker and blocks immediate retaliation");
                Check(Mathf.Approximately(b.Health, b.maxHealth), "Parry preserves defender health");
                Capture("Documentation/combat-parry-1280.png");
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
                Capture("Documentation/combat-smash-1280.png");
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
                Capture("Documentation/combat-combo-1280.png");
                SetStage(33);
            } else if (stage == 33 && SimElapsed > 1.1f) {
                Check(a.ComboCount == 0, "Combo counter expires when attacks stop connecting");
                a.ResetScout();b.ResetScout();
                Complete(true, "COMBAT_VFX_AND_EDGE_PHYSICS_VALIDATED");
            }
        } catch (Exception error) { Complete(false, error.ToString()); }
    }
    static void DriveTestInput()
    {
        if (testKeyboard == null) return;
        testKeyboard.MakeCurrent();InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(heldKey));
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
    static void StartJumpCase(WoodlandGame game)
    {
        var scout = game.players[0];scout.ResetScout();scout.inputEnabled = true;
        game.plank.rotation = 0;game.plank.transform.rotation = Quaternion.identity;
        var box = game.plank.GetComponent<BoxCollider2D>();
        float top = game.plank.transform.TransformPoint(box.offset + Vector2.up * box.size.y * .5f).y + box.edgeRadius * game.plank.transform.lossyScale.x;
        Vector2 point = new Vector2(0, top + .625f);
        scout.transform.position = point;scout.Body.position = point;scout.Body.gravityScale = 1;scout.Body.linearVelocity = Vector2.zero;scout.Body.simulated = true;
        SessionState.SetFloat("Woodland.JumpY", point.y);
        heldKey = Key.W;DriveTestInput();Physics2D.SyncTransforms();
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
        var mode = canvas.renderMode;var previousCamera = canvas.worldCamera;float distance = canvas.planeDistance;
        var rt = new RenderTexture(1280, 720, 24);
        camera.targetTexture = rt;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;canvas.worldCamera = camera;canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active = rt;
        var shot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);shot.Apply();File.WriteAllBytes(path, shot.EncodeToPNG());
        camera.targetTexture = null;RenderTexture.active = null;
        canvas.renderMode = mode;canvas.worldCamera = previousCamera;canvas.planeDistance = distance;
        UnityEngine.Object.DestroyImmediate(shot);UnityEngine.Object.DestroyImmediate(rt);
    }
    static void Complete(bool success, string status)
    {
        SessionState.SetBool(Active, false);
        InputSystem.onBeforeUpdate -= DriveTestInput;
        if (testKeyboard != null) { InputSystem.RemoveDevice(testKeyboard);testKeyboard = null; }
        if (originalInputSettings != null) { InputSystem.settings = originalInputSettings;UnityEngine.Object.DestroyImmediate(validationInputSettings);originalInputSettings = null; }
        var report = new Report { success = success, status = status, checks = SessionState.GetString("Woodland.Checks", "").Split('\n').Where(s => s.Length > 0).ToArray() };
        File.WriteAllText("Documentation/PLAYMODE-VALIDATION-v3.json", JsonUtility.ToJson(report, true));
        if (success) Debug.Log(status);else Debug.LogError(status);
        EditorApplication.isPlaying = false;
        if (SessionState.GetBool("Woodland.ValidationExit", false)) EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
    }
}
