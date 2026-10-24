using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime {
public class BTFactoryGame : BTTrainingGame {
    public BTTimeTarget entryControl,liftControl,conveyor,powerCoupler,coreConsole,nursery,miracleTree;
    public BTTimeTarget[] entryRelays,boilers;
    public BTFactoryLift freightLift,coreLift;
    public Transform entryPanel,freightButton,conveyorPanel,boilerPanel,powerPanel,shutdownPanel,waterPanel,coreLiftButton,plantPoint;
    public Transform[] shutoffValves;
    public bool[] valvesClosed=new bool[3];
    public Transform entryDoor,conveyorDoor,pressureDoor;
    public GameObject ringCrossing,steam,dirtyWater,cleanWater,restoredNursery,wideRestoration;
    public Renderer[] reactorRenderers;
    public Material reactorDanger,reactorSafe;
    public Light[] reactorLights;
    public bool entryOpen,transportRestored,conveyorOpen,pressureBalanced,powerLatched,pollutionStopped,waterFlowRestored,seedPlanted,nurseryRestored;
    public string ending="";
    public Vector3 spawn=new Vector3(0,.15f,-25);
    public bool funFirst=true;
    public BTTimeTarget emergencyWalkway;
    public GameObject[] smokestacks;
    public float tensionUntil;
    public bool GoodEnding => ending=="Hopeful Renewal";
    bool entryHadSeed;
    bool freightReached;
    float entryEnergy,entryLift,conveyorLift,pressureLift;
    int lastStage=-1,factoryHintTier;
    GUIStyle heading,factoryBody,factorySmall,large;
    Texture2D factoryPanel,tint,shade;
    protected override void Start() {
        InitializeAudio();
        if(!GetComponent<BTPresentation>()) gameObject.AddComponent<BTPresentation>();
        entryHadSeed=BTJourney.HasMiracleSeed; entryEnergy=BTJourney.FromCity?BTJourney.CarriedEnergy:50;
        hasMiracleSeed=entryHadSeed; player.energy=entryEnergy; advanceToCity=false;
        if(BTJourney.FromCity) Begin();
        else { stage=0; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        ApplyFactoryState();
    }
    public override void Begin() { stage=1; lastStage=-1; Goal(); Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
    protected override void Update() {
        if(BorrowedTime.Menu.GamePauseMenu.BlocksInputFor(player))return;
        if(stage==0 && Keyboard.current!=null && Keyboard.current.enterKey.wasPressedThisFrame) Begin();
        if(stage==0 || player.paused) return;
        if(robo) {
            Vector3 p=player.transform.position+Quaternion.Euler(0,player.yaw,0)*new Vector3(1.2f,2.05f,-.35f); p.y+=Mathf.Sin(Time.time*2.8f)*.12f;
            robo.position=Vector3.Lerp(robo.position,p,Time.deltaTime*4); robo.rotation=Quaternion.Slerp(robo.rotation,Quaternion.Euler(0,player.yaw+180,0),Time.deltaTime*5);
        }
        entryLift=Mathf.MoveTowards(entryLift,entryOpen?5:0,Time.deltaTime*2.6f);
        conveyorLift=Mathf.MoveTowards(conveyorLift,conveyorOpen?5:0,Time.deltaTime*2.6f);
        pressureLift=Mathf.MoveTowards(pressureLift,pressureBalanced?5:0,Time.deltaTime*2.6f);
        entryDoor.position=new Vector3(0,2+entryLift,-18);
        conveyorDoor.position=new Vector3(16,8+conveyorLift,2.5f);
        pressureDoor.position=new Vector3(16,8+pressureLift,19);
        if(bridge.age==0) transportRestored=true;
        if(player.transform.position.y>5.8f) freightReached=true;
        nurseryRestored=pollutionStopped && waterFlowRestored && nursery.age==1;
        if(restoredNursery) restoredNursery.SetActive(nurseryRestored);
        if(!completed && seedPlanted && miracleTree.age==2) FinishEnding();
        if(!completed) Goal();
        if(player.transform.position.y>11.8f && player.transform.position.z>21) player.checkpoint=new Vector3(-15,12.1f,24);
        else if(powerLatched && player.transform.position.x<-8 && player.transform.position.z>22 && player.transform.position.y>5.8f) player.checkpoint=new Vector3(-12,6.1f,25);
        else if(conveyorOpen && player.transform.position.x>9 && player.transform.position.z>4 && player.transform.position.z<18 && player.transform.position.y>5.8f) player.checkpoint=new Vector3(16,6.1f,4.8f);
        else if(player.transform.position.x<-13 && player.transform.position.z<8 && player.transform.position.y>5.8f) player.checkpoint=new Vector3(-18,6.1f,6);
        else if(transportRestored && player.transform.position.x<-10 && player.transform.position.z>1 && player.transform.position.y<1) player.checkpoint=new Vector3(-18,.1f,3);
    }
    void Goal() {
        int next=!entryOpen?(player.walked>2?2:1):!transportRestored?3:!freightReached?4:!conveyorOpen?5:!pressureBalanced?6:(!powerLatched || (emergencyWalkway && emergencyWalkway.age==2))?7:!pollutionStopped || !waterFlowRestored?8:9;
        stage=next; if(stage==lastStage) return; lastStage=stage; factoryHintTier=0;
        string[] lines={"","Robo: ที่นี่คือต้นเหตุของมลพิษ เราจะหยุดมัน ก่อนปลูกเมล็ดพันธุ์","Robo: คืนสภาพแผงควบคุมโถงเข้าแล้วกด F รีเลย์อื่นเป็นเครื่องจักรเสริม","Robo: ทางขนส่งขาดอยู่ เลื่อนล้อเมาส์ถอยเพื่อคืนสภาพทีละขั้น","Robo: ระบบลิฟต์เสีย คืนสภาพแผงลิฟต์ แล้วขึ้นแท่นและกด F","Robo: ลังขวางสายพานอยู่ เร่งสภาพระบบให้ลังไปถึงช่องพัก แล้วเปิดทาง","Robo: หม้อไอน้ำรั่วสามจุด คืนสภาพทั้งหมด แล้วใช้แผงระบายแรงดัน","Robo: ทางเดินวงแหวนขาดอยู่ สังเกตชิ้นส่วนที่ย้อนเวลาได้ แล้วข้ามไปห้องควบคุม","Robo: ดูไฟแสดงทิศทาง ซ่อมแผงหลักแล้วกด F เพื่อหยุดการปล่อยมลพิษ น้ำสะอาดที่เราฟื้นในเมืองจะไหลเข้ามา","Robo: ถึงเวลาคืนชีวิตแล้ว ขึ้นลิฟต์สู่ห้องแกนกลาง ปลูกเมล็ดและเร่งให้เติบโต"};
        roboLine=lines[next];
    }
    bool AllRelays() { foreach(var t in entryRelays) if(t.age!=0) return false; return true; }
    bool AllBoilers() { foreach(var t in boilers) if(t.age!=0) return false; return true; }
    bool AllValves() { foreach(bool b in valvesClosed) if(!b) return false; return true; }
    bool Near(Transform t,float distance=2.7f) => t && Vector3.Distance(player.transform.position,t.position)<distance;
    public override void EnergyAbsorbed(float amount) { }
    public override void TargetChanged(BTTimeTarget t) {
        if(t==bridge && t.age==0) transportRestored=true;
        if(t==miracleTree && t.age>0) player.Message("เมล็ดพันธุ์เริ่มเติบโตแล้ว");
    }
    public override void Interact() {
        if(Near(entryPanel,3)) {
            if(entryControl.age>1 || (!funFirst && !AllRelays())) { player.Message("คืนสภาพแผงควบคุม และรีเลย์สีฟ้าทั้งสองจุดก่อน"); return; }
            entryOpen=true; PlayTone(720); player.Message("เปิดประตูโถงขนส่งแล้ว"); return;
        }
        if(Near(freightButton,3) || Vector3.Distance(player.transform.position,freightButton.position+Vector3.up*6)<3 || freightLift.PlayerOnPlatform()) {
            if(!entryOpen || !transportRestored || liftControl.age>1) { player.Message("คืนสภาพแผงลิฟต์ และซ่อมทางขนส่งก่อน"); return; }
            freightLift.Toggle(); player.Message("ลิฟต์ขนส่งกำลังเคลื่อนที่"); return;
        }
        if(Near(conveyorPanel,3)) {
            if(conveyor.age!=2) { player.Message("เร่งสภาพระบบสายพานจนลังถึงช่องพักก่อน"); return; }
            conveyorOpen=true; if(funFirst) { pressureBalanced=true;powerLatched=true;tensionUntil=Time.time+22;ApplyFactoryState();player.checkpoint=new Vector3(12,6.1f,-11); } player.Message("สายพานเปิดทางเข้าห้องหม้อไอน้ำแล้ว"); PlayTone(760); return;
        }
        if(Near(boilerPanel,3)) {
            if(!AllBoilers()) { player.Message("ยังมีหม้อไอน้ำรั่ว คืนสภาพทั้งสามจุดให้สมบูรณ์"); return; }
            pressureBalanced=true; ApplyFactoryState(); PlayTone(760); player.Message("แรงดันสมดุล เปิดห้องพลังงานแล้ว"); return;
        }
        if(Near(powerPanel,3)) {
            if(!pressureBalanced || powerCoupler.age!=2) { player.Message("ต้องระบายแรงดัน และเชื่อมวงจรพลังงานให้เสถียรก่อน"); return; }
            powerLatched=true; ApplyFactoryState(); PlayTone(820); player.Message("สะพานวงแหวนเปิดแล้ว"); return;
        }
        for(int i=0;i<shutoffValves.Length;i++) if(Near(shutoffValves[i],2.5f)) {
            if(!powerLatched) { player.Message("ห้องควบคุมยังไม่มีไฟ"); return; }
            valvesClosed[i]=true; player.Message("ปิดวาล์วมลพิษ "+(i+1)+" / 3 แล้ว"); PlayTone(580+i*80); return;
        }
        if(Near(shutdownPanel,2.7f)) {
            if(!powerLatched || (!funFirst && !AllValves()) || coreConsole.age>1) { player.Message("ปิดวาล์วสามจุด และคืนสภาพแผงควบคุมหลักก่อน"); return; }
            pollutionStopped=true; if(funFirst && BTJourney.CityWaterRestored)waterFlowRestored=true; BTJourney.PollutionStopped=true; ApplyFactoryState(); PlayTone(900); player.Message("หยุดการปล่อยมลพิษจากโรงงานแล้ว"); return;
        }
        if(Near(waterPanel,2.7f)) {
            if(!pollutionStopped) { player.Message("ต้องหยุดการปล่อยมลพิษก่อนเปิดน้ำเข้าห้องปลูก"); return; }
            waterFlowRestored=true; ApplyFactoryState(); player.Message("ฟื้นระบบน้ำสะอาดเข้าสู่ห้องแกนกลางแล้ว"); return;
        }
        if(Near(coreLiftButton,3) || Vector3.Distance(player.transform.position,coreLiftButton.position+Vector3.up*6)<3 || coreLift.PlayerOnPlatform()) {
            if(!pollutionStopped || !waterFlowRestored) { player.Message("ห้องปลูกยังไม่ปลอดภัย หยุดมลพิษและเปิดน้ำสะอาดก่อน"); return; }
            coreLift.Toggle(); player.Message("ลิฟต์แกนกลางกำลังเคลื่อนที่"); return;
        }
        if(Near(plantPoint,3)) {
            if(seedPlanted) { player.Message("เล็งผลึกต้นไม้ แล้วดันล้อเมาส์ไปหน้าเพื่อเร่งการเติบโต"); return; }
            if(!CanPlant(out string reason)) { player.Message(reason); return; }
            player.energy-=30; seedPlanted=true; hasMiracleSeed=false; BTJourney.HasMiracleSeed=false; BTJourney.SeedPlanted=true;
            miracleTree.gameObject.SetActive(true); miracleTree.Apply(); PlayTone(1050); player.Message("ปลูกเมล็ดแล้ว เร่งสภาพอีกสองขั้นให้เติบโต"); return;
        }
        player.Message("F ใช้งานแผงควบคุม วาล์ว ลิฟต์ หรือปลูกเมล็ดเมื่อเข้าใกล้");
    }
    public bool CanPlant(out string reason) {
        if(!BTExpansionWorld.CanLeave(this,out reason))return false;
        if(!hasMiracleSeed) { reason="ยังไม่มีเมล็ดพันธุ์ เริ่มจากด่านฝึกและเดินทางผ่านเมืองก่อน"; return false; }
        if(!pollutionStopped || !waterFlowRestored) { reason="ปลูกไม่ได้: ต้องหยุดมลพิษและฟื้นน้ำสะอาดก่อน"; return false; }
        if(player.energy<30) { reason="ต้องมีพลัง 30 หน่วยสำหรับปลูก เติมพลังจากพืชในห้องก่อน"; return false; }
        reason="พร้อมปลูก"; return true;
    }
    void FinishEnding() {
        completed=true; BTJourney.FinalNurseryRestored=nurseryRestored;
        ending=BTJourney.OptionalGardenRestored && nurseryRestored?"Hopeful Renewal":"Fragile Tomorrow";
        BTJourney.Ending=ending; if(wideRestoration) wideRestoration.SetActive(GoodEnding);
        roboLine=GoodEnding?"Robo: การดูแลทั้งสวนเล็กและโรงเพาะกล้า ช่วยให้โลกฟื้นตัวได้กว้างไกล":"Robo: เราหยุดต้นเหตุได้แล้ว แต่ยังมีพื้นที่ที่ต้องดูแล การฟื้นฟูเพิ่งเริ่มต้น";
        PlayTone(GoodEnding?1150:820); Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
    }
    public void RefreshAfterLoad(float[] heights){ freightReached=transportRestored && player.checkpoint.y>5;entryHadSeed=hasMiracleSeed||seedPlanted;entryEnergy=50; foreach(var lift in new[]{freightLift,coreLift})lift.ResetLift(); if(heights!=null){freightLift.platform.position=new Vector3(-18,heights[0]>3?6:0,10);coreLift.platform.position=new Vector3(-20,heights[1]>9?12:6,20);}miracleTree.gameObject.SetActive(seedPlanted);if(wideRestoration)wideRestoration.SetActive(GoodEnding);lastStage=-1;if(!completed)Goal();ApplyFactoryState(); }
    void ApplyFactoryState() {
        if(ringCrossing) ringCrossing.SetActive(powerLatched);
        if(emergencyWalkway)emergencyWalkway.Apply();
        if(smokestacks!=null)foreach(var g in smokestacks)if(g)g.SetActive(!pollutionStopped);
        if(steam) steam.SetActive(!pressureBalanced);
        if(dirtyWater) dirtyWater.SetActive(!waterFlowRestored);
        if(cleanWater) cleanWater.SetActive(waterFlowRestored);
        if(restoredNursery) restoredNursery.SetActive(nurseryRestored);
        foreach(var r in reactorRenderers) if(r) r.sharedMaterial=pollutionStopped?reactorSafe:reactorDanger;
        foreach(var l in reactorLights) if(l) { l.color=pollutionStopped?new Color(.3f,.85f,1):new Color(1,.24f,.12f); l.intensity=pollutionStopped?2:3; }
    }
    public override void ResetTraining() {
        var expansion=GetComponent<BTExpansionWorld>();if(expansion)expansion.ResetWorld();
        foreach(var t in FindObjectsByType<BTTimeTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None)) t.ResetTarget();
        entryOpen=transportRestored=freightReached=conveyorOpen=pressureBalanced=powerLatched=pollutionStopped=waterFlowRestored=seedPlanted=nurseryRestored=completed=false;
        for(int i=0;i<valvesClosed.Length;i++) valvesClosed[i]=false;
        entryLift=conveyorLift=pressureLift=0; tensionUntil=0; ending=""; lastStage=-1;
        hasMiracleSeed=entryHadSeed; BTJourney.HasMiracleSeed=entryHadSeed; BTJourney.SeedPlanted=BTJourney.PollutionStopped=BTJourney.FinalNurseryRestored=false; BTJourney.Ending="";
        freightLift.ResetLift(); coreLift.ResetLift(); miracleTree.gameObject.SetActive(false); if(wideRestoration) wideRestoration.SetActive(false);
        player.energy=entryEnergy; player.walked=0; player.yaw=0; player.pitch=20; player.paused=player.overview=false; player.checkpoint=spawn; player.Respawn();
        stage=1; Goal(); ApplyFactoryState(); Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
    }
    public override void ShowHint() {
        string[] hints={"","สำรวจโถงเข้าและเติมพลังจากพืชในกระถาง","สำรวจแผงโถงเข้า คืนสภาพแล้วกด F เพื่อเปิดทางไปลิฟต์","คืนสภาพสะพานทางขนส่งด้วยล้อเมาส์ถอยสองครั้ง","คืนสภาพแผงลิฟต์ จากนั้นยืนกลางแท่นและกด F","ดันล้อเมาส์ไปหน้าที่ระบบสายพานสองขั้น แล้วกด F ที่แผง","คืนสภาพหม้อไอน้ำทั้งสามให้สมบูรณ์ แล้วกด F ที่แผงระบาย","คืนทางเดินวงแหวนอย่างน้อยหนึ่งขั้นด้วยล้อเมาส์ถอย ให้มีพื้นเดินข้ามได้","คืนสภาพแผงหลัก แล้วกด F หยุดโรงงาน น้ำสะอาดเชื่อมมาจากเมือง","ขึ้นลิฟต์แกนกลาง เติมพลังอย่างน้อย 60 ปลูกด้วย F และเร่งต้นไม้สองขั้น"};
        string[] areas={"","สำรวจโถงรับสินค้า","สังเกตแผงหน้าประตู","ดูช่องว่างในทางขนส่ง","สำรวจฐานลิฟต์","มองหาพื้นที่ปลอดภัยหลังสายพาน","เดินตามทางบริการข้างหม้อไอน้ำ","ห้องควบคุมอยู่ฝั่งตรงข้ามวงแหวน","อ่านไฟสถานะที่แผงควบคุมหลัก","สำรวจห้องปลูก มีโรงเพาะกล้าทางเลือกที่ช่วยให้พื้นที่ฟื้นกว้างขึ้น"};
        string[] objects={"","พืชในกระถางให้พลังได้","แผงหลักมีผลึกสีฟ้า รีเลย์อื่นเป็นเครื่องเสริม","สะพานมีชิ้นส่วนสองช่วงเวลาที่เสียหาย","แผงซ่อมอยู่ใกล้แท่น กด F บนแท่นได้","ตัวควบคุมสายพานเคลื่อนลังไปช่องพัก","เครื่องจักรเสริมไม่บังคับ เปิดประตูต่อไปได้แล้ว","ผลึกสีฟ้าควบคุมทางเดินวงแหวน","เครื่องควบคุมหลักชำรุด แต่ระบบน้ำที่เมืองพร้อมแล้ว","เติมพลังจากพืช ปลูกที่แท่นกลางห้อง โรงเพาะกล้าเป็นทางเลือก"};
        int tier=Mathf.Min(2,factoryHintTier++);int at=Mathf.Clamp(stage,1,9);roboLine="Robo: "+(tier==0?areas[at]:tier==1?objects[at]:hints[at]);
    }
    Texture2D Solid(Color c) { var t=new Texture2D(1,1);t.SetPixel(0,0,c);t.Apply();return t; }
    void InitUI() {
        if(heading!=null)return;
        heading=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=23,fontStyle=FontStyle.Bold,wordWrap=true};heading.normal.textColor=Color.white;
        factoryBody=new GUIStyle(heading){fontSize=18,fontStyle=FontStyle.Normal};factorySmall=new GUIStyle(factoryBody){fontSize=14};factorySmall.normal.textColor=new Color(.78f,.86f,.87f);large=new GUIStyle(heading){fontSize=35};
        factoryPanel=Solid(new Color(.035f,.07f,.1f,.95f));tint=Solid(new Color(.95f,.58f,.24f));shade=Solid(new Color(.015f,.025f,.04f,.77f));
    }
    void DrawFactoryPanel(Rect r){GUI.DrawTexture(r,factoryPanel);} void Label(Rect r,string s,GUIStyle style){GUI.Label(r,s,style);}
    protected override void OnGUI() {
        if(BorrowedTime.Menu.GamePauseMenu.IsPausedFor(player))return;
        if(player.hideHud)return; InitUI();GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
        DrawFactoryPanel(new Rect(24,24,650,132));GUI.DrawTexture(new Rect(24,24,5,132),tint);
        Label(new Rect(44,35,600,34),"03 / THE LAST SEED",heading);Label(new Rect(44,73,600,30),"โรงงานแห่งต้นตอ  •  หยุดมลพิษ ก่อนคืนชีวิต",factorySmall);
        string[] goals={"เข้าสู่โรงงาน","สำรวจโถงรับสินค้า","เปิดวงจรและประตูด้านใน","คืนสภาพทางขนส่ง","ซ่อมและขึ้นลิฟต์ขนส่ง","เร่งสายพานให้เปิดทาง","ซ่อมหม้อไอน้ำทั้งสามจุด","จ่ายไฟและเปิดสะพานวงแหวน","หยุดมลพิษและฟื้นระบบน้ำ","ปลูกเมล็ดพันธุ์และเร่งการเติบโต"};
        Label(new Rect(44,110,600,40),stage+" / 9   "+goals[Mathf.Clamp(stage,0,9)],factoryBody);
        DrawFactoryPanel(new Rect(1080,24,336,236));Label(new Rect(1100,38,300,32),"พลังถุงมือ  "+Mathf.RoundToInt(player.energy)+" / 100",factoryBody);GUI.DrawTexture(new Rect(1100,80,290*player.energy/100,12),tint);
        Label(new Rect(1100,112,300,32),(pollutionStopped?"●":"○")+" หยุดมลพิษ   "+(waterFlowRestored?"●":"○")+" น้ำสะอาด",factoryBody);
        Label(new Rect(1100,151,300,30),seedPlanted?"◆ ปลูกเมล็ดแล้ว":hasMiracleSeed?"◆ เมล็ดพันธุ์จากด่านฝึก":"◇ ยังไม่มีเมล็ดพันธุ์",factorySmall);
        Label(new Rect(1100,183,300,32),(BTJourney.OptionalGardenRestored?"★":"☆")+" สวนเล็กในเมือง",factorySmall);
        Label(new Rect(1100,215,300,32),(nurseryRestored?"★":"☆")+" โรงเพาะกล้า (ทางเลือก)",factorySmall);
        if(stage>0 && !completed) {
            DrawFactoryPanel(new Rect(24,730,1392,90));Label(new Rect(44,742,1320,26),"ROBO",factorySmall);Label(new Rect(44,774,1340,42),roboLine,factoryBody);
            Label(new Rect(25,845,1400,42),"WASD เดิน   Shift วิ่ง   Space กระโดด   RMB เล็ง + ล้อถอย คืนสภาพ / ล้อหน้า เร่งสภาพ / LMB ดูดพลัง   F ใช้งาน / ลิฟต์ / ปลูก   H คำใบ้   Tab แผนที่   R รีเซ็ต   Esc พัก",factorySmall);
            if(!player.overview) {
                Label(new Rect(708,435,35,35),"+",heading);
                if(player.target) {DrawFactoryPanel(new Rect(470,558,500,96));Label(new Rect(490,568,460,36),player.target.displayName,heading);Label(new Rect(490,611,460,36),player.target.StateName+(player.target.IsLiving && player.target.absorbAllowed?" / พลังชีวิต "+Mathf.RoundToInt(player.target.life)+"%":""),factoryBody);}
            }
            if(markers!=null && stage<markers.Length) {
                var p=player.view.WorldToScreenPoint(markers[stage]+Vector3.up*2.6f);if(p.z>0&&p.x>0&&p.x<Screen.width&&p.y>0&&p.y<Screen.height)Label(new Rect(p.x/Screen.width*1440-20,900-p.y/Screen.height*900-20,100,40),"◆ "+stage,heading);
            }
            if(player.overview)for(int i=1;i<markers.Length;i++){var p=player.view.WorldToScreenPoint(markers[i]+Vector3.up*1.8f);if(p.z>0)Label(new Rect(p.x/Screen.width*1440,900-p.y/Screen.height*900-18,90,38),"● "+i,heading);}
        }
        if(player.Feedback!=""){DrawFactoryPanel(new Rect(300,670,840,46));Label(new Rect(320,676,800,40),player.Feedback,factoryBody);}
        if(stage==0 || player.paused || completed) {
            GUI.DrawTexture(new Rect(0,0,1440,900),shade);DrawFactoryPanel(new Rect(325,200,790,495));GUI.DrawTexture(new Rect(325,200,790,5),tint);
            Label(new Rect(365,240,710,70),completed?(GoodEnding?"ความหวังผลิบาน":"วันพรุ่งนี้ที่เปราะบาง"):player.paused?"พักการเดินทาง":"เมล็ดพันธุ์สุดท้าย",large);
            string message=completed?ending+"\n\nโรงงานหยุดปล่อยมลพิษ น้ำสะอาดกลับมา และเมล็ดพันธุ์เติบโตแล้ว\n\n"+(GoodEnding?"คุณดูแลทั้งสวนเล็กและโรงเพาะกล้า การฟื้นตัวจึงแผ่กว้าง\nRobo: สิ่งเล็ก ๆ ที่เราใส่ใจ ช่วยเปลี่ยนวันพรุ่งนี้":"เราหยุดต้นเหตุได้สำเร็จ แต่บางพื้นที่ยังต้องได้รับการดูแล\nRobo: การฟื้นฟูเพิ่งเริ่มต้น เรายังช่วยโลกต่อได้"):player.paused?"Esc เพื่อเดินทางต่อ\nR เพื่อเริ่มด่านโรงงานใหม่ โดยคงเมล็ดที่นำเข้ามา":"ต้นเหตุของมลพิษอยู่ที่นี่\nเปิดเส้นทางผ่านระบบขนส่ง หม้อไอน้ำ และห้องควบคุม\nหยุดของเสีย ฟื้นน้ำสะอาด แล้วนำเมล็ดสู่ห้องแกนกลาง\n\n"+(hasMiracleSeed?"เมล็ดพันธุ์และพลังงานจากเมืองถูกนำมาด้วยแล้ว":"เปิดฉากตรงเพื่อทดสอบได้ แต่ต้องเริ่มจากด่าน 1 เพื่อรับเมล็ดจริง");
            Label(new Rect(365,330,710,235),message,factoryBody);var button=new GUIStyle(GUI.skin.button){font=uiFont,fontSize=20};
            if(stage==0 && GUI.Button(new Rect(365,594,710,55),"เริ่มสำรวจ / ENTER",button))Begin();
            else if(completed && GUI.Button(new Rect(365,594,710,55),"เริ่มการเดินทางใหม่",button))BTJourney.LoadTraining();
            else if(player.paused && GUI.Button(new Rect(365,594,710,55),"เดินทางต่อ / ESC",button)){player.paused=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        }
    }
}
}
