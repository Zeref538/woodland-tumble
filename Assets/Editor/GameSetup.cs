using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        foreach (var file in Directory.GetFiles("Assets/Resources/Art", "*.png")) ImportSprite(file, 100);
        foreach (var file in Directory.GetFiles("Assets/Art/Characters", "*.png", SearchOption.AllDirectories)) ImportSprite(file.Replace('\\', '/'), 160, new Vector2(.5f, 24f / 256));
        foreach (var file in Directory.GetFiles("Assets/Art/VFX", "*.png", SearchOption.AllDirectories)) ImportSprite(file.Replace('\\','/'),256);
        foreach (var file in Directory.GetFiles("Assets/Art/Arena", "*.png")) ImportSprite(file, 100);
        ImportSprite("Assets/Art/UI/WoodlandPanel.png", 100, null, new Vector4(12, 12, 12, 12));
        ImportSprite("Assets/Art/UI/Solid.png", 100);
        string shadePath="Assets/Art/UI/HeaderShade.png";
        if(!File.Exists(shadePath)) {
            var shade=new Texture2D(2,64,TextureFormat.RGBA32,false);
            for(int y=0;y<64;y++)for(int x=0;x<2;x++)shade.SetPixel(x,y,new Color(.015f,.035f,.045f,Mathf.Pow(y/63f,1.6f)*.88f));
            shade.Apply();File.WriteAllBytes(shadePath,shade.EncodeToPNG());Object.DestroyImmediate(shade);AssetDatabase.Refresh();
        }
        ImportSprite(shadePath,100);var shadeImporter=(TextureImporter)AssetImporter.GetAtPath(shadePath);shadeImporter.filterMode=FilterMode.Bilinear;shadeImporter.SaveAndReimport();
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
        camera.orthographic = true; camera.orthographicSize = 4.5f; camera.allowHDR = true;
        camera.transform.position = new Vector3(0, 1, -10);
        camera.backgroundColor = new Color(.035f, .10f, .12f);
        camera.gameObject.AddComponent<AudioListener>();
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        string bloomPath = "Assets/Settings/CombatBloom.asset";
        var bloomProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(bloomPath);
        if (!bloomProfile) { bloomProfile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(bloomProfile,bloomPath); }
        if (!bloomProfile.TryGet<Bloom>(out var bloom)) { bloom=bloomProfile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,bloomProfile); }
        bloom.intensity.Override(.48f);bloom.threshold.Override(1.15f);bloom.scatter.Override(.65f);EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(bloomProfile);
        var volume = new GameObject("Elemental Bloom - Subtle HDR Light").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=bloomProfile;
        var game = new GameObject("Woodland Game - Match Rules").AddComponent<WoodlandGame>();
        var effects = new GameObject("Effects - Trails and Splashes").AddComponent<WoodlandEffects>();
        string[] cues={"swing","hit_light","hit_heavy","jump","double_jump","dash","land","block","parry","guard_break","knockout","victory","ui_click"};
        effects.fernSlash=Enumerable.Range(1,8).Select(i=>SpriteAt("Assets/Art/VFX/Fern/"+i.ToString("00")+".png")).ToArray();
        effects.emberSlash=Enumerable.Range(1,8).Select(i=>SpriteAt("Assets/Art/VFX/Ember/"+i.ToString("00")+".png")).ToArray();
        effects.soundBank=cues.Select(cue=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Combat/"+cue+".ogg")).ToArray();
        if(effects.soundBank.Any(clip=>!clip))throw new System.Exception("Missing action sound effect");
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
        game.backgroundArt = bg.GetComponent<SpriteRenderer>(); game.plankArt = beam.GetComponent<SpriteRenderer>(); game.sphereArt = ball.GetComponent<SpriteRenderer>();
        game.backgrounds = new[] { SpriteAt("Assets/Resources/Art/Forest.png"), SpriteAt("Assets/Resources/Art/MoonlitLake.png"), SpriteAt("Assets/Resources/Art/EmberGrove.png") };
        game.planks = new[] { SpriteAt("Assets/Art/Arena/MossboundPlank.png"), SpriteAt("Assets/Art/Arena/MoonlitPlank.png"), SpriteAt("Assets/Art/Arena/EmberPlank.png") };
        game.spheres = new[] { SpriteAt("Assets/Art/Arena/RunewoodSphere.png"), SpriteAt("Assets/Art/Arena/MoonstoneSphere.png"), SpriteAt("Assets/Art/Arena/ObsidianSphere.png") };
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
        Debug.Log("WOODLAND_EDITABLE_READY: scene, 160 PNG frames, animation clips, prefabs and HUD saved.");
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
        MakeCombatClips(name, dir, set, controller);
        var idleClip = MakeClip(dir + "/Idle.anim", new[] { set.walk[0] }, 1, true);
        var idle = controller.layers[0].stateMachine.AddState("Idle");idle.motion = idleClip;controller.layers[0].stateMachine.defaultState = idle;
        EditorUtility.SetDirty(set);EditorUtility.SetDirty(controller);
        return set;
    }
    static void MakeCombatClips(string name, string dir, ScoutAnimations set, AnimatorController controller)
    {
        bool fern = name == "Fern";
        set.block = Enumerable.Range(1,10).Select(i => SpriteAt("Assets/Art/Characters/" + name + "/Block/" + i.ToString("00") + ".png")).ToArray();
        set.doubleJump = Enumerable.Range(1,10).Select(i => SpriteAt("Assets/Art/Characters/" + name + "/DoubleJump/" + i.ToString("00") + ".png")).ToArray();
        if (set.block.Any(s => !s) || set.doubleJump.Any(s => !s)) throw new System.Exception("Missing dedicated combat frames for " + name);
        foreach (string state in new[] { "Block", "DoubleJump", "Dash", "Charge", "Smash", "Combo2", "Combo3" }) {
            Sprite[] poses = state == "Block" ? set.block.Take(7).ToArray()
                : state == "Charge" ? new[] { set.punch[0], set.punch[1], set.punch[0] }
                : state == "DoubleJump" ? set.doubleJump
                : state == "Dash" ? new[] { set.walk[3], set.walk[4], set.walk[5] }
                : state == "Combo2" ? (fern ? set.punch : set.push)
                : state == "Combo3" ? (fern ? set.push.Reverse().ToArray() : set.punch.Reverse().ToArray())
                : (fern ? set.push : set.punch);
            float rate = state == "DoubleJump" ? 22 : state == "Smash" ? 18 : state == "Dash" || state == "Combo2" || state == "Combo3" ? 20 : 12;
            var clip = MakeClip(dir + "/" + state + ".anim", poses, rate, state == "Charge");
            float length = poses.Length / rate;
            // Leaf scout flips; fire scout tucks and kicks. Pivot stays fixed at the feet.
            float spin = state == "DoubleJump" ? (fern ? -14 : 18) : state == "Block" ? (fern ? 8 : -8) : state == "Charge" ? (fern ? -9 : -14) : state == "Smash" ? (fern ? 18 : -22) : (fern ? -14 : -20);
            var rotation = state == "DoubleJump" || state == "Charge" ? new AnimationCurve(new Keyframe(0,0),new Keyframe(length*.45f,spin),new Keyframe(length,0)) : AnimationCurve.EaseInOut(0,0,length,spin);
            clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", rotation);
            var node = controller.layers[0].stateMachine.AddState(state); node.motion = clip;
        }
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
        var root = new GameObject("HUD - Arena Broadcast", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
        var hud = root.AddComponent<WoodlandHUD>(); hud.game = game; game.hud = hud;
        var pixel = font; var body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Color ink = new Color(.025f,.055f,.065f,1), track = new Color(.12f,.19f,.2f), white = new Color(.92f,.96f,.91f);
        var header=Flat("Header Contrast Fade",root.transform,new Rect(0,0,1280,110),Color.white);header.sprite=SpriteAt("Assets/Art/UI/HeaderShade.png");
        header.rectTransform.anchorMin=new Vector2(0,1);header.rectTransform.anchorMax=new Vector2(1,1);header.rectTransform.pivot=new Vector2(.5f,1);header.rectTransform.anchoredPosition=Vector2.zero;header.rectTransform.sizeDelta=new Vector2(0,110);
        font = pixel;
        LabelAnchored("Wordmark",root.transform,"WOODLAND TUMBLE",new Vector2(.5f,1),new Vector2(0,-28),new Vector2(650,30),18,white);
        font = body;
        hud.arenaText = Label("Arena",root.transform,"SUNLIT HOLLOW  /  TAB SWITCH",new Rect(0,0,600,22),14,new Color(.7f,.78f,.72f),TextAnchor.MiddleCenter);
        var ar = hud.arenaText.rectTransform; ar.anchorMin = ar.anchorMax = new Vector2(.5f,1); ar.pivot = new Vector2(.5f,1); ar.anchoredPosition = new Vector2(0,-48);
        for(int i=0;i<2;i++) {
            Color accent = i==0 ? hud.fernColor : hud.emberColor;
            var plate = new GameObject(i==0?"Fern Fighter Plate":"Ember Fighter Plate",typeof(RectTransform)).GetComponent<RectTransform>();
            plate.SetParent(root.transform,false); plate.anchorMin = plate.anchorMax = new Vector2(i,0); plate.pivot = new Vector2(i,0); plate.anchoredPosition = new Vector2(i==0?24:-24,26); plate.sizeDelta = new Vector2(410,122);
            hud.fighterPlates[i] = plate;
            Flat("Plate",plate,new Rect(0,0,410,122),ink);
            Flat("Accent Rule",plate,new Rect(12,0,386,1),accent);
            Flat("Portrait Well",plate,new Rect(12,12,96,98),new Color(accent.r*.17f,accent.g*.17f,accent.b*.17f,.8f));
            var portraitMask=Box("Portrait Crop",plate,new Rect(12,12,96,98));portraitMask.gameObject.AddComponent<RectMask2D>();
            var portrait=Flat("Scout Portrait",portraitMask,new Rect(-40,-16,176,176),Color.white); portrait.sprite = SpriteAt("Assets/Art/Characters/"+(i==0?"Fern":"Ember")+"/Walk/01.png"); portrait.preserveAspect=true;
            hud.names[i]=Label("Player Name",plate,i==0?"P1  FERN":"P2  EMBER",new Rect(122,9,178,22),18,accent);
            for(int j=0;j<3;j++){hud.roundPips[i*3+j]=Flat("Round "+(j+1),plate,new Rect(342+j*19,17,9,9),accent);hud.roundPips[i*3+j].rectTransform.localRotation=Quaternion.Euler(0,0,45);}
            hud.healthText[i]=Label("Health Number",plate,"100",new Rect(122,28,95,48),39,white);hud.healthText[i].fontStyle=FontStyle.Bold;
            Label("HP Caption",plate,"HP",new Rect(213,45,28,19),12,new Color(.57f,.66f,.65f));
            hud.damageText[i]=Label("Knockback",plate,"+0%",new Rect(270,45,122,22),17,new Color(.79f,.7f,.48f),TextAnchor.MiddleRight);
            Flat("Health Track",plate,new Rect(122,78,270,6),track);
            hud.damageGhost[i]=Flat("Damage Echo",plate,new Rect(122,78,270,6),new Color(1,.86f,.63f)); hud.damageGhost[i].type=Image.Type.Filled;hud.damageGhost[i].fillMethod=Image.FillMethod.Horizontal;
            hud.healthFill[i]=Flat("Health Fill",plate,new Rect(122,78,270,6),accent);hud.healthFill[i].type=Image.Type.Filled;hud.healthFill[i].fillMethod=Image.FillMethod.Horizontal;
            Flat("Ward Track",plate,new Rect(122,88,270,3),track);
            hud.shieldFill[i]=Flat("Ward",plate,new Rect(122,88,270,3),new Color(.5f,.79f,.95f));hud.shieldFill[i].type=Image.Type.Filled;hud.shieldFill[i].fillMethod=Image.FillMethod.Horizontal;
            hud.shieldText[i]=Label("Ward Caption",plate,"WARD",new Rect(122,96,50,15),11,new Color(.57f,.7f,.76f));
            Label("Push Caption",plate,"PUSH",new Rect(188,96,35,15),11,new Color(.65f,.72f,.69f));
            Label("Punch Caption",plate,"PUNCH",new Rect(292,96,43,15),11,new Color(.65f,.72f,.69f));
            Flat("Push Track",plate,new Rect(231,102,44,3),track);Flat("Punch Track",plate,new Rect(346,102,46,3),track);
            hud.pushFill[i]=Flat("Push Ready",plate,new Rect(231,102,44,3),accent);hud.pushFill[i].type=Image.Type.Filled;hud.pushFill[i].fillMethod=Image.FillMethod.Horizontal;
            hud.punchFill[i]=Flat("Punch Ready",plate,new Rect(346,102,46,3),accent);hud.punchFill[i].type=Image.Type.Filled;hud.punchFill[i].fillMethod=Image.FillMethod.Horizontal;
            hud.statusText[i]=Label("Fighter Combo Popup",root.transform,"",new Rect(0,0,280,45),25,accent,TextAnchor.MiddleCenter);
            hud.statusText[i].rectTransform.pivot=Vector2.one*.5f;
            var comboOutline=hud.statusText[i].gameObject.AddComponent<Outline>();comboOutline.effectColor=new Color(.015f,.035f,.04f,1);comboOutline.effectDistance=new Vector2(1,-1);
        }
        var footer=Flat("Quiet Controls Strip",root.transform,new Rect(0,0,390,30),ink);footer.rectTransform.anchorMin=footer.rectTransform.anchorMax=new Vector2(.5f,0);footer.rectTransform.pivot=new Vector2(.5f,0);footer.rectTransform.anchoredPosition=new Vector2(0,7);
        hud.helpHint=Label("Help Hint",root.transform,"F1 CONTROLS    ESC PAUSE    R REMATCH",new Rect(0,0,390,22),14,new Color(.82f,.87f,.82f),TextAnchor.MiddleCenter);
        var hintRect=hud.helpHint.rectTransform;hintRect.anchorMin=hintRect.anchorMax=new Vector2(.5f,0);hintRect.pivot=Vector2.one*.5f;hintRect.anchoredPosition=new Vector2(0,23);
        var banner = new GameObject("Round Announcement",typeof(RectTransform)).GetComponent<RectTransform>();banner.SetParent(root.transform,false);banner.anchorMin=banner.anchorMax=Vector2.one*.5f;banner.pivot=Vector2.one*.5f;banner.sizeDelta=new Vector2(520,100);banner.anchoredPosition=new Vector2(0,130);
        Flat("Announcement Backing",banner,new Rect(0,0,520,100),new Color(.025f,.055f,.065f,.90f));Flat("Announcement Rule",banner,new Rect(160,0,200,2),gold);
        hud.bannerPanel=banner.gameObject;hud.bannerText=Label("Announcement",banner,"GET READY",new Rect(10,8,500,84),30,white,TextAnchor.MiddleCenter);banner.gameObject.SetActive(false);
        var help = new GameObject("F1 Controls Overlay",typeof(RectTransform)).GetComponent<RectTransform>();help.SetParent(root.transform,false);help.anchorMin=help.anchorMax=Vector2.one*.5f;help.pivot=Vector2.one*.5f;help.sizeDelta=new Vector2(800,370);
        Flat("Help Backing",help,new Rect(0,0,800,370),new Color(.02f,.045f,.055f,.98f));Flat("Help Rule",help,new Rect(28,26,744,2),gold);
        Label("Help Heading",help,"THE FIELD GUIDE",new Rect(30,40,740,34),26,white,TextAnchor.MiddleCenter);
        for(int i=0;i<2;i++){
            float x=36+i*395;Color c=i==0?hud.fernColor:hud.emberColor;
            Label("Player",help,i==0?"P1  FERN":"P2  EMBER",new Rect(x,100,330,25),22,c);
            Label("Keys",help,i==0?"A / D    Move\nW    Jump / double jump\nS    Guard / timed parry\nF    Shove   G    Punch\nH    Hold to charge smash\nLeft Shift    Dash":"Left / Right    Move\nUp    Jump / double jump\nDown    Guard / timed parry\nK    Shove   L    Punch\nJ    Hold to charge smash\nRight Shift    Dash",new Rect(x,136,340,165),18,white);
        }
        Label("System Keys",help,"TAB arena    R rematch    ESC pause    M mute    - / + volume    F1 close",new Rect(24,322,752,24),16,new Color(.71f,.77f,.7f),TextAnchor.MiddleCenter);
        hud.helpPanel=help.gameObject; help.gameObject.SetActive(false);
        hud.soundText=Label("Sound Feedback",root.transform,"",new Rect(0,0,330,24),16,white,TextAnchor.MiddleCenter);
        var soundRect=hud.soundText.rectTransform;soundRect.anchorMin=soundRect.anchorMax=new Vector2(.5f,0);soundRect.pivot=Vector2.one*.5f;soundRect.anchoredPosition=new Vector2(0,23);
        font=pixel;
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
    public static void BuildWindows()
    {
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Build/Windows/WoodlandTumble.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed");
    }
    public static void Build()
    {
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Build/Woodland Tumble.app", BuildTarget.StandaloneOSX, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Build failed");
    }
}
