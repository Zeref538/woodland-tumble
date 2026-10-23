using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WoodlandGame : MonoBehaviour
{
    public enum MenuScreen { Title, Rules, Characters, Maps, Settings, Match }
    public MenuScreen Screen { get; private set; } = MenuScreen.Title;
    public bool SoloMode { get; private set; }
    public int PendingArena { get; private set; }
    // Retained for legacy serialized callbacks and historical validation only.
    public bool BalanceMode => false;
    public int ChoosingPlayer { get; private set; }
    public int PendingCharacter { get; private set; }
    public int[] CharacterChoices = {0,1};
    public string[] CharacterNames={"Fern","Ember","Luna","Bram","Zephyr","Volt","Nyx","Corin"};
    public string[] SkillNames={"Vine Bolt","Fire Bolt","Frost Wave","Stone Wave","Gale Crescent","Arc Lance","Dusk Orb","Prism Shard"};
    public string[] SkillDescriptions={"Fire a binding vine bolt. Last-square ultimate: Wildroot Prison.","Launch a fire bolt. Last-square ultimate: Phoenix Combo.","Send a slowing frost wave. Last-square ultimate: Moonfrost Seal.","Launch a rising stone wave. Last-square ultimate: World Shatter.","A wind crescent drives rivals back. Last-square ultimate: Tempest Spiral.","A fast arc lance interrupts approaches. Last-square ultimate: Thunder Rush.","A shadow orb binds rivals. Last-square ultimate: Eclipse Snare.","A crystal shard controls space. Last-square ultimate: Crystal Crown."};
    public Sprite[] CharacterPortraits=new Sprite[8];
    public ScoutAnimations[] CharacterAnimationSets=new ScoutAnimations[8];
    public RuntimeAnimatorController[] CharacterControllers=new RuntimeAnimatorController[8];
    public Color[] CharacterColors={new Color(.38f,1,.68f),new Color(1,.48f,.18f),new Color(.45f,.72f,1),new Color(.85f,.68f,.4f),new Color(.55f,1,.85f),new Color(1,.85f,.25f),new Color(.7f,.4f,1),new Color(1,.55f,.8f)};
    public Rigidbody2D[] arenaPlatforms=new Rigidbody2D[0];
    public float SettingsVolume=>WoodlandEffects.Instance?WoodlandEffects.Instance.SfxVolume:.65f;
    public bool SettingsFullscreen=>UnityEngine.Screen.fullScreen;
    public bool CameraShakeEnabled=>WoodlandEffects.Instance&&WoodlandEffects.Instance.cameraShake;
    public string EffectsQualityName=>!WoodlandEffects.Instance?"STANDARD":WoodlandEffects.Instance.effectIntensity<.8f?"LOW":WoodlandEffects.Instance.effectIntensity>1.1f?"HIGH":"STANDARD";
    public static WoodlandGame Instance { get; private set; }
    [Header("Editable scene references")]
    public Scout[] players = new Scout[2];
    public Rigidbody2D plank;
    public WoodlandHUD hud;
    [Header("Arena wardrobe")]
    public SpriteRenderer backgroundArt, plankArt, sphereArt;
    public Sprite[] backgrounds = new Sprite[4], planks = new Sprite[4], spheres = new Sprite[4];
    public Sprite[] PvpBases = new Sprite[4], PvpLedges = new Sprite[4];
    public string[] arenaNames = { "SUNLIT HOLLOW", "MOONLIT LAKE", "EMBER GROVE", "SKYWARD RUINS", "STORMGLASS CAUSEWAY", "HOLLOWROOT CISTERN" };
    public int ArenaIndex { get; private set; }
    public string ArenaName => arenaNames[ArenaIndex];
    public void SelectArena(int index)
    {
        if (index < 0 || index >= backgrounds.Length || !backgrounds[index]) return;
        bool changed=ArenaIndex!=index;ArenaIndex = index;
        if(changed)WoodlandEffects.Instance?.PlayCue(12,.18f);
        backgroundArt.sprite = backgrounds[index];
        ApplyPlatformArt();EnsureEdgeHatches();
        float scale = Mathf.Max(22 / backgroundArt.sprite.bounds.size.x, 12.5f / backgroundArt.sprite.bounds.size.y);
        backgroundArt.transform.localScale = Vector3.one * scale;
        if (WoodlandEffects.Instance) WoodlandEffects.Instance.ambientTint = index == 1 ? new Color(.3f,1,.85f) : index == 2 ? new Color(1,.4f,.12f) : new Color(1,.86f,.42f);
    }
    [Header("Match rules")]
    [Min(1)] public int roundsToWin = 3;
    public float fallBoundary = -4.6f;
    [Min(10)] public float roundDuration = 99;
    public float RoundTimeRemaining { get; private set; } = 99;
    public bool SuddenDeath { get; private set; }
    public float SafeFloorHalfWidth { get; private set; } = 5;
    float suddenDeathTime;
    public const float ArenaHalfWidth = 6.1f;
    [Header("Breakable edge hatches")]
    public BoxCollider2D[] edgeHatches = new BoxCollider2D[2];
    public SpriteRenderer[] edgeHatchArt = new SpriteRenderer[2];
    public Sprite hatchClosedArt, hatchOpenArt;
    [Min(1)] public int hatchDurability = 2;
    readonly int[] hatchDamage = new int[2];
    public bool HatchIsOpen(int side) => side>=0 && side<2 && hatchDamage[side]>=hatchDurability;
    public float plankRestoringForce = .24f;
    public float knockoutDisplayTime = 1.5f;
    public int[] score = new int[2];
    [HideInInspector] public bool playing;
    [HideInInspector] public string message;
    public bool Paused { get; private set; }
    public bool MatchOver => score[0] >= roundsToWin || score[1] >= roundsToWin;
    public bool CanPlayAgain => MatchOver && roundRoutine==null;
    public string ReplayHint {
        get { var pad=GetGamepad(0)??GetGamepad(1);string style=GamepadStyle(pad);return (style=="PLAYSTATION"?"CROSS":style=="XBOX"?"A":pad!=null?"SOUTH":"R")+" TO PLAY AGAIN"; }
    }

    Vector2 plankHome;
    float plankAngle;
    Coroutine roundRoutine;
    readonly Gamepad[] gamepads = new Gamepad[2];

    public void RefreshGamepads()
    {
        for (int i = 0; i < gamepads.Length; i++)
            if (gamepads[i] != null && (!gamepads[i].added || !gamepads[i].enabled)) gamepads[i] = null;
        foreach (var pad in Gamepad.all) {
            if (!pad.enabled || pad == gamepads[0] || pad == gamepads[1]) continue;
            for (int i = 0; i < gamepads.Length; i++)
                if (gamepads[i] == null) { gamepads[i] = pad; break; }
        }
    }
    public Gamepad GetGamepad(int slot)
    {
        RefreshGamepads();
        return slot >= 0 && slot < gamepads.Length ? gamepads[slot] : null;
    }
    public static string GamepadStyle(Gamepad pad)
    {
        if (pad is UnityEngine.InputSystem.DualShock.DualShockGamepad) return "PLAYSTATION";
        if (pad is UnityEngine.InputSystem.XInput.XInputController) return "XBOX";
        return pad == null ? "KEYBOARD" : "GAMEPAD";
    }
    public static bool PadMenuChord(Gamepad pad) => pad != null && pad.selectButton.isPressed;

    void Awake() { Instance = this; }
    void Start()
    {
        Application.targetFrameRate = 60;
        SelectArena(0);
        Physics2D.gravity = new Vector2(0, -20);
        plankHome = plank.position;
        plankAngle = 0;
        var hinge = plank.GetComponent<HingeJoint2D>();
        if (hinge != null && hinge.connectedBody == null) hinge.connectedAnchor = plank.transform.TransformPoint(hinge.anchor);
        if(WoodlandEffects.Instance){WoodlandEffects.Instance.cameraShake=PlayerPrefs.GetInt("Woodland.CameraShake",1)==1;WoodlandEffects.Instance.effectIntensity=Mathf.Clamp(PlayerPrefs.GetFloat("Woodland.EffectsQuality",1.35f),.25f,2);}
        ApplyRules();ReturnToMenu();
    }
    void FreezeMatch()
    {
        StopAllCoroutines();roundRoutine=null;
        playing=false;Paused=false;Time.timeScale=1;AudioListener.pause=false;message="";
        WoodlandEffects.Instance?.ResetCameraPresentation();
        plank.simulated=false;plank.linearVelocity=Vector2.zero;plank.angularVelocity=0;
        plank.position=plankHome;plank.rotation=plankAngle;
        foreach(var scout in players) { scout.ResetScout();scout.Body.simulated=false; }
        if(hud && hud.helpPanel)hud.helpPanel.SetActive(false);
    }
    public void ChooseMode(bool solo)
    {
        FreezeMatch();SoloMode=solo;
        for(int i=0;i<players.Length;i++)players[i].aiControlled=solo && i==1;
        ChoosingPlayer=0;PendingCharacter=CharacterChoices[0];ApplyRules();
        Screen=MenuScreen.Characters;
        if(hud)hud.RefreshMenu();
    }
    public void ChooseRules(bool balance)
    {
        if(Screen!=MenuScreen.Rules)return;
        ApplyRules();ChoosingPlayer=0;PendingCharacter=CharacterChoices[0];Screen=MenuScreen.Characters;
        if(hud)hud.RefreshMenu();
    }
    public bool CharacterAvailable(int index)=>index>=0&&index<CharacterNames.Length&&(SoloMode||ChoosingPlayer==0||index!=CharacterChoices[0]);
    public void PreviewCharacter(int index)
    {
        if(Screen!=MenuScreen.Characters||!CharacterAvailable(index))return;
        PendingCharacter=index;if(hud)hud.RefreshMenu();
    }
    public void ConfirmCharacter()
    {
        if(Screen!=MenuScreen.Characters||!CharacterAvailable(PendingCharacter))return;
        CharacterChoices[ChoosingPlayer]=PendingCharacter;
        if(!SoloMode&&ChoosingPlayer==0){ChoosingPlayer=1;PendingCharacter=CharacterChoices[1]==CharacterChoices[0]?(CharacterChoices[0]+1)%CharacterNames.Length:CharacterChoices[1];}
        else {if(SoloMode)CharacterChoices[1]=(CharacterChoices[0]+1)%CharacterNames.Length;ApplyCharacters();Screen=MenuScreen.Maps;SelectArena(PendingArena);}
        if(hud)hud.RefreshMenu();
    }
    void ApplyCharacters()
    {
        for(int i=0;i<players.Length;i++){
            int choice=CharacterChoices[i];
            players[i].SetCharacter(choice,CharacterNames[choice],CharacterAnimationSets[choice],CharacterControllers[choice],CharacterColors[choice]);
            players[i].aiControlled=SoloMode&&i==1;
        }
    }
    public bool IsGroundBody(Rigidbody2D body)
    {
        if(body==null)return false;
        if(body==plank)return true;
        if(!BalanceMode)foreach(var platform in arenaPlatforms)if(body==platform&&platform.gameObject.activeSelf)return true;
        return false;
    }
    void ApplyRules()
    {
        if(plank){var hinge=plank.GetComponent<HingeJoint2D>();if(hinge)hinge.enabled=false;plank.bodyType=RigidbodyType2D.Kinematic;plank.rotation=0;}
        ApplyPlatformArt();EnsureEdgeHatches();
        foreach(var platform in arenaPlatforms)if(platform)platform.gameObject.SetActive(false);
        if(sphereArt)sphereArt.gameObject.SetActive(false);
        if(WoodlandEffects.Instance)WoodlandEffects.Instance.SetArenaCamera(5.8f,new Vector3(0,1.4f,-10));
    }
    void ApplyPlatformArt()
    {
        if(!plank || !plankArt)return;
        plankArt.enabled=false;
        plank.transform.localScale=Vector3.one;
        var box=plank.GetComponent<BoxCollider2D>();
        if(box){box.size=new Vector2(10,.22f);box.offset=new Vector2(0,.04f);box.edgeRadius=.04f;}
    }
    void EnsureEdgeHatches()
    {
        if(!hatchClosedArt)hatchClosedArt=Resources.Load<Sprite>("Art/Hatches/Closed");
        if(!hatchOpenArt)hatchOpenArt=Resources.Load<Sprite>("Art/Hatches/RuneHole");
        if(edgeHatches==null || edgeHatches.Length!=2)edgeHatches=new BoxCollider2D[2];
        if(edgeHatchArt==null || edgeHatchArt.Length!=2)edgeHatchArt=new SpriteRenderer[2];
        for(int i=0;i<2;i++) {
            if(!edgeHatches[i]) {
                var cover=new GameObject(i==0?"Left Rune Hatch":"Right Rune Hatch");
                cover.transform.SetParent(plank.transform,false);
                edgeHatches[i]=cover.AddComponent<BoxCollider2D>();
                var visual=new GameObject("Hatch Artwork");visual.transform.SetParent(cover.transform,false);
                edgeHatchArt[i]=visual.AddComponent<SpriteRenderer>();edgeHatchArt[i].sortingOrder=4;
            }
            var rect=edgeHatches[i].transform;
            rect.position=new Vector3(plank.position.x+(i==0?-5.75f:5.75f),plank.position.y,0);
            rect.localScale=new Vector3(1/plank.transform.lossyScale.x,1/plank.transform.lossyScale.y,1);
            edgeHatches[i].size=new Vector2(1.5f,.22f);edgeHatches[i].offset=new Vector2(0,.04f);
        }
        ResetEdgeHatches();
    }
    void RefreshHatchArt(int side)
    {
        var renderer=edgeHatchArt[side];if(!renderer)return;
        bool open=HatchIsOpen(side);
        renderer.sprite=open?hatchOpenArt:hatchClosedArt;
        if(renderer.sprite)renderer.transform.localScale=Vector3.one*(1.5f/renderer.sprite.bounds.size.x);
        renderer.color=open?Color.white:Color.Lerp(Color.white,new Color(1,.6f,.3f),hatchDamage[side]/(float)hatchDurability);
    }
    void ResetEdgeHatches()
    {
        for(int i=0;i<2;i++) {
            hatchDamage[i]=hatchDurability;if(edgeHatches[i])edgeHatches[i].enabled=false;
            if(edgeHatchArt[i])edgeHatchArt[i].transform.localPosition=Vector3.zero;
            RefreshHatchArt(i);
        }
    }
    public void DamageEdgeHatch(Vector2 contact,int damage=1)
    {
        if(!playing || Mathf.Abs(contact.x-plank.position.x)<4.75f)return;
        int side=contact.x<plank.position.x?0:1;
        if(HatchIsOpen(side))return;
        hatchDamage[side]=Mathf.Min(hatchDurability,hatchDamage[side]+Mathf.Max(1,damage));
        if(edgeHatches[side])edgeHatches[side].enabled=!HatchIsOpen(side);
        RefreshHatchArt(side);
        WoodlandEffects.Instance?.HeavyImpact(contact,side==0?-1:1,new Color(.7f,.9f,.8f),1);
        if(HatchIsOpen(side))WoodlandEffects.Instance?.Burst(contact,Vector2.up,20,new Color(.55f,.7f,.55f),3);
    }
    static void SizePlatform(Rigidbody2D platform,Sprite sprite,float artWidth,float colliderWidth)
    {
        var renderer=platform.GetComponent<SpriteRenderer>();
        if(!renderer || sprite.bounds.size.x<=0)return;
        renderer.sprite=sprite;
        float scale=artWidth/sprite.bounds.size.x;
        platform.transform.localScale=Vector3.one*scale;
        var box=platform.GetComponent<BoxCollider2D>();
        if(!box)return;
        box.size=new Vector2(colliderWidth/scale,.22f/scale);
        box.offset=new Vector2(0,.04f/scale);
        box.edgeRadius=.04f/scale;
    }
    public void OpenMapSelection()
    {
        FreezeMatch();Screen=MenuScreen.Maps;if(hud)hud.RefreshMenu();
    }
    public void MenuBack()
    {
        if(Screen==MenuScreen.Maps){ChoosingPlayer=SoloMode?0:1;PendingCharacter=CharacterChoices[ChoosingPlayer];Screen=MenuScreen.Characters;}
        else if(Screen==MenuScreen.Characters&&ChoosingPlayer==1){ChoosingPlayer=0;PendingCharacter=CharacterChoices[0];}
        else if(Screen==MenuScreen.Characters)Screen=MenuScreen.Title;
        else if(Screen==MenuScreen.Rules||Screen==MenuScreen.Settings)Screen=MenuScreen.Title;
        if(hud)hud.RefreshMenu();
    }
    public void OpenSettings(){FreezeMatch();Screen=MenuScreen.Settings;if(hud)hud.RefreshMenu();}
    public void StepVolume(float delta){if(WoodlandEffects.Instance)WoodlandEffects.Instance.SetVolume(SettingsVolume+delta);}
    public void ToggleFullscreen(){UnityEngine.Screen.fullScreen=!UnityEngine.Screen.fullScreen;}
    public void ToggleCameraShake(){if(WoodlandEffects.Instance){WoodlandEffects.Instance.cameraShake=!CameraShakeEnabled;PlayerPrefs.SetInt("Woodland.CameraShake",CameraShakeEnabled?1:0);}}
    public void CycleEffectsQuality(){if(WoodlandEffects.Instance){float value=EffectsQualityName=="LOW"?1:EffectsQualityName=="STANDARD"?1.35f:.65f;WoodlandEffects.Instance.effectIntensity=value;PlayerPrefs.SetFloat("Woodland.EffectsQuality",value);}}
    public void RequestExit()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
    public void PreviewArena(int index)
    {
        if(Screen!=MenuScreen.Maps || index<0 || index>=backgrounds.Length)return;
        PendingArena=index;SelectArena(index);
        if(hud)hud.RefreshMenu();
    }
    public void StartSelectedMatch()
    {
        if(Screen!=MenuScreen.Maps)return;
        ApplyRules();ApplyCharacters();SelectArena(PendingArena);Screen=MenuScreen.Match;
        if(hud)hud.RefreshMenu();
        RestartMatch();
    }
    public void ReturnToMenu()
    {
        FreezeMatch();Screen=MenuScreen.Title;
        if(hud)hud.RefreshMenu();
    }
    public void BackToTitle()=>ReturnToMenu();
    public void ResumeMatch()
    {
        if(Screen!=MenuScreen.Match)return;
        if(hud)hud.CloseMoveGuide();
        Paused=false;Time.timeScale=1;AudioListener.pause=false;
    }
    IEnumerator StartRound()
    {
        WoodlandEffects.Instance?.ResetCameraPresentation();
        playing = false;
        RoundTimeRemaining = roundDuration;
        SuddenDeath=false;suddenDeathTime=0;SafeFloorHalfWidth=5;ApplyPlatformArt();
        plank.simulated = false;
        plank.transform.SetPositionAndRotation(plankHome, Quaternion.Euler(0, 0, plankAngle));
        plank.position = plankHome;
        plank.rotation = plankAngle;
        plank.linearVelocity = Vector2.zero;
        plank.angularVelocity = 0;
        ResetEdgeHatches();
        foreach (var scout in players) scout.ResetScout();
        for (int count = 3; count > 0; count--) {
            message = "GET READY\n" + count;
            yield return new WaitForSeconds(.55f);
        }
        plank.simulated = true;
        foreach (var scout in players) scout.Body.simulated = true;
        playing = true;
        message = "RIVALS, FIGHT!";
        yield return new WaitForSeconds(.65f);
        if (playing) message = "";
        roundRoutine = null;
    }
    public void RegisterKnockout(Scout loser)
    {
        if (!playing) return;
        playing = false;
        WoodlandEffects.Instance?.ResetCameraPresentation();
        message = "KNOCKOUT!";
        if (roundRoutine != null) StopCoroutine(roundRoutine);
        roundRoutine = StartCoroutine(ResolveRound(loser.id));
    }
    IEnumerator ResolveRound(int loser)
    {
        if(loser < 0) {
            message = "DRAW!";
            yield return new WaitForSeconds(1.5f);
            roundRoutine = StartCoroutine(StartRound());
            yield break;
        }
        // Leave the full collapse animation visible before scoring or respawning.
        yield return new WaitForSeconds(knockoutDisplayTime);
        int winner = 1 - loser;
        score[winner]++;
        players[winner].PlayVictory();
        WoodlandEffects.Instance?.Celebrate(players[winner].transform.position + Vector3.up * .5f, players[winner].Element);
        message = players[winner].displayName.ToUpper() + " WINS";
        yield return new WaitForSeconds(1.5f);
        if (MatchOver) {
            message = players[winner].displayName.ToUpper() + " IS CHAMPION!\n"+ReplayHint;
            roundRoutine = null;
        } else roundRoutine = StartCoroutine(StartRound());
    }
    public void RestartMatch()
    {
        if(Screen!=MenuScreen.Match)return;
        if(hud)hud.CloseMoveGuide();
        StopAllCoroutines();
        score[0] = score[1] = 0;
        Time.timeScale = 1;
        Paused = false;
        roundRoutine = StartCoroutine(StartRound());
    }
    public void TogglePause() { if(Screen!=MenuScreen.Match)return;Paused = !Paused; Time.timeScale = Paused ? 0 : 1;if(Paused)WoodlandEffects.Instance?.ResetCameraPresentation(); }
    void Update()
    {
        RefreshGamepads();
        if(Screen!=MenuScreen.Match)return;
        if(CanPlayAgain)message=players[score[0]>=roundsToWin?0:1].displayName.ToUpper()+" IS CHAMPION!\n"+ReplayHint;
        if(playing && !Paused) {
            RoundTimeRemaining = Mathf.Max(0,RoundTimeRemaining-Time.deltaTime);
            if(RoundTimeRemaining<=0) {
                if(!SuddenDeath) {
                    SuddenDeath=true;message="SUDDEN DEATH!";
                    for(int side=0;side<2;side++) {hatchDamage[side]=hatchDurability;if(edgeHatches[side])edgeHatches[side].enabled=false;RefreshHatchArt(side);}
                    WoodlandEffects.Instance?.PlayCue(9,.7f);
                }
                suddenDeathTime+=Time.deltaTime;
                SafeFloorHalfWidth=Mathf.Max(.15f,5-suddenDeathTime*.18f);
                var floor=plank.GetComponent<BoxCollider2D>();if(floor)floor.size=new Vector2(SafeFloorHalfWidth*2,.22f);
                for(int side=0;side<2;side++) {
                    float width=6.5f-SafeFloorHalfWidth;
                    var art=edgeHatchArt[side];if(!art || !art.sprite)continue;
                    art.transform.position=new Vector3((side==0?-1:1)*(SafeFloorHalfWidth+width*.5f),plank.position.y,0);
                    art.transform.localScale=new Vector3(width/art.sprite.bounds.size.x,1.5f/art.sprite.bounds.size.x,1);
                }
                if(suddenDeathTime>2)message="";
            }
        }
        var keyboard = Keyboard.current;
        bool pause = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        bool restart = keyboard != null && keyboard.rKey.wasPressedThisFrame;
        if (keyboard != null) {
            if (keyboard.tabKey.wasPressedThisFrame) SelectArena((ArenaIndex + 1) % backgrounds.Length);
            if (keyboard.digit1Key.wasPressedThisFrame) SelectArena(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectArena(1);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectArena(2);
        }
        foreach (var pad in gamepads) {
            if (pad == null) continue;
            pause |= pad.startButton.wasPressedThisFrame;
            restart |= CanPlayAgain && pad.buttonSouth.wasPressedThisFrame;
            if (!PadMenuChord(pad)) continue;
            restart |= pad.buttonSouth.wasPressedThisFrame;
            if (pad.rightShoulder.wasPressedThisFrame) SelectArena((ArenaIndex + 1) % backgrounds.Length);
            if (pad.leftShoulder.wasPressedThisFrame) SelectArena((ArenaIndex + backgrounds.Length - 1) % backgrounds.Length);
        }
        if (restart) RestartMatch();
        else if (pause) { if(hud && hud.MoveGuideOpen)hud.CloseMoveGuide();else TogglePause(); }
    }
    void LateUpdate()
    {
        if(Screen!=MenuScreen.Match)return;
        // Outer walls keep combat on screen; opened hatches inside them allow downward ring-outs.
        foreach(var fighter in players) {
            if(!fighter || !fighter.Body || !fighter.Body.simulated)continue;
            var position=fighter.Body.position;
            float x=Mathf.Clamp(position.x,-ArenaHalfWidth,ArenaHalfWidth);
            if(x==position.x)continue;
            fighter.Body.position=new Vector2(x,position.y);
            var velocity=fighter.Body.linearVelocity;
            if(velocity.x*Mathf.Sign(position.x)>0)velocity.x=0;
            fighter.Body.linearVelocity=velocity;
        }
    }
    void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; }
}
