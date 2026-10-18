using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class WoodlandHUD : MonoBehaviour
{
    public const float FighterHudScale=.8f;
    public const float FighterHudHeight=154;
    public const float ActionDiamondHeight=230;
    public const float ActionButtonDiameter=80;
    public WoodlandGame game;
    public Text[] names = new Text[2];
    public Text[] healthText = new Text[2];
    public Text[] statusText = new Text[2];
    public Text[] damageText = new Text[2];
    public Image[] healthFill = new Image[2];
    public Image[] pushFill = new Image[2];
    public Image[] punchFill = new Image[2];
    public Image[] actionDiscs = new Image[8];
    public Button[] actionButtons = new Button[8];
    public Image[] actionRejectMarks = new Image[8];
    public Image[] fighterPortraitRings = new Image[2];
    public int AcceptedActionClicks { get; private set; }
    public int RejectedActionClicks { get; private set; }
    public int LastActionSlot { get; private set; } = -1;
    public bool LastActionAccepted { get; private set; }
    public Image[] actionIcons = new Image[8];
    public Text[] actionGlyphs = new Text[8];
    public Image[] actionCovers = new Image[8];
    public Image[] actionRipples = new Image[8];
    public Image[] specialCooldown = new Image[2];
    public Text[] specialCooldownText = new Text[2];
    public Text[] ultimateLabels = new Text[2];
    public GameObject[] ultimatePanels = new GameObject[2];
    public GameObject[] actionTooltipPanels = new GameObject[2];
    public Text[] actionTooltips = new Text[2];
    public Sprite[] characterActionCovers = new Sprite[16];
    public Sprite[] playstationIcons = new Sprite[4];
    public Sprite[] neutralIcons = new Sprite[4];
    public Text arenaText;
    public Text soundText;
    public Text helpHint;
    public Text[] deviceText = new Text[2];
    public Text[] controlsText = new Text[2];
    public Text[] shieldText = new Text[2];
    public Image[] shieldFill = new Image[2];
    public Image[] roundPips = new Image[6];
    public Image[] damageGhost = new Image[2];
    public RectTransform[] fighterPlates = new RectTransform[2];
    public GameObject menuRoot, titleMenu, rulesMenu, characterMenu, mapMenu, settingsMenu, matchHud, pauseMenu;
    public Button[] modeButtons = new Button[4];
    public Button[] ruleButtons = new Button[2];
    public Button[] characterButtons = new Button[4];
    public Button[] settingsButtons = new Button[5];
    public Button confirmCharacterButton, ruleBackButton, characterBackButton, settingsBackButton;
    public Text characterDetail, characterHeading, settingsSummary;
    public Image[] fighterPortraits = new Image[2];
    public Button[] mapButtons = new Button[4];
    public Button[] pauseButtons = new Button[4];
    public Button startButton, backButton;
    public Text modeText, mapDescription;
    public Image mapPreview;
    public GameObject helpPanel;
    public GameObject bannerPanel;
    public Text bannerText;
    public Color fernColor = new Color(.43f, .94f, .70f);
    public Color emberColor = new Color(1, .67f, .32f);
    readonly float[] previousHealth = { -1, -1 };
    readonly float[] ghostDelay = new float[2];
    readonly float[] punch = new float[2];
    readonly int[] previousCombo = new int[2];
    readonly float[] comboPop = new float[2];
    readonly float[] rippleLife = new float[8];
    readonly float[] rejectionLife = new float[8];
    readonly float[] lastAcceptedAt = {-10,-10,-10,-10,-10,-10,-10,-10};
    readonly float[] pendingAttemptUntil = new float[8];
    readonly bool[] observationsReady = new bool[2];
    readonly float[] observedPunch = new float[2], observedKick = new float[2], observedSpecial = new float[2], observedVertical = new float[2];
    readonly int[] observedAirJumps = new int[2];
    readonly string[] observedState = new string[2];
    readonly bool[] actionHeld = new bool[8];
    readonly int[] hoveredAction = {-1,-1};
    readonly int[] recentAction = {-1,-1};
    readonly float[] tooltipUntil = new float[2];

    int menuFocus, pauseFocus;
    WoodlandGame.MenuScreen lastScreen = (WoodlandGame.MenuScreen)(-1);
    bool wasPaused;
    float nextNavigation;
    void Awake()
    {
        // Scene-built buttons bind at runtime because AddListener calls made by an editor builder are not serialized.
        if(modeButtons.Length>0 && modeButtons[0])modeButtons[0].onClick.AddListener(()=>game.ChooseMode(true));
        if(modeButtons.Length>1 && modeButtons[1])modeButtons[1].onClick.AddListener(()=>game.ChooseMode(false));
        if(modeButtons.Length>2 && modeButtons[2])modeButtons[2].onClick.AddListener(game.OpenSettings);
        if(modeButtons.Length>3 && modeButtons[3])modeButtons[3].onClick.AddListener(game.RequestExit);
        for(int i=0;i<ruleButtons.Length;i++){int index=i;if(ruleButtons[i])ruleButtons[i].onClick.AddListener(()=>game.ChooseRules(index==1));}
        for(int i=0;i<characterButtons.Length;i++){int index=i;if(characterButtons[i])characterButtons[i].onClick.AddListener(()=>game.PreviewCharacter(index));}
        if(confirmCharacterButton)confirmCharacterButton.onClick.AddListener(game.ConfirmCharacter);
        if(ruleBackButton)ruleBackButton.onClick.AddListener(game.MenuBack);
        if(characterBackButton)characterBackButton.onClick.AddListener(game.MenuBack);
        if(settingsBackButton)settingsBackButton.onClick.AddListener(game.MenuBack);
        if(settingsButtons.Length>0 && settingsButtons[0])settingsButtons[0].onClick.AddListener(()=>game.StepVolume(-.1f));
        if(settingsButtons.Length>1 && settingsButtons[1])settingsButtons[1].onClick.AddListener(()=>game.StepVolume(.1f));
        if(settingsButtons.Length>2 && settingsButtons[2])settingsButtons[2].onClick.AddListener(game.ToggleFullscreen);
        if(settingsButtons.Length>3 && settingsButtons[3])settingsButtons[3].onClick.AddListener(game.ToggleCameraShake);
        if(settingsButtons.Length>4 && settingsButtons[4])settingsButtons[4].onClick.AddListener(game.CycleEffectsQuality);
        for(int i=0;i<mapButtons.Length;i++) {int index=i;if(mapButtons[i])mapButtons[i].onClick.AddListener(()=>game.PreviewArena(index));}
        if(startButton)startButton.onClick.AddListener(game.StartSelectedMatch);
        if(backButton)backButton.onClick.AddListener(game.MenuBack);
        if(pauseButtons.Length>0 && pauseButtons[0])pauseButtons[0].onClick.AddListener(game.ResumeMatch);
        if(pauseButtons.Length>1 && pauseButtons[1])pauseButtons[1].onClick.AddListener(game.RestartMatch);
        if(pauseButtons.Length>2 && pauseButtons[2])pauseButtons[2].onClick.AddListener(game.OpenMapSelection);
        if(pauseButtons.Length>3 && pauseButtons[3])pauseButtons[3].onClick.AddListener(game.ReturnToMenu);
        for(int slot=0;slot<actionDiscs.Length;slot++){
            if(!actionDiscs[slot])continue;int captured=slot;
            if(actionButtons[slot])actionButtons[slot].onClick.AddListener(()=>ActivateAction(captured/4,captured%4));
            var events=actionDiscs[slot].gameObject.AddComponent<EventTrigger>();
            var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};enter.callback.AddListener(_=>hoveredAction[captured/4]=captured%4);events.triggers.Add(enter);
            var exit=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit};exit.callback.AddListener(_=>hoveredAction[captured/4]=-1);events.triggers.Add(exit);
        }
    }
    void Update()
    {
        if (!game) return;
        RefreshMenu();
        if (game.Screen != WoodlandGame.MenuScreen.Match || game.Paused) {
            NavigateMenus();
            return;
        }
        bool toggle = Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
        if (game) for (int i=0;i<2;i++) {
            var pad=game.GetGamepad(i);
            toggle |= pad!=null && pad.selectButton.wasReleasedThisFrame &&
                !pad.buttonSouth.isPressed && !pad.leftShoulder.isPressed && !pad.rightShoulder.isPressed && !menuChordUsed;
            if(pad!=null && pad.selectButton.isPressed && (pad.buttonSouth.isPressed || pad.leftShoulder.isPressed || pad.rightShoulder.isPressed)) menuChordUsed=true;
        }
        if (helpPanel && toggle) { helpPanel.SetActive(!helpPanel.activeSelf);WoodlandEffects.Instance?.PlayCue(12,.18f); }
        bool menuHeld=false;
        if(game)for(int i=0;i<2;i++)menuHeld|=WoodlandGame.PadMenuChord(game.GetGamepad(i));
        if(!menuHeld)menuChordUsed=false;
    }
    bool menuChordUsed;
    void LateUpdate() => Refresh();
    public void Refresh()
    {
        if (!game || game.players == null) return;
        RefreshMenu();
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < 2; i++) {
            if (i >= game.players.Length || !game.players[i]) continue;
            Scout scout = game.players[i];
            var pad=game.GetGamepad(i);
            string style=WoodlandGame.GamepadStyle(pad);
            if(deviceText[i])deviceText[i].text=i==1 && game.SoloMode?"CPU RIVAL  /  "+scout.displayName.ToUpper():style=="KEYBOARD"?"KEYBOARD  /  "+(i==0?"A D + W":"ARROW KEYS"):style+"  /  PAD "+(i+1);
            if(controlsText[i])controlsText[i].text=i==1 && game.SoloMode?scout.displayName.ToUpper()+" / CPU RIVAL\nReads spacing, guards and counters.\nBait a strike, punish the recovery.":pad==null?(i==0?"A / D   Move      W   Jump / double jump\nS   Block    Q   Backdash    Shift   Dash\nG   Punch    F   Kick\nH   Ranged skill [6s cooldown]\nT   Ultimate [25 HP or less, once / round]":"Arrows   Move      Up   Jump / double jump\nDown   Block    U   Backdash    R Shift   Dash\nL   Punch    K   Kick\nJ   Ranged skill [6s cooldown]\nI   Ultimate [25 HP or less, once / round]"):
                "STICK / D-PAD   Move\n"+(style=="PLAYSTATION"?"CROSS   Jump    SQUARE   Punch\nCIRCLE   Kick    TRIANGLE   Ranged [6s]\nL2   Block    L1   Backdash    R1   Dash\nL2 + R2   Ultimate [25 HP or less]":style=="XBOX"?"A   Jump    X   Punch    B   Kick\nY   Ranged [6s]    LT   Block\nLB   Backdash    RB   Dash\nLT + RT   Ultimate [25 HP or less]":"SOUTH   Jump    WEST   Punch\nEAST   Kick    NORTH   Ranged [6s]\nL TRIGGER   Block    L SHOULDER   Backdash\nR SHOULDER   Dash    BOTH TRIGGERS   Ult");
            RefreshActionDiamond(i,scout,pad,style);
            Color accent = scout.Element;
            int character=game.CharacterChoices[i];
            if(fighterPortraits[i] && character>=0 && character<game.CharacterPortraits.Length)fighterPortraits[i].sprite=game.CharacterPortraits[character];
            float ratio = Mathf.Clamp01(scout.Health / Mathf.Max(1, scout.maxHealth));
            bool reset = previousHealth[i] < 0 || scout.Health > previousHealth[i];
            if (scout.Health < previousHealth[i]) { ghostDelay[i] = .42f; punch[i] = 1; }
            previousHealth[i] = scout.Health;
            if(names[i]) {names[i].text = (i==1 && game.SoloMode?"CPU":"P"+(i+1))+"  "+scout.displayName.ToUpper();names[i].color=accent;}
            if(healthText[i]) healthText[i].text = Mathf.CeilToInt(scout.Health)+" / "+Mathf.RoundToInt(scout.maxHealth);
            if(healthFill[i]) {
                healthFill[i].fillAmount = reset ? ratio : Mathf.Lerp(healthFill[i].fillAmount,ratio,1-Mathf.Exp(-18*dt));
                healthFill[i].color=Color.Lerp(new Color(1,.33f,.26f),accent,Mathf.Clamp01(ratio*2));
            }
            if(damageGhost[i]) {
                ghostDelay[i]-=dt;
                if(reset) damageGhost[i].fillAmount=ratio;
                else if(ghostDelay[i]<=0) damageGhost[i].fillAmount=Mathf.MoveTowards(damageGhost[i].fillAmount,ratio,dt*.38f);
            }
            punch[i] = Mathf.MoveTowards(punch[i],0,dt*5);
            if(fighterPlates[i]) fighterPlates[i].localScale=Vector3.one*(FighterHudScale*(1+.035f*punch[i]));
            if(damageText[i]) damageText[i].text="+"+Mathf.RoundToInt((1-ratio)*scout.damageKnockbackMultiplier*100)+"% KB";
            if(shieldText[i]) {shieldText[i].text=Mathf.CeilToInt(scout.Stamina)+" / "+Mathf.RoundToInt(scout.StaminaCapacity);shieldText[i].color=scout.Stamina<20?new Color(1,.75f,.57f):new Color(.96f,.97f,.88f);}
            if(shieldFill[i]) shieldFill[i].fillAmount=Mathf.Lerp(shieldFill[i].fillAmount,scout.Stamina/Mathf.Max(1,scout.StaminaCapacity),1-Mathf.Exp(-14*dt));
            if(pushFill[i]) pushFill[i].fillAmount=scout.PushReady;
            if(punchFill[i]) punchFill[i].fillAmount=scout.PunchReady;
            for(int p=0;p<3;p++) if(roundPips[i*3+p]) roundPips[i*3+p].color=game.score[i]>p?accent:new Color(.22f,.29f,.28f);
            int combo=scout.ComboCount;
            if(combo>1 && combo!=previousCombo[i]) comboPop[i]=1;
            previousCombo[i]=combo;
            comboPop[i]=Mathf.MoveTowards(comboPop[i],0,dt*2.6f);
            if(statusText[i]) {
                statusText[i].text=combo>1?combo+" HIT":"";
                statusText[i].color=new Color(accent.r,accent.g,accent.b,combo>1?Mathf.Clamp01(comboPop[i]*2):0);
                var rect=statusText[i].rectTransform;
                rect.anchorMin=rect.anchorMax=Vector2.one*.5f;
                if(Camera.main) {
                    Vector2 point=RectTransformUtility.WorldToScreenPoint(Camera.main,scout.transform.position+Vector3.up*2.1f);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,point,null,out Vector2 local);
                    rect.anchoredPosition=local;
                }
                rect.localScale=Vector3.one*(1+.18f*comboPop[i]);
            }
        }
        var audio=WoodlandEffects.Instance;
        if(helpHint)helpHint.text=game.GetGamepad(0)!=null||game.GetGamepad(1)!=null?"START PAUSE    SHARE / VIEW CONTROLS":"F1 CONTROLS    ESC PAUSE    R REMATCH";
        if(soundText && audio) {
            bool quiet=audio.Muted||audio.SfxVolume<=0;
            bool notice=quiet||Time.unscaledTime<audio.AudioNoticeUntil;soundText.gameObject.SetActive(notice);if(helpHint)helpHint.gameObject.SetActive(!notice);
            soundText.text=audio.Muted?"SOUND MUTED  /  M TO RESTORE":audio.SfxVolume<=0?"SOUND 0%  /  + TO RESTORE":"SOUND  "+Mathf.RoundToInt(audio.SfxVolume*100)+"%";
            var color=soundText.color;color.a=quiet?1:Mathf.Clamp01((audio.AudioNoticeUntil-Time.unscaledTime)*3);soundText.color=color;
        }
        if(arenaText) arenaText.text=game.ArenaName;
        string text=game.Paused?"":game.message;
        if(bannerPanel) bannerPanel.SetActive(!string.IsNullOrEmpty(text));
        if(bannerText) bannerText.text=text;
    }

    public float ActionFeedback(int slot)
    {
        if(slot<0 || slot>=8)return 0;
        return rejectionLife[slot]>0?-rejectionLife[slot]:rippleLife[slot];
    }
    public bool ActivateAction(int player,int action)
    {
        int slot=player>=0 && player<2 && action>=0 && action<4?player*4+action:-1;
        LastActionSlot=slot;bool accepted=false;
        if(slot>=0 && game && game.Screen==WoodlandGame.MenuScreen.Match && game.playing && !game.Paused && game.players!=null && player<game.players.Length && game.players[player]){
            Scout scout=game.players[player];
            // Busy mouse taps must not enter the keyboard/controller melee buffer on a rejected click.
            if(!scout.ActionBusy)accepted=action==0?scout.TryJump():action==1?scout.TryAttack(true):action==2?scout.TryAttack(false):scout.TrySpecial();
        }
        LastActionAccepted=accepted;
        if(accepted)AcceptedActionClicks++;else RejectedActionClicks++;
        if(slot>=0){PulseAction(slot,accepted);recentAction[player]=action;tooltipUntil[player]=Time.unscaledTime+.8f;}
        return accepted;
    }
    void PulseAction(int slot,bool accepted)
    {
        if(accepted){rippleLife[slot]=1;rejectionLife[slot]=0;lastAcceptedAt[slot]=Time.unscaledTime;}
        else{rejectionLife[slot]=1;rippleLife[slot]=0;}
        pendingAttemptUntil[slot]=0;
    }
    void ObserveAcceptedActions(int player,Scout scout)
    {
        float vertical=scout.Body?scout.Body.linearVelocity.y:0;
        if(observationsReady[player]){
            bool jumping=scout.AnimationState=="Jump" || scout.AnimationState=="DoubleJump";
            bool jump=jumping && (scout.AirJumpsRemaining<observedAirJumps[player] || vertical>observedVertical[player]+2 || scout.AnimationState!=observedState[player] && vertical>.5f);
            bool[] accepted={jump,scout.PunchReady<observedPunch[player]-.025f,scout.PushReady<observedKick[player]-.025f,scout.SpecialCooldownRemaining>observedSpecial[player]+.05f};
            for(int action=0;action<4;action++)if(accepted[action]){int slot=player*4+action;if(Time.unscaledTime-lastAcceptedAt[slot]>.08f)PulseAction(slot,true);}
        }
        observationsReady[player]=true;observedPunch[player]=scout.PunchReady;observedKick[player]=scout.PushReady;observedSpecial[player]=scout.SpecialCooldownRemaining;observedVertical[player]=vertical;observedAirJumps[player]=scout.AirJumpsRemaining;observedState[player]=scout.AnimationState;
    }
    void RefreshActionDiamond(int player,Scout scout,Gamepad pad,string style)
    {
        bool cpu=player==1 && game.SoloMode;
        bool ps=pad!=null && style=="PLAYSTATION";
        string[] letters=pad==null?(player==0?new[]{"W","G","F","H"}:new[]{"UP","L","K","J"}):style=="XBOX"?new[]{"A","X","B","Y"}:new[]{"S","W","E","N"};
        float dt=Time.unscaledDeltaTime,ease=1-Mathf.Exp(-24*dt);
        ObserveAcceptedActions(player,scout);
        for(int a=0;a<4;a++){
            int slot=player*4+a,coverIndex=Mathf.Clamp(scout.CharacterIndex,0,3)*4+a;
            if(actionCovers[slot] && coverIndex<characterActionCovers.Length && characterActionCovers[coverIndex])actionCovers[slot].sprite=characterActionCovers[coverIndex];
            if(actionIcons[slot]){actionIcons[slot].enabled=!cpu&&ps;actionIcons[slot].sprite=playstationIcons[a];actionIcons[slot].color=Color.white;}
            if(actionGlyphs[slot]){actionGlyphs[slot].text=cpu?"CPU":ps?"":letters[a];actionGlyphs[slot].fontSize=cpu?11:letters[a].Length>1?13:18;actionGlyphs[slot].color=pad==null?new Color(.91f,.95f,.86f):a==0?new Color(.51f,.89f,.53f):a==1?new Color(.44f,.71f,1):a==2?new Color(1,.48f,.43f):new Color(1,.82f,.35f);}
            bool pressed=false,rawPress=false;
            if(!cpu && pad!=null){var button=a==0?pad.buttonSouth:a==1?pad.buttonWest:a==2?pad.buttonEast:pad.buttonNorth;pressed=button.isPressed;rawPress=button.wasPressedThisFrame;}
            else if(!cpu && Keyboard.current!=null){var keys=Keyboard.current;var button=player==0?(a==0?keys.wKey:a==1?keys.gKey:a==2?keys.fKey:keys.hKey):(a==0?keys.upArrowKey:a==1?keys.lKey:a==2?keys.kKey:keys.jKey);pressed=button.isPressed;rawPress=button.wasPressedThisFrame;}
            if(rawPress){recentAction[player]=a;tooltipUntil[player]=Time.unscaledTime+.7f;if(Time.unscaledTime-lastAcceptedAt[slot]>.05f)pendingAttemptUntil[slot]=Time.unscaledTime+.48f;if(a==3 && scout.SpecialCooldownRemaining>0 && Time.unscaledTime-lastAcceptedAt[slot]>.05f)PulseAction(slot,false);}
            if(pendingAttemptUntil[slot]>0 && Time.unscaledTime>=pendingAttemptUntil[slot])PulseAction(slot,false);
            actionHeld[slot]=pressed;
            rippleLife[slot]=Mathf.MoveTowards(rippleLife[slot],0,dt*2.9f);rejectionLife[slot]=Mathf.MoveTowards(rejectionLife[slot],0,dt*4.5f);
            float acceptedPulse=rippleLife[slot],denied=rejectionLife[slot];
            float targetScale=1+acceptedPulse*.13f-denied*.035f-(pressed && acceptedPulse<=0 && denied<=0?.05f:0);
            if(actionDiscs[slot])actionDiscs[slot].rectTransform.localScale=Vector3.Lerp(actionDiscs[slot].rectTransform.localScale,Vector3.one*targetScale,ease);
            if(actionCovers[slot]){
                Color tint=Color.Lerp(Color.white,scout.Element,acceptedPulse*.35f);tint=Color.Lerp(tint,new Color(1,.36f,.29f),denied*.62f);actionCovers[slot].color=Color.Lerp(actionCovers[slot].color,tint,ease);
                actionCovers[slot].rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Sin((1-acceptedPulse)*Mathf.PI*5)*acceptedPulse*4);
            }
            if(actionRipples[slot]){var ring=scout.Element;ring.a=acceptedPulse*.95f;actionRipples[slot].color=ring;actionRipples[slot].rectTransform.localScale=Vector3.one*(1+(1-acceptedPulse)*.4f);}
            if(actionRejectMarks[slot])actionRejectMarks[slot].color=new Color(1,.36f,.29f,denied*.9f);
        }
        float remaining=scout.SpecialCooldownRemaining;
        if(specialCooldown[player]){specialCooldown[player].gameObject.SetActive(remaining>.02f);specialCooldown[player].fillAmount=1-scout.SpecialReady;}
        if(specialCooldownText[player])specialCooldownText[player].text=remaining>.02f?Mathf.CeilToInt(remaining).ToString():"";
        int tip=hoveredAction[player]>=0?hoveredAction[player]:recentAction[player];
        bool visible=tip>=0 && (hoveredAction[player]>=0 || Time.unscaledTime<tooltipUntil[player]);
        if(actionTooltipPanels[player])actionTooltipPanels[player].SetActive(visible);
        if(visible && actionTooltips[player])actionTooltips[player].text=tip==0?"JUMP / DOUBLE JUMP":tip==1?"PUNCH / COMBO":tip==2?"KICK / LAUNCH":scout.SkillName.ToUpper()+"\n"+(remaining>.02f?remaining.ToString("0.0")+"s to ready":"RANGED READY");
        bool ultimate=scout.UltimateReady || scout.UltimateActive;
        if(ultimatePanels[player])ultimatePanels[player].SetActive(ultimate);
        if(ultimateLabels[player]){
            string binding=cpu?"CPU":pad==null?(player==0?"T":"I"):ps?"L2 + R2":style=="XBOX"?"LT + RT":"BOTH TRIGGERS";
            ultimateLabels[player].text=scout.UltimateActive?scout.UltimateName.ToUpper():"ULTIMATE READY: "+scout.UltimateName.ToUpper()+" ["+binding+"]";
            var color=new Color(1,.86f,.48f);color.a=scout.UltimateActive?1:.86f+Mathf.Sin(Time.unscaledTime*4)*.14f;ultimateLabels[player].color=color;
        }
        if(fighterPortraitRings[player]){var color=ultimate?Color.Lerp(scout.Element,new Color(1,.85f,.4f),.5f+.25f*Mathf.Sin(Time.unscaledTime*4)):scout.Element;color.a=ultimate?.95f:.7f;fighterPortraitRings[player].color=color;}
    }
    static readonly string[] mapDescriptions={"Warm light. Old roots. A friendly rivalry.","Moonstone, falling water and a silver edge.","Glowing embers. A battle under red leaves.","Ancient sky ruins. A fight above the clouds."};
    public void RefreshMenu()
    {
        if(!game) return;
        var screen=game.Screen;bool match=screen==WoodlandGame.MenuScreen.Match;
        if(menuRoot)menuRoot.SetActive(!match);
        if(titleMenu)titleMenu.SetActive(screen==WoodlandGame.MenuScreen.Title);
        if(rulesMenu)rulesMenu.SetActive(screen==WoodlandGame.MenuScreen.Rules);
        if(characterMenu)characterMenu.SetActive(screen==WoodlandGame.MenuScreen.Characters);
        if(mapMenu)mapMenu.SetActive(screen==WoodlandGame.MenuScreen.Maps);
        if(settingsMenu)settingsMenu.SetActive(screen==WoodlandGame.MenuScreen.Settings);
        if(matchHud)matchHud.SetActive(match);
        if(pauseMenu)pauseMenu.SetActive(match && game.Paused);
        if(screen!=lastScreen){
            menuFocus=screen==WoodlandGame.MenuScreen.Rules && game.BalanceMode?1:0;nextNavigation=Time.unscaledTime+.12f;lastScreen=screen;if(helpPanel)helpPanel.SetActive(false);
            for(int i=0;i<2;i++){hoveredAction[i]=recentAction[i]=-1;tooltipUntil[i]=0;observationsReady[i]=false;}
            for(int i=0;i<8;i++){actionHeld[i]=false;rippleLife[i]=rejectionLife[i]=pendingAttemptUntil[i]=0;}
        }
        if(game.Paused && !wasPaused){pauseFocus=0;nextNavigation=Time.unscaledTime+.12f;if(helpPanel)helpPanel.SetActive(false);}
        wasPaused=game.Paused;
        string rules=game.BalanceMode?"BALANCE":"ARENA PVP";
        if(modeText)modeText.text=(game.SoloMode?"SINGLE PLAYER":"LOCAL MULTIPLAYER")+"  /  "+rules;
        int arena=Mathf.Clamp(game.PendingArena,0,mapDescriptions.Length-1);
        if(mapDescription)mapDescription.text=mapDescriptions[arena];
        if(mapPreview && game.backgrounds.Length>arena)mapPreview.sprite=game.backgrounds[arena];
        for(int i=0;i<modeButtons.Length;i++)MarkButton(modeButtons[i],i==menuFocus,i==0?fernColor:emberColor);
        for(int i=0;i<ruleButtons.Length;i++)MarkButton(ruleButtons[i],i==menuFocus,i==0?fernColor:emberColor);
        for(int i=0;i<mapButtons.Length;i++)MarkButton(mapButtons[i],i==arena,i==0?fernColor:i==1?new Color(.45f,.78f,1):emberColor);
        for(int i=0;i<pauseButtons.Length;i++)MarkButton(pauseButtons[i],i==pauseFocus,fernColor);
        int scout=Mathf.Clamp(game.PendingCharacter,0,3);
        if(characterHeading)characterHeading.text="PLAYER "+(game.ChoosingPlayer+1)+": CHOOSE YOUR SCOUT";
        if(characterDetail)characterDetail.text=game.CharacterNames[scout]+" / "+game.SkillNames[scout]+"\n"+game.SkillDescriptions[scout];
        for(int i=0;i<characterButtons.Length;i++){
            if(!characterButtons[i])continue;bool available=game.CharacterAvailable(i);characterButtons[i].interactable=available;
            MarkButton(characterButtons[i],i==scout,i==0?fernColor:i==1?emberColor:i==2?new Color(.65f,.73f,1):new Color(.82f,.7f,.52f));
            var image=characterButtons[i].transform.Find("Scout Portrait");if(image){var portrait=image.GetComponent<Image>();if(portrait)portrait.color=available?Color.white:new Color(.35f,.4f,.39f,.7f);}
        }
        if(confirmCharacterButton)confirmCharacterButton.interactable=game.CharacterAvailable(scout);
        if(settingsSummary)settingsSummary.text="MASTER VOLUME  "+Mathf.RoundToInt(game.SettingsVolume*100)+"%";
        if(settingsButtons.Length>2)SetButtonTitle(settingsButtons[2],"FULLSCREEN  /  "+(game.SettingsFullscreen?"ON":"OFF"));
        if(settingsButtons.Length>3)SetButtonTitle(settingsButtons[3],"CAMERA SHAKE  /  "+(game.CameraShakeEnabled?"ON":"OFF"));
        if(settingsButtons.Length>4)SetButtonTitle(settingsButtons[4],"EFFECTS QUALITY  /  "+game.EffectsQualityName.ToUpper());
        for(int i=0;i<settingsButtons.Length;i++)MarkButton(settingsButtons[i],i==menuFocus,fernColor);
    }
    static void SetButtonTitle(Button button,string value)
    {
        if(!button)return;var child=button.transform.Find("Choice");if(child){var text=child.GetComponent<Text>();if(text)text.text=value;}
    }
    static void MarkButton(Button button,bool chosen,Color accent)
    {
        if(!button) return;
        button.image.color=chosen?new Color(.095f,.17f,.14f,1):new Color(.03f,.065f,.063f,.97f);
        var outline=button.GetComponent<Outline>();if(outline)outline.effectColor=chosen?accent:new Color(.28f,.36f,.31f,.85f);
        // Choice text belongs to this button, leaving all illustration and native layout editable.
        var text=button.transform.Find("Choice");if(text){var label=text.GetComponent<Text>();if(label)label.color=chosen?new Color(.99f,.96f,.8f):new Color(.8f,.86f,.77f);}
    }
    void NavigateMenus()
    {
        var keys=Keyboard.current;
        bool confirm=keys!=null && (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame);
        bool back=game.Screen!=WoodlandGame.MenuScreen.Match && keys!=null && keys.escapeKey.wasPressedThisFrame;
        bool next=keys!=null && (keys.downArrowKey.wasPressedThisFrame || keys.rightArrowKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame || keys.dKey.wasPressedThisFrame);
        bool previous=keys!=null && (keys.upArrowKey.wasPressedThisFrame || keys.leftArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame || keys.aKey.wasPressedThisFrame);
        bool analogNext=false,analogPrevious=false;
        foreach(var pad in Gamepad.all){
            if(!pad.added || !pad.enabled)continue;
            confirm|=pad.buttonSouth.wasPressedThisFrame;back|=pad.buttonEast.wasPressedThisFrame;
            next|=pad.dpad.down.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame;
            previous|=pad.dpad.up.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame;
            Vector2 stick=pad.leftStick.ReadValue();analogNext|=stick.x>.55f || stick.y<-.55f;analogPrevious|=stick.x<-.55f || stick.y>.55f;
        }
        bool analog=analogNext||analogPrevious;
        if(Time.unscaledTime>=nextNavigation){next|=analogNext;previous|=analogPrevious;}
        if(!analog && !next && !previous)nextNavigation=0;
        if(next || previous){
            int direction=next?1:-1;
            if(game.Screen==WoodlandGame.MenuScreen.Title)menuFocus=(menuFocus+direction+modeButtons.Length)%modeButtons.Length;
            else if(game.Screen==WoodlandGame.MenuScreen.Rules)menuFocus=(menuFocus+direction+2)%2;
            else if(game.Screen==WoodlandGame.MenuScreen.Characters){
                int candidate=game.PendingCharacter;
                for(int n=0;n<4;n++){candidate=(candidate+direction+4)%4;if(game.CharacterAvailable(candidate)){game.PreviewCharacter(candidate);break;}}
            }
            else if(game.Screen==WoodlandGame.MenuScreen.Maps)game.PreviewArena((game.PendingArena+direction+mapButtons.Length)%mapButtons.Length);
            else if(game.Screen==WoodlandGame.MenuScreen.Settings)menuFocus=(menuFocus+direction+settingsButtons.Length)%settingsButtons.Length;
            else pauseFocus=(pauseFocus+direction+4)%4;
            nextNavigation=Time.unscaledTime+.22f;
        }
        if(back){
            if(game.Screen==WoodlandGame.MenuScreen.Match && game.Paused)game.ResumeMatch();
            else if(game.Screen!=WoodlandGame.MenuScreen.Title)game.MenuBack();
            return;
        }
        if(confirm){
            if(game.Screen==WoodlandGame.MenuScreen.Title && modeButtons.Length>menuFocus && modeButtons[menuFocus])modeButtons[menuFocus].onClick.Invoke();
            else if(game.Screen==WoodlandGame.MenuScreen.Rules && ruleButtons.Length>menuFocus && ruleButtons[menuFocus])ruleButtons[menuFocus].onClick.Invoke();
            else if(game.Screen==WoodlandGame.MenuScreen.Characters)game.ConfirmCharacter();
            else if(game.Screen==WoodlandGame.MenuScreen.Maps)game.StartSelectedMatch();
            else if(game.Screen==WoodlandGame.MenuScreen.Settings && settingsButtons.Length>menuFocus && settingsButtons[menuFocus])settingsButtons[menuFocus].onClick.Invoke();
            else if(game.Paused && pauseButtons.Length>pauseFocus && pauseButtons[pauseFocus])pauseButtons[pauseFocus].onClick.Invoke();
        }
    }
}
