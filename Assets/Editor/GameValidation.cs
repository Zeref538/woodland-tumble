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
    public static void Run() => Begin(true);
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
                foreach (var scout in game.players) foreach (string state in new[] { "Walk", "Jump", "Hurt", "Push", "Punch", "Knockout" }) {
                    var frames = scout.animations.Get(state);
                    Check(frames.Length == 10 && frames.All(f => f != null && f.rect.width == 256 && f.rect.height == 256), scout.displayName + "/" + state + ": ten independent 256px sprites");
                    Check(scout.animator.HasState(0, Animator.StringToHash("Base Layer." + state)), scout.displayName + "/" + state + ": editable Animator state exists");
                }
                Check(game.hud.healthFill.All(i => i.sprite != null && i.type == Image.Type.Filled), "HUD health fills and panel sprites are serialized");
                Capture("Documentation/GamePreview-v2.png");
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
                Capture("Documentation/VFXPreview.png");
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
            } else if (stage == 2 && SimElapsed > .40f) {
                Arrange(game);
                Check(a.TryAttack(false), "Push recovers after cooldown");
                SetStage(1);
            } else if (stage == 3 && SimElapsed > .8f) {
                // Place the collapsed sprite on the board for a clear animation preview.
                b.Body.linearVelocity = Vector2.zero;b.Body.position = new Vector2(1.5f, .12f);b.transform.position = b.Body.position;
                Capture("Documentation/KnockoutPreview-v2.png");
                SetStage(4);
            } else if (stage == 4 && SimElapsed > .65f) {
                Check(game.score[0] == 1, "Knockout awards the opponent a round");
                game.StopAllCoroutines();game.playing = true;
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
        File.WriteAllText("Documentation/PLAYMODE-VALIDATION-v2.json", JsonUtility.ToJson(report, true));
        if (success) Debug.Log(status);else Debug.LogError(status);
        EditorApplication.isPlaying = false;
        if (SessionState.GetBool("Woodland.ValidationExit", false)) EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
    }
}
