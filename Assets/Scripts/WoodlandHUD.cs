using UnityEngine;
using UnityEngine.UI;

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
    public Image[] roundPips = new Image[6];
    public GameObject bannerPanel;
    public Text bannerText;
    public Color fernColor = new Color(.53f, .81f, .43f);
    public Color emberColor = new Color(1, .62f, .30f);
    void LateUpdate()
    {
        if (game == null) return;
        for (int i = 0; i < 2; i++) {
            Scout scout = game.players[i];
            Color accent = i == 0 ? fernColor : emberColor;
            names[i].text = "P" + (i + 1) + "  " + scout.displayName.ToUpper();
            names[i].color = accent;
            healthText[i].text = Mathf.CeilToInt(scout.Health) + " / " + scout.maxHealth + " HP";
            healthFill[i].fillAmount = Mathf.MoveTowards(healthFill[i].fillAmount, scout.Health / scout.maxHealth, Time.unscaledDeltaTime * 2.5f);
            healthFill[i].color = Color.Lerp(new Color(.87f, .30f, .24f), accent, scout.Health / scout.maxHealth);
            statusText[i].text = scout.Status;
            statusText[i].color = scout.KnockedOut ? new Color(1, .48f, .32f) : new Color(.83f, .86f, .68f);
            damageText[i].text = "KNOCKBACK +" + Mathf.RoundToInt((1 - scout.Health / scout.maxHealth) * scout.damageKnockbackMultiplier * 100) + "%";
            pushFill[i].fillAmount = scout.PushReady;
            punchFill[i].fillAmount = scout.PunchReady;
            for (int p = 0; p < 3; p++) roundPips[i * 3 + p].color = game.score[i] > p ? accent : new Color(.23f, .29f, .23f);
        }
        string text = game.Paused ? "PAUSED\nESC TO RESUME" : game.message;
        bannerPanel.SetActive(!string.IsNullOrEmpty(text));
        bannerText.text = text;
    }
}
