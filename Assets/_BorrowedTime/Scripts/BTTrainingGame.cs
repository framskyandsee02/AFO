using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime {
public class BTTrainingGame : MonoBehaviour {
    public BTPlayer player;
    public BTTimeTarget tree,bridge,vine,mechanism,seal;
    public BTTimeTarget wonderTree;
    public Transform robo,door,seed,norman;
    public Transform moveMarker;
    public Camera overviewCamera;
    public Font uiFont;
    public int stage;
    public bool completed,hasMiracleSeed,bridgeLearned,vineLearned;
    public float absorbed;
    public string roboLine="";
    public string[] objectives={"พบ Norman และ Robo","เดินตามทางไปยังวงแหวน","ดูดพลังชีวิตจากต้นไม้ 20 หน่วย","เร่งต้นอ่อนให้โต แล้วคืนสภาพสะพาน","Accelerate เถาวัลย์ให้เป็นทางเดิน","ซ่อมกลไก และเร่งเถาวัลย์ที่ปิดประตู","ผ่านประตู แล้วรับเมล็ดพันธุ์จาก Norman","การฝึกเสร็จสมบูรณ์"};
    public Vector3[] markers;
    public Vector3 mapCameraPosition=new Vector3(-38,43,-43),mapCameraTarget=new Vector3(0,0,4);
    public float mapCameraSize=33;
    public bool advanceToCity=true;
    float transitionAt;
    float doorLift;
    int hintTier;
    protected AudioSource audioSource;
    GUIStyle title,body,small,big;
    Texture2D panel,accent,dim;
    protected virtual void Start() {
        BTJourney.NewJourney();
        audioSource=gameObject.AddComponent<AudioSource>();
        if(!GetComponent<BTPresentation>()) gameObject.AddComponent<BTPresentation>();
        SetStage(0); Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
    }
    public virtual void Begin() {
        if(stage!=0) return; var presentation=GetComponent<BTPresentation>(); if(presentation && !presentation.AdvanceOpening())return; SetStage(1); Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
    }
    void SetStage(int next) {
        stage=next; hintTier=0;
        string[] lines={"Norman: ถุงมือนี้ยืมพลังจากชีวิต จงใช้ด้วยความระมัดระวังนะ","Robo: ยินดีต้อนรับ! เดินไปที่วงแหวน ลองวิ่งและกระโดดได้เลย","Robo: พืชให้พลังชีวิตแก่ถุงมือ เล็งต้นไม้แล้วดูดอย่างพอดี เราจะไม่ทำร้ายมันจนตาย","Robo: ลองเร่งต้นอ่อนข้างทางให้โตเต็มวัยก่อน แล้วค่อยคืนสภาพสะพานทีละขั้น","Robo: เถาวัลย์ยังเล็กอยู่ ลองพามันเติบโตเป็นทางขึ้น","Robo: ใช้สิ่งที่เรียนมา! กลไกเก่าต้องย้อนเวลา ส่วนเถาวัลย์ที่ขวางประตูต้องแก่ลง","Norman: เก่งมาก! ฉันรออยู่หลังประตู พร้อมเมล็ดพันธุ์แห่งความหวัง","Robo: ภารกิจต่อไปคือเมืองร้าง เราจะคืนชีวิตให้โลกใบนี้ไปด้วยกัน"};
        roboLine=lines[next]; if(next>0) PlayTone(720);
    }
    protected virtual void Update() {
        if(stage==0 && Keyboard.current!=null && Keyboard.current.enterKey.wasPressedThisFrame) Begin();
        if(player.paused) return;
        if(completed && advanceToCity && Time.time>=transitionAt) { advanceToCity=false; BTJourney.LoadCity(player.energy); return; }
        if(robo) {
            Vector3 desired=player.transform.position+Quaternion.Euler(0,player.yaw,0)*new Vector3(1.2f,2.1f,-.3f);
            desired.y+=Mathf.Sin(Time.time*2.8f)*.13f;
            robo.position=Vector3.Lerp(robo.position,desired,Time.deltaTime*4);
            robo.rotation=Quaternion.Slerp(robo.rotation,Quaternion.Euler(0,player.yaw+180,Mathf.Sin(Time.time*1.7f)*5),Time.deltaTime*5);
        }
        if(seed) { seed.Rotate(0,35*Time.deltaTime,0); }
        if(moveMarker) moveMarker.gameObject.SetActive(stage==1);
        if(stage==1 && Vector3.Distance(player.transform.position,markers[1])<2.2f && player.walked>3) SetStage(absorbed>=19.95f?3:2);
        if(stage==3 && bridge.age==0 && (!wonderTree || wonderTree.age==1)) { bridgeLearned=true; SetStage(4); }
        if(stage==4 && vine.age==1) { vineLearned=true; SetStage(5); }
        if(stage==5 && mechanism.age==0 && seal.age==2 && bridgeLearned && vineLearned) SetStage(6);
        doorLift=Mathf.MoveTowards(doorLift,stage>=6?5.5f:0,Time.deltaTime*2.5f);
        if(door) door.localPosition=new Vector3(12,5+doorLift,19);
        if(stage>=5 && player.transform.position.y>2.8f && player.transform.position.z>14 && player.transform.position.z<18) player.checkpoint=new Vector3(12,3.1f,16);
        else if(stage>=4 && player.transform.position.x>4 && player.transform.position.z<8) player.checkpoint=new Vector3(8,.1f,4);
    }
    public virtual void EnergyAbsorbed(float amount) {
        if(stage==1 || stage==2) { absorbed+=amount; if(stage==2 && absorbed>=19.95f) SetStage(3); }
    }
    public virtual void TargetChanged(BTTimeTarget t) {
        if(t==wonderTree && t.age==1) { roboLine="Robo: จากต้นอ่อนเป็นร่มเงา! เร่งเวลาเปลี่ยนอายุ ส่วนพลังชีวิตคืออีกอย่างหนึ่ง"; BTPresentation.Burst(t.transform.position+Vector3.up*2,Color.green,28); }
        if(t==bridge && t.age==0 && stage<3) player.Message("สะพานซ่อมแล้ว เมื่อถึงขั้นฝึกระบบจะตรวจให้");
    }
    public virtual void Interact() {
        if(stage==6 && Vector3.Distance(player.transform.position,new Vector3(12,3,22))<3) {
            hasMiracleSeed=true; completed=true; if(seed) seed.gameObject.SetActive(false); SetStage(7);
            BTJourney.HasMiracleSeed=true; transitionAt=Time.time+1.8f; BTSaveGame.Request(this);
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        } else player.Message("เดินไปหา Norman หลังประตู แล้วกด F เพื่อรับเมล็ดพันธุ์");
    }
    public virtual void ResetTraining() {
        BTJourney.NewJourney();
        foreach(var t in new[]{tree,bridge,vine,mechanism,seal,wonderTree}) if(t) t.ResetTarget();
        absorbed=0; completed=hasMiracleSeed=bridgeLearned=vineLearned=false;
        player.energy=50; player.walked=0; player.yaw=0; player.pitch=20; player.paused=false; player.overview=false; player.checkpoint=new Vector3(-16,.15f,-12); player.Respawn();
        if(seed) seed.gameObject.SetActive(true); SetStage(1);
        Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
    }
    public virtual void ShowHint() {
        string[,] hints={
            {"เริ่มการฝึกกับ Robo","กด Enter เพื่อเริ่ม","กด Enter เพื่อเริ่ม"},
            {"มองหาวงแหวนสีทอง","เดินตามทางทรายไปข้างหน้า","WASD เดิน / Shift วิ่ง / Space กระโดด"},
            {"ต้นไม้ใหญ่บนเกาะฝั่งนี้ให้พลังได้","เข้าใกล้ต้นไม้ภายใน 3 เมตร","กด RMB ค้างเล็ง แล้วกด LMB ค้างประมาณ 2 วินาที"},
            {"ข้ามธารด้วยสะพานไม้","สะพานต้องย้อนกลับสองขั้น","เล็งวงแหวนสีฟ้า กด RMB ค้าง แล้วเลื่อนล้อเมาส์ถอยสองครั้ง"},
            {"มองหาเถาวัลย์ตรงผาหลังสะพาน","เถาวัลย์เล็กต้องโตเป็นทางเดิน","เล็งผลึกสีเขียว กด RMB ค้าง แล้วดันล้อเมาส์ไปหน้าหนึ่งครั้ง"},
            {"ประตูมีสองสิ่งที่ต้องจัดการ","กลไกฝั่งซ้ายต้องใหม่ เถาวัลย์ฝั่งขวาต้องแก่","ล้อเมาส์ถอย: คืนสภาพกลไก / ล้อเมาส์ไปหน้า: เร่งเถาวัลย์"},
            {"Norman อยู่ด้านหลังประตู","เดินลอดประตูไปจนถึงแท่นเมล็ดพันธุ์","เข้าใกล้แท่น แล้วกด F"},
            {"การฝึกจบแล้ว","กด R เพื่อเริ่มฝึกใหม่","ด่านเมืองร้างจะพัฒนาต่อไป"}};
        roboLine="Robo: "+hints[stage,Mathf.Min(2,hintTier++)];
    }
    public void PlayTone(float frequency) {
        if(!audioSource) return;
        int count=5000; var samples=new float[count];
        for(int i=0;i<count;i++) samples[i]=Mathf.Sin(i*frequency*6.283185f/22050)*Mathf.Sin(Mathf.PI*i/count)*.1f;
        var clip=AudioClip.Create("Robo chirp",count,1,22050,false); clip.SetData(samples,0); audioSource.PlayOneShot(clip); Destroy(clip,1);
    }
    Texture2D Solid(Color c) { var t=new Texture2D(1,1); t.SetPixel(0,0,c); t.Apply(); return t; }
    void InitUI() {
        if(title!=null) return;
        title=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=23,fontStyle=FontStyle.Bold,wordWrap=true}; title.normal.textColor=Color.white;
        body=new GUIStyle(title){fontSize=18,fontStyle=FontStyle.Normal};
        small=new GUIStyle(body){fontSize=14}; small.normal.textColor=new Color(.79f,.9f,.89f);
        big=new GUIStyle(title){fontSize=38};
        panel=Solid(new Color(.045f,.13f,.16f,.94f)); accent=Solid(new Color(.55f,.94f,.64f)); dim=Solid(new Color(.02f,.07f,.09f,.78f));
    }
    void Box(Rect r,Texture2D t) { GUI.DrawTexture(r,t); }
    void Label(Rect r,string s,GUIStyle style) { GUI.Label(r,s,style); }
    protected virtual void OnGUI() {
        if(player.hideHud) return;
        InitUI(); GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
        Box(new Rect(24,24,490,128),panel); Box(new Rect(24,24,5,128),accent);
        Label(new Rect(45,36,450,34),"BORROWED TIME",title);
        Label(new Rect(45,70,450,26),"01 / สวนฝึกแห่งกาลเวลา",small);
        Label(new Rect(45,103,440,44),Mathf.Min(stage,6)+" / 6   "+objectives[stage],body);
        Box(new Rect(1110,24,306,105),panel);
        Label(new Rect(1130,36,260,26),"TIME GAUNTLET",small);
        Box(new Rect(1130,72,222,14),dim); Box(new Rect(1130,72,222*player.energy/100,14),accent);
        Label(new Rect(1360,63,50,30),Mathf.RoundToInt(player.energy).ToString(),body);
        Label(new Rect(1130,93,270,28),"พลังชีวิต ≠ อายุของวัตถุ",small);
        if(stage>0 && !completed) {
            Box(new Rect(24,724,1392,84),panel);
            Label(new Rect(44,736,110,26),"ROBO",small);
            Label(new Rect(44,765,1350,40),roboLine,body);
            Label(new Rect(28,837,1400,40),"WASD เดิน   Shift วิ่ง   Space กระโดด   RMB เล็ง + LMB ดูดพลัง / ล้อถอย คืนสภาพ / ล้อหน้า เร่งสภาพ   F รับเมล็ด   H คำใบ้   Tab แผนที่   R เริ่มใหม่   Esc พัก",small);
            if(!player.overview) {
                Label(new Rect(709,435,30,30),"+",title);
                if(player.target) {
                    Box(new Rect(495,557,450,100),panel);
                    Label(new Rect(515,568,410,34),player.target.displayName, title);
                    string status=player.target.StateName;
                    if(player.target.kind==TargetKind.Plant) status+="  |  พลังชีวิต "+Mathf.RoundToInt(player.target.life)+"%";
                    Label(new Rect(515,608,410,44),status,body);
                }
                if(stage<7 && markers!=null) {
                    var p=player.view.WorldToScreenPoint(markers[stage]+Vector3.up*2.8f);
                    if(p.z>0 && p.x>0 && p.x<Screen.width && p.y>0 && p.y<Screen.height) {
                        float x=p.x/Screen.width*1440, y=900-p.y/Screen.height*900;
                        Label(new Rect(x-20,y-22,90,36),"◆ "+stage,title);
                    }
                }
            } else {
                for(int i=1;i<markers.Length;i++) {
                    var p=player.view.WorldToScreenPoint(markers[i]+Vector3.up*1.5f);
                    if(p.z>0) Label(new Rect(p.x/Screen.width*1440-10,900-p.y/Screen.height*900-20,100,40),"● "+i,title);
                }
            }
        }
        if(player.Feedback!="") { Box(new Rect(370,666,700,44),panel); Label(new Rect(390,671,660,40),player.Feedback,body); }
        if(stage==0 || completed || player.paused) {
            Box(new Rect(0,0,1440,900),dim); Box(new Rect(365,230,710,450),panel); Box(new Rect(365,230,710,5),accent);
            Label(new Rect(405,266,630,60),completed?"เมล็ดพันธุ์แห่งวันพรุ่งนี้":player.paused?"พักการเดินทาง":"สวนฝึกแห่งกาลเวลา",big);
            Label(new Rect(405,347,630,180),completed?"Norman มอบเมล็ดพันธุ์วิเศษให้คุณแล้ว\n\nการฝึกเสร็จสมบูรณ์: ดูดพลัง • Rewind • Accelerate\nจุดหมายต่อไปคือเมืองร้าง และต้นเหตุของมลพิษ":player.paused?"กด Esc เพื่อเดินทางต่อ\n\nR เริ่มการฝึกใหม่ / Tab ดูภาพรวมด่าน":"Norman: โลกกำลังสูญเสียชีวิต ถุงมือนี้ช่วยเปลี่ยนเวลาของวัตถุได้ แต่ต้องยืมพลังจากพืช\n\nRobo จะพาคุณฝึกดูดพลัง ย้อนเวลาสะพาน และเร่งเถาวัลย์ ก่อนฉันจะมอบเมล็ดพันธุ์ให้",body);
            var button=new GUIStyle(GUI.skin.button){font=uiFont,fontSize=21};
            if(stage==0 && GUI.Button(new Rect(405,568,630,58),"เริ่มการฝึก  /  ENTER",button)) Begin();
            if(completed) Label(new Rect(405,568,630,58),"รับเมล็ดพันธุ์แล้ว กำลังเดินทางสู่เมืองที่ถูกลืม…",body);
            if(player.paused && GUI.Button(new Rect(405,568,630,58),"เดินทางต่อ  /  ESC",button)) { player.paused=false; Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
        }
    }
}
}
