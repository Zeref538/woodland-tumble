using UnityEngine;

// Shared pool for native pixel shapes, sprite afterimages and woodland atmosphere.
public class WoodlandEffects : MonoBehaviour
{
    struct Mote
    {
        public SpriteRenderer art;
        public Vector2 velocity, startSize, endSize;
        public float life, total, spin, gravity, drag;
        public Color color;
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
    const int Capacity = 320;
    readonly Mote[] pool = new Mote[Capacity];
    int cursor;
    Sprite pixel, leaf, star, ring, slash, streak;
    Material material;
    MaterialPropertyBlock textureBinding;
    Camera cam;
    Vector3 cameraHome;
    float shake, nextAmbient;
    public static WoodlandEffects Instance;
    public int ActiveMotes { get { int count = 0; foreach (var mote in pool) if (mote.life > 0) count++; return count; } }

    void Awake()
    {
        Instance = this;
        textureBinding = new MaterialPropertyBlock();
        cam = Camera.main;
        if (cam) cameraHome = cam.transform.position;
        material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        pixel = Shape("Dust", 8, 8, (x,y) => Mathf.Abs(x-3)+Mathf.Abs(y-3)<5);
        leaf = Shape("Leaf", 12, 12, (x,y) => Mathf.Abs(x-y)<4 && x+y>3 && x+y<18);
        star = Shape("Spark", 16, 16, (x,y) => Mathf.Abs(x-7)<2 || Mathf.Abs(y-7)<2 || Mathf.Abs(x-7)+Mathf.Abs(y-7)<6);
        ring = Shape("Pixel shockwave", 32, 32, (x,y) => { float d = Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f));return d>12 && d<15; });
        slash = Shape("Attack arc", 32, 32, (x,y) => { float d = Vector2.Distance(new Vector2(x,y),new Vector2(7,15));return x>11 && d>15 && d<20; });
        streak = Shape("Speed line", 24, 4, (x,y) => x>2 && x<22 && (y==1 || y==2));
        material.mainTexture = pixel.texture;
        for (int i=0; i<Capacity; i++) {
            var go = new GameObject("Pooled woodland VFX");go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();sr.sharedMaterial = material;sr.enabled = false;pool[i].art = sr;
        }
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
        sr.enabled=true;sr.sharedMaterial=material;sr.sprite=sprite;sr.flipX=false;sr.sortingOrder=order;
        textureBinding.Clear();textureBinding.SetTexture("_MainTex",sprite.texture);sr.SetPropertyBlock(textureBinding);
        sr.transform.SetPositionAndRotation(point,Quaternion.Euler(0,0,angle));sr.transform.localScale=new Vector3(size.x,size.y,1);sr.color=color;
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
        int i=Emit(source.sprite,source.transform.position,Vector2.zero,new Color(tint.r,tint.g,tint.b,.25f),trailDuration,scale,scale,0,4,source.transform.eulerAngles.z);
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
        Emit(ring,point,Vector2.zero,new Color(tint.r,tint.g,tint.b,.7f),duration,new Vector2(.12f,flat?.05f:.12f),new Vector2(size,flat?size*.28f:size));
    }
    public void Footstep(Vector2 feet, float direction)
    {
        Burst(feet,new Vector2(-direction,.25f),3,new Color(.57f,.68f,.32f),.65f);
    }
    public void Jump(Vector2 feet, Color tint)
    {
        Pulse(feet,tint,.9f,.25f,true);
        Burst(feet,new Vector2(-1,.2f),6,tint,1.7f);Burst(feet,new Vector2(1,.2f),6,tint,1.7f);
        for(int i=0;i<3;i++) Emit(streak,feet+new Vector2((i-1)*.15f,.2f),Vector2.down*.6f,new Color(1,.92f,.65f,.6f),.2f,new Vector2(.32f,.9f),Vector2.zero,0,7,90);
    }
    public void Land(Vector2 feet, float speed)
    {
        float power=Mathf.Clamp(speed/8,.3f,1.2f);
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
    public void Swipe(Vector2 point, float facing, bool punch)
    {
        var color=punch?Gold:new Color(.68f,1,.55f);
        Emit(slash,point+Vector2.right*facing*.4f,Vector2.right*facing*1.8f,color,.16f,new Vector2(.55f,.7f),new Vector2(1.1f,1.15f),0,8,facing<0?180:0,-facing*150);
        for(int i=0;i<Count(4);i++) Emit(streak,point+new Vector2(-facing*.25f,Random.Range(-.3f,.3f)),Vector2.right*facing*3,color,.16f,new Vector2(.7f,.7f),new Vector2(.15f,.3f));
    }
    public void Impact(Vector2 point, float facing, bool punch)
    {
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
        if (combo == 3) { Pulse(point, Color.white, 2.1f,.18f); shake=Mathf.Max(shake,.11f); }
    }
    public void Ward(Vector2 point, float facing, Color tint, float strength)
    {
        // A forward crescent leaves the back exposed and makes guard direction visible.
        Emit(slash, point + Vector2.right * facing * .4f, Vector2.zero,
            new Color(tint.r, tint.g, tint.b, .45f + strength * .35f), .1f,
            new Vector2(.85f, 1.5f), new Vector2(.9f, 1.5f), 0, 7, facing < 0 ? 180 : 0);
    }
    public void AirJump(Vector2 feet, Color tint, int player)
    {
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
        Burst(point, Vector2.left * facing, 14, tint, 2.5f);
        for (int i=0;i<7;i++) Emit(streak, point + new Vector2(-facing * .3f, (i-3)*.14f),
            Vector2.left*facing*3, tint, .22f, new Vector2(1.4f,.6f), Vector2.zero, 0, 7);
    }
    public void GuardImpact(Vector2 point, float facing, Color tint, bool parry, bool broken)
    {
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
        float size = 1 + charge;
        Emit(slash, point + Vector2.right*facing*.6f, Vector2.right*facing*2, tint, .24f,
            new Vector2(.65f, 1.3f)*size, new Vector2(1.5f,1.6f)*size, 0, 10, facing<0?180:0, -facing*140);
        Pulse(point + Vector2.right*facing*.8f, tint, 1.6f+charge, .28f);
        Burst(point, new Vector2(facing,.25f), 24, tint, 3+charge*2);
        shake=Mathf.Max(shake,.05f+charge*.07f);
    }
    public void Knockout(Vector2 point, Color tint)
    {
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
        for(int i=0;i<Capacity;i++) {
            if(pool[i].life<=0)continue;
            pool[i].life-=dt;var sr=pool[i].art;
            if(pool[i].life<=0){sr.enabled=false;continue;}
            float fade=pool[i].life/pool[i].total;
            pool[i].velocity=(pool[i].velocity+Vector2.down*pool[i].gravity*dt)*Mathf.Exp(-pool[i].drag*dt);
            sr.transform.position+=(Vector3)(pool[i].velocity*dt);sr.transform.Rotate(0,0,pool[i].spin*dt);
            Vector2 size=Vector2.Lerp(pool[i].endSize,pool[i].startSize,fade);sr.transform.localScale=new Vector3(size.x,size.y,1);
            var color=pool[i].color;color.a*=Mathf.Min(1,fade*1.7f);sr.color=color;
        }
        if(cam){cam.transform.position=cameraHome+(cameraShake?(Vector3)(Random.insideUnitCircle*shake):Vector3.zero);shake=Mathf.MoveTowards(shake,0,dt*.55f);}
    }
    void OnDestroy()
    {
        if(cam)cam.transform.position=cameraHome;
        if(Instance==this)Instance=null;
        foreach(var sprite in new[]{pixel,leaf,star,ring,slash,streak})if(sprite){Destroy(sprite.texture);Destroy(sprite);}
        if(material)Destroy(material);
    }
}
