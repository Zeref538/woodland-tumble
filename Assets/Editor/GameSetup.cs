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
    static readonly string[] States = { "Walk", "Jump", "Hurt", "Push", "Punch", "Kick", "Knockout" };
    static Font font;
    static Sprite panelSprite;
    static Color cream = new Color(1, .91f, .71f);
    static Color gold = new Color(.80f, .66f, .34f);
    public static void ImportComboArtwork()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var file in Directory.GetFiles("Assets/Resources/Art/Combo","*.png",SearchOption.AllDirectories))
            ImportSprite(file.Replace('\\','/'),256);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Woodland/Rebuild Editable Demo Scene")]
    public static void Prepare()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode before rebuilding the scene.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var file in Directory.GetFiles("Assets/Resources/Art", "*.png")) ImportSprite(file, 100);
        ImportComboArtwork();
        AssetDatabase.StartAssetEditing();
        try { foreach (var file in Directory.GetFiles("Assets/Art/CharactersRework", "*.png", SearchOption.AllDirectories)) {
            string normalized=file.Replace('\\','/');
            var importer=(TextureImporter)AssetImporter.GetAtPath(normalized);
            ImportSprite(normalized,80,importer.spritePivot);
        }} finally {AssetDatabase.StopAssetEditing();}
        foreach(var file in Directory.GetFiles("Assets/Art/PortraitsRework","*.png"))ImportSprite(file,100);
        foreach (var file in Directory.GetFiles("Assets/Art/VFX", "*.png", SearchOption.AllDirectories)) ImportSprite(file.Replace('\\','/'),256);
        foreach (var file in Directory.GetFiles("Assets/Art/Arena", "*.png", SearchOption.AllDirectories)) ImportSprite(file, 100);
        if (Directory.Exists("Assets/Art/Menu")) foreach (var file in Directory.GetFiles("Assets/Art/Menu", "*.png")) ImportSprite(file, 100);
        ImportSprite("Assets/Art/UI/WoodlandPanel.png", 100, null, new Vector4(12, 12, 12, 12));
        ImportSprite("Assets/Art/UI/Solid.png", 100);
        CreateFighterPlate();
        CreateActionGlyphs();
        CreateControlRipple();
        ImportSprite("Assets/Art/UI/Versus-RuneRumble.png",100);
        foreach(var file in Directory.GetFiles("Assets/Art/UI/Actions","*.png",SearchOption.AllDirectories)) {
            ImportSprite(file.Replace('\\','/'),256);
            var actionImporter=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));actionImporter.filterMode=FilterMode.Point;actionImporter.SaveAndReimport();
        }
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
        camera.orthographic = true; camera.orthographicSize = 5.8f; camera.allowHDR = true;
        camera.transform.position = new Vector3(0, 1.4f, -10);
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
        var effects = new GameObject("Effects - Trails and Splashes").AddComponent<WoodlandEffects>();effects.effectIntensity=1.35f;
        string[] cues={"swing","hit_light","hit_heavy","jump","double_jump","dash","land","block","parry","guard_break","knockout","victory","ui_click"};
        effects.fernSlash=Enumerable.Range(1,8).Select(i=>SpriteAt("Assets/Art/VFX/Fern/"+i.ToString("00")+".png")).ToArray();
        effects.emberSlash=Enumerable.Range(1,8).Select(i=>SpriteAt("Assets/Art/VFX/Ember/"+i.ToString("00")+".png")).ToArray();
        effects.soundBank=cues.Select(cue=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Combat/"+cue+".ogg")).ToArray();
        if(effects.soundBank.Any(clip=>!clip))throw new System.Exception("Missing action sound effect");
        var environment = new GameObject("Environment - Edit These Objects").transform;
        var bg = Visual("Grounded Woodland Arena", SpriteAt("Assets/Resources/Art/Grounded/Forest.png"), new Vector2(0, 1), -20, environment);
        Fit(bg, 22, 12.5f);
        var ball = Visual("Runewood Balancing Sphere", SpriteAt("Assets/Art/Arena/RunewoodSphere.png"), new Vector2(0, -1.56f), 1, environment);
        Fit(ball, 1.85f, 1.85f);
        var beam = Visual("Arena Floor Collision", SpriteAt("Assets/Art/Arena/MossboundPlank.png"), new Vector2(0, -1.7f), 3, environment);
        beam.GetComponent<SpriteRenderer>().enabled=false;ball.SetActive(false);
        beam.transform.localScale=Vector3.one;
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
        game.backgrounds = new[]{"Forest","Moonlit","Ember","Skyward","Stormglass","Hollowroot"}.Select(theme=>SpriteAt("Assets/Resources/Art/Grounded/"+theme+".png")).ToArray();
        game.planks = new[] { SpriteAt("Assets/Art/Arena/MossboundPlank.png"), SpriteAt("Assets/Art/Arena/MoonlitPlank.png"), SpriteAt("Assets/Art/Arena/EmberPlank.png"), SpriteAt("Assets/Art/Arena/SkywardPlank.png") };
        game.spheres = new[] { SpriteAt("Assets/Art/Arena/RunewoodSphere.png"), SpriteAt("Assets/Art/Arena/MoonstoneSphere.png"), SpriteAt("Assets/Art/Arena/ObsidianSphere.png"), SpriteAt("Assets/Art/Arena/CrystalSphere.png") };
        string[] pvpThemes={"Forest","Moonlit","Ember","Skyward"};
        game.PvpBases=pvpThemes.Select(theme=>SpriteAt("Assets/Art/Arena/PVP/"+theme+"/Base.png")).ToArray();
        game.PvpLedges=pvpThemes.Select(theme=>SpriteAt("Assets/Art/Arena/PVP/"+theme+"/Ledge.png")).ToArray();
        if(game.PvpBases.Any(sprite=>!sprite)||game.PvpLedges.Any(sprite=>!sprite))throw new System.Exception("Missing dedicated Arena PVP base or ledge artwork");
        game.arenaPlatforms = new Rigidbody2D[0];
        for(int i=0;i<game.CharacterNames.Length;i++) {
            string identity=game.CharacterNames[i];
            game.CharacterAnimationSets[i]=MakeAnimations(identity);
            game.CharacterControllers[i]=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/AnimationsRework/"+identity+"/"+identity+".controller");
            game.CharacterPortraits[i]=SpriteAt("Assets/Art/PortraitsRework/"+identity+".png") ?? game.CharacterAnimationSets[i].walk[0];
        }
        var actors = new GameObject("Players - Editable Prefab Instances").transform;
        var spawns = new GameObject("Spawn Points - Move These To Change Starting Positions").transform;
        for (int p = 0; p < 2; p++) {
            string name = p == 0 ? "Fern" : "Ember";
            ScoutAnimations set = game.CharacterAnimationSets[p];
            var spawn = new GameObject(name + " Spawn").transform;spawn.SetParent(spawns);spawn.position = new Vector3(p == 0 ? -2.7f : 2.7f, -.6f, 0);
            spawn.gameObject.AddComponent<SpawnMarker>().color = p == 0 ? Color.green : new Color(1, .5f, .2f);
            var root = new GameObject(name + " - Player " + (p + 1));root.transform.SetParent(actors);root.transform.position = spawn.position;
            var body = root.AddComponent<Rigidbody2D>();body.mass = 1;body.freezeRotation = true;body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var capsule = root.AddComponent<CapsuleCollider2D>();capsule.size = new Vector2(1.05f, 2.375f);capsule.sharedMaterial = PlayerSlideMaterial();
            var sprite = Visual("Character - Animated Sprites", set.walk[0], Vector2.zero, 5, root.transform);
            sprite.transform.localPosition = new Vector3(0, -1.1875f, 0);
            sprite.GetComponent<SpriteRenderer>().flipX = p == 1;
            var anim = sprite.AddComponent<Animator>();anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/AnimationsRework/" + name + "/" + name + ".controller");anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var scout = root.AddComponent<Scout>();scout.id = p;scout.displayName = name;scout.art = sprite.GetComponent<SpriteRenderer>();scout.animator = anim;scout.animations = set;
            string prefabPath = "Assets/Prefabs/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
            scout.spawnPoint = spawn; // Scene-specific reference remains an editable prefab override.
            PrefabUtility.RecordPrefabInstancePropertyModifications(scout);
            game.players[p] = scout;
        }
        CreateHUD(game);
        var events = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var uiInput=events.GetComponent<InputSystemUIInputModule>();uiInput.AssignDefaultActions();
        // HUD owns menu navigation; this module handles pointer clicks only.
        uiInput.move=null;uiInput.submit=null;uiInput.cancel=null;
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        PlayerSettings.companyName = "Woodland Games";PlayerSettings.productName = "Rune Rumble";PlayerSettings.bundleVersion="8.1.0";
        PlayerSettings.defaultScreenWidth = 1280;PlayerSettings.defaultScreenHeight = 720;PlayerSettings.runInBackground = true;
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = beam;
        SceneView.RepaintAll();
        Debug.Log("WOODLAND_EDITABLE_READY: eight character catalogs, six grounded maps, ring-out duels and editable HUD saved.");
    }
    public static void PrepareHUD()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode before rebuilding the HUD.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/WoodlandTumble.unity");
        var game=Object.FindAnyObjectByType<WoodlandGame>();
        if(!game)throw new System.Exception("Saved game scene is missing its game component.");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PressStart2P-Regular.ttf");
        panelSprite=SpriteAt("Assets/Art/UI/WoodlandPanel.png");
        if(game.hud)Object.DestroyImmediate(game.hud.gameObject);
        CreateHUD(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static void ImportSprite(string path, float ppu, Vector2? pivot = null, Vector4? border = null)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) throw new System.Exception("Missing art: " + path);
        var existing=new TextureImporterSettings();imp.ReadTextureSettings(existing);
        if(imp.textureType==TextureImporterType.Sprite && imp.spriteImportMode==SpriteImportMode.Single && Mathf.Approximately(imp.spritePixelsPerUnit,ppu)
            && existing.spriteMeshType==SpriteMeshType.FullRect && existing.spriteAlignment==(int)SpriteAlignment.Custom && existing.spritePivot==(pivot??Vector2.one*.5f)
            && (!border.HasValue || imp.spriteBorder==border.Value) && imp.filterMode==FilterMode.Point && imp.textureCompression==TextureImporterCompression.Uncompressed
            && imp.alphaIsTransparency && !imp.mipmapEnabled && imp.isReadable && imp.maxTextureSize==2048)return;
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
        string dir = "Assets/AnimationsRework/" + name;Directory.CreateDirectory(dir);AssetDatabase.Refresh();
        string setPath = dir + "/Frames.asset";
        var set = AssetDatabase.LoadAssetAtPath<ScoutAnimations>(setPath);
        if (!set) { set = ScriptableObject.CreateInstance<ScoutAnimations>();AssetDatabase.CreateAsset(set, setPath); }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(dir + "/" + name + ".controller");
        if (controller) AssetDatabase.DeleteAsset(dir + "/" + name + ".controller");
        controller = AnimatorController.CreateAnimatorControllerAtPath(dir + "/" + name + ".controller");
        foreach (string state in States) {
            string source=state;
            var sprites = Enumerable.Range(1, 10).Select(i => SpriteAt("Assets/Art/CharactersRework/" + name + "/" + source + "/" + i.ToString("00") + ".png")).ToArray();
            if (sprites.Any(s => s == null)) throw new System.Exception("Incomplete animation: " + name + "/" + state);
            switch (state) { case "Walk":set.walk = sprites;break;case "Jump":set.jump = sprites;break;case "Hurt":set.hurt = sprites;break;case "Push":set.push = sprites;break;case "Punch":set.punch = sprites;break;case "Kick":set.kick=sprites;break;case "Knockout":set.knockout = sprites;break; }
            float rate = state == "Push" || state == "Punch" ? 36 : state=="Kick" ? 33 : state == "Knockout" ? 30 : state == "Hurt" ? 22 : state == "Jump" ? 12 : 10;
            var clip = MakeClip(dir + "/" + state + ".anim", sprites, rate, state == "Walk");
            var node = controller.layers[0].stateMachine.AddState(state);node.motion = clip;
        }
        MakeCombatClips(name, dir, set, controller);
        var idleSprite=SpriteAt("Assets/Art/CharactersRework/"+name+"/Idle/01.png") ?? set.walk[0];
        var idleClip = MakeClip(dir + "/Idle.anim", new[] { idleSprite }, 1, true);
        var idle = controller.layers[0].stateMachine.AddState("Idle");idle.motion = idleClip;controller.layers[0].stateMachine.defaultState = idle;
        EditorUtility.SetDirty(set);EditorUtility.SetDirty(controller);
        return set;
    }
    static void MakeCombatClips(string name, string dir, ScoutAnimations set, AnimatorController controller)
    {
        bool fern = name == "Fern";
        set.downed=Enumerable.Range(1,10).Select(i=>SpriteAt("Assets/Art/CharactersRework/"+name+"/Downed/"+i.ToString("00")+".png")).ToArray();
        set.getUp=Enumerable.Range(1,10).Select(i=>SpriteAt("Assets/Art/CharactersRework/"+name+"/GetUp/"+i.ToString("00")+".png")).ToArray();
        set.block = Enumerable.Range(1,10).Select(i => SpriteAt("Assets/Art/CharactersRework/" + name + "/Block/" + i.ToString("00") + ".png")).ToArray();
        string airSource="DoubleJump";
        set.doubleJump = Enumerable.Range(1,10).Select(i => SpriteAt("Assets/Art/CharactersRework/" + name + "/"+airSource+"/" + i.ToString("00") + ".png")).ToArray();
        set.special=Enumerable.Range(1,10).Select(i=>SpriteAt("Assets/Art/CharactersRework/"+name+"/Special/"+i.ToString("00")+".png")).ToArray();
        set.dash=Enumerable.Range(1,10).Select(i=>SpriteAt("Assets/Art/CharactersRework/"+name+"/Dash/"+i.ToString("00")+".png")).ToArray();
        set.backstep=Enumerable.Range(1,10).Select(i=>SpriteAt("Assets/Art/CharactersRework/"+name+"/Backstep/"+i.ToString("00")+".png")).ToArray();
        set.victory=Enumerable.Range(1,10).Select(i=>SpriteAt("Assets/Art/CharactersRework/"+name+"/Victory/"+i.ToString("00")+".png")).ToArray();
        set.ultimate=Enumerable.Range(1,10).Select(i=>SpriteAt("Assets/Art/CharactersRework/"+name+"/Ultimate/"+i.ToString("00")+".png")).ToArray();
        if(set.dash.Concat(set.backstep).Concat(set.victory).Concat(set.ultimate).Any(sprite=>!sprite))throw new System.Exception("Missing v7 movement, victory or ultimate poses for "+name);
        if(set.special.Any(s=>!s))throw new System.Exception("Missing special frames for "+name);
        if (set.block.Any(s => !s) || set.doubleJump.Any(s => !s)) throw new System.Exception("Missing dedicated combat frames for " + name);
        foreach (string state in new[] { "Downed", "GetUp", "Block", "DoubleJump", "Dash", "Backstep", "Grab", "Charge", "Smash", "Combo2", "Combo3", "Victory", "Ultimate" }) {
            Sprite[] poses = state == "Downed" ? set.downed : state == "GetUp" ? set.getUp : state == "Block" ? set.block
                : state == "Charge" ? new[] { set.special[0],set.special[1],set.special[2],set.special[1] }
                : state == "DoubleJump" ? set.doubleJump
                : state == "Dash" ? set.dash
                : state == "Backstep" ? set.backstep
                : state == "Victory" ? set.victory
                : state == "Ultimate" ? set.ultimate
                : state == "Grab" ? set.punch
                : state == "Combo2" ? (fern ? set.punch : set.kick)
                : state == "Combo3" ? (fern ? set.push.Reverse().ToArray() : set.punch.Reverse().ToArray())
                : set.special;
            float rate = state=="Downed"?10:state=="GetUp"?16.667f:state=="Dash"?64:state=="Backstep"?72:state=="Victory"?12:state=="Ultimate"?10:state=="DoubleJump"?22:state=="Smash"?26:state=="Combo2"||state=="Combo3"?35:12;
            var clip = MakeClip(dir + "/" + state + ".anim", poses, rate, state == "Downed" || state == "Charge" || state=="Victory");
            float length = poses.Length / rate;
            // Leaf scout flips; fire scout tucks and kicks. Pivot stays fixed at the feet.
            float spin = state=="Downed"||state=="GetUp"||state=="Dash"||state=="Backstep"||state=="Victory"||state=="Ultimate"?0:state == "DoubleJump" ? (fern ? -14 : 18) : state == "Block" ? (fern ? 8 : -8) : state == "Charge" ? (fern ? -9 : -14) : state == "Smash" ? (fern ? 18 : -22) : (fern ? -14 : -20);
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
        var match = MenuLayer("Match HUD",root.transform); hud.matchHud=match.gameObject;
        var pixel = font; var body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Color ink = new Color(.025f,.055f,.065f,1), track = new Color(.12f,.19f,.2f), white = new Color(.92f,.96f,.91f);
        var header=Flat("Header Contrast Fade",match,new Rect(0,0,1280,110),new Color(1,1,1,.25f));header.sprite=SpriteAt("Assets/Art/UI/HeaderShade.png");
        header.rectTransform.anchorMin=new Vector2(0,1);header.rectTransform.anchorMax=new Vector2(1,1);header.rectTransform.pivot=new Vector2(.5f,1);header.rectTransform.anchoredPosition=Vector2.zero;header.rectTransform.sizeDelta=new Vector2(0,110);
        font = body;
        var versus=Flat("Versus Emblem",match,new Rect(0,0,168,94),Color.white);versus.sprite=SpriteAt("Assets/Art/UI/Versus-RuneRumble.png");versus.preserveAspect=true;var vsRect=versus.rectTransform;vsRect.anchorMin=vsRect.anchorMax=new Vector2(.5f,1);vsRect.pivot=new Vector2(.5f,1);vsRect.anchoredPosition=new Vector2(0,-18);
        font=pixel;
        hud.roundTimerText=Label("Round Timer",match,"99\n<size=11>BEST OF 5</size>",new Rect(0,0,150,70),28,white,TextAnchor.UpperCenter);
        hud.roundTimerText.supportRichText=true;
        var timerRect=hud.roundTimerText.rectTransform;timerRect.anchorMin=timerRect.anchorMax=new Vector2(.5f,1);
        timerRect.pivot=new Vector2(.5f,1);timerRect.anchoredPosition=new Vector2(0,-110);
        font=body;
        for(int a=0;a<4;a++){hud.playstationIcons[a]=SpriteAt("Assets/Art/UI/PSAction"+a+".png");hud.neutralIcons[a]=SpriteAt("Assets/Art/UI/CPUAction"+a+".png");}
        for(int c=0;c<game.CharacterNames.Length;c++){
            hud.characterGuardCovers[c]=SpriteAt("Assets/Art/UI/Actions/"+game.CharacterNames[c]+"/Guard.png");
            if(!hud.characterGuardCovers[c])throw new System.Exception("Missing pixel shield icon: "+game.CharacterNames[c]);
        }
        string[] actionNames={"Jump","Punch","Kick","Special"};
        for(int character=0;character<game.CharacterNames.Length;character++)for(int action=0;action<4;action++) {
            var cover=SpriteAt("Assets/Art/UI/Actions/"+game.CharacterNames[character]+"/"+actionNames[action]+".png");
            if(!cover)throw new System.Exception("Missing character action cover: "+game.CharacterNames[character]+"/"+actionNames[action]);
            hud.characterActionCovers[character*4+action]=cover;
        }
        for(int i=0;i<2;i++) {
            Color accent = i==0 ? hud.fernColor : hud.emberColor;
            var plate = new GameObject(i==0?"Fern Fighter Plate":"Ember Fighter Plate",typeof(RectTransform)).GetComponent<RectTransform>();
            plate.SetParent(match,false);plate.anchorMin=plate.anchorMax=new Vector2(i,1);plate.pivot=new Vector2(i,1);plate.anchoredPosition=new Vector2(i==0?24:-24,-18);plate.sizeDelta=new Vector2(590,WoodlandHUD.FighterHudHeight);plate.localScale=Vector3.one*WoodlandHUD.FighterHudScale;
            hud.fighterPlates[i]=plate;
            float portraitX=i==0?0:490,meterX=i==0?120:18;TextAnchor side=i==0?TextAnchor.MiddleLeft:TextAnchor.MiddleRight;
            var portraitMask=Box("Circular Portrait Mask",plate,new Rect(portraitX,5,100,100));
            var maskImage=portraitMask.gameObject.AddComponent<Image>();maskImage.sprite=SpriteAt("Assets/Art/UI/ControlDisc.png");maskImage.raycastTarget=false;
            var mask=portraitMask.gameObject.AddComponent<Mask>();mask.showMaskGraphic=false;
            var portrait=Flat("Scout Portrait",portraitMask,new Rect(0,0,100,100),Color.white);portrait.sprite=game.CharacterPortraits[i];portrait.preserveAspect=true;hud.fighterPortraits[i]=portrait;
            if(i==1){var portraitRect=portrait.rectTransform;portraitRect.pivot=Vector2.one*.5f;portraitRect.anchoredPosition+=new Vector2(50,-50);portraitRect.localScale=new Vector3(-1,1,1);}
            hud.fighterPortraitRings[i]=Flat("Portrait Fine Ring",plate,new Rect(portraitX-5,0,110,110),new Color(accent.r,accent.g,accent.b,.72f));hud.fighterPortraitRings[i].sprite=SpriteAt("Assets/Art/UI/ControlRipple.png");
            Flat("Stamina Rail",plate,new Rect(meterX-2,65,456,25),new Color(.07f,.075f,.045f,.82f));
            hud.shieldFill[i]=Flat("Stamina Fill",plate,new Rect(meterX,67,452,21),new Color(.94f,.78f,.39f));hud.shieldFill[i].type=Image.Type.Filled;hud.shieldFill[i].fillMethod=Image.FillMethod.Horizontal;hud.shieldFill[i].fillOrigin=i;
            hud.shieldText[i]=Label("Stamina Number",plate,"STAMINA",new Rect(meterX,67,452,21),12,white,TextAnchor.MiddleCenter);hud.shieldText[i].fontStyle=FontStyle.Bold;
            var staminaOutline=hud.shieldText[i].gameObject.AddComponent<Outline>();staminaOutline.effectColor=new Color(.08f,.065f,.02f,1);staminaOutline.effectDistance=new Vector2(1,-1);
            hud.names[i]=Label("Player Name",plate,i==0?"P1  FERN":"P2  EMBER",new Rect(meterX,95,452,30),22,accent,side);
            Flat("Five Hit Meter Rail",plate,new Rect(meterX-2,16,456,40),new Color(.025f,.04f,.045f,.9f));
            for(int hit=0;hit<Scout.HitsToKnockdown;hit++) {
                float squareX=i==0?meterX+hit*91.2f:meterX+452-87.2f-hit*91.2f;
                hud.hitSquares[i*Scout.HitsToKnockdown+hit]=Flat("Hit Square P"+(i+1)+" "+(hit+1),plate,new Rect(squareX,18,87.2f,36),accent);
            }
            for(int j=0;j<3;j++){hud.roundPips[i*3+j]=Flat("Round "+(j+1),plate,new Rect(i==0?516+j*19:18+j*19,106,11,11),accent);hud.roundPips[i*3+j].rectTransform.localRotation=Quaternion.Euler(0,0,45);}
            hud.deviceText[i]=Label("Input Device",plate,"KEYBOARD",new Rect(meterX,130,452,20),14,new Color(.84f,.91f,.85f),side);
            var ultimatePanel=Box("Ultimate Ready Callout",plate,new Rect(0,160,410,34));
            Flat("Ultimate Rail",ultimatePanel,new Rect(0,0,410,34),new Color(.035f,.055f,.06f,.75f));
            hud.ultimateLabels[i]=Label("Ultimate State",ultimatePanel,"",new Rect(7,3,396,28),15,new Color(1,.87f,.52f),TextAnchor.MiddleCenter);hud.ultimatePanels[i]=ultimatePanel.gameObject;
            var cutin=Box("Ultimate Character Cut-in",match,new Rect(i==0?35:925,178,320,270));
            var cutinGroup=cutin.gameObject.AddComponent<CanvasGroup>();cutinGroup.interactable=false;cutinGroup.blocksRaycasts=false;
            hud.ultimateCutinArt[i]=Flat("Cinematic Character",cutin,new Rect(35,0,250,210),Color.white);hud.ultimateCutinArt[i].preserveAspect=true;
            hud.ultimateCutinNames[i]=Label("Cinematic Move Name",cutin,"",new Rect(0,210,320,58),24,accent,TextAnchor.MiddleCenter);hud.ultimateCutinNames[i].fontStyle=FontStyle.Bold;
            var cutinOutline=hud.ultimateCutinNames[i].gameObject.AddComponent<Outline>();cutinOutline.effectColor=new Color(.02f,.035f,.04f,1);cutinOutline.effectDistance=new Vector2(2,-2);
            hud.ultimateCutins[i]=cutin.gameObject;cutin.gameObject.SetActive(false);
            var diamond=Box(i==0?"Fern Action Diamond":"Ember Action Diamond",match,new Rect(0,0,WoodlandHUD.ActionDiamondHeight,WoodlandHUD.ActionDiamondHeight));
            diamond.anchorMin=diamond.anchorMax=new Vector2(i,0);diamond.pivot=new Vector2(i,0);diamond.anchoredPosition=new Vector2(i==0?32:-32,32);
            Vector2[] buttonPositions={new Vector2(75,150),new Vector2(0,75),new Vector2(150,75),new Vector2(75,0)};
            string[] captions={"Guard","Punch","Kick","Special"};
            for(int a=0;a<4;a++){
                int slot=i*4+a;var position=buttonPositions[a];
                var disc=Flat("Face Button "+captions[a],diamond,new Rect(position.x,position.y,WoodlandHUD.ActionButtonDiameter,WoodlandHUD.ActionButtonDiameter),Color.white);disc.sprite=SpriteAt("Assets/Art/UI/ControlDisc.png");disc.raycastTarget=true;disc.alphaHitTestMinimumThreshold=.1f;disc.rectTransform.pivot=Vector2.one*.5f;disc.rectTransform.anchoredPosition+=new Vector2(40,-40);hud.actionDiscs[slot]=disc;
                var button=disc.gameObject.AddComponent<Button>();button.targetGraphic=disc;button.transition=Selectable.Transition.None;button.navigation=new Navigation{mode=Navigation.Mode.None};hud.actionButtons[slot]=button;
                hud.actionCovers[slot]=Flat("Illustrated "+captions[a],disc.transform,new Rect(0,0,80,80),Color.white);hud.actionCovers[slot].sprite=a==0?hud.characterGuardCovers[i]:hud.characterActionCovers[i*4+a];hud.actionCovers[slot].preserveAspect=true;
                if(a==3){
                    hud.specialCooldown[i]=Flat("Ranged Cooldown Sweep",disc.transform,new Rect(0,0,80,80),new Color(.018f,.03f,.035f,.8f));hud.specialCooldown[i].sprite=SpriteAt("Assets/Art/UI/ControlDisc.png");hud.specialCooldown[i].type=Image.Type.Filled;hud.specialCooldown[i].fillMethod=Image.FillMethod.Radial360;hud.specialCooldown[i].fillOrigin=(int)Image.Origin360.Top;hud.specialCooldown[i].fillClockwise=false;
                    hud.specialCooldownText[i]=Label("Ranged Cooldown Seconds",disc.transform,"",new Rect(0,19,80,43),29,white,TextAnchor.MiddleCenter);hud.specialCooldownText[i].fontStyle=FontStyle.Bold;
                    var outline=hud.specialCooldownText[i].gameObject.AddComponent<Outline>();outline.effectColor=Color.black;outline.effectDistance=new Vector2(1,-1);
                }
                hud.actionRipples[slot]=Flat("Action Accepted Ripple",disc.transform,new Rect(-4,-4,88,88),Color.clear);hud.actionRipples[slot].sprite=SpriteAt("Assets/Art/UI/ControlRipple.png");hud.actionRipples[slot].rectTransform.pivot=Vector2.one*.5f;hud.actionRipples[slot].rectTransform.anchoredPosition+=new Vector2(44,-44);
                hud.actionRejectMarks[slot]=Flat("Unavailable Tick",disc.transform,new Rect(29,70,22,3),Color.clear);
                var badge=Flat("Input Badge",disc.transform,new Rect(52,-4,32,32),new Color(.95f,.97f,.95f));badge.sprite=SpriteAt("Assets/Art/UI/ControlDisc.png");
                hud.actionIcons[slot]=Flat("Device Shape",badge.transform,new Rect(1,1,30,30),Color.white);hud.actionIcons[slot].sprite=hud.playstationIcons[a];hud.actionIcons[slot].preserveAspect=true;
                hud.actionGlyphs[slot]=Label("Device Letter",badge.transform,"",new Rect(0,0,32,32),18,white,TextAnchor.MiddleCenter);hud.actionGlyphs[slot].fontStyle=FontStyle.Bold;
            }
            var tooltip=Box("Action Tooltip",diamond,new Rect(0,-54,230,46));
            Flat("Tooltip Rail",tooltip,new Rect(0,0,230,46),new Color(.025f,.055f,.065f,.87f));
            hud.actionTooltips[i]=Label("Action Description",tooltip,"",new Rect(6,3,218,40),14,new Color(.92f,.95f,.87f),TextAnchor.MiddleCenter);hud.actionTooltipPanels[i]=tooltip.gameObject;tooltip.gameObject.SetActive(false);
            hud.statusText[i]=Label("Fighter Combo Popup",match,"",new Rect(0,0,280,45),25,accent,TextAnchor.MiddleCenter);hud.statusText[i].rectTransform.pivot=Vector2.one*.5f;
            var comboOutline=hud.statusText[i].gameObject.AddComponent<Outline>();comboOutline.effectColor=new Color(.015f,.035f,.04f,1);comboOutline.effectDistance=new Vector2(1,-1);
        }
        var footer=Flat("Quiet Controls Strip",match,new Rect(0,0,210,30),new Color(ink.r,ink.g,ink.b,.5f));footer.rectTransform.anchorMin=footer.rectTransform.anchorMax=new Vector2(.5f,0);footer.rectTransform.pivot=new Vector2(.5f,0);footer.rectTransform.anchoredPosition=new Vector2(0,7);
        hud.helpHint=Label("Help Hint",match,"MENU  [ESC]",new Rect(0,0,210,22),14,new Color(.82f,.87f,.82f),TextAnchor.MiddleCenter);
        foreach(var text in hud.names.Concat(hud.deviceText).Concat(new[]{hud.helpHint})) {
            var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.04f,.035f,1);outline.effectDistance=new Vector2(1,-1);
        }
        var hintRect=hud.helpHint.rectTransform;hintRect.anchorMin=hintRect.anchorMax=new Vector2(.5f,0);hintRect.pivot=Vector2.one*.5f;hintRect.anchoredPosition=new Vector2(0,23);
        var banner = new GameObject("Round Announcement",typeof(RectTransform)).GetComponent<RectTransform>();banner.SetParent(match,false);banner.anchorMin=banner.anchorMax=Vector2.one*.5f;banner.pivot=Vector2.one*.5f;banner.sizeDelta=new Vector2(520,100);banner.anchoredPosition=new Vector2(0,130);
        Flat("Announcement Backing",banner,new Rect(0,0,520,100),new Color(.025f,.055f,.065f,.90f));Flat("Announcement Rule",banner,new Rect(160,0,200,2),gold);
        hud.bannerPanel=banner.gameObject;hud.bannerText=Label("Announcement",banner,"GET READY",new Rect(10,8,500,84),30,white,TextAnchor.MiddleCenter);banner.gameObject.SetActive(false);
        var help = new GameObject("F1 Controls Overlay",typeof(RectTransform)).GetComponent<RectTransform>();help.SetParent(root.transform,false);help.anchorMin=help.anchorMax=Vector2.one*.5f;help.pivot=Vector2.one*.5f;help.sizeDelta=new Vector2(1140,636);
        Flat("Guide Surface",help,new Rect(0,0,1140,636),new Color(.02f,.045f,.055f,1));Flat("Guide Rule",help,new Rect(32,22,1076,2),gold);
        Label("Help Heading",help,"MOVE LIST",new Rect(36,30,800,40),28,white);
        Label("Guide Subtitle",help,"Your selected rivals. Their moves and counterplay.",new Rect(36,72,1000,25),17,new Color(.72f,.8f,.75f));
        Flat("Guide Divider",help,new Rect(570,112,1,450),new Color(.22f,.32f,.32f));
        for(int i=0;i<2;i++){
            float x=36+i*566;
            hud.guideNames[i]=Label("Guide Scout",help,"",new Rect(x,111,500,30),22,i==0?hud.fernColor:hud.emberColor);
            hud.controlsText[i]=Label("Move Explanations",help,"",new Rect(x,152,500,425),17,white,TextAnchor.UpperLeft);hud.controlsText[i].supportRichText=true;
        }
        hud.guideCloseButton=MenuButton("Close Move List",help,new Rect(870,580,234,42),"BACK","",gold);
        Label("Guide Footer",help,"ESC / OPTIONS / MENU: back     EAST BUTTON: back",new Rect(36,590,810,25),14,new Color(.72f,.8f,.75f));
        hud.helpPanel=help.gameObject;help.gameObject.SetActive(false);

        CreateMenus(hud,root.transform,body,pixel);
        font=pixel;
    }
    static RectTransform MenuLayer(string name,Transform parent)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
    }
    static void MenuBackdrop(string name,Transform parent,string path)
    {
        var rect=MenuLayer(name,parent);var image=rect.gameObject.AddComponent<Image>();image.sprite=SpriteAt(path);image.raycastTarget=false;
        if(image.sprite){var fit=rect.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=image.sprite.rect.width/image.sprite.rect.height;}
    }
    static Button MenuButton(string name,Transform parent,Rect bounds,string title,string subtitle,Color accent)
    {
        var face=Flat(name,parent,bounds,new Color(.035f,.075f,.075f,.97f));face.raycastTarget=true;
        var outline=face.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(1,-1);outline.effectColor=new Color(.32f,.4f,.34f,.8f);
        Flat("Upper Seam",face.transform,new Rect(14,0,bounds.width-28,1),new Color(accent.r,accent.g,accent.b,.48f));
        Label("Choice",face.transform,title,new Rect(24,subtitle.Length>0?13:9,bounds.width-48,30),22,new Color(.94f,.95f,.85f));
        if(subtitle.Length>0)Label("Description",face.transform,subtitle,new Rect(24,46,bounds.width-48,23),15,new Color(.67f,.76f,.69f));
        var button=face.gameObject.AddComponent<Button>();button.targetGraphic=face;
        button.navigation=new Navigation{mode=Navigation.Mode.None};var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.25f,1.25f,1.2f);colors.pressedColor=new Color(.6f,.8f,.68f);colors.selectedColor=Color.white;colors.fadeDuration=.10f;button.colors=colors;
        return button;
    }
    static void CreateMenus(WoodlandHUD hud,Transform canvas,Font body,Font pixel)
    {
        Color white=new Color(.94f,.95f,.86f), muted=new Color(.7f,.78f,.69f), brass=new Color(.76f,.64f,.38f);
        Color[] identities=hud.game.CharacterColors;
        font=body;
        var menu=MenuLayer("Woodland Front End",canvas);hud.menuRoot=menu.gameObject;
        var title=MenuLayer("Title Screen",menu);hud.titleMenu=title.gameObject;
        MenuBackdrop("Illustrated Woodland",title,"Assets/Art/Menu/TitleBackdrop.png");
        Flat("Left Reading Field",title,new Rect(0,0,570,720),new Color(.018f,.045f,.042f,.83f));
        Flat("Lower Reading Field",title,new Rect(0,650,1280,70),new Color(.018f,.045f,.042f,.9f));
        Flat("Fine Brass Line",title,new Rect(72,115,320,1),brass);
        font=pixel;var titleText=Label("Rune Rumble Title",title,"RUNE\nRUMBLE",new Rect(72,144,490,130),38,white);titleText.lineSpacing=1.25f;font=body;
        Label("Title Description",title,"Eight fighters. One arena rivalry.\nChoose a rival. Make the woodland yours.",new Rect(76,295,430,64),21,muted);
        hud.modeButtons=new Button[4];
        hud.modeButtons[0]=MenuButton("Single Player",title,new Rect(76,382,410,76),"SINGLE PLAYER","Face a CPU rival with your chosen scout.",hud.fernColor);
        hud.modeButtons[1]=MenuButton("Multiplayer",title,new Rect(76,473,410,76),"MULTIPLAYER","Share a keyboard or connect two controllers.",hud.emberColor);
        hud.modeButtons[2]=MenuButton("Settings",title,new Rect(76,569,195,52),"SETTINGS","",brass);
        hud.modeButtons[3]=MenuButton("Exit",title,new Rect(291,569,195,52),"EXIT","",brass);
        Label("Title Footer",title,"ARROWS / STICK  choose     ENTER / SOUTH  confirm",new Rect(76,669,670,27),15,muted);
        Label("Title Audio Hint",title,"M  mute     - / +  volume",new Rect(890,669,310,27),15,muted,TextAnchor.MiddleRight);

        hud.ruleButtons=new Button[0];

        var roster=MenuLayer("Choose Scout",menu);hud.characterMenu=roster.gameObject;
        MenuBackdrop("Rune Hall",roster,"Assets/Art/Menu/SelectionHall.png");
        Flat("Roster Atmosphere",roster,new Rect(0,0,1280,720),new Color(.01f,.025f,.035f,.68f));
        Flat("Roster Header",roster,new Rect(0,0,1280,98),new Color(.015f,.035f,.045f,.9f));
        font=pixel;hud.characterHeading=Label("Choosing Player",roster,"CHOOSE YOUR FIGHTER",new Rect(60,32,1160,42),26,white,TextAnchor.MiddleCenter);font=body;
        Label("Roster Subtitle",roster,"RUNE RUMBLE   /   EIGHT RIVALS. ONE VICTOR.",new Rect(300,88,680,28),15,brass,TextAnchor.MiddleCenter);
        hud.selectionPortraits=new Image[2];hud.selectionNames=new Text[2];hud.selectionSkills=new Text[2];hud.selectionStates=new Text[2];
        for(int side=0;side<2;side++) {
            float x=side==0?24:1000;Color accent=side==0?hud.fernColor:hud.emberColor;
            var panel=Box("Player "+(side+1)+" Showcase",roster,new Rect(x,133,256,430));
            Flat("Showcase Shade",panel,new Rect(0,0,256,430),new Color(.015f,.04f,.05f,.72f));
            Flat("Player Accent",panel,new Rect(0,0,256,3),accent);
            Label("Player Badge",panel,"PLAYER "+(side+1),new Rect(16,16,224,28),18,accent,TextAnchor.MiddleCenter);
            hud.selectionPortraits[side]=Flat("Full Fighter Preview",panel,new Rect(10,48,236,264),Color.white);hud.selectionPortraits[side].preserveAspect=true;
            var showcaseRect=hud.selectionPortraits[side].rectTransform;
            showcaseRect.pivot=new Vector2(.5f,.5f);showcaseRect.anchoredPosition=new Vector2(128,-180);
            showcaseRect.localScale=new Vector3(side==0?1:-1,1,1);
            hud.selectionNames[side]=Label("Fighter Name",panel,"",new Rect(8,312,240,36),30,white,TextAnchor.MiddleCenter);
            hud.selectionSkills[side]=Label("Fighter Skill",panel,"",new Rect(12,350,232,27),17,muted,TextAnchor.MiddleCenter);
            hud.selectionStates[side]=Label("Ready State",panel,"SELECTING",new Rect(12,391,232,24),15,accent,TextAnchor.MiddleCenter);
        }
        hud.characterButtons=new Button[hud.game.CharacterNames.Length];
        for(int j=0;j<hud.characterButtons.Length;j++) {
            var card=MenuButton("Choose "+hud.game.CharacterNames[j],roster,new Rect(298+j%4*174,163+j/4*174,162,162),"","",identities[j]);hud.characterButtons[j]=card;
            Flat("Identity Wash",card.transform,new Rect(4,4,154,126),new Color(identities[j].r*.12f,identities[j].g*.12f,identities[j].b*.12f,.9f));
            var portrait=Flat("Scout Portrait",card.transform,new Rect(19,7,124,124),Color.white);portrait.sprite=hud.game.CharacterPortraits[j];portrait.preserveAspect=true;
            Flat("Identity Line",card.transform,new Rect(9,132,144,2),identities[j]);
            Label("Scout Name",card.transform,hud.game.CharacterNames[j].ToUpper(),new Rect(4,136,154,23),17,white,TextAnchor.MiddleCenter);
            Label("P1 Marker",card.transform,"P1",new Rect(4,4,35,23),14,hud.fernColor).gameObject.SetActive(false);
            Label("P2 Marker",card.transform,"P2",new Rect(123,4,35,23),14,hud.emberColor).gameObject.SetActive(false);
        }
        hud.characterDetail=Label("Selected Scout Detail",roster,"",new Rect(300,526,680,60),18,white,TextAnchor.MiddleCenter);
        Flat("Roster Footer Shade",roster,new Rect(0,600,1280,120),new Color(.01f,.025f,.035f,.94f));
        hud.characterBackButton=MenuButton("Back From Scouts",roster,new Rect(40,628,170,52),"BACK","",brass);
        hud.confirmCharacterButton=MenuButton("Confirm Scout",roster,new Rect(1000,620,240,64),"LOCK IN","",hud.fernColor);
        Label("Roster Footer",roster,"ARROWS / WASD  SELECT    ENTER / SOUTH  LOCK IN",new Rect(250,638,720,30),16,muted,TextAnchor.MiddleCenter);

        var maps=MenuLayer("Stage Select",menu);hud.mapMenu=maps.gameObject;
        MenuBackdrop("Rune Hall",maps,"Assets/Art/Menu/SelectionHall.png");
        Flat("Stage Scrim",maps,new Rect(0,0,1280,720),new Color(.01f,.025f,.035f,.8f));
        font=pixel;Label("Stage Heading",maps,"SELECT YOUR ARENA",new Rect(40,25,1200,42),27,white,TextAnchor.MiddleCenter);font=body;
        hud.modeText=Label("Match Type",maps,"",new Rect(40,77,1200,27),16,brass,TextAnchor.MiddleCenter);
        var stageWindow=Box("Selected Arena Window",maps,new Rect(220,119,840,322));stageWindow.gameObject.AddComponent<RectMask2D>();
        hud.mapPreview=Flat("Selected Arena",stageWindow,new Rect(0,0,840,322),Color.white);
        var previewFit=hud.mapPreview.gameObject.AddComponent<AspectRatioFitter>();previewFit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;previewFit.aspectRatio=16f/9;
        Flat("Preview Caption Shade",maps,new Rect(220,387,840,54),new Color(.01f,.025f,.035f,.86f));
        hud.selectedArenaName=Label("Selected Arena Name",maps,"",new Rect(235,396,810,35),25,white,TextAnchor.MiddleCenter);
        hud.mapButtons=new Button[hud.game.backgrounds.Length];
        for(int j=0;j<hud.mapButtons.Length;j++) {
            var card=MenuButton("Select "+hud.game.arenaNames[j],maps,new Rect(40+j*202,462,190,115),"","",brass);
            var crop=Box("Arena Window",card.transform,new Rect(4,4,182,77));crop.gameObject.AddComponent<RectMask2D>();
            var picture=Flat("Arena Illustration",crop,new Rect(0,0,182,77),Color.white);picture.sprite=hud.game.backgrounds[j];
            var fit=picture.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=16f/9;
            Label("Arena Name",card.transform,hud.game.arenaNames[j],new Rect(5,84,180,26),13,white,TextAnchor.MiddleCenter);hud.mapButtons[j]=card;
        }
        hud.mapDescription=Label("Stage Description",maps,"",new Rect(130,589,1020,29),18,muted,TextAnchor.MiddleCenter);
        hud.backButton=MenuButton("Back From Atlas",maps,new Rect(40,641,170,52),"BACK","",brass);
        hud.startButton=MenuButton("Begin Match",maps,new Rect(1000,633,240,64),"FIGHT","",hud.fernColor);
        Label("Atlas Footer",maps,"LEFT / RIGHT  SELECT    ENTER / SOUTH  FIGHT",new Rect(240,652,730,26),16,muted,TextAnchor.MiddleCenter);

        var settings=MenuLayer("Settings",menu);hud.settingsMenu=settings.gameObject;
        MenuBackdrop("Settings Woodland",settings,"Assets/Art/Menu/TitleBackdrop.png");Flat("Settings Reading Field",settings,new Rect(0,0,1280,720),new Color(.015f,.035f,.035f,.94f));
        font=pixel;Label("Settings Heading",settings,"SETTINGS",new Rect(284,58,710,48),28,white);font=body;
        hud.settingsSummary=Label("Settings Summary",settings,"",new Rect(288,125,710,36),21,muted);
        hud.settingsButtons=new Button[5];
        hud.settingsButtons[0]=MenuButton("Lower Volume",settings,new Rect(286,185,338,60),"VOLUME -","",brass);
        hud.settingsButtons[1]=MenuButton("Raise Volume",settings,new Rect(648,185,338,60),"VOLUME +","",brass);
        hud.settingsButtons[2]=MenuButton("Toggle Fullscreen",settings,new Rect(286,267,700,60),"FULLSCREEN","",brass);
        hud.settingsButtons[3]=MenuButton("Toggle Camera Shake",settings,new Rect(286,349,700,60),"CAMERA SHAKE","",brass);
        hud.settingsButtons[4]=MenuButton("Cycle Effects",settings,new Rect(286,431,700,60),"EFFECTS QUALITY","",brass);
        hud.settingsBackButton=MenuButton("Back From Settings",settings,new Rect(286,545,200,55),"BACK","",brass);
        Label("Settings Footer",settings,"UP / DOWN  choose     ENTER / SOUTH  change     ESC / EAST  back",new Rect(286,641,730,32),15,muted);
        var pause=MenuLayer("Paused Match",canvas);hud.pauseMenu=pause.gameObject;
        Flat("Pause Scrim",pause,new Rect(0,0,1280,720),new Color(.01f,.025f,.025f,.8f));
        var pauseSheet=Box("Pause Sheet",pause,new Rect(410,76,460,568));
        Flat("Pause Surface",pauseSheet,new Rect(0,0,460,568),new Color(.026f,.06f,.057f));Flat("Pause Fine Rule",pauseSheet,new Rect(32,22,396,1),brass);
        Label("Pause Heading",pauseSheet,"TAKE A BREATHER",new Rect(32,42,396,48),30,white,TextAnchor.MiddleCenter);
        hud.pauseButtons=new Button[5];string[] choices={"RESUME","REMATCH","CHOOSE MAP","MAIN MENU","MOVE LIST"};
        for(int j=0;j<5;j++)hud.pauseButtons[j]=MenuButton(choices[j],pauseSheet,new Rect(36,113+j*77,388,60),choices[j],"",brass);
        Label("Pause Footer",pauseSheet,"UP / DOWN  choose     ENTER / SOUTH  confirm",new Rect(20,524,420,26),14,muted,TextAnchor.MiddleCenter);
        pause.gameObject.SetActive(false);hud.RefreshMenu();
    }
    static void CreateControlRipple()
    {
        var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){
            float distance=Mathf.Abs(Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))-28);
            texture.SetPixel(x,y,new Color(1,1,1,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1.8f,distance))));
        }
        texture.Apply();const string path="Assets/Art/UI/ControlRipple.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);ImportSprite(path,64);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
    }
    static float GlyphLine(Vector2 p,Vector2 a,Vector2 b)
    {
        Vector2 ab=b-a;return Vector2.Distance(p,a+ab*Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude));
    }
    static void CreateActionGlyphs()
    {
        var connector=new Texture2D(146,146,TextureFormat.RGBA32,false);
        for(int y=0;y<146;y++)for(int x=0;x<146;x++) {
            float edge=48-(Mathf.Abs(x+.5f-73)+Mathf.Abs(y+.5f-73));
            Color ink=edge<2?new Color(.54f,.58f,.47f):new Color(.025f,.065f,.06f);
            ink.a=Mathf.Clamp01(edge+.75f)*.96f;connector.SetPixel(x,y,ink);
        }
        connector.Apply();const string connectorPath="Assets/Art/UI/ActionDiamond.png";
        File.WriteAllBytes(connectorPath,connector.EncodeToPNG());Object.DestroyImmediate(connector);AssetDatabase.ImportAsset(connectorPath,ImportAssetOptions.ForceSynchronousImport);ImportSprite(connectorPath,146);
        var connectorImporter=(TextureImporter)AssetImporter.GetAtPath(connectorPath);connectorImporter.filterMode=FilterMode.Bilinear;connectorImporter.SaveAndReimport();
        // Small native UI marks use analytic strokes, keeping device symbols sharp and editable in this builder.
        for(int glyph=-1;glyph<8;glyph++){
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
            Color[] colors={new Color(.39f,.73f,1),new Color(.98f,.48f,.73f),new Color(1,.43f,.41f),new Color(.43f,.96f,.67f)};
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){
                Vector2 p=new Vector2(x+.5f,y+.5f);float radius=Vector2.Distance(p,new Vector2(32,32));Color color=Color.clear;
                if(glyph==-1){
                    float alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(29.8f,31,radius));color=Color.Lerp(new Color(.055f,.095f,.10f),new Color(.16f,.22f,.23f),y/64f);
                    if(radius>28)color=new Color(.41f,.49f,.47f);color.a=alpha;
                } else {
                    p=(p-new Vector2(32,32))/1.5f+new Vector2(32,32);radius=Vector2.Distance(p,new Vector2(32,32));
                    int shape=glyph%4;float d=100;
                    if(glyph<4){
                        if(shape==0)d=Mathf.Min(GlyphLine(p,new Vector2(21,21),new Vector2(43,43)),GlyphLine(p,new Vector2(21,43),new Vector2(43,21)));
                        if(shape==1)d=Mathf.Min(Mathf.Min(GlyphLine(p,new Vector2(20,20),new Vector2(44,20)),GlyphLine(p,new Vector2(44,20),new Vector2(44,44))),Mathf.Min(GlyphLine(p,new Vector2(44,44),new Vector2(20,44)),GlyphLine(p,new Vector2(20,44),new Vector2(20,20))));
                        if(shape==2)d=Mathf.Abs(radius-13);
                        if(shape==3)d=Mathf.Min(GlyphLine(p,new Vector2(32,47),new Vector2(17,20)),Mathf.Min(GlyphLine(p,new Vector2(17,20),new Vector2(47,20)),GlyphLine(p,new Vector2(47,20),new Vector2(32,47))));
                        color=colors[shape];
                    } else {
                        if(shape==0){ // Shield: block.
                            Vector2[] v={new Vector2(19,44),new Vector2(32,48),new Vector2(45,44),new Vector2(43,28),new Vector2(32,16),new Vector2(21,28)};
                            for(int k=0;k<v.Length;k++)d=Mathf.Min(d,GlyphLine(p,v[k],v[(k+1)%v.Length]));
                        }
                        if(shape==1){d=Mathf.Min(Mathf.Abs(p.x-32),Mathf.Abs(p.y-32));if(radius>16)d=100;d=Mathf.Min(d,Mathf.Min(GlyphLine(p,new Vector2(22,22),new Vector2(42,42)),GlyphLine(p,new Vector2(22,42),new Vector2(42,22))));}
                        if(shape==2){d=Mathf.Min(GlyphLine(p,new Vector2(18,32),new Vector2(46,32)),Mathf.Min(GlyphLine(p,new Vector2(35,20),new Vector2(47,32)),GlyphLine(p,new Vector2(47,32),new Vector2(35,44))));}
                        if(shape==3){Vector2[] v={new Vector2(32,49),new Vector2(45,32),new Vector2(32,15),new Vector2(19,32)};for(int k=0;k<4;k++)d=Mathf.Min(d,GlyphLine(p,v[k],v[(k+1)%4]));d=Mathf.Min(d,GlyphLine(p,new Vector2(32,24),new Vector2(32,40)));}
                        color=Color.white;
                    }
                    color.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.1f,2.2f,d));
                }
                texture.SetPixel(x,y,color);
            }
            texture.Apply();string path="Assets/Art/UI/"+(glyph==-1?"ControlDisc":glyph<4?"PSAction"+glyph:"CPUAction"+(glyph-4))+".png";
            File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);ImportSprite(path,64);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        }
    }
    static void CreateFighterPlate()
    {
        const string path="Assets/Art/UI/ScoutPlate.png";
        int height=(int)WoodlandHUD.FighterHudHeight;
        var texture=new Texture2D(410,height,TextureFormat.RGBA32,false);
        for(int y=0;y<height;y++)for(int x=0;x<410;x++) {
            int edge=Mathf.Min(Mathf.Min(x,409-x),Mathf.Min(y,height-1-y));
            int corner=Mathf.Min(x,409-x)+Mathf.Min(y,height-1-y);
            bool visible=corner>=12;
            Color surface=Color.Lerp(new Color(.025f,.055f,.065f),new Color(.065f,.115f,.125f),y/(float)(height-1));
            if(edge==0||corner==12)surface=new Color(.38f,.46f,.43f);
            if(edge==2||corner==16)surface=new Color(.12f,.2f,.21f);
            texture.SetPixel(x,y,visible?surface:Color.clear);
        }
        texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);ImportSprite(path,100);
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
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Build/Windows/RuneRumble.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed");
    }
    public static void Build()
    {
        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Build/Rune Rumble.app", BuildTarget.StandaloneOSX, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Build failed");
    }
}
