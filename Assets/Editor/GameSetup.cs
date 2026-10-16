using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.IO;
using System.Linq;

public static class GameSetup
{
    static readonly string[] States = { "Walk", "Jump", "Hurt", "Push", "Punch", "Knockout" };
    static Font font;
    static Sprite panelSprite;
    static Color cream = new Color(1, .91f, .71f);
    static Color gold = new Color(.80f, .66f, .34f);

    [MenuItem("Woodland/Rebuild Editable Demo Scene")]
    public static void Prepare()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode before rebuilding the scene.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ImportSprite("Assets/Resources/Art/Forest.png", 100);
        foreach (var file in Directory.GetFiles("Assets/Art/Characters", "*.png", SearchOption.AllDirectories)) ImportSprite(file.Replace('\\', '/'), 160, new Vector2(.5f, 24f / 256));
        foreach (var file in Directory.GetFiles("Assets/Art/Arena", "*.png")) ImportSprite(file, 100);
        ImportSprite("Assets/Art/UI/WoodlandPanel.png", 100, null, new Vector4(12, 12, 12, 12));
        ImportSprite("Assets/Art/UI/Solid.png", 100);
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PressStart2P-Regular.ttf");
        panelSprite = SpriteAt("Assets/Art/UI/WoodlandPanel.png");
        GraphicsSettings.defaultRenderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
        EditorSettings.serializationMode = SerializationMode.ForceText;
        EditorSettings.enterPlayModeOptionsEnabled = false;
        string path = "Assets/Scenes/WoodlandTumble.unity";
        if (File.Exists(path)) File.Copy(path, "Documentation/Backups/WoodlandTumble-before-rebuild.unity.txt", true);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true; camera.orthographicSize = 5.6f;
        camera.transform.position = new Vector3(0, 1, -10);
        camera.backgroundColor = new Color(.035f, .10f, .12f);
        camera.gameObject.AddComponent<AudioListener>();
        var game = new GameObject("Woodland Game - Match Rules").AddComponent<WoodlandGame>();
        new GameObject("Effects - Trails and Splashes").AddComponent<WoodlandEffects>();
        var environment = new GameObject("Environment - Edit These Objects").transform;
        var bg = Visual("Golden Woodland Background", SpriteAt("Assets/Resources/Art/Forest.png"), new Vector2(0, 1), -20, environment);
        Fit(bg, 22, 12.5f);
        var ball = Visual("Runewood Balancing Sphere", SpriteAt("Assets/Art/Arena/RunewoodSphere.png"), new Vector2(0, -1.56f), 1, environment);
        Fit(ball, 1.85f, 1.85f);
        var beam = Visual("Mossbound Oak Plank", SpriteAt("Assets/Art/Arena/MossboundPlank.png"), new Vector2(0, -.56f), 3, environment);
        Fit(beam, 10.4f, .90f);
        var rb = beam.AddComponent<Rigidbody2D>();rb.mass = 6;rb.angularDamping = 2;rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var grip = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Arena/WoodGrip.physicsMaterial2D");
        if (grip == null) { grip = new PhysicsMaterial2D("Wood Grip") { friction = .65f }; AssetDatabase.CreateAsset(grip, "Assets/Art/Arena/WoodGrip.physicsMaterial2D"); }
        var box = beam.AddComponent<BoxCollider2D>();
        // The mushrooms and hanging vines are decorative; this flat collider matches the moss walking surface.
        box.size = new Vector2(9.9f / beam.transform.localScale.x, .22f / beam.transform.localScale.y);
        box.offset = new Vector2(0, .04f / beam.transform.localScale.y);
        box.sharedMaterial = grip;
        box.edgeRadius = .04f / beam.transform.localScale.x;
        var hinge = beam.AddComponent<HingeJoint2D>();hinge.autoConfigureConnectedAnchor = false;hinge.connectedAnchor = beam.transform.position;hinge.useLimits = true;hinge.limits = new JointAngleLimits2D { min = -25, max = 25 };
        game.plank = rb;
        var actors = new GameObject("Players - Editable Prefab Instances").transform;
        var spawns = new GameObject("Spawn Points - Move These To Change Starting Positions").transform;
        for (int p = 0; p < 2; p++) {
            string name = p == 0 ? "Fern" : "Ember";
            ScoutAnimations set = MakeAnimations(name);
            var spawn = new GameObject(name + " Spawn").transform;spawn.SetParent(spawns);spawn.position = new Vector3(p == 0 ? -2.7f : 2.7f, .3f, 0);
            spawn.gameObject.AddComponent<SpawnMarker>().color = p == 0 ? Color.green : new Color(1, .5f, .2f);
            var root = new GameObject(name + " - Player " + (p + 1));root.transform.SetParent(actors);root.transform.position = spawn.position;
            var body = root.AddComponent<Rigidbody2D>();body.mass = 1;body.freezeRotation = true;body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var capsule = root.AddComponent<CapsuleCollider2D>();capsule.size = new Vector2(.64f, 1.25f);capsule.sharedMaterial = PlayerSlideMaterial();
            var sprite = Visual("Character - Animated Sprites", set.walk[0], Vector2.zero, 5, root.transform);
            sprite.transform.localPosition = new Vector3(0, -.625f, 0);
            sprite.GetComponent<SpriteRenderer>().flipX = p == 1;
            var anim = sprite.AddComponent<Animator>();anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/" + name + "/" + name + ".controller");anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var scout = root.AddComponent<Scout>();scout.id = p;scout.displayName = name;scout.art = sprite.GetComponent<SpriteRenderer>();scout.animator = anim;scout.animations = set;
            string prefabPath = "Assets/Prefabs/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
            scout.spawnPoint = spawn; // Scene-specific reference remains an editable prefab override.
            PrefabUtility.RecordPrefabInstancePropertyModifications(scout);
            game.players[p] = scout;
        }
        CreateHUD(game);
        var events = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        PlayerSettings.companyName = "Woodland Games";PlayerSettings.productName = "Woodland Tumble";
        PlayerSettings.defaultScreenWidth = 1280;PlayerSettings.defaultScreenHeight = 720;PlayerSettings.runInBackground = true;
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = beam;
        SceneView.RepaintAll();
        Debug.Log("WOODLAND_EDITABLE_READY: scene, 120 PNG frames, animation clips, prefabs and HUD saved.");
    }
    static void ImportSprite(string path, float ppu, Vector2? pivot = null, Vector4? border = null)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) throw new System.Exception("Missing art: " + path);
        imp.textureType = TextureImporterType.Sprite;imp.spriteImportMode = SpriteImportMode.Single;imp.spritePixelsPerUnit = ppu;
        var settings = new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteMeshType = SpriteMeshType.FullRect;settings.spriteAlignment = (int)SpriteAlignment.Custom;settings.spritePivot = pivot ?? Vector2.one * .5f;imp.SetTextureSettings(settings);
        if (border.HasValue) imp.spriteBorder = border.Value;
        imp.filterMode = FilterMode.Point;imp.textureCompression = TextureImporterCompression.Uncompressed;imp.alphaIsTransparency = true;imp.mipmapEnabled = false;imp.isReadable = true;imp.maxTextureSize = 2048;
        imp.SaveAndReimport();
    }
    static Sprite SpriteAt(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static GameObject Visual(string name, Sprite sprite, Vector2 pos, int order, Transform parent)
    {
        var go = new GameObject(name);go.transform.SetParent(parent, false);go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();sr.sprite = sprite;sr.sortingOrder = order;
        string materialPath = "Assets/Art/" + (name.StartsWith("Character") ? "Characters/" + (parent.name.StartsWith("Fern") ? "Fern" : "Ember") : "Arena/" + name.Replace(" ", "")) + ".mat";
        // Character sprites each have their own texture, so animation must bind _MainTex per frame.
        // SpriteRenderer supplies this via its per-renderer properties; distinct renderer materials also avoid URP texture sharing.
        var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(mat, materialPath); }
        mat.mainTexture = sprite.texture;EditorUtility.SetDirty(mat);sr.sharedMaterial = mat;
        return go;
    }
    static void Fit(GameObject go, float width, float height) { var size = go.GetComponent<SpriteRenderer>().sprite.bounds.size;go.transform.localScale = new Vector3(width / size.x, height / size.y, 1); }
    static ScoutAnimations MakeAnimations(string name)
    {
        string dir = "Assets/Animations/" + name;Directory.CreateDirectory(dir);AssetDatabase.Refresh();
        string setPath = dir + "/Frames.asset";
        var set = AssetDatabase.LoadAssetAtPath<ScoutAnimations>(setPath);
        if (!set) { set = ScriptableObject.CreateInstance<ScoutAnimations>();AssetDatabase.CreateAsset(set, setPath); }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(dir + "/" + name + ".controller");
        if (controller) AssetDatabase.DeleteAsset(dir + "/" + name + ".controller");
        controller = AnimatorController.CreateAnimatorControllerAtPath(dir + "/" + name + ".controller");
        foreach (string state in States) {
            var sprites = Enumerable.Range(1, 10).Select(i => SpriteAt("Assets/Art/Characters/" + name + "/" + state + "/" + i.ToString("00") + ".png")).ToArray();
            if (sprites.Any(s => s == null)) throw new System.Exception("Incomplete animation: " + name + "/" + state);
            switch (state) { case "Walk":set.walk = sprites;break;case "Jump":set.jump = sprites;break;case "Hurt":set.hurt = sprites;break;case "Push":set.push = sprites;break;case "Punch":set.punch = sprites;break;case "Knockout":set.knockout = sprites;break; }
            float rate = state == "Push" || state == "Punch" ? 20 : state == "Hurt" ? 22 : state == "Jump" ? 12 : 10;
            var clip = MakeClip(dir + "/" + state + ".anim", sprites, rate, state == "Walk");
            var node = controller.layers[0].stateMachine.AddState(state);node.motion = clip;
        }
        var idleClip = MakeClip(dir + "/Idle.anim", new[] { set.walk[0] }, 1, true);
        var idle = controller.layers[0].stateMachine.AddState("Idle");idle.motion = idleClip;controller.layers[0].stateMachine.defaultState = idle;
        EditorUtility.SetDirty(set);EditorUtility.SetDirty(controller);
        return set;
    }
    static AnimationClip MakeClip(string path, Sprite[] sprites, float rate, bool loop)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (!clip) { clip = new AnimationClip();AssetDatabase.CreateAsset(clip, path); }
        clip.frameRate = rate;
        var keys = sprites.Select((s, i) => new ObjectReferenceKeyframe { time = i / rate, value = s }).ToList();
        keys.Add(new ObjectReferenceKeyframe { time = sprites.Length / rate, value = sprites[sprites.Length - 1] });
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys.ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime = loop;AnimationUtility.SetAnimationClipSettings(clip, settings);EditorUtility.SetDirty(clip);
        return clip;
    }
    static void CreateHUD(WoodlandGame game)
    {
        var root = new GameObject("HUD - Edit Text and Woodland Panels", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution = new Vector2(1280, 720);scaler.matchWidthOrHeight = .5f;
        var hud = root.AddComponent<WoodlandHUD>();hud.game = game;game.hud = hud;
        var title = Panel("Title - Moss and Brass", root.transform, new Vector2(.5f, 1), new Vector2(0, -18), new Vector2(414, 94));
        Label("Game Title", title, "WOODLAND TUMBLE", new Rect(16, 16, 382, 30), 23, cream, TextAnchor.MiddleCenter);
        Label("Match Rules", title, "BALANCE / BRAWL / BEST OF FIVE", new Rect(16, 56, 382, 16), 10, gold, TextAnchor.MiddleCenter);
        for (int i = 0; i < 2; i++) {
            Color accent = i == 0 ? hud.fernColor : hud.emberColor;
            var panel = Panel(i == 0 ? "Fern Player Panel" : "Ember Player Panel", root.transform, new Vector2(i, 1), new Vector2(i == 0 ? 24 : -24, -18), new Vector2(360, 154));
            panel.pivot = new Vector2(i, 1);
            hud.names[i] = Label("Player Name", panel, i == 0 ? "P1  FERN" : "P2  EMBER", new Rect(18, 15, 224, 22), 18, accent);
            for (int p = 0; p < 3; p++) { var pip = Flat("Round " + (p + 1), panel, new Rect(282 + p * 20, 20, 10, 10), accent);pip.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);hud.roundPips[i * 3 + p] = pip; }
            Flat("Health Bar Track", panel, new Rect(18, 47, 324, 14), new Color(.15f, .20f, .15f));
            hud.healthFill[i] = Flat("Health Fill", panel, new Rect(20, 49, 320, 10), accent);hud.healthFill[i].type = Image.Type.Filled;hud.healthFill[i].fillMethod = Image.FillMethod.Horizontal;
            hud.healthText[i] = Label("Health", panel, "100 / 100 HP", new Rect(18, 69, 165, 16), 11, cream);
            hud.damageText[i] = Label("Knockback Buildup", panel, "KNOCKBACK +0%", new Rect(180, 69, 164, 16), 10, gold, TextAnchor.MiddleRight);
            hud.statusText[i] = Label("Combat Status", panel, "HOLD YOUR GROUND", new Rect(18, 93, 324, 16), 11, cream);
            Label("Push Caption", panel, "PUSH", new Rect(18, 122, 44, 13), 9, gold);
            Label("Punch Caption", panel, "PUNCH", new Rect(182, 122, 53, 13), 9, gold);
            Flat("Push Cooldown Track", panel, new Rect(66, 123, 94, 8), new Color(.15f, .20f, .15f));
            Flat("Punch Cooldown Track", panel, new Rect(241, 123, 101, 8), new Color(.15f, .20f, .15f));
            hud.pushFill[i] = Flat("Push Ready", panel, new Rect(66, 123, 94, 8), accent);hud.pushFill[i].type = Image.Type.Filled;hud.pushFill[i].fillMethod = Image.FillMethod.Horizontal;
            hud.punchFill[i] = Flat("Punch Ready", panel, new Rect(241, 123, 101, 8), gold);hud.punchFill[i].type = Image.Type.Filled;hud.punchFill[i].fillMethod = Image.FillMethod.Horizontal;
            var controls = Panel(i == 0 ? "Fern Controls" : "Ember Controls", root.transform, new Vector2(i, 0), new Vector2(i == 0 ? 24 : -24, 24), new Vector2(592, 82));controls.pivot = new Vector2(i, 0);
            Label("Control Heading", controls, i == 0 ? "FERN / PLAYER ONE" : "EMBER / PLAYER TWO", new Rect(18, 13, 556, 17), 12, accent);
            Label("Movement Keys", controls, i == 0 ? "A D  MOVE     W  JUMP" : "ARROWS  MOVE     UP  JUMP", new Rect(18, 37, 556, 15), 11, cream);
            Label("Combat Keys", controls, i == 0 ? "F  SHOVE     G  PUNCH" : "K  SHOVE     L  PUNCH", new Rect(18, 58, 556, 14), 10, gold);
        }
        LabelAnchored("Pause and Restart Hint", root.transform, "ESC  PAUSE     R  RESTART", new Vector2(.5f, 0), new Vector2(0, 10), new Vector2(440, 12), 9, gold);
        var banner = Panel("Round Banner", root.transform, Vector2.one * .5f, new Vector2(0, 104), new Vector2(640, 110));banner.pivot = Vector2.one * .5f;
        hud.bannerPanel = banner.gameObject;
        hud.bannerText = Label("Round Announcement", banner, "GET READY", new Rect(18, 18, 604, 74), 23, cream, TextAnchor.MiddleCenter);
        banner.gameObject.SetActive(false);
    }
    static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));var rect = go.GetComponent<RectTransform>();rect.SetParent(parent, false);rect.anchorMin = rect.anchorMax = anchor;rect.pivot = new Vector2(.5f, 1);rect.sizeDelta = size;rect.anchoredPosition = position;
        var image = go.GetComponent<Image>();image.sprite = panelSprite;image.type = Image.Type.Sliced;image.raycastTarget = false;
        return rect;
    }
    static RectTransform Box(string name, Transform parent, Rect bounds)
    {
        var go = new GameObject(name, typeof(RectTransform));var rect = go.GetComponent<RectTransform>();rect.SetParent(parent, false);rect.anchorMin = rect.anchorMax = new Vector2(0, 1);rect.pivot = new Vector2(0, 1);rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);rect.sizeDelta = bounds.size;return rect;
    }
    static Text Label(string name, Transform parent, string value, Rect bounds, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var rect = Box(name, parent, bounds);var text = rect.gameObject.AddComponent<Text>();text.font = font;text.text = value;text.fontSize = size;text.color = color;text.alignment = alignment;text.raycastTarget = false;text.horizontalOverflow = HorizontalWrapMode.Wrap;text.verticalOverflow = VerticalWrapMode.Truncate;
        var shadow = rect.gameObject.AddComponent<Shadow>();shadow.effectColor = new Color(.025f, .07f, .06f, .95f);shadow.effectDistance = new Vector2(2, -2);
        return text;
    }
    static void LabelAnchored(string name, Transform parent, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var text = Label(name, parent, value, new Rect(0, 0, size.x, size.y), fontSize, color, TextAnchor.MiddleCenter);var rect = text.rectTransform;rect.anchorMin = rect.anchorMax = anchor;rect.pivot = Vector2.one * .5f;rect.anchoredPosition = position;
    }
    static Image Flat(string name, Transform parent, Rect bounds, Color color)
    {
        var rect = Box(name, parent, bounds);var image = rect.gameObject.AddComponent<Image>();image.sprite = SpriteAt("Assets/Art/UI/Solid.png");image.color = color;image.raycastTarget = false;return image;
    }
    static PhysicsMaterial2D PlayerSlideMaterial()
    {
        const string path = "Assets/Art/Arena/PlayerSlide.physicsMaterial2D";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (!material) { material = new PhysicsMaterial2D("Player Slide");AssetDatabase.CreateAsset(material, path); }
        material.friction = 0;material.bounciness = 0;material.frictionCombine = PhysicsMaterialCombine2D.Minimum;
        EditorUtility.SetDirty(material);
        return material;
    }
    static void TuneScout(Scout scout)
    {
        scout.pushDamage = 12;scout.punchDamage = 16;scout.pushKnockback = 4.8f;scout.punchKnockback = 4;
        scout.damageKnockbackMultiplier = .30f;scout.airAcceleration = 8;
        scout.GetComponent<CapsuleCollider2D>().sharedMaterial = PlayerSlideMaterial();
        EditorUtility.SetDirty(scout);
    }
    public static void ApplyPhysicsTuning()
    {
        foreach (string name in new[] { "Fern", "Ember" }) {
            string path = "Assets/Prefabs/" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            TuneScout(root.GetComponent<Scout>());
            PrefabUtility.SaveAsPrefabAsset(root, path);PrefabUtility.UnloadPrefabContents(root);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/WoodlandTumble.unity");
        var game = Object.FindFirstObjectByType<WoodlandGame>();
        foreach (var scout in game.players) { TuneScout(scout);PrefabUtility.RecordPrefabInstancePropertyModifications(scout); }
        var collider = game.plank.GetComponent<BoxCollider2D>();collider.edgeRadius = .04f / game.plank.transform.lossyScale.x;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("PHYSICS_TUNING_APPLIED");
    }
    public static void FinishLayout()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/WoodlandTumble.unity");
        GameObject.Find("Runewood Balancing Sphere").transform.position = new Vector3(0, -1.56f, 0);
        AssetDatabase.ImportAsset("Assets/Art/UI/WoodlandPanel.png", ImportAssetOptions.ForceUpdate);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }
    public static void Build()
    {
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Build/Woodland Tumble.app", BuildTarget.StandaloneOSX, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Build failed");
    }
}
