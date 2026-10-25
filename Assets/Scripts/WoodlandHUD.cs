using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class WoodlandHUD : MonoBehaviour
{
    public const float FighterHudScale=.9f;
    public const float FighterHudHeight=154;
    public const float ActionClusterWidth=256;
    public const float ActionClusterHeight=280;
    public const float ActionButtonDiameter=80;
    public WoodlandGame game;
    public Text[] names = new Text[2];
    public Text[] healthText = new Text[2];
    public Text[] statusText = new Text[2];
    public Text[] damageText = new Text[2];
    public Image[] healthFill = new Image[2];
    public Image[] hitSquares = new Image[10];
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
    public GameObject[] ultimateCutins=new GameObject[2];
    public Image[] ultimateCutinArt=new Image[2];
    public Text[] ultimateCutinNames=new Text[2];
    readonly bool[] cutinWasActive=new bool[2];
    readonly float[] cutinStarted=new float[2];

    public GameObject[] actionTooltipPanels = new GameObject[2];
    public Text[] actionTooltips = new Text[2];
    public Sprite[] characterActionCovers = new Sprite[32];
    public Sprite[] characterGuardCovers=new Sprite[8];
    public Sprite[] playstationIcons = new Sprite[4];
    public Sprite[] neutralIcons = new Sprite[4];
    public Text arenaText;
    public Text roundTimerText;
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
    public CanvasGroup[] actionClusters=new CanvasGroup[2];
    public Image[] fighterPortraits = new Image[2];
    public Button[] mapButtons = new Button[4];
    public Button[] pauseButtons = new Button[4];
    public Button startButton, backButton;
    public Text modeText, mapDescription;
    public Image mapPreview;
    public Image[] selectionSweeps=new Image[2];
    public Sprite[] hudPortraits=new Sprite[8];
    public Sprite[] selectionIdleFrames=new Sprite[32];
    int showcaseIdentity=-1,showcasePlayer=-1,lastArenaSelection=-1;
    float arenaSelectionChangedAt;
    float showcaseChangedAt;
    public Image[] selectionPortraits=new Image[2];
    public Text[] selectionNames=new Text[2], selectionSkills=new Text[2], selectionStates=new Text[2];
    public Text selectedArenaName;
    public GameObject helpPanel;
    public Text[] guideNames=new Text[2];
    public Button guideCloseButton;
    public bool MoveGuideOpen => helpPanel && helpPanel.activeSelf;
    int guideClosedFrame=-1;
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
    readonly bool[] pointerGuardHeld = new bool[2], oneShotGuard = new bool[2];
    readonly int[] guardReleaseFrame = {-10,-10};
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
    float screenEnteredAt;
    GameObject enteringScreen;
    CanvasGroup enteringGroup;
    void Awake()
    {
        if(!roundTimerText && matchHud && bannerText) {
            var timer=new GameObject("Round Timer",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
            timer.transform.SetParent(matchHud.transform,false);
            roundTimerText=timer.GetComponent<Text>();roundTimerText.font=bannerText.font;
            roundTimerText.fontSize=24;roundTimerText.alignment=TextAnchor.MiddleCenter;roundTimerText.supportRichText=false;
            roundTimerText.raycastTarget=false;
            var rect=roundTimerText.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
            rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-97);rect.sizeDelta=new Vector2(74,30);
        }
        // Old saved HUDs may still contain health graphics; retain portrait/name/skill controls.
        foreach(var plate in fighterPlates)if(plate)foreach(var child in plate.GetComponentsInChildren<Transform>(true))
            if(child.name=="Health Rail" || child.name=="Health Fill" || child.name=="Health Number" || child.name=="Damage Echo")child.gameObject.SetActive(false);
        foreach(var value in damageText)if(value)value.gameObject.SetActive(false);
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
        if(pauseButtons.Length>4 && pauseButtons[4])pauseButtons[4].onClick.AddListener(OpenMoveGuide);
        if(guideCloseButton)guideCloseButton.onClick.AddListener(CloseMoveGuide);
        for(int slot=0;slot<actionDiscs.Length;slot++){
            if(!actionDiscs[slot])continue;int captured=slot;
            if(actionButtons[slot])actionButtons[slot].onClick.AddListener(()=>{
                int player=captured/4;
                if(captured%4==0 && (pointerGuardHeld[player] || guardReleaseFrame[player]==Time.frameCount))return;
                ActivateAction(player,captured%4);
            });
            var events=actionDiscs[slot].gameObject.AddComponent<EventTrigger>();
            var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};enter.callback.AddListener(_=>hoveredAction[captured/4]=captured%4);events.triggers.Add(enter);
            var exit=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit};exit.callback.AddListener(_=>{hoveredAction[captured/4]=-1;if(captured%4==0)ReleasePointerGuard(captured/4);});events.triggers.Add(exit);
            if(captured%4==0){
                var down=new EventTrigger.Entry{eventID=EventTriggerType.PointerDown};down.callback.AddListener(data=>{var pointer=data as PointerEventData;if(pointer!=null && pointer.button!=PointerEventData.InputButton.Left)return;pointerGuardHeld[captured/4]=true;ActivateAction(captured/4,0);});events.triggers.Add(down);
                var up=new EventTrigger.Entry{eventID=EventTriggerType.PointerUp};up.callback.AddListener(_=>ReleasePointerGuard(captured/4));events.triggers.Add(up);
            }
        }
    }
    void Update()
    {
        if (!game) return;
        for(int player=0;player<2;player++)if(oneShotGuard[player] && !pointerGuardHeld[player])ReleasePointerGuard(player);
        RefreshMenu();
        AnimateFrontEnd();
        if (game.Screen != WoodlandGame.MenuScreen.Match || game.Paused) {
            ReleaseAllPointerGuards();
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
        if (helpPanel && toggle) { if(MoveGuideOpen)CloseMoveGuide();else OpenMoveGuide(); }
        bool menuHeld=false;
        if(game)for(int i=0;i<2;i++)menuHeld|=WoodlandGame.PadMenuChord(game.GetGamepad(i));
        if(!menuHeld)menuChordUsed=false;
    }
    public string MenuHint {
        get {
            var pad=game?game.GetGamepad(0)??game.GetGamepad(1):null;
            string style=WoodlandGame.GamepadStyle(pad);
            return style=="PLAYSTATION"?"MENU [OPTIONS]":style=="XBOX"?"MENU [≡]":pad!=null?"MENU [START]":"MENU [ESC]";
        }
    }
    public void OpenMoveGuide()
    {
        if(!helpPanel || !game || game.Screen!=WoodlandGame.MenuScreen.Match)return;
        RefreshMenu();RefreshMoveGuide();helpPanel.transform.SetAsLastSibling();helpPanel.SetActive(true);
        WoodlandEffects.Instance?.PlayCue(12,.18f);
    }
    public void CloseMoveGuide(){if(helpPanel)helpPanel.SetActive(false);guideClosedFrame=Time.frameCount;}
    void RefreshMoveGuide()
    {
        for(int i=0;i<2;i++){
            if(game.players.Length<=i || !game.players[i])continue;
            var actor=game.players[i];var pad=game.GetGamepad(i);string style=WoodlandGame.GamepadStyle(pad);
            bool ps=style=="PLAYSTATION", xbox=style=="XBOX";
            string punch=ps?"SQUARE":xbox?"X":pad!=null?"WEST":i==0?"T":"I";
            string kick=ps?"CIRCLE":xbox?"B":pad!=null?"EAST":i==0?"Y":"O";
            string jump=pad!=null?"D-PAD UP / STICK UP":i==0?"W":"UP";
            string skill=ps?"TRIANGLE":xbox?"Y":pad!=null?"NORTH":i==0?"G":"K";
            string guard=ps?"HOLD CROSS / L2":xbox?"HOLD A / LT":pad!=null?"HOLD SOUTH / LEFT TRIGGER":i==0?"HOLD S":"HOLD DOWN";
            string dash=ps?"L1 / R1":xbox?"LB / RB":pad!=null?"LEFT / RIGHT SHOULDER":i==0?"Q / E":"U / P";
            string ultimate=ps?"L2 + R2":xbox?"LT + RT":pad!=null?"BOTH TRIGGERS":i==0?"H":"L";
            string ranged=game.SkillDescriptions[Mathf.Clamp(actor.CharacterIndex,0,game.SkillDescriptions.Length-1)].Split('.')[0]+".";
            string finisher="Five confirmed hits cause a 3s knockdown. Push a downed rival into an open edge hatch for a ring-out.";
            if(guideNames[i]){guideNames[i].text=(i==1 && game.SoloMode?"CPU":"P"+(i+1))+"  "+actor.displayName.ToUpper();guideNames[i].color=actor.Element;}
            if(controlsText[i])controlsText[i].text=
                "<b>"+punch+"   PUNCH</b>\n8 stamina. Start a close-range combo.\n"+
                "<b>"+kick+"   KICK</b>\n6 stamina. Finish a string and launch.\n"+
                "<b>"+punch+", "+punch+", "+kick+"   THREE-HIT COMBO</b>\nConfirm P, P, K to launch. Repeated punches reset the route.\n"+
                "<b>"+jump+"   JUMP / DOUBLE JUMP</b>\n4 stamina on ground; 6 for the aerial jump.\n"+
                "<b>"+guard+"   GUARD / PARRY</b>\nBlock, then punish. Fresh guard parries; "+(i==0?"F":"J")+" grabs guards.\n"+
                "<b>"+dash+"   BACKDASH / FORWARD DASH</b>\n12 / 10 stamina. Backdash briefly avoids hits.\n"+
                "<b>"+skill+"   "+actor.SkillName+"</b>\n14 stamina. 0.85s charge, then launch. "+ranged+"\n"+
                "<b>"+ultimate+"   "+actor.UltimateName+"</b>\nOne hit square left. Once per round. "+finisher;
        }
    }
    bool menuChordUsed;
    void LateUpdate() => Refresh();
    public void Refresh()
    {
        if (!game || game.players == null) return;
        RefreshMenu();
        if(helpHint)helpHint.text=MenuHint;
        RefreshMoveGuide();
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < 2; i++) {
            if (i >= game.players.Length || !game.players[i]) continue;
            Scout scout = game.players[i];
            var pad=game.GetGamepad(i);
            string style=WoodlandGame.GamepadStyle(pad);
            if(deviceText[i])deviceText[i].text=i==1 && game.SoloMode?"CPU RIVAL  /  "+scout.displayName.ToUpper():style=="KEYBOARD"?"KEYBOARD  /  "+(i==0?"A D + W":"ARROW KEYS"):style+"  /  PAD "+(i+1);
            RefreshActionControls(i,scout,pad,style);
            if(scout.UltimateActive && !cutinWasActive[i])cutinStarted[i]=Time.unscaledTime;
            float cutinAge=Time.unscaledTime-cutinStarted[i];
            bool showCutin=scout.UltimateActive && !game.Paused && cutinAge<.72f;
            if(ultimateCutins[i]){
                ultimateCutins[i].SetActive(showCutin);
                if(showCutin){
                    var group=ultimateCutins[i].GetComponent<CanvasGroup>();
                    group.alpha=Mathf.Min(Mathf.Clamp01(cutinAge/.1f),Mathf.Clamp01((.72f-cutinAge)/.18f));
                    ultimateCutins[i].transform.localScale=Vector3.one*Mathf.Lerp(.88f,1,Mathf.SmoothStep(0,1,cutinAge/.16f));
                    var frames=scout.animations.ultimate;
                    if(ultimateCutinArt[i] && frames!=null && frames.Length>0)ultimateCutinArt[i].sprite=frames[Mathf.Min(frames.Length-1,Mathf.FloorToInt(cutinAge*14))];
                    if(ultimateCutinNames[i]){ultimateCutinNames[i].text=scout.UltimateName;ultimateCutinNames[i].color=scout.Element;}
                }
            }
            cutinWasActive[i]=scout.UltimateActive;

            Color accent = scout.Element;
            int character=game.CharacterChoices[i];
            if(fighterPortraits[i] && character>=0 && character<game.CharacterPortraits.Length)fighterPortraits[i].sprite=character<hudPortraits.Length && hudPortraits[character]?hudPortraits[character]:game.CharacterPortraits[character];
            float ratio = Mathf.Clamp01(scout.Health / Mathf.Max(1, scout.maxHealth));
            bool reset = previousHealth[i] < 0 || scout.Health > previousHealth[i];
            if (scout.Health < previousHealth[i]) { ghostDelay[i] = .42f; punch[i] = 1; }
            previousHealth[i] = scout.Health;
            for(int hit=0;hit<Scout.HitsToKnockdown;hit++) {
                var square=hitSquares[i*Scout.HitsToKnockdown+hit];if(!square)continue;
                square.color=hit<scout.HitsRemaining?accent:scout.Downed?new Color(.55f,.25f,.23f):new Color(.2f,.26f,.28f);
            }
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
            if(shieldText[i]) {shieldText[i].text="STAMINA";shieldText[i].color=scout.Stamina<20?new Color(1,.75f,.57f):new Color(.96f,.97f,.88f);}
            if(shieldFill[i]) shieldFill[i].fillAmount=Mathf.Lerp(shieldFill[i].fillAmount,scout.Stamina/Mathf.Max(1,scout.StaminaCapacity),1-Mathf.Exp(-14*dt));
            if(pushFill[i]) pushFill[i].fillAmount=scout.PushReady;
            if(punchFill[i]) punchFill[i].fillAmount=scout.PunchReady;
            for(int p=0;p<3;p++) if(roundPips[i*3+p]) roundPips[i*3+p].color=game.score[i]>p?accent:new Color(.22f,.29f,.28f);
            int combo=scout.ComboCount;
            if(combo>1 && combo!=previousCombo[i]) comboPop[i]=1;
            previousCombo[i]=combo;
            comboPop[i]=Mathf.MoveTowards(comboPop[i],0,dt*2.6f);
            if(statusText[i]) {
                string cue=scout.CombatCue;
                statusText[i].text=cue.Length>0?cue:scout.Downed?"DOWN!  "+scout.KnockdownRemaining.ToString("0.0")+"s":combo>1?combo+" HIT":"";
                statusText[i].color=new Color(accent.r,accent.g,accent.b,cue.Length>0 || scout.Downed?1:combo>1?Mathf.Clamp01(comboPop[i]*2):0);
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
        if(helpHint)helpHint.text=MenuHint;
        if(soundText && audio) {
            bool quiet=audio.Muted||audio.SfxVolume<=0;
            bool notice=quiet||Time.unscaledTime<audio.AudioNoticeUntil;soundText.gameObject.SetActive(notice);if(helpHint)helpHint.gameObject.SetActive(!notice);
            soundText.text=audio.Muted?"SOUND MUTED  /  M TO RESTORE":audio.SfxVolume<=0?"SOUND 0%  /  + TO RESTORE":"SOUND  "+Mathf.RoundToInt(audio.SfxVolume*100)+"%";
            var color=soundText.color;color.a=quiet?1:Mathf.Clamp01((audio.AudioNoticeUntil-Time.unscaledTime)*3);soundText.color=color;
        }
        if(roundTimerText) {
            roundTimerText.text=(game.SuddenDeath?"!!":Mathf.CeilToInt(game.RoundTimeRemaining).ToString("00"));
            roundTimerText.color=game.RoundTimeRemaining<=10?new Color(1,.35f,.25f):new Color(1,.94f,.8f);
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
            if(action==0){accepted=scout.SetPointerGuard(true);if(!pointerGuardHeld[player])oneShotGuard[player]=true;}
            else if(!scout.ActionBusy)accepted=action==1?scout.TryAttack(true):action==2?scout.TryAttack(false):scout.TrySpecial();
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
    public void ReleasePointerGuard(int player)
    {
        if(player<0 || player>=2 || !pointerGuardHeld[player] && !oneShotGuard[player])return;
        if(pointerGuardHeld[player])guardReleaseFrame[player]=Time.frameCount;
        pointerGuardHeld[player]=oneShotGuard[player]=false;
        if(game && game.players!=null && player<game.players.Length && game.players[player])game.players[player].SetPointerGuard(false);
    }
    void ReleaseAllPointerGuards(){for(int player=0;player<2;player++)ReleasePointerGuard(player);}
    void OnDisable()=>ReleaseAllPointerGuards();
    void OnApplicationFocus(bool focus){if(!focus)ReleaseAllPointerGuards();}
    void ObserveAcceptedActions(int player,Scout scout)
    {
        if(observationsReady[player]){
            bool guard=scout.Blocking && observedState[player]!="Block";
            bool charging=scout.Charging && observedState[player]!="Charge";
            bool[] accepted={guard,scout.PunchReady<observedPunch[player]-.025f,scout.PushReady<observedKick[player]-.025f,charging};
            for(int action=0;action<4;action++)if(accepted[action]){int slot=player*4+action;if(Time.unscaledTime-lastAcceptedAt[slot]>.08f)PulseAction(slot,true);}
        }
        observationsReady[player]=true;observedPunch[player]=scout.PunchReady;observedKick[player]=scout.PushReady;observedState[player]=scout.Blocking?"Block":scout.Charging?"Charge":scout.AnimationState;
    }
    void RefreshActionControls(int player,Scout scout,Gamepad pad,string style)
    {
        if(player<actionClusters.Length && actionClusters[player]) {
            bool ringOut=game.players[0].RingOutFalling || game.players[1].RingOutFalling || game.players[0].KnockedOut || game.players[1].KnockedOut;
            var group=actionClusters[player];group.alpha=Mathf.MoveTowards(group.alpha,ringOut?.08f:1,Time.unscaledDeltaTime*9);
            group.interactable=group.blocksRaycasts=!ringOut;
        }
        bool cpu=player==1 && game.SoloMode;
        bool ps=pad!=null && style=="PLAYSTATION";
        string[] letters=pad==null?(player==0?new[]{"S","T","Y","G"}:new[]{"DN","I","O","K"}):style=="XBOX"?new[]{"A","X","B","Y"}:new[]{"S","W","E","N"};
        float dt=Time.unscaledDeltaTime,ease=1-Mathf.Exp(-24*dt);
        ObserveAcceptedActions(player,scout);
        for(int a=0;a<4;a++){
            int slot=player*4+a,coverIndex=Mathf.Clamp(scout.CharacterIndex,0,game.CharacterNames.Length-1)*4+a;
            if(actionCovers[slot]){
                int character=Mathf.Clamp(scout.CharacterIndex,0,game.CharacterAnimationSets.Length-1);
                var set=game.CharacterAnimationSets[character];
                if(a==0 && character>=0 && character<characterGuardCovers.Length && characterGuardCovers[character])actionCovers[slot].sprite=characterGuardCovers[character];
                else if(a!=0 && coverIndex<characterActionCovers.Length && characterActionCovers[coverIndex])actionCovers[slot].sprite=characterActionCovers[coverIndex];
            }
            if(actionIcons[slot]){actionIcons[slot].enabled=!cpu&&ps;actionIcons[slot].sprite=playstationIcons[a];actionIcons[slot].color=Color.white;}
            if(actionGlyphs[slot]){actionGlyphs[slot].text=cpu?"CPU":ps?"":letters[a];actionGlyphs[slot].fontSize=cpu?12:letters[a].Length>2?12:letters[a].Length>1?15:20;actionGlyphs[slot].color=pad==null?new Color(.91f,.95f,.86f):a==0?new Color(.51f,.89f,.53f):a==1?new Color(.44f,.71f,1):a==2?new Color(1,.48f,.43f):new Color(1,.82f,.35f);}
            bool pressed=false,rawPress=false;
            if(!cpu && pad!=null){var button=a==0?pad.buttonSouth:a==1?pad.buttonWest:a==2?pad.buttonEast:pad.buttonNorth;pressed=button.isPressed;rawPress=button.wasPressedThisFrame;}
            else if(!cpu && Keyboard.current!=null){var keys=Keyboard.current;var button=player==0?(a==0?keys.sKey:a==1?keys.tKey:a==2?keys.yKey:keys.gKey):(a==0?keys.downArrowKey:a==1?keys.iKey:a==2?keys.oKey:keys.kKey);pressed=button.isPressed;rawPress=button.wasPressedThisFrame;}
            if(a==0)pressed|=pointerGuardHeld[player] || scout.Blocking;
            if(a==3)pressed|=scout.Charging;
            if(rawPress){recentAction[player]=a;tooltipUntil[player]=Time.unscaledTime+.7f;if(Time.unscaledTime-lastAcceptedAt[slot]>.05f)pendingAttemptUntil[slot]=Time.unscaledTime+.48f;}
            if(pendingAttemptUntil[slot]>0 && Time.unscaledTime>=pendingAttemptUntil[slot])PulseAction(slot,false);
            actionHeld[slot]=pressed;
            rippleLife[slot]=Mathf.MoveTowards(rippleLife[slot],0,dt*2.9f);rejectionLife[slot]=Mathf.MoveTowards(rejectionLife[slot],0,dt*4.5f);
            bool sustained=a==0 && scout.Blocking || a==3 && scout.Charging;
            float acceptedPulse=Mathf.Max(rippleLife[slot],sustained?.14f+.07f*Mathf.Sin(Time.unscaledTime*7):0),denied=rejectionLife[slot];
            float targetScale=1+acceptedPulse*.13f-denied*.035f-(pressed && acceptedPulse<=0 && denied<=0?.05f:0);
            if(actionDiscs[slot])actionDiscs[slot].rectTransform.localScale=Vector3.Lerp(actionDiscs[slot].rectTransform.localScale,Vector3.one*targetScale,ease);
            if(actionCovers[slot]){
                Color tint=Color.Lerp(Color.white,scout.Element,acceptedPulse*.35f);tint=Color.Lerp(tint,new Color(1,.36f,.29f),denied*.62f);actionCovers[slot].color=Color.Lerp(actionCovers[slot].color,tint,ease);
                actionCovers[slot].rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Sin((1-acceptedPulse)*Mathf.PI*5)*acceptedPulse*4);
            }
            if(actionRipples[slot]){var ring=scout.Element;ring.a=acceptedPulse*.95f;actionRipples[slot].color=ring;actionRipples[slot].rectTransform.localScale=Vector3.one*(1+(1-acceptedPulse)*.4f);}
            if(actionRejectMarks[slot])actionRejectMarks[slot].color=new Color(1,.36f,.29f,denied*.9f);
        }
        if(specialCooldown[player]){specialCooldown[player].gameObject.SetActive(scout.Charging);specialCooldown[player].fillAmount=scout.Charge;specialCooldown[player].fillClockwise=true;var chargeColor=scout.Element;chargeColor.a=.6f;specialCooldown[player].color=chargeColor;}
        if(specialCooldownText[player]){specialCooldownText[player].fontSize=15;specialCooldownText[player].text=scout.Charging?"CHARGE\n"+Mathf.RoundToInt(scout.Charge*100)+"%":"";}
        int tip=hoveredAction[player]>=0?hoveredAction[player]:recentAction[player];
        bool visible=tip>=0 && (hoveredAction[player]>=0 || Time.unscaledTime<tooltipUntil[player]);
        if(actionTooltipPanels[player])actionTooltipPanels[player].SetActive(visible);
        if(visible && actionTooltips[player])actionTooltips[player].text=tip==0?"HOLD GUARD / PARRY":tip==1?"PUNCH / COMBO":tip==2?"KICK / LAUNCH":scout.SkillName.ToUpper()+"\n"+(scout.Charging?"CHARGING "+Mathf.RoundToInt(scout.Charge*100)+"%":"0.85s WIND-UP / 14 STAMINA");
        bool ultimate=scout.UltimateReady || scout.UltimateActive;
        if(ultimatePanels[player])ultimatePanels[player].SetActive(ultimate);
        if(ultimateLabels[player]){
            string binding=cpu?"CPU":pad==null?(player==0?"H":"L"):ps?"L2 + R2":style=="XBOX"?"LT + RT":"BOTH TRIGGERS";
            ultimateLabels[player].text=scout.UltimateActive?scout.UltimateName.ToUpper():"ULTIMATE READY: "+scout.UltimateName.ToUpper()+" ["+binding+"]";
            var color=new Color(1,.86f,.48f);color.a=scout.UltimateActive?1:.86f+Mathf.Sin(Time.unscaledTime*4)*.14f;ultimateLabels[player].color=color;
        }
        if(fighterPortraitRings[player]){var color=ultimate?Color.Lerp(scout.Element,new Color(1,.85f,.4f),.5f+.25f*Mathf.Sin(Time.unscaledTime*4)):scout.Element;color.a=ultimate?.95f:.7f;fighterPortraitRings[player].color=color;}
    }
    static readonly string[] mapDescriptions={"Rain-dark streets. Elemental lamps. A crowd beyond the combat lane.","Moonstone, falling water and a silver edge.","Glowing embers. A battle under red leaves.","Ancient sky ruins. A fight above the clouds.","Stormglass towers. Open shafts guard both edges.","Ancient roots and dark cisterns. Open shafts widen after 99 seconds."};
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
        if(pauseMenu)pauseMenu.SetActive(match && game.Paused && !MoveGuideOpen);
        if(screen!=lastScreen){
            if(enteringGroup)enteringGroup.alpha=1;
            if(enteringScreen)enteringScreen.transform.localScale=Vector3.one;
            enteringScreen=screen==WoodlandGame.MenuScreen.Title?titleMenu:screen==WoodlandGame.MenuScreen.Characters?characterMenu:screen==WoodlandGame.MenuScreen.Maps?mapMenu:screen==WoodlandGame.MenuScreen.Settings?settingsMenu:null;
            enteringGroup=enteringScreen?enteringScreen.GetComponent<CanvasGroup>():null;
            if(enteringScreen && !enteringGroup)enteringGroup=enteringScreen.AddComponent<CanvasGroup>();
            screenEnteredAt=Time.unscaledTime;
            ReleaseAllPointerGuards();
            menuFocus=screen==WoodlandGame.MenuScreen.Rules && game.BalanceMode?1:0;nextNavigation=Time.unscaledTime+.12f;lastScreen=screen;if(helpPanel)helpPanel.SetActive(false);
            for(int i=0;i<2;i++){hoveredAction[i]=recentAction[i]=-1;tooltipUntil[i]=0;observationsReady[i]=false;}
            for(int i=0;i<8;i++){actionHeld[i]=false;rippleLife[i]=rejectionLife[i]=pendingAttemptUntil[i]=0;}
        }
        if(game.Paused && !wasPaused){pauseFocus=0;nextNavigation=Time.unscaledTime+.12f;if(helpPanel)helpPanel.SetActive(false);}
        wasPaused=game.Paused;
        string rules="DUEL";
        if(modeText)modeText.text=(game.SoloMode?"SINGLE PLAYER":"LOCAL MULTIPLAYER")+"  /  "+rules;
        int arena=Mathf.Clamp(game.PendingArena,0,mapDescriptions.Length-1);
        if(mapDescription)mapDescription.text=mapDescriptions[arena];
        if(mapPreview && game.backgrounds.Length>arena)mapPreview.sprite=game.backgrounds[arena];
        for(int i=0;i<modeButtons.Length;i++)MarkButton(modeButtons[i],i==menuFocus,i==0?fernColor:emberColor);
        for(int i=0;i<ruleButtons.Length;i++)MarkButton(ruleButtons[i],i==menuFocus,i==0?fernColor:emberColor);
        for(int i=0;i<mapButtons.Length;i++)MarkButton(mapButtons[i],i==arena,i==0?fernColor:i==1?new Color(.45f,.78f,1):emberColor);
        for(int i=0;i<pauseButtons.Length;i++)MarkButton(pauseButtons[i],i==pauseFocus,fernColor);
        int scout=Mathf.Clamp(game.PendingCharacter,0,game.CharacterNames.Length-1);
        if(characterHeading)characterHeading.text="PLAYER "+(game.ChoosingPlayer+1)+" / CHOOSE YOUR FIGHTER";
        if(selectedArenaName)selectedArenaName.text=game.arenaNames[arena];
        for(int side=0;side<2;side++) {
            bool choosing=side==game.ChoosingPlayer;
            int selected=choosing?scout:Mathf.Clamp(game.CharacterChoices[side],0,game.CharacterNames.Length-1);
            if(selectionPortraits.Length>side && selectionPortraits[side])selectionPortraits[side].transform.parent.gameObject.SetActive(choosing);
            if(selectionNames.Length>side && selectionNames[side]){selectionNames[side].text=game.CharacterNames[selected].ToUpper();selectionNames[side].color=game.CharacterColors[selected];}
            if(selectionSkills.Length>side && selectionSkills[side])selectionSkills[side].text=game.SkillNames[selected];
            if(selectionStates.Length>side && selectionStates[side])selectionStates[side].text=choosing?"SELECTING":side<game.ChoosingPlayer?"LOCKED IN":game.SoloMode?"CPU RIVAL":"AWAITING PLAYER";
        }
        if(characterDetail)characterDetail.text=game.CharacterNames[scout]+" / "+game.SkillNames[scout]+"\n"+game.SkillDescriptions[scout];
        for(int i=0;i<characterButtons.Length;i++){
            if(!characterButtons[i])continue;bool available=game.CharacterAvailable(i);characterButtons[i].interactable=available;
            MarkButton(characterButtons[i],i==scout,game.CharacterColors[i]);
            for(int side=0;side<2;side++) {var marker=characterButtons[i].transform.Find(side==0?"P1 Marker":"P2 Marker");if(marker)marker.gameObject.SetActive(i==(side==game.ChoosingPlayer?scout:game.CharacterChoices[side]));}
            var image=characterButtons[i].transform.Find("Scout Portrait");if(image){var portrait=image.GetComponent<Image>();if(portrait)portrait.color=available?Color.white:new Color(.35f,.4f,.39f,.7f);}
        }
        if(confirmCharacterButton)confirmCharacterButton.interactable=game.CharacterAvailable(scout);
        if(settingsSummary)settingsSummary.text="MASTER VOLUME  "+Mathf.RoundToInt(game.SettingsVolume*100)+"%";
        if(settingsButtons.Length>2)SetButtonTitle(settingsButtons[2],"FULLSCREEN  /  "+(game.SettingsFullscreen?"ON":"OFF"));
        if(settingsButtons.Length>3)SetButtonTitle(settingsButtons[3],"CAMERA SHAKE  /  "+(game.CameraShakeEnabled?"ON":"OFF"));
        if(settingsButtons.Length>4)SetButtonTitle(settingsButtons[4],"EFFECTS QUALITY  /  "+game.EffectsQualityName.ToUpper());
        for(int i=0;i<settingsButtons.Length;i++)MarkButton(settingsButtons[i],i==menuFocus,fernColor);
    }
    void AnimateFrontEnd()
    {
        if(game && game.Screen==WoodlandGame.MenuScreen.Characters) {
            int character=game.PendingCharacter,side=game.ChoosingPlayer;
            if(character!=showcaseIdentity || side!=showcasePlayer) {
                showcaseIdentity=character;showcasePlayer=side;showcaseChangedAt=Time.unscaledTime;
            }
            if(side<selectionPortraits.Length && selectionPortraits[side]) {
                var hero=selectionPortraits[side];
                // Held key poses make one relaxed breath; artwork carries the movement.
                float elapsed=Time.unscaledTime-showcaseChangedAt;
                int pose=Mathf.FloorToInt(elapsed/.38f)%4,index=character*4+pose;
                hero.sprite=index<selectionIdleFrames.Length && selectionIdleFrames[index]?selectionIdleFrames[index]:game.CharacterProfiles[character];
                float enter=1-Mathf.Pow(1-Mathf.Clamp01(elapsed/.32f),3);
                hero.rectTransform.anchoredPosition=new Vector2(265-65*(1-enter),-245);
                hero.color=new Color(1,1,1,enter);
                if(side<selectionSweeps.Length && selectionSweeps[side]) {
                    var sweep=selectionSweeps[side];Color accent=game.CharacterColors[character];
                    sweep.color=new Color(accent.r,accent.g,accent.b,.42f*(1-enter));
                    sweep.rectTransform.anchoredPosition=new Vector2(180+180*enter,-230);
                    sweep.rectTransform.localScale=new Vector3(1.4f,.7f+.3f*enter,1);
                }
            }
        }
        if(game && game.Screen==WoodlandGame.MenuScreen.Maps) {
            if(lastArenaSelection!=game.PendingArena){lastArenaSelection=game.PendingArena;arenaSelectionChangedAt=Time.unscaledTime;}
            float selectionAge=Time.unscaledTime-arenaSelectionChangedAt;
            float atlasEase=1-Mathf.Exp(-14*Time.unscaledDeltaTime);
            for(int i=0;i<mapButtons.Length;i++) {
                var card=mapButtons[i];if(!card)continue;
                bool chosen=i==game.PendingArena;
                card.transform.localScale=Vector3.Lerp(card.transform.localScale,Vector3.one*(chosen?1.06f:1),atlasEase);
                var rect=card.GetComponent<RectTransform>();
                rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,new Vector2(25+i*204,chosen?-132:-144),atlasEase);
                var art=card.transform.Find("Arena Illustration");
                if(art){art.GetComponent<Image>().color=chosen?new Color(.8f,.8f,.88f):WoodlandGame.ArenaExposure;art.localScale=Vector3.Lerp(art.localScale,Vector3.one*(chosen?1.045f:1),atlasEase);}
                var flash=card.transform.Find("Selection Glint");if(flash)flash.GetComponent<Image>().color=new Color(.45f,.85f,1,chosen?.26f*(1-Mathf.Clamp01(selectionAge/.4f)):0);
                var stroke=card.transform.Find("Selection Stroke");if(stroke)stroke.gameObject.SetActive(chosen);
            }
        }
        if(!enteringScreen || !enteringGroup)return;
        float progress=Mathf.Clamp01((Time.unscaledTime-screenEnteredAt)/.26f);
        float ease=1-Mathf.Pow(1-progress,3);
        enteringGroup.alpha=Mathf.Lerp(.78f,1,ease);
        enteringScreen.transform.localScale=Vector3.one*Mathf.Lerp(.985f,1,ease);
    }
    static void SetButtonTitle(Button button,string value)
    {
        if(!button)return;var child=button.transform.Find("Choice");if(child){var text=child.GetComponent<Text>();if(text)text.text=value;}
    }
    static void MarkButton(Button button,bool chosen,Color accent)
    {
        if(!button) return;
        bool card=button.transform.Find("Painted Card Frame");
        button.image.color=chosen?(card?new Color(.20f,.12f,.35f,1):new Color(.78f,1,.25f,1)):new Color(.105f,.06f,.22f,.98f);
        var outline=button.GetComponent<Outline>();if(outline)outline.effectColor=chosen?accent:new Color(.32f,.23f,.47f,.85f);
        // Choice text belongs to this button, leaving all illustration and native layout editable.
        var description=button.transform.Find("Description");if(description){var label=description.GetComponent<Text>();if(label)label.color=chosen?new Color(.08f,.11f,.12f):new Color(.78f,.81f,.94f);}
        var text=button.transform.Find("Choice");if(text){var label=text.GetComponent<Text>();if(label)label.color=chosen?new Color(.035f,.025f,.08f):new Color(1,.97f,.85f);}
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
        if(MoveGuideOpen){
            if(back || confirm || (keys!=null && keys.f1Key.wasPressedThisFrame))CloseMoveGuide();
            return;
        }
        if(guideClosedFrame==Time.frameCount)return;
        bool analog=analogNext||analogPrevious;
        if(Time.unscaledTime>=nextNavigation){next|=analogNext;previous|=analogPrevious;}
        if(!analog && !next && !previous)nextNavigation=0;
        if(next || previous){
            int direction=next?1:-1;
            if(game.Screen==WoodlandGame.MenuScreen.Title)menuFocus=(menuFocus+direction+modeButtons.Length)%modeButtons.Length;
            else if(game.Screen==WoodlandGame.MenuScreen.Rules)menuFocus=(menuFocus+direction+2)%2;
            else if(game.Screen==WoodlandGame.MenuScreen.Characters){
                int candidate=game.PendingCharacter;
                for(int n=0;n<game.CharacterNames.Length;n++){candidate=(candidate+direction+game.CharacterNames.Length)%game.CharacterNames.Length;if(game.CharacterAvailable(candidate)){game.PreviewCharacter(candidate);break;}}
            }
            else if(game.Screen==WoodlandGame.MenuScreen.Maps)game.PreviewArena((game.PendingArena+direction+mapButtons.Length)%mapButtons.Length);
            else if(game.Screen==WoodlandGame.MenuScreen.Settings)menuFocus=(menuFocus+direction+settingsButtons.Length)%settingsButtons.Length;
            else pauseFocus=(pauseFocus+direction+pauseButtons.Length)%pauseButtons.Length;
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
