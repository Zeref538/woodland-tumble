using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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
    [Range(.25f, 2)] public float effectIntensity = 1;
    public bool ambientFireflies = true;
    public Color ambientTint = new Color(1,.86f,.42f);
    public bool cameraShake = true;
    public float footstepInterval = .16f;
    [Header("Elemental sprite animation")]
    public Sprite[] fernSlash=new Sprite[8],emberSlash=new Sprite[8];
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
    public void SetArenaCamera(float size,Vector3 position)
    {
        if(!cam)cam=Camera.main;
        if(!cam)return;
        cameraHome=position;cameraZoom=size;cameraOffset=cameraVelocity=Vector2.zero;
        cam.transform.position=position;cam.orthographicSize=size;
    }
    const int Capacity = 320;
    readonly Mote[] pool = new Mote[Capacity];
    int cursor;
    Sprite pixel, leaf, star, ring, slash, streak, glow;
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
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-silent-test")>=0)AudioListener.volume=0;
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Woodland.SfxVolume", masterVolume));
        Muted = PlayerPrefs.GetInt("Woodland.SfxMuted",0) == 1;
        for(int i=0;i<voices.Length;i++) { voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;voices[i].priority=100; }

        textureBinding = new MaterialPropertyBlock();
        cam = Camera.main;
        if (cam) { cameraHome = cam.transform.position; cameraZoom = cam.orthographicSize; }
        material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        pixel = Shape("Dust", 8, 8, (x,y) => Mathf.Abs(x-3)+Mathf.Abs(y-3)<5);
        leaf = Shape("Leaf", 12, 12, (x,y) => Mathf.Abs(x-y)<4 && x+y>3 && x+y<18);
        star = Shape("Spark", 16, 16, (x,y) => Mathf.Abs(x-7)<2 || Mathf.Abs(y-7)<2 || Mathf.Abs(x-7)+Mathf.Abs(y-7)<6);
        ring = Shape("Pixel shockwave", 32, 32, (x,y) => { float d = Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f));return d>12 && d<15; });
        slash = Shape("Attack arc", 32, 32, (x,y) => { float d = Vector2.Distance(new Vector2(x,y),new Vector2(7,15));return x>11 && d>15 && d<20; });
        streak = Shape("Speed line", 24, 4, (x,y) => x>2 && x<22 && (y==1 || y==2));
        glow = SoftGlow();
        glowMaterial = new Material(material); glowMaterial.SetColor("_Color", new Color(2.4f,2.4f,2.4f,1));
        material.mainTexture = pixel.texture;
        for (int i=0; i<Capacity; i++) {
            var go = new GameObject("Pooled woodland VFX");go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();sr.sharedMaterial = material;sr.enabled = false;pool[i].art = sr;
        }
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
    void AnimatedSlash(Vector2 point,float facing,bool fern,float size,float duration)
    {
        var frames=fern?fernSlash:emberSlash;if(frames==null||frames.Length==0||!frames[0])return;
        int index=Emit(frames[0],point,Vector2.right*facing*.6f,Color.white,duration,Vector2.one*size,Vector2.one*size*1.12f,0,10);
        pool[index].frames=frames;pool[index].art.flipX=facing<0;
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
        for(int i=0;i<3;i++) Emit(streak,feet+new Vector2((i-1)*.15f,.2f),Vector2.down*.6f,new Color(1,.92f,.65f,.6f),.2f,new Vector2(.32f,.9f),Vector2.zero,0,7,90);
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
    public void Swipe(Vector2 point, float facing, bool punch,int player=0)
    {
        PlayCue(0,.38f,punch?1.08f:.95f);
        var color=punch?Gold:new Color(.68f,1,.55f);
        AnimatedSlash(point+Vector2.right*facing*.65f,facing,player==0,punch?1.8f:1.6f,.26f);
        for(int i=0;i<Count(4);i++) Emit(streak,point+new Vector2(-facing*.25f,Random.Range(-.3f,.3f)),Vector2.right*facing*3,color,.16f,new Vector2(.7f,.7f),new Vector2(.15f,.3f));
    }
    public void Impact(Vector2 point, float facing, bool punch, bool finisher=false)
    {
        PlayCue(finisher?2:1,finisher?.70f:.60f,Random.Range(.94f,1.06f));
        Light(point,Gold,1.8f);KickCamera(new Vector2(facing,.25f),punch?.10f:.06f);FreezeImpact(.035f);
        Emit(slash,point,Vector2.zero,Color.white,.075f,new Vector2(.4f,.65f),new Vector2(.08f,.2f),0,12,45);
        Burst(point,new Vector2(facing,.45f),punch?25:18,Gold,punch?4.5f:3.4f);
        Pulse(point,punch?Gold:new Color(.72f,1,.58f),punch?1.5f:1.1f,.22f);
        Emit(star,point,Vector2.zero,new Color(1,.96f,.74f),.13f,Vector2.one*.6f,Vector2.one*.06f,0,11,45,180);
        for(int i=0;i<6;i++) {
            float angle=i*60+Random.Range(-10,10);Vector2 direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
            Emit(streak,point,direction*4.5f,Gold,.2f,new Vector2(.65f,.65f),Vector2.zero,0,10,angle);
        }
        shake=Mathf.Max(shake,punch?punchShake:pushShake);
    }
    public void ElementHit(Vector2 point, float facing, Color tint, int combo, int player)
    {
        Pulse(point, tint, .9f + combo * .35f, .24f);
        for (int i=0;i<Count(8+combo*4);i++) {
            Vector2 velocity = new Vector2(facing*Random.Range(1f,4f),Random.Range(-2f,3f));
            Emit(player == 0 ? leaf : star, point, velocity, tint, .3f+combo*.07f,
                Vector2.one*Random.Range(.09f,.18f), Vector2.zero, player==0?3:0, 10, Random.Range(0,360), player==0?240:-160);
        }
        Light(point,tint,1.2f+combo*.25f);
        if (combo == 3) {KickCamera(new Vector2(facing,.2f),.13f);FreezeImpact(.055f); Pulse(point, Color.white, 2.1f,.18f); shake=Mathf.Max(shake,.11f); }
    }
    public void Ward(Vector2 point, float facing, Color tint, float strength)
    {
        Light(point+Vector2.right*facing*.45f,tint,.9f,.12f);
        // A forward crescent leaves the back exposed and makes guard direction visible.
        for(int i=0;i<3;i++) {
            float angle=(Time.time*1.6f+i*2.1f);Vector2 offset=new Vector2(facing*(.45f+Mathf.Abs(Mathf.Cos(angle))*.18f),Mathf.Sin(angle)*.5f);
            Emit(star,point+offset,Vector2.zero,new Color(tint.r,tint.g,tint.b,.55f),.15f,Vector2.one*.055f,Vector2.zero,0,7,45);
        }
    }
    public void AirJump(Vector2 feet, Color tint, int player)
    {
        PlayCue(4,.38f,player==0?1.08f:.95f);Light(feet,tint,1.8f,.32f);
        Pulse(feet, tint, 1.7f, .32f, true);
        Pulse(feet + Vector2.up * .15f, Color.white, .9f, .2f, true);
        for (int i = 0; i < Count(16); i++) {
            float angle = i * Mathf.PI * 2 / Count(16);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .35f);
            Emit(player == 0 ? leaf : star, feet, direction * 2.4f, tint, .4f,
                Vector2.one * .12f, Vector2.zero, 2, 8, angle * Mathf.Rad2Deg, player == 0 ? 220 : -180);
        }
    }
    public void Dash(Vector2 point, float facing, Color tint)
    {
        PlayCue(5,.40f,Random.Range(.97f,1.05f));Light(point,tint,1.4f,.22f);
        Burst(point, Vector2.left * facing, 14, tint, 2.5f);
        for (int i=0;i<7;i++) Emit(streak, point + new Vector2(-facing * .3f, (i-3)*.14f),
            Vector2.left*facing*3, tint, .22f, new Vector2(1.4f,.6f), Vector2.zero, 0, 7);
    }
    public void GuardImpact(Vector2 point, float facing, Color tint, bool parry, bool broken)
    {
        PlayCue(broken?9:parry?8:7,broken?.68f:parry?.55f:.48f,Random.Range(.97f,1.04f));
        Light(point,tint,broken?2.6f:1.7f);KickCamera(Vector2.right*facing,parry?.08f:.04f);if(parry)FreezeImpact(.045f);
        var color = parry ? Color.white : broken ? new Color(1,.25f,.2f) : tint;
        Pulse(point, color, broken ? 2.3f : 1.5f, .3f);
        Burst(point, Vector2.right * facing, broken ? 26 : 14, color, 3.5f);
        for(int i=0;i<8;i++) {
            float a=i*45; Vector2 d=new Vector2(Mathf.Cos(a*Mathf.Deg2Rad),Mathf.Sin(a*Mathf.Deg2Rad));
            Emit(parry ? star : streak, point, d*3, color, .25f, Vector2.one*.22f, Vector2.zero, 0, 11, a, 180);
        }
        shake = Mathf.Max(shake, parry ? .06f : .04f);
    }
    public void Smash(Vector2 point, float facing, Color tint, float charge)
    {
        PlayCue(5,.4f,.8f);Light(point+Vector2.right*facing*.6f,tint,2.5f+charge,.32f);
        KickCamera(new Vector2(facing,.2f),.07f+charge*.06f);
        float size = 1 + charge;
        AnimatedSlash(point+Vector2.right*facing*.75f,facing,tint.g>.8f,2.1f+charge*.6f,.32f);
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
            cam.transform.position=cameraHome+(cameraShake?(Vector3)cameraOffset:Vector3.zero);
            cam.orthographicSize=Mathf.Lerp(cam.orthographicSize,cameraZoom-(cameraShake?shake*.45f:0),1-Mathf.Exp(-16*smoothDt));
            shake=Mathf.MoveTowards(shake,0,smoothDt*.7f);
        }
    }
    void OnDestroy()
    {
        AudioListener.pause=false;if(hitPause!=null)Time.timeScale=1;
        if(cam){cam.transform.position=cameraHome;cam.orthographicSize=cameraZoom;}
        if(Instance==this)Instance=null;
        foreach(var sprite in new[]{pixel,leaf,star,ring,slash,streak,glow})if(sprite){Destroy(sprite.texture);Destroy(sprite);}
        if(material)Destroy(material);if(glowMaterial)Destroy(glowMaterial);
    }
}
