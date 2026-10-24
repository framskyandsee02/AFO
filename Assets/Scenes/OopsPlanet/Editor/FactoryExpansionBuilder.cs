#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Object=UnityEngine.Object;
namespace OopsPlanet.Editor {
public static class FactoryExpansionBuilder {
    const string Base="Assets/Scenes/OopsPlanet/";
    const string Source=Base+"Scenes/Factory_SourceLayout.unity";
    static BlobWorld world;static Transform factory;static Font font;
    static Dictionary<string,Transform> rooms;static Material floor,wall,teal,dark,yellow;
    static Material Mat(string role){return AssetDatabase.LoadAssetAtPath<Material>(Base+"Materials/WorldPolish/POL_"+role+".mat")??AssetDatabase.LoadAssetAtPath<Material>(Base+"Materials/FactoryInterior/FI_"+role+".mat");}
    static GameObject Art(string asset,string name,Transform parent,Vector3 point,float yaw=0){
        string folder=asset.StartsWith("FE_")?"FactoryExpansion/":"FactoryInterior/";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Base+"Models/"+folder+asset+".fbx");if(!prefab)throw new Exception("Missing Blender model: "+asset);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent.gameObject.scene);g.name=name;g.transform.SetParent(parent,false);g.transform.position=point;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one;
        foreach(var r in g.GetComponentsInChildren<Renderer>(true)){var m=r.sharedMaterials;for(int i=0;i<m.Length;i++){string n=m[i]?m[i].name:"";int at=n.IndexOf("POL_");string role=at>=0?n.Substring(at+4).Split('.')[0]:"Metal";m[i]=Mat(role)??Mat("Metal");}r.sharedMaterials=m;r.gameObject.layer=2;PrefabUtility.RecordPrefabInstancePropertyModifications(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);}PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);return g;
    }
    static GameObject Block(string name,Vector3 point,Vector3 size,Material material,Transform parent=null,bool collider=true){
        var g=new GameObject(name);g.transform.SetParent(parent?parent:factory,false);g.transform.position=point;
        if(collider){var c=g.AddComponent<BoxCollider>();c.size=size;}
        var art=Art("FE_WallUnit",name+" art",g.transform,point);art.transform.localScale=size;
        foreach(var r in art.GetComponentsInChildren<Renderer>()){r.sharedMaterial=material;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(art.transform);return g;
    }
    static void Label(string text,Vector3 pos,float yaw=0,float size=.07f){var g=new GameObject("Factory sign: "+text);g.transform.SetParent(factory);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);var t=g.AddComponent<TextMesh>();t.text=text;t.font=font;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.12f,.25f,.29f);var material=AssetDatabase.LoadAssetAtPath<Material>(Base+"Materials/FactoryInterior/FI_WorldText.mat");g.GetComponent<Renderer>().sharedMaterial=material?material:font.material;}
    static Transform Group(string n){var g=new GameObject("Room "+n);g.transform.SetParent(factory);return g.transform;}
    static string RoomOf(Transform t){string n=t.name;var p=t.position;
        if(n=="Anchor marker" && p.x<0)return "Cooling";
        if(n.StartsWith("Receiving")||n.Contains("chute")||n.StartsWith("Chute")||n.Contains("Sorting")||n.StartsWith("Shutter")||n.StartsWith("Alcove")||n.StartsWith("Cubby")||n.Contains("Round-the-corner")||n=="Arrow paint"||n=="Puzzle s_storage"||n=="Ship part: part_storage"||n=="Puzzle f1_gate"||n=="Ship part: part_f1"||n=="Reset pad: f1_gate")return "Receiving";
        if(n=="Big Toy Press (landmark)"||n.StartsWith("Ledge")||n.StartsWith("Vine ramp")||n=="Ship part: part_cold_room"||n.StartsWith("Central")||n.StartsWith("Walkway")||n.Contains("Breach")||n.Contains("WallBreach")||n.Contains("PlankBarricade")||n=="Puzzle s_factory_flame")return "Central";
        if(n.Contains("North bank")||n=="Condition: north bank"||n.Contains("Cooling")||n.StartsWith("Channel")||n.StartsWith("Service bridge")||n.StartsWith("Permanent catwalk")||n.StartsWith("Fallen girder")||n.Contains("FI_Ru_Girder")||n=="Puzzle f5_cooling"||n=="Reset pad: f5_cooling"||n=="Puzzle s_factory_shockpool"||n=="Submerged terminal"||n=="Crawl hatch to the receiving hall")return "Cooling";
        if(n.StartsWith("Crane")||n.StartsWith("Safety door")||n.Contains("Rust hatch")||n.StartsWith("Rusty bolt")||n.Contains("Wax")||n.StartsWith("Crawl duct")||n.StartsWith("Old press machine")||n=="Puzzle f4_crane"||n=="Reset pad: f4_crane"||n.StartsWith("Oven")||n.StartsWith("Toy oven")||n.Contains("FI_OverheadCrane")||n=="Puzzle s_beam_ice"||n=="Ship part: part_beam_ice")return "Crane";
        if(n.StartsWith("Boiler")||n.StartsWith("Balcony")||n.StartsWith("Control balcony")||n.StartsWith("Giant boiler")||n.StartsWith("Lift rail")||n.Contains("on the control balcony")||n.Contains("FI_GiantBoiler")||n.Contains("Broken conveyor")||n=="Puzzle f2_boiler"||n=="Ship part: part_f2"||n=="Puzzle s_conveyor_weld")return "Boiler";
        if(n.StartsWith("Assembly")||n.StartsWith("Cabinet")||n.StartsWith("Parts cabinet")||n.StartsWith("Rusty cabinet")||n.StartsWith("Conductor rod")||n.StartsWith("Ice lock")||n.StartsWith("Rod bracket")||n.StartsWith("Spare gear")||n.Contains("FI_PartsCabinet")||n.Contains("FI_Furnace")||n=="Furnace window"||n=="Puzzle f6_assembly"||n=="Condition: ice-lock-melted"||n=="Ship part: part_factory_core"||n=="Reset pad: f6_assembly"||n.StartsWith("Gate door: factory_assembly")||n.StartsWith("Gate lamp: factory_assembly")||n.StartsWith("Zone gate: factory_assembly"))return "Assembly";
        if(p.x<-10 && p.z<39)return "Assembly";if(p.z<39)return "Receiving";if(p.x<=-6)return p.z<53.5f?"Cooling":"Turbine";if(p.x>=6)return p.z<=51?"Crane":"Boiler";return "Central";
    }
    static void TranslateRoomObjects(){
        rooms=new Dictionary<string,Transform>();foreach(string n in FactoryLayout.Rooms)rooms[n]=Group(n);
        var children=new List<Transform>();foreach(Transform t in factory)if(!t.name.StartsWith("Room "))children.Add(t);
        foreach(var t in children){
            if(t.name=="Floor"||t.name=="Floor stripe"||t.name.StartsWith("Lining ")||t.name=="Roof"||t.name=="Roof truss"||t.name=="Skylight"||t.name.StartsWith("Channel wall")){Object.DestroyImmediate(t.gameObject);continue;}
            string room=RoomOf(t);Vector3 delta=FactoryLayout.Offset(room);t.SetParent(rooms[room],true);t.position+=delta;
            foreach(var p in t.GetComponentsInChildren<PuzzleObjective>(true))p.area=new Bounds(p.area.center+delta,p.area.size);
            foreach(var it in t.GetComponentsInChildren<PuzzleItem>(true))it.fallbackPosition+=delta;
            foreach(var raft in t.GetComponentsInChildren<ShuttleRaft>(true)){raft.nearDock+=delta;raft.farDock+=delta;}
            foreach(var s in t.GetComponentsInChildren<BlobInteractable>(true))if(s.kind==BlobInteraction.Checkpoint)s.respawn+=delta;
        }
    }
    static bool OldWall(Transform t){string n=t.name;return n.StartsWith("Central ")&&(n.Contains("wall")||n.Contains("skirting"))||n.StartsWith("Receiving north wall")||n.StartsWith("Crane/boiler wall")||n.StartsWith("West wing south wall")||n.StartsWith("Assembly east wall");}
    // New shell follows the relocated rooms. Mechanics retain their tested metre scale.
    static void SideWall(string name,Vector3 a,Vector3 b,float height,params Vector2[] gaps){
        bool x=Mathf.Abs(b.x-a.x)>Mathf.Abs(b.z-a.z);float lo=x?Mathf.Min(a.x,b.x):Mathf.Min(a.z,b.z),hi=x?Mathf.Max(a.x,b.x):Mathf.Max(a.z,b.z);float fixedAxis=x?a.z:a.x,cur=lo;
        Action<float,float,float,float> seg=(from,to,y0,y1)=>{if(to-from<.03f||y1-y0<.03f)return;Vector3 c=x?new Vector3((from+to)/2,(y0+y1)/2,fixedAxis):new Vector3(fixedAxis,(y0+y1)/2,(from+to)/2);Vector3 s=x?new Vector3(to-from,y1-y0,.35f):new Vector3(.35f,y1-y0,to-from);Block(name,c,s,wall);};
        foreach(var gap in gaps.OrderBy(v=>v.x)){seg(cur,gap.x-gap.y/2,0,height);seg(gap.x-gap.y/2,gap.x+gap.y/2,3.3f,height);cur=gap.x+gap.y/2;}seg(cur,hi,0,height);
    }
    static void Floor(string name,float x0,float x1,float z0,float z1){Block(name,new Vector3((x0+x1)/2,-.25f,(z0+z1)/2),new Vector3(x1-x0,.5f,z1-z0),floor);}
    static void RoomShell(string room,float x0,float x1,float z0,float z1,Vector2[] south,Vector2[] north,Vector2[] west,Vector2[] east,bool cooling=false){
        if(cooling){Floor(room+" floor S",x0,x1,z0,65);Floor(room+" floor N",x0,x1,74.5f,z1);}else Floor(room+" floor",x0,x1,z0,z1);
        SideWall(room+" wall S",new Vector3(x0,0,z0),new Vector3(x1,0,z0),8,south);SideWall(room+" wall N",new Vector3(x0,0,z1),new Vector3(x1,0,z1),8,north);SideWall(room+" wall W",new Vector3(x0,0,z0),new Vector3(x0,0,z1),8,west);SideWall(room+" wall E",new Vector3(x1,0,z0),new Vector3(x1,0,z1),8,east);
        Block(room+" ceiling",new Vector3((x0+x1)/2,8.3f,(z0+z1)/2),new Vector3(x1-x0,.3f,z1-z0),teal);
    }
    static readonly Vector2[] None=new Vector2[0];
    static Vector2[] Gap(float at,float width=3)=>new[]{new Vector2(at,width)};
    static void Corridor(string name,Vector3 a,Vector3 b,float width=4,float y=0){
        bool alongX=Mathf.Abs(b.x-a.x)>Mathf.Abs(b.z-a.z);float length=Vector3.Distance(a,b);var c=(a+b)*.5f;c.y=y-.15f;var size=alongX?new Vector3(length+width,.3f,width):new Vector3(width,.3f,length+width);Block(name+" floor",c,size,teal);
        foreach(int sign in new[]{-1,1})for(float along=-width/2;along<length+width/2;along+=2){float piece=Mathf.Min(2,length+width/2-along);Vector3 p=a+(b-a).normalized*(along+piece/2);p.y=y+2;Vector3 off=alongX?Vector3.forward*(width/2+.18f)*sign:Vector3.right*(width/2+.18f)*sign;Block(name+" passage wall",p+off,alongX?new Vector3(piece,4,.25f):new Vector3(.25f,4,piece),wall);}
        Block(name+" ceiling",c+Vector3.up*6.7f,alongX?new Vector3(length+width,.25f,width):new Vector3(width,.25f,length+width),teal);
        for(float t=3;t<length;t+=9){Vector3 p=Vector3.Lerp(a,b,t/length);p.y=y+4.3f;Lamp(name+" lamp",p,14,1.9f);}
    }
    static Light Lamp(string name,Vector3 p,float range=13,float intensity=2){var g=new GameObject(name);g.transform.SetParent(factory);g.transform.position=p;var l=g.AddComponent<Light>();l.type=LightType.Point;l.range=range;l.intensity=intensity;l.color=new Color(.55f,.67f,.7f);l.shadows=LightShadows.None;Art("FI_CeilingLamp",name+" fixture",factory,p);return l;}
    static void ShellAndRoutes(){
        foreach(var t in factory.GetComponentsInChildren<Transform>(true).ToArray())if(t&&OldWall(t))Object.DestroyImmediate(t.gameObject);
        RoomShell("Receiving",-10,20,30.25f,39,Gap(0,5),new[]{new Vector2(0,6.4f),new Vector2(6.5f,3)},None,None);
        Floor("Sorting chute support floor",-3.6f,3.6f,38.8f,41.5f);
        RoomShell("Central",-6,6,59,85.75f,Gap(0,5),Gap(0,5),new[]{new Vector2(61.5f,3),new Vector2(78,2.2f)},Gap(69.6f,3));
        foreach(var t in factory.GetComponentsInChildren<Transform>().Where(t=>t.name=="Central wall N"&&t.position.x>2.5f).ToArray())Object.DestroyImmediate(t.gameObject);
        Block("Central upper loop sill",new Vector3(4.25f,2.4f,85.75f),new Vector3(3.5f,4.8f,.35f),wall);
        Block("Central upper loop jamb",new Vector3(3.05f,6.4f,85.75f),new Vector3(1.1f,3.2f,.35f),wall);
        RoomShell("Crane",29,42.75f,62,74,None,new[]{new Vector2(35.5f,3),new Vector2(31.6f,1.6f)},Gap(69.6f,3),None);
        RoomShell("Boiler",29,42.75f,96,110.75f,Gap(35.5f,3),None,Gap(108.5f,2),None);
        foreach(var t in factory.GetComponentsInChildren<Transform>().Where(t=>t.name=="Boiler wall W" && t.position.z>107.4f && t.position.z<109.6f).ToArray())Object.DestroyImmediate(t.gameObject);
        Block("Boiler upper opening sill",new Vector3(29,2.4f,108.5f),new Vector3(.35f,4.8f,2),wall);
        RoomShell("Cooling",-42.75f,-29,60,76,Gap(-31,2),Gap(-36,4),None,Gap(61.5f,3),true);
        RoomShell("Turbine",-42.75f,-29,96.5f,108.75f,Gap(-36,4),None,None,Gap(104,3));
        RoomShell("Assembly",-4.75f,5,116.65f,125.4f,None,Gap(0,4),None,Gap(122.6f,2.4f));
        Corridor("Receiving to central",new Vector3(6.5f,0,39),new Vector3(6.5f,0,59),4);
        // A short elbow joins the receiving passage to the central south opening.
        Corridor("Central entrance elbow",new Vector3(6.5f,0,57.5f),new Vector3(0,0,57.5f),4);
        Corridor("Central to crane",new Vector3(6,0,69.6f),new Vector3(29,0,69.6f));
        Corridor("Crane to boiler",new Vector3(35.5f,0,74),new Vector3(35.5f,0,96));
        Corridor("Central to cooling",new Vector3(-6,0,61.5f),new Vector3(-29,0,61.5f));
        RoomShell("Cooling service store",-33,-29,56,60,None,Gap(-31,2),None,None);
        Corridor("Cooling to turbine",new Vector3(-36,0,76),new Vector3(-36,0,96.5f));
        Corridor("Turbine return",new Vector3(-29,0,104),new Vector3(0,0,104));
        Corridor("North hall",new Vector3(0,0,85.75f),new Vector3(0,0,104));
        Corridor("Assembly access E",new Vector3(0,0,104),new Vector3(10,0,104));
        Corridor("Assembly access N",new Vector3(10,0,104),new Vector3(10,0,122.6f));
        Corridor("Assembly entry",new Vector3(10,0,122.6f),new Vector3(5,0,122.6f));
        Corridor("Boiler upper return",new Vector3(29,5,108.5f),new Vector3(5.1f,5,108.5f),3,5);
        Corridor("Upper loop",new Vector3(5.1f,5,108.5f),new Vector3(5.1f,5,83.5f),3,5);
        Corridor("Repair arena access",new Vector3(0,0,125.4f),new Vector3(0,0,135),5);
        Floor("Boss supply alcove floor",-12,12,134,144);
        Floor("Crane north landing floor",29,40,74,78);
        // The burned-plank discovery opens a maintenance loop, not an empty hole.
        Corridor("Hidden service elbow",new Vector3(-6,0,78),new Vector3(-20,0,78),3);
        Corridor("Hidden service loop",new Vector3(-20,0,78),new Vector3(-20,0,104),3);
        // Trim only passage-wall segments at intersecting floors; junctions remain walkable.
        var floors=factory.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.name.EndsWith(" floor")||c.name.Contains(" floor ")).ToArray();
        foreach(var c in factory.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.name.Contains(" passage wall")).ToArray()){
            Vector3 p=c.bounds.center;bool junction=floors.Any(f=>{var b=f.bounds;return Mathf.Abs((b.max.y)-c.bounds.min.y)<.7f && p.x>b.min.x+.03f&&p.x<b.max.x-.03f&&p.z>b.min.z+.03f&&p.z<b.max.z-.03f;});
            if(junction)Object.DestroyImmediate(c.gameObject);
        }
        Label("โถงกลาง\n← หล่อเย็น   เครน →\n↑ ห้องประกอบ / ผู้ดูแล",new Vector3(0,3,67.5f));
        Label("ปีกเครื่องกล →\nเครน / หม้อต้ม",new Vector3(17,2.8f,71.2f));
        Label("← ปีกหล่อเย็น\nคลอง / กังหัน",new Vector3(-17,2.8f,63.1f));
        Label("ห้องประกอบ\n↑ ผู้ดูแลโรงงาน",new Vector3(0,3,124));
        foreach(var entry in new[]{new Vector3(0,0,33),new Vector3(1,0,64),new Vector3(33,0,64),new Vector3(32,0,98),new Vector3(-32,0,62),new Vector3(-32,0,98),new Vector3(8,0,119)}){
            var g=new GameObject("Factory exploration checkpoint");g.transform.SetParent(factory);g.transform.position=entry;var c=g.AddComponent<FactoryCheckpoint>();c.world=world;c.respawn=entry+Vector3.up*.1f;
        }
    }
    static void OpenHall(){
        // One continuous warehouse, no room partitions or corridor tunnels.
        foreach(var t in factory.GetComponentsInChildren<Transform>(true).ToArray()){
            if(!t)continue;
            if(t.name=="Kit FI_Ru_WallStain"){Object.DestroyImmediate(t.gameObject);continue;}
            if(t.name=="Kit FI_Ru_CeilingDebris"||t.name=="Kit FI_Ru_HangingBeam"){t.position+=Vector3.up*10;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
            if(t.name=="Kit FI_PipeRun"){
                var renderers=t.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float length=17.3f-bounds.max.y;if(length<=0)continue;
                foreach(int sign in new[]{-1,1}){Vector3 at=bounds.center+(bounds.size.x>bounds.size.z?Vector3.right*bounds.size.x:Vector3.forward*bounds.size.z)*.35f*sign;at.y=bounds.max.y+length/2;Block("Pipe ceiling suspension",at,new Vector3(.06f,length,.06f),dark,null,false);}
            }
        }
        foreach(Transform t in factory.Cast<Transform>().ToArray()){
            string n=t.name;
            if(n.Contains(" passage wall")||n.EndsWith(" ceiling")||((n.EndsWith(" floor")||n.Contains(" floor "))&&t.position.y<1)||n=="Central upper loop sill"||n=="Central upper loop jamb"||n=="Boiler upper opening sill"||n.Contains(" wall "))Object.DestroyImmediate(t.gameObject);
        }
        // Keep the elevated return decks as open catwalks, with low safety rails, not enclosed corridors.
        var decks=factory.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.EndsWith(" floor")&&c.bounds.center.y>3).ToArray();
        foreach(var deck in decks){var b=deck.bounds;bool alongX=b.size.x>b.size.z;float length=alongX?b.size.x:b.size.z;for(float t=0;t<length;t+=2)foreach(int sign in new[]{-1,1}){
            float segment=Mathf.Min(2,length-t);Vector3 at=b.center;at.y=b.max.y+.55f;if(alongX){at.x=b.min.x+t+segment/2;at.z=b.center.z+sign*(b.extents.z+.12f);}else{at.z=b.min.z+t+segment/2;at.x=b.center.x+sign*(b.extents.x+.12f);}
            if(decks.Any(other=>other!=deck&&at.x>other.bounds.min.x&&at.x<other.bounds.max.x&&at.z>other.bounds.min.z&&at.z<other.bounds.max.z))continue;
            Block("Open upper catwalk rail",at,alongX?new Vector3(segment,1.1f,.12f):new Vector3(.12f,1.1f,segment),teal);
        }}
        // Tile the Blender floor module so each tile receives nearby URP lights. Leave the cooling trench open.
        for(float x=-45;x<45;x+=6)for(float z=29;z<188;z+=6){
            float x1=Mathf.Min(x+6,45),z1=Mathf.Min(z+6,188);
            var xs=new[]{x,x1,-42.75f,-29f}.Where(v=>v>=x&&v<=x1).Distinct().OrderBy(v=>v).ToArray();
            var zs=new[]{z,z1,65f,74.5f}.Where(v=>v>=z&&v<=z1).Distinct().OrderBy(v=>v).ToArray();
            for(int i=0;i<xs.Length-1;i++)for(int j=0;j<zs.Length-1;j++){
                float cx=(xs[i]+xs[i+1])/2,cz=(zs[j]+zs[j+1])/2;if(cx>-42.75f&&cx<-29&&cz>65&&cz<74.5f)continue;
                var tile=Block("Open factory floor",new Vector3(cx,-.25f,cz),new Vector3(xs[i+1]-xs[i],.5f,zs[j+1]-zs[j]),floor);
                foreach(Transform art in tile.transform){art.position+=Vector3.down*.035f;PrefabUtility.RecordPrefabInstancePropertyModifications(art);}
            }
        }
        SideWall("Warehouse perimeter west",new Vector3(-45,0,29),new Vector3(-45,0,188),18);
        SideWall("Warehouse perimeter east",new Vector3(45,0,29),new Vector3(45,0,188),18);
        SideWall("Warehouse perimeter north",new Vector3(-45,0,188),new Vector3(45,0,188),18);
        SideWall("Warehouse entrance wall",new Vector3(-45,0,29),new Vector3(45,0,29),18,Gap(0,5));
        Block("Shared warehouse roof",new Vector3(0,18.4f,108.5f),new Vector3(91,.6f,160),teal);
        for(int z=36;z<=180;z+=18){
            Block("Warehouse roof truss",new Vector3(0,17.5f,z),new Vector3(90,.6f,.65f),dark);
            foreach(int x in new[]{-44,-22,22,44})Block("Warehouse rounded column",new Vector3(x,8.5f,z),new Vector3(.9f,17,.9f),teal);
        }
        for(int x=-32;x<=32;x+=16)for(int z=40;z<=172;z+=22){Lamp("Warehouse task light",new Vector3(x,9,z),27,12);Block("Lamp suspension",new Vector3(x,13.5f,z),new Vector3(.05f,9,.05f),dark,null,false);}
        // Aisle paint guides exploration without walls.
        foreach(int x in new[]{-25,0,25})for(int z=43;z<132;z+=5)Block("Warehouse aisle paint",new Vector3(x,.012f,z),new Vector3(.12f,.018f,2),yellow,null,false);
        foreach(int side in new[]{-1,1})foreach(int z in new[]{48,90,114,132}){var pallet=Art("FI_OldCrates","Warehouse storage pallet",factory,new Vector3(side*16,0,z),side*15);var renderers=pallet.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);var c=pallet.AddComponent<BoxCollider>();c.center=pallet.transform.InverseTransformPoint(bounds.center);c.size=bounds.size;}}
        Label("โรงงานผู้ดูแล\nสำรวจจุดซ่อม 6 จุด แล้วช่วยผู้ดูแลท้ายโถง",new Vector3(0,5,36),0,.07f);
        foreach(var text in factory.GetComponentsInChildren<TextMesh>())text.text=text.text.Replace("ห้อง", "จุด");
        foreach(var puzzle in world.puzzles)if(puzzle)puzzle.titleTh=puzzle.titleTh.Replace("ห้อง", "จุด");
    }
    static void ThreeRoutes(){
        var p1=world.FindPuzzle("f1_gate");p1.conditions=p1.conditions.Where(c=>c.name!="Condition: chute chute_c").ToArray();p1.hintTh="เปิดรางใดรางหนึ่ง: ลากเหล็ก เป่าโฟม หรือเผากระดาษ";
        // Seal the removed heavy-crate demonstration lane with a permanent decorative service panel.
        foreach(var t in factory.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Chute C")||t.name=="Condition: chute chute_c").ToArray())if(t)Object.DestroyImmediate(t.gameObject);
        Block("Closed sorting service lane",new Vector3(.8f,1.1f,39.6f),new Vector3(1.2f,2.2f,.4f),teal,rooms["Receiving"]);
        foreach(var text in rooms["Receiving"].GetComponentsInChildren<TextMesh>())if(text.text=="ลังหนัก")text.text="ซ่อมบำรุง";
        foreach(int sign in new[]{-1,1})Block("Sorting shutter gantry",new Vector3(6.5f+sign*1.9f,3.75f,39),new Vector3(.4f,7.5f,.6f),teal);
        Block("Sorting shutter overhead housing",new Vector3(6.5f,7.25f,39),new Vector3(4.2f,.7f,1.1f),teal);
        p1.solvedTh="ระบบคัดแยกกลับมาทำงานแล้ว";
        var p2=world.FindPuzzle("f4_crane");p2.conditions=p2.conditions.Where(c=>c.name!="Wax crawl hatch hinge").ToArray();p2.hintTh="3 ทางหลัก: ตุ้มแม่เหล็ก / ยกเครื่องหนัก / ยิงสลักสนิม";
        foreach(var t in factory.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Wax")||t.name.StartsWith("Crawl duct")).ToArray())if(t)Object.DestroyImmediate(t.gameObject);
        // Plate has two meaningful tools, cannon is the third route; the unused crawl opening is walled off.
        Block("Closed former wax access",FactoryLayout.Map(new Vector3(17.6f,1.6f,51),"Crane"),new Vector3(2,3.2f,.4f),wall,rooms["Crane"]);
        var p4=world.FindPuzzle("f5_cooling");p4.titleTh="ทดสอบระบบข้ามร่องหล่อเย็น";p4.hintTh="ทดสอบข้ามจริง 1 ทาง: แพแม่เหล็ก / สะพานไฟฟ้า / เด้งจากคาน";
        var oldNorth=(AreaReachedCondition)p4.conditions[0];var crossing=oldNorth.gameObject.AddComponent<FactoryCrossingCondition>();crossing.world=world;crossing.northBank=oldNorth.Area;crossing.channel=new Bounds(new Vector3(-35.9f,2,69.75f),new Vector3(13.7f,12,9.5f));crossing.girderTip=new Bounds(new Vector3(-41.6f,1.5f,69.7f),new Vector3(3,5,3));crossing.raft=factory.GetComponentInChildren<ShuttleRaft>();crossing.bridge=rooms["Cooling"].GetComponentsInChildren<PoweredLift>().First(l=>l.name=="Service bridge deck");p4.conditions=new MonoBehaviour[]{crossing};Object.DestroyImmediate(oldNorth);
        foreach(var t in factory.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("North bank anchor")||t.name=="North bank post"||t.name=="Anchor marker" && t.IsChildOf(rooms["Cooling"])).ToArray())if(t)Object.DestroyImmediate(t.gameObject);
        var p5=world.FindPuzzle("f3_turbine");p5.conditions=p5.conditions.Where(c=>!(c is MaterialStateCondition m && m.label=="far-motor")).ToArray();p5.hintTh="3 ทางหลัก: เป่ากังหัน / ป้อนไอน้ำ / สายฟ้าชิ่ง 1-2-3";
        foreach(var t in factory.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Turbine far motor")||t.name=="Condition: far-motor"||t.name=="Far motor bracket").ToArray())if(t)Object.DestroyImmediate(t.gameObject);
        // Leave a dedicated long-spark opportunity as an optional secret, rather than a fourth main solution.
        var lamp=Block("Factory distance relay",new Vector3(0,5,101),Vector3.one*.7f,yellow);var l=lamp.AddComponent<ComboLock>();l.acceptedCombos=new[]{ComboTable.Id(BlobProperty.Pressure,BlobProperty.Electric)};
        var secret=new GameObject("Puzzle factory_distance_relay").AddComponent<PuzzleObjective>();secret.transform.SetParent(factory);secret.id="factory_distance_relay";secret.titleTh="ไฟทางเดินเหนือ";secret.isSecret=true;secret.mode=ConditionMode.Any;secret.conditions=new MonoBehaviour[]{l};secret.items=new PuzzleItem[0];secret.materials=new WorldMaterial[0];secret.area=new Bounds(new Vector3(0,2,96),new Vector3(7,5,12));
        var list=world.puzzles.ToList();list.Add(secret);world.puzzles=list.ToArray();
    }
    static void Restoration(){
        string[] ids={"f1_gate","f4_crane","f2_boiler","f5_cooling","f3_turbine","f6_assembly","factory_boss_repaired"};string[] roomIds={"Receiving","Crane","Boiler","Cooling","Turbine","Assembly","Central"};
        for(int i=0;i<ids.Length;i++){
            var p=world.FindPuzzle(ids[i]);var room=rooms[roomIds[i]];var restoration=p.gameObject.AddComponent<FactoryRestoration>();restoration.objective=p;restoration.roomName=roomIds[i];
            var mess=new List<Transform>();foreach(Transform t in room)if((t.name.StartsWith("Kit FI_Ru_")||t.name=="Kit FI_DustPile")&&!t.name.Contains("Girder")&&!t.name.Contains("WallBreach")&&!t.name.Contains("PlankBarricade")&&!t.name.Contains("FloorCracks")&&t.GetComponentsInChildren<Collider>(true).Length==0)mess.Add(t);
            restoration.debris=mess.ToArray();var centre=i==6?new Vector3(0,.2f,72):p.area.center;centre.y=.2f;restoration.destinations=mess.Select((t,k)=>centre+new Vector3((k%3)*.25f,0,0)).ToArray();
            var cleanSurfaces=new List<Renderer>();foreach(var t in factory.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith(roomIds[i]+" wall")||t.name.StartsWith(roomIds[i]+" floor"))cleanSurfaces.AddRange(t.GetComponentsInChildren<Renderer>());cleanSurfaces.AddRange(factory.GetComponentsInChildren<Renderer>().Where(r=>r.transform.parent && r.transform.parent.name.Contains("Open factory floor") && p.area.Contains(new Vector3(r.bounds.center.x,p.area.center.y,r.bounds.center.z))));restoration.surfaces=cleanSurfaces.Distinct().ToArray();
            restoration.obstacles=room.GetComponentsInChildren<Collider>(true).Where(c=>c.name.StartsWith("Ruin collider:") && mess.Any(t=>Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(c.transform.position.x,c.transform.position.z))<.1f)).ToArray();
            var light=Lamp(roomIds[i]+" restoration light",centre+Vector3.up*4.5f,18,1.4f);restoration.roomLights=new[]{light};
            if(i==1){var arm=room.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Kit FI_OverheadCrane");restoration.arm=arm;}
            // PressureTurbine already owns its rotor animation after restoration.
            var dust=new GameObject("Room restoration dust").AddComponent<ParticleSystem>();dust.transform.SetParent(room);dust.transform.position=centre+Vector3.up*1.5f;var main=dust.main;main.startLifetime=2;main.startSpeed=.2f;main.startSize=.08f;main.startColor=new Color(.6f,.55f,.4f,.2f);main.maxParticles=60;var emission=dust.emission;emission.rateOverTime=4;restoration.dust=dust;dust.GetComponent<ParticleSystemRenderer>().sharedMaterial=Mat("Dust");
        }
    }
    static BlobInteractable SourcePower(BlobProperty prop,Vector3 pos){
        var source=factory.GetComponentsInChildren<BlobInteractable>(true).First(s=>s.property==prop);var go=Object.Instantiate(source.gameObject,factory);go.name="Boss supply "+prop;go.transform.position=pos;return go.GetComponent<BlobInteractable>();
    }
    static Transform BossArt(string asset,Transform parent,Vector3 local){var g=Art(asset,asset,parent,parent.TransformPoint(local));g.transform.localRotation=Quaternion.identity;PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);return g.transform;}
    static void Boss(){

        var arena=Art("FE_BossArena","Caretaker circular repair arena",factory,new Vector3(0,0,164));var collider=arena.AddComponent<BoxCollider>();collider.center=new Vector3(0,-.25f,0);collider.size=new Vector3(44,.5f,44);
        var go=new GameObject("Caretaker - repair boss");go.transform.SetParent(factory);go.transform.position=new Vector3(0,0,177);var boss=go.AddComponent<FactoryRepairBoss>();boss.world=world;boss.arenaCenter=new Vector3(0,0,164);boss.checkpoint=new Vector3(0,.1f,140);boss.requiredRepairs=new[]{"f1_gate","f4_crane","f2_boiler","f5_cooling","f3_turbine","f6_assembly"}.Select(id=>world.FindPuzzle(id)).ToArray();
        BossArt("FE_BossBase",go.transform,Vector3.zero);boss.body=BossArt("FE_BossBody",go.transform,new Vector3(0,3,0));
        var le=BossArt("FE_BossEye",boss.body,new Vector3(-2.6f,5.7f,-2.5f));var re=BossArt("FE_BossEye",boss.body,new Vector3(2.6f,5.7f,-2.5f));boss.leftBrow=BossArt("FE_BossBrow",boss.body,new Vector3(-2.6f,7.05f,-2.65f));boss.rightBrow=BossArt("FE_BossBrow",boss.body,new Vector3(2.6f,7.05f,-2.65f));
        boss.leftArm=BossArt("FE_BossArm",go.transform,new Vector3(-5.3f,6.6f,0));boss.leftArm.localRotation=Quaternion.Euler(0,180,0);PrefabUtility.RecordPrefabInstancePropertyModifications(boss.leftArm);
        boss.rightArm=BossArt("FE_BossArm",go.transform,new Vector3(5.3f,6.6f,0));var lh=BossArt("FE_BossHand",boss.leftArm,new Vector3(4.5f,0,0));lh.localRotation=Quaternion.Euler(0,180,0);PrefabUtility.RecordPrefabInstancePropertyModifications(lh);BossArt("FE_BossHand",boss.rightArm,new Vector3(4.5f,0,0));
        var jam=new GameObject("Boss jammed intake - physical scrap only");jam.transform.SetParent(go.transform,false);jam.transform.localPosition=new Vector3(0,6.3f,-3.2f);var jc=jam.AddComponent<BoxCollider>();jc.size=new Vector3(3.7f,3,1.2f);var jr=jam.AddComponent<BossJamReceiver>();jr.boss=boss;
        var torso=go.AddComponent<BoxCollider>();torso.center=new Vector3(0,7,0);torso.size=new Vector3(10,8,4.5f);
        boss.bodyRenderers=boss.body.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.Contains("Teal")).ToArray();boss.eyes=le.GetComponentsInChildren<Renderer>().Concat(re.GetComponentsInChildren<Renderer>()).Where(r=>r.name.Contains("Blue")).ToArray();
        boss.warning=Block("Boss attack warning",new Vector3(0,.03f,160),new Vector3(1,.03f,1),Mat("Coral"),null,false).transform;boss.warning.gameObject.SetActive(false);
        var pz=new GameObject("Puzzle factory_boss_repaired").AddComponent<PuzzleObjective>();pz.transform.SetParent(factory);pz.id="factory_boss_repaired";pz.titleTh="ซ่อมผู้ดูแลโรงงาน";pz.hintTh="ใช้เฟืองยิงช่องติดขัด เปิดปั๊มน้ำ แล้วใส่เฟืองอะไหล่และเชื่อมซ่อม";pz.solvedTh="ผู้ดูแลกลับมาเป็นปกติแล้ว";pz.area=new Bounds(new Vector3(0,5,164),new Vector3(44,15,44));pz.mode=ConditionMode.Any;pz.conditions=new MonoBehaviour[]{boss};pz.items=new PuzzleItem[0];pz.materials=new WorldMaterial[0];boss.objective=pz;
        var pump=Art("FI_GiantBoiler","Boss environmental cooling pump",factory,new Vector3(-9,0,165));pump.transform.localScale=Vector3.one*.35f;PrefabUtility.RecordPrefabInstancePropertyModifications(pump.transform);var pc=pump.AddComponent<BoxCollider>();pc.center=new Vector3(0,2,0);pc.size=new Vector3(4,4,4);var ps=pump.AddComponent<BossServiceStation>();ps.boss=boss;ps.station=1;
        var bench=Block("Boss repair bench",new Vector3(9,.225f,166),new Vector3(3,.45f,2.5f),teal);var bs=bench.AddComponent<BossServiceStation>();bs.boss=boss;bs.station=2;var work=bench.AddComponent<BoxCollider>();work.isTrigger=true;work.center=new Vector3(0,.8f,0);work.size=new Vector3(3,1.8f,2.5f);Block("Boss gear socket highlight",new Vector3(9,.46f,166),new Vector3(1.3f,.03f,1.3f),yellow,null,false);var socket=new GameObject("Boss repair gear socket");socket.transform.SetParent(factory);socket.transform.position=new Vector3(9,.9f,166);boss.repairSocket=socket.transform;
        var parts=new List<Transform>();for(int i=0;i<3;i++){var g=Art("FE_BossGear","Boss spare gear "+i,factory,new Vector3(-5+i*5,.5f,155));var c=g.AddComponent<BoxCollider>();c.size=new Vector3(.8f,.8f,.8f);var rb=g.AddComponent<Rigidbody>();rb.mass=3;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;g.AddComponent<BlobMagnetic>();var part=g.AddComponent<BossRepairPart>();part.forRepair=true;parts.Add(g.transform);}boss.scrap=parts.ToArray();
        foreach(var item in new[]{Tuple.Create(BlobProperty.Magnet,-7f),Tuple.Create(BlobProperty.Pressure,-4f),Tuple.Create(BlobProperty.Water,-1f),Tuple.Create(BlobProperty.Electric,2f),Tuple.Create(BlobProperty.Heat,5f),Tuple.Create(BlobProperty.Bounce,8f)})SourcePower(item.Item1,new Vector3(item.Item2,0,138));
        Label("ผู้ดูแลเสียการควบคุม\n1 ยิงเฟืองเข้าช่องติดขัด\n2 เปิดปั๊มน้ำซ้าย\n3 ใส่เฟืองบนโต๊ะขวาแล้วเชื่อม",new Vector3(0,3,140),0,.06f);
        var gate=Block("Caretaker safety status indicator",new Vector3(3.5f,1.2f,140),new Vector3(.6f,2.4f,.6f),teal);var gg=new GameObject("Zone gate: caretaker").AddComponent<ZoneGate>();gg.transform.SetParent(factory);gg.world=world;gg.zoneId="factory_caretaker_arena";gg.zoneNameTh="ลานซ่อมผู้ดูแล";gg.requires=new[]{"f1_gate","f4_crane","f2_boiler","f5_cooling","f3_turbine","f6_assembly"}.Select(id=>world.FindPuzzle(id)).ToArray();gg.requiresAnyOf=new PuzzleObjective[0];gg.door=gate.transform;gg.openOffset=Vector3.up*5;
        world.puzzles=world.puzzles.Concat(new[]{pz}).ToArray();world.zoneGates=world.zoneGates.Concat(new[]{gg}).ToArray();Lamp("Caretaker arena light",new Vector3(0,10,168),32,18);Lamp("Caretaker warm key",new Vector3(-9,8,169),22,12).color=new Color(1,.86f,.67f);Lamp("Caretaker cool fill",new Vector3(9,8,169),22,10).color=new Color(.63f,.86f,1);
    }
    static void BuildFactory(){
        var scene=EditorSceneManager.GetActiveScene();world=Object.FindFirstObjectByType<BlobWorld>();font=world.uiFont;factory=GameObject.Find("FACTORY INTERIOR").transform;factory.SetParent(null,true);
        var cargo=new GameObject("Factory shared cargo").AddComponent<ShipRepair>();cargo.world=world;cargo.parts=factory.GetComponentsInChildren<ShipPart>(true);cargo.stageObjects=new GameObject[0];cargo.installArea=new Bounds(new Vector3(1000,0,1000),Vector3.one);world.ship=cargo;foreach(var p in cargo.parts)p.ship=cargo;
        foreach(var rootName in new[]{"OOPS V2 GREYBOX","Soft World Garden","World polish accents"}){var g=GameObject.Find(rootName);if(g)Object.DestroyImmediate(g);}
        world.hub=null;world.preserveOtherSceneProgress=true;world.spawn=new Vector3(0,.1f,32);world.checkpoint=world.spawn;world.levelTitle="OOPS! PLANET — โรงงานผู้ดูแล";world.zones=new BlobInteractable[0];world.puzzles=factory.GetComponentsInChildren<PuzzleObjective>(true);world.sources=factory.GetComponentsInChildren<BlobInteractable>(true);world.zoneGates=factory.GetComponentsInChildren<ZoneGate>(true);
        floor=AssetDatabase.LoadAssetAtPath<Material>(Base+"Materials/FactoryInterior/FI_FloorWorn.mat");wall=AssetDatabase.LoadAssetAtPath<Material>(Base+"Materials/FactoryInterior/FI_WallCreamPeeled.mat");teal=Mat("Teal");dark=Mat("Dark");yellow=Mat("Yellow");
        TranslateRoomObjects();ShellAndRoutes();OpenHall();ThreeRoutes();Boss();Restoration();world.sources=factory.GetComponentsInChildren<BlobInteractable>(true);
        world.player.transform.position=world.spawn;world.player.yaw=0;world.player.pitch=18;
        var link=world.gameObject.AddComponent<FactorySceneLink>();link.world=world;var exit=new GameObject("Factory exit portal").AddComponent<FactoryPortal>();exit.transform.SetParent(factory);exit.transform.position=new Vector3(0,1.4f,30.1f);exit.world=world;exit.toInterior=false;
        if(world.ambience)world.ambience.zones=new[]{new ZoneAmbience.Zone{name="Factory",area=new Bounds(new Vector3(0,5,110),new Vector3(120,30,180)),normal=Ambience.Factory,restored=Ambience.FactoryRestored,restoredWhen=new[]{world.FindPuzzle("factory_boss_repaired")}}};
        foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.type==LightType.Directional)l.intensity=l.name=="Directional Light"?.48f:.12f;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.31f,.36f,.39f);RenderSettings.fog=false;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,FactoryTravel.Interior);AssetDatabase.SaveAssets();
    }
    static void MakeOutsideFacade(){
        world=Object.FindFirstObjectByType<BlobWorld>();font=world.uiFont;var old=GameObject.Find("FACTORY INTERIOR");if(!old){Debug.Log("Outside facade already installed; preserving outside edits");return;}
        var proxies=new GameObject("Factory progress metadata");var replacements=new Dictionary<Object,Object>();
        foreach(var p in old.GetComponentsInChildren<PuzzleObjective>(true)){var g=new GameObject(p.name+" metadata");g.transform.SetParent(proxies.transform);g.transform.position=new Vector3(0,-1000,0);var q=g.AddComponent<PuzzleObjective>();q.id=p.id;q.titleTh=p.titleTh;q.hintTh=p.hintTh;q.solvedTh=p.solvedTh;q.isSecret=p.isSecret;q.mode=ConditionMode.Any;q.area=new Bounds(new Vector3(0,-1000,0),Vector3.one);q.conditions=new MonoBehaviour[0];q.items=new PuzzleItem[0];q.materials=new WorldMaterial[0];replacements[p]=q;}
        foreach(var p in old.GetComponentsInChildren<ShipPart>(true)){var g=new GameObject(p.name+" metadata");g.transform.SetParent(proxies.transform);g.transform.position=new Vector3(0,-1000,0);var q=g.AddComponent<ShipPart>();q.id=p.id;q.nameTh=p.nameTh;q.zone=p.zone;q.ship=world.ship;q.pickupRadius=-1;replacements[p]=q;}
        var oldSources=new HashSet<BlobInteractable>(old.GetComponentsInChildren<BlobInteractable>(true));world.sources=world.sources.Where(s=>s&&!oldSources.Contains(s)).ToArray();world.zoneGates=world.zoneGates.Where(g=>g&&!g.transform.IsChildOf(old.transform)).ToArray();world.zones=world.zones.Where(z=>z && !(z.transform.position.z>25 && z.transform.position.z<76 && Mathf.Abs(z.transform.position.x)<28)).ToArray();
        foreach(var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(!component||component.transform.IsChildOf(old.transform))continue;var so=new SerializedObject(component);var property=so.GetIterator();bool changed=false;while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue && replacements.TryGetValue(property.objectReferenceValue,out var replacement)){property.objectReferenceValue=replacement;changed=true;}if(changed)so.ApplyModifiedPropertiesWithoutUndo();}
        Object.DestroyImmediate(old);
        var top=GameObject.Find("OOPS V2 GREYBOX");foreach(Transform t in top.transform.Cast<Transform>().ToArray())if(t.position.z>28&&t.position.z<76&&Mathf.Abs(t.position.x)<28 && !t.GetComponent<ShipRepair>())Object.DestroyImmediate(t.gameObject);
        world.sources=world.sources.Where(s=>s).ToArray();world.puzzles=world.puzzles.Where(p=>p).ToArray();world.zoneGates=world.zoneGates.Where(g=>g).ToArray();world.zones=world.zones.Where(z=>z).ToArray();world.ship.parts=world.ship.parts.Where(p=>p).ToArray();world.preserveOtherSceneProgress=true;
        factory=new GameObject("Factory entrance facade only").transform;Art("FE_Entrance","Factory entrance",factory,new Vector3(0,0,26));
        var portal=new GameObject("Walk into factory").AddComponent<FactoryPortal>();portal.transform.SetParent(factory);portal.transform.position=new Vector3(0,1.4f,25);portal.world=world;portal.toInterior=true;
        var link=world.gameObject.GetComponent<FactorySceneLink>()??world.gameObject.AddComponent<FactorySceneLink>();link.world=world;
        Label("โรงงานของเล่น\nเดินผ่านประตูเพื่อเข้า",new Vector3(0,6.1f,24.9f),0,.07f);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
    [MenuItem("Tools/OOPS Planet/Factory Expansion/Build Separate Factory And Entrance")]
    public static void Build(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play Mode first");
        if(SceneManager.GetActiveScene().path!=FactoryTravel.Outside)EditorSceneManager.OpenScene(FactoryTravel.Outside);
        var main=SceneManager.GetActiveScene();if(main.isDirty)EditorSceneManager.SaveScene(main);
        Directory.CreateDirectory("Backups");File.Copy(FactoryTravel.Outside,"Backups/Before_Factory_Expansion_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
        if(!File.Exists(Source))EditorSceneManager.SaveScene(main,Source,true);
        EditorSceneManager.OpenScene(Source);BuildFactory();
        EditorSceneManager.OpenScene(FactoryTravel.Outside);MakeOutsideFacade();
        var entrance=GameObject.Find("Factory entrance facade only");if(entrance)foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(text.text.Contains("โรงงาน")&&!text.transform.IsChildOf(entrance.transform)&&Mathf.Abs(text.transform.position.x)<8&&text.transform.position.z>17&&text.transform.position.z<29)Object.DestroyImmediate(text.gameObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        var scenes=EditorBuildSettings.scenes.ToList();foreach(var path in new[]{FactoryTravel.Outside,FactoryTravel.Interior}){var entry=scenes.FirstOrDefault(s=>s.path==path);if(entry!=null)entry.enabled=true;else scenes.Add(new EditorBuildSettingsScene(path,true));}EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
        Debug.Log("FACTORY_EXPANSION_BUILT separate scene, three main routes per room, restoration and caretaker repair encounter");
    }
}
}
#endif
