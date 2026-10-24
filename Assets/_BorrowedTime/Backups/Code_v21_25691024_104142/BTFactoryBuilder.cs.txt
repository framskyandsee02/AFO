#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using B=BorrowedTime.Editor.BTTrainingBuilder;
namespace BorrowedTime.Editor {
public static class BTFactoryBuilder {
    public const string ScenePath="Assets/_BorrowedTime/Scenes/FactoryInterior.unity";
    static BTFactoryGame factory;
    static Transform root,structure,machines,nature;
    static Mesh cube;
    static readonly List<Light> coreLights=new List<Light>();
    static readonly List<Renderer> coreGlows=new List<Renderer>();
    static Material Mat(string n) => AssetDatabase.LoadAssetAtPath<Material>(B.Root+"/Materials/"+n+".mat");
    static GameObject Box(string n,Vector3 p,Vector3 size,string material,Transform parent=null,bool solid=false) {
        var g=B.Shape(n,p,size,material,parent?parent:structure,solid); g.GetComponent<MeshFilter>().sharedMesh=cube; return g;
    }
    static void Floor(string n,Vector3 center,Vector3 size,float top) {
        Box(n,new Vector3(center.x,top-.25f,center.z),new Vector3(size.x,.5f,size.z),"SteelFloor",structure,true);
        // Plate seams and light strips mark real walkable edges.
        if(size.x>7) for(float x=center.x-size.x*.5f+2;x<center.x+size.x*.5f;x+=4) Box("Deck plate seam",new Vector3(x,top+.012f,center.z),new Vector3(.035f,.018f,size.z-.15f),"SteelEdge");
        if(size.z>7) for(float z=center.z-size.z*.5f+2;z<center.z+size.z*.5f;z+=4) Box("Deck plate seam",new Vector3(center.x,top+.014f,z),new Vector3(size.x-.15f,.018f,.035f),"SteelEdge");
    }
    static void Rail(Vector3 a,Vector3 b,bool safety=true,Transform parent=null) {
        if(!parent) parent=structure;
        int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/2.2f));
        for(int i=0;i<=count;i++) Box("Safety railing stanchion",Vector3.Lerp(a,b,i/(float)count)+Vector3.up*.63f,new Vector3(.1f,1.25f,.1f),"SteelEdge",parent,safety);
        B.Beam("Safety handrail",a+Vector3.up*1.17f,b+Vector3.up*1.17f,.11f,"HazardGold",parent,safety);
        B.Beam("Safety lower rail",a+Vector3.up*.55f,b+Vector3.up*.55f,.07f,"SteelEdge",parent,safety);
    }
    static void Sign(string words,Vector3 p,float size=.08f,Transform parent=null) {
        var g=new GameObject("Factory sign · "+words);g.transform.position=p;g.layer=2;if(parent)g.transform.SetParent(parent,true);
        var tm=g.AddComponent<TextMesh>();tm.text=words;tm.font=factory.uiFont;tm.fontSize=54;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.79f,.88f,.9f);tm.GetComponent<Renderer>().sharedMaterial=factory.uiFont.material;
    }
    static Transform Panel(string name,Vector3 p,string label) {
        var t=B.Group(name,machines);t.position=p;
        Box("Control desk base",p+Vector3.up*.55f,new Vector3(1.3f,1.1f,.85f),"SteelEdge",t,true);
        var face=Box("Control screen",p+new Vector3(0,1.25f,-.05f),new Vector3(1.15f,.65f,.2f),"BlueSignal",t);face.transform.Rotate(15,0,0);
        Sign(label,p+new Vector3(0,2,-.35f),.07f,t);return t;
    }
    static BTTimeTarget Device(string name,Vector3 p,int initial,string[] states,string glow="GlowBlue") {
        var t=B.Target(name,TargetKind.Mechanism,p,initial,states,machines);t.states=new GameObject[states.Length];
        for(int i=0;i<states.Length;i++) {
            var state=B.Group("Temporal state "+i,t.transform);t.states[i]=state.gameObject;
            Box("Device casing",p+Vector3.up*.8f,new Vector3(1.25f,1.6f,.9f),i==states.Length-1?"Rust":"SteelEdge",state,true);
            Box("Device display",p+new Vector3(0,1.3f,-.5f),new Vector3(.85f,.55f,.1f),i==0?"BlueSignal":"DarkPanel",state);
        }
        B.TargetCrystal(t,p+new Vector3(0,1.3f,-.65f),glow);t.Apply();return t;
    }
    static void PlantSource(Vector3 p,float scale=.42f) {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(B.Root+"/Prefabs/LivingEnergyTree.prefab");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name="Renewable service planter";go.transform.SetParent(nature,true);go.transform.position=p+Vector3.up*.5f;go.transform.localScale=Vector3.one*scale;
        var target=go.GetComponent<BTTimeTarget>();target.game=factory;target.displayName="พืชในกระถางฟื้นพลัง";target.absorbAllowed=true;target.ResetTarget();PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        Box("Supported plant container",p+Vector3.up*.25f,new Vector3(1.35f,.5f,1.35f),"Planter",nature,true);
    }
    static void LightAt(Vector3 p,Color color,float range=12,float intensity=2,bool core=false) {
        var t=B.Group("Industrial practical light",root);t.position=p;var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;
        B.Shape("Protected light globe",p,new Vector3(.28f,.28f,.28f),core?"ReactorRed":"BlueSignal",t,false,PrimitiveType.Sphere);if(core)coreLights.Add(l);
    }
    static void DoorFrame(Transform door,Vector3 center,float width,float baseY) {
        foreach(float x in new[]{center.x-width*.5f-.25f,center.x+width*.5f+.25f}) Box("Door steel post",new Vector3(x,baseY+2.4f,center.z),new Vector3(.45f,4.8f,1),"SteelEdge",machines,true);
        Box("Door lintel",new Vector3(center.x,baseY+4.6f,center.z),new Vector3(width+1,.6f,1),"SteelEdge",machines,true);
        for(int i=0;i<5;i++) Box("Door warning stripe",center+new Vector3((i-2)*width*.16f,1.6f,-.43f),new Vector3(width*.12f,.2f,.05f),"HazardGold",door);
    }
    static void Entry() {
        Floor("01 Receiving hall",new Vector3(0,0,-23),new Vector3(52,0,10),0);
        Floor("02 Lower transport hall",new Vector3(0,0,-12.5f),new Vector3(52,0,11),0);
        foreach(float x in new[]{-15,15}) Box("Receiving checkpoint wall",new Vector3(x,2,-18),new Vector3(22,4,.7f),"SteelWall",structure,true);
        factory.entryDoor=Box("Receiving hall gate",new Vector3(0,2,-18),new Vector3(8,4,.75f),"DarkPanel",machines,true).transform;
        DoorFrame(factory.entryDoor,new Vector3(0,2,-18),8,0);
        factory.entryControl=Device("แผงควบคุมโถงเข้า",new Vector3(-1.5f,0,-22),2,new[]{"ใหม่","ทำงานได้","เสียหาย"});
        factory.entryPanel=B.Group("Entry panel interaction",machines);factory.entryPanel.position=new Vector3(-1.5f,0,-22);
        factory.entryRelays=new[]{Device("รีเลย์ฝั่งซ้าย",new Vector3(-10,0,-23),1,new[]{"เชื่อมต่อแล้ว","หลุดจากวงจร"}),Device("รีเลย์ฝั่งขวา",new Vector3(10,0,-23),1,new[]{"เชื่อมต่อแล้ว","หลุดจากวงจร"})};
        Sign("02 / INTAKE CONTROL\nF เปิดประตู เมื่อวงจรพร้อม",new Vector3(-1.5f,2.8f,-22.5f),.075f,machines);
        PlantSource(new Vector3(-5,0,-25));PlantSource(new Vector3(18,0,-24));
        foreach(float x in new[]{-22,22}) for(int i=0;i<3;i++) {
            Box("Stacked receiving crate",new Vector3(x,.6f+i*1.2f,-25),new Vector3(2,1.2f,2),"Crate",machines,true);
            Box("Crate reinforcement",new Vector3(x,.6f+i*1.2f,-26.03f),new Vector3(1.8f,.13f,.07f),"SteelEdge",machines);
        }
        Sign("01 / RECEIVING\nจุดเริ่มต้นภายในโรงงาน",new Vector3(0,3.4f,-27.5f),.12f,machines);
        Rail(new Vector3(-25,0,-7),new Vector3(-19.4f,0,-7));Rail(new Vector3(-14.6f,0,-7),new Vector3(25,0,-7));
        LightAt(new Vector3(-12,3.4f,-24),new Color(.35f,.7f,1));LightAt(new Vector3(12,3.4f,-24),new Color(.35f,.7f,1));
    }
    static void LowerCrossing() {
        Floor("04 Raw materials floor",new Vector3(-17,0,9),new Vector3(18,0,18),0);
        var t=B.Target("สะพานระบบลำเลียง",TargetKind.Bridge,Vector3.zero,2,new[]{"สมบูรณ์","สึกหรอ","ขาด"},machines);factory.bridge=t;t.states=new GameObject[3];
        for(int s=0;s<3;s++) {
            var state=B.Group("Transport bridge state "+s,t.transform);t.states[s]=state.gameObject;
            for(int i=0;i<17;i++) {
                if(s==2 && i>4 && i<12)continue;
                var g=Box("Transport bridge deck",new Vector3(-17,-.12f,-7.5f+i*.48f),new Vector3(4,.24f,.46f),s==0?"SteelFloor":"Rust",state,true);
                if(s>0)g.transform.Rotate((i%3-1)*s*3,0,0);
            }
        }
        foreach(float x in new[]{-19.15f,-14.85f})Rail(new Vector3(x,0,-7.5f),new Vector3(x,0,.2f),false,t.transform);
        B.TargetCrystal(t,new Vector3(-12.5f,1.4f,-9),"GlowBlue");t.Apply();
        PlantSource(new Vector3(-22,0,4));
        for(int i=0;i<4;i++)Box("Raw material rack",new Vector3(-24,1.2f,2+i*3.5f),new Vector3(2,2.4f,2),"Rust",machines,true);
        Sign("03 / TRANSPORT\nคืนสภาพทางขนส่ง",new Vector3(-17,2.7f,-6.5f),.09f,machines);
    }
    static BTFactoryLift Lift(string name,Vector3 p,float low,float high,bool openSouth) {
        var lift=B.Group(name,machines).gameObject.AddComponent<BTFactoryLift>();lift.game=factory;lift.lowHeight=low;lift.highHeight=high;
        var platform=B.Group("Moving elevator platform",lift.transform);platform.position=new Vector3(p.x,low-.25f,p.z);lift.platform=platform;
        Box("Elevator deck",platform.position,new Vector3(4,.5f,4),"SteelFloor",platform,true);
        foreach(float dx in new[]{-2,2})Rail(new Vector3(p.x+dx,low,p.z-2),new Vector3(p.x+dx,low,p.z+2),true,platform);
        float back=p.z+(openSouth?2:-2);Rail(new Vector3(p.x-2,low,back),new Vector3(p.x+2,low,back),true,platform);
        foreach(float dx in new[]{-2.45f,2.45f}) {
            Box("Elevator guide column",new Vector3(p.x+dx,(low+high)*.5f+1.7f,p.z),new Vector3(.4f,high-low+4,.5f),"SteelEdge",machines,true);
            Box("Elevator cable",new Vector3(p.x+dx*.6f,(low+high)*.5f+2,p.z+1.8f),new Vector3(.06f,high-low+4,.06f),"SteelEdge",machines);
        }
        Box("Elevator overhead crossbeam",new Vector3(p.x,high+3.7f,p.z),new Vector3(5.5f,.5f,1.2f),"HazardGold",machines,true);
        return lift;
    }
    static void Freight() {
        factory.liftControl=Device("แผงลิฟต์ขนส่ง",new Vector3(-21,0,10),2,new[]{"ใหม่","ทำงานได้","เสียหาย"});
        factory.freightButton=B.Group("Freight lift button",machines);factory.freightButton.position=new Vector3(-21,0,10);
        factory.freightLift=Lift("04 Freight elevator",new Vector3(-18,0,10),0,6,true);
        Floor("Freight upper landing",new Vector3(-18.5f,0,6),new Vector3(11,0,4),6);
        Floor("West upper catwalk",new Vector3(-18,0,-3.5f),new Vector3(4,0,15),6);
        Floor("South transfer catwalk",new Vector3(-1,0,-11),new Vector3(38,0,4),6);
        Rail(new Vector3(-20,6,-9),new Vector3(-20,6,4));Rail(new Vector3(-16,6,-9),new Vector3(-16,6,4));
        Rail(new Vector3(-20,6,-13),new Vector3(18,6,-13));Rail(new Vector3(-14,6,-9),new Vector3(8,6,-9));
        Rail(new Vector3(-24,6,4),new Vector3(-24,6,8));Rail(new Vector3(-24,6,8),new Vector3(-20.4f,6,8));Rail(new Vector3(-15.6f,6,8),new Vector3(-13,6,8));
        PlantSource(new Vector3(-22.5f,6,6));PlantSource(new Vector3(10,6,-10));
        Sign("04 / MATERIAL LIFT\nยืนบนแท่น แล้วกด F",new Vector3(-18,3,11.8f),.09f,machines);
    }
    static void Conveyor() {
        Floor("05 Conveyor hall",new Vector3(16,0,-6),new Vector3(16,0,18),6);
        Rail(new Vector3(8,6,-8.6f),new Vector3(8,6,2));
        Rail(new Vector3(24,6,-13),new Vector3(24,6,2));
        factory.conveyor=Device("ตัวควบคุมสายพาน",new Vector3(11,6,-5),0,new[]{"ลังขวางทาง","กำลังลำเลียง","ลังเข้าช่องพัก"},"GlowGold");
        for(int s=0;s<3;s++) {
            Vector3 p=s==0?new Vector3(16,7.24f,-1):s==1?new Vector3(18,7.24f,-1):new Vector3(22,7.24f,-1);
            Box("Conveyor cargo temporal position",p,new Vector3(3.8f,2,2),"Crate",factory.conveyor.states[s].transform,true);
        }
        Box("Conveyor belt frame",new Vector3(18,6.12f,-1),new Vector3(12,.24f,2.3f),"DarkPanel",machines,true);
        for(int i=0;i<12;i++)B.Shape("Conveyor roller",new Vector3(12.5f+i,6.28f,-1),new Vector3(.24f,1.1f,.24f),"SteelEdge",machines,false,PrimitiveType.Cylinder).transform.rotation=Quaternion.Euler(90,0,0);
        factory.conveyorPanel=Panel("Conveyor release panel",new Vector3(20,6,0),"F / เปิดทางสายพาน");
        factory.conveyorDoor=Box("Conveyor access door",new Vector3(16,8,2.5f),new Vector3(6,4,.6f),"DarkPanel",machines,true).transform;DoorFrame(factory.conveyorDoor,new Vector3(16,8,2.5f),6,6);
        foreach(float x in new[]{10.4f,21.6f})Box("Conveyor bulkhead",new Vector3(x,8,2.5f),new Vector3(4.7f,4,.6f),"SteelWall",machines,true);
        Sign("05 / CONVEYOR FLOOR",new Vector3(16,10,-5),.11f,machines);
    }
    static void Boilers() {
        Floor("06 Boiler maintenance deck",new Vector3(16,0,11),new Vector3(16,0,17),6);
        foreach(float x in new[]{8,24})Rail(new Vector3(x,6,3),new Vector3(x,6,19));
        factory.boilers=new BTTimeTarget[3];factory.steam=B.Group("Leaking boiler steam",machines).gameObject;
        for(int i=0;i<3;i++) {
            float x=11+i*5;var t=B.Target("หม้อไอน้ำ "+(i+1),TargetKind.Mechanism,new Vector3(x,6,9),2,new[]{"สมบูรณ์","รั่ว","เสียหาย"},machines);factory.boilers[i]=t;t.states=new GameObject[3];
            for(int s=0;s<3;s++) {
                var state=B.Group("Boiler age "+s,t.transform);t.states[s]=state.gameObject;
                B.Shape("Boiler tank",new Vector3(x,8.5f,9),new Vector3(2,2.4f,2),s==0?"SteelEdge":"Rust",state,true,PrimitiveType.Cylinder);
                B.Shape("Boiler cap",new Vector3(x,11,9),new Vector3(2.3f,.3f,2.3f),"SteelEdge",state,false,PrimitiveType.Cylinder);
                Box("Boiler pressure gauge",new Vector3(x,8.2f,7.92f),new Vector3(.65f,.65f,.18f),s==0?"BlueSignal":"ReactorRed",state);
            }
            B.TargetCrystal(t,new Vector3(x,7.3f,7.55f),"GlowBlue");t.Apply();
            for(int j=0;j<3;j++)B.Shape("Steam cloud",new Vector3(x+.9f+j*.3f,10+j*.6f,8.4f),new Vector3(.55f+j*.15f,.55f,.55f+j*.15f),"Steam",factory.steam.transform,false,PrimitiveType.Sphere);
            B.Beam("Boiler outlet pipe",new Vector3(x,10.8f,9),new Vector3(x,10.8f,17),.35f,"Rust",machines);
        }
        factory.boilerPanel=Panel("Boiler vent console",new Vector3(16,6,15.5f),"F / ระบายแรงดัน");
        factory.pressureDoor=Box("Pressure chamber door",new Vector3(16,8,19),new Vector3(6,4,.6f),"DarkPanel",machines,true).transform;DoorFrame(factory.pressureDoor,new Vector3(16,8,19),6,6);
        foreach(float x in new[]{10.4f,21.6f})Box("Boiler bulkhead",new Vector3(x,8,19),new Vector3(4.7f,4,.6f),"SteelWall",machines,true);
        PlantSource(new Vector3(23,6,4.5f));Sign("06 / STEAM & PRESSURE",new Vector3(16,12.2f,10),.1f,machines);
        LightAt(new Vector3(16,11.8f,14),new Color(1,.55f,.2f),14,3);
    }
    static void PowerAndRing() {
        Floor("07 Power room",new Vector3(16,0,24),new Vector3(16,0,10),6);
        factory.powerCoupler=Device("ตัวเชื่อมวงจรพลังงาน",new Vector3(20,6,24),0,new[]{"ไม่เชื่อมต่อ","กำลังเชื่อม","วงจรเสถียร"},"GlowGold");
        factory.powerPanel=Panel("Power distribution panel",new Vector3(16,6,24.5f),"F / เปิดสะพานวงแหวน");
        for(int i=0;i<4;i++)Box("Power distribution cabinet",new Vector3(11+i*3.2f,8,28.2f),new Vector3(2.4f,4,1.1f),"SteelEdge",machines,true);
        factory.ringCrossing=B.Group("Powered ring bridge",structure).gameObject;
        Box("Extended ring crossing deck",new Vector3(0,5.75f,25),new Vector3(16,.5f,4),"SteelFloor",factory.ringCrossing.transform,true);
        Rail(new Vector3(-8,6,23),new Vector3(8,6,23),true,factory.ringCrossing.transform);Rail(new Vector3(-8,6,27),new Vector3(8,6,27),true,factory.ringCrossing.transform);
        for(int i=0;i<5;i++)Box("Ring bridge guide lamp",new Vector3(-6+i*3,6.08f,25),new Vector3(1,.05f,.18f),"BlueSignal",factory.ringCrossing.transform);
        factory.ringCrossing.SetActive(false);
        Rail(new Vector3(24,6,19.2f),new Vector3(24,6,29));Rail(new Vector3(8,6,19.2f),new Vector3(8,6,22.6f));Rail(new Vector3(8,6,27.4f),new Vector3(24,6,29));
        PlantSource(new Vector3(22,6,26.5f));Sign("07 / POWER DISTRIBUTION",new Vector3(16,11,26),.1f,machines);
        LightAt(new Vector3(16,10.5f,25),new Color(.2f,.6f,1),14,3);
    }
    static void Controls() {
        Floor("08 Main control room",new Vector3(-16,0,25.5f),new Vector3(16,0,9),6);
        Rail(new Vector3(-8,6,21),new Vector3(-8,6,22.5f));Rail(new Vector3(-8,6,27.5f),new Vector3(-8,6,30));
        Rail(new Vector3(-24,6,22),new Vector3(-24,6,30));Rail(new Vector3(-24,6,30),new Vector3(-8,6,30));
        factory.shutoffValves=new Transform[3];Vector3[] valves={new Vector3(-19,6,23.5f),new Vector3(-19,6,27.5f),new Vector3(-14,6,28)};
        for(int i=0;i<3;i++) {
            var p=valves[i];var t=B.Group("Pollution shutoff valve "+(i+1),machines);t.position=p;factory.shutoffValves[i]=t;
            Box("Valve pipe support",p+Vector3.up*.65f,new Vector3(.6f,1.3f,.65f),"Rust",t,true);B.Ring("Shutoff wheel",p+new Vector3(0,1.3f,-.4f),.43f,"HazardGold",t,true);Sign("F / V"+(i+1),p+new Vector3(0,2.1f,-.5f),.075f,t);
        }
        factory.coreConsole=Device("แผงควบคุมแกนมลพิษ",new Vector3(-10.5f,6,24),2,new[]{"ใหม่","ทำงานได้","เสียหาย"});
        factory.shutdownPanel=B.Group("Shutdown interaction",machines);factory.shutdownPanel.position=new Vector3(-10.5f,6,24);
        Sign("F / หยุดการปล่อยมลพิษ",new Vector3(-10.5f,8.6f,23.3f),.075f,machines);
        factory.waterPanel=Panel("Clean water restoration panel",new Vector3(-9.5f,6,29),"F / เปิดน้ำสะอาด");
        factory.coreLiftButton=B.Group("Core lift button",machines);factory.coreLiftButton.position=new Vector3(-22,6,20);
        factory.coreLift=Lift("09 Core chamber elevator",new Vector3(-20,6,20),6,12,false);
        Floor("Core lift upper landing",new Vector3(-16,0,24),new Vector3(16,0,4),12);
        Floor("Upper core connection",new Vector3(-4,0,24),new Vector3(8,0,4),12);
        Floor("Upper planting access bridge",new Vector3(0,0,21),new Vector3(4,0,6),12);
        Rail(new Vector3(-24,12,26),new Vector3(2,12,26));Rail(new Vector3(-17.6f,12,22),new Vector3(-2,12,22));Rail(new Vector3(-2,12,22),new Vector3(-2,12,20));
        Sign("08 / MAIN CONTROL\nF ปิดวาล์ว → หยุดมลพิษ → เปิดน้ำ",new Vector3(-16,10,29.5f),.1f,machines);
        PlantSource(new Vector3(-22,6,28));LightAt(new Vector3(-16,10.5f,27),new Color(.3f,.7f,1),16,3);
    }
    static void Core() {
        var core=B.Group("Factory pollution reactor",machines);
        B.Shape("Reactor pressure vessel",new Vector3(0,5.5f,11),new Vector3(9,6,9),"SteelWall",core,true,PrimitiveType.Cylinder);
        for(int i=0;i<8;i++) {
            float a=i*Mathf.PI*.25f;Vector3 p=new Vector3(Mathf.Sin(a)*5.05f,5.5f,11+Mathf.Cos(a)*5.05f);
            var g=Box("Reactor luminous spine",p,new Vector3(.38f,10,.38f),"ReactorRed",core);coreGlows.Add(g.GetComponent<Renderer>());
            Box("Reactor external rib",new Vector3(p.x*1.08f,5.5f,11+(p.z-11)*1.08f),new Vector3(.55f,11,.55f),"SteelEdge",core);
        }
        foreach(float y in new[]{.5f,5.5f,10.5f})B.Ring("Reactor reinforcement ring",new Vector3(0,y,11),4.8f,"HazardGold",core);
        LightAt(new Vector3(-4,6,11),new Color(1,.2f,.1f),15,3,true);LightAt(new Vector3(4,6,11),new Color(1,.2f,.1f),15,3,true);
        Floor("09 Planting chamber floor",new Vector3(0,0,12),new Vector3(16,0,16),12);
        Rail(new Vector3(-8,12,4),new Vector3(8,12,4));Rail(new Vector3(-8,12,4),new Vector3(-8,12,20));Rail(new Vector3(8,12,4),new Vector3(8,12,20));Rail(new Vector3(-8,12,20),new Vector3(-2.4f,12,20));Rail(new Vector3(2.4f,12,20),new Vector3(8,12,20));
        Box("Planting soil bed",new Vector3(0,12.03f,12),new Vector3(3,.06f,3),"Soil",nature);
        B.Ring("Seed planting cradle",new Vector3(0,12.15f,12),1.9f,"GlowGreen",nature);factory.plantPoint=B.Group("Miracle seed planting point",nature);factory.plantPoint.position=new Vector3(0,12,12);
        var tree=B.Target("ต้นไม้แห่งวันพรุ่งนี้",TargetKind.LivingTime,new Vector3(0,12,12),0,new[]{"เมล็ดพันธุ์","ต้นอ่อน","เติบโตสมบูรณ์"},nature);factory.miracleTree=tree;tree.absorbAllowed=false;tree.states=new GameObject[3];
        for(int s=0;s<3;s++) {
            var state=B.Group("Miracle growth "+s,tree.transform);tree.states[s]=state.gameObject;
            if(s==0)B.Shape("Planted miracle seed",new Vector3(0,12.45f,12),new Vector3(.45f,.75f,.4f),"GlowGreen",state,false,PrimitiveType.Sphere);
            else {var tr=B.Tree("Hope tree growth "+s,new Vector3(0,12,12),s==1?.42f:1.25f,true);tr.SetParent(state,true);}
        }
        B.TargetCrystal(tree,new Vector3(0,13.2f,10.5f),"GlowGreen");tree.Apply();tree.gameObject.SetActive(false);
        var nursery=B.Target("โรงเพาะกล้าที่ยังฟื้นได้",TargetKind.LivingTime,new Vector3(-5,12,14),2,new[]{"ต้นอ่อน","แข็งแรง","เหี่ยวเฉา"},nature);factory.nursery=nursery;nursery.states=new GameObject[3];
        Box("Nursery raised bed",new Vector3(-5,12.25f,14),new Vector3(2.4f,.5f,2.2f),"Planter",nature,true);
        for(int s=0;s<3;s++) {
            var state=B.Group("Nursery growth "+s,nursery.transform);nursery.states[s]=state.gameObject;
            for(int j=0;j<3;j++)B.Shape("Nursery foliage",new Vector3(-5.7f+j*.7f,12.8f,14),new Vector3(.55f,s==0?.5f:.9f,.55f),s==2?"Wilt":"Leaf",state,false,PrimitiveType.Sphere);
        }
        B.TargetCrystal(nursery,new Vector3(-5,13.2f,12.8f),"GlowGreen");nursery.Apply();
        factory.restoredNursery=B.Group("Restored nursery flowers",nature).gameObject;
        for(int i=0;i<4;i++)B.Shape("Nursery blossom",new Vector3(-5.8f+i*.5f,13.2f,14),new Vector3(.25f,.2f,.25f),"Gold",factory.restoredNursery.transform,false,PrimitiveType.Sphere);
        factory.restoredNursery.SetActive(false);
        PlantSource(new Vector3(5,12,17));PlantSource(new Vector3(5,12,7));PlantSource(new Vector3(-5,12,7));
        factory.wideRestoration=B.Group("Hopeful renewal ending restoration",nature).gameObject;
        for(int i=0;i<16;i++) {
            float a=i*Mathf.PI/8;B.Shape("Ending living canopy",new Vector3(Mathf.Sin(a)*6.5f,12.6f,12+Mathf.Cos(a)*6.5f),new Vector3(1.2f,.8f,1.2f),i%2==0?"LeafLight":"Leaf",factory.wideRestoration.transform,false,PrimitiveType.Sphere);
        }
        factory.wideRestoration.SetActive(false);
        factory.dirtyWater=B.Group("Polluted factory drainage",machines).gameObject;factory.cleanWater=B.Group("Restored factory drainage",machines).gameObject;
        B.Shape("Dirty reactor coolant",new Vector3(0,-4.5f,6),new Vector3(48,.15f,57),"Pollution",factory.dirtyWater.transform);
        B.Shape("Clean reactor coolant",new Vector3(0,-4.5f,6),new Vector3(48,.15f,57),"Water",factory.cleanWater.transform);factory.cleanWater.SetActive(false);
        factory.reactorRenderers=coreGlows.ToArray();factory.reactorLights=coreLights.ToArray();factory.reactorDanger=Mat("ReactorRed");factory.reactorSafe=Mat("BlueSignal");
        Sign("09 / THE LAST SEED\nปลูกเมื่อหยุดมลพิษและมีน้ำสะอาดแล้ว",new Vector3(0,15.4f,18.5f),.1f,nature);LightAt(new Vector3(0,18,12),new Color(.35f,.65f,1),15,2);
    }
    static void Shell() {
        Box("Factory back retaining wall",new Vector3(0,9,32),new Vector3(56,18,.7f),"SteelWall",structure,true);
        Box("Factory east retaining wall",new Vector3(28,9,1),new Vector3(.7f,18,62),"SteelWall",structure,true);
        Box("Factory west cutaway wall",new Vector3(-28,2,1),new Vector3(.7f,4,62),"SteelWall",structure,true);
        Box("Factory south cutaway wall",new Vector3(0,2,-29),new Vector3(56,4,.7f),"SteelWall",structure,true);
        for(int z=-27;z<=29;z+=8)foreach(float x in new[]{-27,27}) {
            Box("Supported factory column",new Vector3(x,11,z),new Vector3(.8f,22,.8f),"SteelEdge",structure,true);
            B.Beam("Factory overhead roof truss",new Vector3(-27,22,z),new Vector3(27,22,z),.35f,"SteelEdge",structure);
        }
        foreach(float x in new[]{-6,6})B.Beam("Suspended service pipe",new Vector3(x,20,-26),new Vector3(x,20,30),.45f,"Rust",machines);
        for(int z=-24;z<=28;z+=12) {
            LightAt(new Vector3(-25,8,z),new Color(.25f,.48f,.7f),17,2);LightAt(new Vector3(25,13,z),new Color(1,.48f,.18f),17,2);
            Box("Wall mounted service cabinet",new Vector3(27.5f,8,z),new Vector3(.8f,2,1.4f),"SteelEdge",machines,true);
        }
        for(int i=0;i<5;i++)Box("Supported raw stock pile",new Vector3(-11,1+i*.3f,15),new Vector3(3-i*.3f,2,2.5f),"Rust",machines,true);
    }
    [MenuItem("Tools/Borrowed Time/Build Factory Interior")]
    public static void Build() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building");
        B.Folder(B.Root+"/Backups");B.Folder(B.Root+"/Art/Meshes");
        var old=EditorSceneManager.GetActiveScene();if(old.isDirty)EditorSceneManager.SaveScene(old);
        if(System.IO.File.Exists(ScenePath))AssetDatabase.CopyAsset(ScenePath,B.Root+"/Backups/Factory_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity");
        var training=EditorSceneManager.OpenScene(BTJourney.TrainingPath,OpenSceneMode.Single);var template=Object.FindFirstObjectByType<BTPlayer>();var oldCamera=template.view;var oldRobot=template.game.robo;var font=template.game.uiFont;var oldVolume=Object.FindFirstObjectByType<Volume>();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        factory=new GameObject("Factory gameplay systems").AddComponent<BTFactoryGame>();factory.uiFont=font;
        var p=Object.Instantiate(template.gameObject).GetComponent<BTPlayer>();p.name="Protagonist";p.game=factory;p.transform.position=factory.spawn;factory.player=p;
        var camera=Object.Instantiate(oldCamera.gameObject).GetComponent<Camera>();camera.name="Main Camera";p.view=camera;
        var robot=Object.Instantiate(oldRobot.gameObject).transform;robot.name="ROBO";robot.SetParent(null,true);robot.position=factory.spawn+new Vector3(1.2f,2,0);factory.robo=robot;
        if(oldVolume){var v=Object.Instantiate(oldVolume.gameObject);v.name="Factory bloom and colour";}
        root=B.Group("BORROWED TIME · Factory Interior");structure=B.Group("Supported factory architecture",root);machines=B.Group("Industrial systems and puzzles",root);nature=B.Group("Living energy and final restoration",root);B.BeginCity(factory,root,nature);
        B.Mat("SteelFloor","596D7B");B.Mat("SteelWall","344655");B.Mat("SteelEdge","8496A1");B.Mat("DarkPanel","263A49");B.Mat("HazardGold","E3B765");B.Mat("Crate","B4865D");B.Mat("Planter","A6C3B2");B.Mat("Soil","675B4D");B.Mat("Steam","B7CBD1");B.Mat("ReactorRed","FF7758",true);B.Mat("BlueSignal","6CD5F2",true);
        foreach(string key in new[]{"ReactorRed","BlueSignal"}) { var m=Mat(key);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",key=="ReactorRed"?new Color(2f,.025f,.008f):new Color(.015f,.45f,1.8f));EditorUtility.SetDirty(m); }
        cube=AssetDatabase.LoadAssetAtPath<Mesh>(B.Root+"/Art/Meshes/FactoryUnitCube.asset");
        if(!cube){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh);cube.name="Factory unit box";AssetDatabase.CreateAsset(cube,B.Root+"/Art/Meshes/FactoryUnitCube.asset");Object.DestroyImmediate(g);}
        coreLights.Clear();coreGlows.Clear();Entry();LowerCrossing();Freight();Conveyor();Boilers();PowerAndRing();Controls();Core();Shell();
        var sun=new GameObject("Factory skylight").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(70,-30,0);sun.intensity=.9f;sun.color=new Color(.66f,.77f,.9f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.6f;RenderSettings.sun=sun;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.3f,.4f,.5f);RenderSettings.ambientEquatorColor=new Color(.28f,.34f,.4f);RenderSettings.ambientGroundColor=new Color(.14f,.2f,.24f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.12f,.19f,.26f);RenderSettings.fogStartDistance=95;RenderSettings.fogEndDistance=190;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.14f,.2f);camera.transform.position=new Vector3(-54,60,-65);camera.transform.LookAt(new Vector3(0,5,2));camera.orthographic=true;camera.orthographicSize=44;
        factory.mapCameraPosition=new Vector3(-50,67,-63);factory.mapCameraTarget=new Vector3(0,5,2);factory.mapCameraSize=43;
        factory.markers=new[]{factory.spawn,new Vector3(0,0,-25),new Vector3(-1.5f,0,-22),new Vector3(-17,0,-5),new Vector3(-18,0,10),new Vector3(16,6,-4),new Vector3(16,6,10),new Vector3(16,6,25),new Vector3(-16,6,25),new Vector3(0,12,12)};
        EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(training,true);SceneManager.SetActiveScene(scene);BTCityBuilder.RegisterJourneyScenes();AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,6,3),Quaternion.Euler(48,28,0),48);
        Debug.Log("BORROWED_TIME_FACTORY_READY "+ScenePath);
    }
}
}
#endif
