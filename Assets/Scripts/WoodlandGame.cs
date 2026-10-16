using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WoodlandGame : MonoBehaviour
{
    public static WoodlandGame Instance { get; private set; }
    [Header("Editable scene references")]
    public Scout[] players = new Scout[2];
    public Rigidbody2D plank;
    public WoodlandHUD hud;
    [Header("Arena wardrobe")]
    public SpriteRenderer backgroundArt, plankArt, sphereArt;
    public Sprite[] backgrounds = new Sprite[3], planks = new Sprite[3], spheres = new Sprite[3];
    public string[] arenaNames = { "SUNLIT HOLLOW", "MOONLIT LAKE", "EMBER GROVE" };
    public int ArenaIndex { get; private set; }
    public string ArenaName => arenaNames[ArenaIndex];
    public void SelectArena(int index)
    {
        if (index < 0 || index >= backgrounds.Length || !backgrounds[index] || !planks[index] || !spheres[index]) return;
        bool changed=ArenaIndex!=index;ArenaIndex = index;
        if(changed)WoodlandEffects.Instance?.PlayCue(12,.18f);
        backgroundArt.sprite = backgrounds[index]; plankArt.sprite = planks[index]; sphereArt.sprite = spheres[index];
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
        roundRoutine = StartCoroutine(StartRound());
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
        StopAllCoroutines();
        score[0] = score[1] = 0;
        Time.timeScale = 1;
        Paused = false;
        roundRoutine = StartCoroutine(StartRound());
    }
    public void TogglePause() { Paused = !Paused; Time.timeScale = Paused ? 0 : 1; }
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.tabKey.wasPressedThisFrame) SelectArena((ArenaIndex + 1) % 3);
        if (keyboard.digit1Key.wasPressedThisFrame) SelectArena(0);
        if (keyboard.digit2Key.wasPressedThisFrame) SelectArena(1);
        if (keyboard.digit3Key.wasPressedThisFrame) SelectArena(2);
        if (keyboard.rKey.wasPressedThisFrame) RestartMatch();
        if (keyboard.escapeKey.wasPressedThisFrame) TogglePause();
    }
    void FixedUpdate()
    {
        if (playing) plank.AddTorque(-Mathf.DeltaAngle(plankAngle, plank.rotation) * plankRestoringForce);
    }
    void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; }
}
