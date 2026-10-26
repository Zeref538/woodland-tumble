using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Linq;

// Shared pool for native pixel shapes, sprite afterimages and woodland atmosphere.
public class WoodlandEffects : MonoBehaviour
{
    struct Mote
    {
        public SpriteRenderer art;
        public Vector2 velocity, startSize, endSize;
        public float life, total, spin, gravity, drag;
        public Color color;
        public Sprite[] frames;
    }
    [Header("Effect tuning")]
    public float trailDuration = .22f;
    public float punchShake = .09f;
    public float pushShake = .035f;
    public float particleGravity = 7;
    [Range(.25f, 2)] public float effectIntensity = 1.35f;
    public bool ambientFireflies = true;
    public Color ambientTint = new Color(1,.86f,.42f);
    public bool cameraShake = true;
    public float footstepInterval = .16f;
    [Header("Elemental sprite animation")]
    public Sprite[] fernSlash=new Sprite[8],emberSlash=new Sprite[8];
    readonly Sprite[][] comboHits=new Sprite[8][],comboFinishers=new Sprite[8][],brushSwings=new Sprite[8][],brushGuards=new Sprite[8][];
    readonly Sprite[][] elementalShields=new Sprite[8][];
    public int ShieldFramesLoaded(int character)=>character>=0&&character<8?(elementalShields[character]?.Length??0):0;
    public int AttackFramesLoaded(int character)=>character>=0&&character<8?ComboFramesLoaded(character)+(brushSwings[character]?.Length??0)+(brushGuards[character]?.Length??0):0;
    public int ComboAnimationsPlayed { get; private set; }
    public int ComboFramesLoaded(int character)=>character>=0&&character<8?(comboHits[character]?.Length??0)+(comboFinishers[character]?.Length??0):0;
    [Header("Action sound mix")]
    public AudioClip[] soundBank = new AudioClip[13];
    [Range(0,1)] public float masterVolume = .65f;
    public float SfxVolume => masterVolume;
    public bool Muted { get; private set; }
    public float AudioNoticeUntil { get; private set; }
    public int SoundsPlayed { get; private set; }
    readonly AudioSource[] voices = new AudioSource[8];
    readonly float[] voiceGains = new float[8];
    int voiceCursor;
    float nextStepSound;
    Coroutine hitPause;
    Vector2 cameraOffset, cameraVelocity;
    float cameraZoom;
    Scout cinematicScout;
    Vector2 cinematicFocus;
    float cinematicImpactUntil, nextCinematicMote;
    float cinematicStarted, cinematicExitUntil;
    readonly SpriteRenderer[] cinematicLayers = new SpriteRenderer[4];
    public bool CinematicActive => cinematicScout != null;
    public int UltimateCinematicsStarted { get; private set; }
    public int UltimateImpactsPlayed { get; private set; }
    public void SetArenaCamera(float size,Vector3 position)
    {
        if(!cam)cam=Camera.main;
        if(!cam)return;
        cinematicScout=null;cinematicImpactUntil=0;
        cameraHome=position;cameraZoom=size;cameraOffset=cameraVelocity=Vector2.zero;
        cam.transform.position=position;cam.orthographicSize=size;
    }
    const int Capacity = 320;
    readonly Mote[] pool = new Mote[Capacity];
    int cursor;
    Sprite pixel, leaf, star, ring, slash, glow, panel;
    Material material, glowMaterial;
    MaterialPropertyBlock textureBinding;
    Camera cam;
    Vector3 cameraHome;
    float shake, nextAmbient, nextWeather;
    public static WoodlandEffects Instance;
    public int ActiveMotes { get { int count = 0; foreach (var mote in pool) if (mote.life > 0) count++; return count; } }

