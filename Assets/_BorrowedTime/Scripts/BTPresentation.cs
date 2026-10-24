using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime {
// Shared presentation only. Quest state and collisions remain in the puzzle components.
public class BTPresentation : MonoBehaviour {
    BTTrainingGame game;
    GUIStyle textStyle,headingStyle,buttonStyle;
    int openingPage;
    public bool briefingComplete;
    float nextAlarm; bool endingSound;
    string[] opening={
        "แม่น้ำสีม่วง สวนที่เหี่ยวเฉา และเมืองที่เงียบลงทุกวัน\nการฟื้นฟูต้องเริ่มที่ต้นเหตุของมลพิษ",
        "ERI — องค์กรอนุรักษ์ติดต่อคุณ\nเราต้องการคนที่เข้าใจระบบนิเวศ และพร้อมเดินทางไปหยุดต้นเหตุ",
        "Protagonist: ผมเข้าใจว่าถ้าปล่อยไว้ ชีวิตจะค่อย ๆ หายไป\nผมรับภารกิจนี้",
        "ERI พาคุณมาถึงสถานีวิจัยของ Norman Scott\nอดีตนักวิจัยของบริษัทเจ้าของโรงงาน",
        "Norman: ฉันสร้าง Time Gauntlet เพื่อซ่อมสิ่งที่เราเคยทำลาย\nเปลี่ยนอายุของวัตถุได้ แต่ต้องยืมพลังชีวิตจากพืชอย่างพอดี",
        "Norman: นี่คือ Robo ผู้ช่วยของเธอ\nหลังฝึกเสร็จ ฉันจะมอบเมล็ดพันธุ์สุดท้ายให้ ปลูกเมื่อหยุดโรงงานและมีน้ำสะอาดแล้ว",
        "Robo: ไปสวนฝึกกัน! H ขอคำใบ้ได้ทีละระดับ\nคืนสภาพด้วยล้อเมาส์ถอย เร่งสภาพด้วยล้อเมาส์ไปหน้า ขณะกด RMB ค้าง"
    };
    void Start(){game=GetComponent<BTTrainingGame>();}
    public bool AdvanceOpening(){
        if(briefingComplete)return true;
        if(++openingPage>=opening.Length){briefingComplete=true;return true;}return false;
    }
    void Update(){
        if(game && BorrowedTime.Menu.GamePauseMenu.IsPausedFor(game.player))return;
        if(!game)return;
        BTSaveGame.Tick(game);
        var factory=game as BTFactoryGame;
        if(factory && Time.time<factory.tensionUntil && !game.player.paused && Time.time>nextAlarm){game.PlayTone(280);nextAlarm=Time.time+3;}
        if(factory && factory.completed){if(RenderSettings.sun){RenderSettings.sun.color=Color.Lerp(RenderSettings.sun.color,new Color(1,.93f,.72f),Time.deltaTime*.5f);RenderSettings.sun.intensity=Mathf.MoveTowards(RenderSettings.sun.intensity,1.35f,Time.deltaTime*.2f);} if(!endingSound){endingSound=true;game.PlayTone(523);game.PlayTone(659);game.PlayTone(factory.GoodEnding?784:698);Burst(factory.miracleTree.transform.position+Vector3.up*6,Color.green,36);}}
        if(game.robo){
            float d=Vector3.Distance(game.robo.position,game.player.transform.position);
            if(d>16)game.robo.position=game.player.transform.position+Vector3.up*2;
        }
    }
    bool Button(Rect r,string label){return GUI.Button(r,label,buttonStyle);}
    void OnGUI(){
        if(game && BorrowedTime.Menu.GamePauseMenu.IsPausedFor(game.player))return;
        if(!game || !game.player || game.player.hideHud)return;
        if(textStyle==null){textStyle=new GUIStyle(GUI.skin.label){font=game.uiFont,fontSize=17,wordWrap=true};headingStyle=new GUIStyle(textStyle){fontSize=24,fontStyle=FontStyle.Bold};buttonStyle=new GUIStyle(GUI.skin.button){font=game.uiFont,fontSize=20};}
        GUI.depth=-5;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
        if(game.stage>0 && !game.player.overview && !game.completed && !game.player.paused){
            GUI.Box(new Rect(24,603,290,96),"");GUI.Label(new Rect(40,610,260,33),"พลังถุงมือ  "+Mathf.RoundToInt(game.player.energy)+" / 100",headingStyle);
            GUI.color=new Color(.35f,1,.7f);GUI.DrawTexture(new Rect(42,652,250*game.player.energy/100,9),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(40,669,260,27),"พลังชีวิตที่ยืมมา • F5 บันทึก",textStyle);
            GUI.Label(new Rect(708,435,40,35),"+",headingStyle);
            var t=game.player.target;
            if(t){GUI.Box(new Rect(460,486,520,60),"");GUI.Label(new Rect(475,490,490,54),"อายุ "+t.age+" / "+(t.states==null?0:Mathf.Max(0,t.states.Length-1))+"  •  เปลี่ยนขั้นละ 15"+(t.IsLiving?"\nพลังชีวิต "+Mathf.RoundToInt(t.life)+"%"+(t.absorbAllowed?"  •  ดูดอย่างพอดี":"  •  ดูดพลังไม่ได้"):"  •  วัตถุไม่มีพลังชีวิต")+(t.pollutionStress?"  / รากป่วยจากมลพิษ":""),textStyle);}
            foreach(var target in FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None)){
                if(!target.aimPoint || Vector3.Distance(game.player.transform.position,target.aimPoint.position)>12 || target==t)continue;
                var pos=target.aimPoint.position;RaycastHit hit;
                if(Physics.Linecast(game.player.view.transform.position,pos,out hit,~(1<<2),QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<BTTimeTarget>()!=target)continue;
                var screen=game.player.view.WorldToScreenPoint(pos+Vector3.up*.8f);if(screen.z<=0)continue;
                GUI.color=target.IsLiving&&target.absorbAllowed?new Color(.5f,1,.6f):new Color(.45f,.85f,1);
                GUI.Label(new Rect(screen.x/Screen.width*1440-65,900-screen.y/Screen.height*900-12,145,28),target.IsLiving&&target.absorbAllowed?"● พลังชีวิต":"◇ เวลา",textStyle);GUI.color=Color.white;
            }
            var f=game as BTFactoryGame;
            if(f && Time.time<f.tensionUntil){GUI.color=new Color(1,.65f,.3f);GUI.Box(new Rect(400,172,640,52),"");GUI.Label(new Rect(420,181,600,40),"แรงดันสูง! คืนสภาพทางเดินแล้วข้ามไปแท่นปลอดภัย • ไม่มีเวลานับถอยหลัง",textStyle);GUI.color=Color.white;}
        }
        if(game.stage==0 && game.GetType()==typeof(BTTrainingGame) && !briefingComplete){
            GUI.DrawTexture(new Rect(0,0,1440,900),Texture2D.blackTexture);GUI.Box(new Rect(260,170,920,540),"");
            GUI.Label(new Rect(305,205,820,60),"BORROWED TIME  /  "+(openingPage+1)+" : "+opening.Length,headingStyle);
            GUI.Label(new Rect(305,300,820,225),opening[Mathf.Clamp(openingPage,0,opening.Length-1)],headingStyle);
            if(Button(new Rect(305,550,820,52),"ต่อ / ENTER")){game.Begin();}
            if(Button(new Rect(305,615,390,42),"ข้ามบทนำและเริ่มฝึก")){briefingComplete=true;game.Begin();}
            if(BTSaveGame.Exists && Button(new Rect(725,615,400,42),"เล่นต่อจากบันทึก / C"))BTSaveGame.Continue();
            if(Button(new Rect(305,665,820,40),"ฉากทดลอง / Fun Prototype"))UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/_BorrowedTime/Scenes/FunPrototype.unity");
        }else if((game.stage==0 || game.player.paused) && BTSaveGame.Exists){
            if(Button(new Rect(505,700,430,42),"เล่นต่อจากบันทึกล่าสุด / C"))BTSaveGame.Continue();
        }
        if(BTSaveGame.SavedUntil>Time.unscaledTime)GUI.Label(new Rect(1050,820,365,40),BTSaveGame.Notice,textStyle);
    }
    public static void Burst(Vector3 p,Color color,int count=12){
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=color;
        for(int i=0;i<count;i++){
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="Temporal sparkle";g.layer=2;Destroy(g.GetComponent<Collider>());g.transform.position=p+Random.insideUnitSphere*.5f;g.transform.localScale=Vector3.one*Random.Range(.05f,.14f);g.GetComponent<Renderer>().sharedMaterial=material;
            var effect=g.AddComponent<BTSparkle>();effect.velocity=Random.onUnitSphere*.9f+Vector3.up;Destroy(g,1.3f);
        }Destroy(material,1.5f);
    }
}
public class BTSparkle : MonoBehaviour {public Vector3 velocity;void Update(){transform.position+=velocity*Time.deltaTime;transform.localScale*=Mathf.Exp(-Time.deltaTime*1.4f);}}
}
