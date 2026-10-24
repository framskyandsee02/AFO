using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime {
public class BTCityGame : BTTrainingGame {
    public BTTimeTarget waterMachine,optionalVines,natureCrossing;
    public string crossingMethod="";
    public bool CrossingPassable => bridge.age==0 || (natureCrossing && natureCrossing.age==1);
    public bool optionalWatered;
    public void WaterOptionalGarden() { optionalWatered=true; }
    public Transform waterConsole,factoryConsole,note;
    public GameObject pollutedWater,cleanWater,parkBloom,optionalBloom;
    public bool waterRestored,gardenRestored,factoryOpen,optionalRestored,observedRelapse;
    public Vector3 spawn=new Vector3(-23,.15f,-19);
    public Vector3 factoryDoorClosed=new Vector3(22,2.5f,24);
    public string noteText="Norman Scott / สมุดภาคสนาม\nโรงงานปล่อยของเสียลงท่อเก่าของเมือง\nปิดท่อน้ำเสียได้ช่วยย่านนี้ แต่ต้นเหตุยังอยู่หลังประตูโรงงาน\nเมล็ดพันธุ์จะเติบโตอย่างยั่งยืน เมื่อเราหยุดต้นเหตุเท่านั้น";
    public bool noteOpen;
    int noteOpenedFrame;
    float relapseAt=-1,gateLift;
    float factoryTransitionAt;
    bool factoryLoading;
    int hints,lastStage=-1;
    GUIStyle heading,text,tiny,large;
    Texture2D background,bar,shade;
    protected override void Start() {
        audioSource=gameObject.AddComponent<AudioSource>();
        if(!GetComponent<BTPresentation>()) gameObject.AddComponent<BTPresentation>();
        hasMiracleSeed=BTJourney.HasMiracleSeed;
        player.energy=BTJourney.FromTraining?BTJourney.CarriedEnergy:50;
        advanceToCity=false;
        if(BTJourney.FromTraining) Begin();
        else { stage=0; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        ApplyRestoration();
    }
    public override void Begin() {
        stage=1; lastStage=-1; SetGoal(); Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
    }
    protected override void Update() {
        var k=Keyboard.current;
        if(stage==0 && k!=null && k.enterKey.wasPressedThisFrame) Begin();
        if(noteOpen && Time.frameCount>noteOpenedFrame && k!=null && (k.fKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame)) CloseNote();
        if(player.paused || stage==0) return;
        if(completed && !factoryLoading && Time.time>=factoryTransitionAt) {
            factoryLoading=true; BTJourney.LoadFactory(player.energy,waterRestored,gardenRestored); return;
        }
        if(robo) {
            Vector3 follow=player.transform.position+Quaternion.Euler(0,player.yaw,0)*new Vector3(1.25f,2.1f,-.35f);
            follow.y+=Mathf.Sin(Time.time*2.8f)*.13f;
            robo.position=Vector3.Lerp(robo.position,follow,Time.deltaTime*4);
            robo.rotation=Quaternion.Slerp(robo.rotation,Quaternion.Euler(0,player.yaw+180,0),Time.deltaTime*5);
        }
        if(!waterRestored && tree.age==1 && !tree.pollutionStress) {
            if(relapseAt<0) { relapseAt=Time.time+6.5f; roboLine="Robo: ต้นไม้กลับมาเขียวแล้ว… แต่ดูสิ น้ำที่รากยังเป็นสีม่วงอยู่"; }
            if(Time.time>=relapseAt) {
                tree.pollutionStress=true; tree.Apply(); observedRelapse=true; relapseAt=-1;
                player.Message("ต้นไม้เหี่ยวซ้ำ: ต้องแก้ท่อน้ำเสียที่ต้นเหตุ");
            }
        } else relapseAt=-1;
        bool restoredNow=waterRestored && tree.age==1 && !tree.pollutionStress;
        if(gardenRestored!=restoredNow) { gardenRestored=restoredNow; ApplyRestoration(); }
        if(factoryOpen) gateLift=Mathf.MoveTowards(gateLift,6,Time.deltaTime*2.5f);
        if(door) door.position=factoryDoorClosed+Vector3.up*gateLift;
        if(!completed && factoryOpen && player.transform.position.z>25.5f && Mathf.Abs(player.transform.position.x-22)<3) {
            completed=true; stage=6; BTJourney.OptionalGardenRestored=optionalRestored;
            factoryTransitionAt=Time.time+1.8f;
            roboLine="Robo: ทางเข้าสู่โรงงานเปิดแล้ว ต้นเหตุของมลพิษอยู่ข้างใน";
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        }
        if(!completed) SetGoal();
        if(player.transform.position.x>6 && CrossingPassable) { player.checkpoint=new Vector3(9,.15f,8); if(Mathf.Abs(player.transform.position.z-4.5f)<1.7f && natureCrossing && natureCrossing.age==1)crossingMethod="Nature"; else if(Mathf.Abs(player.transform.position.z-8)<1.5f && bridge.age==0)crossingMethod="Bridge"; }
        else if(waterRestored && player.transform.position.x<-4 && player.transform.position.z>4) player.checkpoint=new Vector3(-8,.15f,8);
    }
    void SetGoal() {
        int next=!waterRestored?(observedRelapse?2:1):!gardenRestored?3:!CrossingPassable?4:5;
        stage=next;
        if(lastStage==stage) return;
        lastStage=stage; hints=0;
        switch(stage) {
            case 1: roboLine="Robo: เมืองนี้เงียบจัง… ลองคืนสภาพต้นไม้กลางสวน เราจะหาว่าอะไรทำให้มันป่วย"; break;
            case 2: roboLine="Robo: คืนสภาพอย่างเดียวช่วยไม่ได้ ท่อน้ำเสียยังไหลเข้าสวนอยู่ ลองตามท่อไปที่สถานีควบคุมน้ำ"; break;
            case 3: roboLine="Robo: น้ำสะอาดแล้ว แต่รากยังป่วย ลองคืนให้เป็นต้นอ่อน แล้วเร่งกลับโตเต็มวัย คราวนี้จะฟื้นได้ยั่งยืน"; break;
            case 4: roboLine="Robo: ริมคลองมีทั้งสะพานพังและรากไม้ เลือกคืนสภาพสะพาน หรือเร่งรากไม้ให้เป็นทางข้ามได้"; break;
            case 5: roboLine="Robo: ประตูยังปิดอยู่ เร่งสภาพสิ่งกีดขวางจนสลาย แล้วกด F ที่แผงควบคุม"; break;
        }
    }
    public override void EnergyAbsorbed(float amount) { }
    public override void TargetChanged(BTTimeTarget t) {
        if(t==waterMachine && t.age<=1) roboLine="Robo: เครื่องควบคุมทำงานแล้ว เข้าใกล้วาล์ว แล้วกด F เพื่อแยกน้ำเสียออกจากสวน";
        if(t==tree) { if(waterRestored) { tree.pollutionStress=false; tree.Apply(); } else if(t.age==1) relapseAt=-1; }
        if(t==natureCrossing && t.age==1) player.Message("รากไม้เป็นทางข้ามแล้ว เดินผ่านได้ทั้งสองทิศทาง");
        if(t==bridge && t.age==0) player.Message("สะพานกลับมาสมบูรณ์ เดินข้ามได้แล้ว");
        if(t==optionalVines && t.age==2) roboLine="Robo: เถาวัลย์เปิดทางแล้ว มีบันทึกของ Norman อยู่ในสวนเล็ก";
    }
    public override void Interact() {
        if(noteOpen) { CloseNote(); return; }
        if(Vector3.Distance(player.transform.position,waterConsole.position)<3) {
            if(waterMachine.age>1) { player.Message("เครื่องควบคุมพังอยู่ เล็งแล้วเลื่อนล้อเมาส์ถอยเพื่อคืนสภาพก่อน"); return; }
            if(!waterRestored) { waterRestored=true; PlayTone(700); ApplyRestoration(); player.Message("ปิดท่อน้ำเสีย และเปิดน้ำสะอาดให้ย่านสวนแล้ว"); }
            else player.Message("วาล์วน้ำเสียปิดแล้ว น้ำสะอาดกำลังไหลเข้าสวน");
            return;
        }
        if(Vector3.Distance(player.transform.position,note.position)<2.7f) {
            if(optionalVines.age!=2) { player.Message("เถาวัลย์ขวางสวนเล็กอยู่ เร่งสภาพให้แห้งสลายก่อน"); return; }
            if(!optionalWatered) { optionalWatered=true; player.Message("เปิดน้ำให้สวนเล็กแล้ว กด F อีกครั้งเพื่อฟื้นพื้นที่และอ่านบันทึก"); return; }
            optionalRestored=true; BTJourney.OptionalGardenRestored=true; ApplyRestoration(); BTPresentation.Burst(note.position+Vector3.up,Color.green,24);
            noteOpen=true; noteOpenedFrame=Time.frameCount; player.paused=true; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; return;
        }
        if(Vector3.Distance(player.transform.position,factoryConsole.position)<3.2f) {
            if(!gardenRestored || !waterRestored || !CrossingPassable) { player.Message("ยังเปิดไม่ได้: ต้องฟื้นสวน แก้ระบบน้ำ และเปิดทางข้ามคลองก่อน"); return; }
            if(seal.age!=2) { player.Message("สิ่งกีดขวางยังไม่สลาย เล็งแล้วดันล้อเมาส์ไปหน้าสองขั้น"); return; }
            if(!BTExpansionWorld.CanLeave(this,out string pending)){player.Message(pending);return;}
            factoryOpen=true; if(crossingMethod=="")crossingMethod=bridge.age==0?"Bridge":"Nature"; BTJourney.CrossingMethod=crossingMethod; PlayTone(850); player.Message("ประตูเปิดแล้ว เดินผ่านเพื่อเข้าโรงงาน หรือกลับไปฟื้นสวนเล็กก่อนได้"); return;
        }
        player.Message("F ใช้งานวาล์วน้ำ อ่านบันทึก หรือเปิดแผงควบคุมประตูเมื่อเข้าใกล้");
    }
    public void CloseNote() { noteOpen=false; player.paused=false; Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
    public void RefreshAfterLoad(){ lastStage=-1;SetGoal();ApplyRestoration(); if(door)door.position=factoryDoorClosed+Vector3.up*(factoryOpen?6:0); }
    void ApplyRestoration() {
        if(pollutedWater) pollutedWater.SetActive(!waterRestored);
        if(cleanWater) cleanWater.SetActive(waterRestored);
        if(parkBloom) parkBloom.SetActive(gardenRestored);
        if(optionalBloom) optionalBloom.SetActive(optionalRestored);
    }
    public override void ResetTraining() {
        var expansion=GetComponent<BTExpansionWorld>();if(expansion)expansion.ResetWorld();
        foreach(var t in FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None)) t.ResetTarget();
        waterRestored=gardenRestored=factoryOpen=optionalRestored=observedRelapse=completed=noteOpen=factoryLoading=false;
        BTJourney.OptionalGardenRestored=false; optionalWatered=false; crossingMethod=""; relapseAt=-1; gateLift=0; lastStage=-1;
        player.energy=BTJourney.FromTraining?BTJourney.CarriedEnergy:50; player.walked=0; player.yaw=0; player.pitch=20; player.overview=player.paused=false;
        player.checkpoint=spawn; player.Respawn(); stage=1; SetGoal(); ApplyRestoration();
        if(door) door.position=factoryDoorClosed;
        Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
    }
    public override void ShowHint() {
        string[,] lines={
            {"สำรวจสวนกลางเมือง","มองหาต้นไม้ที่ไม่มีใบ","RMB เล็งต้นไม้ แล้วเลื่อนล้อเมาส์ถอยหนึ่งขั้น"},
            {"ตามท่อน้ำสีม่วงไปทางเหนือของสวน","เครื่องควบคุมอยู่หน้าตึกประปา","คืนสภาพเครื่องด้วยล้อเมาส์ถอย แล้วกด F ที่วาล์ว"},
            {"กลับไปที่สวน","คืนสภาพต้นไม้หลังแก้น้ำเสีย","คืนให้เป็นต้นอ่อน แล้วเร่งกลับโตเต็มวัย เพื่อฟื้นรากหลังแก้น้ำเสีย"},
            {"เดินจากสถานีประปาไปริมคลอง","มีสะพานพังและรากไม้ริมคลอง","เลือก: คืนสะพานสองขั้น หรือเร่งรากไม้หนึ่งขั้น แล้วเดินข้าม"},
            {"ประตูโรงงานอยู่ปลายถนนฝั่งตะวันออก","สิ่งกีดขวางหน้าโรงงานต้องแก่จนสลาย","RMB + ดันล้อเมาส์ไปหน้าสองครั้ง แล้วกด F ที่แผงควบคุม"}};
        roboLine="Robo: "+lines[Mathf.Clamp(stage-1,0,4),Mathf.Min(2,hints++)];
    }
    Texture2D Solid(Color c) { var t=new Texture2D(1,1); t.SetPixel(0,0,c); t.Apply(); return t; }
    void Init() {
        if(heading!=null) return;
        heading=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=24,fontStyle=FontStyle.Bold,wordWrap=true}; heading.normal.textColor=Color.white;
        text=new GUIStyle(heading){fontSize=18,fontStyle=FontStyle.Normal}; tiny=new GUIStyle(text){fontSize=14}; tiny.normal.textColor=new Color(.77f,.9f,.87f);
        large=new GUIStyle(heading){fontSize=35};
        background=Solid(new Color(.035f,.11f,.14f,.94f)); bar=Solid(new Color(.5f,.94f,.66f)); shade=Solid(new Color(.015f,.04f,.055f,.76f));
    }
    void Panel(Rect r) { GUI.DrawTexture(r,background); }
    void Label(Rect r,string s,GUIStyle style) { GUI.Label(r,s,style); }
    protected override void OnGUI() {
        if(player.hideHud) return; Init(); GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
        Panel(new Rect(24,24,650,132)); GUI.DrawTexture(new Rect(24,24,5,132),bar);
        Label(new Rect(44,35,605,34),"02 / THE FORGOTTEN CITY",heading);
        Label(new Rect(44,70,605,26),"เมืองที่ถูกลืม  •  ฟื้นฟูย่านสวน และเปิดทางสู่โรงงาน",tiny);
        string[] goals={"สำรวจเมือง","คืนสภาพต้นไม้กลางสวน","ตามท่อน้ำเสียไปที่ระบบประปา","กลับมาฟื้นสวนหลังหยุดน้ำเสีย","เลือกข้ามคลอง: สะพาน หรือรากไม้","เปิดประตูโรงงาน","เปิดทางสู่โรงงานแล้ว"};
        Label(new Rect(44,108,605,42),goals[Mathf.Clamp(stage,0,6)],text);
        Panel(new Rect(1080,24,336,228));
        Label(new Rect(1098,35,295,28),"TIME GAUNTLET   "+Mathf.RoundToInt(player.energy)+" / 100",text);
        GUI.DrawTexture(new Rect(1100,73,290*player.energy/100,12),bar);
        Label(new Rect(1098,98,300,32),(gardenRestored?"●":"○")+" สวนฟื้นตัว   "+(waterRestored?"●":"○")+" น้ำสะอาด",text);
        Label(new Rect(1098,136,300,32),(CrossingPassable?"●":"○")+" ทางข้าม   "+(factoryOpen?"●":"○")+" ประตูโรงงาน",text);
        Label(new Rect(1098,176,300,32),hasMiracleSeed?"◆ เมล็ดพันธุ์: นำมาจากด่าน 1":"◇ เปิดฉากตรง: ยังไม่ได้รับเมล็ด",tiny);
        Label(new Rect(1098,208,300,28),optionalRestored?"★ สวนเล็ก / บันทึก Norman สำเร็จ":"☆ สวนเล็กและบันทึก (ทางเลือก)",tiny);
        if(stage>0 && !completed) {
            Panel(new Rect(24,730,1392,88)); Label(new Rect(44,741,1300,30),"ROBO",tiny); Label(new Rect(44,774,1345,42),roboLine,text);
            Label(new Rect(26,846,1400,40),"WASD เดิน   Shift วิ่ง   Space กระโดด   RMB เล็ง + LMB ดูด / ล้อถอย คืนสภาพ / ล้อหน้า เร่งสภาพ   F ใช้งาน   H คำใบ้   Tab แผนที่   R รีเซ็ตด่าน   Esc พัก",tiny);
            if(!player.overview) {
                Label(new Rect(708,435,32,32),"+",heading);
                if(player.target) {
                    Panel(new Rect(475,558,490,94)); Label(new Rect(495,567,450,36),player.target.displayName,heading);
                    Label(new Rect(495,610,450,36),player.target.StateName+(player.target.IsLiving?"  /  พลังชีวิต "+Mathf.RoundToInt(player.target.life)+"%":""),text);
                }
            }
            if(markers!=null && markers.Length>stage) {
                var p=player.view.WorldToScreenPoint(markers[Mathf.Clamp(stage,1,5)]+Vector3.up*2.8f);
                if(p.z>0 && p.x>0 && p.x<Screen.width && p.y>0 && p.y<Screen.height) Label(new Rect(p.x/Screen.width*1440-18,900-p.y/Screen.height*900-18,100,40),"◆ "+stage,heading);
            }
            if(player.overview) for(int i=1;i<markers.Length;i++) {
                var p=player.view.WorldToScreenPoint(markers[i]+Vector3.up*2);
                if(p.z>0) Label(new Rect(p.x/Screen.width*1440-10,900-p.y/Screen.height*900-18,120,40),"● "+i,heading);
            }
        }
        if(player.Feedback!="") { Panel(new Rect(330,668,780,48)); Label(new Rect(350,674,740,42),player.Feedback,text); }
        if(stage==0 || completed || player.paused || noteOpen) {
            GUI.DrawTexture(new Rect(0,0,1440,900),shade); Panel(new Rect(340,210,760,470)); GUI.DrawTexture(new Rect(340,210,760,5),bar);
            Label(new Rect(380,250,680,70),noteOpen?"บันทึกของ Norman Scott":completed?"เมืองเริ่มกลับมามีชีวิต":player.paused?"พักการเดินทาง":"เมืองที่ถูกลืม",large);
            string message=noteOpen?noteText:completed?"แก้ระบบน้ำ ฟื้นสวน ซ่อมสะพาน และเปิดประตูโรงงานแล้ว\n\n"+(hasMiracleSeed?"เมล็ดพันธุ์ยังอยู่กับคุณ พร้อมสำหรับภารกิจในโรงงาน":"โหมดเปิดฉากตรง: เริ่มจากด่าน 1 เพื่อรับเมล็ดพันธุ์")+"\n"+(optionalRestored?"★ คุณฟื้นสวนเล็กและได้อ่านบันทึกของ Norman":"สวนเล็กและบันทึกของ Norman ยังเป็นทางเลือกที่กลับมาสำรวจได้")+"\n\nกำลังเข้าสู่โรงงานแห่งต้นตอ":player.paused?"Esc เพื่อเล่นต่อ / R รีเซ็ตด่าน\n\nพืชให้พลังชีวิตได้ แต่ไม่สามารถถูกดูดจนตาย":"ถนนร้าง คลองสีม่วง และสวนที่เหี่ยวเฉา\n\nคืนสภาพอาจช่วยสิ่งของได้ แต่ธรรมชาติจะฟื้นอย่างยั่งยืน\nก็ต่อเมื่อเราแก้ต้นเหตุของมลพิษ\n\nRMB ค้าง + เลื่อนล้อเมาส์ถอย: คืนสภาพ\nRMB ค้าง + ดันล้อเมาส์ไปหน้า: เร่งสภาพ";
            Label(new Rect(380,338,680,225),message,text);
            var style=new GUIStyle(GUI.skin.button){font=uiFont,fontSize=20};
            if(stage==0 && GUI.Button(new Rect(380,592,680,52),"เริ่มสำรวจ / ENTER",style)) Begin();
            else if(noteOpen && GUI.Button(new Rect(380,592,680,52),"เก็บบันทึกและสำรวจต่อ / F",style)) CloseNote();
            else if(completed) Label(new Rect(380,592,680,52),"กำลังเข้าสู่โรงงาน…",text);
            else if(player.paused && GUI.Button(new Rect(380,592,680,52),"สำรวจต่อ / ESC",style)) { player.paused=false; Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
        }
    }
}
}
