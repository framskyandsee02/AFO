#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet.Editor {
// Factory interior v3 (owner brief 2026-10-24): one roofed building inside the existing factory footprint
// (x -20..20, z 30..66, 8 m perimeter walls). Outside the footprint nothing is touched; the outside still talks to the
// factory only through: puzzle ids f1_gate / f2_boiler / f3_turbine (rice gate, ambience), s_conveyor_weld
// (conveyor shortcut gate), and the factory ship-part ids. See Docs/FACTORY_INTERIOR_TH.md for the layout tables.
//
//  Receiving hall (south)  -> P1 f1_gate  : four clogged sorting chutes, clear ANY one lane
//  Central hall            -> dormant Big Toy Press landmark, east walkway (5 m), refill sources
//  East wing  south        -> P2 f4_crane : safety door into the maintenance wing (plate / rust bolt / wax hatch)
//  East wing  north        -> P3 f2_boiler: reach the 5 m control balcony (steam / lift / slingshot) -> shortcut to walkway
//  West wing  south        -> P4 f5_cooling: cross the 9.5 m cooling channel (raft / slingshot / powered bridge)
//  West wing  north        -> P5 f3_turbine: restore ventilation (blasts / steam inlet / far motor / chain switches)
//  South-west vault room   -> P6 f6_assembly: open the parts cabinet (weld / cannon / induction) - door opens after F2 or F3
// Every shape is a greybox primitive coloured with the approved WorldPolish palette; sources and motors are dressed
// with the existing WorldPolish models.
public static class FactoryInterior {
    const string Base="Assets/Scenes/OopsPlanet/";
    public const float X0=-19.75f, X1=19.75f, Z0=30.25f, Z1=65.75f, Roof=8f;
    public class Result {
        public PuzzleObjective f1,f2,f3,weld;
        public readonly List<PuzzleObjective> puzzles=new List<PuzzleObjective>();
        public readonly List<BlobInteractable> sources=new List<BlobInteractable>();
        public readonly List<ShipPart> parts=new List<ShipPart>();
        public readonly List<ZoneGate> gates=new List<ZoneGate>();
        public readonly List<GameObject> art=new List<GameObject>();
    }
    static Transform root; static Font font; static BlobWorld world; static ShipRepair ship; static Result R;
    static Material teal, tealDark, cream, coral, dark, metal, yellow, water, coal, floorM, wallM, roofM, glass, wax, ice, wood, straw, plant, lampCool, lampWarm, partM, foam, cardboard;
    // ---------- materials ----------
    static readonly Dictionary<string,string> kitColors=new Dictionary<string,string>{{"Rust","B5735A"},{"Dust","D9D2BF"},{"Glow","FFD58A"},{"Ice","C6EEF7"}};
    static Material Pol(string role) {
        var m=AssetDatabase.LoadAssetAtPath<Material>(Base+"Materials/WorldPolish/POL_"+role+".mat"); if(m) return m;
        Color c=Color.gray; if(kitColors.TryGetValue(role,out var h)) ColorUtility.TryParseHtmlString("#"+h,out c);
        return Own("FI_"+role,c,role=="Glow");
    }
    static Material Own(string name,Color c,bool emit=false,float smooth=.3f) {
        if(!AssetDatabase.IsValidFolder(Base+"Materials/FactoryInterior")) AssetDatabase.CreateFolder(Base+"Materials","FactoryInterior");
        string path=Base+"Materials/FactoryInterior/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path); if(m) return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name}; m.SetColor("_BaseColor",c); m.SetFloat("_Smoothness",smooth);
        if(emit) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",c*1.4f); }
        AssetDatabase.CreateAsset(m,path); return m;
    }
    static void Materials() {
        teal=Pol("Teal"); cream=Pol("Cream"); coral=Pol("Coral"); dark=Pol("Dark"); metal=Pol("Metal"); yellow=Pol("Yellow"); water=Pol("Water"); coal=Pol("Coal");
        tealDark=Own("FI_TealDusty",new Color(.33f,.52f,.53f)); floorM=Own("FI_FloorWorn",new Color(.78f,.76f,.70f));
        wallM=Own("FI_WallCreamPeeled",new Color(.90f,.86f,.76f)); roofM=Own("FI_RoofTeal",new Color(.36f,.55f,.56f));
        glass=Own("FI_GlassPale",new Color(.75f,.92f,.95f),false,.8f); wax=Own("FI_Wax",new Color(.97f,.88f,.66f)); ice=Own("FI_Ice",new Color(.75f,.93f,1f),false,.7f);
        wood=Own("FI_WoodToy",new Color(.76f,.56f,.36f)); straw=Own("FI_PaperStock",new Color(.93f,.83f,.55f)); plant=Own("FI_Sprout",new Color(.52f,.72f,.42f));
        lampCool=Own("FI_LampCool",new Color(.55f,.8f,.95f),true); lampWarm=Own("FI_LampWarm",new Color(1f,.75f,.45f),true); partM=Own("FI_ShipPart",new Color(1f,.8f,.35f),true);
        foam=Own("FI_FoamBox",new Color(.95f,.72f,.70f)); cardboard=Own("FI_Cardboard",new Color(.72f,.55f,.38f));
    }
    // ---------- primitives ----------
    static GameObject Prim(PrimitiveType t,string name,Vector3 pos,Vector3 scale,Material m,Transform parent=null,bool collider=true,Vector3? euler=null) {
        var go=GameObject.CreatePrimitive(t); go.name=name; go.transform.SetParent(parent?parent:root,true);
        go.transform.position=pos; go.transform.rotation=Quaternion.Euler(euler??Vector3.zero); go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=m; if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        else if(t==PrimitiveType.Cylinder || (t==PrimitiveType.Sphere && (scale.x!=scale.y || scale.y!=scale.z))) {
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var mc=go.AddComponent<MeshCollider>(); mc.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh; mc.convex=true;
        }
        return go;
    }
    static GameObject Box(string n,Vector3 c,Vector3 s,Material m,Transform p=null,bool col=true,Vector3? e=null) => Prim(PrimitiveType.Cube,n,c,s,m,p,col,e);
    static GameObject Block(string n,Vector3 b,Vector3 s,Material m,Transform p=null,bool col=true) => Box(n,b+Vector3.up*s.y/2,s,m,p,col);
    static GameObject Cyl(string n,Vector3 b,float r,float h,Material m,Transform p=null,bool col=true) => Prim(PrimitiveType.Cylinder,n,b+Vector3.up*h/2,new Vector3(r*2,h/2,r*2),m,p,col);
    static GameObject Ball(string n,Vector3 c,float r,Material m,Transform p=null,bool col=false) => Prim(PrimitiveType.Sphere,n,c,Vector3.one*r*2,m,p,col);
    static WorldMaterial WM(GameObject g,MaterialKind k,bool critical=false) { var m=g.AddComponent<WorldMaterial>(); m.kind=k; m.critical=critical; return m; }
    // Wall along x or z with openings (centre, width, top, bottom); bottom>0 makes a window, the rest stays solid.
    static void Wall(string n,Vector2 a,Vector2 b,float h,Material m,float thick=.4f,params Vector4[] holes) {
        bool alongX=Mathf.Abs(b.x-a.x)>Mathf.Abs(b.y-a.y);
        float from=alongX?Mathf.Min(a.x,b.x):Mathf.Min(a.y,b.y), to=alongX?Mathf.Max(a.x,b.x):Mathf.Max(a.y,b.y), c=alongX?a.y:a.x;
        var list=new List<Vector4>(holes); list.Sort((p,q)=>p.x.CompareTo(q.x));
        void Seg(float s,float e,float y0,float y1) {
            if(e-s<.02f||y1-y0<.02f) return; float mid=(s+e)/2;
            Box(n,alongX?new Vector3(mid,(y0+y1)/2,c):new Vector3(c,(y0+y1)/2,mid),alongX?new Vector3(e-s,y1-y0,thick):new Vector3(thick,y1-y0,e-s),m);
            if(y0<.01f && y1>.7f) Box(n+" skirting",alongX?new Vector3(mid,.35f,c):new Vector3(c,.35f,mid),alongX?new Vector3(e-s,.7f,thick+.06f):new Vector3(thick+.06f,.7f,e-s),tealDark,null,false);
        }
        float cur=from;
        foreach(var o in list) { float s=o.x-o.y/2, e=o.x+o.y/2; Seg(cur,s,0,h); Seg(s,e,o.z,h); Seg(s,e,0,o.w); cur=e; }
        Seg(cur,to,0,h);
    }
    static Vector4 Door(float c,float w,float top=3.2f) => new Vector4(c,w,top,0);
    static void Label(string text,Vector3 pos,float yaw,float size=.22f,Color? col=null) {
        var g=new GameObject("Sign "+text.Split('\n')[0]); g.transform.SetParent(root); g.transform.position=pos; g.transform.rotation=Quaternion.Euler(0,yaw,0);
        var t=g.AddComponent<TextMesh>(); t.text=text; t.font=font; t.fontSize=64; t.characterSize=size*.45f; t.anchor=TextAnchor.MiddleCenter; t.alignment=TextAlignment.Center;
        t.color=col??new Color(.16f,.29f,.38f); g.GetComponent<Renderer>().sharedMaterial=TextMat();
        var b=g.GetComponent<Renderer>().bounds;
        var back=Box("Sign board",b.center+g.transform.forward*.05f,Vector3.one,cream,g.transform,false);
        back.transform.localRotation=Quaternion.identity; back.transform.localScale=new Vector3(Mathf.Max(b.size.x,b.size.z)+.5f,b.size.y+.3f,.06f);
    }
    // Signs inside the building must be hidden by walls (Shaders/WorldText.shader); the default font material draws on top.
    static Material textMat;
    static Material TextMat() {
        if(textMat) return textMat;
        string path=Base+"Materials/FactoryInterior/FI_WorldText.mat";
        textMat=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("OopsPlanet/WorldText");
        if(!textMat) { textMat=new Material(sh?sh:font.material.shader){name="FI_WorldText"}; AssetDatabase.CreateAsset(textMat,path); }
        textMat.mainTexture=font.material.mainTexture; EditorUtility.SetDirty(textMat);
        return textMat;
    }
    static Light Lamp(string n,Vector3 pos,bool warm=false,float range=11,float intensity=2.2f) {
        var l=new GameObject(n).AddComponent<Light>(); l.transform.SetParent(root); l.transform.position=pos;
        l.type=LightType.Point; l.range=range; l.intensity=intensity; l.shadows=LightShadows.None;
        l.color=warm?new Color(1,.82f,.6f):new Color(.62f,.78f,.9f);
        Ball(n+" bulb",pos+Vector3.up*.25f,.22f,warm?lampWarm:lampCool);
        Cyl(n+" shade",pos+Vector3.up*.3f,.38f,.18f,teal,null,false);
        return l;
    }
    // ---------- gameplay objects ----------
    static readonly Dictionary<BlobProperty,string> nameTh=new Dictionary<BlobProperty,string>{
        {BlobProperty.Magnet,"แม่เหล็ก"},{BlobProperty.Pressure,"แรงดัน"},{BlobProperty.Bounce,"เด้ง"},{BlobProperty.Water,"น้ำ"},{BlobProperty.Electric,"ไฟฟ้า"},{BlobProperty.Heat,"ความร้อน"}};
    static BlobInteractable Source(string room,BlobProperty prop,Vector3 basePos) {
        var go=new GameObject(room+" "+prop+" source"); go.transform.SetParent(root); go.transform.position=basePos;
        Vector3 size=prop==BlobProperty.Pressure?new Vector3(.7f,1.3f,.7f):prop==BlobProperty.Bounce?new Vector3(1.1f,.6f,1.1f):prop==BlobProperty.Water?new Vector3(.9f,1.1f,.9f):prop==BlobProperty.Electric?new Vector3(.6f,.9f,.6f):new Vector3(.9f,.9f,.9f);
        var full=Block("Full",basePos,size,teal,go.transform);
        var s=go.AddComponent<BlobInteractable>(); s.kind=BlobInteraction.Source; s.property=prop; s.onAbsorbed=AbsorbResult.Deplete; s.regenSeconds=20;
        s.fullVisual=full; s.indicator=full.GetComponent<Renderer>();
        var lg=new GameObject("Power label "+nameTh[prop]); lg.transform.SetParent(go.transform); lg.transform.position=basePos+Vector3.up*(size.y+.55f);
        var tm=lg.AddComponent<TextMesh>(); tm.text=nameTh[prop]; tm.font=font; tm.fontSize=64; tm.characterSize=.035f; tm.anchor=TextAnchor.MiddleCenter; tm.color=BlobPlayer.PropertyColor(prop);
        lg.GetComponent<Renderer>().sharedMaterial=font.material; lg.AddComponent<FaceCamera>(); s.powerLabel=lg; lg.SetActive(false);
        go.AddComponent<AbsorbableVisual>().source=s;
        R.sources.Add(s); return s;
    }
    static ShipPart Part(string id,string nameTh,Vector3 pos,bool startActive) {
        var go=new GameObject("Ship part: "+id); go.transform.SetParent(root); go.transform.position=pos;
        var p=go.AddComponent<ShipPart>(); p.id=id; p.nameTh=nameTh; p.zone="factory"; p.ship=ship;
        var v=Box("Part visual",pos,Vector3.one*.45f,partM,go.transform,false,new Vector3(30,45,0)); Ball("Part glow",pos,.14f,partM,v.transform);
        p.visual=v.transform; go.SetActive(startActive); R.parts.Add(p); return p;
    }
    static PuzzleObjective Puzzle(string id,string title,string hint,string solved,Bounds area,MonoBehaviour[] conds,PuzzleItem[] items=null,WorldMaterial[] mats=null,ConditionMode mode=ConditionMode.Any,bool secret=false) {
        var go=new GameObject("Puzzle "+id); go.transform.SetParent(root); go.transform.position=area.center;
        var p=go.AddComponent<PuzzleObjective>(); p.id=id; p.titleTh=title; p.hintTh=hint; p.solvedTh=solved; p.area=area;
        p.conditions=conds; p.items=items??new PuzzleItem[0]; p.materials=mats??new WorldMaterial[0]; p.mode=mode; p.isSecret=secret; p.celebratePoint=area.center;
        R.puzzles.Add(p); return p;
    }
    static PuzzleReaction React(PuzzleObjective pz,GameObject[] show=null,GameObject[] hide=null,Transform mover=null,Vector3 offset=default,Light[] lights=null,Renderer[] recolor=null,Color? col=null) {
        var r=pz.gameObject.AddComponent<PuzzleReaction>(); r.objective=pz;
        r.showWhenSolved=show??new GameObject[0]; r.hideWhenSolved=hide??new GameObject[0]; r.mover=mover; r.moveOffset=offset;
        r.lights=lights??new Light[0]; r.solvedLightColor=new Color(1,.8f,.55f); r.solvedIntensity=3.2f;
        r.recolor=recolor??new Renderer[0]; r.solvedColor=col??new Color(1,.72f,.5f); r.stopWhenSolved=new ParticleSystem[0]; r.grow=new Transform[0];
        return r;
    }
    static T Cond<T>(string n,Vector3 pos) where T:MonoBehaviour { var g=new GameObject(n); g.transform.SetParent(root); g.transform.position=pos; return g.AddComponent<T>(); }
    static MaterialStateCondition State(string label,MaterialGoal goal,params WorldMaterial[] t) { var c=Cond<MaterialStateCondition>("Condition: "+label,t[0].transform.position); c.label=label; c.goal=goal; c.targets=t; return c; }
    static AreaReachedCondition Reach(string n,Vector3 c,Vector3 s) { var r=Cond<AreaReachedCondition>(n,c); r.world=world; r.size=s; return r; }
    static ComboLock Lock(GameObject g,string combo,string label) { var l=g.AddComponent<ComboLock>(); l.acceptedCombos=new[]{combo}; l.label=label; return l; }
    static Rigidbody Body(GameObject g,float mass) { var rb=g.AddComponent<Rigidbody>(); rb.mass=mass; rb.linearDamping=.5f; rb.angularDamping=2; rb.interpolation=RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic; return rb; }
    static PuzzleItem Item(GameObject g,string key,Vector3 fallback) { var i=g.AddComponent<PuzzleItem>(); i.key=key; i.useFallback=true; i.fallbackPosition=fallback; return i; }
    static void ResetPad(Vector3 pos,PuzzleObjective target) {
        var pad=Box("Reset pad: "+target.id,pos+Vector3.up*.03f,new Vector3(1.8f,.06f,1.8f),yellow,null,false);
        var r=pad.AddComponent<PuzzleResetPad>(); r.target=target; r.size=new Vector3(1.8f,2,1.8f);
        Label("รีเซ็ต",pos+Vector3.up*1.3f,0,.12f);
    }
    static ZoneGate Gate(string id,string nameTh,Vector3 center,Vector3 size,PuzzleObjective[] anyOf,int anyCount) {
        var door=Box("Gate door: "+id,center,size,coral);
        var lamp=Ball("Gate lamp: "+id,center+Vector3.up*(size.y/2+.35f),.22f,lampWarm);
        var g=new GameObject("Zone gate: "+id).AddComponent<ZoneGate>(); g.transform.SetParent(root); g.transform.position=center-Vector3.up*size.y/2;
        g.world=world; g.zoneId=id; g.zoneNameTh=nameTh; g.requires=new PuzzleObjective[0]; g.requiresAnyOf=anyOf; g.anyCount=anyCount;
        g.door=door.transform; g.openOffset=Vector3.up*(size.y+.1f); g.lamp=lamp.GetComponent<Renderer>(); g.hintRadius=3.5f;
        R.gates.Add(g); return g;
    }
    // ---------- build ----------
    public static Result Build(BlobWorld w,Transform parent,Font uiFont,ShipRepair shipRepair) {
        world=w; font=uiFont; ship=shipRepair; R=new Result(); textMat=null; Materials();
        root=new GameObject("FACTORY INTERIOR").transform; root.SetParent(parent,true);
        Shell(); Receiving(); Central(); EastWing(); WestWing(); Assembly(); Secrets();
        return R;
    }
    static void Shell() {
        // Floor with the cooling-channel hole (x -19.75..-6, z 44..53.5): falling in = back to the checkpoint.
        foreach(var r in new[]{new Vector4(X0,X1,Z0,44),new Vector4(-6,X1,44,53.5f),new Vector4(X0,X1,53.5f,Z1)})
            Box("Floor",new Vector3((r.x+r.y)/2,-.5f,(r.z+r.w)/2),new Vector3(r.y-r.x,1,r.w-r.z),floorM);
        foreach(var x in new[]{-12f,0f,12f}) Box("Floor stripe",new Vector3(x,.006f,48),new Vector3(.12f,.01f,35),tealDark,null,false);
        // Interior lining of the perimeter (the outside walls keep their colliders but their renderers are hidden by the
        // soft-world art pass, so from inside you would see the sky). Visual only; openings match the outside doors.
        void Lining(string n,Vector2 a,Vector2 b,params Vector4[] holes) {
            int before=root.childCount; Wall(n,a,b,Roof,wallM,.12f,holes);
            for(int i=before;i<root.childCount;i++) { var c=root.GetChild(i).GetComponent<Collider>(); if(c) Object.DestroyImmediate(c); }
        }
        Lining("Lining S",new Vector2(X0,Z0+.05f),new Vector2(X1,Z0+.05f),Door(0,6,Roof),Door(14,6,Roof));
        Lining("Lining N",new Vector2(X0,Z1-.05f),new Vector2(X1,Z1-.05f));
        Lining("Lining W",new Vector2(X0+.05f,Z0),new Vector2(X0+.05f,Z1));
        Lining("Lining E",new Vector2(X1-.05f,Z0),new Vector2(X1-.05f,Z1),Door(55,4,4.4f));
        // channel walls (down to -4) so the gap reads as deep water
        Box("Channel wall S",new Vector3(-12.875f,-2,44),new Vector3(13.75f,4,.3f),tealDark); Box("Channel wall N",new Vector3(-12.875f,-2,53.5f),new Vector3(13.75f,4,.3f),tealDark);
        Box("Channel wall E",new Vector3(-6,-2,48.75f),new Vector3(.3f,4,9.5f),tealDark); Box("Channel wall W",new Vector3(X0,-2,48.75f),new Vector3(.3f,4,9.5f),tealDark);
        // Roof: one building. Renderers do not cast shadows so daylight still fills the rooms; skylight strips glow.
        for(int i=0;i<6;i++) {
            float z=Z0+i*(Z1-Z0)/6+(Z1-Z0)/12;
            var slab=Box("Roof",new Vector3(0,Roof+.2f,z),new Vector3(X1-X0+.5f,.4f,(Z1-Z0)/6),roofM);
            slab.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Box("Roof truss",new Vector3(0,Roof-.25f,z-(Z1-Z0)/12),new Vector3(X1-X0,.3f,.3f),teal,null,false);
            Box("Skylight",new Vector3(0,Roof-.02f,z),new Vector3(X1-X0-2,.04f,1.2f),lampCool,null,false);
        }
    }
    // ---------------- P1: receiving hall + sorting chutes ----------------
    static void Receiving() {
        // wall between receiving hall (z<39) and the halls; chutes x -3.25..3.25 + sorting shutter x 5..8 (opens on solve)
        Wall("Receiving north wall",new Vector2(-6,39),new Vector2(X1,39),Roof,wallM,.4f,new Vector4(0,6.5f,2.2f,0),Door(6.5f,3f,3.2f));
        var shutter=Box("Sorting shutter (opens when a chute is clear)",new Vector3(6.5f,1.6f,39),new Vector3(3,3.2f,.3f),coral);
        for(int i=0;i<5;i++) Box("Shutter slat",new Vector3(6.5f,.4f+i*.6f,38.82f),new Vector3(3,.08f,.05f),tealDark,null,false);
        // chute pillars + roof (2.2 m: BLOB cannot climb over a stack)
        foreach(var x in new[]{-1.6f,0f,1.6f}) Block("Chute divider",new Vector3(x,0,40),new Vector3(.3f,2.2f,3),teal);
        Box("Chute roof",new Vector3(0,2.35f,40),new Vector3(6.5f,.3f,3),teal);
        Box("Chute hopper",new Vector3(0,3.4f,40.4f),new Vector3(6.5f,1.8f,2.2f),cream,null,false,new Vector3(-15,0,0));
        Label("ห้องคัดแยก — รางอุดตัน\nเปิดรางใดรางหนึ่งก็ผ่านได้",new Vector3(0,4.9f,38.7f),0,.17f);
        var items=new List<PuzzleItem>(); var mats=new List<WorldMaterial>(); var conds=new List<MonoBehaviour>();
        void Lane(string key,float x,string sign) {
            var cz=Cond<ClearZoneCondition>("Condition: chute "+key,new Vector3(x,1.1f,40)); cz.key=key; cz.size=new Vector3(1.25f,2.2f,3); conds.Add(cz);
            Label(sign,new Vector3(x,2.75f,38.45f),0,.09f);
        }
        // A: magnetic scrap (Magnet pulls it back into the hall)
        for(int i=0;i<3;i++) { var g=Box("Chute A scrap plate (4 kg)",new Vector3(-2.4f,.35f+i*.62f,39.3f+i*.25f),new Vector3(1.1f,.55f,1f),metal); Body(g,4); g.AddComponent<BlobMagnetic>(); mats.Add(WM(g,MaterialKind.Metal)); items.Add(Item(g,"chute_a",new Vector3(-7,.6f,35+i))); }
        Lane("chute_a",-2.4f,"เศษเหล็ก");
        // B: light foam toy boxes (Pressure blows them through)
        for(int i=0;i<4;i++) { var g=Box("Chute B foam toy box (1 kg)",new Vector3(-.8f,.35f+(i%2)*.7f,39.1f+(i/2)*1.3f),new Vector3(1.05f,.65f,1f),foam); Body(g,1); items.Add(Item(g,"chute_b",new Vector3(-.8f+(i%2)*.8f,.5f,43.5f+i*.7f))); }
        Lane("chute_b",-.8f,"กล่องโฟม");
        // C: heavy plastic crates 16 kg (only the high-pressure water jet moves them)
        for(int i=0;i<2;i++) { var g=Box("Chute C heavy crate (16 kg)",new Vector3(.8f,.6f+i*1.05f,39.6f),new Vector3(1.1f,1f,1.6f),coral); Body(g,16); items.Add(Item(g,"chute_c",new Vector3(.8f,.6f,44.5f+i*1.3f))); }
        Lane("chute_c",.8f,"ลังหนัก");
        // D: bundled cardboard 14 kg (burns; too heavy to blow)
        for(int i=0;i<3;i++) { var g=Box("Chute D cardboard bundle (14 kg)",new Vector3(2.4f,.4f+i*.68f,39.3f+(i%2)*.35f),new Vector3(1.1f,.62f,1.2f),cardboard); Body(g,14); mats.Add(WM(g,MaterialKind.Straw)); items.Add(Item(g,"chute_d",new Vector3(2.4f,.6f,44.5f+i))); }
        Lane("chute_d",2.4f,"กล่องกระดาษ");
        var lights=new[]{Lamp("Central hall lamp S",new Vector3(0,7,44)),Lamp("Central hall lamp N",new Vector3(0,7,55))};
        R.f1=Puzzle("f1_gate","ห้องคัดแยก: รางอุดตัน","รางคัดแยก 4 รางอุดด้วยของคนละแบบ เปิดรางใดรางหนึ่งให้โล่งก็ผ่านไปโถงกลางได้","ทางสู่โถงกลางเปิดแล้ว — ปีกตะวันออกและตะวันตกเลือกไปก่อนได้",
            new Bounds(new Vector3(4,3,34.6f),new Vector3(28,6,9.5f)),conds.ToArray(),items.ToArray(),mats.ToArray());
        var partF1=Part("part_f1","มอเตอร์ประตูยาน",new Vector3(6.5f,.7f,36.8f),false);
        React(R.f1,new[]{partF1.gameObject},null,shutter.transform,Vector3.up*3.3f,lights);
        ResetPad(new Vector3(-6.5f,0,32.2f),R.f1);
        Source("Receiving",BlobProperty.Magnet,new Vector3(-8.2f,0,37.5f));
        Source("Receiving",BlobProperty.Pressure,new Vector3(-4.5f,0,31.2f));
        Source("Receiving",BlobProperty.Water,new Vector3(4.5f,0,31.2f));
        Source("Receiving",BlobProperty.Heat,new Vector3(9.5f,0,37.5f));
        Label("โรงงานของเล่น OOPS!\nโถงรับของ",new Vector3(-6.5f,4.2f,38.75f),0,.2f);
        // dust, old parcels, cracked floor sprouts (decor only, kept off the chute lanes and the shutter)
        foreach(var p in new[]{new Vector3(12,0,31.5f),new Vector3(13.4f,0,31.8f),new Vector3(-9.5f,0,31.4f)}) Block("Old parcel",p,new Vector3(.9f,.7f,.8f),cardboard,null,true);
        foreach(var p in new[]{new Vector3(10.6f,0,35.5f),new Vector3(-2.8f,0,31f),new Vector3(16.5f,0,38.4f)}) Ball("Floor sprout",p+Vector3.up*.12f,.18f,plant);
    }
    // ---------------- central hall ----------------
    static Renderer[] pressGlow;
    const float PressDrop=.35f;
    static void Central() {
        // walls to the wings: west (cooling door z 41.5, turbine return door z 61), east (crane door z 43, balcony door at 5 m z 63.5)
        Wall("Central west wall",new Vector2(-6,39),new Vector2(-6,Z1),Roof,wallM,.4f,Door(41.5f,3f),Door(58f,2.2f,2.4f),Door(61f,2.6f));
        Wall("Central east wall",new Vector2(6,39),new Vector2(6,Z1),Roof,wallM,.4f,Door(49.6f,2.8f),new Vector4(63.5f,1.8f,8f,5f));
        // Big Toy Press landmark (dormant: dark eyes until the factory wakes)
        var press=new GameObject("Big Toy Press (landmark)"); press.transform.SetParent(root);
        Cyl("Press base",new Vector3(0,0,60),2.8f,1.2f,teal,press.transform);
        Cyl("Press body",new Vector3(0,1.2f,60),2.1f,4.2f,cream,press.transform);
        Prim(PrimitiveType.Sphere,"Press dome",new Vector3(0,5.4f,60),new Vector3(4.2f,2.5f,4.2f),teal,press.transform,true);
        Cyl("Press top ledge",new Vector3(0,6.5f,60),1.6f,.25f,teal,press.transform);
        foreach(var x in new[]{-2.6f,2.6f}) { Prim(PrimitiveType.Capsule,"Press arm",new Vector3(x,3.4f,59.2f),new Vector3(.9f,1.6f,.9f),coral,press.transform,true,new Vector3(0,0,x<0?25:-25)); }
        var eyes=new List<Renderer>();
        foreach(var x in new[]{-.8f,.8f}) eyes.Add(Ball("Press eye",new Vector3(x,4.2f,57.95f),.38f,dark,press.transform).GetComponent<Renderer>());
        Box("Press mouth slot",new Vector3(0,2.6f,57.9f),new Vector3(1.6f,.35f,.1f),dark,press.transform,false);
        // the whole press sits 0.35 m low (base sunk in the floor) so BLOB (1.3 m) fits on the top ledge under the 8 m roof
        press.transform.position=Vector3.down*PressDrop;
        pressGlow=eyes.ToArray();
        Label("เครื่องปั๊มของเล่นยักษ์ (หลับอยู่)",new Vector3(0,1.6f,57.4f),0,.14f);
        // east walkway (5 m) with gentle stairs (0.2 m steps)
        Box("Walkway (5 m)",new Vector3(5.1f,4.85f,56f),new Vector3(1.8f,.3f,16.8f),teal);
        for(int i=0;i<25;i++) Block("Walkway stair",new Vector3(5.1f,0,40.3f+i*.3f),new Vector3(1.8f,.2f*(i+1),.3f),tealDark);
        Box("Walkway rail",new Vector3(4.25f,5.55f,56f),new Vector3(.08f,.08f,16.8f),yellow,null,true);
        for(int i=0;i<9;i++) Box("Walkway post",new Vector3(4.25f,5.25f,49.2f+i*1.85f),new Vector3(.08f,.7f,.08f),dark,null,false);
        Source("Central",BlobProperty.Electric,new Vector3(-4.6f,0,47.5f));
        Source("Central",BlobProperty.Bounce,new Vector3(-4.4f,0,52.5f));
        Lamp("Central hall lamp mid",new Vector3(0,7,49.5f));
        foreach(var p in new[]{new Vector3(-4.8f,0,64.6f),new Vector3(3.2f,0,64.8f)}) Ball("Floor sprout",p+Vector3.up*.12f,.2f,plant);
        Box("Dripping pipe",new Vector3(-3,7.2f,45),new Vector3(.3f,.3f,6),metal,null,false);
        Ball("Drip puddle",new Vector3(-3,.01f,46.5f),.6f,water).transform.localScale=new Vector3(1.4f,.02f,1f);
    }
    // ---------------- east wing: P2 crane room, P3 boiler room ----------------
    static void EastWing() {
        // P2 crane room x 6..19.75, z 39..51; safety wall z=51 with main door x 12.5, rust-bolt hatch x 8.6, wax crawl hatch x 17.6
        Wall("Crane/boiler wall",new Vector2(6,51),new Vector2(X1,51),Roof,wallM,.4f,Door(12.5f,3f,4f),Door(8.6f,1.6f,2.2f),Door(17.6f,1.6f,1.6f));
        var safety=Box("Safety door (vault)",new Vector3(12.5f,2,51),new Vector3(3,4,.45f),coral);
        Cyl("Safety door wheel",new Vector3(12.5f,1.6f,50.62f),.55f,.12f,yellow,safety.transform,false).transform.rotation=Quaternion.Euler(90,0,0);
        // overhead crane landmark
        Box("Crane rail W",new Vector3(7,6.6f,45),new Vector3(.4f,.4f,12),teal,null,false); Box("Crane rail E",new Vector3(19.2f,6.6f,45),new Vector3(.4f,.4f,12),teal,null,false);
        Box("Crane bridge",new Vector3(13.1f,6.3f,46),new Vector3(12.6f,.7f,1.2f),coral,null,false);
        Cyl("Crane trolley",new Vector3(13.6f,5.4f,46),.7f,.9f,yellow,null,false);
        Box("Crane hook cable",new Vector3(13.6f,4.3f,46),new Vector3(.06f,1.6f,.06f),dark,null,false);
        Prim(PrimitiveType.Capsule,"Crane hook",new Vector3(13.6f,3.3f,46),new Vector3(.45f,.35f,.45f),yellow,null,false);
        // ① weight plate + 6 kg counterweight  ② 30 kg machine (Electric + Magnet)
        var plateGO=Block("Crane weight plate",new Vector3(12.5f,0,47.6f),new Vector3(2.2f,.1f,2.2f),yellow,null,false);
        var plate=plateGO.AddComponent<WeightPlate>(); plate.size=new Vector3(2.2f,1.4f,2.2f); plate.minMass=5; plate.lamp=plateGO.GetComponent<Renderer>();
        Label("แท่นถ่วง ≥ 5 กก.",new Vector3(12.5f,1.6f,49.1f),180,.11f);
        var weight=Prim(PrimitiveType.Sphere,"Crane counterweight (6 kg, magnetic)",new Vector3(17.5f,.5f,42.5f),Vector3.one,metal);
        Body(weight,6); weight.AddComponent<BlobMagnetic>(); var wItem=Item(weight,"crane_weight",new Vector3(17.5f,.6f,42.5f));
        var heavy=Box("Old press machine (30 kg, magnetic)",new Vector3(8.4f,.75f,41),new Vector3(1.3f,1.5f,1.3f),tealDark);
        Body(heavy,30); heavy.AddComponent<BlobMagnetic>(); var hItem=Item(heavy,"crane_weight",new Vector3(8.4f,.8f,41));
        // ③ rust bolt on the small hatch (Magnet Cannon)  ④ wax-sealed crawl hatch (Heat, then Pressure)
        var hingeA=new GameObject("Rust hatch hinge"); hingeA.transform.SetParent(root); hingeA.transform.position=new Vector3(7.8f,0,51);
        Box("Rust hatch door",new Vector3(8.6f,1.1f,51),new Vector3(1.6f,2.2f,.25f),metal,hingeA.transform);
        var bolt=Box("Rusty bolt (breaks on impact)",new Vector3(9.25f,1.2f,50.7f),new Vector3(.35f,.35f,.3f),Own("FI_Rust",new Color(.7f,.42f,.3f)));
        var boltM=WM(bolt,MaterialKind.Metal); bolt.AddComponent<ImpactBreak>();
        var doorA=hingeA.AddComponent<LatchedDoor>(); doorA.latch=boltM; doorA.hinge=hingeA.transform; bolt.GetComponent<ImpactBreak>().opens=doorA;
        var hingeB=new GameObject("Wax crawl hatch hinge"); hingeB.transform.SetParent(root); hingeB.transform.position=new Vector3(16.8f,0,51);
        Box("Wax crawl hatch",new Vector3(17.6f,.8f,51),new Vector3(1.6f,1.6f,.25f),tealDark,hingeB.transform);
        var waxSeal=WM(Box("Wax seal",new Vector3(18.25f,.8f,50.72f),new Vector3(.3f,.5f,.25f),wax),MaterialKind.Wax);
        var doorB=hingeB.AddComponent<LatchedDoor>(); doorB.latch=waxSeal; doorB.hinge=hingeB.transform;
        Box("Crawl duct roof",new Vector3(17.6f,1.75f,52.4f),new Vector3(1.8f,.3f,2.6f),teal); foreach(var x in new[]{16.65f,18.55f}) Block("Crawl duct side",new Vector3(x,0,52.4f),new Vector3(.2f,1.6f,2.6f),teal);
        Label("ประตูนิรภัย → ปีกซ่อมบำรุง",new Vector3(12.5f,4.6f,50.75f),180,.14f);
        var f4=Puzzle("f4_crane","ห้องเครน: ประตูนิรภัย","เปิดทางเข้าปีกซ่อมบำรุง: ถ่วงแท่นด้วยของหนัก หรือเปิดช่องเล็กข้างประตู","ประตูนิรภัยเปิดแล้ว",
            new Bounds(new Vector3(12.9f,3,45),new Vector3(13.7f,6,12)),new MonoBehaviour[]{plate,doorA,doorB},new[]{wItem,hItem},new[]{boltM,waxSeal});
        React(f4,null,null,safety.transform,Vector3.up*4.1f,new[]{Lamp("Crane room lamp",new Vector3(13,7,44.5f))});
        ResetPad(new Vector3(18.4f,0,46),f4);
        Source("Crane room",BlobProperty.Magnet,new Vector3(13.2f,0,40.2f));
        Source("Crane room",BlobProperty.Heat,new Vector3(18.6f,0,49.4f));
        Source("Crane room",BlobProperty.Pressure,new Vector3(9.8f,0,46.5f));
        // ---- P3 boiler room x 6..19.75, z 51..65.75; control balcony at 5 m (z 60.9..65.75)
        Box("Control balcony (5 m)",new Vector3(12.9f,4.85f,63.33f),new Vector3(13.7f,.3f,4.85f),teal);
        Box("Balcony rail",new Vector3(14f,5.55f,60.95f),new Vector3(7.3f,.08f,.08f),yellow);
        var balconyDoor=Box("Balcony shortcut door",new Vector3(6,6.5f,63.5f),new Vector3(.45f,3f,1.8f),coral);
        Cyl("Giant boiler",new Vector3(15.5f,0,56.5f),2.2f,6.5f,coral);
        Ball("Giant boiler dome",new Vector3(15.5f,6.5f,56.5f),2.2f,teal,null,true);
        var basin=WM(Block("Boiler steam vent basin",new Vector3(10.6f,0,58.4f),new Vector3(2.6f,.6f,2.6f),water),MaterialKind.WaterBody);
        Label("ช่องระบายไอน้ำ",new Vector3(10.6f,1.4f,56.8f),180,.1f);
        var liftMotor=WM(Block("Boiler cargo lift motor (machine)",new Vector3(14.4f,0,62.8f),new Vector3(.9f,1.1f,.9f),tealDark),MaterialKind.Machine,true);
        var liftPlat=Box("Boiler cargo lift",new Vector3(18.1f,-.12f,59.55f),new Vector3(2.6f,.3f,2.6f),yellow);
        var lift=liftPlat.AddComponent<PoweredLift>(); lift.motor=liftMotor; lift.platform=liftPlat.transform; lift.height=4.97f; lift.world=world; lift.waitForRider=true;
        foreach(var dz in new[]{-1.4f,1.4f}) Block("Lift rail",new Vector3(19.5f,0,59.55f+dz),new Vector3(.2f,7.5f,.2f),dark);
        Box("Balcony beam",new Vector3(12.9f,7.3f,63.5f),new Vector3(13.7f,.35f,.35f),metal,null,false);
        var anchor=new GameObject("Balcony beam anchor (magnet slingshot)"); anchor.transform.SetParent(root); anchor.transform.position=new Vector3(9.2f,5.8f,62.8f); anchor.AddComponent<MagnetAnchor>();
        Block("Balcony anchor post",new Vector3(9.2f,5f,62.8f),new Vector3(.2f,.6f,.2f),metal); Ball("Anchor marker",anchor.transform.position,.25f,yellow);
        var balconyReach=Reach("Condition: on the control balcony",new Vector3(12.9f,6.3f,63.3f),new Vector3(13.5f,2.6f,4.7f));
        R.f2=Puzzle("f2_boiler","ห้องหม้อต้ม: ชานควบคุม","ขึ้นชานควบคุมชั้นบน: ไอน้ำจากช่องระบาย ลิฟต์ขนของ หรือสลิงแม่เหล็กเข้าคาน","ชานควบคุมทำงาน — ทางลัดลงโถงกลางเปิดแล้ว",
            new Bounds(new Vector3(12.9f,3,58.4f),new Vector3(13.7f,8,14.7f)),new MonoBehaviour[]{balconyReach},null,new[]{basin});
        var partF2=Part("part_f2","วาล์วหม้อไอน้ำยาน",new Vector3(16.5f,5.7f,64.6f),false);
        React(R.f2,new[]{partF2.gameObject},new[]{balconyDoor},null,default,new[]{Lamp("Boiler room lamp",new Vector3(12,7,55))});
        Source("Boiler room",BlobProperty.Water,new Vector3(7.4f,0,53.2f));
        Source("Boiler room",BlobProperty.Heat,new Vector3(12.6f,0,53.6f));
        Source("Boiler room",BlobProperty.Electric,new Vector3(7.4f,0,57.4f));
        Source("Boiler room",BlobProperty.Bounce,new Vector3(9.2f,0,54.6f));
        // s_conveyor_weld (keeps the outside shortcut interface): broken rail at the east door
        var rail=Box("Broken conveyor rail (weld me)",new Vector3(18.6f,.3f,55),new Vector3(.6f,.6f,2.4f),Own("FI_Rust",new Color(.7f,.42f,.3f)));
        var weld=Lock(rail,"electric+heat","weld");
        R.weld=Puzzle("s_conveyor_weld","รางสายพานขาด","","ทางลัดสายพานกลับฮับเปิดแล้ว",new Bounds(new Vector3(18,1.5f,55),new Vector3(4,4,5)),new MonoBehaviour[]{weld},secret:true);
        React(R.weld,null,null,null,default,null,new[]{rail.GetComponent<Renderer>()},new Color(.6f,.75f,.78f));
        Label("รางขาด — เชื่อมได้?",new Vector3(19.35f,2.2f,57.6f),90,.1f);
        // the big machine warms up as rooms are restored
        var f2Glow=R.f2.gameObject.AddComponent<PuzzleReaction>(); f2Glow.objective=R.f2; f2Glow.recolor=pressGlow; f2Glow.solvedColor=new Color(1,.7f,.35f);
        f2Glow.lights=new Light[0]; f2Glow.showWhenSolved=new GameObject[0]; f2Glow.hideWhenSolved=new GameObject[0]; f2Glow.stopWhenSolved=new ParticleSystem[0]; f2Glow.grow=new Transform[0];
    }
    // ---------------- west wing: P4 cooling channel, P5 turbine ----------------
    static void WestWing() {
        Wall("West wing south wall",new Vector2(X0,39),new Vector2(-6,39),Roof,wallM,.4f,new Vector4(-8f,1.6f,1.7f,0));   // crawl hatch to the receiving hall (W+E secret)
        // channel water (trigger, conducts and steams) + low railing on the south bank edge
        var deep=Own("FI_DeepWater",new Color(.10f,.30f,.52f),false,.92f);
        var waterGO=Box("Cooling channel water",new Vector3(-12.875f,-1.05f,48.75f),new Vector3(13.7f,.9f,9.4f),deep);
        for(int i=0;i<5;i++) Box("Channel ripple",new Vector3(-12.875f,-.58f,45.2f+i*1.9f),new Vector3(12.5f,.02f,.12f),Own("FI_Ripple",new Color(.55f,.8f,.9f)),null,false);
        waterGO.GetComponent<Collider>().isTrigger=true; WM(waterGO,MaterialKind.WaterBody);
        Box("Channel warning stripe",new Vector3(-12.875f,.01f,43.7f),new Vector3(13.7f,.02f,.4f),yellow,null,false);
        Label("ร่องน้ำหล่อเย็น 9.5 ม. — ตกน้ำแล้วกลับจุดเช็กพอยต์",new Vector3(-12.9f,2.4f,39.25f),180,.12f);
        // ① raft (Water + Magnet), starts at the far side
        var raftGO=Box("Cooling raft (20 kg, magnetic)",new Vector3(-8.6f,-.3f,48.8f),new Vector3(2.2f,.4f,2.2f),coral);
        var raftRb=raftGO.AddComponent<Rigidbody>(); raftRb.mass=20; raftGO.AddComponent<BlobMagnetic>();
        var raft=raftGO.AddComponent<ShuttleRaft>(); raft.world=world; raft.nearDock=new Vector3(-8.6f,-.3f,45.3f); raft.farDock=new Vector3(-8.6f,-.3f,52.2f);
        // ② slingshot anchor post on the north bank
        Block("North bank post",new Vector3(-15,0,54.4f),new Vector3(.4f,1.8f,.4f),metal);
        var an=new GameObject("North bank anchor (magnet slingshot)"); an.transform.SetParent(root); an.transform.position=new Vector3(-15,1.9f,54.4f); an.AddComponent<MagnetAnchor>();
        Ball("Anchor marker",an.transform.position,.25f,yellow);
        // ③ powered service bridge (rises while its motor has power)
        var bMotor=WM(Block("Service bridge motor (machine)",new Vector3(-17.8f,0,41.4f),new Vector3(.9f,1.1f,.9f),tealDark),MaterialKind.Machine,true);
        var deck=Box("Service bridge deck",new Vector3(-15.8f,-2.15f,48.75f),new Vector3(2.2f,.3f,9.4f),yellow);
        var bl=deck.AddComponent<PoweredLift>(); bl.motor=bMotor; bl.platform=deck.transform; bl.height=2.0f; bl.speed=3; bl.world=world; bl.standSize=new Vector3(2.2f,1.4f,9.4f);
        var permanent=Box("Permanent catwalk (after crossing)",new Vector3(-11.6f,-.15f,48.75f),new Vector3(1.6f,.3f,9.4f),teal); permanent.SetActive(false);
        var north=Reach("Condition: north bank",new Vector3(-12.9f,1.3f,54.75f),new Vector3(13.7f,2.6f,2.5f));
        var f5=Puzzle("f5_cooling","ห้องหล่อเย็น: ข้ามร่องน้ำ","ข้ามร่องน้ำไปห้องเครื่อง: ลากแพเหล็กมาข้าม ดีดตัวไปเสาอีกฝั่ง หรือจ่ายไฟให้สะพานบริการ","ข้ามได้แล้ว — ทางเดินถาวรกางออก",
            new Bounds(new Vector3(-12.9f,2,47),new Vector3(13.7f,6,16)),new MonoBehaviour[]{north});
        raft.resetWith=f5;
        React(f5,new[]{permanent},null,null,default,new[]{Lamp("Cooling room lamp",new Vector3(-13,7,47))});
        ResetPad(new Vector3(-10.6f,0,40.4f),f5);
        Source("Cooling room",BlobProperty.Water,new Vector3(-11.8f,0,40.3f));
        Source("Cooling room",BlobProperty.Magnet,new Vector3(-7.4f,0,40.4f));
        // ---- P5 turbine room (north): x -19.75..-6, z 53.5..65.75
        var tGO=new GameObject("Round vent turbine"); tGO.transform.SetParent(root); tGO.transform.position=new Vector3(-19.2f,3.6f,60.2f);
        Prim(PrimitiveType.Cylinder,"Turbine ring",tGO.transform.position,new Vector3(5,.35f,5),teal,tGO.transform,false,new Vector3(0,0,90));
        var rotor=new GameObject("Rotor"); rotor.transform.SetParent(tGO.transform); rotor.transform.position=tGO.transform.position+Vector3.right*.2f; rotor.transform.rotation=Quaternion.Euler(0,90,0);
        for(int i=0;i<5;i++) { var bld=Box("Blade",rotor.transform.position,new Vector3(.6f,2.1f,.1f),coral,rotor.transform,false); bld.transform.localRotation=Quaternion.Euler(0,0,i*72); bld.transform.position+=bld.transform.up*1.05f; }
        var tcol=tGO.AddComponent<BoxCollider>(); tcol.size=new Vector3(.8f,4.6f,4.6f);
        var gauge=new Renderer[3]; for(int i=0;i<3;i++) gauge[i]=Ball("Turbine gauge lamp",new Vector3(-19.3f,1+i*.55f,57.2f),.2f,dark).GetComponent<Renderer>();
        var turbine=tGO.AddComponent<PressureTurbine>(); turbine.rotor=rotor.transform; turbine.gaugeLamps=gauge;
        Box("Blow here marker",new Vector3(-15,.01f,60.2f),new Vector3(1.4f,.02f,1.4f),Pol("Blue"),null,false); Label("ช่องลมเข้า",new Vector3(-15,1.6f,61.6f),-90,.1f);
        var feed=WM(Block("Turbine steam feed tank",new Vector3(-17.4f,0,64.4f),new Vector3(2.2f,.5f,2.2f),water),MaterialKind.WaterBody);
        Box("Steam intake pipe",new Vector3(-17.4f,3,65.3f),new Vector3(.8f,4.5f,.6f),metal,null,false);
        var inlet=Cond<SteamInlet>("Condition: turbine steam intake",new Vector3(-17.4f,1.5f,64.4f)); inlet.size=new Vector3(3,4,3); inlet.needed=2;
        var farMotor=WM(Block("Turbine far motor (machine, high)",new Vector3(-12.5f,6.4f,65.2f),new Vector3(1,.9f,.8f),tealDark),MaterialKind.Machine,true);
        Box("Far motor bracket",new Vector3(-12.5f,6.2f,65.5f),new Vector3(1.4f,.15f,.6f),dark,null,false);
        var sws=new List<ElectricSwitch>(); int k=1;
        foreach(var x in new[]{-18f,-14.5f,-11f}) { Block("Switch post",new Vector3(x,0,55.6f),new Vector3(.2f,1.1f,.2f),dark); var s=Box("Vent switch "+k,new Vector3(x,1.45f,55.6f),new Vector3(.8f,.8f,.3f),Own("FI_SwitchOff",new Color(.3f,.32f,.36f))); var es=s.AddComponent<ElectricSwitch>(); es.order=k++; es.lamp=s.GetComponent<Renderer>(); sws.Add(es); }
        Label("สวิตช์ 1 → 2 → 3",new Vector3(-14.5f,2.4f,55.35f),180,.11f);
        var seq=Cond<SwitchGroup>("Condition: vent switches in order",new Vector3(-14.5f,1.5f,55.6f)); seq.mode=SwitchGroup.Mode.Sequence; seq.switches=sws.ToArray(); seq.window=3f;
        var returnDoor=Box("Turbine room return door",new Vector3(-6,1.6f,61),new Vector3(.45f,3.2f,2.6f),coral);
        R.f3=Puzzle("f3_turbine","ห้องกังหัน: ระบบระบายอากาศ","คืนระบบระบายอากาศ: เป่ากังหันจากช่องลม ส่งไอน้ำเข้าท่อ จ่ายไฟมอเตอร์ไกล หรือชิ่งสวิตช์ 1-2-3","อากาศถ่ายเทแล้ว — ประตูกลับโถงกลางเปิด",
            new Bounds(new Vector3(-12.9f,3,59.6f),new Vector3(13.7f,8,12.3f)),new MonoBehaviour[]{turbine,inlet,State("far-motor",MaterialGoal.Powered,farMotor),seq},null,new[]{feed});
        turbine.objective=R.f3;
        var partF3=Part("part_f3","ใบพัดยาน",new Vector3(-10,.7f,62.6f),false);
        var tl=new[]{Lamp("Turbine room lamp",new Vector3(-13,7,60))};
        React(R.f3,new[]{partF3.gameObject},new[]{returnDoor},null,default,tl);
        var f3Glow=R.f3.gameObject.AddComponent<PuzzleReaction>(); f3Glow.objective=R.f3; f3Glow.recolor=pressGlow; f3Glow.solvedColor=new Color(.55f,.95f,1f);
        f3Glow.lights=new Light[0]; f3Glow.showWhenSolved=new GameObject[0]; f3Glow.hideWhenSolved=new GameObject[0]; f3Glow.stopWhenSolved=new ParticleSystem[0]; f3Glow.grow=new Transform[0];
        Source("Turbine room",BlobProperty.Pressure,new Vector3(-12,0,57.4f));
        Source("Turbine room",BlobProperty.Heat,new Vector3(-14.6f,0,64.6f));
        Source("Turbine room",BlobProperty.Electric,new Vector3(-8,0,64.6f));
    }
    // ---------------- P6 assembly vault (south-west) ----------------
    static void Assembly() {
        Wall("Assembly east wall",new Vector2(-10,Z0),new Vector2(-10,39),Roof,wallM,.4f,Door(36.2f,2.4f),new Vector4(32.6f,2.6f,2.6f,1.0f));
        var win=Box("Assembly window glass",new Vector3(-10,1.8f,32.6f),new Vector3(.1f,1.6f,2.6f),glass); win.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        Gate("factory_assembly","ห้องประกอบ",new Vector3(-10,1.6f,36.2f),new Vector3(.45f,3.2f,2.4f),new[]{R.f2,R.f3},1);
        Label("ห้องประกอบ — ไฟเข้าเมื่อหม้อต้มหรือกังหันทำงาน",new Vector3(-9.75f,3.9f,34.4f),90,.11f);
        // cabinet against the west wall
        var cab=Block("Parts cabinet",new Vector3(-18.6f,0,34.6f),new Vector3(2,3.4f,4.4f),teal);
        var doorL=Box("Cabinet door L",new Vector3(-17.55f,1.7f,33.5f),new Vector3(.12f,3.2f,2.1f),cream); var doorR=Box("Cabinet door R",new Vector3(-17.55f,1.7f,35.7f),new Vector3(.12f,3.2f,2.1f),cream);
        Cyl("Assembly furnace (landmark)",new Vector3(-13,0,31.8f),1.2f,2.6f,coral); Ball("Furnace window",new Vector3(-13,1.6f,32.95f),.45f,lampWarm);
        // ① broken drive rail (Electric + Heat weld)
        var drive=Box("Cabinet drive rail (broken)",new Vector3(-16.2f,.25f,37.6f),new Vector3(2.6f,.5f,.5f),Own("FI_Rust",new Color(.7f,.42f,.3f)));
        var weld=Lock(drive,"electric+heat","weld-drive");
        // ② rusty lock flap (Magnet Cannon)
        var flapHinge=new GameObject("Cabinet lock flap hinge"); flapHinge.transform.SetParent(root); flapHinge.transform.position=new Vector3(-17.45f,2.6f,31.9f);
        Box("Cabinet lock flap",new Vector3(-17.45f,2.6f,32.35f),new Vector3(.15f,.7f,.9f),metal,flapHinge.transform);
        var rustLock=Box("Rusty cabinet lock",new Vector3(-17.3f,2.6f,32.35f),new Vector3(.3f,.35f,.35f),Own("FI_Rust",new Color(.7f,.42f,.3f)));
        var rustM=WM(rustLock,MaterialKind.Metal); rustLock.AddComponent<ImpactBreak>();
        var flap=flapHinge.AddComponent<LatchedDoor>(); flap.latch=rustM; flap.hinge=flapHinge.transform; rustLock.GetComponent<ImpactBreak>().opens=flap;
        // ③ ice lock high on the cabinet, a metal conductor rod touching it (Heat + Magnet induction)
        var rod=Box("Conductor rod (metal)",new Vector3(-18.6f,6.1f,34.6f),new Vector3(.25f,1.0f,.25f),metal); WM(rod,MaterialKind.Metal,true);
        var iceLock=WM(Box("Ice lock",new Vector3(-18.6f,6.95f,34.6f),new Vector3(.9f,.8f,.9f),ice),MaterialKind.Ice);
        Box("Rod bracket",new Vector3(-19.3f,5.6f,34.6f),new Vector3(.9f,.12f,.4f),dark,null,false);
        var gear=Prim(PrimitiveType.Cylinder,"Spare gear (3 kg, magnetic)",new Vector3(-13,.2f,37.6f),new Vector3(.8f,.2f,.8f),metal);
        Body(gear,3); gear.AddComponent<BlobMagnetic>(); var gItem=Item(gear,"assembly_gear",new Vector3(-13,.3f,37.6f));
        var f6=Puzzle("f6_assembly","ห้องประกอบ: ตู้ชิ้นส่วน","ปลดตู้เก็บชิ้นส่วน: เชื่อมรางขับ ยิงตัวล็อกสนิม หรือทำให้ก้านโลหะร้อนจนน้ำแข็งละลาย","ตู้เปิดแล้ว — แกนพลังโรงงานเป็นของคุณ",
            new Bounds(new Vector3(-14.9f,3,34.6f),new Vector3(9.7f,6,8.7f)),new MonoBehaviour[]{weld,flap,State("ice-lock-melted",MaterialGoal.Melted,iceLock)},new[]{gItem},new[]{rustM,iceLock});
        var core=Part("part_factory_core","แกนพลังโรงงาน",new Vector3(-17,1.4f,34.6f),false);
        React(f6,new[]{core.gameObject},new[]{doorL,doorR},null,default,new[]{Lamp("Assembly lamp",new Vector3(-15,7,34.5f),true)});
        ResetPad(new Vector3(-12.2f,0,34.8f),f6);
        Source("Assembly",BlobProperty.Heat,new Vector3(-11.4f,0,31.2f));
        Source("Assembly",BlobProperty.Electric,new Vector3(-18.8f,0,38.2f));
        Source("Assembly",BlobProperty.Magnet,new Vector3(-11.4f,0,38.1f));
        Source("Assembly",BlobProperty.Pressure,new Vector3(-16f,0,31f));
    }
    // ---------------- hidden spots (the combos the main path does not feature) ----------------
    static void Secrets() {
        // Pressure + Bounce: wind ball round the corner of the receiving-hall alcove -> part_storage
        Wall("Alcove west wall",new Vector2(15.6f,35.5f),new Vector2(15.6f,39),3.2f,wallM,.35f,Door(38.05f,1.8f,2.6f));
        Wall("Cubby west wall",new Vector2(15.6f,33f),new Vector2(15.6f,35.5f),3.2f,wallM,.35f);
        Wall("Cubby south wall",new Vector2(15.6f,33f),new Vector2(X1,33f),3.2f,wallM,.35f);
        Wall("Cubby/alcove divider",new Vector2(15.6f,35.5f),new Vector2(X1,35.5f),3.2f,wallM,.35f,Door(18.6f,1.6f,2.4f));
        Box("Alcove roof",new Vector3(17.7f,3.3f,36f),new Vector3(4.2f,.2f,6f),teal);
        var cubbyDoor=Box("Alcove cubby door",new Vector3(18.6f,1.2f,35.5f),new Vector3(1.6f,2.4f,.3f),coral);
        var btn=Block("Round-the-corner button",new Vector3(19.4f,1.0f,36.4f),new Vector3(.3f,.6f,.5f),yellow);
        var bLock=Lock(btn,"pressure+bounce","wind-ball-button");
        Box("Arrow paint",new Vector3(15.38f,1.6f,37f),new Vector3(.02f,.25f,1.4f),yellow,null,false);
        var sStorage=Puzzle("s_storage","ช่องเก็บของหลังมุม","","",new Bounds(new Vector3(17.5f,1.5f,36.5f),new Vector3(5,3,5)),new MonoBehaviour[]{bLock},secret:true);
        var pStorage=Part("part_storage","ถังเชื้อเพลิงยาน",new Vector3(17.7f,.7f,34.2f),true);
        React(sStorage,null,new[]{cubbyDoor});
        // Water + Bounce: sprout on the Big Press ledge behind a screen -> vine ramp up to part_cold_room
        var sprout=WM(Cyl("Ledge sprout pot",new Vector3(0,6.75f-PressDrop,60.6f),.35f,.5f,plant),MaterialKind.Plant,true);
        foreach(var a in new[]{0f,90f,180f,270f}) { var sc=Box("Ledge screen",new Vector3(0,7.1f-PressDrop,60.6f),new Vector3(1.6f,.9f,.1f),cream,null,false); sc.transform.rotation=Quaternion.Euler(0,a,0); sc.transform.position+=sc.transform.forward*.8f; }
        var ramp=Box("Vine ramp",new Vector3(0,3.4f-PressDrop,54.68f),new Vector3(1.6f,.3f,10f),Own("FI_Vine",new Color(.45f,.7f,.4f)),null,true,new Vector3(-42,0,0));
        ramp.SetActive(false); var vb=sprout.gameObject.AddComponent<VineBridge>(); vb.seed=sprout; vb.bridge=ramp;
        Part("part_cold_room","ตัวทำความเย็นยาน",new Vector3(1.1f,7.25f-PressDrop,59.6f),true);
        // Bounce + Heat: oven behind the crane-room wall, only a lobbed fireball lights it -> part_beam_ice
        Wall("Oven enclosure W",new Vector2(15.2f,39.2f),new Vector2(15.2f,41.6f),1.1f,wallM,.35f); Wall("Oven enclosure N",new Vector2(15.2f,41.6f),new Vector2(X1,41.6f),1.1f,wallM,.35f);
        var oven=Block("Toy oven (fireball only)",new Vector3(18.2f,0,40.3f),new Vector3(1.4f,1.2f,1.4f),coral);
        var ovenLock=Lock(oven,"bounce+heat","fireball-oven");
        Ball("Oven window (dark)",new Vector3(18.2f,.7f,41.05f),.3f,dark);
        var ovenFire=Ball("Oven fire",new Vector3(18.2f,1.4f,40.3f),.45f,lampWarm); ovenFire.SetActive(false);
        var sOven=Puzzle("s_beam_ice","เตาหลังกำแพง","","",new Bounds(new Vector3(17.5f,1.5f,40.4f),new Vector3(5,3,3)),new MonoBehaviour[]{ovenLock},secret:true);
        var pOven=Part("part_beam_ice","คริสตัลนำทางยาน",new Vector3(14.2f,.7f,40.4f),false);
        React(sOven,new[]{pOven.gameObject,ovenFire});
        // Pressure + Heat: wooden barricade 7 m up (beyond the plain heat cone); the paper stock beside it must not burn
        var barricade=WM(Box("High niche wooden barricade",new Vector3(-5.6f,7f,52.2f),new Vector3(.3f,1.2f,1.4f),wood),MaterialKind.Wood);
        var stock=WM(Box("Paper stock shelf (keep it safe)",new Vector3(-5.6f,7f,54.3f),new Vector3(.3f,1f,1.2f),straw),MaterialKind.Straw);
        Label("ระวังไฟลามกองกระดาษ!",new Vector3(-5.55f,5.9f,54.3f),90,.08f);
        var sFlame=Puzzle("s_factory_flame","ช่องไม้ด้านบน","","",new Bounds(new Vector3(-3,4,53),new Vector3(6,8,6)),
            new MonoBehaviour[]{State("barricade-burned",MaterialGoal.Burned,barricade),State("stock-safe",MaterialGoal.Intact,stock)},null,new[]{barricade,stock},ConditionMode.All,true);
        var pFlame=Part("part_shed_roof","เสาอากาศยาน",new Vector3(-4.6f,.7f,52.2f),false);
        React(sFlame,new[]{pFlame.gameObject});
        ResetPad(new Vector3(-3.2f,0,62.9f),sFlame);   // clear of the turbine-room breach
        // Electric + Heat: fuse in the turbine room -> maintenance cubby with part_fuse
        var fuseBox=Block("Turbine fuse box",new Vector3(-19.4f,1.2f,58.4f),new Vector3(.5f,1,1),tealDark);
        var fuse=Box("Fuse (short-circuit lock)",new Vector3(-19.1f,1.7f,58.4f),new Vector3(.15f,.35f,.25f),wax);
        var fLock=Lock(fuse,"electric+heat","fuse");
        var sFuse=Puzzle("s_fuse","ตู้ฟิวส์ห้องกังหัน","","",new Bounds(new Vector3(-18,1.5f,58.4f),new Vector3(4,3,3)),new MonoBehaviour[]{fLock},secret:true);
        var pFuse=Part("part_fuse","ฟิวส์พลังงานยาน",new Vector3(-18.3f,.6f,59.9f),false);
        React(sFuse,new[]{pFuse.gameObject},new[]{fuse});
        // Water + Electric: submerged terminal in a small cooling pool -> crawl hatch shortcut back to the receiving hall
        var pool=Box("Cooling pool (electric water only)",new Vector3(-14.6f,.15f,41.2f),new Vector3(3,.3f,2.6f),Own("FI_DeepWater",new Color(.10f,.30f,.52f),false,.92f));
        pool.GetComponent<Collider>().isTrigger=true; var pLock=Lock(pool,"water+electric","shock-pool");
        Block("Submerged terminal",new Vector3(-14.6f,0,41.2f),new Vector3(.5f,.25f,.5f),dark,null,false);
        var crawlHatch=Box("Crawl hatch to the receiving hall",new Vector3(-8,.85f,39),new Vector3(1.6f,1.7f,.45f),coral);
        var sPool=Puzzle("s_factory_shockpool","บ่อหล่อเย็นไฟฟ้า","","ช่องลอดกลับโถงรับของเปิดแล้ว",new Bounds(new Vector3(-15,1.5f,40.5f),new Vector3(10,3,3)),new MonoBehaviour[]{pLock},secret:true);
        React(sPool,null,new[]{crawlHatch});
        Label("ขั้วรับไฟใต้น้ำ",new Vector3(-14.6f,1.4f,39.4f),0,.09f);
    }
    // ---------------- dressing with the approved WorldPolish models ----------------
    static Dictionary<string,Material> pol;
    static GameObject Dress(Transform parent,string model,Vector3 basePoint,float height) {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Base+"Models/WorldPolish/"+model+".fbx"); if(!prefab) return null;
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent.gameObject.scene); obj.name="World polish "+model;
        obj.transform.SetParent(parent,true);
        foreach(var r in obj.GetComponentsInChildren<Renderer>(true)) {
            var a=r.sharedMaterials; for(int i=0;i<a.Length;i++) { string n=a[i]?a[i].name:""; int at=n.IndexOf("POL_"); string role=at>=0?n.Substring(at+4).Split('.')[0]:"Metal"; a[i]=Pol(role); } r.sharedMaterials=a;
        }
        var rs=obj.GetComponentsInChildren<Renderer>(true); var b=rs[0].bounds; foreach(var r in rs) b.Encapsulate(r.bounds);
        obj.transform.localScale*=height/Mathf.Max(.001f,b.size.y);
        rs=obj.GetComponentsInChildren<Renderer>(true); b=rs[0].bounds; foreach(var r in rs) b.Encapsulate(r.bounds);
        obj.transform.position+=basePoint-new Vector3(b.center.x,b.min.y,b.center.z);
        foreach(var t in obj.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=2;
        foreach(var r in obj.GetComponentsInChildren<Renderer>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        return obj;
    }
    public static void DressAll(Result r) {
        foreach(var s in r.sources) {
            if(Appliance(s)) continue;
            var full=s.fullVisual; var fr=full.GetComponent<Renderer>(); var b=fr.bounds;
            var art=Dress(s.transform,"Source_"+s.property,new Vector3(b.center.x,b.min.y,b.center.z),b.size.y); if(!art) continue;
            fr.enabled=false; s.highlightVisual=art.transform;
            var fx=s.GetComponent<AbsorbableVisual>(); if(fx) fx.renderers=art.GetComponentsInChildren<Renderer>(true);
            EditorUtility.SetDirty(s); if(fx) EditorUtility.SetDirty(fx);
        }
        foreach(var m in root.GetComponentsInChildren<WorldMaterial>(true)) {
            if(!m.name.Contains("(machine")) continue;
            var c=m.GetComponent<Collider>();
            var art=Kit(m.transform,"FI_Motor",new Vector3(c.bounds.center.x,c.bounds.min.y,c.bounds.center.z),0,c.bounds.size.y/1.1f); if(!art) continue;
            m.GetComponent<Renderer>().enabled=false; m.visuals=art.GetComponentsInChildren<Renderer>(true); EditorUtility.SetDirty(m);
        }
        foreach(var p in r.parts) {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Base+"Models/PROP_Gear.fbx"); if(!prefab||!p.visual) continue;
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,p.gameObject.scene); obj.name="World polish ship part"; obj.transform.SetParent(p.transform,true);
            obj.transform.position=p.visual.position; obj.transform.localScale=Vector3.one*.55f;
            foreach(var rr in obj.GetComponentsInChildren<Renderer>(true)) { rr.sharedMaterial=Pol("Yellow"); PrefabUtility.RecordPrefabInstancePropertyModifications(rr); }
            foreach(var rr in p.visual.GetComponentsInChildren<Renderer>(true)) rr.enabled=false;
            p.visual=obj.transform; EditorUtility.SetDirty(p);
        }
    }
    // ---------------- disguised sources (ArtSource/Blender/build_factory_appliances.py) ----------------
    // Kirby-style: every absorbable is an everyday factory appliance of its element (owner brief 2026-10-24).
    // Full model while available, drained model while it recovers (ApplianceState); faint shimmer stays.
    static readonly Dictionary<string,string> applianceFor=new Dictionary<string,string>{
        {"Receiving Magnet","ToolCart"},{"Cooling room Magnet","MetalLocker"},{"Crane room Magnet","ToolCart"},{"Assembly Magnet","MetalLocker"},
        {"Receiving Pressure","FireExtinguisher"},{"Crane room Pressure","AirCompressor"},{"Assembly Pressure","FireExtinguisher"},{"Turbine room Pressure","AirCompressor"},
        {"Central Bounce","SpringHorse"},{"Boiler room Bounce","TireStack"},
        {"Receiving Water","WaterCooler"},{"Boiler room Water","WaterBarrel"},{"Cooling room Water","LeakyPipe"},
        {"Turbine room Electric","ElectricPanel"},{"Assembly Electric","BatteryBank"},{"Central Electric","ArcadeMachine"},{"Boiler room Electric","ElectricPanel"},
        {"Receiving Heat","Heater"},{"Crane room Heat","CharcoalStove"},{"Boiler room Heat","Grill"},{"Turbine room Heat","CharcoalStove"},{"Assembly Heat","Heater"}};
    // the appliance faces into its room
    static readonly Dictionary<string,Vector2> roomCentre=new Dictionary<string,Vector2>{
        {"Receiving",new Vector2(4,34)},{"Central",new Vector2(0,50)},{"Crane room",new Vector2(13,45)},{"Boiler room",new Vector2(12,58)},
        {"Cooling room",new Vector2(-13,41.5f)},{"Turbine room",new Vector2(-13,60)},{"Assembly",new Vector2(-15,34.5f)}};
    static bool Appliance(BlobInteractable s) {
        string key=s.name.Replace(" source",""); if(!applianceFor.TryGetValue(key,out var model)) return false;
        string room=key.Substring(0,key.LastIndexOf(' '));
        var fr=s.fullVisual.GetComponent<Renderer>(); var b=fr.bounds; Vector3 at=new Vector3(b.center.x,b.min.y,b.center.z);
        Vector2 c=roomCentre[room]; float yaw=Mathf.Atan2(c.x-at.x,c.y-at.z)*Mathf.Rad2Deg+180;   // model front is -z
        var holder=new GameObject("Appliance "+model); holder.transform.SetParent(s.transform,false); holder.transform.position=at;
        var full=Kit(holder.transform,"FI_Ap_"+model,at,yaw); var drained=Kit(holder.transform,"FI_Ap_"+model+"_Drained",at,yaw);
        if(!full||!drained) { Object.DestroyImmediate(holder); return false; }
        drained.SetActive(false);
        fr.enabled=false; s.highlightVisual=holder.transform;
        var st=s.GetComponent<ApplianceState>()??s.gameObject.AddComponent<ApplianceState>(); st.source=s; st.fullArt=full; st.drainedArt=drained;
        var fx=s.GetComponent<AbsorbableVisual>();
        if(fx) { var rs=new List<Renderer>(full.GetComponentsInChildren<Renderer>(true)); rs.AddRange(drained.GetComponentsInChildren<Renderer>(true)); fx.renderers=rs.ToArray(); fx.greyWhenDrained=false; EditorUtility.SetDirty(fx); }
        EditorUtility.SetDirty(s); EditorUtility.SetDirty(st);
        return true;
    }
    // ---------------- abandoned-factory ruins (ArtSource/Blender/build_factory_ruins.py) ----------------
    // Heavy ruin that changes routes: a fallen girder over the cooling channel (P4 4th way: Bounce from its tip) and a
    // breach from the central hall into the turbine room sealed by rotten planks (Heat burns them). The rest is mess:
    // rubble that blocks parts of the floor, hanging ceiling debris, toppled shelves, cracks, junk, overgrowth.
    static GameObject Ruin(string model,Vector3 pos,float yaw=0,float scale=1,Vector3? block=null) {
        var art=Kit(root,"FI_Ru_"+model,pos,yaw,scale);
        if(block.HasValue) { var c=Block("Ruin collider: "+model,pos,block.Value,wallM); c.transform.rotation=Quaternion.Euler(0,yaw,0); c.GetComponent<Renderer>().enabled=false; }
        return art;
    }
    static void Stain(Vector3 from,Vector3 dir) {
        if(!Physics.Raycast(from,dir,out RaycastHit h,12f,~(1<<2),QueryTriggerInteraction.Ignore)) return;
        var n=h.normal; n.y=0; n.Normalize(); float yaw=Mathf.Atan2(-n.x,-n.z)*Mathf.Rad2Deg;
        Kit(root,"FI_Ru_WallStain",new Vector3(h.point.x,0,h.point.z)+n*.03f,yaw);
    }
    public static void RuinAll() {
        Physics.SyncTransforms();
        // route: fallen girder stub, walkable, 4.1 m short of the north bank (a plain Bounce from its tip clears it)
        var girder=Block("Fallen girder (walkable)",new Vector3(-18.6f,-.1f,46.7f),new Vector3(.8f,.1f,5.4f),metal); HideBox(girder.transform);
        Kit(root,"FI_Ru_Girder",new Vector3(-18.6f,0,44f));
        // route: breach central hall -> turbine room behind rotten planks (Wood: Heat or the flamethrower burns them)
        Kit(root,"FI_Ru_WallBreach",new Vector3(-6,0,58),90);
        var planks=WM(Box("Breach plank barricade (burns)",new Vector3(-5.75f,1.2f,58),new Vector3(.25f,2.4f,2.2f),wood),MaterialKind.Wood);
        var pa=Kit(root,"FI_Ru_PlankBarricade",new Vector3(-6.1f,0,58),-90); HideBox(planks.transform);   // under root: a rotated child of the thin box would be stretched
        if(pa) { planks.visuals=pa.GetComponentsInChildren<Renderer>(true); EditorUtility.SetDirty(planks); }
        // mess: receiving hall
        Ruin("RubblePile",new Vector3(11.6f,0,33.6f),20,.8f,new Vector3(2.3f,.9f,1.8f));
        Ruin("CeilingDebris",new Vector3(-1,8,33.5f),40); Ruin("FloorCracks",new Vector3(2,0,33),15); Ruin("ToyJunk",new Vector3(-8.7f,0,34.4f),0);
        Ruin("Overgrowth",new Vector3(12.2f,0,38.7f),180); Ruin("Puddles",new Vector3(8.4f,0,31.6f));
        // central hall
        Ruin("RubblePile",new Vector3(-3.4f,0,44.6f),-30,.75f,new Vector3(2.1f,.85f,1.7f));
        Ruin("FallenTruss",new Vector3(-5.2f,0,64.4f),90,1,new Vector3(.9f,.9f,8f));
        Ruin("CeilingDebris",new Vector3(2.6f,8,50.2f),0); Ruin("RubbleSmall",new Vector3(2.6f,0,50.6f));
        Ruin("FloorCracks",new Vector3(-2,0,47),60); Ruin("Puddles",new Vector3(3,0,55.2f)); Ruin("ToyJunk",new Vector3(2.8f,0,63.7f),90);
        Ruin("ToppledShelf",new Vector3(-4.8f,0,64.6f),90,.9f);
        // crane room
        Ruin("CeilingDebris",new Vector3(11,8,41.6f),120); Ruin("FloorCracks",new Vector3(11,0,42.6f),0); Ruin("Puddles",new Vector3(15,0,49.8f));
        Ruin("Overgrowth",new Vector3(19.7f,0,44.2f),-90);
        // boiler room
        Ruin("RubbleSmall",new Vector3(7.2f,0,64.9f)); Ruin("ToyJunk",new Vector3(12.6f,0,65.1f),30); Ruin("Puddles",new Vector3(9,0,60.6f));
        Ruin("CeilingDebris",new Vector3(17.5f,8,53.2f),200);
        // cooling room
        Ruin("CeilingDebris",new Vector3(-12,8,49),70); Ruin("Overgrowth",new Vector3(-19.7f,0,41.2f),90); Ruin("FloorCracks",new Vector3(-18.4f,0,40.6f),30);
        // turbine room
        Ruin("RubblePile",new Vector3(-9.6f,0,58.8f),10,.7f,new Vector3(1.9f,.8f,1.6f)); Ruin("RubbleSmall",new Vector3(-7.1f,0,56.3f));
        Ruin("ToppledShelf",new Vector3(-10.6f,0,65.1f),0,.9f); Ruin("CeilingDebris",new Vector3(-16,8,62),300);
        // more wreckage: roof beams hanging from the ceiling, scattered debris, a fallen pipe, extra rubble
        foreach(var c in new[]{new Vector3(6,8,33.5f),new Vector3(-2.5f,8,58),new Vector3(16,8,48.5f),new Vector3(-11,8,61),new Vector3(-9,8,46)}) Ruin("HangingBeam",c,Random.Range(-20f,20f));
        foreach(var c in new[]{new Vector3(13,0,36.5f),new Vector3(-3.8f,0,59.6f),new Vector3(2.8f,0,46.5f),new Vector3(8.2f,0,49.4f),new Vector3(-17.5f,0,62),new Vector3(-16.5f,0,40.4f),new Vector3(17.5f,0,64.6f),new Vector3(-17f,0,37.8f)}) Ruin("DebrisScatter",c,Random.Range(0,360f));
        Ruin("FallenPipe",new Vector3(17.4f,0,31),90,1,new Vector3(3.4f,.4f,.45f)); Ruin("FallenPipe",new Vector3(-18.8f,0,63));
        Ruin("RubblePile",new Vector3(7.2f,0,60.8f),-15,.6f,new Vector3(1.7f,.7f,1.4f));
        // grime: water streaks, peeling paint and cracks on the walls (placed on the wall face found by a ray)
        foreach(var r in new[]{new[]{new Vector3(12,1.5f,34),Vector3.forward},new[]{new Vector3(-7,1.5f,33),Vector3.back},new[]{new Vector3(9,1.5f,33),Vector3.back},
            new[]{new Vector3(-3,1.5f,50),Vector3.left},new[]{new Vector3(3,1.5f,53),Vector3.right},new[]{new Vector3(13,1.5f,44),Vector3.right},new[]{new Vector3(10.2f,1.5f,48),Vector3.forward},
            new[]{new Vector3(12,1.5f,62),Vector3.forward},new[]{new Vector3(16,1.5f,62),Vector3.right},new[]{new Vector3(-13,1.5f,41.5f),Vector3.back},new[]{new Vector3(-10,1.5f,62),Vector3.forward},
            new[]{new Vector3(-8,1.5f,55),Vector3.right},new[]{new Vector3(-14,1.5f,37),Vector3.forward},new[]{new Vector3(-17,1.5f,36),Vector3.left}}) Stain(r[0],r[1]);
        // assembly room
        Ruin("CeilingDebris",new Vector3(-14,8,36),10); Ruin("FloorCracks",new Vector3(-15,0,37.9f),45); Ruin("Overgrowth",new Vector3(-14.2f,0,30.45f),0);
    }
    // ---------------- Blender interior kit (ArtSource/Blender/build_factory_interior.py -> Models/FactoryInterior) ----------------
    // Models are authored in metres at 1:1. Kit() parents a model under 'parent', puts its origin at 'worldPos' with a
    // yaw, cancels the parent's (possibly non-uniform) scale and optionally applies a uniform extra scale.
    static GameObject Kit(Transform parent,string model,Vector3 worldPos,float yaw=0,float scale=1,Vector3? stretch=null) {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Base+"Models/FactoryInterior/"+model+".fbx"); if(!prefab) { Debug.LogWarning("FACTORY_KIT missing "+model); return null; }
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent.gameObject.scene); obj.name="Kit "+model;
        obj.transform.SetParent(parent,false);
        obj.transform.rotation=Quaternion.Euler(0,yaw,0); obj.transform.position=worldPos;
        var ls=parent.lossyScale; var st=stretch??Vector3.one;
        obj.transform.localScale=new Vector3(st.x*scale/Mathf.Max(.0001f,ls.x),st.y*scale/Mathf.Max(.0001f,ls.y),st.z*scale/Mathf.Max(.0001f,ls.z));
        foreach(var r in obj.GetComponentsInChildren<Renderer>(true)) {
            var a=r.sharedMaterials; for(int i=0;i<a.Length;i++) { string n=a[i]?a[i].name:""; int at=n.IndexOf("POL_"); a[i]=Pol(at>=0?n.Substring(at+4).Split(' ')[0].Split('.')[0]:"Metal"); } r.sharedMaterials=a;
            r.gameObject.layer=2; PrefabUtility.RecordPrefabInstancePropertyModifications(r); PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        return obj;
    }
    static IEnumerable<Transform> Named(string name) { foreach(var t in root.GetComponentsInChildren<Transform>(true)) if(t.name==name) yield return t; }
    static void HideBox(Transform t) { foreach(var r in t.GetComponents<Renderer>()) r.enabled=false; }
    static void HideAll(string name) { foreach(var t in new List<Transform>(Named(name))) HideBox(t); }
    // Swap the greybox look of a body for a kit model that moves with it (doors, raft, lift, junk). Returns the model.
    static GameObject Skin(Transform t,string model,float yaw=0,Vector3? stretch=null) {
        var art=Kit(t,model,t.position,yaw,1,stretch); if(!art) return null; HideBox(t);
        var m=t.GetComponent<WorldMaterial>(); if(m) { m.visuals=art.GetComponentsInChildren<Renderer>(true); EditorUtility.SetDirty(m); }
        return art;
    }
    public static void KitAll() {
        // landmarks
        var press=root.Find("Big Toy Press (landmark)");
        if(press) { foreach(Transform c in press) if(c.name!="Press eye") HideBox(c); Kit(press,"FI_BigToyPress",new Vector3(0,-PressDrop,60)); }
        foreach(var t in Named("Giant boiler")) { HideBox(t); Kit(root,"FI_GiantBoiler",new Vector3(15.5f,0,56.5f)); }
        HideAll("Giant boiler dome");
        var turbine=root.Find("Round vent turbine");
        if(turbine) {
            foreach(Transform c in turbine) if(c.name=="Turbine ring") HideBox(c);
            Kit(turbine,"FI_TurbineHousing",turbine.position,90);
            var rotor=turbine.Find("Rotor");
            if(rotor) { foreach(Transform b in rotor) HideBox(b); var ra=Kit(rotor,"FI_TurbineRotor",rotor.position); if(ra) ra.transform.localRotation=Quaternion.identity; }
        }
        foreach(var n in new[]{"Crane rail W","Crane rail E","Crane bridge","Crane trolley","Crane hook cable","Crane hook"}) HideAll(n);
        Kit(root,"FI_OverheadCrane",new Vector3(13.1f,0,45));
        foreach(var n in new[]{"Chute divider","Chute roof","Chute hopper"}) HideAll(n);
        Kit(root,"FI_SortingChutes",new Vector3(0,0,40));
        foreach(var t in Named("Parts cabinet")) { HideBox(t); Kit(root,"FI_PartsCabinet",new Vector3(-18.6f,0,34.6f)); }
        foreach(var n in new[]{"Cabinet door L","Cabinet door R"}) foreach(var t in new List<Transform>(Named(n))) Skin(t,"FI_CabinetDoor");
        foreach(var t in Named("Assembly furnace (landmark)")) { HideBox(t); Kit(root,"FI_Furnace",new Vector3(-13,0,31.8f),180); }
        HideAll("Furnace window");
        // puzzle bodies and doors (the model follows every move of its body)
        foreach(var t in new List<Transform>(Named("Safety door (vault)"))) { foreach(Transform c in t) HideBox(c); Skin(t,"FI_SafetyDoor"); }
        foreach(var t in new List<Transform>(Named("Rust hatch door"))) Skin(t,"FI_Hatch");
        foreach(var t in new List<Transform>(Named("Wax crawl hatch"))) Skin(t,"FI_Hatch",0,new Vector3(1,1.6f/2.2f,1));
        foreach(var t in new List<Transform>(Named("Rusty bolt (breaks on impact)"))) Skin(t,"FI_RustBolt",0);      // faces the crane room (-z)
        foreach(var t in new List<Transform>(Named("Rusty cabinet lock"))) Skin(t,"FI_RustBolt",-90);              // faces the vault room (+x)
        foreach(var t in new List<Transform>(Named("Wax seal"))) Skin(t,"FI_WaxSeal");
        foreach(var t in new List<Transform>(Named("Boiler cargo lift"))) Skin(t,"FI_LiftDeck");
        foreach(var t in new List<Transform>(Named("Cooling raft (20 kg, magnetic)"))) Skin(t,"FI_Raft");
        foreach(var t in new List<Transform>(Named("Service bridge deck"))) Skin(t,"FI_ServiceBridge");
        foreach(var t in new List<Transform>(Named("Permanent catwalk (after crossing)"))) Skin(t,"FI_ServiceBridge",0,new Vector3(1.6f/2.2f,1,1));
        foreach(var t in new List<Transform>(Named("Chute A scrap plate (4 kg)"))) Skin(t,"FI_ScrapPlate");
        foreach(var t in new List<Transform>(Named("Chute B foam toy box (1 kg)"))) Skin(t,"FI_FoamToyBox");
        foreach(var t in new List<Transform>(Named("Chute C heavy crate (16 kg)"))) Skin(t,"FI_HeavyCrate");
        foreach(var t in new List<Transform>(Named("Chute D cardboard bundle (14 kg)"))) Skin(t,"FI_CardboardBundle");
        foreach(var t in new List<Transform>(Named("Crane counterweight (6 kg, magnetic)"))) Skin(t,"FI_Counterweight");
        foreach(var t in new List<Transform>(Named("Old press machine (30 kg, magnetic)"))) Skin(t,"FI_OldPressMachine");
        // dripping pipe, lamps, sprouts
        foreach(var t in new List<Transform>(Named("Dripping pipe"))) { HideBox(t); Kit(root,"FI_PipeRun",t.position,90); }
        foreach(var t in new List<Transform>(root.GetComponentsInChildren<Transform>(true)))
            if(t.name.EndsWith(" shade") || t.name.EndsWith(" bulb")) HideBox(t);
        foreach(var l in root.GetComponentsInChildren<Light>(true)) Kit(root,"FI_CeilingLamp",l.transform.position+Vector3.up*.3f);
        foreach(var t in new List<Transform>(Named("Floor sprout"))) { HideBox(t); Kit(root,"FI_FloorSprouts",new Vector3(t.position.x,0,t.position.z),Random.Range(0,360)); }
        // set dressing kept clear of paths, chutes, landing spots and aim lines
        foreach(var p in new[]{new Vector3(-5.4f,0,39.7f),new Vector3(-5.4f,0,65.1f),new Vector3(-6.6f,0,65.1f),new Vector3(-19.1f,0,30.9f)}) Kit(root,"FI_Pillar",p);
        foreach(var p in new[]{new Vector3(-18.8f,0,65f),new Vector3(-4.8f,0,64.4f),new Vector3(19f,0,38.6f),new Vector3(10.6f,0,64.9f)}) Kit(root,"FI_DustPile",p,Random.Range(0,360));
        Kit(root,"FI_BrokenToyBear",new Vector3(12.6f,0,31.1f),200); Kit(root,"FI_BrokenToyBear",new Vector3(-19f,0,64.9f),120);
        Kit(root,"FI_OldCrates",new Vector3(10.8f,0,39.8f),15); Kit(root,"FI_OldCrates",new Vector3(-19f,0,53.9f),70);
        foreach(var p in new[]{new Vector3(-5.6f,6.6f,58f),new Vector3(5.6f,7.1f,42f)}) Kit(root,"FI_PipeRun",p,90);
    }
    [MenuItem("Tools/OOPS Planet/V2/Run Factory Interior Test (2 rounds)")]
    public static void RunTest() {
        if(Object.FindFirstObjectByType<FactorySceneLink>()) { Debug.LogWarning("Legacy factory test uses compact coordinates. Run FactoryExpansionTest in OopsPlanet_Factory with a separate test save slot.");return; }
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode in OopsPlanet_V2 first"); return; }
        var w=Object.FindFirstObjectByType<BlobWorld>();
        if(!w || !w.ship) { Debug.LogError("Not the V2 scene"); return; }
        if(!w.GetComponent<OopsFactoryInteriorTest>()) w.gameObject.AddComponent<OopsFactoryInteriorTest>();
    }
    // ---------------- apply to the current V2 scene (keeps everything outside the footprint) ----------------
    static bool Inside(Vector3 p) => p.x>X0+.15f && p.x<X1-.15f && p.z>Z0+.05f && p.z<Z1-.05f;
    [MenuItem("Tools/OOPS Planet/V2/Rebuild Factory Interior (keeps outside)")]
    public static void ApplyToScene() {
        if(Object.FindFirstObjectByType<FactorySceneLink>()) { Debug.LogWarning("Separate factory is installed. Use Tools/OOPS Planet/Factory Expansion/Build Separate Factory And Entrance.");return; }
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode first");
        var scene=EditorSceneManager.GetActiveScene();
        if(!scene.path.EndsWith("OopsPlanet_V2.unity")) throw new System.InvalidOperationException("Open OopsPlanet_V2 first");
        // backup outside Assets/
        string backupDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Backups")); Directory.CreateDirectory(backupDir);
        string backup=Path.Combine(backupDir,"OopsPlanet_V2_before_factory_interior_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity");
        EditorSceneManager.SaveScene(scene); File.Copy(Path.GetFullPath(scene.path),backup,true);
        var w=Object.FindFirstObjectByType<BlobWorld>(); var top=GameObject.Find("OOPS V2 GREYBOX");
        if(!w||!top||!w.ship) throw new System.InvalidOperationException("V2 scene objects missing");
        int removed=ApplyInPlace(w,top);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("FACTORY_INTERIOR_APPLIED removed="+removed+" puzzles="+R.puzzles.Count+" sources="+R.sources.Count+" parts="+R.parts.Count+" backup="+backup);
        OopsV2Builder.Validate();
    }
    // Shared by the menu above and by the full V2 rebuild: clear everything inside the footprint (except the area zone
    // and checkpoint 1), build the interior, dress it and reconnect the outside interfaces.
    public static int ApplyInPlace(BlobWorld w,GameObject top) {
        var keep=new HashSet<string>{"Area Factory","Checkpoint 1","Checkpoint pad 1"};
        var remove=new List<GameObject>();
        foreach(Transform t in top.transform) if(t.name=="FACTORY INTERIOR" || (Inside(t.position) && !keep.Contains(t.name))) remove.Add(t.gameObject);
        foreach(var g in remove) Object.DestroyImmediate(g);
        TrimArtInsideFootprint();
        var r=Build(w,top.transform,w.uiFont,w.ship);
        Random.InitState(2410);
        KitAll();
        RuinAll();
        DressAll(r);
        Rewire(w,r);
        return remove.Count;
    }
    // The merged soft-world garden / accent meshes span the whole map and had trees and stripes inside the old open-air
    // factory. This scene now uses trimmed copies without the triangles inside the footprint; the source FBX and
    // everything outside the footprint are unchanged. Running it again finds nothing left to trim.
    static void TrimArtInsideFootprint() {
        string dir=Base+"Models/FactoryInterior";
        if(!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(Base+"Models","FactoryInterior");
        foreach(var rootName in new[]{"Soft World Garden","World polish accents"}) {
            var top=GameObject.Find(rootName); if(!top) continue;
            foreach(var mf in top.GetComponentsInChildren<MeshFilter>(true)) {
                var src=mf.sharedMesh; if(!src) continue;
                var verts=src.vertices; var l2w=mf.transform.localToWorldMatrix;
                var subs=new List<int[]>(); bool changed=false;
                for(int sm=0;sm<src.subMeshCount;sm++) {
                    var tri=src.GetTriangles(sm); var keep=new List<int>(tri.Length);
                    for(int i=0;i<tri.Length;i+=3) {
                        Vector3 c=(l2w.MultiplyPoint3x4(verts[tri[i]])+l2w.MultiplyPoint3x4(verts[tri[i+1]])+l2w.MultiplyPoint3x4(verts[tri[i+2]]))/3f;
                        if(Inside(c)) { changed=true; continue; }
                        keep.Add(tri[i]); keep.Add(tri[i+1]); keep.Add(tri[i+2]);
                    }
                    subs.Add(keep.ToArray());
                }
                if(!changed) continue;
                var m=Object.Instantiate(src); m.name=src.name+" (factory trimmed)";
                for(int sm=0;sm<subs.Count;sm++) m.SetTriangles(subs[sm],sm);
                string path=dir+"/Trim_"+rootName.Replace(" ","_")+"_"+mf.name.Replace(" ","_")+".asset";
                if(AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(m,path);
                mf.sharedMesh=m; PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
                var mc=mf.GetComponent<MeshCollider>(); if(mc) { mc.sharedMesh=m; PrefabUtility.RecordPrefabInstancePropertyModifications(mc); }
            }
        }
    }
    public static void Rewire(BlobWorld w,Result r) {
        var pz=new List<PuzzleObjective>(); foreach(var p in w.puzzles) if(p) pz.Add(p); pz.AddRange(r.puzzles); w.puzzles=pz.ToArray();
        var src=new List<BlobInteractable>(); foreach(var s in w.sources) if(s) src.Add(s); src.AddRange(r.sources); w.sources=src.ToArray();
        var zs=new List<BlobInteractable>(); foreach(var z in w.zones) if(z) zs.Add(z); w.zones=zs.ToArray();
        var parts=new List<ShipPart>(); foreach(var p in w.ship.parts) if(p) parts.Add(p); parts.AddRange(r.parts); w.ship.parts=parts.ToArray();
        var gates=new List<ZoneGate>(); foreach(var g in w.zoneGates) if(g) gates.Add(g); gates.AddRange(r.gates); w.zoneGates=gates.ToArray();
        foreach(var g in gates) {
            if(g.zoneId=="rice") { g.requires=new[]{r.f1}; g.requiresAnyOf=new[]{r.f2,r.f3}; g.anyCount=1; }
            if(g.zoneId=="shortcut_conveyor") { g.requires=new[]{r.weld}; }
            EditorUtility.SetDirty(g);
        }
        if(w.ambience) {
            foreach(var z in w.ambience.zones) {
                if(z.name=="Factory") z.restoredWhen=new[]{r.f1,r.f2,r.f3};
                else if(z.restoredWhen!=null) for(int i=0;i<z.restoredWhen.Length;i++) if(!z.restoredWhen[i]) z.restoredWhen[i]=r.f1;
            }
            EditorUtility.SetDirty(w.ambience);
        }
        foreach(var z in w.zones) if(z && z.name=="Area Factory") { z.label="ZONE 01 FACTORY — โรงงานของเล่นเก่า: เปิดรางคัดแยกเข้าโถงกลาง แล้วเลือกปีกตะวันออก (เครน/หม้อต้ม) หรือตะวันตก (หล่อเย็น/กังหัน)"; z.size=new Vector3(40,8,36); EditorUtility.SetDirty(z); }
        EditorUtility.SetDirty(w); EditorUtility.SetDirty(w.ship);
    }
}
}
#endif
