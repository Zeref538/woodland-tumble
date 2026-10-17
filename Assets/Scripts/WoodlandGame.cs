using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WoodlandGame : MonoBehaviour
{
    public enum MenuScreen { Title, Rules, Characters, Maps, Settings, Match }
    public MenuScreen Screen { get; private set; } = MenuScreen.Title;
    public bool SoloMode { get; private set; }
    public int PendingArena { get; private set; }
    public bool BalanceMode { get; private set; }=true;
    public int ChoosingPlayer { get; private set; }
    public int PendingCharacter { get; private set; }
    public int[] CharacterChoices = {0,1};
    public string[] CharacterNames={"Fern","Ember","Luna","Bram"};
    public string[] SkillNames={"Vine Bind","Flame Rush","Frost Wave","Stone Quake"};
    public string[] SkillDescriptions={"Catch a rival in living vines and control the ground.","Lunge forward with two fiery strikes.","Send a freezing wave across the arena.","Slam the ground and launch nearby rivals upward."};
    public Sprite[] CharacterPortraits=new Sprite[4];
    public ScoutAnimations[] CharacterAnimationSets=new ScoutAnimations[4];
    public RuntimeAnimatorController[] CharacterControllers=new RuntimeAnimatorController[4];
    public Color[] CharacterColors={new Color(.38f,1,.68f),new Color(1,.48f,.18f),new Color(.45f,.72f,1),new Color(.85f,.68f,.4f)};
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
    public string[] arenaNames = { "SUNLIT HOLLOW", "MOONLIT LAKE", "EMBER GROVE", "SKYWARD RUINS" };
    public int ArenaIndex { get; private set; }
    public string ArenaName => arenaNames[ArenaIndex];
    public void SelectArena(int index)
    {
        if (index < 0 || index >= backgrounds.Length || !backgrounds[index] || !planks[index] || !spheres[index]) return;
        bool changed=ArenaIndex!=index;ArenaIndex = index;
        if(changed)WoodlandEffects.Instance?.PlayCue(12,.18f);
        backgroundArt.sprite = backgrounds[index]; plankArt.sprite = planks[index]; sphereArt.sprite = spheres[index];
        foreach(var platform in arenaPlatforms)if(platform)platform.GetComponent<SpriteRenderer>().sprite=planks[index];
        // All plank and sphere canvases share dimensions, keeping the physical surface unchanged.
        float scale = Mathf.Max(22 / backgroundArt.sprite.bounds.size.x, 12.5f / backgroundArt.sprite.bounds.size.y);
        backgroundArt.transform.localScale = Vector3.one * scale;
        if (WoodlandEffects.Instance) WoodlandEffects.Instance.ambientTint = index == 1 ? new Color(.3f,1,.85f) : index == 2 ? new Color(1,.4f,.12f) : new Color(1,.86f,.42f);
    }
    [Header("Match rules")]
    [Min(1)] public int roundsToWin = 3;
    public float fallBoundary = -4.6f;
    public float plankRestoringForce = .24f;
    public float knockoutDisplayTime = 1.5f;
    public int[] score = new int[2];
    [HideInInspector] public bool playing;
    [HideInInspector] public string message;
    public bool Paused { get; private set; }
    public bool MatchOver => score[0] >= roundsToWin || score[1] >= roundsToWin;
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
        plankAngle = plank.rotation;
        var hinge = plank.GetComponent<HingeJoint2D>();
        if (hinge != null && hinge.connectedBody == null) hinge.connectedAnchor = plank.transform.TransformPoint(hinge.anchor);
        if(WoodlandEffects.Instance){WoodlandEffects.Instance.cameraShake=PlayerPrefs.GetInt("Woodland.CameraShake",1)==1;WoodlandEffects.Instance.effectIntensity=PlayerPrefs.GetFloat("Woodland.EffectsQuality",1);}
        ApplyRules();ReturnToMenu();
    }
    void FreezeMatch()
    {
        StopAllCoroutines();roundRoutine=null;
        playing=false;Paused=false;Time.timeScale=1;AudioListener.pause=false;message="";
        plank.simulated=false;plank.linearVelocity=Vector2.zero;plank.angularVelocity=0;
        plank.position=plankHome;plank.rotation=plankAngle;
        foreach(var scout in players) { scout.ResetScout();scout.Body.simulated=false; }
        if(hud && hud.helpPanel)hud.helpPanel.SetActive(false);
    }
    public void ChooseMode(bool solo)
    {
        FreezeMatch();SoloMode=solo;
        for(int i=0;i<players.Length;i++)players[i].aiControlled=solo && i==1;
        Screen=MenuScreen.Rules;
        if(hud)hud.RefreshMenu();
    }
    public void ChooseRules(bool balance)
    {
        if(Screen!=MenuScreen.Rules)return;
        BalanceMode=balance;ApplyRules();ChoosingPlayer=0;PendingCharacter=CharacterChoices[0];Screen=MenuScreen.Characters;
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
        if(plank){var hinge=plank.GetComponent<HingeJoint2D>();if(hinge)hinge.enabled=BalanceMode;plank.bodyType=BalanceMode?RigidbodyType2D.Dynamic:RigidbodyType2D.Kinematic;}
        foreach(var platform in arenaPlatforms)if(platform)platform.gameObject.SetActive(!BalanceMode);
        if(sphereArt)sphereArt.gameObject.SetActive(BalanceMode);
        if(WoodlandEffects.Instance)WoodlandEffects.Instance.SetArenaCamera(BalanceMode?4.5f:5.3f,new Vector3(0,BalanceMode?1:1.4f,-10));
    }
    public void OpenMapSelection()
    {
        FreezeMatch();Screen=MenuScreen.Maps;if(hud)hud.RefreshMenu();
    }
    public void MenuBack()
    {
        if(Screen==MenuScreen.Maps){ChoosingPlayer=SoloMode?0:1;PendingCharacter=CharacterChoices[ChoosingPlayer];Screen=MenuScreen.Characters;}
        else if(Screen==MenuScreen.Characters&&ChoosingPlayer==1){ChoosingPlayer=0;PendingCharacter=CharacterChoices[0];}
        else if(Screen==MenuScreen.Characters)Screen=MenuScreen.Rules;
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
        Paused=false;Time.timeScale=1;AudioListener.pause=false;
    }
    IEnumerator StartRound()
    {
        playing = false;
        plank.simulated = false;
        plank.transform.SetPositionAndRotation(plankHome, Quaternion.Euler(0, 0, plankAngle));
        plank.position = plankHome;
        plank.rotation = plankAngle;
        plank.linearVelocity = Vector2.zero;
        plank.angularVelocity = 0;
        foreach (var scout in players) scout.ResetScout();
        for (int count = 3; count > 0; count--) {
            message = "GET READY\n" + count;
            yield return new WaitForSeconds(.55f);
        }
        plank.simulated = true;
        foreach (var scout in players) scout.Body.simulated = true;
        playing = true;
        message = "SCOUTS, SHOVE!";
        yield return new WaitForSeconds(.65f);
        if (playing) message = "";
        roundRoutine = null;
    }
    public void RegisterKnockout(Scout loser)
    {
        if (!playing) return;
        playing = false;
        message = "KNOCKOUT!";
        if (roundRoutine != null) StopCoroutine(roundRoutine);
        roundRoutine = StartCoroutine(ResolveRound(loser.id));
    }
    IEnumerator ResolveRound(int loser)
    {
        // Leave the full collapse animation visible before scoring or respawning.
        yield return new WaitForSeconds(knockoutDisplayTime);
        int winner = 1 - loser;
        score[winner]++;
        WoodlandEffects.Instance?.Celebrate(players[winner].transform.position + Vector3.up * .5f, winner == 0 ? new Color(.6f, .85f, .35f) : new Color(1, .6f, .25f));
        message = players[winner].displayName.ToUpper() + " WINS";
        yield return new WaitForSeconds(1.1f);
        if (MatchOver) {
            message = players[winner].displayName.ToUpper() + " IS CHAMPION!\nR TO PLAY AGAIN";
            roundRoutine = null;
        } else roundRoutine = StartCoroutine(StartRound());
    }
    public void RestartMatch()
    {
        if(Screen!=MenuScreen.Match)return;
        StopAllCoroutines();
        score[0] = score[1] = 0;
        Time.timeScale = 1;
        Paused = false;
        roundRoutine = StartCoroutine(StartRound());
    }
    public void TogglePause() { if(Screen!=MenuScreen.Match)return;Paused = !Paused; Time.timeScale = Paused ? 0 : 1; }
    void Update()
    {
        RefreshGamepads();
        if(Screen!=MenuScreen.Match)return;
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
            if (!PadMenuChord(pad)) continue;
            restart |= pad.buttonSouth.wasPressedThisFrame;
            if (pad.rightShoulder.wasPressedThisFrame) SelectArena((ArenaIndex + 1) % backgrounds.Length);
            if (pad.leftShoulder.wasPressedThisFrame) SelectArena((ArenaIndex + backgrounds.Length - 1) % backgrounds.Length);
        }
        if (restart) RestartMatch();
        else if (pause) TogglePause();
    }
    void FixedUpdate()
    {
        if (playing&&BalanceMode) plank.AddTorque(-Mathf.DeltaAngle(plankAngle, plank.rotation) * plankRestoringForce);
    }
    void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; }
}
