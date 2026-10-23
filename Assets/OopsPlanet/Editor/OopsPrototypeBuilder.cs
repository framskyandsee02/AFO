#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet.Editor {
// P01 greybox: Hub + Zone 01 Factory (north) + Zone 02 Rice Fields (east). Movement/camera only; no powers or puzzles.
public static class OopsPrototypeBuilder {
    const string Root="Assets/OopsPlanet";
    const string ScenePath=Root+"/Scenes/OopsPlanet_Prototype01.unity";
    static Transform root;
    static Material grey,dark,light,green,white,black,glow,blobBody,factoryWall,rust,metal,wood,water,paddy,straw,rice;
    static List<BlobInteractable> zones;
    static Material Mat(string name,Color color,bool emission=false) {
        string path=Root+"/Materials/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat) return mat;
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");
        if(!shader)throw new System.Exception("URP Lit shader unavailable");
        mat=new Material(shader){name=name};
        mat.SetColor("_BaseColor",color); mat.SetFloat("_Smoothness",.18f);
        if(emission) { mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*1.5f); }
        AssetDatabase.CreateAsset(mat,path);
        return mat;
    }
    static GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null,bool collider=true) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
        go.transform.SetParent(parent?parent:root); go.transform.position=pos;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static GameObject Cylinder(string name,Vector3 pos,float radius,float height,Material mat,Transform parent=null) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name=name;
        go.transform.SetParent(parent?parent:root); go.transform.position=pos;go.transform.localScale=new Vector3(radius*2,height/2,radius*2);
        go.GetComponent<Renderer>().sharedMaterial=mat; return go;
    }
    static void Label(string text,Vector3 pos,float yaw=0,float size=.3f,Color? color=null) {
        var go=new GameObject("Sign "+text.Split('\n')[0]);go.transform.SetParent(root);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var tm=go.AddComponent<TextMesh>();tm.text=text;tm.characterSize=size*.45f;tm.fontSize=64;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;
        tm.color=color??new Color(.93f,.97f,1); tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<Renderer>().sharedMaterial=tm.font.material;
        var b=go.GetComponent<Renderer>().bounds;
        var backing=Box("Sign backing",b.center+go.transform.forward*.04f,Vector3.one,dark,go.transform,false);
        backing.transform.localRotation=Quaternion.identity;
        backing.transform.localScale=new Vector3(Mathf.Max(b.size.x,b.size.z)+.4f,b.size.y+.25f,.05f);
    }
    // Floor slab with 2 m grid lines for reading scale.
    static void Floor(string name,Vector3 center,float sizeX,float sizeZ,Material mat) {
        Box(name,center-Vector3.up*.5f,new Vector3(sizeX,1,sizeZ),mat);
        for(float x=-sizeX/2+2;x<sizeX/2;x+=2) Box("2m grid",center+new Vector3(x,.008f,0),new Vector3(.025f,.012f,sizeZ),light,null,false);
        for(float z=-sizeZ/2+2;z<sizeZ/2;z+=2) Box("2m grid",center+new Vector3(0,.009f,z),new Vector3(sizeX,.012f,.025f),light,null,false);
    }
    // Perimeter wall with an optional 6 m opening on one side (n/e/s/w).
    static void Walls(string zone,Vector3 c,float sx,float sz,string openings,float height=1.2f) {
        void Side(char side,Vector3 mid,bool alongX,float len) {
            if(openings.IndexOf(side)<0) { Box(zone+" wall "+side,mid,alongX?new Vector3(len,height,.5f):new Vector3(.5f,height,len),dark); return; }
            float seg=(len-6)/2;
            Vector3 dir=alongX?Vector3.right:Vector3.forward;
            Box(zone+" wall "+side+" A",mid-dir*(3+seg/2),alongX?new Vector3(seg,height,.5f):new Vector3(.5f,height,seg),dark);
            Box(zone+" wall "+side+" B",mid+dir*(3+seg/2),alongX?new Vector3(seg,height,.5f):new Vector3(.5f,height,seg),dark);
        }
        float y=height/2;
        Side('n',c+new Vector3(0,y,sz/2),true,sx); Side('s',c+new Vector3(0,y,-sz/2),true,sx);
        Side('e',c+new Vector3(sx/2,y,0),false,sz); Side('w',c+new Vector3(-sx/2,y,0),false,sz);
    }
    static BlobInteractable Zone(string name,BlobInteraction kind,Vector3 pos,Vector3 size,string label=null) {
        var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=pos;
        var z=go.AddComponent<BlobInteractable>();z.kind=kind;z.size=size;z.label=label;zones.Add(z);return z;
    }
    static void Checkpoint(int index,Vector3 p,float yaw) {
        var z=Zone("Checkpoint "+index,BlobInteraction.Checkpoint,p+Vector3.up,new Vector3(6,3,3));z.index=index;z.respawn=p+Vector3.up*.1f;
        var pad=Box("Checkpoint pad "+index,p+Vector3.up*.03f,new Vector3(4,.06f,2),green,null,false);
        pad.transform.rotation=Quaternion.Euler(0,yaw,0);
        z.transform.rotation=Quaternion.identity;
        if(Mathf.Abs(Mathf.DeltaAngle(yaw,90))<1) z.size=new Vector3(3,3,6);
    }
    static GameObject PropPrefab(string name,Material mat) {
        string path=Root+"/Prefabs/"+name+".prefab";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(existing) return existing;
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+name+".fbx");
        if(!model)throw new System.Exception("Blender export missing: "+name+".fbx");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(model);
        foreach(var mf in go.GetComponentsInChildren<MeshFilter>()) {
            mf.GetComponent<Renderer>().sharedMaterial=mat;
            if(!mf.GetComponent<Collider>()) mf.gameObject.AddComponent<BoxCollider>();
        }
        var prefab=PrefabUtility.SaveAsPrefabAsset(go,path);
        Object.DestroyImmediate(go);
        return prefab;
    }
    static GameObject Place(GameObject prefab,Vector3 pos,float yaw=0) {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);
        go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);return go;
    }
    [MenuItem("Tools/OOPS Planet/Create Prototype 01")]
    public static void Create() => Build(false);
    [MenuItem("Tools/OOPS Planet/Rebuild Prototype 01 (overwrites generated scene)")]
    public static void Rebuild() => Build(true);
    static void Build(bool overwrite) {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode before creating a scene");
        if(File.Exists(ScenePath) && !overwrite)throw new System.Exception("Prototype scene already exists; use Rebuild to regenerate it.");
        foreach(var d in new[]{"Scenes","Materials","Prefabs","Evidence"}) Directory.CreateDirectory(Root+"/"+d);
        AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        root=new GameObject("OOPS PROTOTYPE 01").transform;
        zones=new List<BlobInteractable>();
        grey=Mat("Hub concrete",new Color(.5f,.53f,.55f));dark=Mat("Structural charcoal",new Color(.15f,.2f,.25f));
        light=Mat("Grid lines",new Color(.62f,.66f,.69f));green=Mat("Checkpoint green",new Color(.17f,.8f,.38f),true);
        white=Mat("Eye white",new Color(.95f,.98f,1));black=Mat("Pupils",new Color(.02f,.025f,.035f));
        glow=Mat("BLOB core",new Color(1,.7f,.2f),true);blobBody=Mat("BLOB body",new Color(.63f,.73f,.78f));
        factoryWall=Mat("Factory floor",new Color(.38f,.36f,.34f));rust=Mat("Rust",new Color(.55f,.28f,.15f));
        metal=Mat("Metal grey",new Color(.45f,.5f,.55f));wood=Mat("Crate wood",new Color(.6f,.45f,.25f));
        water=Mat("Paddy water",new Color(.25f,.45f,.4f));paddy=Mat("Paddy soil",new Color(.42f,.36f,.25f));
        straw=Mat("Straw",new Color(.86f,.74f,.38f));rice=Mat("Rice plants",new Color(.45f,.68f,.3f));
        var crateP=PropPrefab("PROP_Crate",wood);var pipeP=PropPrefab("PROP_Pipe",metal);
        var barrelP=PropPrefab("PROP_Barrel",rust);var scrapP=PropPrefab("PROP_ScrapPile",dark);
        var world=new GameObject("Oops Runtime").AddComponent<BlobWorld>();
        world.legacyRoute=false;world.levelTitle="OOPS! PLANET / PROTOTYPE 01 (GREYBOX)";world.checkpointCount=2;
        world.overviewPosition=new Vector3(22,70,-40);world.overviewTarget=new Vector3(22,0,18);
        world.objective="HUB — Choose a path";

        // HUB (spawn at origin; exits north to Factory, east to Rice Fields)
        Floor("Hub floor",Vector3.zero,30,30,grey);
        Walls("Hub",Vector3.zero,30,30,"ne");
        Cylinder("Hub landmark placeholder",new Vector3(-6,2.5f,-6),1.2f,5,glow);
        Label("FACTORY >>",new Vector3(0,2.6f,14.4f),0,.3f);
        Label("RICE FIELDS >>",new Vector3(14.4f,2.6f,0),90,.3f);
        Label("OOPS! PLANET\nHUB",new Vector3(0,3.2f,-14.4f),180,.35f);
        Zone("Area Hub",BlobInteraction.Area,new Vector3(0,2,0),new Vector3(30,6,30),"HUB — Go north to the FACTORY or east to the RICE FIELDS");
        Floor("Path to Factory",new Vector3(0,0,20),6,10,grey);
        Walls("Factory path",new Vector3(0,0,20),6,10,"ns",.8f);
        Floor("Path to Rice Fields",new Vector3(20,0,0),10,6,grey);
        Walls("Rice path",new Vector3(20,0,0),10,6,"ew",.8f);

        // ZONE 01 FACTORY (z 25..55)
        var fc=new Vector3(0,0,40);
        Floor("Factory floor",fc,30,30,factoryWall);
        Walls("Factory",fc,30,30,"s",2.5f);
        Zone("Area Factory",BlobInteraction.Area,fc+Vector3.up*2,new Vector3(30,6,30),"ZONE 01 FACTORY — Greybox (powers & puzzles arrive in P02–P04)");
        Checkpoint(1,new Vector3(0,0,28),0);
        Box("Warehouse block",new Vector3(-9,4,45),new Vector3(10,8,14),dark);
        Box("Warehouse roof lip",new Vector3(-9,8.2f,45),new Vector3(10.6f,.4f,14.6f),metal);
        Cylinder("Chimney",new Vector3(10,6,50),1.5f,12,rust);
        Cylinder("Chimney cap",new Vector3(10,12.2f,50),1.8f,.4f,dark);
        var overhead=Box("Overhead pipe run",new Vector3(1,6.5f,48),new Vector3(16,.6f,.6f),metal);
        Box("Pipe support",new Vector3(1,3.25f,48),new Vector3(.3f,6.5f,.3f),dark);
        Box("Loading dock (jump test, 1 m)",new Vector3(8,.5f,38),new Vector3(8,1,6),metal);
        Label("LOADING DOCK\nJUMP UP (1 m)",new Vector3(8,2.5f,34.8f),0,.18f);
        Place(crateP,new Vector3(-3,0,32));Place(crateP,new Vector3(3,0,49),20);Place(crateP,new Vector3(3.2f,1.2f,49),35);
        Place(barrelP,new Vector3(5,0,52));Place(barrelP,new Vector3(6.1f,0,52.6f));
        Place(pipeP,new Vector3(-2,0,52.5f));Place(scrapP,new Vector3(-4,0,29));Place(scrapP,new Vector3(12,0,30),90);

        // ZONE 02 RICE FIELDS (x 25..59)
        var rc=new Vector3(42,0,0);
        Floor("Rice field ground",rc,34,30,paddy);
        Walls("Rice",rc,34,30,"w",.8f);
        Zone("Area Rice",BlobInteraction.Area,rc+Vector3.up*2,new Vector3(34,6,30),"ZONE 02 RICE FIELDS — Follow the dike to the demo end");
        Checkpoint(2,new Vector3(28,0,0),90);
        Box("Main dike (walkable, 0.15 m)",new Vector3(42,.075f,0),new Vector3(30,.15f,2.2f),paddy);
        foreach(float x in new[]{34f,46f}) Box("Cross dike",new Vector3(x,.075f,0),new Vector3(1.2f,.15f,28),paddy);
        int plants=0;
        foreach(var q in new[]{new Vector3(30,0,8),new Vector3(40,0,8),new Vector3(52,0,8),new Vector3(30,0,-8),new Vector3(40,0,-8),new Vector3(52,0,-8)}) {
            Box("Paddy water",q+Vector3.up*.02f,new Vector3(9,.02f,11),water,null,false);
            for(int i=0;i<3;i++) for(int j=0;j<2;j++,plants++)
                Box("Rice plant",q+new Vector3(-3+i*3,.3f,-2.5f+j*5),new Vector3(.3f,.6f,.3f),rice,null,false);
        }
        Box("Irrigation channel (blocked, visual)",new Vector3(42,.03f,13.2f),new Vector3(32,.03f,1.6f),water,null,false);
        for(int i=0;i<4;i++) Box("Straw bundle",new Vector3(36+i*3.5f,.35f,13.2f),new Vector3(1.4f,.7f,.8f),straw);
        Box("Pump house",new Vector3(56,1.5f,12),new Vector3(4,3,3),metal);

        // DEMO END
        Box("Demo end left pillar",new Vector3(56,1.5f,-2.2f),new Vector3(.5f,3,.5f),green);
        Box("Demo end right pillar",new Vector3(56,1.5f,2.2f),new Vector3(.5f,3,.5f),green);
        Box("Demo end lintel",new Vector3(56,3,0),new Vector3(.5f,.5f,4.9f),green);
        Label("DEMO END",new Vector3(55.6f,2.2f,0),90,.3f,new Color(.3f,1,.5f));
        Zone("Demo end",BlobInteraction.Exit,new Vector3(56.5f,1.5f,0),new Vector3(2,3,4));

        world.sources=new BlobInteractable[0];world.zones=zones.ToArray();
        var cameraGO=new GameObject("Main Camera");cameraGO.tag="MainCamera";cameraGO.AddComponent<AudioListener>();
        var cam=cameraGO.AddComponent<Camera>();cam.fieldOfView=65;cam.farClipPlane=250;cam.nearClipPlane=.08f;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.55f,.7f,.82f);
        var playerGO=new GameObject("BLOB Player");playerGO.layer=2;playerGO.tag="Player";
        playerGO.AddComponent<CharacterController>();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/BLOB_Greybox.fbx");
        if(!model)throw new System.Exception("Blender BLOB export missing");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,playerGO.transform);visual.name="BLOB Blender Visual";
        visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
        foreach(var r in visual.GetComponentsInChildren<Renderer>()) {
            string n=r.name;
            r.sharedMaterial=n.Contains("Pupil")?black:n.Contains("Eye")?white:n.Contains("Core")?glow:blobBody;
            r.gameObject.layer=2;
        }
        var beam=playerGO.AddComponent<LineRenderer>();beam.sharedMaterial=Mat("Absorption stream",new Color(.3f,.85f,1),true);
        beam.positionCount=2;beam.startWidth=.055f;beam.endWidth=.02f;beam.enabled=false;
        playerGO.AddComponent<AudioSource>().spatialBlend=0;
        var player=playerGO.AddComponent<BlobPlayer>();player.world=world;player.view=cam;player.visual=visual.transform;world.player=player;
        playerGO.transform.position=world.spawn;
        cam.transform.position=new Vector3(.65f,3.3f,-5.5f);cam.transform.LookAt(new Vector3(0,1.2f,2));
        var sun=new GameObject("Directional Light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(50,-30,0);
        var fill=new GameObject("Soft fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.35f;fill.color=new Color(.6f,.75f,1);fill.transform.rotation=Quaternion.Euler(25,145,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.55f,.62f);
        RenderSettings.fog=false;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=playerGO;
        Debug.Log("OOPS_BUILD_OK "+ScenePath+" | zones="+zones.Count+" plants="+plants);
        Validate();
    }
    [MenuItem("Tools/OOPS Planet/Validate Prototype 01")]
    public static void Validate() {
        var errors=new List<string>();
        Physics.SyncTransforms();
        var worlds=Object.FindObjectsByType<BlobWorld>(FindObjectsSortMode.None);
        var players=Object.FindObjectsByType<BlobPlayer>(FindObjectsSortMode.None);
        if(worlds.Length!=1) errors.Add("expected 1 BlobWorld, found "+worlds.Length);
        if(players.Length!=1) errors.Add("expected 1 BlobPlayer, found "+players.Length);
        if(!Camera.main) errors.Add("no MainCamera");
        if(Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length==0) errors.Add("no light");
        foreach(var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)>0) errors.Add("missing script on "+go.name);
        if(worlds.Length==1) {
            var w=worlds[0];
            if(!w.player) errors.Add("BlobWorld.player not set");
            if(w.sources==null) errors.Add("BlobWorld.sources is null");
            if(!Physics.Raycast(w.spawn+Vector3.up,Vector3.down,3)) errors.Add("spawn has no ground below");
            int exits=0,areas=0,checkpoints=0;
            foreach(var z in w.zones) {
                if(!z) { errors.Add("null zone entry"); continue; }
                if(z.kind==BlobInteraction.Exit) exits++;
                if(z.kind==BlobInteraction.Area) areas++;
                if(z.kind==BlobInteraction.Checkpoint) {
                    checkpoints++;
                    if(!Physics.Raycast(z.respawn+Vector3.up,Vector3.down,3)) errors.Add("checkpoint "+z.index+" respawn has no ground");
                }
            }
            if(exits!=1) errors.Add("expected 1 demo end, found "+exits);
            if(!w.legacyRoute && areas<3) errors.Add("expected Hub/Factory/Rice areas, found "+areas);
            if(!w.legacyRoute && checkpoints!=w.checkpointCount) errors.Add("checkpointCount "+w.checkpointCount+" != "+checkpoints);
        }
        if(errors.Count==0) Debug.Log("OOPS_VALIDATE_OK scene="+EditorSceneManager.GetActiveScene().path);
        else foreach(var e in errors) Debug.LogError("OOPS_VALIDATE_FAIL "+e);
    }
    [MenuItem("Tools/OOPS Planet/Run P01 Route Test")]
    public static void RouteTest() {
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode first");return; }
        var world=Object.FindFirstObjectByType<BlobWorld>();
        if(!world)throw new System.Exception("BlobWorld not found");
        if(world.GetComponent<OopsRouteTest>())throw new System.Exception("Route test already exists");
        world.gameObject.AddComponent<OopsRouteTest>();
    }
}
}
#endif
