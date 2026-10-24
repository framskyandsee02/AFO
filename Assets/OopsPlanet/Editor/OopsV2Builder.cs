#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace OopsPlanet.Editor {
// Design v2 greybox (Docs/DESIGN_V2_TH.md §6–7): Hub crash site + Factory (north) + Rice Fields & canal (east) in
// its own scene, so Prototype 01 and its tests stay as they are. Every shape here is a placeholder primitive -
// final models need the team's references first (§9).
//  Hub     : H1 crash-pit exit, BLOB's ship (repair = demo goal), 6 training sources, 2 hidden spots
//  Factory : F1 gate (5 ways), F2 boiler room walkway (5 ways), F3 turbine (4 ways), 5 hidden spots, conveyor shortcut
//  Rice    : R1 straw sluice (5 ways), R2 canal (5 ways), R3 pump (6 ways), 2 hidden spots, dike shortcut
public static class OopsV2Builder {
    const string Root="Assets/OopsPlanet";
    public const string ScenePath=Root+"/Scenes/OopsPlanet_V2.unity";
    const float WallH=8f;
    static Transform root;
    static Font uiFont;
    static List<BlobInteractable> zones, sources;
    static List<ShipPart> parts;
    static List<PuzzleObjective> puzzles;
    static BlobWorld world;
    static ShipRepair ship;
    static Material grey, dark, gridM, metal, rust, wood, strawM, waterM, iceM, waxM, plantM, mudM, machineM, glowGreen, hazard, paddy, partM, steelBeam;
    // ---------- helpers ----------
    static Material Mat(string name,Color color,bool emission=false) {
        string path=Root+"/Materials/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path); if(mat) return mat;
        var shader=Shader.Find("Universal Render Pipeline/Lit"); if(!shader) throw new System.Exception("URP Lit shader unavailable");
        mat=new Material(shader){name=name}; mat.SetColor("_BaseColor",color); mat.SetFloat("_Smoothness",.18f);
        if(emission) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor",color*1.5f); }
        AssetDatabase.CreateAsset(mat,path); return mat;
    }
    static GameObject Box(string name,Vector3 center,Vector3 size,Material mat,Transform parent=null,bool collider=true) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
        go.transform.SetParent(parent?parent:root); go.transform.position=center; go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    // Box standing on the ground (pos = centre of the footprint at its base).
    static GameObject Block(string name,Vector3 basePos,Vector3 size,Material mat,Transform parent=null,bool collider=true) => Box(name,basePos+Vector3.up*size.y/2,size,mat,parent,collider);
    static GameObject Prim(PrimitiveType type,string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null,bool collider=true) {
        var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent?parent:root);
        go.transform.position=pos; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static WorldMaterial WM(GameObject go,MaterialKind kind,bool critical=false) { var m=go.AddComponent<WorldMaterial>(); m.kind=kind; m.critical=critical; return m; }
    static void Label(string text,Vector3 pos,float yaw=0,float size=.3f) {
        var go=new GameObject("Sign "+text.Split('\n')[0]); go.transform.SetParent(root); go.transform.position=pos; go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var tm=go.AddComponent<TextMesh>(); tm.text=text; tm.characterSize=size*.45f; tm.fontSize=64; tm.anchor=TextAnchor.MiddleCenter; tm.alignment=TextAlignment.Center;
        tm.color=new Color(.93f,.97f,1); tm.font=uiFont; go.GetComponent<Renderer>().sharedMaterial=uiFont.material;
        var b=go.GetComponent<Renderer>().bounds;
        var backing=Box("Sign backing",b.center+go.transform.forward*.04f,Vector3.one,dark,go.transform,false);
        backing.transform.localRotation=Quaternion.identity; backing.transform.localScale=new Vector3(Mathf.Max(b.size.x,b.size.z)+.4f,b.size.y+.25f,.05f);
    }
    static void Floor(string name,Vector3 center,float sx,float sz,Material mat) {
        Box(name,center-Vector3.up*.5f,new Vector3(sx,1,sz),mat);
        for(float x=-sx/2+2;x<sx/2;x+=2) Box("2m grid",center+new Vector3(x,.008f,0),new Vector3(.025f,.012f,sz),gridM,null,false);
        for(float z=-sz/2+2;z<sz/2;z+=2) Box("2m grid",center+new Vector3(0,.009f,z),new Vector3(sx,.012f,.025f),gridM,null,false);
    }
    // Straight wall from a to b (x/z) with door-sized openings (centre along the wall, width); optional lintel over each.
    static void Wall(string name,Vector2 a,Vector2 b,float height,Material mat,float thick=.5f,float y0=0,params Vector3[] openings) {
        bool alongX=Mathf.Abs(b.x-a.x)>Mathf.Abs(b.y-a.y);
        float from=alongX?Mathf.Min(a.x,b.x):Mathf.Min(a.y,b.y), to=alongX?Mathf.Max(a.x,b.x):Mathf.Max(a.y,b.y), fixedC=alongX?a.y:a.x;
        var cuts=new List<Vector3>(openings); cuts.Sort((p,q)=>p.x.CompareTo(q.x));
        float cursor=from;
        void Seg(float s,float e,float yb,float yt) {
            if(e-s<.01f || yt-yb<.01f) return;
            float mid=(s+e)/2, len=e-s;
            Box(name,alongX?new Vector3(mid,(yb+yt)/2,fixedC):new Vector3(fixedC,(yb+yt)/2,mid),alongX?new Vector3(len,yt-yb,thick):new Vector3(thick,yt-yb,len),mat);
        }
        foreach(var o in cuts) {   // o = (centre, width, opening height)
            Seg(cursor,o.x-o.y/2,y0,y0+height);
            Seg(o.x-o.y/2,o.x+o.y/2,y0+o.z,y0+height);   // lintel above the opening
            cursor=o.x+o.y/2;
        }
        Seg(cursor,to,y0,y0+height);
    }
    static Vector3 Opening(float centre,float width,float height=WallH) => new Vector3(centre,width,height);
    static BlobInteractable Zone(string name,BlobInteraction kind,Vector3 pos,Vector3 size,string label=null) {
        var go=new GameObject(name); go.transform.SetParent(root); go.transform.position=pos;
        var z=go.AddComponent<BlobInteractable>(); z.kind=kind; z.size=size; z.label=label; zones.Add(z); return z;
    }
    static void Checkpoint(int index,Vector3 p,bool alongX) {
        var z=Zone("Checkpoint "+index,BlobInteraction.Checkpoint,p+Vector3.up,alongX?new Vector3(3,3,6):new Vector3(6,3,3)); z.index=index; z.respawn=p+Vector3.up*.1f;
        Box("Checkpoint pad "+index,p+Vector3.up*.03f,alongX?new Vector3(2,.06f,4):new Vector3(4,.06f,2),glowGreen,null,false);
    }
    // Absorbable source made of primitives (greybox stand-in for the §3.2 scene objects). Deplete + regen keeps the
    // object in the scene (it greys out when drained) and brings it back after 20 s - puzzle sources never run out.
    static BlobInteractable Source(string name,BlobProperty prop,Vector3 basePos,PrimitiveType shape,Vector3 size,Color color,float regen=20f) {
        var rootGO=new GameObject(name); rootGO.transform.SetParent(root); rootGO.transform.position=basePos;
        var full=Prim(shape,"Full",basePos+Vector3.up*size.y/2,shape==PrimitiveType.Cylinder?new Vector3(size.x,size.y/2,size.z):size,Mat("V2 src "+prop+" "+ColorUtility.ToHtmlStringRGB(color),color),rootGO.transform);
        var s=rootGO.AddComponent<BlobInteractable>();
        s.kind=BlobInteraction.Source; s.property=prop; s.onAbsorbed=regen>0?AbsorbResult.Deplete:AbsorbResult.Remains; s.regenSeconds=regen;
        s.fullVisual=full; s.indicator=full.GetComponent<Renderer>();
        // Thai name: shown only in sense mode (AbsorbableVisual).
        string n=prop==BlobProperty.Magnet?"แม่เหล็ก":prop==BlobProperty.Pressure?"แรงดัน":prop==BlobProperty.Bounce?"เด้ง":prop==BlobProperty.Water?"น้ำ":prop==BlobProperty.Electric?"ไฟฟ้า":"ความร้อน";
        var b=s.indicator.bounds;
        var lg=new GameObject("Power label "+n); lg.transform.SetParent(rootGO.transform); lg.transform.position=new Vector3(b.center.x,b.max.y+.55f,b.center.z);
        var tm=lg.AddComponent<TextMesh>(); tm.text=n; tm.font=uiFont; tm.fontSize=64; tm.characterSize=.035f; tm.anchor=TextAnchor.MiddleCenter; tm.color=BlobPlayer.PropertyColor(prop);
        lg.GetComponent<Renderer>().sharedMaterial=uiFont.material; lg.AddComponent<FaceCamera>(); s.powerLabel=lg; lg.SetActive(false);
        rootGO.AddComponent<AbsorbableVisual>().source=s;
        sources.Add(s); return s;
    }
    static readonly Color cMagnet=new Color(.5f,.52f,.55f), cPressure=new Color(.3f,.5f,.75f), cBounce=new Color(.12f,.12f,.14f), cWater=new Color(.3f,.55f,.7f), cElectric=new Color(.85f,.8f,.35f), cHeat=new Color(.35f,.2f,.15f);
    static void SourceSet(string zone,BlobProperty prop,Vector3 pos) {
        switch(prop) {
            case BlobProperty.Magnet: Source(zone+" Magnet source (scrap)",prop,pos,PrimitiveType.Cube,new Vector3(1,.6f,.8f),cMagnet); break;
            case BlobProperty.Pressure: Source(zone+" Pressure source (gas tank)",prop,pos,PrimitiveType.Cylinder,new Vector3(.7f,1.3f,.7f),cPressure); break;
            case BlobProperty.Bounce: Source(zone+" Bounce source (tyre / spring)",prop,pos,PrimitiveType.Cylinder,new Vector3(1.1f,.4f,1.1f),cBounce); break;
            case BlobProperty.Water: Source(zone+" Water source (barrel)",prop,pos,PrimitiveType.Cylinder,new Vector3(.9f,1.1f,.9f),cWater); break;
            case BlobProperty.Electric: Source(zone+" Electric source (battery / lamp)",prop,pos,PrimitiveType.Cube,new Vector3(.6f,.9f,.6f),cElectric); break;
            default: Source(zone+" Heat source (coal brazier)",prop,pos,PrimitiveType.Cylinder,new Vector3(1,.7f,1),cHeat); break;
        }
    }
    static ShipPart Part(string id,string nameTh,string zone,Vector3 pos,bool startActive=true) {
        var go=new GameObject("Ship part: "+id); go.transform.SetParent(root); go.transform.position=pos;
        var p=go.AddComponent<ShipPart>(); p.id=id; p.nameTh=nameTh; p.zone=zone; p.ship=ship;
        var v=Box("Part visual",pos,new Vector3(.45f,.45f,.45f),partM,go.transform,false); v.transform.rotation=Quaternion.Euler(30,45,0);
        Prim(PrimitiveType.Sphere,"Part glow",pos,Vector3.one*.25f,partM,v.transform,false);
        p.visual=v.transform;
        go.SetActive(startActive);
        parts.Add(p); return p;
    }
    static PuzzleObjective Puzzle(string name,string id,string titleTh,string hintTh,string solvedTh,Bounds area,PuzzleObjective requires,MonoBehaviour[] conditions,PuzzleItem[] items=null,WorldMaterial[] mats=null,ConditionMode mode=ConditionMode.Any,bool secret=false) {
        var go=new GameObject(name); go.transform.SetParent(root); go.transform.position=area.center;
        var p=go.AddComponent<PuzzleObjective>();
        p.id=id; p.titleTh=titleTh; p.hintTh=hintTh; p.solvedTh=solvedTh; p.area=area; p.requires=requires;
        p.conditions=conditions; p.items=items??new PuzzleItem[0]; p.materials=mats??new WorldMaterial[0]; p.mode=mode; p.isSecret=secret;
        p.celebratePoint=area.center;
        puzzles.Add(p); return p;
    }
    static PuzzleReaction React(PuzzleObjective pz,GameObject[] show=null,GameObject[] hide=null,Transform mover=null,Vector3 offset=default,Renderer[] recolor=null,Color? color=null,Light[] lights=null) {
        var r=pz.gameObject.AddComponent<PuzzleReaction>(); r.objective=pz;
        r.showWhenSolved=show??new GameObject[0]; r.hideWhenSolved=hide??new GameObject[0];
        r.mover=mover; r.moveOffset=offset; r.recolor=recolor??new Renderer[0]; if(color.HasValue) r.solvedColor=color.Value;
        r.lights=lights??new Light[0]; r.stopWhenSolved=new ParticleSystem[0]; r.grow=new Transform[0];
        return r;
    }
    static T Cond<T>(string name,Vector3 pos) where T:MonoBehaviour { var go=new GameObject(name); go.transform.SetParent(root); go.transform.position=pos; return go.AddComponent<T>(); }
    static MaterialStateCondition State(string label,MaterialGoal goal,params WorldMaterial[] targets) {
        var c=Cond<MaterialStateCondition>("Condition: "+label,targets[0].transform.position); c.label=label; c.goal=goal; c.targets=targets; return c;
    }
    static AreaReachedCondition Reach(string name,Vector3 center,Vector3 size) { var c=Cond<AreaReachedCondition>(name,center); c.world=world; c.size=size; return c; }
    static Rigidbody Body(GameObject go,float mass) {
        var rb=go.AddComponent<Rigidbody>(); rb.mass=mass; rb.linearDamping=.5f; rb.angularDamping=2;
        rb.interpolation=RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic; return rb;
    }
    static ZoneGate Gate(string id,string nameTh,Vector3 doorCenter,Vector3 doorSize,PuzzleObjective[] requires,PuzzleObjective[] anyOf=null,int anyCount=0) {
        var door=Box("Gate door: "+id,doorCenter,doorSize,hazard);
        var lamp=Prim(PrimitiveType.Sphere,"Gate lamp: "+id,doorCenter+Vector3.up*(doorSize.y/2+.5f),Vector3.one*.4f,Mat("V2 lamp red",new Color(.9f,.15f,.1f),true),null,false);
        var g=new GameObject("Zone gate: "+id).AddComponent<ZoneGate>(); g.transform.SetParent(root); g.transform.position=doorCenter-Vector3.up*doorSize.y/2;
        g.world=world; g.zoneId=id; g.zoneNameTh=nameTh; g.requires=requires??new PuzzleObjective[0]; g.requiresAnyOf=anyOf??new PuzzleObjective[0]; g.anyCount=anyCount;
        g.door=door.transform; g.openOffset=Vector3.up*(doorSize.y+.2f); g.lamp=lamp.GetComponent<Renderer>();
        return g;
    }
    static void ResetPad(string name,Vector3 pos,PuzzleObjective target) {
        var pad=Box(name,pos+Vector3.up*.03f,new Vector3(2,.06f,2),hazard,null,false);
        var r=pad.AddComponent<PuzzleResetPad>(); r.target=target; r.size=new Vector3(2,2,2);
        Label("รีเซ็ต RESET",pos+Vector3.up*1.2f,0,.12f);
    }
    // ---------- data ----------
    static T DataAsset<T>(string name,System.Action<T> init) where T:ScriptableObject {
        string path=Root+"/Data/"+name+".asset";
        var a=AssetDatabase.LoadAssetAtPath<T>(path); if(a) return a;
        a=ScriptableObject.CreateInstance<T>(); init(a); AssetDatabase.CreateAsset(a,path); return a;
    }
    static PowerSet EnsurePowerData() {
        Directory.CreateDirectory(Root+"/Data"); AssetDatabase.Refresh();
        var magnet=AssetDatabase.LoadAssetAtPath<PowerDefinition>(Root+"/Data/Power_Magnet.asset");
        var pressure=AssetDatabase.LoadAssetAtPath<PowerDefinition>(Root+"/Data/Power_Pressure.asset");
        var bounce=AssetDatabase.LoadAssetAtPath<PowerDefinition>(Root+"/Data/Power_Bounce.asset");
        var cannon=AssetDatabase.LoadAssetAtPath<CombinationDefinition>(Root+"/Data/Combo_MagnetPressure.asset");
        var dash=AssetDatabase.LoadAssetAtPath<CombinationDefinition>(Root+"/Data/Combo_PressureBounce.asset");
        var table=AssetDatabase.LoadAssetAtPath<ComboTable>(Root+"/Data/ComboTable_V2.asset");
        if(!magnet||!pressure||!bounce||!cannon||!dash||!table) throw new System.Exception("Run 'Rebuild Prototype 01' once first: v1 power data / ComboTable_V2 missing");
        var water=DataAsset<PowerDefinition>("Power_Water",d=>{ d.property=BlobProperty.Water; d.nameTh="น้ำ"; d.nameEn="Water"; d.hintTh="กดค้างเพื่อฉีดน้ำ: ดับไฟ รดพืช ล้างโคลน ทำให้เปียก (นำไฟฟ้า)"; d.range=7; });
        var electric=DataAsset<PowerDefinition>("Power_Electric",d=>{ d.property=BlobProperty.Electric; d.nameTh="ไฟฟ้า"; d.nameEn="Electric"; d.hintTh="กดเพื่อปล่อยประกายใส่เป้า: เปิดเครื่อง วิ่งผ่านโลหะและน้ำ"; d.range=9; d.cooldown=.5f; });
        var heat=DataAsset<PowerDefinition>("Power_Heat",d=>{ d.property=BlobProperty.Heat; d.nameTh="ความร้อน"; d.nameEn="Heat"; d.hintTh="กดค้างเพื่อแผ่ความร้อน: เผาฟาง ละลายน้ำแข็ง/ขี้ผึ้ง ทำโคลนแห้ง ต้มน้ำ"; d.range=4.5f; });
        var set=DataAsset<PowerSet>("PowerSet_V2",s=>{ s.powers.AddRange(new[]{magnet,pressure,bounce,water,electric,heat}); s.combinations.AddRange(new[]{cannon,dash}); s.table=table; });
        AssetDatabase.SaveAssets();
        return set;
    }
    // ---------- build ----------
    [MenuItem("Tools/OOPS Planet/V2/Rebuild V2 Greybox Scene (overwrites OopsPlanet_V2)")]
    public static void Rebuild() => RebuildScene(false);
    [MenuItem("Tools/OOPS Planet/V2/Rebuild V2 With Soft World Art (overwrites OopsPlanet_V2)")]
    public static void RebuildWithSoftWorld() => RebuildScene(true);
    static void RebuildScene(bool softWorld) {
        if(EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode before building a scene");
        foreach(var d in new[]{"Scenes","Materials"}) Directory.CreateDirectory(Root+"/"+d);
        AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        root=new GameObject("OOPS V2 GREYBOX").transform;
        uiFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansThai-Variable.ttf");
        if(!uiFont) throw new System.Exception("Thai UI font missing");
        zones=new List<BlobInteractable>(); sources=new List<BlobInteractable>(); parts=new List<ShipPart>(); puzzles=new List<PuzzleObjective>();
        grey=Mat("Hub concrete",new Color(.5f,.53f,.55f)); dark=Mat("Structural charcoal",new Color(.15f,.2f,.25f)); gridM=Mat("Grid lines",new Color(.62f,.66f,.69f));
        metal=Mat("Metal grey",new Color(.45f,.5f,.55f)); rust=Mat("Rust",new Color(.55f,.28f,.15f)); wood=Mat("Crate wood",new Color(.6f,.45f,.25f));
        strawM=Mat("Straw",new Color(.86f,.74f,.38f)); paddy=Mat("Paddy soil",new Color(.42f,.36f,.25f)); hazard=Mat("Hazard yellow",new Color(.95f,.75f,.1f));
        waterM=Mat("V2 water",new Color(.2f,.45f,.75f)); iceM=Mat("V2 ice",new Color(.72f,.9f,1)); waxM=Mat("V2 wax",new Color(.95f,.88f,.7f));
        plantM=Mat("V2 plant seedling",new Color(.5f,.55f,.3f)); mudM=Mat("V2 mud",new Color(.35f,.25f,.15f)); machineM=Mat("V2 machine",new Color(.3f,.32f,.42f));
        glowGreen=Mat("Checkpoint green",new Color(.17f,.8f,.38f),true); partM=Mat("V2 ship part",new Color(1,.78f,.2f),true); steelBeam=Mat("V2 steel beam",new Color(.4f,.42f,.48f));
        // Temporary blockout palette; semantic power, hazard and collectible colors remain readable.
        // Separate material assets keep Prototype 01 and the opt-in Soft World version intact.
        if(!softWorld) {
            grey=Mat("Greybox Floor",new Color(.56f,.56f,.56f));
            dark=Mat("Greybox Structure",new Color(.28f,.28f,.28f));
            gridM=Mat("Greybox 2m Grid",new Color(.68f,.68f,.68f));
            metal=Mat("Greybox Metal",new Color(.48f,.48f,.48f));
            rust=Mat("Greybox Machinery",new Color(.40f,.40f,.40f));
            wood=Mat("Greybox Crates",new Color(.60f,.60f,.60f));
            strawM=Mat("Greybox Straw",new Color(.67f,.67f,.67f));
            paddy=Mat("Greybox Paddy",new Color(.44f,.44f,.44f));
            plantM=Mat("Greybox Plant",new Color(.58f,.58f,.58f));
            mudM=Mat("Greybox Mud",new Color(.37f,.37f,.37f));
            machineM=Mat("Greybox Machine",new Color(.34f,.34f,.34f));
            steelBeam=Mat("Greybox Beam",new Color(.46f,.46f,.46f));
        }
        var powerSet=EnsurePowerData();
        world=new GameObject("Oops Runtime").AddComponent<BlobWorld>();
        world.levelTitle="OOPS! PLANET / V2 GREYBOX"; world.checkpointCount=2; world.saveSlot="oops_save_v2";
        world.spawn=new Vector3(-14,.1f,-14);
        world.overviewPosition=new Vector3(26,90,-46); world.overviewTarget=new Vector3(26,0,18);
        world.objective="HUB — จุดยานตก";
        world.gameObject.AddComponent<SteamSystem>().world=world;
        var shipGO=new GameObject("BLOB ship (repair)"); shipGO.transform.SetParent(root); shipGO.transform.position=new Vector3(8,0,6);
        ship=shipGO.AddComponent<ShipRepair>(); ship.world=world; world.ship=ship;
        BuildHub(); var factory=BuildFactory(); BuildRice(factory);
        ship.parts=parts.ToArray();
        world.puzzles=puzzles.ToArray(); world.sources=sources.ToArray(); world.zones=zones.ToArray();
        Ambience();
        BuildPlayer(powerSet);
        if(softWorld) OopsSoftWorldArt.Apply();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("OOPS_V2_BUILD_OK "+ScenePath+" | puzzles="+puzzles.Count+" sources="+sources.Count+" parts="+parts.Count);
        Validate();
    }
    // ---------- HUB: crash site ----------
    static PuzzleObjective h1;
    static void BuildHub() {
        Floor("Hub floor",Vector3.zero,40,40,grey);
        Wall("Hub wall S",new Vector2(-20,-20),new Vector2(20,-20),WallH,dark);
        Wall("Hub wall W",new Vector2(-20,-20),new Vector2(-20,20),WallH,dark);
        Wall("Hub wall N",new Vector2(-20,20),new Vector2(20,20),WallH,dark,.5f,0,Opening(0,6),Opening(14,6,4.6f));
        Wall("Hub wall E",new Vector2(20,-20),new Vector2(20,20),WallH,dark,.5f,0,Opening(-15,6),Opening(0,6),Opening(15,6,4.6f));
        var hubZone=Zone("Area Hub",BlobInteraction.Area,new Vector3(0,3,0),new Vector3(40,8,40),"HUB — จุดยานตก: ออกจากหลุมยาน แล้วเก็บชิ้นส่วนจากทุกโซนมาซ่อมยาน (ติดตั้ง 4 ชิ้น)");
        Label("OOPS! PLANET\nจุดยานตก CRASH SITE",new Vector3(0,3.4f,19.6f),0,.3f);
        Label("โรงงาน FACTORY ^",new Vector3(0,5.4f,19.6f),0,.22f);
        Label("นาข้าว RICE FIELDS >",new Vector3(19.6f,5.4f,0),90,.22f);
        // --- Crash pit (south-west corner, 3.2 m walls). H1: the way out is blocked by junk.
        var pitWall=Mat("V2 pit wall",new Color(.35f,.3f,.27f));
        Wall("Crash pit wall E",new Vector2(-8,-20),new Vector2(-8,-8),3.2f,pitWall,.6f);
        Wall("Crash pit wall N",new Vector2(-20,-8),new Vector2(-8,-8),3.2f,pitWall,.6f,0,Opening(-14,4,3.2f));
        Label("หลุมยานตก\nหาทางออก!",new Vector3(-14,2.4f,-8.4f),0,.16f);
        SourceSet("Pit",BlobProperty.Magnet,new Vector3(-18,0,-11));
        SourceSet("Pit",BlobProperty.Pressure,new Vector3(-10,0,-18));
        SourceSet("Pit",BlobProperty.Heat,new Vector3(-18,0,-18));
        SourceSet("Pit",BlobProperty.Bounce,new Vector3(-10.5f,0,-11));
        // Junk: cardboard bundles tied with scrap wire - light (Pressure), metal-tied (Magnet), flammable (Heat).
        var junk=new List<PuzzleItem>(); var junkMats=new List<WorldMaterial>();
        var card=Mat("V2 cardboard",new Color(.62f,.48f,.3f));
        var junkPos=new[]{new Vector3(-15.1f,.45f,-8),new Vector3(-13.9f,.45f,-8),new Vector3(-14.5f,1.35f,-8)};
        foreach(var jp in junkPos) {
            var j=Box("Junk bundle (cardboard + scrap wire, 1.5 kg)",jp,new Vector3(1.1f,.9f,1f),card);
            Body(j,1.5f); j.AddComponent<BlobMagnetic>();
            junkMats.Add(WM(j,MaterialKind.Straw));
            var it=j.AddComponent<PuzzleItem>(); it.key="junk"; it.useFallback=true; it.fallbackPosition=new Vector3(-12,.5f,-12); junk.Add(it);
        }
        var clear=Cond<ClearZoneCondition>("Condition: junk cleared",new Vector3(-14,1.3f,-8)); clear.key="junk"; clear.size=new Vector3(4,2.6f,1.8f); clear.initialCount=3;
        var outN=Reach("Condition: out of the pit (north)",new Vector3(0,1.5f,6.5f),new Vector3(40,3,27));
        var outE=Reach("Condition: out of the pit (east)",new Vector3(6.5f,1.5f,-13.5f),new Vector3(27,3,13));
        h1=Puzzle("Puzzle H1 Crash Pit Exit","h1_pit_exit","ทางออกจากหลุมยานตก","กองขยะขวางทางออก: ลากด้วยแม่เหล็ก เป่าด้วยแรงดัน เผาด้วยความร้อน หรือเด้งข้ามกำแพง","ออกมาได้แล้ว! ไปเก็บชิ้นส่วนยาน",
            new Bounds(new Vector3(-14,2,-14),new Vector3(12,6,12)),null,new MonoBehaviour[]{clear,outN,outE},junk.ToArray(),junkMats.ToArray());
        var partH1=Part("part_h1","แผงหน้าปัดยาน","hub",new Vector3(-14,.7f,-5),false);
        React(h1,new[]{partH1.gameObject});
        ResetPad("Reset pad: H1",new Vector3(-17.5f,0,-14),h1);
        // --- Ship (greybox) with 4 repair stages; the ship rises when repaired.
        var hull=new GameObject("Ship hull"); hull.transform.SetParent(ship.transform); hull.transform.localPosition=Vector3.zero;
        var shipM=Mat("V2 ship hull",new Color(.75f,.78f,.85f)); var shipDark=Mat("V2 ship scorched",new Color(.25f,.25f,.28f));
        Box("Ship body",new Vector3(8,1.6f,6),new Vector3(4.5f,2.6f,6.5f),shipM,hull.transform);
        Box("Ship nose",new Vector3(8,1.3f,10),new Vector3(3,2,2),shipM,hull.transform);
        Box("Ship scorch",new Vector3(8,2.95f,6),new Vector3(4.6f,.1f,6.6f),shipDark,hull.transform,false);
        var stages=new List<GameObject>();
        var stageM=Mat("V2 ship repaired",new Color(.3f,.85f,1f),true);
        foreach(var sp in new[]{new Vector3(5.6f,2,6),new Vector3(10.4f,2,6),new Vector3(8,3.1f,4),new Vector3(8,3.1f,8)})
            stages.Add(Box("Ship repair stage",sp,new Vector3(sp.y>3?3:.2f,sp.y>3?.2f:1.6f,sp.y>3?1.5f:4),stageM,hull.transform,false));
        foreach(var g in stages) g.SetActive(false);
        ship.stageObjects=stages.ToArray(); ship.liftOff=hull.transform; ship.installArea=new Bounds(new Vector3(8,1.5f,6),new Vector3(10,4,11));
        Label("ยานของ BLOB\nนำชิ้นส่วนมาติดตั้งที่นี่",new Vector3(8,4.3f,1.6f),0,.16f);
        SourceSet("Hub",BlobProperty.Electric,new Vector3(3.5f,0,2));
        SourceSet("Hub",BlobProperty.Water,new Vector3(-4,0,12));
        // --- Hidden: 4 electric floor pads in the junk yard, all at once (Bounce + Electric landing shock).
        var pads=new List<ElectricSwitch>(); var padLamp=Mat("V2 pad",new Color(.25f,.25f,.28f));
        int pi=0; foreach(var pp in new[]{new Vector3(10.8f,0,-10.8f),new Vector3(13.2f,0,-10.8f),new Vector3(10.8f,0,-13.2f),new Vector3(13.2f,0,-13.2f)}) {
            var pad=Block("Electric floor pad",pp,new Vector3(1,.12f,1),padLamp); var sw=pad.AddComponent<ElectricSwitch>(); sw.order=pi++; sw.lamp=pad.GetComponent<Renderer>(); pads.Add(sw);
        }
        Label("แผ่นไฟฟ้า 4 แผ่น",new Vector3(12,1.6f,-9.2f),180,.12f);
        var padGroup=Cond<SwitchGroup>("Condition: 4 pads together",new Vector3(12,0,-12)); padGroup.mode=SwitchGroup.Mode.Together; padGroup.switches=pads.ToArray(); padGroup.window=1f;
        var padsPz=Puzzle("Secret Hub Pads","s_hub_pads","แผ่นไฟฟ้าในลานขยะ","","",new Bounds(new Vector3(12,1,-12),new Vector3(6,3,6)),null,new MonoBehaviour[]{padGroup},secret:true);
        var partPads=Part("part_hub_pads","ขดลวดเครื่องยนต์","hub",new Vector3(16,.7f,-12),false); React(padsPz,new[]{partPads.gameObject});
        // --- Hidden shortcut: 3 street lights at once (Pressure + Electric lightning wind) open the side door to the Factory.
        var lamps=new List<WorldMaterial>();
        foreach(var lp in new[]{new Vector3(10,0,16.5f),new Vector3(14,0,17.5f),new Vector3(18,0,16.5f)}) {
            Block("Street light post",lp,new Vector3(.25f,3,.25f),metal);
            lamps.Add(WM(Block("Street light (machine)",lp+Vector3.up*3,new Vector3(.6f,.5f,.6f),machineM),MaterialKind.Machine,true));
        }
        var lightsPz=Puzzle("Secret Hub Street Lights","s_hub_lights","ไฟถนน 3 ดวง","","ทางลัดไปโรงงานเปิดแล้ว",new Bounds(new Vector3(14,2,16),new Vector3(12,6,8)),null,
            new MonoBehaviour[]{State("lights-all",MaterialGoal.Powered,lamps.ToArray())},secret:true);
        React(lightsPz);
        Gate("shortcut_lights","ทางลัดไฟถนน",new Vector3(14,2.3f,20),new Vector3(6,4.6f,.4f),new[]{lightsPz});
        Floor("Lights shortcut path",new Vector3(14,0,25),6,10,grey);
        Wall("Lights shortcut wall W",new Vector2(11,20),new Vector2(11,30),WallH,dark); Wall("Lights shortcut wall E",new Vector2(17,20),new Vector2(17,30),WallH,dark);
        // --- Paths out of the Hub
        Floor("Path to Factory",new Vector3(0,0,25),6,10,grey);
        Wall("Factory path wall W",new Vector2(-3,20),new Vector2(-3,30),WallH,dark); Wall("Factory path wall E",new Vector2(3,20),new Vector2(3,30),WallH,dark);
        Floor("Path to Rice",new Vector3(26,0,0),12,6,grey);
        Wall("Rice path wall N",new Vector2(20,3),new Vector2(32,3),WallH,dark); Wall("Rice path wall S",new Vector2(20,-3),new Vector2(32,-3),WallH,dark);
        hubAreaZone=hubZone;
    }
    static BlobInteractable hubAreaZone;
    // ---------- FACTORY ----------
    struct FactoryRefs { public PuzzleObjective f1,f2,f3; }
    static FactoryRefs BuildFactory() {
        var fc=new Vector3(0,0,48);
        Floor("Factory floor",fc,40,36,Mat("Factory floor",new Color(.38f,.36f,.34f)));
        Wall("Factory wall S",new Vector2(-20,30),new Vector2(20,30),WallH,dark,.5f,0,Opening(0,6),Opening(14,6));
        Wall("Factory wall N",new Vector2(-20,66),new Vector2(20,66),WallH,dark);
        Wall("Factory wall W",new Vector2(-20,30),new Vector2(-20,66),WallH,dark);
        Wall("Factory wall E",new Vector2(20,30),new Vector2(20,66),WallH,dark,.5f,0,Opening(55,4,4.4f));
        Zone("Area Factory",BlobInteraction.Area,fc+Vector3.up*3,new Vector3(40,8,36),"ZONE 01 FACTORY — โรงงาน: เปิดประตู (F1) แล้วฟื้นฟูห้องหม้อไอน้ำ (F2) หรือห้องกังหัน (F3)");
        Checkpoint(1,new Vector3(0,0,32),false);
        // Divider with the gate (x=0, 4 m) and the side hatch (x=-8, 1.6 m).
        Wall("Factory divider",new Vector2(-20,44),new Vector2(20,44),WallH,dark,.5f,0,Opening(0,4,4.4f),Opening(-8,1.6f,2.2f));
        var gateDoor=Box("Factory gate door",new Vector3(0,2.2f,44),new Vector3(4,4.4f,.4f),hazard);
        Label("F1 ประตูโรงงาน",new Vector3(0,5,43.6f),0,.2f);
        // F1 ① motor
        var motor=WM(Block("F1 gate motor (machine)",new Vector3(3.2f,0,43),new Vector3(1.2f,1.4f,1),machineM),MaterialKind.Machine,true);
        // F1 ② weight plate: 6 kg counterweight, or the 30 kg old machine with Electric + Magnet
        var plateGO=Block("F1 weight plate",new Vector3(-4,0,39.5f),new Vector3(2,.1f,2),hazard,null,false);
        var plate=plateGO.AddComponent<WeightPlate>(); plate.size=new Vector3(2,1.2f,2); plate.minMass=5f; plate.lamp=plateGO.GetComponent<Renderer>();
        var weight=Box("Counterweight (6 kg, magnetic)",new Vector3(-9,.45f,34),new Vector3(.9f,.9f,.9f),Mat("Magnet red",BlobPlayer.PropertyColor(BlobProperty.Magnet)));
        Body(weight,6); weight.AddComponent<BlobMagnetic>(); var wItem=weight.AddComponent<PuzzleItem>(); wItem.key="weight";
        var oldMachine=Box("Old machine (30 kg, magnetic)",new Vector3(5,.75f,37),new Vector3(1.4f,1.5f,1.2f),rust);
        Body(oldMachine,30); oldMachine.AddComponent<BlobMagnetic>(); var mItem=oldMachine.AddComponent<PuzzleItem>(); mItem.key="weight";
        // F1 ③⑤ side hatch: wax-sealed rusty latch; melt it (Heat) then blow (Pressure), or knock it off with a Magnet Cannon shot.
        var hingeGO=new GameObject("F1 hatch hinge"); hingeGO.transform.SetParent(root); hingeGO.transform.position=new Vector3(-8.8f,0,44);
        Box("F1 hatch door",new Vector3(-8,1.1f,44),new Vector3(1.6f,2.2f,.25f),wood,hingeGO.transform);
        var latchGO=Box("F1 hatch latch (wax-sealed rusty bolt)",new Vector3(-7.4f,1.1f,43.75f),new Vector3(.35f,.35f,.25f),waxM);
        var latch=WM(latchGO,MaterialKind.Wax,false); latchGO.AddComponent<ImpactBreak>();
        var hatch=hingeGO.AddComponent<LatchedDoor>(); hatch.latch=latch; hatch.hinge=hingeGO.transform; latchGO.GetComponent<ImpactBreak>().opens=hatch;
        // F1 ④ remote socket (portable battery: Magnet + Electric)
        var socket=WM(Block("F1 remote power socket (machine)",new Vector3(-17,0,41.5f),new Vector3(.8f,1,.8f),machineM),MaterialKind.Machine,true);
        Label("ช่องจ่ายไฟสำรอง",new Vector3(-17,2,41.9f),0,.12f);
        var f1=Puzzle("Puzzle F1 Factory Gate","f1_gate","ประตูโรงงาน","จ่ายไฟให้มอเตอร์ วางของหนักบนแผ่นกด ละลายสลักแล้วเป่าประตูเล็ก หรือจ่ายไฟที่ช่องสำรอง","ประตูเปิดแล้ว — ห้องหม้อไอน้ำอยู่ซ้าย ห้องกังหันอยู่ขวา",
            new Bounds(new Vector3(0,3,37),new Vector3(40,8,14)),null,new MonoBehaviour[]{State("motor",MaterialGoal.Powered,motor),plate,hatch,State("battery-socket",MaterialGoal.Powered,socket)},
            new[]{wItem,mItem},new[]{latch});
        var partF1=Part("part_f1","มอเตอร์ประตูยาน","factory",new Vector3(0,.7f,46),false);
        React(f1,new[]{partF1.gameObject},null,gateDoor.transform,Vector3.up*4.5f);
        ResetPad("Reset pad: F1",new Vector3(-12,0,32),f1);
        SourceSet("Factory yard",BlobProperty.Electric,new Vector3(6,0,32));
        SourceSet("Factory yard",BlobProperty.Magnet,new Vector3(-6,0,33));
        SourceSet("Factory yard",BlobProperty.Pressure,new Vector3(2.5f,0,36.5f));
        SourceSet("Factory yard",BlobProperty.Water,new Vector3(-2.5f,0,36.5f));
        SourceSet("Factory yard",BlobProperty.Heat,new Vector3(-11,0,40));
        SourceSet("Factory yard",BlobProperty.Bounce,new Vector3(17,0,32));
        // Hidden: shed with a wooden wall (Pressure + Magnet nail, then Magnet + Bounce slingshot) - part on the roof.
        var shed=Block("Storage shed (wooden walls)",new Vector3(-16.5f,0,33.5f),new Vector3(5,7,5),wood);
        WM(shed,MaterialKind.Wood,true);
        Part("part_shed_roof","เสาอากาศยาน","factory",new Vector3(-16.5f,7.6f,33.5f));
        // Hidden: cold room - ice floor over a pit (Bounce + Heat meteor landing melts it).
        var coldM=Mat("V2 cold room",new Color(.6f,.7f,.8f));
        Block("Cold room base W",new Vector3(10.5f,0,33),new Vector3(1,1.5f,4),coldM); Block("Cold room base E",new Vector3(13.5f,0,33),new Vector3(1,1.5f,4),coldM);
        Block("Cold room base S",new Vector3(12,0,31.5f),new Vector3(2,1.5f,1),coldM); Block("Cold room base N",new Vector3(12,0,34.5f),new Vector3(2,1.5f,1),coldM);
        Block("Cold room step",new Vector3(8.6f,0,33),new Vector3(1,.7f,1.2f),coldM);
        WM(Box("Cold room ice floor",new Vector3(12,1.35f,33),new Vector3(2,.3f,2),iceM),MaterialKind.Ice);
        Part("part_cold_room","ตัวทำความเย็นยาน","factory",new Vector3(12,.5f,33));
        Label("ห้องเย็น",new Vector3(12,2.6f,35.3f),180,.12f);
        // Hidden: storage room behind a corner; only a wind ball (Pressure + Bounce) reaches its switch.
        Wall("Storage room W",new Vector2(15,38),new Vector2(15,44),3,dark,.4f,0,Opening(41,2,3));
        Wall("Storage room S",new Vector2(15,38),new Vector2(20,38),3,dark,.4f);
        Box("Storage room roof",new Vector3(17.5f,3.1f,41),new Vector3(5,.2f,6),dark);
        var storageDoor=Box("Storage room door",new Vector3(15,1.5f,41),new Vector3(.3f,3,2),wood);
        Wall("Storage corner stub",new Vector2(13,36.2f),new Vector2(17,36.2f),2.5f,dark,.4f);
        var lockGO=Block("Storage switch (round the corner)",new Vector3(18.5f,1,37.2f),new Vector3(.6f,.6f,.3f),hazard);
        var storageLock=lockGO.AddComponent<ComboLock>(); storageLock.acceptedCombos=new[]{"pressure+bounce"}; storageLock.label="wind-ball-switch";
        var storagePz=Puzzle("Secret Storage Room","s_storage","ห้องเก็บของ","","",new Bounds(new Vector3(17.5f,1.5f,39),new Vector3(6,4,8)),null,new MonoBehaviour[]{storageLock},secret:true);
        React(storagePz,null,new[]{storageDoor});
        Part("part_storage","ถังเชื้อเพลิงยาน","factory",new Vector3(17.5f,.7f,41.5f));
        // ---- F2 boiler room (west back hall): reach the 6 m walkway.
        Wall("Hall partition",new Vector2(0,50),new Vector2(0,66),WallH,dark);
        Block("F2 walkway (6 m)",new Vector3(-17.5f,0,56.5f),new Vector3(5,6,19),steelBeam);
        var climb=Block("F2 metal wall (climbable)",new Vector3(-14.9f,0,59),new Vector3(.2f,6,4),metal); WM(climb,MaterialKind.Metal,true);
        var pool=WM(Block("F2 boiler water tank",new Vector3(-12.5f,0,56),new Vector3(3,.6f,3),waterM),MaterialKind.WaterBody);
        Block("F2 boiler",new Vector3(-12.5f,0,59.2f),new Vector3(2,2.5f,2),rust);
        Block("F2 pipe platform (3 m)",new Vector3(-11.5f,0,63.5f),new Vector3(3,3,3),metal);
        var liftMotor=WM(Block("F2 cargo lift motor (machine)",new Vector3(-13.5f,0,45.5f),new Vector3(1,1.2f,1),machineM),MaterialKind.Machine,true);
        var liftPlat=Box("F2 cargo lift platform",new Vector3(-17.5f,.15f,45.6f),new Vector3(2.5f,.3f,2.4f),hazard);
        var lift=liftPlat.AddComponent<PoweredLift>(); lift.motor=liftMotor; lift.platform=liftPlat.transform; lift.height=6; lift.world=world;
        Box("F2 steel beam (8 m)",new Vector3(-10,8,58),new Vector3(20,.4f,.4f),steelBeam);
        var anchorGO=new GameObject("F2 beam anchor"); anchorGO.transform.SetParent(root); anchorGO.transform.position=new Vector3(-17,7.6f,58); anchorGO.AddComponent<MagnetAnchor>();
        Label("F2 ห้องหม้อไอน้ำ\nขึ้นไปทางเดินชั้นบน",new Vector3(-7,3,50.3f),0,.18f);
        var walkway=Reach("Condition: on the walkway",new Vector3(-17.5f,7.3f,56.5f),new Vector3(5,2.6f,19));
        var f2=Puzzle("Puzzle F2 Boiler Room","f2_boiler","ห้องหม้อไอน้ำ","ขึ้นทางเดินสูง 6 ม.: ไอน้ำจากถังน้ำร้อน ไต่ผนังเหล็ก ลิฟต์ขนของ ดีดตัวจากคานเหล็ก หรือลมร้อน","ขึ้นมาได้แล้ว! เก็บชิ้นส่วนที่ปลายทางเดิน",
            new Bounds(new Vector3(-10,3,57),new Vector3(20,10,16)),f1,new MonoBehaviour[]{walkway},null,new[]{pool});
        var partF2=Part("part_f2","วาล์วหม้อไอน้ำยาน","factory",new Vector3(-17.5f,6.7f,64.5f),false);
        React(f2,new[]{partF2.gameObject});
        SourceSet("Boiler room",BlobProperty.Water,new Vector3(-5,0,60));
        SourceSet("Boiler room",BlobProperty.Heat,new Vector3(-5,0,52));
        SourceSet("Boiler room",BlobProperty.Electric,new Vector3(-9,0,49.5f));
        SourceSet("Boiler room",BlobProperty.Magnet,new Vector3(-4,0,64.5f));
        SourceSet("Boiler room",BlobProperty.Bounce,new Vector3(-7,0,64.5f));
        SourceSet("Boiler room",BlobProperty.Pressure,new Vector3(-2,0,56));
        // Hidden: fuse box only a short circuit (Electric + Heat) opens.
        var fuseDoor=Box("Fuse box door",new Vector3(-.6f,1.5f,62),new Vector3(.25f,1.2f,1),machineM);
        var fuseGO=Box("Fuse (short-circuit lock)",new Vector3(-.9f,1.5f,62),new Vector3(.2f,.3f,.3f),waxM);
        var fuse=fuseGO.AddComponent<ComboLock>(); fuse.acceptedCombos=new[]{"electric+heat"}; fuse.label="short-circuit";
        var fusePz=Puzzle("Secret Fuse Box","s_fuse","ตู้ฟิวส์","","",new Bounds(new Vector3(-1.5f,1.5f,62),new Vector3(4,4,4)),null,new MonoBehaviour[]{fuse},secret:true);
        var partFuse=Part("part_fuse","ฟิวส์พลังงานยาน","factory",new Vector3(-1.6f,.6f,62),false);
        React(fusePz,new[]{partFuse.gameObject},new[]{fuseDoor});
        // ---- F3 turbine room (east back hall)
        var turbineGO=new GameObject("F3 ventilation turbine"); turbineGO.transform.SetParent(root); turbineGO.transform.position=new Vector3(10,4,65.4f);
        Box("Turbine housing",turbineGO.transform.position,new Vector3(3.6f,3.6f,.4f),metal,turbineGO.transform);
        var rotor=new GameObject("Rotor"); rotor.transform.SetParent(turbineGO.transform); rotor.transform.position=turbineGO.transform.position+Vector3.back*.3f;
        for(int i=0;i<4;i++) { var blade=Box("Blade",rotor.transform.position,new Vector3(.5f,3,.1f),hazard,rotor.transform,false); blade.transform.localRotation=Quaternion.Euler(0,0,i*45); }
        var tcol=turbineGO.AddComponent<BoxCollider>(); tcol.size=new Vector3(3.6f,3.6f,.8f);
        var gauge=new Renderer[3]; for(int i=0;i<3;i++) gauge[i]=Prim(PrimitiveType.Sphere,"Turbine gauge lamp",new Vector3(13,1.2f+i*.6f,65.5f),Vector3.one*.35f,dark,null,false).GetComponent<Renderer>();
        var turbine=turbineGO.AddComponent<PressureTurbine>(); turbine.rotor=rotor.transform; turbine.gaugeLamps=gauge;
        var tMotor=WM(Block("F3 turbine motor (machine, high)",new Vector3(16,5,65.3f),new Vector3(1,1,.8f),machineM),MaterialKind.Machine,true);
        Block("F3 motor bracket",new Vector3(16,0,65.6f),new Vector3(.3f,5,.3f),metal);
        var feed=WM(Block("F3 steam feed tank",new Vector3(4,0,64),new Vector3(2.4f,.5f,2.4f),waterM),MaterialKind.WaterBody);
        Block("F3 intake pipe",new Vector3(4,2.5f,65.6f),new Vector3(1,3,.8f),rust);
        var inlet=Cond<SteamInlet>("Condition: steam intake",new Vector3(4,1.5f,64)); inlet.size=new Vector3(3,4,3); inlet.needed=2;
        var sw=new List<ElectricSwitch>(); int so=1;
        foreach(var z in new[]{59.5f,62f,64.5f}) { var s=Block("F3 sequence switch "+so,new Vector3(19.6f,1.6f,z),new Vector3(.3f,.8f,.8f),Mat("V2 pad",new Color(.25f,.25f,.28f))); var es=s.AddComponent<ElectricSwitch>(); es.order=so++; es.lamp=s.GetComponent<Renderer>(); sw.Add(es); }
        Label("สวิตช์ 1 → 2 → 3",new Vector3(19.4f,3.2f,62),90,.14f);
        var seq=Cond<SwitchGroup>("Condition: switches in order",new Vector3(19,1.6f,62)); seq.mode=SwitchGroup.Mode.Sequence; seq.switches=sw.ToArray(); seq.window=3f;
        Label("F3 ห้องกังหัน",new Vector3(10,3,50.3f),0,.2f);
        var f3=Puzzle("Puzzle F3 Ventilation Turbine","f3_turbine","กังหันระบายอากาศ","หมุนกังหันให้เต็ม: เป่าแรงดัน จ่ายไฟให้มอเตอร์ ส่งไอน้ำเข้าท่อ หรือเปิดสวิตช์ 1-2-3 ตามลำดับ","อากาศถ่ายเทแล้ว!",
            new Bounds(new Vector3(10,3,57.5f),new Vector3(20,10,17)),f1,new MonoBehaviour[]{turbine,State("turbine-motor",MaterialGoal.Powered,tMotor),inlet,seq},null,new[]{feed});
        turbine.objective=f3;
        var partF3=Part("part_f3","ใบพัดยาน","factory",new Vector3(10,.7f,60),false);
        React(f3,new[]{partF3.gameObject});
        SourceSet("Turbine room",BlobProperty.Pressure,new Vector3(7,0,52));
        SourceSet("Turbine room",BlobProperty.Electric,new Vector3(13,0,52));
        SourceSet("Turbine room",BlobProperty.Heat,new Vector3(1.8f,0,60));
        SourceSet("Turbine room",BlobProperty.Water,new Vector3(6.5f,0,61));
        SourceSet("Turbine room",BlobProperty.Bounce,new Vector3(17,0,59));
        // Hidden: ice-wrapped part on the 8 m steel beam (Heat + Magnet induction heats the beam from below).
        var beamGO=Box("F3 steel beam (8 m)",new Vector3(10,8,57),new Vector3(16,.4f,.4f),steelBeam); WM(beamGO,MaterialKind.Metal,true);
        var beamIce=WM(Box("Ice block on the beam",new Vector3(10,8.75f,57),new Vector3(1.1f,1.1f,1.1f),iceM),MaterialKind.Ice);
        var icePz=Puzzle("Secret Beam Ice","s_beam_ice","น้ำแข็งบนคานเหล็ก","","",new Bounds(new Vector3(10,4,57),new Vector3(18,10,6)),null,new MonoBehaviour[]{State("ice-melted",MaterialGoal.Melted,beamIce)},secret:true);
        var partIce=Part("part_beam_ice","คริสตัลนำทางยาน","factory",new Vector3(10,.6f,56),false);
        React(icePz,new[]{partIce.gameObject});
        // Conveyor shortcut back to the Hub: weld the broken rail (Heat + Electric) to open it.
        var railGO=Box("Broken conveyor rail (weld me)",new Vector3(18.6f,.3f,55),new Vector3(.6f,.6f,2.4f),rust);
        var weld=railGO.AddComponent<ComboLock>(); weld.acceptedCombos=new[]{"heat+electric"}; weld.label="weld";
        var weldPz=Puzzle("Secret Conveyor Weld","s_conveyor_weld","รางสายพานขาด","","ทางลัดสายพานกลับฮับเปิดแล้ว",new Bounds(new Vector3(18,1.5f,55),new Vector3(4,4,5)),null,new MonoBehaviour[]{weld},secret:true);
        React(weldPz,null,null,railGO.transform,Vector3.up*.01f,new[]{railGO.GetComponent<Renderer>()},new Color(.7f,.75f,.8f));
        Gate("shortcut_conveyor","ทางลัดสายพาน",new Vector3(20.3f,2.2f,55),new Vector3(.4f,4.4f,4),new[]{weldPz});
        var belt=Mat("V2 conveyor",new Color(.2f,.2f,.22f));
        Floor("Conveyor shortcut",new Vector3(23,0,35),6,46,belt);
        Wall("Conveyor wall E",new Vector2(26,12),new Vector2(26,58),WallH,dark); Wall("Conveyor wall N",new Vector2(20,58),new Vector2(26,58),WallH,dark);
        Wall("Conveyor wall S",new Vector2(20,12),new Vector2(26,12),WallH,dark); Wall("Conveyor wall W",new Vector2(20,20),new Vector2(20,30),WallH,dark);
        return new FactoryRefs{f1=f1,f2=f2,f3=f3};
    }
    // ---------- RICE FIELDS & CANAL ----------
    static void BuildRice(FactoryRefs f) {
        Floor("Rice west bank",new Vector3(42,0,0),20,44,paddy);
        Floor("Rice east bank",new Vector3(66,0,0),12,44,paddy);
        Wall("Rice wall W",new Vector2(32,-22),new Vector2(32,22),WallH,dark,.5f,0,Opening(-15,6),Opening(0,6));
        Wall("Rice wall N",new Vector2(32,22),new Vector2(72,22),WallH+12,dark,.5f,-12);
        Wall("Rice wall S",new Vector2(32,-22),new Vector2(72,-22),WallH+12,dark,.5f,-12);
        Wall("Rice wall E",new Vector2(72,-22),new Vector2(72,22),WallH,dark);
        Zone("Area Rice",BlobInteraction.Area,new Vector3(52,3,0),new Vector3(40,8,44),"ZONE 02 RICE FIELDS — นาข้าวและคลอง: เคลียร์ประตูน้ำ (R1) ข้ามคลอง (R2) และเดินปั๊มชลประทาน (R3)");
        Checkpoint(2,new Vector3(35,0,0),true);
        // Zone gate: Factory power restored = F1 + one of F2 / F3 (design §6).
        Gate("rice","ทางไปนาข้าว",new Vector3(26,2.2f,0),new Vector3(.4f,4.4f,6),new[]{f.f1},new[]{f.f2,f.f3},1);
        Box("Rice gate transom",new Vector3(26,6.2f,0),new Vector3(.4f,3.6f,6),dark);
        Label("นาข้าว: ฟื้นฟูไฟในโรงงานก่อน\n(F1 + F2 หรือ F3)",new Vector3(25.6f,3.2f,0),90,.14f);
        // Canal (8 m): falling in = back to the checkpoint. The water is a trigger volume (conducts, steams).
        WM(Box("Canal water",new Vector3(56,-.65f,0),new Vector3(8,.7f,44),waterM),MaterialKind.WaterBody).GetComponent<Collider>().isTrigger=true;
        var skim=Box("Canal surface (Bounce + Water skim)",new Vector3(56,-.4f,0),new Vector3(8,.2f,44),waterM,null,true);
        skim.GetComponent<Renderer>().enabled=false; skim.AddComponent<WaterSkim>().world=world;
        // ---- R1 straw-blocked sluice on the 3 m platform; burning is allowed but the drying rack must survive.
        Block("R1 sluice platform (3 m)",new Vector3(42,0,16),new Vector3(9,3,5),dark);
        Wall("R1 sluice back wall",new Vector2(34,19.2f),new Vector2(50,19.2f),5,dark,.6f);
        var straws=new List<PuzzleItem>(); var strawMats=new List<WorldMaterial>();
        float[] sx={39.5f,41.2f,42.9f,44.6f};
        for(int i=0;i<sx.Length;i++) {
            var s=Block("Straw bundle (2 kg)",new Vector3(sx[i],3,16),new Vector3(1.2f,.8f,1.2f),strawM);
            Body(s,2); strawMats.Add(WM(s,MaterialKind.Straw));
            var it=s.AddComponent<PuzzleItem>(); it.key="straw"; it.useFallback=true; it.fallbackPosition=new Vector3(36,.5f,9+i*1.2f); straws.Add(it);
        }
        var rack=WM(Block("Rice drying rack (burns!)",new Vector3(46.05f,3,16),new Vector3(.6f,1.4f,2),Mat("V2 rice dry",new Color(.75f,.7f,.35f))),MaterialKind.Plant);
        var strawZone=Cond<ClearZoneCondition>("Condition: straw cleared",new Vector3(42,3.6f,16)); strawZone.key="straw"; strawZone.size=new Vector3(9,1.4f,5); strawZone.initialCount=4;
        Label("R1 ประตูน้ำ\nเคลียร์ฟาง (ระวังไฟลามไปราวตากข้าว)",new Vector3(42,1.8f,13.4f),0,.16f);
        var r1=Puzzle("Puzzle R1 Sluice","r1_sluice","ฟางอุดประตูน้ำ","เคลียร์ฟาง 4 มัด: เป่า ฉีดน้ำแรงสูง หรือเผา (แต่ต้องดับไฟก่อนลามไปราวตากข้าว)","น้ำไหลเข้านาแล้ว",
            new Bounds(new Vector3(42,3,12),new Vector3(20,8,14)),null,new MonoBehaviour[]{strawZone,State("rack-intact",MaterialGoal.Intact,rack)},straws.ToArray(),strawMats.Concat(rack),ConditionMode.All);
        var channel=Box("Irrigation channel (flowing)",new Vector3(42,.03f,20.5f),new Vector3(18,.03f,1.6f),Mat("Flowing water",new Color(.2f,.65f,.95f),true),null,false); channel.SetActive(false);
        var partR1=Part("part_r1","ปั๊มน้ำยาน","rice",new Vector3(42,.7f,11),false);
        React(r1,new[]{channel,partR1.gameObject});
        ResetPad("Reset pad: R1",new Vector3(36,0,6.5f),r1);
        SourceSet("Rice west",BlobProperty.Pressure,new Vector3(37,0,10));
        SourceSet("Rice west",BlobProperty.Heat,new Vector3(35,0,16));
        SourceSet("Rice west",BlobProperty.Water,new Vector3(40,0,8.5f));
        SourceSet("Rice west",BlobProperty.Bounce,new Vector3(48,0,9));
        // Reuse the existing scrap source as the movable hot-metal tool (no new model).
        SourceSet("Rice west",BlobProperty.Magnet,new Vector3(39.5f,3,14.5f));
        var scrap=sources[sources.Count-1].gameObject;
        Body(scrap,3); scrap.AddComponent<BlobMagnetic>(); WM(scrap,MaterialKind.Metal,true);
        scrap.AddComponent<PuzzleItem>().key="hot-metal";
        SourceSet("Rice west",BlobProperty.Electric,new Vector3(48,0,-4));
        // ---- R2 cross the canal
        var seed=WM(Block("R2 vine seed",new Vector3(51.4f,0,7.3f),new Vector3(.5f,.4f,.5f),plantM),MaterialKind.Plant,true);
        var bridge=Box("R2 vine bridge",new Vector3(56,-.1f,6),new Vector3(8.6f,.2f,2),Mat("V2 vine",new Color(.25f,.7f,.25f)));
        bridge.SetActive(false); var vb=seed.gameObject.AddComponent<VineBridge>(); vb.seed=seed; vb.bridge=bridge;
        var pole=Block("R2 iron lamp post",new Vector3(61.5f,0,-6),new Vector3(.3f,5,.3f),metal); WM(pole,MaterialKind.Metal,true);
        var poleAnchor=new GameObject("R2 lamp post anchor"); poleAnchor.transform.SetParent(root); poleAnchor.transform.position=new Vector3(61.5f,5.2f,-6); poleAnchor.AddComponent<MagnetAnchor>();
        var raftGO=Box("R2 iron raft (25 kg)",new Vector3(58.7f,-.2f,-12),new Vector3(2.4f,.4f,2.4f),rust);
        var raftRb=raftGO.AddComponent<Rigidbody>(); raftRb.mass=25; raftGO.AddComponent<BlobMagnetic>();
        var raft=raftGO.AddComponent<FerryRaft>(); raft.world=world; raft.nearDock=new Vector3(53.3f,-.2f,-12); raft.farDock=new Vector3(58.7f,-.2f,-12);
        Label("R2 ข้ามคลอง (8 ม.)",new Vector3(51.5f,2.2f,0),90,.18f);
        var r2=Puzzle("Puzzle R2 Canal","r2_canal","ข้ามคลอง","ไปอีกฝั่งคลอง: รดเมล็ดให้เถาวัลย์เป็นสะพาน พุ่งกลางอากาศ ดีดตัวจากเสาไฟเหล็ก ลากแพเหล็ก หรือเด้งบนผิวน้ำ","ข้ามมาได้แล้ว!",
            new Bounds(new Vector3(56,2,0),new Vector3(24,6,44)),null,new MonoBehaviour[]{Reach("Condition: east bank",new Vector3(66,1.5f,0),new Vector3(12,3,44))},null,new[]{seed});
        var partR2=Part("part_r2","ทุ่นลอยยาน","rice",new Vector3(66,.7f,-2),false);
        React(r2,new[]{partR2.gameObject});
        // ---- R3 irrigation pump (east bank)
        var pump=WM(Block("R3 irrigation pump (machine)",new Vector3(64,0,18),new Vector3(1.4f,1.6f,1.4f),machineM),MaterialKind.Machine,true);
        var millGO=new GameObject("R3 windmill"); millGO.transform.SetParent(root); millGO.transform.position=new Vector3(69.5f,4,19);
        Block("Windmill post",new Vector3(69.5f,0,19.6f),new Vector3(.4f,4,.4f),wood);
        var millRotor=new GameObject("Rotor"); millRotor.transform.SetParent(millGO.transform); millRotor.transform.position=millGO.transform.position;
        for(int i=0;i<4;i++) { var bl=Box("Sail",millGO.transform.position,new Vector3(.4f,2.6f,.08f),Mat("V2 sail",new Color(.85f,.82f,.75f)),millRotor.transform,false); bl.transform.localRotation=Quaternion.Euler(0,0,i*45); }
        var mcol=millGO.AddComponent<BoxCollider>(); mcol.size=new Vector3(2.8f,2.8f,.5f);
        var millLamps=new Renderer[3]; for(int i=0;i<3;i++) millLamps[i]=Prim(PrimitiveType.Sphere,"Windmill lamp",new Vector3(67.5f,.8f+i*.5f,19.6f),Vector3.one*.3f,dark,null,false).GetComponent<Renderer>();
        var mill=millGO.AddComponent<PressureTurbine>(); mill.rotor=millRotor.transform; mill.gaugeLamps=millLamps;
        var pond=WM(Box("R3 pond (submerged terminal)",new Vector3(64,.2f,4),new Vector3(4,.4f,4),waterM),MaterialKind.WaterBody); pond.GetComponent<Collider>().isTrigger=true;
        var terminal=WM(Block("R3 submerged pump terminal (machine)",new Vector3(64,0,4),new Vector3(.6f,.35f,.6f),machineM),MaterialKind.Machine,true);
        var feedR=WM(Block("R3 steam pump feed tank",new Vector3(70,0,7.5f),new Vector3(2,.6f,2),waterM),MaterialKind.WaterBody);
        var inletR=Cond<SteamInlet>("Condition: steam pump intake",new Vector3(70,1.5f,7.5f)); inletR.size=new Vector3(3,4,3); inletR.needed=2;
        var plots=new List<WorldMaterial>();
        foreach(var px in new[]{62f,66f,70f}) foreach(var pz in new[]{-10f,-15.5f})
            plots.Add(WM(Block("R3 rice plot",new Vector3(px,0,pz),new Vector3(3.4f,.15f,4.6f),plantM),MaterialKind.Plant,true));
        Label("R3 ปั๊มชลประทาน",new Vector3(64,3,16.6f),0,.16f);
        var r3=Puzzle("Puzzle R3 Irrigation Pump","r3_pump","ปั๊มชลประทาน","เติมน้ำให้นา: จ่ายไฟให้ปั๊ม หมุนกังหันลม ช็อตผ่านบ่อถึงขั้วใต้น้ำ เดินปั๊มไอน้ำ หรือรดทั้ง 6 แปลงเอง","นาทั้ง 6 แปลงได้น้ำแล้ว ข้าวกลับมาเขียว",
            new Bounds(new Vector3(66,3,2),new Vector3(12,8,44)),null,
            new MonoBehaviour[]{State("pump-power",MaterialGoal.Powered,pump),mill,State("submerged-terminal",MaterialGoal.Powered,terminal),inletR,State("fields-watered",MaterialGoal.Watered,plots.ToArray())},null,plots.ToArray().Concat(feedR));
        mill.objective=r3;
        var partR3=Part("part_r3","ระบบหล่อเย็นยาน","rice",new Vector3(66,.7f,8),false);
        React(r3,new[]{partR3.gameObject});
        SourceSet("Rice east",BlobProperty.Electric,new Vector3(70,0,2));
        SourceSet("Rice east",BlobProperty.Water,new Vector3(62,0,-3));
        SourceSet("Rice east",BlobProperty.Heat,new Vector3(70,0,-3));
        SourceSet("Rice east",BlobProperty.Pressure,new Vector3(61.5f,0,15));
        SourceSet("Rice east",BlobProperty.Bounce,new Vector3(66,0,-20.5f));
        // Hidden: pump-house roof seed behind a parapet (Water + Bounce water ball) grows a vine stair.
        Block("Pump house",new Vector3(69,0,13),new Vector3(4,5,4),wood);
        foreach(var pp in new[]{new Vector4(69,15,4,.2f),new Vector4(69,11,4,.2f),new Vector4(71,13,.2f,4),new Vector4(67,13,.2f,4)}) Block("Pump house parapet",new Vector3(pp.x,5,pp.y),new Vector3(pp.z,.9f,pp.w),wood);
        var roofSeed=WM(Block("Roof vine seed",new Vector3(69,5,13),new Vector3(.5f,.4f,.5f),plantM),MaterialKind.Plant,true);
        var stair=new GameObject("Roof vine stair"); stair.transform.SetParent(root);
        for(int i=0;i<6;i++) {
            var step=Block("Vine step",new Vector3(66.55f-(5-i)*.9f,0,13),new Vector3(.9f,.85f*(i+1),1.6f),Mat("V2 vine",new Color(.25f,.7f,.25f)),stair.transform);
            step.GetComponent<Collider>().enabled=false;
        }
        // Smooth collision under the existing stair visuals; each visual riser was 0.85 m (too tall to walk).
        var ramp=new GameObject("Vine stair walkable collision"); ramp.transform.SetParent(stair.transform);
        ramp.transform.position=new Vector3(63.6f,3.3f,13); ramp.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(6.6f,7.2f)*Mathf.Rad2Deg);
        ramp.AddComponent<BoxCollider>().size=new Vector3(Mathf.Sqrt(7.2f*7.2f+6.6f*6.6f),.16f,1.6f);
        stair.SetActive(false); var rv=roofSeed.gameObject.AddComponent<VineBridge>(); rv.seed=roofSeed; rv.bridge=stair;
        Part("part_pump_roof","เสาส่งสัญญาณยาน","rice",new Vector3(69.8f,5.7f,12.2f));
        // Hidden: charcoal kiln inside a closed yard - only a fireball (Heat + Bounce) lobbed over the wall lights it.
        Wall("Kiln yard N",new Vector2(42,-14),new Vector2(50,-14),3,dark,.5f);
        Wall("Kiln yard W",new Vector2(42,-22),new Vector2(42,-14),3,dark,.5f); Wall("Kiln yard E",new Vector2(50,-22),new Vector2(50,-14),3,dark,.5f);
        var kiln=WM(Block("Charcoal kiln (wood)",new Vector3(46,0,-19.5f),new Vector3(1.6f,1.6f,1.6f),wood),MaterialKind.Wood);
        var kilnPz=Puzzle("Secret Kiln","s_kiln","เตาเผาถ่านหลังกำแพง","","",new Bounds(new Vector3(46,2,-17),new Vector3(10,5,10)),null,new MonoBehaviour[]{State("kiln-lit",MaterialGoal.Ignited,kiln)},null,new[]{kiln},secret:true);
        var partKiln=Part("part_kiln","แกนความร้อนยาน","rice",new Vector3(46,.7f,-12.5f),false);
        React(kilnPz,new[]{partKiln.gameObject});
        // Dike shortcut back to the Hub: open after R1 or R2.
        Floor("Dike shortcut",new Vector3(26,0,-15),12,6,paddy);
        Wall("Dike wall N",new Vector2(20,-12),new Vector2(32,-12),WallH,dark); Wall("Dike wall S",new Vector2(20,-18),new Vector2(32,-18),WallH,dark);
        Gate("shortcut_dike","ทางลัดคันนา",new Vector3(31,2.2f,-15),new Vector3(.4f,4.4f,6),new PuzzleObjective[0],new[]{r1,r2},1);
        Box("Dike gate transom",new Vector3(31,6.2f,-15),new Vector3(.4f,3.6f,6),dark);
        // Zone gates list (order: zone, shortcuts)
        var gates=new List<ZoneGate>(Object.FindObjectsByType<ZoneGate>(FindObjectsSortMode.None));
        gates.Sort((a,b)=>a.zoneId=="rice"?-1:b.zoneId=="rice"?1:string.CompareOrdinal(a.zoneId,b.zoneId));
        world.zoneGates=gates.ToArray();
    }
    static WorldMaterial[] Concat(this IEnumerable<WorldMaterial> a,WorldMaterial b) { var l=new List<WorldMaterial>(a); l.Add(b); return l.ToArray(); }
    // ---------- ambience / player / lighting ----------
    static void Ambience() {
        var vfxGO=new GameObject("VFX kit"); vfxGO.transform.SetParent(root);
        var ps=vfxGO.AddComponent<ParticleSystem>();
        var main=ps.main; main.simulationSpace=ParticleSystemSimulationSpace.World; main.maxParticles=4000; main.startSpeed=0; main.startLifetime=.5f; main.gravityModifier=.35f; main.playOnAwake=true; main.loop=true;
        var em=ps.emission; em.rateOverTime=0; var sh=ps.shape; sh.enabled=false;
        var sol=ps.sizeOverLifetime; sol.enabled=true; sol.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
        var pr=vfxGO.GetComponent<ParticleSystemRenderer>(); pr.renderMode=ParticleSystemRenderMode.Mesh; pr.mesh=Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        pr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/VFX particle.mat"); pr.shadowCastingMode=ShadowCastingMode.Off;
        vfxGO.AddComponent<VfxKit>();
        var ambGO=new GameObject("Zone ambience"); ambGO.transform.SetParent(root);
        var amb=ambGO.AddComponent<ZoneAmbience>(); amb.world=world;
        PuzzleObjective P(string id) { foreach(var p in puzzles) if(p.id==id) return p; return null; }
        amb.zones=new[]{
            new ZoneAmbience.Zone{name="Hub",area=new Bounds(new Vector3(0,3,0),new Vector3(40,12,40)),normal=OopsPlanet.Ambience.Hub,restored=OopsPlanet.Ambience.HubRestored,restoredWhen=new[]{P("h1_pit_exit"),P("f1_gate"),P("r1_sluice")}},
            new ZoneAmbience.Zone{name="Factory",area=new Bounds(new Vector3(0,4,48),new Vector3(40,14,36)),normal=OopsPlanet.Ambience.Factory,restored=OopsPlanet.Ambience.FactoryRestored,restoredWhen=new[]{P("f1_gate"),P("f2_boiler"),P("f3_turbine")}},
            new ZoneAmbience.Zone{name="Rice",area=new Bounds(new Vector3(52,3,0),new Vector3(40,12,44)),normal=OopsPlanet.Ambience.Rice,restored=OopsPlanet.Ambience.RiceRestored,restoredWhen=new[]{P("r1_sluice"),P("r2_canal"),P("r3_pump")}},
        };
        world.ambience=amb; world.uiFont=uiFont;
    }
    static void BuildPlayer(PowerSet powerSet) {
        var white=Mat("Eye white",new Color(.95f,.98f,1)); var black=Mat("Pupils",new Color(.02f,.025f,.035f));
        var glow=Mat("BLOB core",new Color(1,.7f,.2f),true); var blobBody=Mat("BLOB body",new Color(.63f,.73f,.78f));
        var cameraGO=new GameObject("Main Camera"); cameraGO.tag="MainCamera"; cameraGO.AddComponent<AudioListener>();
        var cam=cameraGO.AddComponent<Camera>(); cam.fieldOfView=65; cam.farClipPlane=300; cam.nearClipPlane=.08f;
        cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.62f,.74f,.85f);
        var playerGO=new GameObject("BLOB Player"); playerGO.layer=2; playerGO.tag="Player";
        playerGO.AddComponent<CharacterController>();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/BLOB_Greybox.fbx");
        if(!model) throw new System.Exception("Blender BLOB export missing");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,playerGO.transform); visual.name="BLOB Blender Visual";
        visual.transform.localPosition=Vector3.zero; visual.transform.localRotation=Quaternion.identity;
        foreach(var r in visual.GetComponentsInChildren<Renderer>()) {
            string n=r.name;
            r.sharedMaterial=n.Contains("Pupil")||n.Contains("Mouth")?black:n.Contains("Eye")?white:n.Contains("Core")?glow:n.Contains("PowerBand")?Mat("BLOB power band",new Color(.8f,.85f,.9f)):n.Contains("Cue_")?Mat("BLOB power cue",new Color(.9f,.9f,.9f)):blobBody;
            r.gameObject.layer=2;
        }
        var beam=playerGO.AddComponent<LineRenderer>(); beam.sharedMaterial=Mat("Absorption stream",new Color(.3f,.85f,1),true);
        beam.positionCount=2; beam.startWidth=.055f; beam.endWidth=.02f; beam.enabled=false;
        playerGO.AddComponent<AudioSource>().spatialBlend=0;
        var player=playerGO.AddComponent<BlobPlayer>(); player.world=world; player.view=cam; player.visual=visual.transform; world.player=player;
        var powers=playerGO.AddComponent<BlobPowers>(); powers.set=powerSet; player.powers=powers;
        playerGO.AddComponent<BlobElementPowers>();
        Transform Find(string n) { foreach(var t in visual.GetComponentsInChildren<Transform>(true)) if(t.name==n) return t; throw new System.Exception("BLOB part missing: "+n); }
        var ex=playerGO.AddComponent<BlobExpression>(); ex.player=player;
        ex.eyes=new[]{Find("BLOB_Eye_L"),Find("BLOB_Eye_R")}; ex.pupils=new[]{Find("BLOB_Pupil_L"),Find("BLOB_Pupil_R")}; ex.mouth=Find("BLOB_Mouth");
        foreach(var t in visual.GetComponentsInChildren<Transform>(true)) if(t.name=="BLOB_Drip") ex.antenna=t;
        var sense=playerGO.AddComponent<BlobSense>();
        var sv=new GameObject("Sense FX volume").AddComponent<Volume>(); sv.isGlobal=true; sv.priority=1; sv.weight=0;
        sv.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Settings/OOPS_SenseFX.asset"); sense.senseVolume=sv;
        var fb=playerGO.AddComponent<BlobFeedback>(); fb.player=player; fb.powers=powers; fb.world=world; fb.expression=ex;
        AudioSource Src() { var a=playerGO.AddComponent<AudioSource>(); a.spatialBlend=0; a.playOnAwake=false; return a; }
        fb.voice=Src(); fb.sfx=Src(); fb.magnetLoop=Src();
        playerGO.transform.position=world.spawn;
        cam.transform.position=world.spawn+new Vector3(.65f,3.3f,-5.5f); cam.transform.LookAt(world.spawn+new Vector3(0,1.2f,2));
        var sun=new GameObject("Directional Light").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.5f; sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(50,-30,0);
        var fill=new GameObject("Soft fill").AddComponent<Light>(); fill.type=LightType.Directional; fill.intensity=.35f; fill.color=new Color(.6f,.75f,1); fill.transform.rotation=Quaternion.Euler(25,145,0);
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.62f,.7f,.85f); RenderSettings.ambientEquatorColor=new Color(.5f,.52f,.55f); RenderSettings.ambientGroundColor=new Color(.3f,.28f,.25f);
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=80; RenderSettings.fogEndDistance=240; RenderSettings.fogColor=new Color(.62f,.74f,.85f);
        var camData=cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing=true; camData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        var vol=new GameObject("Post FX volume").AddComponent<Volume>(); vol.isGlobal=true; vol.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Settings/OOPS_PostFX.asset");
    }
    // ---------- validation ----------
    [MenuItem("Tools/OOPS Planet/V2/Validate V2 Scene")]
    public static void Validate() {
        var errors=new List<string>();
        Physics.SyncTransforms();
        var w=Object.FindFirstObjectByType<BlobWorld>();
        if(!w) { Debug.LogError("OOPS_V2_VALIDATE_FAIL no BlobWorld"); return; }
        if(!w.player) errors.Add("no player");
        else {
            var el=w.player.GetComponent<BlobElementPowers>(); if(!el) errors.Add("BlobElementPowers missing");
            var set=w.player.powers?w.player.powers.set:null;
            if(!set || !set.table || set.table.cells.Count!=30) errors.Add("ComboTable (30 cells) not assigned");
            else foreach(var p in ComboTable.Powers) if(!set.Get(p)) errors.Add("PowerSet missing "+p);
            var bs=w.player.GetComponent<BlobSense>(); if(!bs || !bs.senseVolume || !bs.senseVolume.sharedProfile) errors.Add("sense volume not wired");
        }
        if(!w.ship || w.ship.parts==null || w.ship.parts.Length==0) errors.Add("ship / parts not wired");
        else {
            var ids=new HashSet<string>(); var zonesSeen=new HashSet<string>();
            foreach(var p in w.ship.parts) { if(!p || !ids.Add(p.id)) errors.Add("duplicate / null ship part "+(p?p.id:"")); else { zonesSeen.Add(p.zone); if(!p.ship) errors.Add("part without ship: "+p.id); } }
            foreach(var z in w.ship.requiredZones) if(!zonesSeen.Contains(z)) errors.Add("no ship part in zone "+z);
            if(w.ship.parts.Length<w.ship.partsToFinish) errors.Add("fewer parts than needed to finish");
        }
        int main=0;
        foreach(var pz in w.puzzles) {
            if(!pz) { errors.Add("null puzzle"); continue; }
            int conds=0; foreach(var c in pz.conditions) if(c is IPuzzleCondition) conds++;
            if(conds==0) errors.Add("puzzle without conditions: "+pz.id);
            if(pz.isSecret) continue;
            main++;
            if(string.IsNullOrEmpty(pz.titleTh)||string.IsNullOrEmpty(pz.hintTh)) errors.Add("puzzle missing Thai text: "+pz.id);
            if(!pz.GetComponent<PuzzleReaction>()) errors.Add("puzzle without reaction: "+pz.id);
            // Sandbox rule (§7.2): at least 4 of the 6 powers available within 15 m of the puzzle.
            var near=new HashSet<BlobProperty>();
            foreach(var s in w.sources) if(s && Vector3.Distance(Flat(s.transform.position),Flat(pz.area.ClosestPoint(s.transform.position)))<=15f) near.Add(s.property);
            if(near.Count<4) errors.Add("puzzle "+pz.id+" has only "+near.Count+" power kinds within 15 m");
        }
        if(main!=7) errors.Add("expected 7 main puzzles, found "+main);
        foreach(var s in w.sources) {
            if(!s) { errors.Add("null source"); continue; }
            if(!s.GetComponent<AbsorbableVisual>()) errors.Add("source without AbsorbableVisual: "+s.name);
            if(!s.powerLabel) errors.Add("source without power label: "+s.name); else if(s.powerLabel.activeSelf) errors.Add("permanent power label: "+s.name);
            if(!s.GetComponentInChildren<Collider>()) errors.Add("source without collider: "+s.name);
        }
        foreach(var p in ComboTable.Powers) { bool any=false; foreach(var s in w.sources) if(s && s.property==p) any=true; if(!any) errors.Add("no source for "+p); }
        if(w.zoneGates==null || w.zoneGates.Length<4) errors.Add("expected rice gate + 3 shortcuts");
        else foreach(var g in w.zoneGates) if(!g || !g.door || !g.world || (g.requires.Length==0 && g.anyCount==0)) errors.Add("gate not wired: "+(g?g.zoneId:"null"));
        if(!Physics.Raycast(w.spawn+Vector3.up,Vector3.down,3)) errors.Add("spawn has no ground");
        int cps=0; foreach(var z in w.zones) if(z && z.kind==BlobInteraction.Checkpoint) { cps++; if(!Physics.Raycast(z.respawn+Vector3.up,Vector3.down,3)) errors.Add("checkpoint "+z.index+" has no ground"); }
        if(cps!=w.checkpointCount) errors.Add("checkpointCount mismatch");
        foreach(var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)>0) errors.Add("missing script on "+go.name);
        if(errors.Count==0) Debug.Log("OOPS_V2_VALIDATE_OK puzzles="+w.puzzles.Length+" sources="+w.sources.Length+" parts="+w.ship.parts.Length);
        else foreach(var e in errors) Debug.LogError("OOPS_V2_VALIDATE_FAIL "+e);
    }
    static Vector3 Flat(Vector3 v) { v.y=0; return v; }
    [MenuItem("Tools/OOPS Planet/V2/Run V2 Test (all solutions)")]
    public static void RunV2Test() {
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode in OopsPlanet_V2 first"); return; }
        var w=Object.FindFirstObjectByType<BlobWorld>();
        if(!w || !w.ship) { Debug.LogError("Not the V2 scene (no ship)"); return; }
        if(!w.GetComponent<OopsV2Test>()) w.gameObject.AddComponent<OopsV2Test>();
    }
    // Map image for reviews: top-down render of the open scene into Evidence/v2-overview.png.
    [MenuItem("Tools/OOPS Planet/V2/Capture V2 Overview Image")]
    public static void CaptureOverview() {
        var go=new GameObject("Overview capture camera"); var cam=go.AddComponent<Camera>();
        cam.orthographic=true; cam.orthographicSize=48; cam.transform.SetPositionAndRotation(new Vector3(26,120,20),Quaternion.Euler(90,0,0));
        cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.62f,.74f,.85f); cam.farClipPlane=300;
        var rt=new RenderTexture(1600,1600,24); cam.targetTexture=rt; cam.Render();
        RenderTexture.active=rt; var tex=new Texture2D(1600,1600,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1600,1600),0,0); tex.Apply(); RenderTexture.active=null;
        Directory.CreateDirectory(Root+"/Evidence");
        File.WriteAllBytes(Root+"/Evidence/v2-overview.png",tex.EncodeToPNG());
        cam.targetTexture=null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
        AssetDatabase.Refresh(); Debug.Log("OOPS_V2_OVERVIEW "+Root+"/Evidence/v2-overview.png");
    }
}
}
#endif