    void Awake()
    {
        Instance = this;
        effectIntensity = Mathf.Clamp(PlayerPrefs.GetFloat("Woodland.EffectsQuality",1.35f),.25f,2);
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-silent-test")>=0)AudioListener.volume=0;
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Woodland.SfxVolume", masterVolume));
        Muted = PlayerPrefs.GetInt("Woodland.SfxMuted",0) == 1;
        for(int i=0;i<voices.Length;i++) { voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;voices[i].priority=100; }

        textureBinding = new MaterialPropertyBlock();
        cam = Camera.main;
        if (cam) { cameraHome = cam.transform.position; cameraZoom = cam.orthographicSize; }
        material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        pixel = Shape("Dust", 8, 8, (x,y) => Mathf.Abs(x-3)+Mathf.Abs(y-3)<5);
        panel = Shape("Cinematic solid",2,2,(x,y)=>true);
        leaf = Shape("Leaf", 12, 12, (x,y) => Mathf.Abs(x-y)<4 && x+y>3 && x+y<18);
        star = Shape("Spark", 16, 16, (x,y) => Mathf.Abs(x-7)<2 || Mathf.Abs(y-7)<2 || Mathf.Abs(x-7)+Mathf.Abs(y-7)<6);
        ring = Shape("Pixel shockwave", 32, 32, (x,y) => { float d = Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f));return d>12 && d<15; });
        slash = Shape("Attack arc", 32, 32, (x,y) => { float d = Vector2.Distance(new Vector2(x,y),new Vector2(7,15));return x>11 && d>15 && d<20; });
        glow = SoftGlow();
        glowMaterial = new Material(material); glowMaterial.SetColor("_Color", new Color(2.4f,2.4f,2.4f,1));
        material.mainTexture = pixel.texture;
        string[] characters={"Fern","Ember","Luna","Bram","Zephyr","Volt","Nyx","Marin"};
        for(int character=0;character<characters.Length;character++) {
            brushSwings[character]=LoadComboFrames(characters[character],"Slash");
            brushGuards[character]=LoadComboFrames(characters[character],"Guard");
            elementalShields[character]=Resources.LoadAll<Sprite>("Art/ElementShieldsV14/"+characters[character]).OrderBy(frame=>frame.name).ToArray();
            comboHits[character]=LoadComboFrames(characters[character],"Hit");
            comboFinishers[character]=LoadComboFrames(characters[character],"Finisher");
        }
        for (int i=0; i<Capacity; i++) {
            var go = new GameObject("Pooled woodland VFX");go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();sr.sharedMaterial = material;sr.enabled = false;pool[i].art = sr;
        }
        for(int i=0;i<cinematicLayers.Length;i++) {
            var layer=new GameObject("Ultimate cinematic layer");layer.transform.SetParent(transform);
            var sr=layer.AddComponent<SpriteRenderer>();sr.sharedMaterial=material;sr.sprite=panel;
            textureBinding.Clear();textureBinding.SetTexture("_MainTex",panel.texture);sr.SetPropertyBlock(textureBinding);
            sr.sortingOrder=i==0?3:14;sr.enabled=false;cinematicLayers[i]=sr;
        }
    }
    void OnEnable()
    {
        Scout.UltimateCinematic += UltimateCinematic;
        Scout.UltimateImpact += UltimateImpact;
    }
    void OnDisable()
    {
        Scout.UltimateCinematic -= UltimateCinematic;
        Scout.UltimateImpact -= UltimateImpact;
        RestoreCinematic();
    }
    public void ResetCameraPresentation(bool holdFraming=false)
    {
        if(holdFraming && cam) {
            Vector3 position=cam.transform.position;float size=cam.orthographicSize;
            RestoreCinematic();cam.transform.position=position;cam.orthographicSize=size;
        } else RestoreCinematic();
    }
    void RestoreCinematic()
    {
        cinematicScout = null; cinematicImpactUntil = 0;
        cinematicExitUntil=0;
        foreach(var layer in cinematicLayers)if(layer)layer.enabled=false;
        cameraOffset = cameraVelocity = Vector2.zero; shake = 0;
        if (cam) { cam.transform.position = cameraHome; cam.orthographicSize = cameraZoom; }
    }
    void UltimateCinematic(Scout caster, bool active)
    {
        if (!active) {
            if(cinematicScout==caster) {
                RestoreCinematic();
                var match=WoodlandGame.Instance;
                if(match&&match.playing&&!match.Paused)cinematicExitUntil=Time.unscaledTime+.18f;
            }
            return;
        }
        var game = WoodlandGame.Instance;
        if (!caster || !game || !game.playing || game.Paused) return;
        cinematicScout = caster;
        cinematicStarted=Time.unscaledTime;cinematicExitUntil=0;
        Vector2 rival = game.players[1-caster.id].Body.position;
        // Camera focus is presentation only. Neither fighters nor attack paths are moved or tracked.
        cinematicFocus = Vector2.Lerp(caster.Body.position, rival, .35f);
        cinematicFocus.x = Mathf.Clamp(cinematicFocus.x, cameraHome.x-1, cameraHome.x+1);
        cinematicFocus.y = Mathf.Clamp(cinematicFocus.y, cameraHome.y-.55f, cameraHome.y+.55f);
        nextCinematicMote = 0;
        UltimateCinematicsStarted++;
        Pulse(caster.Body.position, caster.Element, 2.2f, .55f, true);
        Light(caster.Body.position, caster.Element, 2.7f, .55f);
        Ghost(caster.art,caster.Element);
        CinematicAccents(caster.Body.position,caster.Element);
        PlayCue(4,.32f,.75f);
    }
    void UltimateImpact(Scout caster, Scout victim)
    {
        if (!caster || !victim) return;
        UltimateImpactsPlayed++;
        Vector2 point = victim.Body.position;
        Color tint = caster.Element;
        int character = caster.CharacterIndex;
        ComboImpact(point, caster.Facing, character, true);
        CinematicAccents(point,tint);
        Ghost(victim.art,tint);
        Light(point, tint, 3.4f, .45f);
        if (character == 0) {
            // Curved leafy strands leave the target readable inside the binding effect.
            for (int strand=0;strand<3;strand++) for(int step=0;step<6;step++) {
                float phase=strand*Mathf.PI*2/3+step*.65f;
                Vector2 offset=new Vector2(Mathf.Cos(phase)*.65f,-.6f+step*.22f);
                Emit(leaf,point+offset,new Vector2(-Mathf.Sin(phase)*.3f,.35f),tint,.55f+step*.025f,
                    Vector2.one*.13f,Vector2.one*.04f,0,10,phase*Mathf.Rad2Deg,100);
            }
            Pulse(point+Vector2.down*.5f,tint,3.2f,.5f,true);
        } else if (character == 1) {
            // Every confirmed phoenix contact throws a separate radial flame burst.
            Burst(point,Vector2.up,24,tint,4.2f);
            for (int i=0;i<8;i++) {
                float a=i*45*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                Emit(leaf,point,direction*3.6f,Color.Lerp(tint,Gold,.35f),.35f,new Vector2(.5f,.2f),Vector2.zero,0,11,i*45,140);
            }
            Pulse(point,Gold,2.8f,.25f);
        } else if (character == 2) {
            // Frost spokes and a crystalline ring mark a confirmed moonfrost freeze.
            Pulse(point,tint,2.7f,.65f);Pulse(point,Color.white,1.8f,.35f);
            for (int i=0;i<12;i++) {
                float a=i*30*Mathf.Deg2Rad;
                Vector2 direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                Emit(slash,point,direction*1.4f,tint,.6f,new Vector2(.5f,.6f),Vector2.zero,0,11,i*30,40);
                Emit(star,point+direction*.5f,Vector2.up*.25f,Color.white,.55f,Vector2.one*.14f,Vector2.zero,0,12,45,35);
            }
        } else {
            // Stone columns erupt outward from the hit, then fall with visible weight.
            for (int i=0;i<7;i++) {
                Vector2 origin=point+new Vector2((i-3)*.32f,-.65f);
                Emit(pixel,origin,new Vector2((i-3)*.3f,2.7f),tint,.65f,new Vector2(.25f,.25f),new Vector2(.15f,.9f),5,10,i*13,i*25);
            }
            Burst(point+Vector2.down*.6f,Vector2.up,26,tint,3.2f);
            Pulse(point+Vector2.down*.6f,tint,3.8f,.45f,true);
        }
        var game=WoodlandGame.Instance;
        if (game && game.playing && !game.Paused && cinematicScout==caster) cinematicImpactUntil=Time.unscaledTime+.23f;
        KickCamera(new Vector2(caster.Facing,.35f),.12f);
    }
    Sprite SoftGlow()
    {
        const int size=64;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Soft elemental light",filterMode=FilterMode.Bilinear};
        for(int y=0;y<size;y++) for(int x=0;x<size;x++) {
            float d=Vector2.Distance(new Vector2(x,y),Vector2.one*(size-1)*.5f)/(size*.5f);
            texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2.8f)));
        }
        texture.Apply();return Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,size);
    }
    void Light(Vector2 point,Color tint,float size,float duration=.24f)
    {
        Emit(glow,point,Vector2.zero,new Color(tint.r,tint.g,tint.b,.24f),duration,Vector2.one*size,Vector2.one*size*1.35f,0,4);
    }
    public void SetVolume(float value,bool showNotice=true)
    {
        AudioNoticeUntil=showNotice?Time.unscaledTime+2:0;
        masterVolume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("Woodland.SfxVolume",masterVolume);
        for(int i=0;i<voices.Length;i++)if(voices[i])voices[i].volume=Muted?0:masterVolume*voiceGains[i];
    }
    public void ToggleMute()
    {
        Muted=!Muted;PlayerPrefs.SetInt("Woodland.SfxMuted",Muted?1:0);SetVolume(masterVolume);
    }
    public bool PlayCue(int index,float gain=.5f,float pitch=1)
    {
        var game=WoodlandGame.Instance;
        if(Muted||masterVolume<=0||(game&&game.Paused)||index<0||index>=soundBank.Length||!soundBank[index])return false;
        int slot=voiceCursor++%voices.Length;var voice=voices[slot];
        voiceGains[slot]=Mathf.Clamp01(gain);voice.volume=masterVolume*voiceGains[slot];voice.pitch=Mathf.Clamp(pitch,.5f,1.8f);
        voice.clip=soundBank[index];voice.Play();SoundsPlayed++;return true;
    }
    void Update()
    {
        var game=WoodlandGame.Instance;
        if (cinematicScout && (!game || !game.playing || game.Paused || !cinematicScout.UltimateActive)) RestoreCinematic();
        if (cinematicScout && Time.time>=nextCinematicMote) {
            nextCinematicMote=Time.time+.1f;
            Windup(cinematicScout.Body.position+Vector2.up*.2f,cinematicScout.Facing,cinematicScout.Element);
        }
        var keys=Keyboard.current;
        if(keys!=null){
            if(keys.mKey.wasPressedThisFrame)ToggleMute();
            if(keys.minusKey.wasPressedThisFrame||keys.numpadMinusKey.wasPressedThisFrame)SetVolume(masterVolume-.1f);
            if(keys.equalsKey.wasPressedThisFrame||keys.numpadPlusKey.wasPressedThisFrame)SetVolume(masterVolume+.1f);
        }
        AudioListener.pause=WoodlandGame.Instance&&WoodlandGame.Instance.Paused;
        int active=0;foreach(var voice in voices)if(voice.isPlaying)active++;
        float duck=1/Mathf.Sqrt(Mathf.Max(1,active));
        for(int i=0;i<voices.Length;i++)voices[i].volume=Mathf.Lerp(voices[i].volume,Muted?0:masterVolume*voiceGains[i]*duck,1-Mathf.Exp(-20*Time.unscaledDeltaTime));
    }
    void KickCamera(Vector2 direction,float strength)
    {
        if(cameraShake)cameraVelocity+=direction.normalized*strength*18;
    }
    void FreezeImpact(float duration)
    {
        if(hitPause==null && WoodlandGame.Instance && !WoodlandGame.Instance.Paused)hitPause=StartCoroutine(ImpactPause(duration));
    }
    IEnumerator ImpactPause(float duration)
    {
        Time.timeScale=.08f;yield return new WaitForSecondsRealtime(duration);
        Time.timeScale=WoodlandGame.Instance&&WoodlandGame.Instance.Paused?0:1;hitPause=null;
    }
    Sprite Shape(string name, int width, int height, System.Func<int,int,bool> visible)
    {
        var texture = new Texture2D(width,height,TextureFormat.RGBA32,false) { name=name, filterMode=FilterMode.Point };
        for (int y=0; y<height; y++) for (int x=0; x<width; x++) texture.SetPixel(x,y,visible(x,y)?Color.white:Color.clear);
        texture.Apply();return Sprite.Create(texture,new Rect(0,0,width,height),Vector2.one*.5f,width);
    }
    int Emit(Sprite sprite, Vector2 point, Vector2 velocity, Color color, float life, Vector2 size, Vector2 endSize, float gravity=0, int order=9, float angle=0, float spin=0, float drag=0)
    {
        int i=cursor++%Capacity;var sr=pool[i].art;
        sr.enabled=true;sr.sharedMaterial=sprite==glow?glowMaterial:material;sr.sprite=sprite;sr.flipX=false;sr.sortingOrder=order;
        textureBinding.Clear();textureBinding.SetTexture("_MainTex",sprite.texture);sr.SetPropertyBlock(textureBinding);
        sr.transform.SetPositionAndRotation(point,Quaternion.Euler(0,0,angle));sr.transform.localScale=new Vector3(size.x,size.y,1);sr.color=color;
        pool[i].frames=null;
        pool[i].velocity=velocity;pool[i].color=color;pool[i].life=pool[i].total=Mathf.Max(.01f,life);
        pool[i].startSize=size;pool[i].endSize=endSize;pool[i].gravity=gravity;pool[i].spin=spin;pool[i].drag=drag;
        return i;
    }
    int Count(int count) => Mathf.Max(1,Mathf.RoundToInt(count*effectIntensity));
    Color Gold => new Color(1,.82f,.37f);
    public void Ghost(SpriteRenderer source, Color tint)
    {
        if (!source || !source.sprite) return;
        Vector3 scale=source.transform.lossyScale;
        int i=Emit(source.sprite,source.transform.position,Vector2.zero,new Color(tint.r,tint.g,tint.b,.32f),trailDuration,scale,scale*.98f,0,4,source.transform.eulerAngles.z);
        pool[i].art.sharedMaterial=source.sharedMaterial;pool[i].art.flipX=source.flipX;
    }
    public void Burst(Vector2 origin, Vector2 direction, int count, Color tint, float speed=3)
    {
        for (int n=0;n<Count(count);n++) {
            float size=Random.Range(.055f,.15f);
            Emit(n%3==0?leaf:pixel,origin,direction*speed+Random.insideUnitCircle*speed*.85f,Color.Lerp(tint,Gold,Random.value*.45f),Random.Range(.25f,.55f),Vector2.one*size,Vector2.zero,particleGravity,9,Random.Range(0,360),Random.Range(-320,320),.8f);
        }
    }
    void Pulse(Vector2 point, Color tint, float size, float duration=.25f, bool flat=false)
    {
        Emit(ring,point,Vector2.zero,new Color(tint.r,tint.g,tint.b,.68f),duration,new Vector2(.22f,flat?.07f:.22f),new Vector2(size,flat?size*.28f:size));
    }
    public void Footstep(Vector2 feet, float direction)
    {
        if(Time.time>=nextStepSound){nextStepSound=Time.time+.24f;PlayCue(6,.10f,Random.Range(.94f,1.06f));}
        Burst(feet,new Vector2(-direction,.25f),3,new Color(.57f,.68f,.32f),.65f);
    }
    public void Jump(Vector2 feet, Color tint)
    {
        PlayCue(3,.28f,Random.Range(.96f,1.04f));Light(feet,tint,1,.2f);
        Pulse(feet,tint,.9f,.25f,true);
        Burst(feet,new Vector2(-1,.2f),6,tint,1.7f);Burst(feet,new Vector2(1,.2f),6,tint,1.7f);
        for(int i=0;i<3;i++) Emit(leaf,feet+new Vector2((i-1)*.15f,.2f),Vector2.down*.6f,new Color(1,.92f,.65f,.6f),.2f,new Vector2(.12f,.2f),Vector2.zero,0,7,90,120);
    }
    public void Land(Vector2 feet, float speed)
    {
        float power=Mathf.Clamp(speed/8,.3f,1.2f);
        PlayCue(6,.16f+power*.12f,Random.Range(.94f,1.03f));
        Pulse(feet,Gold,1.2f*power,.25f,true);
        Burst(feet,new Vector2(-1,.25f),7,new Color(.66f,.69f,.38f),2*power);
        Burst(feet,new Vector2(1,.25f),7,new Color(.66f,.69f,.38f),2*power);
    }
    public void Windup(Vector2 point, float facing, Color tint)
    {
        for(int i=0;i<Count(9);i++) {
            Vector2 offset=Random.insideUnitCircle.normalized*Random.Range(.25f,.55f);
            Emit(i%3==0?star:pixel,point+offset,-offset/.18f,tint,.18f,Vector2.one*.07f,Vector2.one*.015f);
        }
        Emit(star,point,Vector2.zero,Gold,.18f,Vector2.one*.08f,Vector2.one*.22f,0,8,0,180);
    }
    // Presentation only: the controller owns charge duration, path and damage.
    public void ChargeAura(Vector2 point,int character,float charge)
    {
        charge=Mathf.Clamp01(charge);Color tint=CharacterTint(character);
        Vector2 feet=point+Vector2.down*.58f;
        Emit(ring,feet,Vector2.zero,new Color(tint.r,tint.g,tint.b,.42f),.18f,
            new Vector2(1.25f+charge*.6f,.28f),new Vector2(.55f,.1f),0,4,0);
        for(int i=0;i<4;i++) {
            float phase=Time.time*5+i*Mathf.PI*.5f;
            Vector2 offset=new Vector2(Mathf.Cos(phase)*(.65f-charge*.22f),Mathf.Sin(phase)*.45f+.1f);
            Emit(character==0?leaf:character==2?star:pixel,point+offset,-offset*2.2f,tint,.2f,
                Vector2.one*(.08f+charge*.045f),Vector2.zero,0,7,phase*Mathf.Rad2Deg,character==3?0:140);
        }
        Light(point,tint,.8f+charge*.8f,.13f);
    }
    public void RangedProjectile(Vector2 point,float direction,int character,float charge)
    {
        Color tint=CharacterTint(character);charge=Mathf.Clamp01(charge);
        float size=.9f+charge*.25f;
        Light(point,tint,1.6f,.15f);
        var frames=brushSwings[Mathf.Clamp(character,0,7)];
        if(frames==null || frames.Length==0)return;
        InkUnderpaint(frames,point,direction,.16f,1.45f*size,Vector2.right*direction*1.6f);
        int index=Emit(frames[0],point,Vector2.right*direction*1.6f,Color.white,.16f,Vector2.one*(1.45f*size),Vector2.one*(1.45f*size),0,10);
        pool[index].frames=frames;pool[index].art.flipX=direction<0;
    }

    void CinematicAccents(Vector2 point,Color tint)
    {
        for(int i=0;i<10;i++) {
            float angle=i*Mathf.PI*2/10;
            Vector2 radial=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            Emit(leaf,point+radial*.85f,radial*2,
                new Color(tint.r,tint.g,tint.b,.42f),.28f,new Vector2(.23f,.13f),Vector2.zero,0,4,angle*Mathf.Rad2Deg,180);
        }
    }
    void UpdateCinematicLayers()
    {
        if(!cam)return;
        var game=WoodlandGame.Instance;
        bool allowed=game&&game.playing&&!game.Paused;
        bool active=allowed&&cinematicScout;
        float strength=active?Mathf.Clamp01((Time.unscaledTime-cinematicStarted)/.14f):
            allowed?Mathf.Clamp01((cinematicExitUntil-Time.unscaledTime)/.18f):0;
        float height=cam.orthographicSize*2,width=height*cam.aspect;
        Vector3 center=cam.transform.position;center.z=0;
        for(int i=0;i<cinematicLayers.Length;i++) {
            var layer=cinematicLayers[i];layer.enabled=strength>0;if(!layer.enabled)continue;
            Vector3 position=center;Vector2 scale=new Vector2(width,height);
            Color color=new Color(.025f,.035f,.08f,.32f*strength);
            if(i==1||i==2) {
                float bar=height*.065f*strength;scale=new Vector2(width,bar);
                position.y+=(i==1?1:-1)*(height*.5f-bar*.5f);color=new Color(.015f,.02f,.035f,.92f);
            } else if(i==3) {
                float hit=active?Mathf.Clamp01((cinematicImpactUntil-Time.unscaledTime)/.23f):0;
                color=new Color(1,.95f,.8f,hit*.13f);
            }
            layer.transform.position=position;layer.transform.localScale=new Vector3(scale.x,scale.y,1);layer.color=color;
        }
    }
    int CharacterForPlayer(int player)
    {
        var game=WoodlandGame.Instance;
        return game&&player>=0&&player<game.players.Length&&game.players[player]
            ?game.players[player].CharacterIndex:Mathf.Clamp(player,0,3);
    }
    Color CharacterTint(int character)
    {
        switch(character) {
            case 1:return new Color(1,.38f,.12f);
            case 2:return new Color(.56f,.76f,1);
            case 3:return new Color(1,.76f,.32f);
            case 4:return new Color(.5f,1,.85f);
            case 5:return new Color(1,.85f,.2f);
            case 6:return new Color(.7f,.4f,1);
            case 7:return new Color(.18f,.84f,1);
            default:return new Color(.38f,1,.68f);
        }
    }
    public void Swipe(Vector2 point, float facing, bool punch,int player=0,int character=-1)
    {
        PlayCue(0,.38f,punch?1.08f:.95f);
        character=character<0?CharacterForPlayer(player):character;
        var color=CharacterTint(character);
        Vector2 contact=point+new Vector2(facing*.95f,punch?.2f:-.3f);
        var frames=brushSwings[Mathf.Clamp(character,0,7)];
        if(frames!=null && frames.Length>0) {
            float size=punch?1.75f:2.1f;
            InkUnderpaint(frames,contact,facing,.23f,size,Vector2.right*facing*.22f,punch?0:-20*facing);
            int index=Emit(frames[0],contact,Vector2.right*facing*.22f,Color.white,.23f,Vector2.one*size,Vector2.one*size,0,10,punch?0:-20*facing);
            pool[index].frames=frames;pool[index].art.flipX=facing<0;
        }
        Light(contact,color,.65f,.12f);
    }

    public void Impact(Vector2 point, float facing, bool punch, bool finisher=false)
    {
        PlayCue(finisher?2:1,finisher?.70f:.60f,Random.Range(.94f,1.06f));
        Light(point,Gold,1.8f);KickCamera(new Vector2(facing,.25f),finisher?.10f:punch?.045f:.03f);FreezeImpact(finisher?.045f:.025f);
        Emit(slash,point,Vector2.zero,Color.white,.075f,new Vector2(.4f,.65f),new Vector2(.08f,.2f),0,12,45);
        Burst(point,new Vector2(facing,.45f),finisher?20:8,Gold,punch?4.5f:3.4f);
        Pulse(point,punch?Gold:new Color(.72f,1,.58f),punch?1.5f:1.1f,.22f);
        Emit(star,point,Vector2.zero,new Color(1,.96f,.74f),.13f,Vector2.one*.6f,Vector2.one*.06f,0,11,45,180);
        for(int i=0;i<6;i++) {
            float angle=i*60+Random.Range(-10,10);Vector2 direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
            Emit(star,point,direction*4.5f,Gold,.2f,Vector2.one*.18f,Vector2.zero,0,10,angle,100);
        }
        shake=Mathf.Max(shake,punch?punchShake:pushShake);
    }
    public void TacticalImpact(Vector2 point,float facing,bool punish)
    {
        Color tint=punish?new Color(1,.78f,.35f):new Color(.8f,.95f,1);
        Pulse(point,tint,punish?1.25f:.9f,.19f);
        Emit(star,point,Vector2.zero,tint,.12f,new Vector2(.45f,.7f),Vector2.zero,0,12,45);
        Burst(point,Vector2.right*facing,10,tint,2.5f);
        FreezeImpact(.045f);
    }
    static Sprite[] LoadComboFrames(string character,string action)
    {
        var frames=new Sprite[6];
        for(int i=0;i<frames.Length;i++) {
            frames[i]=Resources.Load<Sprite>("Art/CombatV9/"+character+"/"+action+"/"+(i+1).ToString("00"));
            if(!frames[i])return new Sprite[0];
        }
        return frames;
    }
    void InkUnderpaint(Sprite[] frames,Vector2 point,float facing,float duration,float size,Vector2 velocity,float angle=0)
    {
        int ink=Emit(frames[0],point,velocity,new Color(.03f,.02f,.05f,.85f),duration,Vector2.one*(size*1.14f),Vector2.one*size,0,9,angle);
        pool[ink].frames=frames;pool[ink].art.flipX=facing<0;
    }
    public void ComboImpact(Vector2 point,float facing,int character,bool finisher)
    {
        if(character<0 || character>=8)return;
        var frames=finisher?comboFinishers[character]:comboHits[character];
        if(frames==null || frames.Length==0)return;
        float size=finisher?3.0f:2.0f;
        InkUnderpaint(frames,point,facing,finisher?.40f:.27f,size,Vector2.right*facing*.22f);
        int index=Emit(frames[0],point,Vector2.right*facing*.22f,Color.white,finisher?.40f:.27f,Vector2.one*size,Vector2.one*size,0,11);
        pool[index].frames=frames;pool[index].art.flipX=facing<0;
        ComboAnimationsPlayed++;
    }
    public void ElementHit(Vector2 point, float facing, Color tint, int combo, int player,int character=-1)
    {
        character=character<0?CharacterForPlayer(player):character;
        ComboImpact(point,facing,character,combo==3);
        Pulse(point, tint, .9f + combo * .35f, .24f);
        for (int i=0;i<Count(8+combo*4);i++) {
            Vector2 velocity = new Vector2(facing*Random.Range(1f,4f),Random.Range(-2f,3f));
            Emit(character==0?leaf:character==2?star:pixel, point, velocity, tint, .3f+combo*.07f,
                Vector2.one*Random.Range(.09f,.18f), Vector2.zero, character==3?5:0, 10, Random.Range(0,360), character==0?240:-160);
        }
        Light(point,tint,1.2f+combo*.25f);
        if (combo == 3) {KickCamera(new Vector2(facing,.2f),.13f);FreezeImpact(.055f); Pulse(point, Color.white, 2.1f,.18f); shake=Mathf.Max(shake,.11f); }
    }
    public void Ward(Vector2 point, float facing, Color tint, float strength,int character=0)
    {
        var frames=elementalShields[Mathf.Clamp(character,0,7)];
        if(frames!=null && frames.Length>0) {
            var frame=frames[Mathf.FloorToInt(Time.time*12)%frames.Length];
            int shield=Emit(frame,point+new Vector2(facing*.72f,.05f),Vector2.zero,new Color(1,1,1,.72f+.28f*strength),.09f,Vector2.one*2.4f,Vector2.one*2.4f,0,10);
            pool[shield].art.flipX=facing<0;return;
        }
        Light(point+Vector2.right*facing*.45f,tint,.9f,.12f);
        // A forward crescent leaves the back exposed and makes guard direction visible.
        for(int i=0;i<3;i++) {
            float angle=(Time.time*1.6f+i*2.1f);Vector2 offset=new Vector2(facing*(.45f+Mathf.Abs(Mathf.Cos(angle))*.18f),Mathf.Sin(angle)*.5f);
            Emit(star,point+offset,Vector2.zero,new Color(tint.r,tint.g,tint.b,.55f),.15f,Vector2.one*.055f,Vector2.zero,0,7,45);
        }
    }
    public void AirJump(Vector2 feet, Color tint, int player)
    {
        int character=CharacterForPlayer(player);
        PlayCue(4,.38f,player==0?1.08f:.95f);Light(feet,tint,1.8f,.32f);
        Pulse(feet, tint, 1.7f, .32f, true);
        Pulse(feet + Vector2.up * .15f, Color.white, .9f, .2f, true);
        for (int i = 0; i < Count(16); i++) {
            float angle = i * Mathf.PI * 2 / Count(16);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .35f);
            Emit(character==0?leaf:character==2?star:pixel, feet, direction * 2.4f, tint, .4f,
                Vector2.one * .12f, Vector2.zero, 2, 8, angle * Mathf.Rad2Deg, character==0?220:-180);
        }
    }
    public void Dash(Vector2 point, float facing, Color tint)
    {
        PlayCue(5,.40f,Random.Range(.97f,1.05f));Light(point,tint,1.4f,.22f);
        Burst(point + Vector2.down*.55f, Vector2.left * facing, 6, tint, 1.4f);
        // Sprite afterimages are emitted by the controller; the feet keep only a dust kick.
    }
    public void GuardImpact(Vector2 point, float facing, Color tint, bool parry, bool broken,int character=-1)
    {
        PlayCue(broken?9:parry?8:7,broken?.68f:parry?.55f:.48f,Random.Range(.97f,1.04f));
        Light(point,tint,broken?2.6f:1.7f);KickCamera(Vector2.right*facing,parry?.08f:.04f);if(parry)FreezeImpact(.045f);
        var color = parry ? Color.white : broken ? new Color(1,.25f,.2f) : tint;
        character=character<0?0:Mathf.Clamp(character,0,7);
        var frames=brushGuards[character];
        if(frames!=null && frames.Length>0) {
            float size=broken?2.35f:parry?1.9f:1.5f;
            int index=Emit(frames[0],point,Vector2.zero,broken?color:Color.white,.28f,Vector2.one*size,Vector2.one*size,0,11);
            pool[index].frames=frames;pool[index].art.flipX=facing<0;
        }
        Burst(point,Vector2.right*facing,broken?10:4,color,2);
        shake = Mathf.Max(shake, parry ? .06f : .04f);
    }
    public void Smash(Vector2 point, float facing, Color tint, float charge,int character=0)
    {
        PlayCue(5,.4f,.8f);Light(point+Vector2.right*facing*.6f,tint,2.5f+charge,.32f);
        KickCamera(new Vector2(facing,.2f),.07f+charge*.06f);
        RangedProjectile(point+Vector2.right*facing*.75f,facing,character,charge);
        Pulse(point + Vector2.right*facing*.8f, tint, 1.6f+charge, .28f);
        Burst(point, new Vector2(facing,.25f), 24, tint, 3+charge*2);
        shake=Mathf.Max(shake,.05f+charge*.07f);
    }
    public void HeavyImpact(Vector2 point,float facing,Color tint,float power)
    {
        PlayCue(2,.76f,.90f);Light(point,tint,3,.34f);Pulse(point,Color.white,1.4f,.16f);
        Burst(point,new Vector2(facing,.4f),26,tint,4.5f);
        Emit(star,point,Vector2.zero,Color.white,.1f,Vector2.one*.65f,Vector2.zero,0,12,45);
        KickCamera(new Vector2(facing,.25f),.10f+power*.05f);FreezeImpact(.055f);
    }
    public void RingOut(Vector2 opening,Color tint)
    {
        PlayCue(10,.65f,.7f);KickCamera(Vector2.down,.09f);
        Pulse(opening,new Color(.55f,.8f,.7f),2.2f,.5f,true);
        for(int i=0;i<Count(22);i++) {
            Vector2 offset=new Vector2(Random.Range(-1.1f,1.1f),Random.Range(.05f,.65f));
            Emit(i%3==0?leaf:pixel,opening+offset,new Vector2(-offset.x*2,-1.4f),Color.Lerp(tint,new Color(.35f,.45f,.35f),.6f),Random.Range(.35f,.65f),Vector2.one*Random.Range(.08f,.16f),Vector2.zero,2,7,Random.Range(0,180),90);
        }
        Emit(glow,opening,Vector2.zero,new Color(.15f,.4f,.3f,.65f),.55f,new Vector2(2.4f,.55f),Vector2.zero,0,2);
    }
    public void Knockout(Vector2 point, Color tint)
    {
        PlayCue(10,.70f,.95f);Light(point,tint,4,.5f);KickCamera(Vector2.up,.16f);FreezeImpact(.065f);
        Pulse(point,Gold,2.7f,.45f);Pulse(point,tint,1.9f,.32f);
        Burst(point,Vector2.up,35,tint,4.2f);
        for(int i=0;i<Count(12);i++) {
            float angle=i*Mathf.PI*2/Count(12);Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            Emit(star,point,direction*Random.Range(2,4),Gold,Random.Range(.45f,.85f),Vector2.one*.18f,Vector2.zero,2,10,45,150);
        }
        shake=Mathf.Max(shake,punchShake*1.5f);
    }
    public void Celebrate(Vector2 point, Color tint)
    {
        PlayCue(11,.42f,1);Light(point,tint,3,.7f);
        Burst(point,Vector2.up,28,tint,3.4f);
        for(int i=0;i<Count(10);i++) Emit(star,point, new Vector2(Random.Range(-3f,3f),Random.Range(2f,5f)),Gold,.8f,Vector2.one*.12f,Vector2.zero,4,10,45,120);
    }
    void LateUpdate()
    {
        UpdateCinematicLayers();
        float dt=Time.deltaTime;if(dt<=0)return;
        if(ambientFireflies && Time.time>=nextAmbient) {
            nextAmbient=Time.time+.35f;
            Emit(star,new Vector2(Random.Range(-8.5f,8.5f),Random.Range(-2.5f,4.8f)),new Vector2(Random.Range(-.12f,.12f),.18f),new Color(ambientTint.r,ambientTint.g,ambientTint.b,.45f),Random.Range(2,4),Vector2.one*.045f,Vector2.one*.015f,0,-8,45,20);
        }
        if(ambientFireflies && Time.time>=nextWeather) {
            nextWeather=Time.time+.22f;
            int arena=WoodlandGame.Instance?WoodlandGame.Instance.ArenaIndex:0;
            Vector2 position=new Vector2(Random.Range(-7.5f,7.5f),Random.Range(1.5f,5.5f));
            Color weather=arena==1?new Color(.5f,.85f,1,.22f):arena==2?new Color(1,.44f,.18f,.3f):new Color(.8f,1,.55f,.25f);
            Emit(arena==1?pixel:leaf,position,new Vector2(.12f,arena==2?.2f:-.18f),weather,3.2f,Vector2.one*.05f,Vector2.zero,0,-5,45,arena==1?0:35);
        }
        for(int i=0;i<Capacity;i++) {
            if(pool[i].life<=0)continue;
            pool[i].life-=dt;var sr=pool[i].art;
            if(pool[i].life<=0){sr.enabled=false;continue;}
            float fade=pool[i].life/pool[i].total;
            var frames=pool[i].frames;
            if(frames!=null && frames.Length>0) {
                int frame=Mathf.Clamp(Mathf.FloorToInt((1-fade)*frames.Length),0,frames.Length-1);
                if(sr.sprite!=frames[frame]) { sr.sprite=frames[frame];textureBinding.Clear();textureBinding.SetTexture("_MainTex",sr.sprite.texture);sr.SetPropertyBlock(textureBinding); }
            }
            pool[i].velocity=(pool[i].velocity+Vector2.down*pool[i].gravity*dt)*Mathf.Exp(-pool[i].drag*dt);
            sr.transform.position+=(Vector3)(pool[i].velocity*dt);sr.transform.Rotate(0,0,pool[i].spin*dt);
            float eased=pool[i].endSize.sqrMagnitude>pool[i].startSize.sqrMagnitude?1-Mathf.Pow(fade,3):Mathf.Pow(1-fade,2);
            Vector2 size=Vector2.Lerp(pool[i].startSize,pool[i].endSize,eased);sr.transform.localScale=new Vector3(size.x,size.y,1);
            var color=pool[i].color;color.a*=Mathf.Min(1,fade*1.7f);sr.color=color;
        }
        if(cam){
            float smoothDt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            cameraVelocity+=(-cameraOffset*145-cameraVelocity*20)*smoothDt;
            cameraOffset+=cameraVelocity*smoothDt;cameraOffset=Vector2.ClampMagnitude(cameraOffset,.18f);
            var game=WoodlandGame.Instance;
            bool inMatch=game && game.playing && game.Screen==WoodlandGame.MenuScreen.Match;
            bool motionAllowed=inMatch && !game.Paused;
            if (!motionAllowed) { cameraOffset=cameraVelocity=Vector2.zero;shake=0; }
            bool cinematic=motionAllowed && cameraShake && cinematicScout;
            Vector3 focus=cameraHome;float matchZoom=cameraZoom;
            if(inMatch && game.players.Length==2 && game.players[0] && game.players[1] && game.players[0].Body && game.players[1].Body) {
                var first=game.players[0];var second=game.players[1];
                if(game.Paused) {
                    focus=cam.transform.position;matchZoom=cam.orthographicSize;
                } else {
                    float standingY=game.plank.position.y+.15f+1.1875f;
                    float firstY=first.RingOutFalling || first.KnockedOut?standingY:first.Body.position.y;
                    float secondY=second.RingOutFalling || second.KnockedOut?standingY:second.Body.position.y;
                    float low=Mathf.Min(firstY,secondY)-1.1875f;
                    // Marin's larger artwork shares the same feet and collision body.
                    float high=Mathf.Max(firstY+1.1875f+(first.CharacterIndex==7?.25f:0),secondY+1.1875f+(second.CharacterIndex==7?.25f:0));
                    float span=Mathf.Abs(first.Body.position.x-second.Body.position.x);
                    bool edgeApproach=first.RingOutFalling || second.RingOutFalling || Mathf.Max(Mathf.Abs(first.Body.position.x),Mathf.Abs(second.Body.position.x))>game.SafeFloorHalfWidth-.9f;
                    float sideMargin=edgeApproach?5.5f:2.8f;
                    matchZoom=Mathf.Clamp(Mathf.Max(3,(span+sideMargin)/(2*cam.aspect),high-low+.25f),3,cameraZoom);
                    float imageHalfWidth=game.backgroundArt.bounds.extents.x;
                    float limit=Mathf.Max(0,imageHalfWidth-matchZoom*cam.aspect-.05f);
                    float desiredY=(low+high)*.5f+matchZoom*.16f;
                    var scenery=game.backgroundArt.bounds;
                    float minY=scenery.min.y+matchZoom,maxY=scenery.max.y-matchZoom;
                    float framedY=minY<=maxY?Mathf.Clamp(desiredY,minY,maxY):scenery.center.y;
                    focus=new Vector3(Mathf.Clamp((first.Body.position.x+second.Body.position.x)*.5f,-limit,limit),framedY,cameraHome.z);
                }
            }
            if(cinematic)focus=Vector3.Lerp(focus,new Vector3(cinematicFocus.x,cinematicFocus.y,cameraHome.z),.28f);
            Vector3 target=focus+(cameraShake&&motionAllowed?(Vector3)cameraOffset:Vector3.zero);
            float zoom=matchZoom*(cinematic?(Time.unscaledTime<cinematicImpactUntil?.91f:.95f):1);
            bool edgeFall=inMatch && (game.players[0].RingOutFalling || game.players[1].RingOutFalling);
            float cameraEase=1-Mathf.Exp(-(edgeFall?18:10)*smoothDt);
            cam.transform.position=inMatch?Vector3.Lerp(cam.transform.position,target,cameraEase):cameraHome;
            cam.orthographicSize=inMatch?Mathf.Lerp(cam.orthographicSize,zoom-(cameraShake?shake*.45f:0),cameraEase):cameraZoom;
            shake=Mathf.MoveTowards(shake,0,smoothDt*.7f);
        }
    }
    void OnDestroy()
    {
        AudioListener.pause=false;if(hitPause!=null)Time.timeScale=1;
        if(cam){cam.transform.position=cameraHome;cam.orthographicSize=cameraZoom;}
        if(Instance==this)Instance=null;
        foreach(var sprite in new[]{pixel,leaf,star,ring,slash,glow,panel})if(sprite){Destroy(sprite.texture);Destroy(sprite);}
        if(material)Destroy(material);if(glowMaterial)Destroy(glowMaterial);
    }
}
