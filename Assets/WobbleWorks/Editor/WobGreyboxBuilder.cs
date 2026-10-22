#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
namespace WobbleWorks.Editor {
public static class WobGreyboxBuilder {
    static Transform root;
    static Material grey,dark,light,red,yellow,purple,cyan,green,white,black,glow;
    static List<WobInteractable> sources,zones;
    static Material Mat(string name,Color color,bool emission=false) {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");
        if(!shader)throw new System.Exception("URP Lit shader unavailable");
        var mat=new Material(shader){name=name};
        mat.SetColor("_BaseColor",color); mat.SetFloat("_Smoothness",.18f);
        if(emission) { mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*1.5f); }
        AssetDatabase.CreateAsset(mat,AssetDatabase.GenerateUniqueAssetPath("Assets/WobbleWorks/Materials/"+name+".mat"));
        return mat;
    }
    static GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
        go.transform.SetParent(parent?parent:root); go.transform.position=pos;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=mat; return go;
    }
    static GameObject Ball(string name,Vector3 pos,float size,Material mat,Transform parent=null) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;
        go.transform.SetParent(parent?parent:root);go.transform.position=pos;go.transform.localScale=Vector3.one*size;go.GetComponent<Renderer>().sharedMaterial=mat;return go;
    }
    static void Label(string text,Vector3 pos,float size=.35f,Color? color=null) {
        var go=new GameObject("Sign "+text);go.transform.SetParent(root);go.transform.position=pos;go.transform.rotation=Quaternion.identity;
        var tm=go.AddComponent<TextMesh>();tm.text=text;tm.characterSize=size*.45f;tm.fontSize=64;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;
        tm.color=color??new Color(.93f,.97f,1); tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<Renderer>().sharedMaterial=tm.font.material;
        var bounds=go.GetComponent<Renderer>().bounds;
        var backing=Box("Sign backing",bounds.center+Vector3.forward*.04f,new Vector3(bounds.size.x+.4f,bounds.size.y+.25f,.05f),dark,go.transform);
        Object.DestroyImmediate(backing.GetComponent<Collider>());
        if(text.StartsWith("0")) { go.transform.localScale*=.4f; var p=go.transform.position; p.x=-4.3f; go.transform.position=p; }
        else if(text.StartsWith("CHECKPOINT")) { var p=go.transform.position; p.x=3.5f; go.transform.position=p; }
    }
    static void Floor(string name,float z,float length,float y=0,float width=14) {
        Box(name,new Vector3(0,y-.5f,z),new Vector3(width,1,length),grey);
        for(float dz=-length/2+2;dz<length/2;dz+=2) {
            var line=Box("2m grid",new Vector3(0,y+.008f,z+dz),new Vector3(width,.012f,.025f),light);
            Object.DestroyImmediate(line.GetComponent<Collider>());
        }
        for(float x=-width/2+2;x<width/2;x+=2) {
            var line=Box("2m grid",new Vector3(x,y+.009f,z),new Vector3(.025f,.012f,length),light);
            Object.DestroyImmediate(line.GetComponent<Collider>());
        }
        if(length>=6 && width>=10) {
            foreach(float x in new[]{-width/2,width/2}) {
                Box("Safety rail",new Vector3(x,y+.65f,z),new Vector3(.12f,.12f,length),dark);
                for(float q=z-length/2+1;q<z+length/2;q+=3)Box("Rail post",new Vector3(x,y+.35f,q),new Vector3(.13f,.7f,.13f),dark);
                Box("Foundation",new Vector3(x*.7f,y-3,z),new Vector3(1,5,length*.65f),dark);
            }
        }
    }
    static Material PowerMat(WobProperty p) => p==WobProperty.Magnet?red:p==WobProperty.Spring?yellow:p==WobProperty.Gas?purple:cyan;
    static WobInteractable Source(string name,WobProperty property,Vector3 pos) {
        Box(name+" pedestal",pos-Vector3.up*.65f,new Vector3(1.4f,.5f,1.4f),dark);
        var go=Ball(name,pos,.85f,PowerMat(property));
        var s=go.AddComponent<WobInteractable>();s.kind=WobInteraction.Source;s.property=property;s.indicator=go.GetComponent<Renderer>();
        sources.Add(s);
        Label(property.ToString().ToUpper()+"\nHOLD E",pos+Vector3.up*1.1f,.14f,WobPlayer.PropertyColor(property));
        return s;
    }
    static WobInteractable Zone(string name,WobInteraction kind,Vector3 pos,Vector3 size) {
        var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=pos;
        var z=go.AddComponent<WobInteractable>();z.kind=kind;z.size=size;zones.Add(z);return z;
    }
    static void Checkpoint(int index,Vector3 p) {
        var z=Zone("Checkpoint "+index,WobInteraction.Checkpoint,p+Vector3.up,new Vector3(5,3,3));z.index=index;z.respawn=p+Vector3.up*.1f;
        Box("Checkpoint pad "+index,p+Vector3.up*.03f,new Vector3(4,.06f,2),green);
        Box("Checkpoint mast",p+new Vector3(-2,1,0),new Vector3(.12f,2,.12f),dark);
        Box("Checkpoint flag",p+new Vector3(-1.5f,1.7f,0),new Vector3(1,.5f,.08f),green);
        Label("CHECKPOINT "+index,p+new Vector3(0,2.1f,0),.18f,new Color(.3f,1,.6f));
    }
    static void ConsoleAt(Vector3 p) {
        Box("Reset console",p+Vector3.up*.5f,new Vector3(.7f,1,.7f),dark);
        Ball("Reset button",p+new Vector3(0,1,0),.25f,red);
        Zone("Reset console range",WobInteraction.Reset,p+Vector3.up,new Vector3(4,3,4));
        Label("Q / RESET CUBE",p+Vector3.up*1.6f,.12f);
    }
    [MenuItem("Tools/Wobble Works/Create Greybox 01")]
    public static void Build() {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode before creating a scene");
        const string scenePath="Assets/WobbleWorks/Scenes/WobbleGreybox01.unity";
        if(File.Exists(scenePath))throw new System.Exception("Greybox scene already exists; preserve it and edit in place.");
        Directory.CreateDirectory("Assets/WobbleWorks/Scenes");
        Directory.CreateDirectory("Assets/WobbleWorks/Materials");
        Directory.CreateDirectory("Assets/WobbleWorks/Models");
        Directory.CreateDirectory("Assets/WobbleWorks/Evidence");
        File.Copy(@"E:\!for WORK\GPT\WobbleGreybox\WOB_Greybox.obj","Assets/WobbleWorks/Models/WOB_Greybox.obj",false);
        AssetDatabase.ImportAsset("Assets/WobbleWorks/Models/WOB_Greybox.obj",ImportAssetOptions.ForceSynchronousImport);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        root=new GameObject("WOBBLE GREYBOX 01").transform;
        sources=new List<WobInteractable>();zones=new List<WobInteractable>();
        grey=Mat("Blockout concrete",new Color(.43f,.48f,.53f));dark=Mat("Structural charcoal",new Color(.15f,.20f,.25f));
        light=Mat("Grid lines",new Color(.59f,.65f,.69f));white=Mat("Eye white",new Color(.95f,.98f,1));black=Mat("Pupils",new Color(.02f,.025f,.035f));
        red=Mat("MAGNET red",WobPlayer.PropertyColor(WobProperty.Magnet),true);
        yellow=Mat("SPRING yellow",WobPlayer.PropertyColor(WobProperty.Spring),true);
        purple=Mat("GAS purple",WobPlayer.PropertyColor(WobProperty.Gas),true);
        cyan=Mat("LIQUID cyan",WobPlayer.PropertyColor(WobProperty.Liquid),true);
        green=Mat("Checkpoint green",new Color(.17f,.8f,.38f),true);glow=Mat("WOB glowing core",new Color(1,.7f,.2f),true);
        var manager=new GameObject("Wobble Runtime");var world=manager.AddComponent<WobWorld>();
        Floor("01 Arrival",0,12,0,12);
        Floor("Jump landing",11,6,0,12);
        Floor("02 Absorption lab",20,12);
        Floor("03 Magnet room",34,16);
        Source("Tutorial source",WobProperty.Magnet,new Vector3(-3,1,20));
        Source("Occluded source",WobProperty.Magnet,new Vector3(4,1,22));
        Box("Line of sight test wall",new Vector3(3,1.25f,20),new Vector3(4.5f,2.5f,.4f),dark);
        Source("Magnet source",WobProperty.Magnet,new Vector3(-4,1,29));
        Label("01 / MOVE + JUMP\nWASD + SPACE",new Vector3(0,3,4),.26f);
        Label("02 / ABSORPTION LAB\nAIM + HOLD E",new Vector3(0,3.5f,24),.25f);
        Label("03 / MAGNET\nPULL THE CUBE TO THE PLATE",new Vector3(0,3.6f,39),.24f,WobPlayer.PropertyColor(WobProperty.Magnet));
        var crate=Box("Designated Magnetic Cube",new Vector3(-3,.65f,34),Vector3.one*1.2f,dark);
        var rb=crate.AddComponent<Rigidbody>();rb.mass=2;rb.linearDamping=1;rb.angularDamping=3;rb.constraints=RigidbodyConstraints.FreezeRotation;
        rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        var rim=Box("Red magnetic designation",crate.transform.position+new Vector3(0,.62f,0),new Vector3(1,.035f,1),red,crate.transform);Object.DestroyImmediate(rim.GetComponent<Collider>());
        world.crate=rb;world.crateStart=rb.position;
        var plate=Box("Crate pressure plate",new Vector3(3,.08f,36),new Vector3(2.4f,.16f,2.4f),red);
        world.plate=plate.transform;world.plateLamp=plate.GetComponent<Renderer>();
        Object.DestroyImmediate(plate.GetComponent<Collider>());
        var bridge=Box("Unlocked crossing",new Vector3(0,-.35f,45),new Vector3(4,.7f,6),green);bridge.SetActive(false);world.bridge=bridge;
        Label("PLATE OPENS BRIDGE",new Vector3(0,2.4f,43),.17f);
        ConsoleAt(new Vector3(5,0,32));
        Floor("04 Spring chamber",54,12);
        Source("Spring source",WobProperty.Spring,new Vector3(-4,1,52));
        Checkpoint(1,new Vector3(0,0,50));
        Label("04 / SPRING\nHOLD SPACE / RELEASE",new Vector3(0,3.5f,58),.26f,WobPlayer.PropertyColor(WobProperty.Spring));
        Box("Spring launch marking",new Vector3(0,.025f,57),new Vector3(3,.05f,2),yellow);
        Floor("Spring upper landing",65,10,5);
        Checkpoint(2,new Vector3(0,5,65));
        Floor("05 Gas approach",76,12,5);
        Source("Gas source",WobProperty.Gas,new Vector3(-3,6,71));
        Label("05 / GAS\nSPACE IN THE SHAFT TO RISE",new Vector3(0,9,74),.24f,WobPlayer.PropertyColor(WobProperty.Gas));
        Zone("Designated gas updraft",WobInteraction.GasVent,new Vector3(0,9,78),new Vector3(4.2f,8,6.2f));
        Box("Vent west wall",new Vector3(-2.3f,9,78),new Vector3(.4f,8,6.4f),dark);
        Box("Vent east wall",new Vector3(2.3f,9,78),new Vector3(.4f,8,6.4f),dark);
        Box("Vent entry lintel",new Vector3(0,10.3f,74.9f),new Vector3(4.2f,5.4f,.3f),grey);
        Box("Vent exit lower wall",new Vector3(0,7.95f,81.1f),new Vector3(4.2f,5.9f,.3f),grey);
        Box("Vent ceiling",new Vector3(0,13.4f,78),new Vector3(5,.4f,6.4f),dark);
        for(float y=5.2f;y<13;y+=1.5f) {
            var bar=Box("Updraft marker",new Vector3(0,y,78),new Vector3(1.2f,.035f,.1f),purple);Object.DestroyImmediate(bar.GetComponent<Collider>());
        }
        Floor("06 Liquid approach",88,14,11);
        Checkpoint(3,new Vector3(0,11,87));
        Source("Liquid source",WobProperty.Liquid,new Vector3(-3,12,91));
        Label("06 / LIQUID\nHOLD F TO FLATTEN",new Vector3(0,14.5f,94),.26f,WobPlayer.PropertyColor(WobProperty.Liquid));
        Floor("Narrow pipe floor",100,10,11,2.1f);
        Box("Pipe west side",new Vector3(-1.2f,12.6f,100),new Vector3(.3f,3.2f,10),dark);
        Box("Pipe east side",new Vector3(1.2f,12.6f,100),new Vector3(.3f,3.2f,10),dark);
        Box("Pipe roof — 0.52m clearance",new Vector3(0,12.87f,100),new Vector3(2.7f,2.7f,10),grey);
        Zone("Designated liquid clearance",WobInteraction.Pipe,new Vector3(0,11.4f,100),new Vector3(2.1f,1.1f,10));
        for(float z=95;z<=105;z+=2) {
            Box("Pipe cyan floor strip",new Vector3(0,11.015f,z),new Vector3(1.7f,.02f,.1f),cyan);
        }
        Floor("Exit platform",111,12,11);
        Box("Exit left pillar",new Vector3(-2,12.5f,114),new Vector3(.5f,3,.5f),green);
        Box("Exit right pillar",new Vector3(2,12.5f,114),new Vector3(.5f,3,.5f),green);
        Box("Exit lintel",new Vector3(0,14,114),new Vector3(4.5f,.5f,.5f),green);
        Label("EXIT",new Vector3(0,13.1f,114),.5f,new Color(.3f,1,.5f));
        Zone("Level complete",WobInteraction.Exit,new Vector3(0,12,114),new Vector3(4,3,2));
        ConsoleAt(new Vector3(5,11,109));
        world.sources=sources.ToArray();world.zones=zones.ToArray();
        var cameraGO=new GameObject("Main Camera");cameraGO.tag="MainCamera";cameraGO.AddComponent<AudioListener>();
        var cam=cameraGO.AddComponent<Camera>();cam.fieldOfView=65;cam.farClipPlane=250;cam.nearClipPlane=.08f;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.055f,.08f,.12f);
        var playerGO=new GameObject("WOB Player");playerGO.layer=2;playerGO.tag="Player";
        playerGO.AddComponent<CharacterController>();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WobbleWorks/Models/WOB_Greybox.obj");
        if(!model)throw new System.Exception("Blender mesh import failed");
        var visual=Object.Instantiate(model,playerGO.transform);visual.name="WOB Blender Visual";
        visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
        foreach(var r in visual.GetComponentsInChildren<Renderer>()) {
            string name=r.name;
            r.sharedMaterial=name.Contains("Pupil")?black:name.Contains("Eye")?white:name.Contains("Core")?glow:name.Contains("Orbit")?cyan:light;
            r.gameObject.layer=2;
        }
        var beam=playerGO.AddComponent<LineRenderer>();beam.sharedMaterial=Mat("Absorption stream",new Color(.3f,.85f,1),true);
        beam.positionCount=2;beam.startWidth=.055f;beam.endWidth=.02f;beam.enabled=false;
        playerGO.AddComponent<AudioSource>().spatialBlend=0;
        var player=playerGO.AddComponent<WobPlayer>();player.world=world;player.view=cam;player.visual=visual.transform;world.player=player;
        playerGO.transform.position=world.spawn;
        cam.transform.position=new Vector3(.65f,3.3f,-5.5f);cam.transform.LookAt(new Vector3(0,1.2f,2));
        var sun=new GameObject("Directional Light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.6f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-32,0);
        var fill=new GameObject("Soft fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.35f;fill.color=new Color(.55f,.73f,1);fill.transform.rotation=Quaternion.Euler(25,145,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.43f,.5f,.6f);
        RenderSettings.fog=false;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,scenePath);
        AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,5,57),Quaternion.Euler(45,-35,0),80);
        Selection.activeGameObject=playerGO;
        Debug.Log("WOBBLE_BUILD_OK "+scenePath+" | sources="+sources.Count+" zones="+zones.Count+" | Blender WOB imported");
    }
    [MenuItem("Tools/Wobble Works/Run Playtest")]
    public static void Test() {
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode first");return; }
        var world=Object.FindFirstObjectByType<WobWorld>();
        if(!world)throw new System.Exception("WobWorld not found");
        if(world.GetComponent<WobPlaytest>())throw new System.Exception("Playtest already exists");
        world.gameObject.AddComponent<WobPlaytest>();
    }
    [MenuItem("Tools/Wobble Works/Show Overview")]
    public static void Overview() {
        var world=Object.FindFirstObjectByType<WobWorld>();
        if(world)world.overview=true;
    }
}
}
#endif
