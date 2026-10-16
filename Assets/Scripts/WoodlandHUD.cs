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
    public Text arenaText;
    public Text soundText;
    public Text helpHint;
    public Text[] shieldText = new Text[2];
    public Image[] shieldFill = new Image[2];
    public Image[] roundPips = new Image[6];
    public Image[] damageGhost = new Image[2];
    public RectTransform[] fighterPlates = new RectTransform[2];
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

    void Update()
    {
        if (helpPanel && Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        { helpPanel.SetActive(!helpPanel.activeSelf);WoodlandEffects.Instance?.PlayCue(12,.18f); }
    }
    void LateUpdate() => Refresh();
    public void Refresh()
    {
        if (!game || game.players == null) return;
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < 2; i++) {
            if (i >= game.players.Length || !game.players[i]) continue;
            Scout scout = game.players[i];
            Color accent = i == 0 ? fernColor : emberColor;
            float ratio = Mathf.Clamp01(scout.Health / Mathf.Max(1, scout.maxHealth));
            bool reset = previousHealth[i] < 0 || scout.Health > previousHealth[i];
            if (scout.Health < previousHealth[i]) { ghostDelay[i] = .42f; punch[i] = 1; }
            previousHealth[i] = scout.Health;
            if(names[i]) {names[i].text = "P"+(i+1)+"  "+scout.displayName.ToUpper();names[i].color=accent;}
            if(healthText[i]) healthText[i].text = Mathf.CeilToInt(scout.Health).ToString();
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
            if(shieldText[i]) {shieldText[i].text=scout.Shield<=0?"BROKEN":"WARD";shieldText[i].color=scout.Shield<=0?new Color(1,.4f,.3f):new Color(.57f,.7f,.76f);}
            if(shieldFill[i]) shieldFill[i].fillAmount=Mathf.Lerp(shieldFill[i].fillAmount,scout.Shield/Mathf.Max(1,scout.shieldCapacity),1-Mathf.Exp(-14*dt));
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
        if(soundText && audio) {
            bool notice=Time.unscaledTime<audio.AudioNoticeUntil;soundText.gameObject.SetActive(notice);if(helpHint)helpHint.gameObject.SetActive(!notice);
            soundText.text=audio.Muted?"SOUND MUTED":"SOUND  "+Mathf.RoundToInt(audio.SfxVolume*100)+"%";
            var color=soundText.color;color.a=Mathf.Clamp01((audio.AudioNoticeUntil-Time.unscaledTime)*3);soundText.color=color;
        }
        if(arenaText) arenaText.text=game.ArenaName+"   /   TAB SWITCH";
        string text=game.Paused?"PAUSED\nESC TO RESUME":game.message;
        if(bannerPanel) bannerPanel.SetActive(!string.IsNullOrEmpty(text));
        if(bannerText) bannerText.text=text;
    }
}
