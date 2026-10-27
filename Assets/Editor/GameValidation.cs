using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class GameValidation
{
    static Keyboard testKeyboard;
    static Key heldKey;
    static Gamepad[] testPads = new Gamepad[2];
    static GamepadState[] padStates = new GamepadState[2];
    static Gamepad[] disabledPads = new Gamepad[0];
    static int cinematicStarts,cinematicStops;
    static InputSettings originalInputSettings, validationInputSettings;
    const string Active = "Woodland.ValidateV7";
    static GameValidation() { EditorApplication.update += Tick; }
    [Serializable] public class Report { public bool success; public string status; public string[] checks; }
    public static void InspectHUD()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/WoodlandTumble.unity");Canvas.ForceUpdateCanvases();
        var hud=UnityEngine.Object.FindAnyObjectByType<WoodlandHUD>();
        foreach(var text in hud.healthText)Debug.Log("HUD_METRICS: "+text.name+" text="+text.text+" preferred="+text.preferredWidth+"x"+text.preferredHeight+" rect="+text.rectTransform.rect.size+" font="+text.font.name+" size="+text.fontSize);
    }
    [MenuItem("Woodland/Validate Editable Game")]
    public static void RunInEditor() {EditorApplication.ExecuteMenuItem("Window/General/Game");SessionState.SetBool("Woodland.Duel",true);Begin(false);}
    public static void Run() => RunDuelSaved();
    public static void ValidateSpritePlayback()
    {
        SessionState.SetString("Woodland.Checks","");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/WoodlandTumble.unity");
        var game=UnityEngine.Object.FindAnyObjectByType<WoodlandGame>();var renderer=game.players[0].art;var original=renderer.sprite;
        try {
            string[] states={"Idle","Walk","Jump","Hurt","Push","Punch","Kick","Knockout","Block","DoubleJump","Special","Dash","Backstep","Victory","Ultimate","Downed","GetUp"};
            foreach(int character in Enumerable.Range(0,game.CharacterNames.Length))foreach(string state in states){
                string name=game.CharacterNames[character];
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/AnimationsRework/"+name+"/"+(state=="Special"?"Smash":state)+".anim");
                var frames=state=="Idle"?new[]{AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CharactersRework/"+name+"/Idle/01.png")}:game.CharacterAnimationSets[character].Get(state);
                if(!clip || frames.Length!=(state=="Idle"?1:10))throw new Exception("Incomplete native clip: "+name+"/"+state);
                for(int frame=0;frame<frames.Length;frame++){
                    clip.SampleAnimation(renderer.gameObject,(frame+.25f)/clip.frameRate);
                    Check(renderer.sprite==frames[frame],"Native sprite playback: "+name+" / "+state+" / "+(frame+1));
                }
            }
            var report=new Report{success=true,status="SPRITE_PLAYBACK_PASS: every active character frame matches its native clip",checks=SessionState.GetString("Woodland.Checks","").Split('\n').Where(s=>s.Length>0).ToArray()};
            File.WriteAllText("Documentation/SPRITE-PLAYBACK-v17.json",JsonUtility.ToJson(report,true));Debug.Log(report.status+" / "+report.checks.Length);
        } catch(Exception error){
            File.WriteAllText("Documentation/SPRITE-PLAYBACK-v17.json",JsonUtility.ToJson(new Report{success=false,status=error.ToString(),checks=new string[0]},true));throw;
        } finally {renderer.sprite=original;}
    }
    public static void RunDuelFresh(){GameSetup.PrepareHUD();RunDuelSaved();}
    public static void RunDuelSaved(){AudioListener.volume=0;SessionState.SetBool("Woodland.Duel",true);Begin(true);}
    public static void RunCounterplay(){AudioListener.volume=0;SessionState.SetBool("Woodland.Counterplay",true);Begin(true);}
    public static void RunContactsSaved(){AudioListener.volume=0;SessionState.SetBool("Woodland.Contacts",true);Begin(true);}
    public static void RunContactsFresh(){GameSetup.PrepareHUD();RunContactsSaved();}
    public static void RunMidtermCombat(){AudioListener.volume=0;GameSetup.PrepareHUD();SessionState.SetBool("Woodland.MidtermCombat",true);Begin(true);}
    public static void RunSaved() => RunDuelSaved();
    public static void RunHUD()
    {
        AudioListener.volume=0;
        SessionState.SetBool("Woodland.HudOnly",true);
        GameSetup.PrepareHUD();Begin(true);
    }
    public static void RunV81(){AudioListener.volume=0;GameSetup.Prepare();SessionState.SetBool("Woodland.V81",true);SessionState.SetBool("Woodland.V8",true);Begin(true);}
    public static void RunV8Saved(){AudioListener.volume=0;GameSetup.PrepareHUD();SessionState.SetBool("Woodland.V8",true);Begin(true);}
    public static void RunV8(){AudioListener.volume=0;GameSetup.Prepare();SessionState.SetBool("Woodland.V8",true);Begin(true);}
    public static void RunMelee()
    {
        AudioListener.volume=0;
        SessionState.SetBool("Woodland.MeleeOnly",true);
        GameSetup.ImportComboArtwork();Begin(true);
    }
    public static void ValidatePlatformSizing()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Platform sizing validation");
        var texture=new Texture2D(64,32);
        var sprites=new Sprite[4];
        int checks=0;
        try {
            var game=root.AddComponent<WoodlandGame>();
            var beam=new GameObject("Test beam",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(BoxCollider2D));
            beam.transform.SetParent(root.transform);
            game.plank=beam.GetComponent<Rigidbody2D>();game.plankArt=beam.GetComponent<SpriteRenderer>();
            beam.transform.localScale=new Vector3(2,3,1);
            var box=beam.GetComponent<BoxCollider2D>();box.size=new Vector2(4.95f,.22f/3);box.offset=new Vector2(0,.04f/3);box.edgeRadius=.02f;
            var originalSize=box.size;var originalOffset=box.offset;
            var ledge=new GameObject("Test ledge",typeof(SpriteRenderer),typeof(Rigidbody2D),typeof(BoxCollider2D));
            ledge.transform.SetParent(root.transform);game.arenaPlatforms=new[]{ledge.GetComponent<Rigidbody2D>()};
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var apply=typeof(WoodlandGame).GetMethod("ApplyPlatformArt",flags);
            var mode=typeof(WoodlandGame).GetProperty("BalanceMode");
            var arena=typeof(WoodlandGame).GetProperty("ArenaIndex");
            Action<bool,string> require=(ok,message)=>{if(!ok)throw new Exception(message);checks++;};
            for(int i=0;i<4;i++) {
                sprites[i]=Sprite.Create(texture,new Rect(0,0,32+i*8,32),new Vector2(.5f,.5f),100);
                game.planks[i]=sprites[i];game.PvpBases[i]=sprites[i];game.PvpLedges[i]=sprites[i];
            }
            for(int cycle=0;cycle<3;cycle++)for(int i=0;i<4;i++) {
                arena.SetValue(game,i);mode.SetValue(game,false);apply.Invoke(game,null);
                require(game.plankArt.sprite==sprites[i] && ledge.GetComponent<SpriteRenderer>().sprite==sprites[i],"Map art selection");
                require(Mathf.Abs(game.plankArt.bounds.size.x-14)<.001f,"PVP visual width");
                require(Mathf.Abs(box.size.x*beam.transform.localScale.x-13.5f)<.001f,"PVP collision width");
                require(beam.transform.localScale.x==beam.transform.localScale.y,"PVP uniform scale");
                require(Mathf.Abs(ledge.GetComponent<SpriteRenderer>().bounds.size.x-4.2f)<.001f,"Ledge visual width");
                require(Mathf.Abs(ledge.GetComponent<BoxCollider2D>().size.x*ledge.transform.localScale.x-4.05f)<.001f,"Ledge collision width");
                mode.SetValue(game,true);apply.Invoke(game,null);
                require(beam.transform.localScale==new Vector3(2,3,1) && box.size==originalSize && box.offset==originalOffset && Mathf.Approximately(box.edgeRadius,.02f),"Balance geometry restored");
            }
            Debug.Log("PLATFORM_SIZING_PASS: "+checks+" checks across four maps and three PVP/Balance cycles");
        } finally {
            UnityEngine.Object.DestroyImmediate(root);
            foreach(var sprite in sprites)if(sprite)UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
    // Current rules. Historical V7/V8 runners below remain available for their archived reports.
    static void TickCounterplay()
    {
        try {
            if(Elapsed>20)throw new Exception("Counterplay timeout stage "+SessionState.GetInt("Woodland.Stage",-1));
            var game=WoodlandGame.Instance;if(!game)return;
            var a=game.players[0];var b=game.players[1];int stage=SessionState.GetInt("Woodland.Stage",-1);
            if(stage==-1 && Elapsed>.4f){
                game.ChooseMode(false);game.ConfirmCharacter();game.ConfirmCharacter();game.StartSelectedMatch();SetStage(0);
            }else if(stage==0 && game.playing && string.IsNullOrEmpty(game.message)){
                PreparePresentationPair(game,0);a.Body.position=new Vector2(-3,game.plank.position.y+1.4f);b.Body.position=new Vector2(3,game.plank.position.y+1.4f);SetStage(1);
            }else if(stage==1 && SimElapsed>.5f){Check(a.TryAttack(true),"Whiff probe starts");SetStage(2);
            }else if(stage==2 && SimElapsed>.35f){
                Check(a.ActionBusy,"Missed punch remains punishable after 350ms");
                Check(!a.TryAttack(false) && !a.TryDash() && !a.SetBlock(true),"Whiff cannot cancel into kick, dash or guard");
                Check(a.ComboCount==0 && b.HitsRemaining==5,"Whiff earns no route credit or damage");SetStage(3);
            }else if(stage==3 && SimElapsed>.4f){PreparePresentationPair(game,0);SetStage(4);
            }else if(stage==4 && SimElapsed>.5f){b.enabled=false;Check(b.SetBlock(true),"Defender establishes guard");SetStage(5);
            }else if(stage==5 && SimElapsed>.2f){Check(a.TryAttack(true),"Punch challenges established guard");SetStage(6);
            }else if(stage==6 && SimElapsed>.17f){
                Check(b.HitsRemaining==5 && a.ComboCount==0,"Block denies hit-square loss and combo credit");
                Check(b.Stamina>75,"One blocked punch does not drain a quarter of guard");
                b.SetBlock(false);Check(b.TryAttack(true),"Defender retaliates during blocked recovery");SetStage(7);
            }else if(stage==7 && SimElapsed>.17f){
                Check(a.HitsRemaining==4 && b.HitsRemaining==5,"Block then punish wins a hit without trading");b.enabled=true;SetStage(8);
            }else if(stage==8 && SimElapsed>.8f){PreparePresentationPair(game,0);SetStage(9);
            }else if(stage==9 && SimElapsed>.5f){Check(a.TryAttack(true),"Repeated-punch probe starts");SetStage(10);
            }else if(stage==10 && SimElapsed>.15f){a.TryAttack(true);SetStage(11);
            }else if(stage==11 && SimElapsed>.28f){a.TryAttack(true);SetStage(12);
            }else if(stage==12 && SimElapsed>.35f){
                Check(a.ComboCount<3,"Three repeated punches never earn a finisher");
                PreparePresentationPair(game,0);SetStage(13);
            }else if(stage==13 && SimElapsed>.5f){Check(a.TryAttack(true),"Deliberate route starts");SetStage(14);
            }else if(stage==14 && SimElapsed>.15f){a.TryAttack(true);SetStage(15);
            }else if(stage==15 && SimElapsed>.28f){a.TryAttack(false);SetStage(16);
            }else if(stage==16 && SimElapsed>.3f){
                Check(a.ComboCount==3 && b.HitsRemaining==2,"Confirmed punch-punch-kick earns the finisher naturally");
                Check(Mathf.Approximately(a.Stamina,78),"Confirmed route spends exactly 22 stamina without attacking regeneration");
                Check(b.Body.linearVelocity.x>0,"Deliberate finisher launches toward an edge");
                Capture("Documentation/Counterplay-v9.png");Complete(true,"COUNTERPLAY_PASS: whiff recovery, block punishment, guard endurance, anti-mash route and natural finisher");
            }
        }catch(Exception exception){Complete(false,exception.ToString());}
    }
    static void TickContacts()
    {
        try {
            if(Elapsed>15)throw new Exception("Contact regression timed out at "+SessionState.GetInt("Woodland.Stage",-1));
            var game=WoodlandGame.Instance;if(!game)return;var a=game.players[0];var b=game.players[1];var hud=game.hud;
            int stage=SessionState.GetInt("Woodland.Stage",-1);
            if(stage==-1 && Elapsed>.4f){
                foreach(var button in hud.modeButtons)Check(button.image.sprite && button.image.sprite.name.StartsWith("Button_") && !button.transform.Find("Illustrated Label"),"Single illustrated surface: "+button.name);
                game.ChooseMode(false);game.PreviewCharacter(0);originalInputSettings=InputSystem.settings;validationInputSettings=UnityEngine.Object.Instantiate(originalInputSettings);validationInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;validationInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=validationInputSettings;testKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=DriveTestInput;heldKey=Key.DownArrow;SetStage(0);
            }else if(stage==0 && SimElapsed>.15f){heldKey=Key.None;Check(game.PendingCharacter==4,"Keyboard down moves four columns to the next row [pending="+game.PendingCharacter+"]");SetStage(1);}
            else if(stage==1 && SimElapsed>.15f){heldKey=Key.UpArrow;SetStage(2);}
            else if(stage==2 && SimElapsed>.15f){heldKey=Key.None;Check(game.PendingCharacter==0,"Keyboard up returns to the same column");SetStage(3);}
            else if(stage==3 && SimElapsed>.15f){heldKey=Key.RightArrow;SetStage(4);}
            else if(stage==4 && SimElapsed>.15f){heldKey=Key.None;Check(game.PendingCharacter==1,"Keyboard right moves one card");SetStage(5);}
            else if(stage==5 && SimElapsed>.15f){game.PreviewCharacter(0);game.ConfirmCharacter();game.PreviewCharacter(1);heldKey=Key.DownArrow;SetStage(6);}
            else if(stage==6 && SimElapsed>.15f){heldKey=Key.None;Check(game.PendingCharacter==5,"P2 down retains its column while P1 stays locked");testPads[0]=InputSystem.AddDevice<Gamepad>();padStates[0]=new GamepadState().WithButton(GamepadButton.DpadUp);SetStage(7);}
            else if(stage==7 && SimElapsed>.15f){padStates[0]=new GamepadState();Check(game.PendingCharacter==1,"D-pad up moves between roster rows");SetStage(8);}
            else if(stage==8 && SimElapsed>.25f){padStates[0]=new GamepadState{leftStick=new Vector2(0,-1)};SetStage(9);}
            else if(stage==9 && SimElapsed>.15f){padStates[0]=new GamepadState();Check(game.PendingCharacter==5,"Stick down moves between roster rows");InputSystem.RemoveDevice(testPads[0]);testPads[0]=null;game.ConfirmCharacter();game.StartSelectedMatch();SetStage(10);}
            else if(stage==10 && game.playing && string.IsNullOrEmpty(game.message)){PreparePresentationPair(game,0);a.Body.position=new Vector2(-1.9f,a.Body.position.y);b.Body.position=new Vector2(0,b.Body.position.y);SetStage(11);}
            else if(stage==11 && SimElapsed>.4f){Check(b.BodyContact(a.MeleeBounds(true)),"Punch reaches the actual near edge of a body at 1.9 units");Check(a.TryAttack(true),"Body-contact punch starts");SetStage(12);}
            else if(stage==12 && SimElapsed>.17f){Check(b.HitsRemaining==4,"Visible body-edge punch loses one hit square");PreparePresentationPair(game,0);a.Body.position=new Vector2(-3,a.Body.position.y);b.Body.position=new Vector2(0,b.Body.position.y);SetStage(13);}
            else if(stage==13 && SimElapsed>.4f){Check(!b.BodyContact(a.MeleeBounds(true)),"A punch beyond the body cannot connect");b.enabled=false;Check(b.SetBlock(true),"Front shield raises");a.Body.position=new Vector2(b.GuardBounds.min.x-a.punchRange+.08f,b.Body.position.y);Physics2D.SyncTransforms();SetStage(14);}
            else if(stage==14 && SimElapsed>.2f){Check(b.ShieldVisible && b.GuardBounds.size.y>1.5f,"Opaque animated shield has a visible matching trigger");Check(b.ShieldContact(a.MeleeBounds(true),1) && !b.BodyContact(a.MeleeBounds(true)),"Front shield intercepts a strike before it reaches the body");Check(a.TryAttack(true),"Shield-edge punch starts");SetStage(15);}
            else if(stage==15 && SimElapsed>.17f){Check(b.HitsRemaining==5 && b.Stamina<100,"Shield-edge contact consumes guard instead of body health [hits="+b.HitsRemaining+", stamina="+b.Stamina+", cue="+a.Status+"]");Capture("Documentation/Contact-Shield-v15.png");b.enabled=true;PreparePresentationPair(game,0);a.Body.position=new Vector2(1.8f,a.Body.position.y);b.Body.position=new Vector2(0,b.Body.position.y);typeof(Scout).GetField("face",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(a,-1f);b.enabled=false;b.SetBlock(true);SetStage(16);}
            else if(stage==16 && SimElapsed>.2f){Check(!b.ShieldContact(a.MeleeBounds(true),-1),"The exposed back never intercepts a rear strike");Check(a.TryAttack(true),"Rear punch starts");SetStage(17);}
            else if(stage==17 && SimElapsed>.17f){Check(b.HitsRemaining==4,"A rear strike damages a guarding fighter");b.enabled=true;PreparePresentationPair(game,0);a.Body.position=new Vector2(-3,a.Body.position.y);b.Body.position=new Vector2(3,b.Body.position.y);SessionState.SetInt("Woodland.ContactHits",0);SetStage(18);}
            else if(stage==18 && SimElapsed>.24f){int hit=SessionState.GetInt("Woodland.ContactHits",0);Check(a.ReceiveHit(b,-1,true) && b.ReceiveHit(a,1,true),"Recovery setup hit "+hit);a.Body.linearVelocity=b.Body.linearVelocity=Vector2.zero;a.Body.position=new Vector2(-3,a.Body.position.y);b.Body.position=new Vector2(3,b.Body.position.y);SessionState.SetInt("Woodland.ContactHits",hit+1);SetStage(hit<4?18:19);}
            else if(stage==19 && SimElapsed>.45f){Check(a.Downed && b.Downed && a.KnockdownRemaining>3.8f,"Untapped knockdown lasts 4.5 seconds [a="+a.Downed+", b="+b.Downed+", remaining="+a.KnockdownRemaining+", duration="+a.knockdownDuration+"]");a.inputEnabled=true;heldKey=Key.X;SetStage(20);}
            else if(stage==20 && SimElapsed>.3f){heldKey=Key.None;Check(a.RecoveryPresses==1,"Holding X produces one recovery press, not automatic mashing");Check(hud.recoveryPrompts[0].gameObject.activeSelf && hud.recoveryLabels[0].text.Contains("X") && hud.recoveryFills[0].fillAmount>0,"Circular recovery prompt shows the assigned input and recovery progress");SessionState.SetInt("Woodland.Mash",0);SetStage(21);}
            else if(stage==21 && SimElapsed>.1f){if(a.Downed)a.TryRecover();int n=SessionState.GetInt("Woodland.Mash",0)+1;SessionState.SetInt("Woodland.Mash",n);if(n==2)Capture("Documentation/Recovery-Prompt-v15.png");if(n<15)SetStage(21);else SetStage(22);}
            else if(stage==22 && SimElapsed>.6f){Check(!a.Downed && b.Downed,"Tapping wakes earlier while the untouched rival remains down");Check(!a.TryRecover(),"Recovery presses do nothing while standing");SessionState.SetInt("Woodland.UltCharacter",0);PrepareContactUltimate(game,0);SetStage(30);}
            else if(stage==30 && SimElapsed>.4f){Check(a.TryUltimate(),"Ultimate starts: "+a.displayName);SetStage(31);}
            else if(stage==31 && SimElapsed>2.6f){int c=SessionState.GetInt("Woodland.UltCharacter",0);Check(b.HitsRemaining<5,"Ultimate contacts a target "+(c==5?"six":"four")+" units away: "+a.displayName);Check(!a.UltimateActive && !a.ActionBusy,"Ultimate finishes without stuck action: "+a.displayName);c++;if(c<8){SessionState.SetInt("Woodland.UltCharacter",c);PrepareContactUltimate(game,c);SetStage(30);}else{PrepareContactUltimate(game,5);SetStage(32);}}
            else if(stage==32 && SimElapsed>.4f){Check(a.TryUltimate(),"Volt interruption probe starts");SetStage(33);}
            else if(stage==33 && SimElapsed>.82f){a.Stagger(.35f);Check(!a.UltimateActive && Mathf.Abs(a.Body.linearVelocity.x)<.01f,"Interrupting Volt clears cinematic and rush velocity immediately");SessionState.SetInt("Woodland.RecoveryCharacter",0);PreparePresentationPair(game,0);SetStage(40);}
            else if(stage==40 && SimElapsed>.3f){int c=SessionState.GetInt("Woodland.RecoveryCharacter",0);typeof(Scout).GetProperty("HitsRemaining").SetValue(b,0);b.StartCoroutine((System.Collections.IEnumerator)typeof(Scout).GetMethod("TemporaryKnockdown",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(b,null));b.Body.gravityScale=0;if(b.CharacterIndex==2){a.inputEnabled=true;heldKey=Key.D;}foreach(string state in new[]{"Knockout","Downed","GetUp"}){var frames=b.animations.Get(state);Check(frames.Length==10 && frames.All(frame=>AssetDatabase.GetAssetPath(frame).Contains("/CharactersRework/"+game.CharacterNames[b.CharacterIndex]+"/"+state+"/")),"Native recovery catalog: "+game.CharacterNames[b.CharacterIndex]+" / "+state);}SetStage(41);}
            else if(stage==41 && SimElapsed>.6f){int c=SessionState.GetInt("Woodland.RecoveryCharacter",0);Check(b.art.sprite && b.animations.downed.Contains(b.art.sprite),"Animator displays the new downed sprite: "+game.CharacterNames[b.CharacterIndex]);if(b.CharacterIndex==2){Check(a.HurtBounds.max.x+.1f<b.HurtBounds.min.x,"Walking Ember stays outside Luna's downed body");Check(Mathf.Abs(a.Body.position.y-b.Body.position.y)<.12f,"Walking into a downed rival does not climb onto their face");}Capture("Documentation/Recovery-v15/Runtime-"+game.CharacterNames[b.CharacterIndex]+".png");heldKey=Key.None;a.inputEnabled=false;b.enabled=true;c++;if(c<8){SessionState.SetInt("Woodland.RecoveryCharacter",c);PreparePresentationPair(game,c);SetStage(40);}else Complete(true,"CONTACTS_PASS: single buttons, grid navigation, body and shield contacts, recovery input and eight ultimates");}
        }catch(Exception error){Complete(false,error.ToString());}
    }
    static void PrepareContactUltimate(WoodlandGame game,int character)
    {
        game.StopAllCoroutines();game.playing=true;game.message="";game.score[0]=game.score[1]=0;
        PreparePresentationPair(game,character);var a=game.players[0];var b=game.players[1];a.Body.position=new Vector2(character==5?-3:-2,a.Body.position.y);b.Body.position=new Vector2(character==5?3:2,b.Body.position.y);typeof(Scout).GetProperty("HitsRemaining").SetValue(a,1);Physics2D.SyncTransforms();
    }
    static void TickMidtermCombat()
    {
        try {
            if(Elapsed>9)throw new Exception("Midterm combat timeout");
            var game=WoodlandGame.Instance;if(!game)return;var a=game.players[0];var b=game.players[1];
            int stage=SessionState.GetInt("Woodland.Stage",-1);
            if(stage==-1 && Elapsed>.4f){game.ChooseMode(false);game.ConfirmCharacter();game.ConfirmCharacter();game.StartSelectedMatch();SetStage(0);}
            else if(stage==0 && game.playing && string.IsNullOrEmpty(game.message)){
                PreparePresentationPair(game,0);a.Body.position=new Vector2(-3,a.Body.position.y);b.Body.position=new Vector2(3,b.Body.position.y);SetStage(1);
            }else if(stage==1 && SimElapsed>.4f){Check(b.SetPointerGuard(true),"Guard starts through the live hold path");SessionState.SetFloat("Woodland.GuardEnergy",b.Stamina);SetStage(2);}
            else if(stage==2 && SimElapsed>1){
                Check(b.Blocking && SessionState.GetFloat("Woodland.GuardEnergy",0)-b.Stamina<5.5f,"Holding guard for one second costs less than 5.5 stamina [blocking="+b.Blocking+", before="+SessionState.GetFloat("Woodland.GuardEnergy",0)+", after="+b.Stamina+", drain="+b.shieldDrain+", playing="+game.playing+"]");
                float before=b.Stamina;Check(!b.ReceiveHit(a,1,true),"Established guard blocks the frontal punch");
                Check(before-b.Stamina<8,"Blocking a punch costs less energy than throwing one");b.SetPointerGuard(false);
                PreparePresentationPair(game,0);a.Body.position=new Vector2(-3,a.Body.position.y);b.Body.position=new Vector2(1,b.Body.position.y);SessionState.SetInt("Woodland.DownHits",0);SetStage(3);
            }else if(stage==3 && SimElapsed>.26f){
                int count=SessionState.GetInt("Woodland.DownHits",0);b.Body.position=new Vector2(1,b.Body.position.y);b.Body.linearVelocity=new Vector2(count==4?3:0,0);
                Check(b.ReceiveHit(a,1,true),"Knockdown setup confirms hit "+count);count++;SessionState.SetInt("Woodland.DownHits",count);
                if(count==5){Check(b.Downed,"Five confirmed hits cause knockdown");SessionState.SetFloat("Woodland.DownX",b.Body.position.x);SetStage(4);}else SetStage(3);
            }else if(stage==4 && SimElapsed>.6f){
                Check(Mathf.Abs(b.Body.position.x-SessionState.GetFloat("Woodland.DownX",0))<.12f,"A knocked-down fighter does not coast after the hit");
                Check(b.ReceiveHit(a,1,true),"The rival can still shove a downed fighter");SetStage(5);
            }else if(stage==5 && SimElapsed>.15f){
                Check(b.Body.position.x>SessionState.GetFloat("Woodland.DownX",0)+.07f,"An explicit opponent shove moves the downed body");
                PreparePresentationPair(game,5);a.Body.position=new Vector2(-2,a.Body.position.y);b.Body.position=new Vector2(2,b.Body.position.y);
                typeof(Scout).GetProperty("HitsRemaining").SetValue(a,1);SetStage(6);
            }else if(stage==6 && SimElapsed>.4f){
                Check(game.hud.ultimatePanels[0].activeSelf,"Ultimate-ready rail is visible at one hit square");
                SessionState.SetFloat("Woodland.ReadySweepX",game.hud.ultimatePanels[0].transform.Find("Ready Ink Sweep").GetComponent<RectTransform>().anchoredPosition.x);SetStage(60);
            }else if(stage==60 && SimElapsed>.15f){
                Check(Mathf.Abs(game.hud.ultimatePanels[0].transform.Find("Ready Ink Sweep").GetComponent<RectTransform>().anchoredPosition.x-SessionState.GetFloat("Woodland.ReadySweepX",0))>10,"Ultimate-ready ink highlight moves over time");
                Capture("Documentation/RuneRumble-UltimateReady.png");Check(a.TryUltimate(),"Volt starts Thunder Rush from four units away");SetStage(7);
            }
            else if(stage==7 && SimElapsed>2.6f){
                Check(b.HitsRemaining<5,"Thunder Rush reaches and damages the rival at four units");
                Check(a.UltimateUsed && !a.UltimateActive,"Thunder Rush ends and spends its once-per-round use");
                SessionState.SetInt("Woodland.RangeCharacter",0);PrepareMidtermRange(game,0);SetStage(8);
            }else if(stage==8 && SimElapsed>.4f){Check(a.TrySpecial(),"Long-range cast charges for fighter "+a.CharacterIndex);SetStage(9);}
            else if(stage==9 && SimElapsed>2.5f){
                int character=SessionState.GetInt("Woodland.RangeCharacter",0);
                Check(b.HitsRemaining<5,"Projectile reaches a rival 10.5 units away: "+character);
                character++;if(character<8){SessionState.SetInt("Woodland.RangeCharacter",character);PrepareMidtermRange(game,character);SetStage(8);}
                else {SessionState.SetInt("Woodland.RangeUltimate",0);PrepareMidtermRange(game,2);typeof(Scout).GetProperty("HitsRemaining").SetValue(a,1);SetStage(20);}
            }else if(stage==20 && SimElapsed>.4f){Check(a.TryUltimate(),"Ranged ultimate starts for fighter "+a.CharacterIndex);SetStage(21);}
            else if(stage==21 && ((b.HitsRemaining<5 && SimElapsed>1.75f) || SimElapsed>2.5f)){
                Check(b.HitsRemaining<5,"Ranged ultimate reaches a rival 10.5 units away: "+a.CharacterIndex);
                if(SessionState.GetInt("Woodland.RangeUltimate",0)==0){SessionState.SetInt("Woodland.RangeUltimate",1);PrepareMidtermRange(game,7);typeof(Scout).GetProperty("HitsRemaining").SetValue(a,1);SetStage(20);}
                else Complete(true,"MIDTERM_COMBAT_PASS: affordable guard, stable knockdowns, Thunder Rush, eight long-range attacks and two ranged ultimates");
            }
        }catch(Exception error){Complete(false,error.ToString());}
    }
    static void PrepareMidtermRange(WoodlandGame game,int character)
    {
        game.StopAllCoroutines();game.playing=true;game.message="";game.score[0]=game.score[1]=0;
        PreparePresentationPair(game,character);
        game.players[0].Body.position=new Vector2(-5,game.players[0].Body.position.y);
        game.players[1].Body.position=new Vector2(5.5f,game.players[1].Body.position.y);
    }
    static void CheckHudIconCenters(WoodlandHUD hud)
    {
        foreach(var sprite in hud.characterActionCovers) {
            Check(sprite && sprite.texture.width==128 && sprite.texture.height==128,"HUD action uses a dedicated 128-pixel illustrated ability canvas");
            var pixels=sprite.texture.GetPixels32();double total=0,x=0,y=0;
            for(int i=0;i<pixels.Length;i++)if(pixels[i].a>32){double weight=pixels[i].a*pixels[i].a;total+=weight;x+=(i%128)*weight;y+=(i/128)*weight;}
            Check(total>0 && Vector2.Distance(new Vector2((float)(x/total),(float)(y/total)),new Vector2(64,63))<2,"Visible icon artwork is centered inside its circular safe area");
        }
        for(int i=0;i<hud.actionCovers.Length;i++) {
            var cover=hud.actionCovers[i].rectTransform;var disc=hud.actionDiscs[i].rectTransform;
            Check(Vector3.Distance(disc.InverseTransformPoint(cover.TransformPoint(cover.rect.center)),disc.rect.center)<.01f,"Live action icon box is centered on its disc");
        }
        Check(hud.GetComponentsInChildren<Text>(true).All(t=>t.font && t.font.name.StartsWith("Rumble") && t.fontStyle!=FontStyle.Italic && t.fontStyle!=FontStyle.BoldAndItalic),"All interface labels use the three original upright typefaces");
        Check(hud.GetComponentsInChildren<Text>(true).Select(t=>t.font.name).Distinct().Count()==3,"Header, subheader and body font roles are all present");
        for(int player=0;player<2;player++) {
            var guard=hud.actionDiscs[player*4].rectTransform;var punch=hud.actionDiscs[player*4+1].rectTransform;
            var kick=hud.actionDiscs[player*4+2].rectTransform;var special=hud.actionDiscs[player*4+3].rectTransform;
            Check(special.anchoredPosition.y>punch.anchoredPosition.y && guard.anchoredPosition.y<punch.anchoredPosition.y && punch.anchoredPosition.x<special.anchoredPosition.x && kick.anchoredPosition.x>special.anchoredPosition.x,"Skills retain the controller diamond placement");
        }
        var vs=hud.matchHud.transform.Find("Versus Emblem").GetComponent<Image>();
        Check(vs.sprite && hud.roundTimerText.fontSize<=24 && hud.roundTimerText.rectTransform.anchoredPosition.y<vs.rectTransform.anchoredPosition.y-vs.rectTransform.rect.height,"The new VS artwork sits above a small live timer");
    }
    static void TickDuel()
    {
        try {
            if(Elapsed>35)throw new Exception("Duel validation timed out at stage "+SessionState.GetInt("Woodland.Stage",-1));
            var game=WoodlandGame.Instance;if(!game)return;
            var a=game.players[0];var b=game.players[1];var hud=game.hud;
            int stage=SessionState.GetInt("Woodland.Stage",-1);
            if(stage==-1 && Elapsed>.3f) {
                Check(!game.BalanceMode && !hud.rulesMenu,"Balance and its selection menu are removed");
                Check(game.CharacterNames.Length==8 && game.backgrounds.Length==6,"Eight fighters and six arenas are saved");
                Check(game.roundsToWin==3 && game.roundDuration==99,"Rounds are 99 seconds, first to three");
                foreach(var catalog in game.CharacterAnimationSets)foreach(string state in new[]{"Walk","Jump","Hurt","Push","Punch","Kick","Knockout","Block","DoubleJump","Special","Dash","Backstep","Victory","Ultimate","Downed","GetUp"})Check(catalog.Get(state).Length==10 && catalog.Get(state).All(sprite=>sprite),"Complete native animation "+catalog.name+" / "+state);
                CheckLiveLabels(hud.titleMenu);
                foreach(var button in hud.modeButtons) {
                    Check(button.image.sprite && button.image.sprite.name.StartsWith("Button_") && !button.transform.Find("Illustrated Label"),"Main menu has one full-width illustrated button without an overlapping frame");
                    Check(!button.transform.Find("Choice").gameObject.activeSelf,"Generated button text is not duplicated by a live label");
                }
                Check(!hud.titleMenu.transform.Find("Lower Reading Field") && !hud.characterMenu.transform.Find("Roster Footer Shade"),"Menu footer background panels are removed");
                Check(hud.titleMenu.transform.Find("Illustrated Woodland").GetComponent<Image>().sprite.name=="BattleBackdrop","The main menu uses the eight-rival battle illustration");
                Check(!hud.helpHint && !hud.GetComponentsInChildren<Text>(true).Any(text=>text.name.EndsWith("Footer") || text.text.Contains("ARROWS /") || text.text.Contains("LEFT / RIGHT  SELECT")),"Bottom navigation instruction text is removed from every HUD");
                foreach(var button in hud.GetComponentsInChildren<Button>(true))if(button.transform.Find("Choice") && button.transform.Find("Choice").GetComponent<Text>().text.Length>0)Check(button.image.sprite && button.image.sprite.name.StartsWith("Button_"),"Every menu and pause option has generated label artwork: "+button.name);
                foreach(var button in hud.GetComponentsInChildren<Button>(true)) {
                    if(!button.transform.Find("Ink Focus Stroke"))continue;
                    Check(button.image.sprite && button.image.sprite.name=="BrushBand","Menu action uses the irregular ink brush silhouette: "+button.name);
                    var label=button.transform.Find("Choice").GetComponent<Text>();
                    Check(label.color.r>.9f && label.color.g>.9f,"Ink menu labels stay bright against the dark paint: "+button.name);
                }
                Capture("Documentation/RuneRumble-Title.png");
                Capture("Documentation/RuneRumble-Title-16x10.png",1440,900);
                Capture("Documentation/RuneRumble-Title-Ultrawide.png",1920,800);
                hud.modeButtons[2].onClick.Invoke();SetStage(-7);
            }else if(stage==-7 && Elapsed>.4f){
                Check(game.Screen==WoodlandGame.MenuScreen.Settings,"Generated settings button opens settings");CheckLiveLabels(hud.settingsMenu);Capture("Documentation/RuneRumble-Settings.png");
                hud.settingsBackButton.onClick.Invoke();hud.modeButtons[1].onClick.Invoke();Check(game.Screen==WoodlandGame.MenuScreen.Characters,"Saved local-versus button opens roster");SetStage(-2);
            } else if(stage==-2 && Elapsed>.6f) {
                CheckLiveLabels(hud.characterMenu);Capture("Documentation/RuneRumble-CharacterSelect.png");
                Capture("Documentation/RuneRumble-CharacterSelect-16x10.png",1440,900);
                Capture("Documentation/RuneRumble-CharacterSelect-Ultrawide.png",1920,800);
                Check(hud.hudPortraits.Length==8 && hud.hudPortraits.All(p=>p && p.texture.width>=512),"Eight dedicated high-resolution HUD portraits exist");
                Check(hud.selectionIdleFrames.Length==32 && hud.selectionIdleFrames.All(p=>p && p.texture.height>=512),"Eight four-pose high-resolution selection loops exist");
                foreach(var pose in hud.selectionIdleFrames) {
                    var pixels=pose.texture.GetPixels32();int w=pose.texture.width,h=pose.texture.height;
                    bool clear=Enumerable.Range(0,w).All(x=>pixels[x].a<49 && pixels[(h-1)*w+x].a<49) && Enumerable.Range(0,h).All(y=>pixels[y*w].a<49 && pixels[y*w+w-1].a<49);
                    Check(clear,"Selection pose retains head and clothing details inside transparent margins");
                }
                Check(hud.selectionPortraits[0].transform.parent.gameObject.activeSelf && !hud.selectionPortraits[1].transform.parent.gameObject.activeSelf,"Only active P1 showcase is visible");
                foreach(var card in hud.characterButtons)Check(card.transform.Find("Portrait Card Placeholder") && card.transform.Find("Painted Card Frame"),"Fighter cards have layered painted frames");
                foreach(var controller in game.CharacterControllers)foreach(var clip in controller.animationClips)Check(!AnimationUtility.GetCurveBindings(clip).Any(binding=>binding.type==typeof(Transform)),"Sprite clip has no artificial transform rotation: "+clip.name);
                SessionState.SetString("Woodland.SelectionPose",AssetDatabase.GetAssetPath(hud.selectionPortraits[0].sprite));SetStage(-5);
            } else if(stage==-5 && Elapsed>.45f) {
                Check(AssetDatabase.GetAssetPath(hud.selectionPortraits[0].sprite)!=SessionState.GetString("Woodland.SelectionPose",""),"Selection idle changes artwork over time without scaling the body");
                Check(hud.selectionPortraits[0].transform.localScale==Vector3.one,"Breathing uses drawn poses at one body scale");
                hud.characterButtons[4].onClick.Invoke();hud.confirmCharacterButton.onClick.Invoke();SetStage(-3);
            } else if(stage==-3 && Elapsed>.6f) {
                Check(!hud.selectionPortraits[0].transform.parent.gameObject.activeSelf && hud.selectionPortraits[1].transform.parent.gameObject.activeSelf,"P2 uses the same left-side showcase after P1 locks in");
                Check(hud.selectionPortraits[0].transform.parent.localPosition==hud.selectionPortraits[1].transform.parent.localPosition,"Both player showcases occupy the same left-side position");
                Capture("Documentation/RuneRumble-CharacterSelect-P2.png");
                hud.characterButtons[7].onClick.Invoke();SetStage(-6);
            } else if(stage==-6 && Elapsed>.6f) {
                var hero=hud.selectionPortraits[1];var texture=hero.sprite.texture;
                int bottomRow=texture.GetPixels32().Select((pixel,index)=>new{pixel,index}).Where(value=>value.pixel.a>48).Min(value=>value.index/texture.width);
                float artSize=Mathf.Min(hero.rectTransform.rect.width,hero.rectTransform.rect.height);
                float bootsY=hero.rectTransform.TransformPoint(new Vector3(0,-artSize*.5f+artSize*bottomRow/texture.height,0)).y;
                var shade=hero.transform.parent.Find("Showcase Name Shade").GetComponent<RectTransform>();
                Check(bootsY>shade.TransformPoint(new Vector3(0,shade.rect.yMax,0)).y,"The selection nameplate sits below the visible boots");
                Check(!hud.characterMenu.transform.Find("Roster Footer Shade"),"The selection skill subtitle has no opaque footer covering it");
                Capture("Documentation/RuneRumble-Marin-Selection.png");
                Check(hud.selectionIdleFrames.Skip(28).Take(4).Contains(hud.selectionPortraits[1].sprite),"Marin displays its own selection animation");
                hud.confirmCharacterButton.onClick.Invoke();hud.mapButtons[5].onClick.Invoke();SetStage(-4);
            } else if(stage==-4 && Elapsed>.6f) {
                Check(game.CharacterChoices[0]==4 && game.CharacterChoices[1]==7 && game.PendingArena==5,"Saved roster, lock-in and map buttons select the requested match");
                CheckLiveLabels(hud.mapMenu);Capture("Documentation/RuneRumble-StageSelect.png");
                Check(hud.mapButtons[5].GetComponent<RectTransform>().anchoredPosition.y>-138 && hud.mapButtons[5].transform.localScale.x>1.04f,"Selected stage visibly lifts and enlarges after switching");
                Check(hud.mapButtons.Length==6 && hud.mapButtons.All(c=>c.GetComponent<UnityEngine.UI.Mask>()),"Six tall angled stage masks exist");
                for(int index=0;index<6;index++)Check(hud.mapButtons[index].transform.Find("Arena Name").GetComponent<Image>().sprite.name=="Arena_"+index,"Arena names use dedicated illustrated plaques: "+index);
                Check(game.arenaNames[0]=="CINDER QUAY","Cinder Quay replaces the previous primary arena");
                Capture("Documentation/RuneRumble-StageSelect-16x10.png",1440,900);
                Capture("Documentation/RuneRumble-StageSelect-Ultrawide.png",1920,800);
                hud.startButton.onClick.Invoke();SetStage(0);
            } else if(stage==0 && game.playing && string.IsNullOrEmpty(game.message)) {
                Check(game.backgroundArt.color==WoodlandGame.ArenaExposure,"Arena scenery uses the subdued exposure independently from fighter art");
                Check(game.CharacterNames[7]=="Marin" && game.SkillNames[7]=="Tide Surge","The eighth fighter is Marin with the tide identity");
                var marinIdle=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CharactersRework/Marin/Idle/01.png");
                var marinRows=marinIdle.texture.GetPixels32().Select((pixel,index)=>new{pixel,index}).Where(value=>value.pixel.a>48).Select(value=>value.index/marinIdle.texture.width).ToArray();
                Check(marinRows.Max()-marinRows.Min()+1>=205 && Mathf.Approximately(marinIdle.pixelsPerUnit,80),"Marin retains a larger visible body at the roster's common pixels-per-unit scale");
                Check(Mathf.Abs(marinRows.Min()-marinIdle.pivot.y)<=2,"Marin's larger artwork keeps its feet on the common baseline");
                Check(game.plank.bodyType==RigidbodyType2D.Kinematic && !game.plankArt.enabled,"Ground is fixed with no floating platform art");
                Check(game.RoundTimeRemaining>97 && hud.roundTimerText,"Round starts with a live 99-second timer");
                Check(game.edgeHatches.All(h=>h && !h.enabled) && game.HatchIsOpen(0) && game.HatchIsOpen(1),"Both edge holes are open from round start");
                Check(Mathf.Abs(game.edgeHatchArt[0].bounds.size.x-3.6f)<.03f && Mathf.Abs(game.edgeHatchArt[1].bounds.center.x-8.2f)<.03f,"Large open manholes sit at the outer edges");
                foreach(var fighter in game.players){fighter.aiControlled=false;fighter.inputEnabled=false;}
                PlaceOnPvpBase(game,false);SetStage(1);
            } else if(stage==1 && SimElapsed>.4f) {
                Check(a.Grounded && b.Grounded,"Fighters stand on the central floor");
                CheckLiveLabels(hud.matchHud);CheckHudIconCenters(hud);
                Capture("Documentation/RuneRumble-HUD.png");
                Capture("Documentation/RuneRumble-HUD-16x10.png",1440,900);
                Capture("Documentation/RuneRumble-HUD-Ultrawide.png",1920,800);
                for(int map=0;map<6;map++){game.SelectArena(map);Capture("Documentation/RuneRumble-Arena-"+map+".png");}
                game.SelectArena(5);
                hud.OpenMoveGuide();CheckLiveLabels(hud.helpPanel);Capture("Documentation/RuneRumble-MoveGuide.png");hud.CloseMoveGuide();game.ResumeMatch();

                Check(a.Facing>0 && b.Facing<0,"Fighters face their rival");
                float feetY=Camera.main.WorldToViewportPoint(a.Body.position-Vector2.up*1.1875f).y;
                Check(feetY>.17f && feetY<.27f,"Standing fighters sit lower, leaving only about a quarter of the frame for floor");
                Check(Camera.main.orthographicSize<3.2f && a.GetComponent<Collider2D>().bounds.size.y/(Camera.main.orthographicSize*2)>.37f,"Close fight camera makes standing fighters at least 37 percent of the screen height");
                a.Body.gravityScale=b.Body.gravityScale=0;a.Body.linearVelocity=b.Body.linearVelocity=Vector2.zero;
                a.Body.position=new Vector2(-5,-.36f);b.Body.position=new Vector2(5,-.36f);SetStage(100);
            } else if(stage==100 && SimElapsed>.65f) {
                Check(Camera.main.orthographicSize>3.4f,"Fight camera pulls back when rivals separate");
                Check(Camera.main.WorldToViewportPoint(a.Body.position).x>.05f && Camera.main.WorldToViewportPoint(b.Body.position).x<.95f,"Separated rivals stay inside the camera frame");
                b.Body.position=new Vector2(5,2.2f);SetStage(101);
            } else if(stage==101 && SimElapsed>.65f) {
                Check(Camera.main.orthographicSize>4.8f && Camera.main.WorldToViewportPoint(b.Body.position+Vector2.up*(b.CharacterIndex==7?1.4375f:1.1875f)).y<.85f,"Fight camera makes room for the larger airborne rival below the top HUD");
                PlaceOnPvpBase(game,false);
                SessionState.SetFloat("Woodland.PauseZoom",Camera.main.orthographicSize);
                SessionState.SetFloat("Woodland.DuelClock",game.RoundTimeRemaining);game.TogglePause();SetStage(2);
            } else if(stage==2 && Elapsed>.35f) {
                Check(Mathf.Approximately(game.RoundTimeRemaining,SessionState.GetFloat("Woodland.DuelClock",0)),"Pause freezes the timer");
                Check(Mathf.Abs(Camera.main.orthographicSize-SessionState.GetFloat("Woodland.PauseZoom",0))<.01f,"Pause holds the current camera framing");
                CheckLiveLabels(hud.pauseMenu);Check(hud.pauseButtons.All(button=>button.image.sprite.name.StartsWith("Button_")),"Battle options use the long generated ink controls");Capture("Documentation/RuneRumble-Pause.png");
                game.ResumeMatch();SetStage(3);
            } else if(stage==3 && SimElapsed>.35f) {
                Check(game.RoundTimeRemaining<SessionState.GetFloat("Woodland.DuelClock",0)-.2f,"Timer resumes");
                b.Body.gravityScale=1;b.Body.linearVelocity=Vector2.zero;
                Check(b.ReceiveHit(a,1,true),"First confirmed hit connects");SetStage(4);
            } else if(stage==4 && SimElapsed>.65f) {
                b.Body.linearVelocity=Vector2.zero;b.Body.position=new Vector2(1,game.plank.position.y+1.4f);
                int remaining=b.HitsRemaining;
                if(remaining>0){Check(b.ReceiveHit(a,1,true),"Confirmed hit removes one square");SetStage(4);}
                else {Check(b.Downed && game.playing && !b.KnockedOut,"Fifth hit temporarily knocks down without ending the round");SetStage(5);}
            } else if(stage==5 && SimElapsed>b.knockdownDuration+.4f) {
                Check(!b.Downed && b.HitsRemaining==5,"Recovery restores five hit squares");
                b.Body.gravityScale=1;b.Body.position=new Vector2(8.2f,game.plank.position.y+1.2f);b.Body.linearVelocity=Vector2.zero;SetStage(6);
            } else if(stage==6 && b.KnockedOut) {
                Check(!game.playing,"Falling into an open hole ends the round");SetStage(7);
            } else if(stage==7 && game.score[0]==1) {
                Check(game.score[1]==0,"Ring-out awards one round to the rival");game.RestartMatch();SetStage(8);
            } else if(stage==8 && game.playing && string.IsNullOrEmpty(game.message)) {
                Check(Mathf.Approximately(game.SafeFloorHalfWidth,WoodlandGame.InitialSafeFloorHalfWidth) && !game.SuddenDeath,"Restart restores full floor width");
                typeof(WoodlandGame).GetProperty("RoundTimeRemaining").SetValue(game,.01f);SetStage(9);
            } else if(stage==9 && SimElapsed>1.5f) {
                Check(game.SuddenDeath && game.playing && game.score.All(value=>value==0),"99-second timeout keeps the fight running in sudden death");
                Check(game.SafeFloorHalfWidth<WoodlandGame.InitialSafeFloorHalfWidth-.2f && game.plank.GetComponent<BoxCollider2D>().size.x<(WoodlandGame.InitialSafeFloorHalfWidth-.2f)*2,"Both holes expand inward and shrink the safe collision floor");
                Capture("Documentation/RuneRumble-SuddenDeath.png");game.RestartMatch();SetStage(10);
            } else if(stage==10 && game.playing && string.IsNullOrEmpty(game.message)) {
                Check(!game.SuddenDeath && Mathf.Approximately(game.SafeFloorHalfWidth,WoodlandGame.InitialSafeFloorHalfWidth),"Next round resets the expanding holes");
                SessionState.SetInt("Woodland.PresentationCharacter",0);PreparePresentationPair(game,0);SetStage(11);
            } else if(stage==11 && SimElapsed>.4f) {
                int c=SessionState.GetInt("Woodland.PresentationCharacter",0);
                Check(a.Grounded && b.Grounded,"Presentation pair is grounded: "+c);
                Check(WoodlandEffects.Instance.AttackFramesLoaded(c)==24,"Four six-frame brush effects loaded: "+c);
                Check(WoodlandEffects.Instance.ComboFramesLoaded(c)==12,"Hit and finisher VFX loaded: "+c);
                SessionState.SetInt("Woodland.PresentationVfx",WoodlandEffects.Instance.ComboAnimationsPlayed);
                Check(a.TryAttack(true),"Natural punch starts: "+c);SetStage(12);
            } else if(stage==12 && ((b.HitsRemaining<5 && SimElapsed>.11f) || SimElapsed>1)) {
                int c=SessionState.GetInt("Woodland.PresentationCharacter",0);
                Check(b.HitsRemaining==4,"Natural punch connects and removes one segment: "+c+" [hits="+b.HitsRemaining+", position="+a.Body.position+" / "+b.Body.position+", facing="+a.Facing+", state="+a.AnimationState+", ground="+a.Grounded+", playing="+game.playing+"]");
                Check(WoodlandEffects.Instance.ComboAnimationsPlayed>SessionState.GetInt("Woodland.PresentationVfx",0),"Natural punch plays animated hit VFX: "+c);
                Check(Quaternion.Angle(a.art.transform.localRotation,Quaternion.identity)<.1f,"Punch remains upright: "+c);
                Capture("Documentation/RuneRumble-Punch-"+c+".png");SetStage(13);
            } else if(stage==13 && SimElapsed>.45f) {
                PreparePresentationPair(game,SessionState.GetInt("Woodland.PresentationCharacter",0));SetStage(14);
            } else if(stage==14 && SimElapsed>.4f) {
                Check(a.TryAttack(false),"Natural kick starts");SetStage(15);
            } else if(stage==15 && ((b.HitsRemaining<5 && SimElapsed>.13f) || SimElapsed>1)) {
                int c=SessionState.GetInt("Woodland.PresentationCharacter",0);
                Check(b.HitsRemaining==4 && a.AnimationState=="Kick","Kick uses its own poses and connects: "+c);
                Check(Quaternion.Angle(a.art.transform.localRotation,Quaternion.identity)<.1f,"Kick remains upright: "+c);
                Capture("Documentation/RuneRumble-Kick-"+c+".png");SetStage(16);
            } else if(stage==16 && SimElapsed>.45f) {
                int c=SessionState.GetInt("Woodland.PresentationCharacter",0)+1;
                if(c<8){SessionState.SetInt("Woodland.PresentationCharacter",c);PreparePresentationPair(game,c);SetStage(11);}
                else {SessionState.SetInt("Woodland.ShieldCharacter",0);PreparePresentationPair(game,0);a.SetPointerGuard(true);SetStage(160);}
            }else if(stage==160 && SimElapsed>.2f){
                int character=SessionState.GetInt("Woodland.ShieldCharacter",0);
                Check(a.Blocking && WoodlandEffects.Instance.ShieldFramesLoaded(character)==6,"Six real elemental shield poses display while guarding: "+character);
                Capture("Documentation/RuneRumble-Shield-"+game.CharacterNames[character]+".png");character++;
                hud.OpenMoveGuide();CheckLiveLabels(hud.helpPanel);Capture("Documentation/RuneRumble-Guide-"+game.CharacterNames[character-1]+".png");hud.CloseMoveGuide();game.ResumeMatch();
                if(character<8){SessionState.SetInt("Woodland.ShieldCharacter",character);PreparePresentationPair(game,character);a.SetPointerGuard(true);SetStage(160);}
                else {PreparePresentationPair(game,7);b.Body.gravityScale=0;b.Body.position=new Vector2(8.2f,game.plank.position.y+1.8f);b.Body.linearVelocity=Vector2.zero;SetStage(165);}
            }else if(stage==165 && SimElapsed>.4f && Camera.main.WorldToViewportPoint(b.art.bounds.max).x<.98f){
                b.Body.gravityScale=1;b.Body.position=new Vector2(8.2f,game.plank.position.y+.8f);b.Body.linearVelocity=Vector2.down;SetStage(17);
            } else if(stage==17 && b.RingOutFalling && SimElapsed>.2f) {
                Check(!b.KnockedOut && !b.GetComponent<Collider2D>().enabled,"Ring-out presentation disables contact before scoring");
                float fallingX=Camera.main.WorldToViewportPoint(b.Body.position).x;
                Check(fallingX>.03f && fallingX<.97f,"The ring-out camera keeps the falling rival visible at the edge");
                Check(Camera.main.WorldToViewportPoint(b.art.bounds.max).x<.995f,"The complete horizontal fall sprite stays within the right camera edge");
                Check(hud.actionClusters.All(group=>group && group.alpha<.2f),"Skill diamonds fade during ring-out so the losing fighter remains readable");
                Capture("Documentation/RuneRumble-Fall.png");SetStage(18);
            } else if(stage==18 && b.KnockedOut) {
                Check(!b.art.enabled && !b.Body.simulated,"Fallen fighter disappears into the shaft");
                Complete(true,"DUEL_PRESENTATION_PASS: centered responsive cards and portraits, upright natural attacks, all eight hit effects, large outer shafts and falling presentation");
            }
        } catch(Exception exception){Complete(false,exception.ToString());}
    }
    static void CheckLiveLabels(GameObject screen)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var text in screen.GetComponentsInChildren<Text>()) {
            if(!text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text))continue;
            if(text.name=="Device Letter")Check(text.preferredWidth<=text.rectTransform.rect.width,"Input badge stays on one line: "+text.text);
            Check(text.preferredHeight<=text.rectTransform.rect.height+1,"Visible label fits its height: "+screen.name+" / "+text.name+" ["+text.preferredHeight+" <= "+text.rectTransform.rect.height+"]");
        }
    }
    static void PreparePresentationPair(WoodlandGame game,int character)
    {
        game.CharacterChoices[0]=character;game.CharacterChoices[1]=(character+1)%8;
        for(int side=0;side<2;side++) {
            var actor=game.players[side];int c=game.CharacterChoices[side];
            actor.SetCharacter(c,game.CharacterNames[c],game.CharacterAnimationSets[c],game.CharacterControllers[c],game.CharacterColors[c]);
            actor.ResetScout();actor.inputEnabled=false;actor.aiControlled=false;
            actor.Body.position=new Vector2(side==0?-.6f:.6f,game.plank.position.y+.15f+1.1875f);actor.transform.position=actor.Body.position;
            actor.Body.simulated=true;actor.Body.gravityScale=1;
        }
        Physics2D.SyncTransforms();game.hud.Refresh();
    }
    static void Begin(bool exit)
    {
        if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; EditorApplication.delayCall += () => Begin(exit); return; }
        EditorSceneManager.OpenScene("Assets/Scenes/WoodlandTumble.unity");
        SessionState.SetBool("Woodland.ValidationExit", exit);
        SessionState.SetString("Woodland.Checks", "");
        SessionState.SetBool(Active, true);
        SessionState.SetInt("Woodland.PushCount", 0);
        SessionState.SetFloat("Woodland.LastForce", 0);
        SessionState.SetBool("Woodland.RosterCaptured",false);
        SetStage(-1);
        EditorApplication.isPlaying = true;
    }
    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        SessionState.SetString("Woodland.Checks", SessionState.GetString("Woodland.Checks", "") + message + "\n");
    }
    static void SetStage(int stage) { SessionState.SetInt("Woodland.Stage", stage);SessionState.SetFloat("Woodland.StageTime", (float)EditorApplication.timeSinceStartup);SessionState.SetFloat("Woodland.StageGameTime", Time.time); }
    static float SimElapsed => Time.time - SessionState.GetFloat("Woodland.StageGameTime", 0);
    static float Elapsed => (float)EditorApplication.timeSinceStartup - SessionState.GetFloat("Woodland.StageTime", 0);
    static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying) return;
        if(SessionState.GetBool("Woodland.Contacts",false)){TickContacts();return;}
        if(SessionState.GetBool("Woodland.MidtermCombat",false)){TickMidtermCombat();return;}
        if(SessionState.GetBool("Woodland.Counterplay",false)){TickCounterplay();return;}
        if(SessionState.GetBool("Woodland.Duel",false)){TickDuel();return;}
        if(SessionState.GetBool("Woodland.V8",false)){TickV8();return;}
        if(SessionState.GetBool("Woodland.MeleeOnly",false)) { TickMelee(); return; }
        if(SessionState.GetBool("Woodland.HudOnly",false)) { TickHUD(); return; }
        try {
            if (Elapsed > 40) throw new Exception("Play Mode validation timed out.");
            var game = WoodlandGame.Instance;if (game == null) return;
            int stage = SessionState.GetInt("Woodland.Stage", 0);
            var a = game.players[0];var b = game.players[1];
            if (stage == -1 && Elapsed > .25f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title && !game.playing && !game.Paused, "Boot opens the title menu without starting or pausing a match");
                Check(!game.plank.simulated && game.players.All(scout => !scout.Body.simulated), "Title menu freezes the plank and both players");
                Check(game.hud.menuRoot && game.hud.titleMenu && game.hud.mapMenu && game.hud.matchHud && game.hud.startButton && game.hud.backButton && game.hud.modeText && game.hud.mapDescription, "Menu roots, navigation buttons and descriptive labels are wired");
                Check(game.hud.modeButtons != null && game.hud.modeButtons.Length == 4 && game.hud.modeButtons.All(button => button != null) && game.hud.mapButtons != null && game.hud.mapButtons.Length == 4 && game.hud.mapButtons.All(button => button != null), "All four main menu buttons and four map buttons are wired");
                game.hud.RefreshMenu();Capture("Documentation/main-menu-1280-v7.png");
                Check(game.hud.ruleButtons.Length == 2 && game.hud.ruleButtons.All(button => button != null) && game.hud.characterButtons.Length == 4 && game.hud.characterButtons.All(button => button != null) && game.hud.confirmCharacterButton && game.hud.ruleBackButton && game.hud.characterBackButton && game.hud.settingsBackButton, "Rules, four-character roster and settings navigation are wired");
                SelectMenuRoster(game, false, true, 0);
                game.hud.startButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Match && !game.SoloMode, "Map start button enters the selected local match");
                SetStage(0);
            } else if (stage == 0 && game.playing && Time.timeSinceLevelLoad > 3) {
                Scout.UltimateCinematic-=TrackUltimate;Scout.UltimateCinematic+=TrackUltimate;cinematicStarts=cinematicStops=0;
                Check(Camera.main != null && game.plank != null && game.hud != null, "Saved camera, platform and HUD references work");
                Check(a.transform.position.x < -2 && b.transform.position.x > 2, "Players spawn at separate editable spawn points");
                Check(a.transform.position.y > -2 && b.transform.position.y > -2, "Players land on the plank");
                foreach (var scout in game.players) foreach (string state in new[] { "Walk", "Jump", "Hurt", "Push", "Punch", "Knockout", "Block", "DoubleJump", "Kick", "Dash", "Backstep", "Victory", "Ultimate" }) {
                    var frames = scout.animations.Get(state);
                    Check(frames.Length == 10 && frames.All(f => f != null && f.rect.width == (WideAction(state)?384:256) && f.rect.height == 256), scout.displayName + "/" + state + ": ten independent sprites use the correct canvas width");
                    Check(scout.animator.HasState(0, Animator.StringToHash("Base Layer." + state)), scout.displayName + "/" + state + ": editable Animator state exists");
                }
                foreach (var scout in game.players) foreach (string state in new[] { "Dash", "Charge", "Smash", "Combo2", "Combo3", "Backstep", "Grab" }) {
                    var controller = scout.animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                    Check(controller != null && scout.animator.HasState(0, Animator.StringToHash("Base Layer." + state)), scout.displayName + "/" + state + ": real Animator state exists");
                    var node = controller.layers[0].stateMachine.states.FirstOrDefault(entry => entry.state.name == state).state;
                    var clip = node == null ? null : node.motion as AnimationClip;
                    Check(clip != null && AnimationUtility.GetObjectReferenceCurveBindings(clip).Any(binding => binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite" && AnimationUtility.GetObjectReferenceCurve(clip, binding).Any(key => key.value is Sprite)), scout.displayName + "/" + state + ": assigned clip contains real sprite keyframes");
                }
                ValidateRosterAssets();
                for(int character=0;character<4;character++)Check(WoodlandEffects.Instance.ComboFramesLoaded(character)==12,game.CharacterNames[character]+": twelve dedicated animated combo-effect sprites are loaded");
                Check(game.hud.healthFill.All(i => i.sprite != null && i.type == Image.Type.Filled), "HUD health fills and panel sprites are serialized");
                Check(game.hud.shieldFill.Length == 2 && game.hud.shieldFill.All(image => image != null && image.type == Image.Type.Filled), "Both real stamina meters have wired filled images");
                Check(game.hud.specialCooldown.Length==2 && game.hud.specialCooldown.All(image=>image!=null) && game.hud.specialCooldownText.Length==2 && game.hud.specialCooldownText.All(text=>text!=null) && game.hud.ultimateLabels.Length==2 && game.hud.ultimateLabels.All(text=>text!=null),"Both fighters have wired special cooldown indicators and ultimate readiness labels");
                Check(game.hud.actionDiscs.Length == 8 && game.hud.actionDiscs.All(image => image != null) && game.hud.actionIcons.Length == 8 && game.hud.actionIcons.All(image => image != null) && game.hud.actionGlyphs.Length == 8 && game.hud.actionGlyphs.All(text => text != null) && game.hud.actionCovers.Length == 8 && game.hud.actionCovers.All(image => image != null && image.sprite != null), "Both four-button action diamonds have complete discs, controller badges and illustrated action covers");
                Check(game.hud.helpPanel != null && game.hud.damageGhost.Length == 2 && game.hud.damageGhost.All(image => image != null) && game.hud.fighterPlates.Length == 2 && game.hud.fighterPlates.All(plate => plate != null), "Presentation HUD has wired help panel, two damage ghosts and two fighter plates");
                Check(game.hud.deviceText != null && game.hud.deviceText.Length == 2 && game.hud.deviceText.All(text => text != null) && game.hud.controlsText != null && game.hud.controlsText.Length == 2 && game.hud.controlsText.All(text => text != null), "Both player device labels and help control texts are wired");
                ValidateHudCorners(game);
                ValidateHudMeters(game);
                ValidateCharacterActionCovers(game);
                foreach(var label in game.hud.healthText)Check(label.preferredHeight<=label.rectTransform.rect.height,"HP digits fit their text rectangle without clipping");
                Check(WoodlandEffects.Instance.fernSlash.Length==8 && WoodlandEffects.Instance.emberSlash.Length==8 && WoodlandEffects.Instance.fernSlash.All(frame=>frame!=null) && WoodlandEffects.Instance.emberSlash.All(frame=>frame!=null),"Both elemental attack animations contain eight loaded VFX frames");
                if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-silent-test")>=0)Check(AudioListener.volume==0,"Silent test mode keeps speaker output muted");
                ValidateAudio(game);
                Capture("Documentation/GamePreview-v7.png");
                var originalCollider = game.plank.GetComponent<BoxCollider2D>().size;
                var originalScale = game.plank.transform.localScale;
                for (int arena = 0; arena < 4; arena++) {
                    game.SelectArena(arena);
                    Check(game.ArenaIndex == arena && game.backgroundArt.sprite == game.backgrounds[arena] && game.plankArt.sprite == game.planks[arena] && game.sphereArt.sprite == game.spheres[arena], "Arena " + arena + ": matching background, plank and sphere switch together");
                    Check(game.plank.GetComponent<BoxCollider2D>().size == originalCollider && game.plank.transform.localScale == originalScale, "Arena " + arena + ": visual changes preserve collider proportions");
                    Capture("Documentation/arena-" + arena + "-1280-v7.png");
                }
                game.SelectArena(1);
                game.fallBoundary = -100;
                game.plank.bodyType = RigidbodyType2D.Kinematic;
                Arrange(game);
                Check(a.TryAttack(true), "Punch action starts");
                Check(Mathf.Approximately(a.Stamina, a.StaminaCapacity - 8), "Accepted punch spends eight real stamina");
                Check(a.AnimationState == "Punch", "Punch uses its own animation");
                SetStage(10);
            } else if (stage == 10 && SimElapsed > .25f) {
                Check(Mathf.Approximately(b.Health, b.maxHealth - a.punchDamage), "Punch applies reduced damage (" + a.punchDamage + ")");
                Check(b.Body.linearVelocity.x > a.punchKnockback && b.Body.linearVelocity.x < 6, "Punch applies moderated knockback");
                Check(WoodlandEffects.Instance.ActiveMotes > 0 && WoodlandEffects.Instance.ActiveMotes <= 320, "Pooled attack VFX are active within capacity");
                Capture("Documentation/VFXPreview-v7.png");
                a.ResetScout();b.ResetScout();Arrange(game);
                Check(a.TryAttack(false), "Kick action starts");
                Check(Mathf.Approximately(a.Stamina, a.StaminaCapacity - 6), "Accepted kick spends six real stamina");
                Check(a.AnimationState == "Kick", "Kick uses its own animation");
                SetStage(1);
            } else if (stage == 1 && SimElapsed > .25f) {
                int count = SessionState.GetInt("Woodland.PushCount", 0);
                Check(Mathf.Approximately(b.Health, Mathf.Max(0, b.maxHealth - (count + 1) * a.pushDamage)), "Kick " + (count + 1) + " applies reduced damage (" + a.pushDamage + ")");
                float force = b.Body.linearVelocity.x;
                Check(force > SessionState.GetFloat("Woodland.LastForce", 0), "Kick knockback grows as health drops (" + force.ToString("F2") + ")");
                SessionState.SetFloat("Woodland.LastForce", force);
                SessionState.SetInt("Woodland.PushCount", count + 1);
                if (b.Health <= 0) {
                    Check(b.KnockedOut && b.AnimationState == "Knockout" && !game.playing, "Health depletion triggers dedicated knockout state and stops controls");
                    SetStage(3);
                } else SetStage(2);
            } else if (stage == 2 && SimElapsed > 1.1f) {
                Arrange(game);
                Check(a.TryAttack(false), "Kick recovers after cooldown");
                SetStage(1);
            } else if (stage == 3 && SimElapsed > .8f) {
                // Place the collapsed sprite on the board for a clear animation preview.
                b.Body.linearVelocity = Vector2.zero;b.Body.position = new Vector2(1.5f, .12f);b.transform.position = b.Body.position;
                Capture("Documentation/KnockoutPreview-v7.png");
                SetStage(4);
            } else if (stage == 4 && SimElapsed > .65f) {
                Check(game.score[0] == 1, "Knockout awards the opponent a round");
                Check(a.AnimationState=="Victory","The actual round winner enters its dedicated victory animation");
                game.StopAllCoroutines();game.playing = true;game.message = "";
                game.plank.GetComponent<HingeJoint2D>().enabled = false;
                game.plank.bodyType = RigidbodyType2D.Kinematic;
                originalInputSettings = InputSystem.settings;validationInputSettings = UnityEngine.Object.Instantiate(originalInputSettings);
                validationInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                validationInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings = validationInputSettings;
                testKeyboard = InputSystem.AddDevice<Keyboard>();testKeyboard.MakeCurrent();
                InputSystem.onBeforeUpdate += DriveTestInput;
                SessionState.SetInt("Woodland.EdgeCase", 0);
                StartEdgeCase(game, 0);
                SetStage(5);
            } else if (stage == 5 && SimElapsed > .95f) {
                int index = SessionState.GetInt("Woodland.EdgeCase", 0);
                float edgeY = SessionState.GetFloat("Woodland.EdgeY", 0);
                Check(Mathf.Abs(a.MovementInput) > .9f, "Edge case " + index + ": continuous input reaches the player controller");
                Check(a.Body.position.y < edgeY - 1.2f, "Edge case " + index + ": falls clear while input is held (position=" + a.Body.position + ", input=" + a.MovementInput + ")");
                Check(!a.Grounded, "Edge case " + index + ": side/corner is not ground");
                if (index < 5) { SessionState.SetInt("Woodland.EdgeCase", index + 1);StartEdgeCase(game, index + 1);SetStage(5); }
                else { StartJumpCase(game);SetStage(6); }
            } else if (stage == 6 && SimElapsed > .15f) {
                Check(a.Body.linearVelocity.y > 4 && a.Body.position.y > SessionState.GetFloat("Woodland.JumpY", 0) + .3f, "Jump still launches cleanly after ground-control changes");
                Check(WoodlandEffects.Instance.ActiveMotes > 0, "Jump and movement VFX emit successfully");
                SetStage(7);
            } else if (stage == 7 && SimElapsed > 1.2f) {
                Check(a.Grounded && Mathf.Abs(a.Body.linearVelocity.y) < .5f, "Player lands and settles on the plank");
                heldKey = Key.None;
                b.ResetScout();b.Body.simulated=true;b.Body.gravityScale=0;b.Body.position=new Vector2(2.2f,.15f);b.transform.position=b.Body.position;
                a.inputEnabled = false;
                float jumpStamina = a.Stamina;
                Check(a.TryJump() && a.AirJumpsRemaining == 1, "Ground jump preserves the one aerial jump budget");
                Check(Mathf.Approximately(a.Stamina, jumpStamina - 4), "Ground jump spends four stamina");
                Check(a.TryJump() && a.AirJumpsRemaining == 0, "Double jump consumes exactly one aerial jump");
                Check(Mathf.Approximately(a.Stamina, jumpStamina - 10), "Aerial jump spends another six stamina");
                Check(!a.TryJump(), "Third jump is denied before landing");
                Check(Mathf.Approximately(a.Stamina, jumpStamina - 10), "Denied third jump spends no additional stamina");
                Check(a.AnimationState == "DoubleJump", "Aerial jump selects its dedicated animation");
                SetStage(20);
            } else if (stage == 20 && SimElapsed > .18f) {
                Check(WoodlandEffects.Instance.ActiveMotes > 0, "Double jump produces visible pooled effects");
                Capture("Documentation/combat-doublejump-1280-v7.png");
                a.ResetScout();b.ResetScout();Arrange(game);
                Check(a.TryDash(), "Dash starts when ready");
                Check(Mathf.Approximately(a.Stamina, a.StaminaCapacity - 10), "Accepted dash spends ten stamina");
                Check(a.AnimationState == "Dash" && a.Body.linearVelocity.x >= a.dashSpeed * .99f, "Dash uses its animation and intended launch speed");
                Check(!a.TryDash() && !a.TryAttack(true), "Dash denies a second dash and attacks during its burst");
                Check(Mathf.Approximately(a.Stamina, a.StaminaCapacity - 10), "Denied actions during a dash spend no stamina");
                SetStage(21);
            } else if (stage == 21 && SimElapsed > .22f) {
                Check(!a.TryDash() && a.DashReady < 1, "Dash remains unavailable after its burst until cooldown ends");
                SetStage(22);
            } else if (stage == 22 && SimElapsed > .9f) {
                Check(a.DashReady >= .999f && a.TryDash(), "Dash becomes available again after cooldown");
                PrepareGuard(game);
                SetStage(23);
            } else if (stage == 23 && SimElapsed > .22f) {
                Check(b.Blocking && b.Shield < b.shieldCapacity && b.Shield > 80, "Holding guard drains shield gradually");
                float shield = b.Shield;
                Check(!b.ReceiveHit(a, 1, true), "A frontal hit is absorbed after the parry window");
                Check(Mathf.Approximately(b.Health, b.maxHealth) && b.Shield < shield - a.punchDamage * 2, "Normal guard drains shield instead of health");
                Capture("Documentation/combat-guard-1280-v7.png");
                a.Body.position = b.Body.position + Vector2.right;
                a.transform.position = a.Body.position;Physics2D.SyncTransforms();
                Check(b.ReceiveHit(a, -1, true) && b.Health < b.maxHealth, "A rear hit bypasses the directional guard");
                PrepareGuard(game);
                Check(!b.ReceiveHit(a, 1, true), "A freshly raised guard parries a frontal hit");
                Check(a.Status == "STAGGERED" && !a.TryAttack(true) && !a.TryDash(), "Parry staggers the attacker and blocks immediate retaliation");
                Check(Mathf.Approximately(b.Health, b.maxHealth), "Parry preserves defender health");
                Capture("Documentation/combat-parry-1280-v7.png");
                SetStage(24);
            } else if (stage == 24 && SimElapsed > .45f) {
                Check(a.Status != "STAGGERED" && a.TryDash(), "Parry stagger expires and allows actions again");
                PrepareGuard(game);
                SetStage(25);
            } else if (stage == 25 && SimElapsed > .22f) {
                Check(!b.ReceiveHit(a, 1, true, 4), "A heavy frontal hit is initially absorbed by guard");
                Check(!b.Blocking && b.Shield == 0 && b.Status == "GUARD BROKEN", "Depleted guard enters its vulnerable recovery state");
                Check(!b.SetBlock(true) && !b.TryJump() && !b.TryDash() && !b.TryAttack(true) && !b.TrySpecial(), "Guard break denies defense, jump, dash and attacks during recovery");
                SetStage(26);
            } else if (stage == 26 && SimElapsed > .6f) {
                Check(b.Shield == 0 && !b.SetBlock(true), "Broken guard cannot instantly recharge or re-raise");
                SetStage(27);
            } else if (stage == 27 && SimElapsed > 1.5f) {
                Check(b.Status != "STAGGERED" && b.Shield > 0 && b.SetBlock(true), "Guard recovers only after its recovery delay");
                heldKey = Key.None;a.ResetScout();b.ResetScout();Arrange(game);
                PositionSpecialPair(game,5,1.4f);
                Check(a.TrySpecial() && !a.Charging && a.Stamina==a.StaminaCapacity-14,"Instant ranged special starts without holding and costs fourteen stamina");
                float energy=a.Stamina;Check(!a.TrySpecial() && a.Stamina==energy,"Rejected special spam spends no additional stamina");
                Check(a.SpecialCooldownRemaining>5.9f && a.SpecialReady<.01f,"Accepted ranged cast starts its six-second cooldown");
                SetStage(28);
            } else if (stage == 28 && SimElapsed > .85f) {
                Check(b.Health<b.maxHealth,"Ranged special hits a rival five world units away");
                game.hud.Refresh();Check(game.hud.specialCooldown[0].gameObject.activeSelf && game.hud.specialCooldownText[0].text==Mathf.CeilToInt(a.SpecialCooldownRemaining).ToString() && Mathf.Abs(game.hud.specialCooldown[0].fillAmount-(1-a.SpecialReady))<.001f,"Accepted ranged cast displays its real radial cooldown and whole remaining seconds");
                Capture("Documentation/ranged-special-1280-v7.png");
                SessionState.SetFloat("Woodland.SpecialPause",a.SpecialCooldownRemaining);
                game.TogglePause();SetStage(29);
            } else if (stage == 29 && Elapsed > .25f) {
                Check(game.Paused && Mathf.Abs(a.SpecialCooldownRemaining-SessionState.GetFloat("Woodland.SpecialPause",0))<.001f,"Pause freezes the ranged special cooldown timer");
                game.TogglePause();SetStage(129);
            } else if (stage == 129 && (a.SpecialCooldownRemaining<=0 || SimElapsed>6.2f)) {
                string diagnostic="remaining="+a.SpecialCooldownRemaining.ToString("F6")+", ready="+a.SpecialReady.ToString("F6")+", elapsed="+SimElapsed.ToString("F3")+", stamina="+a.Stamina+", state="+a.AnimationState;
                Debug.Log("SPECIAL_COOLDOWN_DIAGNOSTIC: "+diagnostic);
                Check(a.SpecialReady==1 && a.SpecialCooldownRemaining==0 && a.TrySpecial(),"Ranged special becomes usable again only after the full cooldown ("+diagnostic+")");
                a.ResetScout();b.ResetScout();
                foreach (var scout in game.players) {
                    Check(scout.Health == scout.maxHealth && scout.Shield == scout.shieldCapacity, scout.displayName + ": reset restores health and shield");
                    Check(!scout.Blocking && !scout.Charging && !scout.KnockedOut && !scout.UltimateUsed && !scout.UltimateActive && scout.SpecialReady==1 && scout.AirJumpsRemaining == 1 && scout.ComboCount == 0, scout.displayName + ": reset clears defense, special cooldown, ultimate use, knockout, combo and aerial budget");
                    Check(scout.DashReady == 1 && scout.PunchReady == 1 && scout.PushReady == 1 && scout.AnimationState == "Idle", scout.displayName + ": reset clears cooldowns and restores idle");
                }
                Arrange(game);
                Check(a.TryAttack(true), "Combo opener starts with punch");
                SetStage(30);
            } else if (stage == 30 && SimElapsed > .25f) {
                Check(a.ComboCount == 1 && Mathf.Approximately(b.Health, b.maxHealth - a.punchDamage), "A confirmed opener starts the combo counter");
                Arrange(game);
                float energy=a.Stamina;
                Check(!a.TryAttack(true), "A follow-up input buffers while the opening attack is recovering");
                Check(a.Stamina==energy,"Buffered melee input is free until its follow-up actually starts");
                SetStage(31);
            } else if (stage == 31 && SimElapsed > .5f) {
                Check(a.ComboCount == 2 && Mathf.Approximately(b.Health, b.maxHealth - 2 * a.punchDamage), "Buffered second hit connects before normal punch cooldown");
                Arrange(game);
                Check(!a.TryAttack(true), "Finisher input buffers during the second attack");
                SetStage(32);
            } else if (stage == 32 && SimElapsed > .5f) {
                Check(a.ComboCount == 3 && Mathf.Approximately(b.Health, b.maxHealth - 3.4f * a.punchDamage), "Three-hit combo connects with a stronger final hit");
                Capture("Documentation/combat-combo-1280-v7.png");
                SetStage(33);
            } else if (stage == 33 && SimElapsed > 1.1f) {
                Check(a.ComboCount == 0, "Combo counter expires when attacks stop connecting");
                a.ResetScout();b.ResetScout();
                SessionState.SetBool("Woodland.HelpWasVisible", game.hud.helpPanel.activeSelf);
                SessionState.SetBool("Woodland.WasPaused", game.Paused);
                heldKey = Key.F1;DriveTestInput();
                SetStage(34);
            } else if (stage == 34 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.HelpWasVisible", false), "F1 opens or closes the wired controls overlay");
                Check(game.Paused == SessionState.GetBool("Woodland.WasPaused", false), "Controls overlay does not change game pause state");
                Capture("Documentation/controls-1280-v7.png");
                heldKey = Key.None;DriveTestInput();SetStage(35);
            } else if (stage == 35 && SimElapsed > .1f) {
                heldKey = Key.F1;DriveTestInput();SetStage(36);
            } else if (stage == 36 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf == SessionState.GetBool("Woodland.HelpWasVisible", false), "A second F1 press restores the original overlay state");
                Check(game.Paused == SessionState.GetBool("Woodland.WasPaused", false), "Closing controls still leaves pause state unchanged");
                heldKey = Key.None;
                a.ResetScout();b.ResetScout();Arrange(game);
                Check(b.Facing < 0 && b.TryAttack(true), "Player two starts a left-facing punch");
                var effects = WoodlandEffects.Instance;
                SessionState.SetBool("Woodland.RestoreAudioKeys", true);
                SessionState.SetBool("Woodland.AudioKeysMuted", effects.Muted);
                SessionState.SetFloat("Woodland.AudioKeysVolume", effects.SfxVolume);
                effects.SetVolume(.5f, false);
                heldKey = Key.M;DriveTestInput();SetStage(37);
            } else if (stage == 37 && SimElapsed > .25f) {
                var effects = WoodlandEffects.Instance;
                Check(Mathf.Approximately(a.Health, a.maxHealth - b.punchDamage), "Player two left-facing punch damages player one");
                Check(effects.GetComponentsInChildren<SpriteRenderer>().Any(renderer => renderer.enabled && effects.emberSlash.Contains(renderer.sprite)), "Player two attack displays its own active Ember slash sprite");
                Capture("Documentation/combat-ember-1280-v7.png");
                Check(effects.Muted != SessionState.GetBool("Woodland.AudioKeysMuted", false), "Keyboard M toggles mute");
                heldKey = Key.Equals;DriveTestInput();SetStage(38);
            } else if (stage == 38 && SimElapsed > .1f) {
                Check(Mathf.Approximately(WoodlandEffects.Instance.SfxVolume, .6f), "Keyboard equals increases effect volume by one step");
                heldKey = Key.Minus;DriveTestInput();SetStage(39);
            } else if (stage == 39 && SimElapsed > .1f) {
                Check(Mathf.Approximately(WoodlandEffects.Instance.SfxVolume, .5f), "Keyboard minus decreases effect volume by one step");
                heldKey = Key.M;DriveTestInput();SetStage(40);
            } else if (stage == 40 && SimElapsed > .1f) {
                Check(WoodlandEffects.Instance.Muted == SessionState.GetBool("Woodland.AudioKeysMuted", false), "A second keyboard M press restores the original mute state");
                heldKey = Key.None;a.ResetScout();b.ResetScout();Arrange(game);
                Check(b.TryDash() && b.Body.linearVelocity.x <= -b.dashSpeed * .99f, "Player two dash launches left with its intended speed");
                BeginGamepadCases(game);SetStage(41);
            } else if (stage == 41 && SimElapsed > .12f) {
                Check(game.GetGamepad(0) == testPads[0] && game.GetGamepad(1) == testPads[1], "Two connected controllers occupy separate player slots");
                Check(a.MovementInput > .5f && Mathf.Abs(b.MovementInput) < .01f, "Player one stick moves only player one");
                padStates[1] = new GamepadState().WithButton(GamepadButton.DpadLeft);SetStage(42);
            } else if (stage == 42 && SimElapsed > .12f) {
                Check(b.MovementInput < -.9f && a.MovementInput > .5f, "Two controllers simultaneously move their own players in opposite directions");
                InputSystem.RemoveDevice(testPads[0]);testPads[0] = null;game.RefreshGamepads();SetStage(43);
            } else if (stage == 43 && SimElapsed > .12f) {
                Check(game.GetGamepad(0) == null && game.GetGamepad(1) == testPads[1], "Unplugging player one leaves player two in its original slot");
                Check(Mathf.Abs(a.MovementInput) < .01f && b.MovementInput < -.9f, "Disconnected controller releases movement while the other controller still works");
                Check(b.Health == b.maxHealth,"Hot unplug does not synthesize an attack");
                testPads[0] = InputSystem.AddDevice<Gamepad>();game.RefreshGamepads();
                Check(game.GetGamepad(0) == testPads[0] && game.GetGamepad(1) == testPads[1], "A replacement controller fills the empty slot without stealing player two");
                PreparePadCombat(game);padStates[0] = new GamepadState().WithButton(GamepadButton.West);SetStage(44);
            } else if (stage == 44 && SimElapsed > .25f) {
                Check(Mathf.Approximately(b.Health, b.maxHealth - a.punchDamage), "Player one west face button punches player two");
                PreparePadCombat(game);padStates[1] = new GamepadState().WithButton(GamepadButton.East);SetStage(45);
            } else if (stage == 45 && SimElapsed > .25f) {
                Check(Mathf.Approximately(a.Health, a.maxHealth - b.pushDamage), "Player two east face button kicks player one");
                PreparePadCombat(game);padStates[1] = new GamepadState { leftTrigger=1 };SetStage(46);
            } else if (stage == 46 && SimElapsed > .2f) {
                Check(b.Blocking && !a.Blocking && b.Shield < b.shieldCapacity, "Player two left trigger holds and drains its own guard");
                PreparePadCombat(game);b.Body.simulated = false;padStates[0] = new GamepadState().WithButton(GamepadButton.RightShoulder);SetStage(47);
            } else if (stage == 47 && SimElapsed > .08f && (a.DashReady<1 || Elapsed>2)) {
                Check(a.DashReady < 1 && a.Body.linearVelocity.x > 7, "Player one right shoulder launches a right-facing dash");
                PreparePadCombat(game);padStates[1] = new GamepadState().WithButton(GamepadButton.North);SetStage(48);
            } else if (stage == 48 && SimElapsed > .4f) {
                Check(!b.Charging && b.SpecialReady<1 && b.Stamina==b.StaminaCapacity-14,"Player two north button instantly casts once with a real cost and cooldown");
                Check(a.Health<a.maxHealth,"Player two instant ranged cast damages its left-facing rival");
                padStates[1]=new GamepadState();SetStage(49);
            } else if (stage == 49 && SimElapsed > .1f) {
                Capture("Documentation/controller-combat-1280-v7.png");
                PreparePadCombat(game);StartJumpCase(game, false);SetStage(50);
            } else if (stage == 50 && SimElapsed > .2f) {
                Check(a.Grounded, "Controller jump test begins on the walking surface");
                SessionState.SetFloat("Woodland.PadJumpY", a.Body.position.y);
                padStates[0] = new GamepadState().WithButton(GamepadButton.South);SetStage(51);
            } else if (stage == 51 && SimElapsed > .12f) {
                Check(a.Body.position.y > SessionState.GetFloat("Woodland.PadJumpY", 0) + .3f && a.AirJumpsRemaining == 1, "South face button jumps while preserving the extra aerial jump");
                padStates[0] = new GamepadState();SetStage(52);
            } else if (stage == 52 && SimElapsed > .06f) {
                padStates[0] = new GamepadState().WithButton(GamepadButton.South);SetStage(53);
            } else if (stage == 53 && SimElapsed > .1f) {
                Check(a.AirJumpsRemaining == 0 && a.AnimationState == "DoubleJump", "A second south face button press performs the dedicated aerial jump");
                PreparePadCombat(game);StartJumpCase(game,false,1);a.Body.simulated=false;SetStage(154);
            } else if (stage == 154 && SimElapsed > .2f) {
                Check(b.Grounded,"Player two controller jump case starts on the physical plank");SessionState.SetFloat("Woodland.P2JumpY",b.Body.position.y);
                padStates[1]=new GamepadState().WithButton(GamepadButton.South);SetStage(155);
            } else if (stage == 155 && SimElapsed > .12f) {
                Check(b.Body.position.y>SessionState.GetFloat("Woodland.P2JumpY",0)+.3f && b.AirJumpsRemaining==1,"Player two south button jumps and preserves its extra aerial jump");
                padStates[1]=new GamepadState();SetStage(156);
            } else if (stage == 156 && SimElapsed > .06f) {
                padStates[1]=new GamepadState().WithButton(GamepadButton.South);SetStage(157);
            } else if (stage == 157 && SimElapsed > .1f) {
                Check(b.AirJumpsRemaining==0 && b.AnimationState=="DoubleJump","Player two second south press performs its double jump");
                PreparePadCombat(game);padStates[0] = new GamepadState().WithButton(GamepadButton.Start);SetStage(54);
            } else if (stage == 54 && Elapsed > .12f) {
                Check(game.Paused, "Controller start pauses the game");
                padStates[0] = new GamepadState();SetStage(55);
            } else if (stage == 55 && Elapsed > .1f) {
                padStates[0] = new GamepadState().WithButton(GamepadButton.Start);SetStage(56);
            } else if (stage == 56 && Elapsed > .12f) {
                Check(!game.Paused, "A second controller start press resumes the game");
                SessionState.SetBool("Woodland.PadHelp", game.hud.helpPanel.activeSelf);
                padStates[0] = new GamepadState().WithButton(GamepadButton.Select).WithButton(GamepadButton.North);SetStage(62);
            } else if (stage == 62 && SimElapsed > .2f) {
                Check(a.SpecialReady==1 && a.Stamina==a.StaminaCapacity,"Select menu chord suppresses the north ranged-special action");
                padStates[0]=new GamepadState().WithButton(GamepadButton.Select);SetStage(57);
            } else if (stage == 57 && SimElapsed > .12f) {
                padStates[0] = new GamepadState();SetStage(58);
            } else if (stage == 58 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.PadHelp", false), "Releasing controller select toggles the help overlay");
                Check(!game.Paused, "Controller help leaves gameplay unpaused");
                SessionState.SetInt("Woodland.PadArena", game.ArenaIndex);
                padStates[0] = new GamepadState().WithButton(GamepadButton.Select).WithButton(GamepadButton.RightShoulder);SetStage(59);
            } else if (stage == 59 && SimElapsed > .12f) {
                Check(game.ArenaIndex == (SessionState.GetInt("Woodland.PadArena", 0) + 1) % 4, "Select plus right shoulder cycles to the next arena");
                Check(a.DashReady == 1, "Arena shortcut suppresses the shoulder dash action");
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.PadHelp", false), "Arena shortcut does not also toggle help");
                padStates[0] = new GamepadState();SetStage(60);
            } else if (stage == 60 && SimElapsed > .1f) {
                Check(game.hud.helpPanel.activeSelf != SessionState.GetBool("Woodland.PadHelp", false), "Releasing the arena shortcut does not accidentally toggle help");
                game.score[0] = 2;game.score[1] = 1;
                padStates[0] = new GamepadState().WithButton(GamepadButton.Select).WithButton(GamepadButton.South);SetStage(61);
            } else if (stage == 61 && SimElapsed > .12f) {
                Check(game.score[0] == 0 && game.score[1] == 0 && !game.playing, "Select plus south restarts the match into its countdown");
                Check(a.AirJumpsRemaining == 1 && !a.Blocking && a.Body.linearVelocity == Vector2.zero, "Rematch shortcut does not also block or move the player");
                game.hud.helpPanel.SetActive(SessionState.GetBool("Woodland.PadHelp", false));
                heldKey = Key.None;padStates[0] = padStates[1] = new GamepadState();
                game.ReturnToMenu();SetStage(70);
            } else if (stage == 70 && SimElapsed > .15f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title && !game.playing && !game.Paused && !game.plank.simulated && game.players.All(scout => !scout.Body.simulated), "Returning to title freezes gameplay and clears pause");
                Check(!a.TryAttack(true) && !a.TryJump() && !a.TryDash() && !a.TrySpecial(), "Title menu denies combat and movement actions");
                SelectMenuRoster(game, true, true, 0);
                Check(game.SoloMode && game.Screen == WoodlandGame.MenuScreen.Maps && !game.playing, "Solo rules and roster selection reach map selection without starting gameplay");
                for (int arena = 0; arena < 4; arena++) {
                    game.hud.mapButtons[arena].onClick.Invoke();
                    Check(game.PendingArena == arena && game.ArenaIndex == arena && !game.playing && !game.plank.simulated, "Map button " + arena + ": previews the matching arena without starting physics");
                }
                game.hud.backButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Characters && !game.playing, "Map Back button returns to character selection without starting a match");
                game.hud.characterBackButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Rules, "Character Back button returns to rule selection");
                game.hud.ruleBackButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Title, "Rules Back button returns to title");
                SelectMenuRoster(game, true, true, 2);
                game.hud.RefreshMenu();Capture("Documentation/map-menu-1280-v7.png");
                game.hud.startButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Match && game.SoloMode && game.BalanceMode && game.PendingArena == 2 && game.ArenaIndex == 2, "Start button preserves the chosen solo balance arena into the match");
                SetStage(71);
            } else if (stage == 71 && game.playing && SimElapsed > 2) {
                Check(!a.aiControlled && b.aiControlled, "Solo mode enables AI only for player two");
                PrepareAiCase(game, 4);
                SessionState.SetInt("Woodland.AIDecisions", b.AIDecisions);
                SessionState.SetFloat("Woodland.AIStartX", b.Body.position.x);
                padStates[1] = new GamepadState { leftStick = Vector2.right };
                SetStage(72);
            } else if (stage == 72 && SimElapsed > .65f) {
                Check(b.AIDecisions > SessionState.GetInt("Woodland.AIDecisions", 0), "Solo rival makes autonomous decisions without attack inputs");
                Check(Mathf.Abs(a.MovementInput) < .01f && b.Body.position.x < SessionState.GetFloat("Woodland.AIStartX", 0) - .2f, "Solo rival approaches the opponent instead of following its assigned pad's opposing stick");
                padStates[1] = new GamepadState();PrepareAiCase(game, 1.25f);
                SessionState.SetInt("Woodland.AIActions", b.AIActionRequests);SetStage(73);
            } else if (stage == 73 && SimElapsed > 2.2f) {
                Check(b.AIActionRequests > SessionState.GetInt("Woodland.AIActions", 0) && a.Health < a.maxHealth, "Autonomous rival performs real combat actions and damages the nearby opponent");
                Capture("Documentation/solo-combat-1280-v7.png");
                SessionState.SetInt("Woodland.AIDecisions", b.AIDecisions);
                SessionState.SetInt("Woodland.AIActions", b.AIActionRequests);
                SessionState.SetFloat("Woodland.AIFrozenX", b.Body.position.x);
                SessionState.SetFloat("Woodland.AIFrozenY", b.Body.position.y);
                game.TogglePause();SetStage(74);
            } else if (stage == 74 && Elapsed > .3f) {
                Check(game.Paused && b.AIDecisions == SessionState.GetInt("Woodland.AIDecisions", 0) && b.AIActionRequests == SessionState.GetInt("Woodland.AIActions", 0), "Pause freezes autonomous decisions and action counts");
                Check(Vector2.Distance(b.Body.position, new Vector2(SessionState.GetFloat("Woodland.AIFrozenX", 0), SessionState.GetFloat("Woodland.AIFrozenY", 0))) < .001f, "Pause freezes the solo rival's physical position");
                game.ReturnToMenu();
                SessionState.SetInt("Woodland.AIDecisions", b.AIDecisions);SessionState.SetInt("Woodland.AIActions", b.AIActionRequests);SetStage(75);
            } else if (stage == 75 && Elapsed > .3f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title && !game.Paused && !b.Body.simulated && b.AIDecisions == SessionState.GetInt("Woodland.AIDecisions", 0) && b.AIActionRequests == SessionState.GetInt("Woodland.AIActions", 0), "Returning from paused solo gameplay freezes AI and restores title navigation");
                heldKey = Key.RightArrow;DriveTestInput();SetStage(77);
            } else if (stage == 77 && SimElapsed > .12f) {
                heldKey = Key.Enter;DriveTestInput();SetStage(78);
            } else if (stage == 78 && SimElapsed > .12f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Rules && !game.SoloMode && !game.playing, "Keyboard direction and Enter choose local mode and open rules");
                heldKey = Key.Escape;DriveTestInput();SetStage(79);
            } else if (stage == 79 && SimElapsed > .12f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title, "Keyboard Escape backs out of rule selection");
                heldKey = Key.None;padStates[0] = new GamepadState().WithButton(GamepadButton.DpadDown);SetStage(80);
            } else if (stage == 80 && SimElapsed > .12f) {
                padStates[0] = new GamepadState().WithButton(GamepadButton.South);SetStage(81);
            } else if (stage == 81 && SimElapsed > .12f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Rules && !game.SoloMode && !game.playing, "Controller directional pad and south button open local rule selection");
                padStates[0] = new GamepadState().WithButton(GamepadButton.East);SetStage(82);
            } else if (stage == 82 && SimElapsed > .12f) {
                Check(game.Screen == WoodlandGame.MenuScreen.Title, "Controller east button backs out of rule selection");
                padStates[0] = new GamepadState();SelectMenuRoster(game, false, true, 1);
                heldKey = Key.RightArrow;DriveTestInput();SetStage(83);
            } else if (stage == 83 && SimElapsed > .12f) {
                Check(game.PendingArena == 2 && !game.playing, "Keyboard map navigation previews the next of four maps without starting");
                heldKey=Key.None;padStates[0]=new GamepadState().WithButton(GamepadButton.DpadRight);SetStage(84);
            } else if (stage == 84 && SimElapsed > .12f) {
                Check(game.PendingArena == 3 && !game.playing, "Controller map navigation reaches the fourth map without starting");
                padStates[0]=new GamepadState();game.hud.mapButtons[1].onClick.Invoke();game.hud.startButton.onClick.Invoke();SetStage(76);
            } else if (stage == 76 && game.playing && SimElapsed > 2) {
                Check(!game.SoloMode && !a.aiControlled && !b.aiControlled && game.ArenaIndex == 1, "Starting local mode after solo restores two human-controlled players on the chosen map");
                PreparePadCombat(game);
                SessionState.SetFloat("Woodland.TestCapacity", a.shieldCapacity);a.shieldCapacity = 8;a.ResetScout();Arrange(game);
                Check(a.TryAttack(true) && a.Stamina == 0, "An accepted punch can exhaust a small test stamina pool exactly");SetStage(90);
            } else if (stage == 90 && SimElapsed > .55f) {
                Check(a.Stamina == 0, "Spent stamina stays empty during its regeneration delay");
                Check(!a.TryAttack(false) && !a.TryJump() && !a.TryDash() && !a.TryBackstep() && !a.TryGrab() && !a.TrySpecial(), "Exhausted stamina denies kick, jump, dash, backstep, grab and special");
                Check(a.Stamina == 0, "Rejected exhausted actions do not create or spend stamina");SetStage(91);
            } else if (stage == 91 && SimElapsed > .2f) {
                Check(a.Stamina > 0 && a.Stamina <= 8, "Stamina begins regenerating after the spend delay and stays within capacity");
                a.shieldCapacity = SessionState.GetFloat("Woodland.TestCapacity", 100);PreparePadCombat(game);
                Check(a.TryBackstep() && a.Body.linearVelocity.x <= -6.9f && a.Stamina == a.StaminaCapacity - 12, "Backstep moves opposite facing and spends twelve stamina");
                float energy = a.Stamina;
                Check(!a.TryBackstep() && a.Stamina == energy, "Backstep cooldown rejection spends no additional stamina");
                Check(!a.TryDash() && a.Stamina == energy, "A forward dash cannot interrupt backstep or spend stamina during its burst");
                Check(!a.ReceiveHit(b, -1, true) && a.Health == a.maxHealth, "Backstep invulnerability rejects an immediate incoming hit");SetStage(92);
            } else if (stage == 92 && SimElapsed > .24f) {
                Check(a.ReceiveHit(b, -1, true) && a.Health < a.maxHealth, "Backstep invulnerability expires after its short evade window");SetStage(93);
            } else if (stage == 93 && SimElapsed > .8f) {
                Check(a.BackstepReady >= .999f && a.TryBackstep(), "Backstep becomes available again after its cooldown");
                PreparePadCombat(game);padStates[0]=padStates[1]=new GamepadState { rightTrigger=1 };SetStage(94);
            } else if (stage == 94 && SimElapsed > .35f) {
                Check(game.players.All(scout=>scout.Stamina==scout.StaminaCapacity && scout.Health==scout.maxHealth && scout.GrabReady==1 && !scout.UltimateUsed),"Either right trigger alone neither grabs nor spends stamina nor activates an ultimate");
                padStates[0]=padStates[1]=new GamepadState();SetStage(95);
            } else if (stage == 95 && SimElapsed > .1f) {
                PreparePadCombat(game);padStates[1]=new GamepadState().WithButton(GamepadButton.West);SetStage(159);
            } else if (stage == 159 && SimElapsed > .25f) {
                Check(a.Health==a.maxHealth-b.punchDamage,"Player two west button executes its own left-facing punch");
                PreparePadCombat(game);padStates[0]=new GamepadState().WithButton(GamepadButton.East);SetStage(160);
            } else if (stage == 160 && SimElapsed > .25f) {
                Check(b.Health==b.maxHealth-a.pushDamage,"Player one east button executes its own right-facing kick");
                PreparePadCombat(game);padStates[0]=new GamepadState{leftTrigger=1};SetStage(161);
            } else if (stage == 161 && SimElapsed > .2f) {
                Check(a.Blocking && !b.Blocking && a.Stamina<a.StaminaCapacity,"Player one left trigger holds only its own stamina guard");
                PreparePadCombat(game);PositionSpecialPair(game,5,game.BalanceMode?1.4f:4);padStates[0]=new GamepadState().WithButton(GamepadButton.North);SetStage(162);
            } else if (stage == 162 && SimElapsed > .7f) {
                Check(a.SpecialReady<1 && b.Health<b.maxHealth,"Player one north button instantly fires its own ranged attack");
                PreparePadCombat(game);PositionSpecialPair(game,6,game.BalanceMode?1.4f:4);
                SessionState.SetFloat("Woodland.BackstepStartX",a.Body.position.x);
                padStates[0]=padStates[1]=new GamepadState().WithButton(GamepadButton.LeftShoulder);SetStage(97);
            } else if (stage == 97 && SimElapsed > .08f && ((a.BackstepReady<1 && b.BackstepReady<1) || Elapsed>2)) {
                string diagnostic="elapsed="+SimElapsed.ToString("F3")+", age="+(.9f*a.BackstepReady).ToString("F3")+", stamina="+a.Stamina+", velocity="+a.Body.linearVelocity;
                Check(a.BackstepReady<1 && a.Stamina==a.StaminaCapacity-12,"Controller left shoulder starts backdash and spends exactly twelve stamina ("+diagnostic+")");
                Check(a.Body.linearVelocity.x < -6,"Controller left shoulder launches backward at its intended speed ("+diagnostic+")");
                Check(b.BackstepReady<1 && b.Stamina==b.StaminaCapacity-12 && b.Body.linearVelocity.x>6,"Player two left shoulder backdashes in its own opposite facing direction");
                PreparePadCombat(game);PositionSpecialPair(game,6,game.BalanceMode?1.4f:4);padStates[0]=padStates[1]=new GamepadState().WithButton(GamepadButton.RightShoulder);SetStage(158);
            } else if (stage == 158 && SimElapsed > .08f && ((a.DashReady<1 && b.DashReady<1) || Elapsed>2)) {
                Check(a.DashReady<1 && b.DashReady<1 && a.Stamina==a.StaminaCapacity-10 && b.Stamina==b.StaminaCapacity-10 && a.Body.linearVelocity.x>7 && b.Body.linearVelocity.x< -7,"Both right shoulders dash forward in each fighter's own direction with the correct cost");
                padStates[0]=padStates[1]=new GamepadState();
                Check(game.hud.pauseButtons.Length == 5 && game.hud.pauseButtons.All(button => button != null), "All five pause menu buttons are wired");
                game.TogglePause();game.hud.RefreshMenu();
                Check(game.hud.pauseMenu.activeSelf, "Pausing displays the interactive pause menu");
                game.hud.pauseButtons[0].onClick.Invoke();
                Check(!game.Paused && game.Screen == WoodlandGame.MenuScreen.Match, "Pause Resume button restores gameplay");
                game.TogglePause();game.hud.pauseButtons[2].onClick.Invoke();
                Check(!game.Paused && !game.playing && game.Screen != WoodlandGame.MenuScreen.Match, "Pause Change Game button safely leaves combat for configuration");
                game.ReturnToMenu();game.hud.modeButtons[2].onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Settings && !game.playing, "Settings main menu button opens settings without starting gameplay");
                ValidateSettings(game);game.hud.settingsBackButton.onClick.Invoke();
                Check(game.Screen == WoodlandGame.MenuScreen.Title, "Settings Back button returns to title");
                SelectMenuRoster(game, false, false, 3);game.hud.startButton.onClick.Invoke();SetStage(100);
            } else if (stage == 100 && game.playing && SimElapsed > 2) {
                Check(game.plankArt.sprite==game.PvpBases[3] && game.arenaPlatforms.All(platform=>platform.GetComponent<SpriteRenderer>().sprite==game.PvpLedges[3]),"Arena PvP uses the fourth map's dedicated base and ledge artwork");
                Check(Mathf.Abs(game.plankArt.bounds.size.x-14)<.01f && game.arenaPlatforms.All(platform=>Mathf.Abs(platform.GetComponent<SpriteRenderer>().bounds.size.x-4.2f)<.01f),"Arena PvP base is fourteen world units and its ledges are four-point-two units wide");
                Check(!game.BalanceMode && game.ArenaIndex == 3 && game.arenaPlatforms.Length == 3 && game.arenaPlatforms.All(platform => platform != null && platform.gameObject.activeInHierarchy), "Arena PvP starts the fourth map with all three fixed platforms active");
                Check(game.plank.bodyType == RigidbodyType2D.Kinematic && !game.plank.GetComponent<HingeJoint2D>().enabled && !game.sphereArt.gameObject.activeInHierarchy, "Arena PvP disables the balancing hinge and hides its central ball");
                Check(game.arenaPlatforms.All(platform => game.IsGroundBody(platform)), "All Arena PvP platforms are accepted by player ground detection");
                var platform = game.arenaPlatforms[0];var box = platform.GetComponent<BoxCollider2D>();
                Check(box != null, "Arena PvP base platform has an editable walking collider");
                a.ResetScout();b.ResetScout();b.Body.simulated=false;a.inputEnabled=false;
                float top=platform.transform.TransformPoint(box.offset+Vector2.up*box.size.y*.5f).y;
                Vector2 point=new Vector2(platform.position.x,top+.625f);a.Body.position=point;a.transform.position=point;a.Body.gravityScale=1;a.Body.simulated=true;Physics2D.SyncTransforms();SetStage(101);
            } else if (stage == 101 && SimElapsed > .25f) {
                Check(a.Grounded, "Player settles on the Arena PvP platform as valid ground");
                SessionState.SetFloat("Woodland.PvpJumpY",a.Body.position.y);
                Check(a.TryJump() && a.AirJumpsRemaining==1, "Arena PvP ground jump preserves its aerial jump budget");SetStage(102);
            } else if (stage == 102 && SimElapsed > .12f) {
                Check(a.Body.position.y>SessionState.GetFloat("Woodland.PvpJumpY",0)+.3f, "Arena PvP ground jump launches above its fixed platform");
                Capture("Documentation/arena-pvp-1280-v7.png");
                SessionState.SetInt("Woodland.RangedCharacter",0);PrepareSpecialCase(game,0,5);
                Check(a.TrySpecial(),"Fern starts an instant five-unit ranged attack");SetStage(103);
            } else if (stage == 103 && SimElapsed > .7f) {
                int skill=SessionState.GetInt("Woodland.RangedCharacter",0);float[] damage={15.2f,16.8f,13.6f,17.6f};
                Check(Mathf.Approximately(b.maxHealth-b.Health,damage[skill]),game.CharacterNames[skill]+": ranged projectile reaches five units and connects exactly once");
                if(skill==0)Check(b.Rooted && b.Slowed,"Fern ranged contact roots and slows its rival");
                if(skill==1)Check(!b.Rooted && !b.Slowed,"Ember ranged contact applies its distinct direct flame damage");
                if(skill==2)Check(!b.Rooted && b.Slowed,"Luna ranged contact slows without rooting");
                if(skill==3)Check(b.Body.linearVelocity.y>4,"Bram ranged stone contact adds its distinct upward launch");
                Capture("Documentation/ranged-"+game.CharacterNames[skill].ToLowerInvariant()+"-1280-v7.png");
                if(skill<3){SessionState.SetInt("Woodland.RangedCharacter",skill+1);PrepareSpecialCase(game,skill+1,5);Check(a.TrySpecial(),game.CharacterNames[skill+1]+": instant ranged cast starts");SetStage(103);}
                else{PrepareSpecialCase(game,0,5);b.inputEnabled=true;padStates[1]=new GamepadState{leftTrigger=1};Check(a.TrySpecial(),"Ranged guard test starts a visible projectile");SetStage(104);}
            } else if (stage == 104 && SimElapsed > .7f) {
                Check(b.Health==b.maxHealth && b.Stamina<80,"A held front guard absorbs a ranged contact and spends stamina instead of health");
                PrepareSpecialCase(game,0,8.2f);Check(a.TrySpecial(),"Out-of-range ranged cast still starts normally");SetStage(105);
            } else if (stage == 105 && SimElapsed > 1) {
                Check(b.Health==b.maxHealth && a.SpecialReady<1,"A projectile beyond its fixed reach misses while retaining its cooldown");
                PrepareUltimateCase(game,0,0,25.01f,1.2f);SetStage(130);
            } else if (stage == 130 && SimElapsed > .35f) {
                Check(a.Health>25 && a.Health<25.02f && !a.UltimateReady && !a.TryUltimate(),"Ultimate remains locked just above the twenty-five-health boundary");
                a.inputEnabled=true;padStates[0]=new GamepadState{leftTrigger=1,rightTrigger=1};SetStage(153);
            } else if (stage == 153 && SimElapsed > .12f) {
                Check(!a.UltimateUsed && !a.Blocking && a.Stamina==a.StaminaCapacity,"An unavailable dual-trigger chord neither fires an ultimate nor falls through to ordinary guard");
                PrepareUltimateCase(game,0,0,25,1.2f);SetStage(131);
            } else if (stage == 131 && SimElapsed > .35f) {
                Check(a.Health==25 && a.UltimateReady && !a.UltimateUsed,"Ultimate unlocks at exactly twenty-five health");
                game.hud.Refresh();Check(game.hud.ultimateLabels[0].text.Contains(a.UltimateName.ToUpper()) && game.hud.ultimateLabels[0].text.Contains("TRIGGER"),"Ready ultimate label names the character's move and current dual-trigger binding");
                a.inputEnabled=true;SessionState.SetInt("Woodland.CinematicStart",cinematicStarts);SessionState.SetInt("Woodland.CinematicStop",cinematicStops);
                SessionState.SetInt("Woodland.EffectsCinematicStart",WoodlandEffects.Instance.UltimateCinematicsStarted);SessionState.SetInt("Woodland.EffectsImpactStart",WoodlandEffects.Instance.UltimateImpactsPlayed);
                padStates[0]=new GamepadState{leftTrigger=1,rightTrigger=1};SetStage(132);
            } else if (stage == 132 && SimElapsed > .2f) {
                Check(a.UltimateUsed && a.UltimateActive && a.AnimationState=="Ultimate" && b.Health==b.maxHealth,"Player one dual-trigger chord starts a telegraphed ultimate before any damage");
                Check(WoodlandEffects.Instance.CinematicActive && WoodlandEffects.Instance.UltimateCinematicsStarted==SessionState.GetInt("Woodland.EffectsCinematicStart",0)+1,"Accepted ultimate starts the actual camera presentation once");
                Check(!a.Blocking && a.Stamina==a.StaminaCapacity && !a.TryJump() && !a.TrySpecial(),"Ultimate chord overrides guard and ordinary actions without an energy charge");
                game.TogglePause();SetStage(133);
            } else if (stage == 133 && Elapsed > .25f) {
                Check(game.Paused && a.UltimateActive && b.Health==b.maxHealth,"Pausing freezes an ultimate during its counterplay telegraph");
                Check(!WoodlandEffects.Instance.CinematicActive && Mathf.Abs(Camera.main.orthographicSize-SessionState.GetFloat("Woodland.UltimateCameraZoom",5.3f))<.01f,"Pause releases cinematic focus and restores the normal arena zoom");
                game.TogglePause();SetStage(134);
            } else if (stage == 134 && SimElapsed > .8f) {
                Check(b.Health==b.maxHealth-30 && b.Rooted && b.Slowed,"Fern ultimate deals thirty damage and applies its longer binding effect");
                Check(a.UltimateUsed && !a.UltimateActive && !a.TryUltimate(),"Ultimate ends and remains unavailable after its one round use");
                Check(cinematicStarts==SessionState.GetInt("Woodland.CinematicStart",0)+1 && cinematicStops==SessionState.GetInt("Woodland.CinematicStop",0)+1,"Holding both triggers starts exactly one cinematic and closes it once");
                Check(WoodlandEffects.Instance.UltimateImpactsPlayed==SessionState.GetInt("Woodland.EffectsImpactStart",0)+1,"Ultimate impact presentation is triggered by one confirmed hit");CheckCameraRestored();
                Capture("Documentation/ultimate-fern-1280-v7.png");SetStage(135);
            } else if (stage == 135 && SimElapsed > .25f) {
                PrepareUltimateCase(game,1,1,25,1.2f);SetStage(136);
            } else if (stage == 136 && SimElapsed > .35f) {
                Check(b.UltimateReady,"Player two reaches the same low-health ultimate threshold");
                b.inputEnabled=true;padStates[1]=new GamepadState{leftTrigger=1,rightTrigger=1};SetStage(137);
            } else if (stage == 137 && SimElapsed > .62f) {
                Check(b.UltimateUsed && b.UltimateActive && a.Health==a.maxHealth-10,"Player two dual triggers start Ember's first ten-damage ultimate contact");
                PositionSpecialPair(game,1.2f,4);SetStage(138);
            } else if (stage == 138 && SimElapsed > .26f) {
                Check(a.Health==a.maxHealth-20,"Ember ultimate connects its second separated contact");PositionSpecialPair(game,1.2f,4);SetStage(139);
            } else if (stage == 139 && SimElapsed > .26f) {
                Check(a.Health==a.maxHealth-30,"Ember ultimate finishes at three contacts and thirty total damage");Capture("Documentation/ultimate-ember-1280-v7.png");SetStage(140);
            } else if (stage == 140 && SimElapsed > .35f) {
                Check(b.UltimateUsed && !b.UltimateActive && cinematicStarts==cinematicStops,"Player two held ultimate chord cannot retrigger and finishes its cinematic");
                CheckCameraRestored();
                PrepareUltimateCase(game,2,0,25,4.8f);SetStage(141);
            } else if (stage == 141 && SimElapsed > .35f) {
                Check(a.TryUltimate(),"Luna starts its own long-range ultimate wave");SetStage(142);
            } else if (stage == 142 && SimElapsed > 1.05f) {
                Check(b.Health==b.maxHealth-26 && b.Rooted && b.Slowed,"Luna ultimate travels beyond melee reach, deals twenty-six damage and freezes its rival");Capture("Documentation/ultimate-luna-1280-v7.png");SetStage(143);
            } else if (stage == 143 && SimElapsed > .35f) {
                Check(!a.UltimateActive && cinematicStarts==cinematicStops,"Luna ultimate closes its cinematic after its wave");
                CheckCameraRestored();
                PrepareUltimateCase(game,3,0,25,1.2f);PlaceOnPvpBase(game,true);SetStage(144);
            } else if (stage == 144 && SimElapsed > .35f) {
                Check(a.Grounded && a.Facing>0 && b.Body.position.x<a.Body.position.x && a.TryUltimate(),"Grounded Bram starts its radial ultimate with the rival behind him");SessionState.SetFloat("Woodland.BramLaunchY",b.Body.position.y);SetStage(145);
            } else if (stage == 145 && SimElapsed > .62f) {
                string diagnostic="elapsed="+SimElapsed.ToString("F3")+", health="+b.Health+", velocity="+b.Body.linearVelocity+", deltaY="+(b.Body.position.y-SessionState.GetFloat("Woodland.BramLaunchY",0)).ToString("F3")+", casterGrounded="+a.Grounded+", rivalGrounded="+b.Grounded+", casterPosition="+a.Body.position+", rivalPosition="+b.Body.position+", active="+a.UltimateActive+", timeScale="+Time.timeScale;
                Debug.Log("BRAM_ULTIMATE_DIAGNOSTIC: "+diagnostic);
                if(b.Health!=b.maxHealth-35 || b.Body.linearVelocity.y<=3)Capture("Documentation/ultimate-bram-diagnostic-1280-v7.png");
                Check(b.Health==b.maxHealth-35,"Bram ultimate hits a rival behind him for capped thirty-five damage ("+diagnostic+")");
                Check(b.Body.linearVelocity.y>3,"Bram ultimate adds its intended upward launch ("+diagnostic+")");Capture("Documentation/ultimate-bram-1280-v7.png");SetStage(146);
            } else if (stage == 146 && SimElapsed > .35f) {
                Check(!a.UltimateActive && cinematicStarts==cinematicStops,"Bram ultimate closes its cinematic normally");
                CheckCameraRestored();
                PrepareUltimateCase(game,0,0,25,1.2f);SetStage(147);
            } else if (stage == 147 && SimElapsed > .35f) {
                Check(a.TryUltimate(),"Ultimate miss test commits its one-round use before impact");PositionSpecialPair(game,9,4);SetStage(148);
            } else if (stage == 148 && SimElapsed > .95f) {
                Check(b.Health==b.maxHealth && a.UltimateUsed && !a.UltimateActive && !a.TryUltimate(),"Moving out during the telegraph avoids damage without refunding ultimate use");
                CheckCameraRestored();
                PrepareUltimateCase(game,0,0,25,1.2f);b.inputEnabled=true;padStates[1]=new GamepadState{leftTrigger=1};SetStage(164);
            } else if (stage == 164 && SimElapsed > .35f) {
                Check(b.Blocking && a.TryUltimate(),"An established guard can face an incoming ultimate telegraph");SessionState.SetInt("Woodland.GuardedUltimateImpacts",WoodlandEffects.Instance.UltimateImpactsPlayed);SetStage(165);
            } else if (stage == 165 && SimElapsed > .95f) {
                Check(b.Health==b.maxHealth && b.Stamina<b.StaminaCapacity && a.UltimateUsed && !a.UltimateActive,"Front guard absorbs ultimate damage through stamina without refunding the attacker's use");
                Check(WoodlandEffects.Instance.UltimateImpactsPlayed==SessionState.GetInt("Woodland.GuardedUltimateImpacts",0),"A guarded ultimate produces no false confirmed-hit cinematic impact");CheckCameraRestored();Capture("Documentation/ultimate-guard-1280-v7.png");
                PrepareUltimateCase(game,0,0,25,1.2f);SetStage(149);
            } else if (stage == 149 && SimElapsed > .35f) {
                Check(a.TryUltimate() && a.ReceiveHit(b,-1,false,.1f),"An incoming hit can interrupt an accepted ultimate telegraph");
                Check(a.UltimateUsed && !a.UltimateActive && cinematicStarts==cinematicStops,"Interrupted ultimate stays spent and immediately releases its cinematic");SetStage(150);
            } else if (stage == 150 && SimElapsed > .7f) {
                Check(b.Health==b.maxHealth,"An interrupted ultimate never delivers its delayed damage");
                CheckCameraRestored();
                PrepareUltimateCase(game,0,0,25,1.2f);SetStage(151);
            } else if (stage == 151 && SimElapsed > .35f) {
                Check(a.TryUltimate(),"Match-ending cleanup test starts an ultimate");game.playing=false;SetStage(152);
            } else if (stage == 152 && SimElapsed > .35f) {
                Check(!a.UltimateActive && cinematicStarts==cinematicStops,"Ending gameplay cancels the ultimate and releases its cinematic");
                CheckCameraRestored();
                a.ResetScout();Check(!a.UltimateUsed && !a.UltimateActive && a.Health==a.maxHealth && a.AnimationState=="Idle","Round reset clears ultimate use and restores health and idle");
                game.playing=true;PrepareUltimateCase(game,0,0,25,1.2f);SetStage(163);
            } else if (stage == 163 && SimElapsed > .35f) {
                Check(a.TryUltimate() && a.UltimateActive,"Reset cleanup test begins an active ultimate presentation");
                a.ResetScout();Check(!a.UltimateUsed && !a.UltimateActive && a.Health==a.maxHealth && a.AnimationState=="Idle" && cinematicStarts==cinematicStops,"Reset during the telegraph clears its ultimate, use flag and cinematic immediately");CheckCameraRestored();game.playing=false;
                for(int character=0;character<4;character++){
                    a.SetCharacter(character,game.CharacterNames[character],game.CharacterAnimationSets[character],game.CharacterControllers[character],game.CharacterColors[character]);a.PlayVictory();
                    Check(a.AnimationState=="Victory",game.CharacterNames[character]+": winner selects its dedicated victory state");
                }
                a.ResetScout();Check(a.AnimationState=="Idle","Reset clears the victory state");
                Complete(true,"V7_RANGED_ULTIMATES_COMBOS_ROSTER_RULES_SETTINGS_AI_CONTROLLERS_VALIDATED");
            }
        } catch (Exception error) { Complete(false, error.ToString()); }
    }
    static void ValidateAudio(WoodlandGame game)
    {
        var effects = WoodlandEffects.Instance;
        Check(effects != null && effects.soundBank != null && effects.soundBank.Length == 13, "Thirteen action sound clips are wired into the effects bank");
        float volume = effects.SfxVolume;bool muted = effects.Muted;bool paused = game.Paused;
        try {
            if (paused) game.TogglePause();
            if (effects.Muted) effects.ToggleMute();
            effects.SetVolume(-1);Check(effects.SfxVolume == 0, "Sound volume clamps negative input to zero");
            Check(!effects.PlayCue(0), "Zero volume denies playback");
            effects.SetVolume(2);Check(effects.SfxVolume == 1, "Sound volume clamps excessive input to one");
            int sounds = effects.SoundsPlayed;
            for (int i = 0; i < effects.soundBank.Length; i++) {
                var clip = effects.soundBank[i];
                Check(clip != null && clip.loadState == AudioDataLoadState.Loaded && clip.length > .01f && clip.samples > 0 && clip.channels > 0, "Sound " + i + ": clip is loaded with positive duration and sample count");
                var samples = new float[Mathf.Min(clip.samples * clip.channels, 8192)];
                Check(clip.GetData(samples, 0) && samples.Sum(sample => (double)sample * sample) / samples.Length > 1e-10, "Sound " + i + ": decoded waveform contains audible data");
                Check(effects.PlayCue(i,.02f), "Sound " + i + ": playback accepts its valid cue");
            }
            Check(effects.SoundsPlayed == sounds + 13, "Accepted sound cues increment the actual playback counter thirteen times");
            var sources = effects.GetComponents<AudioSource>();
            Check(sources.Length == 8 && sources.Any(source => source.clip != null && source.isPlaying), "Eight pooled audio sources exist and at least one cue is playing");
            Check(!effects.PlayCue(-1) && !effects.PlayCue(13), "Invalid sound indices are rejected");
            effects.ToggleMute();sounds = effects.SoundsPlayed;
            Check(effects.Muted && !effects.PlayCue(0) && effects.SoundsPlayed == sounds, "Mute denies playback without advancing the sound counter");
            effects.ToggleMute();game.TogglePause();
            Check(!effects.PlayCue(0) && effects.SoundsPlayed == sounds, "Pause denies new sound cues");
            game.TogglePause();
            Check(effects.PlayCue(0) && effects.SoundsPlayed == sounds + 1, "Resuming permits sound playback again");
        } finally {
            if (game.Paused != paused) game.TogglePause();
            if (effects.Muted != muted) effects.ToggleMute();
            effects.SetVolume(volume,false);
        }
    }
    static void ValidateHudCorners(WoodlandGame game)
    {
        Canvas.ForceUpdateCanvases();
        var canvas = (RectTransform)game.hud.transform;
        for (int i = 0; i < game.hud.fighterPlates.Length; i++) {
            var corners = new Vector3[4];game.hud.fighterPlates[i].GetWorldCorners(corners);
            var points = corners.Select(point => (Vector2)canvas.InverseTransformPoint(point)).ToArray();
            Check(points.All(point => point.x >= canvas.rect.xMin - 1 && point.x <= canvas.rect.xMax + 1 && point.y >= canvas.rect.yMin - 1 && point.y <= canvas.rect.yMax + 1), "Player " + (i + 1) + ": entire HUD plate fits inside the canvas");
            var diamond=(RectTransform)game.hud.actionDiscs[i*4].transform.parent;var diamondCorners=new Vector3[4];diamond.GetWorldCorners(diamondCorners);
            Check(canvas.rect.yMax-points.Max(point=>point.y)<40 && (i==0?points.Min(point=>point.x)-canvas.rect.xMin:canvas.rect.xMax-points.Max(point=>point.x))<40,"Player "+(i+1)+": floating fighter panel occupies its top screen corner");
            var bottom=diamondCorners.Select(point=>(Vector2)canvas.InverseTransformPoint(point)).ToArray();
            Check(bottom.All(point=>canvas.rect.Contains(point)) && bottom.Min(point=>point.y)-canvas.rect.yMin<50 && (i==0?bottom.Min(point=>point.x)-canvas.rect.xMin:canvas.rect.xMax-bottom.Max(point=>point.x))<50,"Player "+(i+1)+": entire action diamond fits its bottom screen corner");
            Check(i==0?points.Max(point=>point.x)<canvas.rect.center.x:points.Min(point=>point.x)>canvas.rect.center.x,"Player "+(i+1)+": fighter panel stays on its own half of the screen");
        }
    }
    static void ValidateHudMeters(WoodlandGame game)
    {
        game.hud.Refresh();Canvas.ForceUpdateCanvases();
        Check(game.hud.soundText==null,"No sound-percentage popup is created on the combat HUD");
        Check(game.hud.transform.Find("Match HUD/Versus Emblem")!=null,"Generated VS artwork is present between the two HUDs");
        for(int player=0;player<2;player++){
            var diamond=(RectTransform)game.hud.actionDiscs[player*4].transform.parent;
            Check(game.hud.fighterPlates[player].GetComponent<Image>()==null,"Player "+(player+1)+": floating fighter group has no opaque backing slab");
            var centers=Enumerable.Range(0,4).Select(n=>{var r=game.hud.actionDiscs[player*4+n].rectTransform;return r.anchoredPosition+new Vector2(r.rect.width*.5f,-r.rect.height*.5f);}).ToArray();
            foreach(var image in game.hud.actionDiscs.Skip(player*4).Take(4)){
                var points=new Vector3[4];image.rectTransform.GetWorldCorners(points);var canvas=(RectTransform)game.hud.transform;
                Vector3 low=canvas.InverseTransformPoint(points[0]),high=canvas.InverseTransformPoint(points[2]);
                Check(high.x-low.x>=72 && high.y-low.y>=72,"Player "+(player+1)+": action circle keeps a physical target of at least seventy-two canvas pixels");
            }
            Check(Mathf.Approximately(centers[0].x,centers[3].x)&&Mathf.Approximately(centers[1].y,centers[2].y)&&Mathf.Approximately(Vector2.Distance(centers[0],centers[3]),Vector2.Distance(centers[1],centers[2])),"Player "+(player+1)+": four circles occupy the equal diagonal endpoints of a diamond");
        }
        Check(game.hud.fighterPortraits.Length==2 && game.hud.fighterPortraits.All(image=>image!=null && image.sprite!=null),"Both fighter portraits are wired with real character sprites");
        Check(game.hud.actionButtons.Length==8 && game.hud.actionButtons.All(button=>button!=null && button.interactable && button.targetGraphic && button.targetGraphic.raycastTarget),"All eight illustrated actions have live mouse targets that can receive accepted and denied clicks");
        foreach(var portrait in game.hud.fighterPortraits)Check(portrait.GetComponentInParent<Mask>()!=null,"Fighter portrait uses a circular clipping mask");
        for(int i=0;i<2;i++){
            var health=game.hud.healthFill[i].rectTransform;var stamina=game.hud.shieldFill[i].rectTransform;
            var healthNumber=game.hud.healthText[i].rectTransform;var staminaNumber=game.hud.shieldText[i].rectTransform;
            Check(game.hud.healthText[i].text.Replace(" ","")=="100/100" && game.hud.shieldText[i].text.Replace(" ","")=="100/100","Player "+(i+1)+": health and stamina show readable whole-number current/max values");
            Check(health.rect.Contains(health.InverseTransformPoint(healthNumber.TransformPoint(healthNumber.rect.center))) && stamina.rect.Contains(stamina.InverseTransformPoint(staminaNumber.TransformPoint(staminaNumber.rect.center))),"Player "+(i+1)+": each numeric value is centered inside its own meter");
            Check(health.rect.height>stamina.rect.height,"Player "+(i+1)+": health meter is visually larger than the stamina meter");
            var portrait=game.hud.fighterPortraits[i].rectTransform;var name=game.hud.names[i].rectTransform;
            Vector3 portraitCenter=portrait.TransformPoint(portrait.rect.center);
            Check((i==0?portraitCenter.x<health.TransformPoint(health.rect.center).x:portraitCenter.x>health.TransformPoint(health.rect.center).x) && name.TransformPoint(name.rect.center).y<portraitCenter.y,"Player "+(i+1)+": portrait mirrors toward its outside screen edge with its name below");
        }
    }
    static void ValidateCharacterActionCovers(WoodlandGame game)
    {
        var hud=game.hud;
        Check(hud.characterActionCovers.Length==16 && hud.characterActionCovers.All(sprite=>sprite!=null),"All sixteen character-specific action covers are loaded");
        Check(hud.characterActionCovers.Select(sprite=>AssetDatabase.GetAssetPath(sprite)).Distinct().Count()==16,"Each character/action pair references a separate artwork asset");
        int[] previous=(int[])game.CharacterChoices.Clone();
        string[] badges=hud.actionGlyphs.Select(label=>label.text).ToArray();
        string[] actions={"Jump","Punch","Kick","Special"};
        try {
            for(int character=0;character<4;character++) {
                for(int player=0;player<2;player++) {
                    int selected=(character+player)%4;game.CharacterChoices[player]=selected;
                    game.players[player].SetCharacter(selected,game.CharacterNames[selected],game.CharacterAnimationSets[selected],game.CharacterControllers[selected],game.CharacterColors[selected]);
                }
                hud.Refresh();Canvas.ForceUpdateCanvases();
                for(int player=0;player<2;player++)for(int action=0;action<4;action++) {
                    int selected=game.players[player].CharacterIndex;int slot=player*4+action;
                    string expected="Assets/Art/UI/Actions/"+game.CharacterNames[selected]+"/"+actions[action]+".png";
                    Check(AssetDatabase.GetAssetPath(hud.actionCovers[slot].sprite)==expected,"P"+(player+1)+" "+game.CharacterNames[selected]+" "+actions[action]+": live HUD shows its own artwork");
                    Check(hud.actionGlyphs[slot].text==badges[slot],"P"+(player+1)+" "+game.CharacterNames[selected]+" "+actions[action]+": changing character preserves its input badge");
                }
                Capture("Documentation/character-hud-"+game.CharacterNames[character].ToLowerInvariant()+"-v7.png");
            }
        } finally {
            for(int player=0;player<2;player++) {
                int selected=previous[player];game.CharacterChoices[player]=selected;
                game.players[player].SetCharacter(selected,game.CharacterNames[selected],game.CharacterAnimationSets[selected],game.CharacterControllers[selected],game.CharacterColors[selected]);
            }
            hud.Refresh();
        }
    }
    static void PrepareSpecialCase(WoodlandGame game,int skill,float separation)
    {
        heldKey=Key.None;padStates[0]=padStates[1]=new GamepadState();
        int[] choices={skill,skill==0?1:0};
        for(int i=0;i<2;i++){
            var scout=game.players[i];int index=choices[i];
            scout.SetCharacter(index,game.CharacterNames[index],game.CharacterAnimationSets[index],game.CharacterControllers[index],game.CharacterColors[index]);
            scout.ResetScout();scout.inputEnabled=false;scout.aiControlled=false;scout.Body.gravityScale=0;scout.Body.simulated=true;
        }
        PositionSpecialPair(game,separation,game.BalanceMode?1.4f:4);
    }
    static void PrepareUltimateCase(WoodlandGame game,int skill,int caster,float health,float separation)
    {
        PrepareSpecialCase(game,0,separation);
        for(int i=0;i<2;i++){
            int selected=i==caster?skill:(skill==0?1:0);var scout=game.players[i];
            scout.SetCharacter(selected,game.CharacterNames[selected],game.CharacterAnimationSets[selected],game.CharacterControllers[selected],game.CharacterColors[selected]);scout.ResetScout();scout.inputEnabled=false;scout.aiControlled=false;scout.Body.simulated=true;scout.Body.gravityScale=0;
        }
        var actor=game.players[caster];var rival=game.players[1-caster];
        Check(actor.ReceiveHit(rival,caster==0?-1:1,true,(actor.maxHealth-health+.0001f)/(rival.punchDamage*Scout.DamageScale)),"Ultimate health fixture applies real incoming damage");
        PositionSpecialPair(game,separation,4);
        SessionState.SetFloat("Woodland.UltimateCameraZoom",Camera.main.orthographicSize);
        SessionState.SetFloat("Woodland.UltimateCameraX",Camera.main.transform.position.x);SessionState.SetFloat("Woodland.UltimateCameraY",Camera.main.transform.position.y);
    }
    static void PlaceOnPvpBase(WoodlandGame game,bool rivalBehind)
    {
        var box=game.plank.GetComponent<BoxCollider2D>();float top=game.plank.transform.TransformPoint(box.offset+Vector2.up*box.size.y*.5f).y;
        for(int i=0;i<2;i++){
            var scout=game.players[i];float x=i==0?0:(rivalBehind?-1.2f:1.2f);
            scout.Body.position=new Vector2(game.plank.position.x+x,top+1.1875f);scout.transform.position=scout.Body.position;scout.Body.linearVelocity=Vector2.zero;scout.Body.gravityScale=1;scout.Body.simulated=true;
        }
        Physics2D.SyncTransforms();
    }
    static void TrackUltimate(Scout scout,bool active){if(active)cinematicStarts++;else cinematicStops++;}
    static void CheckCameraRestored()
    {
        var camera=Camera.main;Vector2 home=new Vector2(SessionState.GetFloat("Woodland.UltimateCameraX",0),SessionState.GetFloat("Woodland.UltimateCameraY",1.4f));
        Check(!WoodlandEffects.Instance.CinematicActive && Mathf.Abs(camera.orthographicSize-SessionState.GetFloat("Woodland.UltimateCameraZoom",5.3f))<.01f && Vector2.Distance(camera.transform.position,home)<.03f && Mathf.Approximately(Time.timeScale,1),"Finished or cancelled ultimate restores camera focus, arena zoom and normal game time");
    }
    static void PositionSpecialPair(WoodlandGame game,float separation,float height)
    {
        for(int i=0;i<2;i++){
            var scout=game.players[i];scout.Body.position=new Vector2((i==0?-1:1)*separation*.5f,height);scout.transform.position=scout.Body.position;scout.Body.linearVelocity=Vector2.zero;
        }
        Physics2D.SyncTransforms();
    }
    static void SelectMenuRoster(WoodlandGame game, bool solo, bool balance, int arena)
    {
        game.hud.modeButtons[solo ? 0 : 1].onClick.Invoke();
        Check(game.Screen == WoodlandGame.MenuScreen.Rules && game.SoloMode == solo && !game.playing, "Mode button opens the rules screen with the selected player mode");
        game.hud.ruleButtons[balance ? 1 : 0].onClick.Invoke();
        Check(game.Screen == WoodlandGame.MenuScreen.Characters && game.BalanceMode == balance, "Rule button opens character selection with the selected arena rules");
        if(!SessionState.GetBool("Woodland.RosterCaptured",false)){game.hud.RefreshMenu();Capture("Documentation/roster-menu-1280-v7.png");SessionState.SetBool("Woodland.RosterCaptured",true);}
        game.hud.characterButtons[0].onClick.Invoke();game.hud.confirmCharacterButton.onClick.Invoke();
        if (!solo) {
            Check(game.Screen == WoodlandGame.MenuScreen.Characters && game.ChoosingPlayer == 1, "Local roster confirms player one before choosing player two");
            game.hud.characterButtons[1].onClick.Invoke();game.hud.confirmCharacterButton.onClick.Invoke();
        }
        Check(game.Screen == WoodlandGame.MenuScreen.Maps && game.CharacterChoices[0] == 0 && game.CharacterChoices[1] == 1, "Roster selection reaches maps with distinct Fern and Ember choices");
        game.hud.mapButtons[arena].onClick.Invoke();
    }
    static bool WideAction(string state)=>state=="Dash" || state=="Backstep" || state=="Victory" || state=="Ultimate";
    static void ValidateRosterAssets()
    {
        var previous = new System.Collections.Generic.HashSet<string>();
        foreach (string name in new[] { "Fern", "Ember", "Luna", "Bram" }) {
            string directory = "Assets/Animations/" + name + "/";
            var catalog = AssetDatabase.LoadAssetAtPath<ScoutAnimations>(directory + "Frames.asset");
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(directory + name + ".controller");
            Check(catalog != null && controller != null && controller.animationClips.Length > 0, name + ": independent sprite catalog and editable controller exist");
            var own = new System.Collections.Generic.HashSet<string>();
            foreach (string state in new[] { "Walk", "Jump", "Hurt", "Punch", "Knockout", "Block", "DoubleJump", "Kick", "Dash", "Backstep", "Victory", "Ultimate" }) {
                var frames = catalog.Get(state);
                Check(frames.Length == 10 && frames.All(frame => frame != null && frame.rect.width == (WideAction(state)?384:256) && frame.rect.height == 256 && AssetDatabase.GetAssetPath(frame).StartsWith("Assets/Art/Characters/" + name + "/", StringComparison.Ordinal)), name + "/" + state + ": ten aligned sprites belong exclusively to this character");
                Check(frames.Select(frame=>AssetDatabase.GetAssetPath(frame)).Distinct().Count()==10 && frames.All(frame=>Mathf.Approximately(frame.pixelsPerUnit,160) && Mathf.Abs(frame.pivot.x-frame.rect.width*.5f)<.1f && Mathf.Abs(frame.pivot.y-24)<.1f),name+"/"+state+": independent frames keep the common body scale and foot pivot");
                foreach (var frame in frames) own.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(frame)));
            }
            var special = catalog.Get("Special");
            Check(special.Length == 10 && special.All(frame => frame != null && frame.rect.height == 256 && frame.rect.width == 384 && AssetDatabase.GetAssetPath(frame).StartsWith("Assets/Art/Characters/" + name + "/", StringComparison.Ordinal)), name + "/Special: ten own sprites preserve supported full-body proportions");
            foreach (var frame in special) own.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(frame)));
            Check(!own.Overlaps(previous), name + ": source sprite GUIDs are not reused from another roster character");
            Check(controller.animationClips.All(clip => AssetDatabase.GetAssetPath(clip).StartsWith(directory, StringComparison.Ordinal)), name + ": all controller clips belong to its own animation folder");
            Check(controller.animationClips.All(clip => AnimationUtility.GetObjectReferenceCurveBindings(clip).Where(binding => binding.type == typeof(SpriteRenderer)).SelectMany(binding => AnimationUtility.GetObjectReferenceCurve(clip, binding)).Select(key => key.value as Sprite).Where(sprite => sprite != null).All(sprite => AssetDatabase.GetAssetPath(sprite).StartsWith("Assets/Art/Characters/" + name + "/", StringComparison.Ordinal))), name + ": native animation clips never reference another character's sprites");
            previous.UnionWith(own);
        }
    }
    static void ValidateSettings(WoodlandGame game)
    {
        var effects=WoodlandEffects.Instance;float volume=effects.SfxVolume;bool shake=effects.cameraShake;string quality=game.EffectsQualityName;
        try {
            effects.SetVolume(.5f,false);game.StepVolume(-.1f);
            Check(game.SettingsVolume<.5f && game.SettingsVolume>=0,"Settings volume down updates the real bounded audio level");
            float lower=game.SettingsVolume;game.StepVolume(.1f);
            Check(game.SettingsVolume>lower && game.SettingsVolume<=1,"Settings volume up updates the real bounded audio level");
            game.ToggleCameraShake();Check(effects.cameraShake!=shake,"Settings camera shake toggle changes the live effect setting");
            game.ToggleCameraShake();Check(effects.cameraShake==shake,"A second shake toggle restores the original setting");
            game.CycleEffectsQuality();Check(game.EffectsQualityName!=quality,"Settings effect quality cycles to a different preset");
            for(int i=0;i<4 && game.EffectsQualityName!=quality;i++)game.CycleEffectsQuality();
            Check(game.EffectsQualityName==quality,"Effect quality cycling can return to the original preset");
            Capture("Documentation/settings-1280-v7.png");
        } finally {
            effects.SetVolume(volume,false);
            if(effects.cameraShake!=shake)game.ToggleCameraShake();
            for(int i=0;i<4 && game.EffectsQualityName!=quality;i++)game.CycleEffectsQuality();
        }
    }
    static void PrepareAiCase(WoodlandGame game, float separation)
    {
        game.plank.rotation = 0;game.plank.transform.rotation = Quaternion.identity;
        game.plank.bodyType = RigidbodyType2D.Kinematic;game.plank.simulated = true;
        var box = game.plank.GetComponent<BoxCollider2D>();
        float top = game.plank.transform.TransformPoint(box.offset + Vector2.up * box.size.y * .5f).y + box.edgeRadius * game.plank.transform.lossyScale.x;
        for (int i = 0; i < 2; i++) {
            var scout = game.players[i];scout.ResetScout();
            Vector2 point = new Vector2((i == 0 ? -1 : 1) * separation * .5f, top + .625f);
            scout.transform.position = point;scout.Body.position = point;scout.Body.linearVelocity = Vector2.zero;
            scout.Body.gravityScale = 1;scout.Body.simulated = true;scout.inputEnabled = i == 1;
        }
        Physics2D.SyncTransforms();
    }
    static void BeginGamepadCases(WoodlandGame game)
    {
        disabledPads = Gamepad.all.Where(pad => pad.enabled).ToArray();
        foreach (var pad in disabledPads) InputSystem.DisableDevice(pad);
        game.RefreshGamepads();
        var labelPads = new Gamepad[2];
        bool helpWasVisible = game.hud.helpPanel.activeSelf;
        try {
            string[] layouts = { "DualShock4GamepadHID", "XInputController" };
            for (int i = 0; i < layouts.Length; i++) {
                Check(InputSystem.ListLayouts().Contains(layouts[i]), layouts[i] + ": controller layout is available for prompt validation");
                labelPads[i] = InputSystem.AddDevice(layouts[i]) as Gamepad;
                Check(labelPads[i] != null && WoodlandGame.GamepadStyle(labelPads[i]) == (i == 0 ? "PLAYSTATION" : "XBOX"), layouts[i] + ": controller family maps to its expected button labels");
            }
            game.RefreshGamepads();game.hud.helpPanel.SetActive(false);game.hud.Refresh();
            Capture("Documentation/controller-diamond-1280-v7.png");
            game.hud.helpPanel.SetActive(true);game.hud.Refresh();
            Check(game.hud.deviceText[0].text.Contains("PLAYSTATION") && game.hud.deviceText[1].text.Contains("XBOX"), "Assigned PlayStation and Xbox layouts appear on their own player panels");
            Check(game.hud.controlsText[0].text.Contains("CROSS") && game.hud.controlsText[0].text.Contains("SQUARE") && game.hud.controlsText[1].text.ToUpperInvariant().Contains("JUMP") && game.hud.controlsText[1].text.Contains("RB"), "Help displays real PlayStation and Xbox button names for the assigned virtual layouts");
            Capture("Documentation/controller-help-1280-v7.png");
        } finally {
            game.hud.helpPanel.SetActive(helpWasVisible);
            foreach (var pad in labelPads) if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            game.RefreshGamepads();
        }
        testPads[0] = InputSystem.AddDevice<Gamepad>();testPads[1] = InputSystem.AddDevice<Gamepad>();
        Check(WoodlandGame.GamepadStyle(testPads[0]) == "GAMEPAD", "Generic controller receives neutral button labels");
        game.RefreshGamepads();PreparePadCombat(game);
        padStates[0] = new GamepadState { leftStick = Vector2.right * .8f };
    }
    static void PreparePadCombat(WoodlandGame game)
    {
        heldKey = Key.None;padStates[0] = padStates[1] = new GamepadState();
        game.players[0].ResetScout();game.players[1].ResetScout();Arrange(game);
        foreach (var scout in game.players) scout.inputEnabled = true;
        DriveTestInput();
    }
    static void DriveTestInput()
    {
        if (testKeyboard != null) { testKeyboard.MakeCurrent();InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(heldKey)); }
        for (int i = 0; i < testPads.Length; i++) if (testPads[i] != null && testPads[i].added) InputSystem.QueueStateEvent(testPads[i], padStates[i]);
    }
    static void StartEdgeCase(WoodlandGame game, int index)
    {
        var scout = game.players[0];var plank = game.plank;
        scout.ResetScout();game.players[1].Body.simulated = false;
        scout.inputEnabled = true;scout.Body.gravityScale = 1;
        float direction = index % 2 == 0 ? 1 : -1;
        float angle = index == 2 ? -22 : index == 3 ? 22 : 0;
        plank.rotation = angle;plank.transform.rotation = Quaternion.Euler(0, 0, angle);
        plank.linearVelocity = Vector2.zero;plank.angularVelocity = 0;
        var box = plank.GetComponent<BoxCollider2D>();
        Vector2 corner = plank.transform.TransformPoint(box.offset + new Vector2(direction * box.size.x * .5f, box.size.y * .5f));
        bool pressSide = index >= 4;
        Vector2 point = corner + (pressSide ? new Vector2(direction * .25f, -.40f) : new Vector2(-direction * .35f, .68f));
        scout.transform.position = point;scout.Body.position = point;scout.Body.linearVelocity = Vector2.zero;scout.Body.simulated = true;
        SessionState.SetFloat("Woodland.EdgeY", corner.y);
        Key key = (pressSide ? -direction : direction) > 0 ? Key.D : Key.A;
        heldKey = key;DriveTestInput();Physics2D.SyncTransforms();
    }
    static void StartJumpCase(WoodlandGame game, bool driveKeyboardJump = true,int player=0)
    {
        var scout = game.players[player];scout.ResetScout();scout.inputEnabled = true;
        game.plank.rotation = 0;game.plank.transform.rotation = Quaternion.identity;
        var box = game.plank.GetComponent<BoxCollider2D>();
        float top = game.plank.transform.TransformPoint(box.offset + Vector2.up * box.size.y * .5f).y + box.edgeRadius * game.plank.transform.lossyScale.x;
        Vector2 point = new Vector2(0, top + .625f);
        scout.transform.position = point;scout.Body.position = point;scout.Body.gravityScale = 1;scout.Body.linearVelocity = Vector2.zero;scout.Body.simulated = true;
        SessionState.SetFloat("Woodland.JumpY", point.y);
        heldKey = driveKeyboardJump ? (player==0?Key.W:Key.UpArrow) : Key.None;DriveTestInput();Physics2D.SyncTransforms();
    }
    static void PrepareGuard(WoodlandGame game)
    {
        heldKey = Key.DownArrow;
        game.players[0].ResetScout();game.players[1].ResetScout();Arrange(game);
        game.players[1].inputEnabled = true;
        DriveTestInput();
        Check(game.players[1].SetBlock(true), "Player two can raise directional guard");
    }
    static void TickMelee()
    {
        try {
            if(Elapsed>15)throw new Exception("Melee validation timed out at stage "+SessionState.GetInt("Woodland.Stage",-1));
            var game=WoodlandGame.Instance;if(!game)return;
            var a=game.players[0];var b=game.players[1];
            int stage=SessionState.GetInt("Woodland.Stage",-1);
            if(stage==-1 && Elapsed>.25f) {
                SessionState.SetBool("Woodland.RosterCaptured",true);
                SelectMenuRoster(game,false,false,0);game.StartSelectedMatch();SetStage(0);
            } else if(stage==0 && game.playing && SimElapsed>2) {
                for(int character=0;character<4;character++)Check(WoodlandEffects.Instance.ComboFramesLoaded(character)==12,"Character "+character+" loads twelve new combo-effect sprites");
                foreach(var scout in game.players){scout.inputEnabled=false;scout.aiControlled=false;scout.ResetScout();scout.Body.simulated=true;scout.Body.gravityScale=1;}
                a.transform.position=a.Body.position=new Vector2(-.5f,1);b.transform.position=b.Body.position=new Vector2(.5f,1);
                Physics2D.SyncTransforms();SetStage(1);
            } else if(stage==1 && SimElapsed>.7f) {
                Check(a.Grounded && b.Grounded,"Both fighters stand on a real PVP collider before attacking");
                Check(a.TryAttack(true),"Grounded punch starts immediately");SetStage(2);
            } else if(stage==2 && SimElapsed>.15f) {
                Check(a.ComboCount==1 && Mathf.Approximately(b.Health,b.maxHealth-a.punchDamage*Scout.DamageScale),"Grounded opener connects within 150ms (combo="+a.ComboCount+", health="+b.Health+", damage="+a.punchDamage+", separation="+(b.Body.position-a.Body.position)+", action="+a.Status+")");
                float stamina=a.Stamina;
                Check(!a.TryAttack(true) && a.Stamina==stamina,"Second punch buffers during recovery without charging stamina early");SetStage(3);
            } else if(stage==3 && SimElapsed>.28f) {
                Check(a.ComboCount==2 && Mathf.Approximately(b.Health,b.maxHealth-2*a.punchDamage*Scout.DamageScale),"Buffered second punch connects without teleporting either fighter");
                Check(!a.TryAttack(false),"Kick finisher buffers during second punch recovery");SetStage(4);
            } else if(stage==4 && SimElapsed>.3f) {
                Check(a.ComboCount==3 && Mathf.Approximately(b.Health,100-48.8f*Scout.DamageScale),"Punch-punch-kick string lands naturally with a 1.4x finisher (combo="+a.ComboCount+", health="+b.Health+", separation="+(b.Body.position-a.Body.position)+")");
                Check(Mathf.Approximately(a.Stamina,a.StaminaCapacity-22),"Three accepted attacks spend exactly 22 stamina");
                Check(b.Body.linearVelocity.x>0,"Combo finisher knocks the opponent away");SetStage(5);
            } else if(stage==5 && SimElapsed>1.1f) {
                Check(a.ComboCount==0,"Combo expires after its connection window");
                a.ResetScout();b.ResetScout();a.Body.simulated=b.Body.simulated=true;
                a.transform.position=a.Body.position=new Vector2(-3,1);b.transform.position=b.Body.position=new Vector2(3,1);SetStage(6);
            } else if(stage==6 && SimElapsed>.7f) {
                Check(a.TryAttack(true),"Out-of-range punch starts");SetStage(7);
            } else if(stage==7 && SimElapsed>.34f) {
                Check(a.ComboCount==0 && b.Health==100,"Whiff gives no combo credit or damage");
                a.ResetScout();b.ResetScout();a.Body.simulated=b.Body.simulated=true;
                a.transform.position=a.Body.position=new Vector2(-.5f,1);b.transform.position=b.Body.position=new Vector2(.5f,1);SetStage(8);
            } else if(stage==8 && SimElapsed>.7f) {
                b.enabled=false; // Hold direct guard input without the keyboard update releasing it.
                Check(b.SetBlock(true),"Defender can raise guard");SetStage(9);
            } else if(stage==9 && SimElapsed>.18f) {
                Check(a.TryAttack(true),"Attacker tests an established front guard");SetStage(10);
            } else if(stage==10 && SimElapsed>.16f) {
                Check(b.Health==100 && a.ComboCount==0 && b.Stamina<b.StaminaCapacity,"Guard denies hit confirmation and absorbs damage through stamina");
                b.SetBlock(false);SetStage(11);
            } else if(stage==11 && SimElapsed>.35f) {
                b.enabled=true;
                a.ResetScout();b.ResetScout();a.Body.simulated=b.Body.simulated=true;
                a.transform.position=a.Body.position=new Vector2(-.5f,1);b.transform.position=b.Body.position=new Vector2(.5f,1);SetStage(12);
            } else if(stage==12 && SimElapsed>.7f) {
                Check(a.TryAttack(true),"Punch starts before a timed parry");SetStage(13);
            } else if(stage==13 && SimElapsed>.04f) {
                b.enabled=false;
                Check(b.SetBlock(true),"Defender times a fresh guard during punch startup");SetStage(14);
            } else if(stage==14 && SimElapsed>.1f) {
                Check(b.Health==100 && a.Status=="STAGGERED" && a.ComboCount==0,"Timed parry interrupts the attacker and breaks the combo");
                Check(!a.TryAttack(false),"Parried attacker cannot immediately retaliate");
                b.enabled=true;
                int before=WoodlandEffects.Instance.ComboAnimationsPlayed;
                for(int character=0;character<4;character++) {
                    WoodlandEffects.Instance.ComboImpact(new Vector2(-5.4f+character*3.6f,3.1f),1,character,false);
                    WoodlandEffects.Instance.ComboImpact(new Vector2(-5.4f+character*3.6f,1.2f),-1,character,true);
                }
                Check(WoodlandEffects.Instance.ComboAnimationsPlayed==before+8,"All four hit and finisher effects play through the bounded sprite pool");
                SetStage(15);
            } else if(stage==15 && SimElapsed>.1f) {
                Capture("Documentation/Combo-v8.1.png");
                Complete(true,"MELEE_PASS: grounded combo, natural spacing, stamina, whiff, guard, parry and eight animated combo effects passed");
            }
        } catch(Exception exception) { Complete(false,exception.ToString()); }
    }
    static void Arrange(WoodlandGame game)
    {
        for (int i = 0; i < 2; i++) {
            var scout = game.players[i];scout.inputEnabled = false;
            scout.Body.simulated = true;scout.Body.gravityScale = 0;
            var position = new Vector2(i == 0 ? -.7f : .7f, game.BalanceMode ? 1.4f : 4f);
            scout.transform.position = position;scout.Body.position = position;scout.Body.linearVelocity = Vector2.zero;
        }
        Physics2D.SyncTransforms();
    }
    static void Capture(string path,int width=1280,int height=720)
    {
        if(SessionState.GetBool("Woodland.V81",false)){
            if(path.EndsWith("charge-2-v8.png"))path="Documentation/HUD-v8.1.png";
            else if(path.EndsWith("ultimate-2-v8.png"))path="Documentation/Ultimate-v8.1.png";
            else if(path.EndsWith("move-guide-v8.png"))path="Documentation/Move-guide-v8.1.png";
            else if(!path.EndsWith("Replay-v8.1.png"))return;
        }
        WoodlandGame.Instance.hud.Refresh();
        var camera = Camera.main;
        var canvas = WoodlandGame.Instance.hud.GetComponent<Canvas>();
        var mode = canvas.renderMode;var previousCamera = canvas.worldCamera;float distance = canvas.planeDistance;int order=canvas.sortingOrder;
        var rt = new RenderTexture(width, height, 24);
        camera.targetTexture = rt;WoodlandGame.Instance.UpdateCameraViewport();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;canvas.worldCamera = camera;canvas.planeDistance = 1;canvas.sortingOrder=1000;
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active = rt;
        var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);shot.Apply();File.WriteAllBytes(path, shot.EncodeToPNG());
        camera.targetTexture = null;WoodlandGame.Instance.UpdateCameraViewport();RenderTexture.active = null;
        canvas.renderMode = mode;canvas.worldCamera = previousCamera;canvas.planeDistance = distance;canvas.sortingOrder=order;
        UnityEngine.Object.DestroyImmediate(shot);UnityEngine.Object.DestroyImmediate(rt);
    }
    static void ValidateControllerFamilies(WoodlandGame game)
    {
        // Hardware descriptions exercise Unity's actual Sony revision matchers.
        foreach(int product in new[]{0x5C4,0x9CC,0xCE6,0xDF2}){
            var description=new UnityEngine.InputSystem.Layouts.InputDeviceDescription{interfaceName="HID",manufacturer="Sony Interactive Entertainment",product="Wireless Controller",capabilities="{\"vendorId\":1356,\"productId\":"+product+"}"};
            var pad=InputSystem.AddDevice(description) as Gamepad;
            try{
                Check(pad!=null && WoodlandGame.GamepadStyle(pad)=="PLAYSTATION","Sony product "+product.ToString("X")+": actual PS4/DualSense revision matcher resolves to PlayStation");
                game.RefreshGamepads();game.hud.Refresh();Check(game.hud.helpHint.text=="MENU  [OPTIONS]","Sony revision Menu footer renders Options");
                Check(pad.buttonSouth!=null && pad.buttonWest!=null && pad.buttonEast!=null && pad.buttonNorth!=null && pad.leftShoulder!=null && pad.rightShoulder!=null && pad.leftTrigger!=null && pad.rightTrigger!=null && pad.startButton!=null,"Sony revision exposes every gameplay and Menu control");
            }finally{if(pad!=null)InputSystem.RemoveDevice(pad);game.RefreshGamepads();}
        }
        foreach(string layout in new[]{"XInputController","XInputControllerWindows","Gamepad"}){
            if(!InputSystem.ListLayouts().Contains(layout))continue;
            var pad=(Gamepad)InputSystem.AddDevice(layout);
            try{
                game.RefreshGamepads();game.hud.Refresh();bool xbox=layout.StartsWith("XInput");
                Check(WoodlandGame.GamepadStyle(pad)==(xbox?"XBOX":"GAMEPAD"),layout+": shared Xbox or generic layout has correct button family");
                Check(game.hud.helpHint.text==(xbox?"MENU  [\u2261]":"MENU  [START]"),layout+": rendered Menu hint follows assigned layout");
                Check(pad.buttonSouth!=null && pad.buttonWest!=null && pad.buttonEast!=null && pad.buttonNorth!=null && pad.leftShoulder!=null && pad.rightShoulder!=null && pad.leftTrigger!=null && pad.rightTrigger!=null && pad.startButton!=null,layout+": all combat, dash, ultimate and Menu controls exist");
                game.hud.OpenMoveGuide();game.hud.Refresh();
                Check(game.hud.controlsText[0].text.Contains(xbox?"LT + RT":"BOTH TRIGGERS"),layout+": move guide follows its actual trigger labels");
                game.hud.CloseMoveGuide();
            }finally{InputSystem.RemoveDevice(pad);game.RefreshGamepads();}
        }
        game.hud.Refresh();Check(game.hud.helpHint.text=="MENU  [ESC]","Disconnecting matrix devices restores the keyboard Menu label");
    }
    static void TickHUD()
    {
        try {
            if(Elapsed>20)throw new Exception("Targeted HUD validation timed out at stage "+SessionState.GetInt("Woodland.Stage",-1));
            var game=WoodlandGame.Instance;if(game==null)return;
            var hud=game.hud;int stage=SessionState.GetInt("Woodland.Stage",-1);
            if(stage==-1 && Elapsed>.25f){
                Check(AudioListener.volume==0,"HUD validation keeps speaker output silent");
                SelectMenuRoster(game,false,true,0);hud.startButton.onClick.Invoke();SetStage(0);
            }else if(stage==0 && game.playing && string.IsNullOrEmpty(game.message)){
                foreach(var scout in game.players){scout.inputEnabled=false;scout.aiControlled=false;scout.ResetScout();}
                ValidateHudCorners(game);ValidateHudMeters(game);
                Check(hud.arenaText==null && hud.transform.Find("Match HUD/Arena")==null,"Combat HUD removes the arena-name label");
                var vs=(RectTransform)hud.transform.Find("Match HUD/Versus Emblem");Check(vs.rect.width>=160 && vs.rect.height>=90,"VS artwork is enlarged without changing its aspect ratio");
                foreach(var portrait in hud.fighterPortraits)Check(portrait.rectTransform.rect.width<=100 && portrait.rectTransform.rect.height<=100,"Portrait fits the circle instead of cropping into a face close-up");
                for(int i=0;i<2;i++){
                    var hp=hud.healthFill[i].rectTransform;var sp=hud.shieldFill[i].rectTransform;
                    Check(Vector2.Distance(hp.TransformPoint(hp.rect.center),hud.healthText[i].rectTransform.TransformPoint(hud.healthText[i].rectTransform.rect.center))<.1f,"Health value sits at the exact meter center");
                    Check(Vector2.Distance(sp.TransformPoint(sp.rect.center),hud.shieldText[i].rectTransform.TransformPoint(hud.shieldText[i].rectTransform.rect.center))<.1f,"Stamina value sits at the exact meter center");
                }

                disabledPads=Gamepad.all.Where(pad=>pad.enabled).ToArray();foreach(var pad in disabledPads)InputSystem.DisableDevice(pad);
                testPads[0]=(Gamepad)InputSystem.AddDevice("DualShock4GamepadHID");testPads[1]=(Gamepad)InputSystem.AddDevice("XInputController");game.RefreshGamepads();hud.Refresh();
                Check(hud.deviceText[0].text.Contains("PLAYSTATION")&&hud.deviceText[1].text.Contains("XBOX"),"Targeted HUD binds real registered PlayStation and Xbox input layouts");
                ValidateCharacterActionCovers(game);
                Check(hud.MenuHint=="MENU  [OPTIONS]" && hud.helpHint.text==hud.MenuHint,"PlayStation Menu hint uses the actual Options button name");
                game.TogglePause();hud.Refresh();hud.pauseButtons[4].onClick.Invoke();
                Check(game.Paused && Time.timeScale==0 && hud.MoveGuideOpen,"Move List opens from the pause menu while combat stays frozen");
                Check(hud.guideNames[0].text.Contains("FERN") && hud.guideNames[1].text.Contains("EMBER"),"Move guide names match the two selected fighters");
                for(int character=0;character<4;character++){
                    var actor=game.players[0];actor.SetCharacter(character,game.CharacterNames[character],game.CharacterAnimationSets[character],game.CharacterControllers[character],game.CharacterColors[character]);hud.Refresh();Canvas.ForceUpdateCanvases();
                    Check(hud.controlsText[0].text.Contains(actor.SkillName) && hud.controlsText[0].text.Contains(actor.UltimateName),"Character "+character+": guide follows its actual ranged skill and ultimate");
                    Check(hud.controlsText[0].preferredHeight<=hud.controlsText[0].rectTransform.rect.height,"Character "+character+": all move explanations fit without clipping");
                }
                game.players[0].SetCharacter(0,game.CharacterNames[0],game.CharacterAnimationSets[0],game.CharacterControllers[0],game.CharacterColors[0]);hud.Refresh();
                Check(hud.controlsText[0].text.Contains("SQUARE") && hud.controlsText[1].text.Contains("LT + RT"),"Move guide binds PlayStation and Xbox inputs independently");
                SetStage(10);
            }else if(stage==10 && Elapsed>.15f){
                Capture("Documentation/Move-list-1280-v7.1.png");hud.guideCloseButton.onClick.Invoke();
                Check(!hud.MoveGuideOpen && game.Paused,"Move-list Back button returns to the paused match menu");
                game.ResumeMatch();hud.Refresh();

                Capture("Documentation/HUD-centered-1280-v7.1.png");
                for(int i=0;i<2;i++){InputSystem.RemoveDevice(testPads[i]);testPads[i]=null;}game.RefreshGamepads();
                hud.Refresh();Check(hud.MenuHint=="MENU  [ESC]" && hud.helpHint.text==hud.MenuHint,"Removing controllers restores the keyboard Esc Menu hint");

                ValidateControllerFamilies(game);
                SessionState.SetInt("Woodland.HudClick",0);SetStage(1);
            }else if(stage==1){
                int slot=SessionState.GetInt("Woodland.HudClick",0);int player=slot/4,action=slot%4;
                foreach(var scout in game.players){scout.ResetScout();scout.inputEnabled=false;scout.aiControlled=false;scout.Body.simulated=true;scout.Body.gravityScale=0;scout.Body.linearVelocity=Vector2.zero;}
                game.players[0].Body.position=new Vector2(-3,3);game.players[1].Body.position=new Vector2(3,3);Physics2D.SyncTransforms();
                SessionState.SetFloat("Woodland.HudStamina",game.players[player].Stamina);
                SessionState.SetInt("Woodland.HudAccepted",hud.AcceptedActionClicks);
                hud.actionButtons[slot].onClick.Invoke();
                Check(hud.AcceptedActionClicks==SessionState.GetInt("Woodland.HudAccepted",0)+1 && hud.LastActionSlot==slot && hud.LastActionAccepted,"Mouse action "+slot+": callback records acceptance for the correct player and action");
                Check(hud.ActionFeedback(slot)>0,"Mouse action "+slot+": accepted click produces positive visual feedback");
                float spent=SessionState.GetFloat("Woodland.HudStamina",100)-game.players[player].Stamina;
                Check(action==0?(Mathf.Approximately(spent,4)||Mathf.Approximately(spent,6)):Mathf.Approximately(spent,action==1?8:action==2?6:14),"Mouse action "+slot+": real Scout action spends its exact stamina cost");
                if(action==3)Check(game.players[player].SpecialCooldownRemaining>5.9f,"Mouse ranged action starts the real six-second cooldown");
                string expected=action==0?"Jump":action==1?"Punch":action==2?"Kick":"Smash";
                if(action!=0)Check(game.players[player].AnimationState==expected,"Mouse action "+slot+": real fighter enters its action animation (actual "+game.players[player].AnimationState+")");
                hud.Refresh();SetStage(2);
            }else if(stage==2 && SimElapsed>.04f){
                int slot=SessionState.GetInt("Woodland.HudClick",0);int action=slot%4;var actor=game.players[slot/4];
                if(action==0)Check(actor.AnimationState=="Jump"||actor.AnimationState=="DoubleJump","Mouse jump enters its real airborne animation after the simulation updates");
                if(action==3){
                    float stamina=actor.Stamina;int denied=hud.RejectedActionClicks;hud.actionButtons[slot].onClick.Invoke();
                    Check(hud.RejectedActionClicks==denied+1 && !hud.LastActionAccepted && Mathf.Approximately(stamina,actor.Stamina),"Mouse ranged cooldown rejects repeat without spending stamina");
                    Check(hud.ActionFeedback(slot)<0,"Rejected mouse ranged click produces denial feedback");
                    hud.Refresh();Check(!string.IsNullOrEmpty(hud.specialCooldownText[slot/4].text),"Mouse ranged cooldown displays its countdown label");
                }
                slot++;SessionState.SetInt("Woodland.HudClick",slot);SetStage(slot<8?1:3);
            }else if(stage==3){
                game.TogglePause();Check(game.Paused,"Mouse rejection fixture pauses the match");
                for(int slot=0;slot<8;slot++){
                    var actor=game.players[slot/4];float hp=actor.Health,stamina=actor.Stamina;int denied=hud.RejectedActionClicks;
                    hud.actionButtons[slot].onClick.Invoke();
                    Check(hud.RejectedActionClicks==denied+1 && !hud.LastActionAccepted && hud.LastActionSlot==slot && hud.ActionFeedback(slot)<0,"Paused mouse action "+slot+": callback gives visible denial feedback");
                    Check(Mathf.Approximately(actor.Health,hp)&&Mathf.Approximately(actor.Stamina,stamina),"Paused mouse action "+slot+": rejection leaves health and stamina unchanged");
                }
                game.TogglePause();
                foreach(var scout in game.players)scout.ResetScout();hud.Refresh();
                Check(hud.ultimateLabels.All(label=>!string.IsNullOrEmpty(label.text)),"Both ultimate labels remain visible after mouse actions and reset");
                game.TogglePause();hud.Refresh();hud.OpenMoveGuide();game.ResumeMatch();hud.Refresh();
                Check(!hud.MoveGuideOpen && !game.Paused,"Resuming a match always clears the move guide");
                game.TogglePause();hud.Refresh();hud.OpenMoveGuide();game.RestartMatch();hud.Refresh();
                Check(!hud.MoveGuideOpen && !game.Paused,"Rematching always clears the move guide and paused state");
                Complete(true,"HUD_PASS: centered meters, zoomed portraits, enlarged VS, adaptive Menu hint, paused move guide and mouse actions validated");
            }
        }catch(Exception error){Complete(false,error.ToString());}
    }
    static void TickV8()
    {
        try{
            if(Elapsed>25)throw new Exception("V8 timeout stage "+SessionState.GetInt("Woodland.Stage",-1));
            var game=WoodlandGame.Instance;if(!game)return;var a=game.players[0];var b=game.players[1];var hud=game.hud;int stage=SessionState.GetInt("Woodland.Stage",-1);
            if(stage==-1 && Elapsed>.3f){SelectMenuRoster(game,false,false,0);game.StartSelectedMatch();SetStage(0);}
            else if(stage==0 && game.playing && string.IsNullOrEmpty(game.message)){
                Check(AudioListener.volume==0,"V8 tests keep speaker output silent");
                Check(game.arenaPlatforms.Length==0,"PVP contains no upper ledge objects");
                Check(!game.BalanceMode && game.plank.bodyType==RigidbodyType2D.Kinematic,"PVP retains its long fixed main platform");
                for(int c=0;c<4;c++){
                    float expected=c==1?165:c==3?151:160;Check(Mathf.Approximately(game.CharacterPortraits[c].pixelsPerUnit,expected),"Character "+c+": one calibrated uniform pixel scale");
                    Check(game.CharacterAnimationSets[c].punch.Select(AssetDatabase.GetAssetPath).All(path=>path.Contains("/"+game.CharacterNames[c]+"/Punch/")),"Character "+c+": punch frames belong to its own roster");
                    Check(game.CharacterAnimationSets[c].kick.Select(AssetDatabase.GetAssetPath).All(path=>path.Contains("/"+game.CharacterNames[c]+"/Kick/")),"Character "+c+": kick frames belong to its own roster");
                }
                foreach(int c in new[]{2,3})Check(AssetDatabase.GetAssetPath(game.CharacterPortraits[c]).Contains("/Idle/01.png"),"Character "+c+": profile uses true neutral Idle artwork");
                if(SessionState.GetBool("Woodland.V81",false)){
                    Check(b.ReceiveHit(a,1,true) && Mathf.Approximately(100-b.Health,13.6f),"Punch damage is fifteen percent lower: 13.6");
                    Check(a.ReceiveHit(b,-1,false) && Mathf.Approximately(100-a.Health,10.2f),"Kick damage is fifteen percent lower: 10.2");
                    Check(hud.characterGuardCovers.Length==4 && hud.characterGuardCovers.Select(AssetDatabase.GetAssetPath).Distinct().Count()==4,"Four unique shield assets are loaded");
                    for(int c=0;c<4;c++)Check(AssetDatabase.GetAssetPath(hud.characterGuardCovers[c]).EndsWith("/"+game.CharacterNames[c]+"/Guard.png"),"Character "+c+": guard uses its own generated pixel shield");
                }
                disabledPads=Gamepad.all.Where(p=>p.enabled).ToArray();foreach(var pad in disabledPads)InputSystem.DisableDevice(pad);
                originalInputSettings=InputSystem.settings;validationInputSettings=UnityEngine.Object.Instantiate(originalInputSettings);validationInputSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;validationInputSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=validationInputSettings;
                testPads[0]=InputSystem.AddDevice<Gamepad>();game.RefreshGamepads();InputSystem.onBeforeUpdate+=DriveTestInput;
                foreach(var scout in game.players){scout.ResetScout();scout.Body.simulated=true;scout.Body.gravityScale=1;scout.aiControlled=false;scout.inputEnabled=false;}
                a.inputEnabled=true;SetStage(1);
            }else if(stage==1 && SimElapsed>.6f){
                Check(a.Grounded,"Player settles on the single PVP floor before input checks");SessionState.SetFloat("Woodland.V8Y",a.Body.position.y);
                padStates[0]=new GamepadState().WithButton(GamepadButton.South);SetStage(2);
            }else if(stage==2 && SimElapsed>.12f){
                Check(a.Blocking && a.Body.position.y<SessionState.GetFloat("Woodland.V8Y",0)+.06f,"Cross/A holds guard and does not jump");
                Check(hud.actionGlyphs[0].text=="SOUTH" || hud.actionIcons[0].sprite!=null,"Guard circle shows the assigned south-button prompt");
                padStates[0]=new GamepadState();SetStage(3);
            }else if(stage==3 && SimElapsed>.08f){
                Check(!a.Blocking,"Releasing Cross/A ends guard");padStates[0]=new GamepadState().WithButton(GamepadButton.DpadUp);SetStage(4);
            }else if(stage==4 && SimElapsed>.12f){
                Check(!a.Grounded && a.Body.linearVelocity.y>0 && a.AirJumpsRemaining==1,"First D-pad Up press launches a ground jump and retains one aerial jump");
                SessionState.SetFloat("Woodland.V8JumpY",a.Body.position.y);padStates[0]=new GamepadState();SetStage(5);
            }else if(stage==5 && SimElapsed>.08f){padStates[0]=new GamepadState().WithButton(GamepadButton.DpadUp);SetStage(6);}
            else if(stage==6 && SimElapsed>.12f){
                Check(a.AirJumpsRemaining==0 && a.Body.position.y>SessionState.GetFloat("Woodland.V8JumpY",0),"Second Up press produces a real aerial jump");
                Check(a.AnimationState=="DoubleJump","Aerial jump selects its DoubleJump animation");Check(!a.TryJump(),"Third jump is refused without another aerial jump");Capture("Documentation/double-jump-v8.png");
                padStates[0]=new GamepadState();InputSystem.RemoveDevice(testPads[0]);testPads[0]=null;game.RefreshGamepads();a.inputEnabled=false;
                SessionState.SetInt("Woodland.V8Character",0);SetStage(9);
            }else if(stage==9){
                int c=SessionState.GetInt("Woodland.V8Character",0);PrepareSpecialCase(game,c,4);game.CharacterChoices[0]=c;game.CharacterChoices[1]=c==0?1:0;PositionSpecialPair(game,4,.85f);hud.Refresh();
                Check(a.TrySpecial() && a.Charging && a.AnimationState=="Charge","Character "+c+": Triangle starts a visible charge");Check(Mathf.Approximately(a.Stamina,86),"Character "+c+": charge spends fourteen stamina exactly once");
                Check(!a.TrySpecial() && !a.ReleaseSmash(),"Character "+c+": charging cannot be spammed or fired early");Check(a.SpecialCooldownRemaining==0,"Character "+c+": no ranged cooldown timer");SetStage(10);
            }else if(stage==10 && SimElapsed>.3f){
                int c=SessionState.GetInt("Woodland.V8Character",0);Check(a.Charging && a.Charge>0 && a.Charge<1 && b.Health==100,"Character "+c+": charge is readable before any projectile damage");hud.Refresh();Check(hud.specialCooldownText[0].text.Contains("CHARGE"),"Character "+c+": HUD displays charge progress instead of cooldown");Capture("Documentation/charge-"+c+"-v8.png");
                if(c==0){game.TogglePause();SessionState.SetFloat("Woodland.V8FrozenCharge",a.Charge);}SetStage(11);
            }else if(stage==11 && Elapsed>.17f){
                if(game.Paused){Check(Mathf.Approximately(a.Charge,SessionState.GetFloat("Woodland.V8FrozenCharge",0)),"Pausing freezes charge progress");game.ResumeMatch();}SetStage(12);
            }else if(stage==12 && SimElapsed>.82f){
                int c=SessionState.GetInt("Woodland.V8Character",0);Check(!a.Charging,"Character "+c+": complete charge automatically launches");Capture("Documentation/ranged-"+c+"-v8.png");SetStage(13);
            }else if(stage==13 && SimElapsed>.7f){
                int c=SessionState.GetInt("Woodland.V8Character",0);Check(b.Health<100,"Character "+c+": charged projectile reaches and damages its target");
                Check(a.TrySpecial(),"Character "+c+": next charge is allowed without waiting six seconds");a.Stagger(.1f);Check(!a.Charging,"Character "+c+": interruption cancels the charge");
                if(c<3){SessionState.SetInt("Woodland.V8Character",c+1);SetStage(9);}else{SessionState.SetInt("Woodland.V8Character",0);SetStage(20);}
            }else if(stage==20){
                int c=SessionState.GetInt("Woodland.V8Character",0);PrepareUltimateCase(game,c,0,25,1.2f);game.CharacterChoices[0]=c;game.CharacterChoices[1]=c==0?1:0;PlaceOnPvpBase(game,false);SetStage(21);
            }else if(stage==21 && SimElapsed>.4f){Check(a.UltimateReady && a.TryUltimate(),"Low-health ultimate starts for character "+a.CharacterIndex);SetStage(22);}
            else if(stage==22 && SimElapsed>.3f){
                int c=SessionState.GetInt("Woodland.V8Character",0);Check(WoodlandEffects.Instance.CinematicActive,"Character "+c+": ultimate introduction enters cinematic presentation");hud.Refresh();Check(hud.ultimateCutins[0].activeSelf && hud.ultimateCutinNames[0].text==a.UltimateName,"Character "+c+": animated own-character cut-in displays its move name");Capture("Documentation/ultimate-"+c+"-v8.png");SetStage(23);
            }else if(stage==23 && SimElapsed>2.1f){
                int c=SessionState.GetInt("Woodland.V8Character",0);Check(!a.UltimateActive && a.UltimateUsed && !a.TryUltimate(),"Character "+c+": ultimate ends and remains once per round");
                Check(!WoodlandEffects.Instance.CinematicActive,"Character "+c+": cinematic camera restores after ultimate");hud.Refresh();Check(!hud.ultimateCutins[0].activeSelf,"Character "+c+": cut-in hides after the ultimate");
                if(c<3){SessionState.SetInt("Woodland.V8Character",c+1);SetStage(20);}else{
                    PrepareSpecialCase(game,2,3);game.CharacterChoices[0]=2;game.CharacterChoices[1]=3;b.SetCharacter(3,game.CharacterNames[3],game.CharacterAnimationSets[3],game.CharacterControllers[3],game.CharacterColors[3]);PositionSpecialPair(game,3,.85f);hud.Refresh();
                    Check(hud.ActivateAction(0,0) && a.Blocking,"Actual guard-circle callback holds guard");hud.ReleasePointerGuard(0);Check(!a.Blocking,"Releasing the guard circle ends pointer guard");
                    hud.OpenMoveGuide();hud.Refresh();Check(hud.controlsText[0].text.Contains("0.85") && !hud.controlsText[0].text.Contains("6s cooldown"),"Move guide explains wind-up instead of six-second cooldown");Capture("Documentation/move-guide-v8.png");hud.CloseMoveGuide();
                    if(SessionState.GetBool("Woodland.V81",false)){
                        game.score[0]=game.roundsToWin-1;game.score[1]=0;b.KnockOut();SetStage(80);return;
                    }
                    Complete(true,"V8_PASS: single platform, Up double jump, south guard, all charged projectiles, idle portraits, calibrated scales and ultimate cleanup validated");
                }
            }else if(stage==80 && game.CanPlayAgain){
                Check(game.ReplayHint=="R TO PLAY AGAIN","Keyboard match-end hint shows R");
                var sony=(Gamepad)InputSystem.AddDevice("DualShock4GamepadHID");game.RefreshGamepads();Check(game.ReplayHint=="CROSS TO PLAY AGAIN","PS4 match-end hint shows Cross");InputSystem.RemoveDevice(sony);game.RefreshGamepads();
                testPads[0]=(Gamepad)InputSystem.AddDevice("XInputController");padStates[0]=new GamepadState();game.RefreshGamepads();SetStage(81);
            }else if(stage==81 && Elapsed>.1f){
                Check(game.ReplayHint=="A TO PLAY AGAIN" && game.message.Contains("A TO PLAY AGAIN"),"Actual Xbox end screen displays A to play again");Capture("Documentation/Replay-v8.1.png");
                padStates[0]=new GamepadState().WithButton(GamepadButton.South);SetStage(82);
            }else if(stage==82 && Elapsed>.15f){
                Check(!game.MatchOver && game.score[0]==0 && game.score[1]==0,"Xbox A input starts a new match and clears scores");padStates[0]=new GamepadState();
                Complete(true,"V81_PASS: controller replay, lower damage, four pixel shields and v8 combat regression validated");
            }
        }catch(Exception error){Complete(false,error.ToString());}
    }
    static void Complete(bool success, string status)
    {
        SessionState.SetBool(Active, false);
        Scout.UltimateCinematic-=TrackUltimate;
        if (SessionState.GetBool("Woodland.RestoreAudioKeys", false) && WoodlandEffects.Instance != null) {
            var effects = WoodlandEffects.Instance;
            if (effects.Muted != SessionState.GetBool("Woodland.AudioKeysMuted", false)) effects.ToggleMute();
            effects.SetVolume(SessionState.GetFloat("Woodland.AudioKeysVolume", .5f), false);
            SessionState.SetBool("Woodland.RestoreAudioKeys", false);
        }
        InputSystem.onBeforeUpdate -= DriveTestInput;
        for (int i = 0; i < testPads.Length; i++) { if (testPads[i] != null && testPads[i].added) InputSystem.RemoveDevice(testPads[i]);testPads[i] = null;padStates[i] = new GamepadState(); }
        foreach (var pad in disabledPads) if (pad != null && pad.added) InputSystem.EnableDevice(pad);
        disabledPads = new Gamepad[0];
        if (testKeyboard != null) { InputSystem.RemoveDevice(testKeyboard);testKeyboard = null; }
        if (originalInputSettings != null) { InputSystem.settings = originalInputSettings;UnityEngine.Object.DestroyImmediate(validationInputSettings);originalInputSettings = null; }
        var report = new Report { success = success, status = status, checks = SessionState.GetString("Woodland.Checks", "").Split('\n').Where(s => s.Length > 0).ToArray() };
        bool v81=SessionState.GetBool("Woodland.V81",false);SessionState.SetBool("Woodland.V81",false);
        bool v8=SessionState.GetBool("Woodland.V8",false);SessionState.SetBool("Woodland.V8",false);
        bool meleeOnly=SessionState.GetBool("Woodland.MeleeOnly",false);
        bool hudOnly=SessionState.GetBool("Woodland.HudOnly",false);
        SessionState.SetBool("Woodland.MeleeOnly",false);
        SessionState.SetBool("Woodland.HudOnly",false);
        bool duel=SessionState.GetBool("Woodland.Duel",false);SessionState.SetBool("Woodland.Duel",false);
        bool counterplay=SessionState.GetBool("Woodland.Counterplay",false);SessionState.SetBool("Woodland.Counterplay",false);
        bool midterm=SessionState.GetBool("Woodland.MidtermCombat",false);SessionState.SetBool("Woodland.MidtermCombat",false);
        bool contacts=SessionState.GetBool("Woodland.Contacts",false);SessionState.SetBool("Woodland.Contacts",false);
        File.WriteAllText(contacts?"Documentation/CONTACTS-v15.json":midterm?"Documentation/MIDTERM-COMBAT-v14.json":counterplay?"Documentation/COUNTERPLAY-v9.json":duel?"Documentation/DUEL-VALIDATION.json":v81?"Documentation/PLAYMODE-VALIDATION-v8.1.json":v8?"Documentation/PLAYMODE-VALIDATION-v8.json":hudOnly?"Documentation/HUD-VALIDATION-v7.1.json":meleeOnly?"Documentation/MELEE-VALIDATION-v8.1.json":"Documentation/PLAYMODE-VALIDATION-v7.json", JsonUtility.ToJson(report, true));
        if (success) Debug.Log(status);else Debug.LogError(status);
        EditorApplication.isPlaying = false;
        if (SessionState.GetBool("Woodland.ValidationExit", false)) EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
    }
}
