using UnityEngine;
namespace BorrowedTime {
public class BTFunPrototypeGame : BTTrainingGame {
    public BTTimeTarget pump;
    public Transform valve;
    public GameObject polluted,clean,bloom;
    public bool waterRestored,relapsed,crossed;
    float relapseAt=-1;
    int hint;
    protected override void Start(){InitializeAudio();gameObject.AddComponent<BTPresentation>();advanceToCity=false;Begin();}
    public override void Begin(){stage=1;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;roboLine="ลองทั้งสองทางข้าม: คืนสะพาน หรือเร่งรากไม้ • ต้นไม้ป่วยเพราะอะไร?";}
    protected override void Update(){
        if(BorrowedTime.Menu.GamePauseMenu.BlocksInputFor(player))return;
        if(player.paused)return;
        if(robo)robo.position=Vector3.Lerp(robo.position,player.transform.position+new Vector3(1.2f,2,0),Time.deltaTime*4);
        if(!waterRestored && tree.age==1 && !tree.pollutionStress){if(relapseAt<0)relapseAt=Time.time+6.5f;if(Time.time>=relapseAt){tree.pollutionStress=true;tree.Apply();relapsed=true;roboLine="ต้นไม้ยังวัยเดิม แต่รากป่วยจากน้ำเสีย ตามท่อไปที่วาล์ว";}}
        if(player.transform.position.x>6 && (bridge.age==0 || vine.age==1))crossed=true;
        polluted.SetActive(!waterRestored);clean.SetActive(waterRestored);
        bool healthy=waterRestored && tree.age==1 && !tree.pollutionStress;bloom.SetActive(healthy);
        if(healthy && crossed && !completed){completed=true;PlayTone(1000);roboLine="คุณแก้ต้นเหตุและฟื้นพื้นที่แล้ว! R ทดลองทางข้ามอีกแบบ";Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
    public override void Interact(){if(Vector3.Distance(player.transform.position,valve.position)<3){if(pump.age>1){player.Message("คืนสภาพเครื่องสูบน้ำก่อนเปิดวาล์ว");return;}waterRestored=true;player.Message("หยุดน้ำเสียแล้ว กลับไปฟื้นราก: คืนเป็นต้นอ่อน แล้วเร่งกลับแข็งแรง");PlayTone(800);}}
    public override void TargetChanged(BTTimeTarget t){if(t==tree && waterRestored){t.pollutionStress=false;t.Apply();}}
    public override void EnergyAbsorbed(float amount){}
    public override void ResetTraining(){foreach(var t in FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None))t.ResetTarget();waterRestored=relapsed=crossed=completed=false;relapseAt=-1;hint=0;player.energy=50;player.checkpoint=new Vector3(-9,.15f,-6);player.Respawn();Begin();}
    public override void ShowHint(){string[] h={"สำรวจต้นไม้ข้างท่อสีม่วงและริมคลอง","ต้นไม้ป่วยซ้ำเพราะน้ำเสีย เครื่องสูบอยู่ฝั่งตรงข้าม","คืนสะพานสองขั้น หรือเร่งรากไม้หนึ่งขั้น ข้ามไปซ่อมเครื่องแล้ว F วาล์ว"};roboLine=h[Mathf.Min(hint++,2)];}
    protected override void OnGUI(){
        if(BorrowedTime.Menu.GamePauseMenu.IsPausedFor(player))return;
        var style=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=19,wordWrap=true};GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));GUI.Box(new Rect(24,24,710,150),"");GUI.Label(new Rect(44,38,670,128),"FUN PROTOTYPE / สองทางข้ามและต้นเหตุของมลพิษ\n"+(waterRestored?"● น้ำสะอาด":"○ น้ำเสีย")+"   "+(relapsed?"● เห็นผลมลพิษ":"○ ทดลองคืนต้นไม้")+"\n"+roboLine,style);
        GUI.Label(new Rect(25,834,1390,44),"WASD เดิน • เล็งครอสแฮร์ ล้อถอย คืน / ล้อหน้า เร่ง / LMB ดูด • F วาล์ว • H คำใบ้ • R เริ่มใหม่ • Esc พัก",style);
        if(GUI.Button(new Rect(1100,24,310,52),"กลับหน้าหลัก",new GUIStyle(GUI.skin.button){font=uiFont,fontSize=20}))BTJourney.LoadTraining();
        if(completed)GUI.Label(new Rect(420,350,650,100),"ฟื้นฟูสำเร็จ — R ทดลองอีกทางข้าม",style);
    }
}
}
