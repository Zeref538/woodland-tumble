using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WoodlandHUD : MonoBehaviour
{
    public WoodlandGame game;
    public Text[] names = new Text[2];
    public Text[] healthText = new Text[2];
    public Text[] statusText = new Text[2];
    public Text[] damageText = new Text[2];
    public Image[] healthFill = new Image[2];
    public Image[] pushFill = new Image[2];
    public Image[] punchFill = new Image[2];
    public Image[] actionDiscs = new Image[8];
    public Image[] actionIcons = new Image[8];
    public Text[] actionGlyphs = new Text[8];
    public Text[] actionLabels = new Text[8];
    public Text[] defenseHints = new Text[2];
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
            if(controlsText[i])controlsText[i].text=i==1 && game.SoloMode?scout.displayName.ToUpper()+" / CPU RIVAL\nReads distance, guards and counters.\nBait a strike, punish the recovery.":pad==null?(i==0?"A / D   Move      W   Jump / double jump\nS   Block      Q   Backstep\nShift   Dash      E   Grab & toss\nG   Punch      F   Kick\nH   Hold and release special":"Arrows   Move      Up   Jump / double jump\nDown   Block      U   Backstep\nR Shift   Dash      O   Grab & toss\nL   Punch      K   Kick\nJ   Hold and release special"):
                "STICK / D-PAD   Move\n"+(style=="PLAYSTATION"?"CROSS   Block    SQUARE   Punch\nCIRCLE   Kick    TRIANGLE   Special\nL1   Jump    L2   Backstep\nR1   Dash    R2   Grab & toss":style=="XBOX"?"A   Block    X   Punch\nB   Kick    Y   Special\nLB   Jump    LT   Backstep\nRB   Dash    RT   Grab & toss":"SOUTH   Block    WEST   Punch\nEAST   Kick    NORTH   Special\nL SHOULDER   Jump    L TRIGGER   Backstep\nR SHOULDER   Dash    R TRIGGER   Grab");
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
            if(fighterPlates[i]) fighterPlates[i].localScale=Vector3.one*(1+.035f*punch[i]);
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

    void RefreshActionDiamond(int player,Scout scout,Gamepad pad,string style)
    {
        bool cpu=player==1 && game.SoloMode;
        bool ps=pad!=null && style=="PLAYSTATION";
        string[] letters=pad==null?(player==0?new[]{"S","G","F","H"}:new[]{"DOWN","L","K","J"}):style=="XBOX"?new[]{"A","X","B","Y"}:new[]{"S","W","E","N"};
        for(int a=0;a<4;a++){
            int slot=player*4+a;
            if(actionIcons[slot]){actionIcons[slot].enabled=cpu||ps;actionIcons[slot].sprite=cpu?neutralIcons[a]:playstationIcons[a];actionIcons[slot].color=cpu?(player==0?fernColor:emberColor):Color.white;}
            if(actionGlyphs[slot]){actionGlyphs[slot].text=cpu||ps?"":letters[a];actionGlyphs[slot].fontSize=letters[a].Length>2?10:17;actionGlyphs[slot].color=pad==null?new Color(.91f,.95f,.86f):a==0?new Color(.51f,.89f,.53f):a==1?new Color(.44f,.71f,1):a==2?new Color(1,.48f,.43f):new Color(1,.82f,.35f);}
            bool pressed=false;
            if(cpu)pressed=a==0?scout.Blocking:a==1?scout.Status.Contains("PUNCH"):a==2?scout.Status.Contains("KICK"):scout.Charging || scout.Status.Contains("SMASH");
            else if(pad!=null)pressed=a==0?pad.buttonSouth.isPressed:a==1?pad.buttonWest.isPressed:a==2?pad.buttonEast.isPressed:pad.buttonNorth.isPressed;
            else if(Keyboard.current!=null){var keys=Keyboard.current;pressed=player==0?(a==0?keys.sKey.isPressed:a==1?keys.gKey.isPressed:a==2?keys.fKey.isPressed:keys.hKey.isPressed):(a==0?keys.downArrowKey.isPressed:a==1?keys.lKey.isPressed:a==2?keys.kKey.isPressed:keys.jKey.isPressed);}
            if(actionDiscs[slot]){actionDiscs[slot].color=pressed?Color.Lerp(Color.white,player==0?fernColor:emberColor,.55f):Color.white;actionDiscs[slot].rectTransform.localScale=Vector3.one*(pressed?.91f:1);}
        }
        if(defenseHints[player])defenseHints[player].text=cpu?"RIVAL AI":pad==null?(player==0?"W JUMP / Q BACK\nSHIFT DASH / E GRAB":"UP JUMP / U BACK\nRSHIFT DASH / O GRAB"):ps?"L1 JUMP / L2 BACK\nR1 DASH / R2 GRAB":"LB JUMP / LT BACK\nRB DASH / RT GRAB";
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
        if(screen!=lastScreen){menuFocus=screen==WoodlandGame.MenuScreen.Rules && game.BalanceMode?1:0;nextNavigation=Time.unscaledTime+.12f;lastScreen=screen;if(helpPanel)helpPanel.SetActive(false);}
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
