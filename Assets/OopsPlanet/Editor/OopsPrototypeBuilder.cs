#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace OopsPlanet.Editor {
// P01 greybox: Hub + Zone 01 Factory (north) + Zone 02 Rice Fields (east). Movement/camera only; no powers or puzzles.
public static class OopsPrototypeBuilder {
    const string Root="Assets/OopsPlanet";
    const string ScenePath=Root+"/Scenes/OopsPlanet_Prototype01.unity";
    static Transform root;
    static Font uiFont;
    static Material grey,dark,light,green,white,black,glow,blobBody,factoryWall,rust,metal,wood,water,paddy,straw,rice,hazard;
    static List<BlobInteractable> zones,sources;
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
        tm.color=color??new Color(.93f,.97f,1); tm.font=uiFont?uiFont:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
    static GameObject ModelInstance(string model,Material[] mats,Transform parent,bool collider) {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+model+".fbx");
        if(!asset)throw new System.Exception("Blender export missing: "+model+".fbx");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
        go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
        foreach(var r in go.GetComponentsInChildren<Renderer>()) {
            var arr=new Material[r.sharedMaterials.Length];
            for(int i=0;i<arr.Length;i++) arr[i]=mats[Mathf.Min(i,mats.Length-1)];
            r.sharedMaterials=arr;
            if(collider && !r.GetComponent<Collider>()) r.gameObject.AddComponent<BoxCollider>();
        }
        return go;
    }
    // Absorbable source prefab: root BlobInteractable (Source) + "Full" visual (+ optional "Remnant" shown after absorb).
    static GameObject AbsorbablePrefab(string prefabName,string model,Material[] mats,BlobProperty property,AbsorbResult result,string remnantModel=null) {
        string path=Root+"/Prefabs/"+prefabName+".prefab";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(existing) return existing;
        var root=new GameObject(prefabName);
        var full=ModelInstance(model,mats,root.transform,true); full.name="Full";
        var s=root.AddComponent<BlobInteractable>();
        s.kind=BlobInteraction.Source;s.property=property;s.onAbsorbed=result;
        s.fullVisual=full;s.indicator=full.GetComponentInChildren<Renderer>();
        if(remnantModel!=null) {
            var rem=ModelInstance(remnantModel,mats,root.transform,true); rem.name="Remnant"; rem.SetActive(false);
            s.remnantVisual=rem;
        }
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);
        Object.DestroyImmediate(root);
        return prefab;
    }
    static BlobInteractable PlaceSource(GameObject prefab,string name,Vector3 pos,float yaw=0) {
        var go=Place(prefab,pos,yaw); go.name=name;
        var s=go.GetComponent<BlobInteractable>(); sources.Add(s); return s;
    }
    // ---------- P06 art, audio, VFX and lighting ----------
    static GameObject Decor(string model,Material[] mats,Vector3 pos,float yaw,bool collider,float scale=1) {
        string path=Root+"/Prefabs/Decor_"+model+".prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!prefab) {
            var tmp=new GameObject("Decor_"+model);
            ModelInstance(model,mats,tmp.transform,collider);
            prefab=PrefabUtility.SaveAsPrefabAsset(tmp,path); Object.DestroyImmediate(tmp);
        }
        var go=Place(prefab,pos,yaw); go.transform.localScale=Vector3.one*scale; return go;
    }
    static void Polish(BlobWorld world,PuzzleObjective gate,PuzzleObjective vent,PuzzleObjective rice,List<Transform> plants,Material metal,Material rust,Material dark,Material hazard,Material wood,Material straw) {
        world.uiFont=uiFont;
        // Shared VFX particle system (opaque unlit spheres, vertex-coloured per burst).
        var vfxGO=new GameObject("VFX kit"); vfxGO.transform.SetParent(root);
        var ps=vfxGO.AddComponent<ParticleSystem>();
        var main=ps.main; main.simulationSpace=ParticleSystemSimulationSpace.World; main.maxParticles=3000; main.startSpeed=0; main.startLifetime=.5f; main.gravityModifier=.35f; main.playOnAwake=true; main.loop=true;
        var em=ps.emission; em.rateOverTime=0;
        var sh=ps.shape; sh.enabled=false;
        var sol=ps.sizeOverLifetime; sol.enabled=true; sol.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
        var pr=vfxGO.GetComponent<ParticleSystemRenderer>(); pr.renderMode=ParticleSystemRenderMode.Mesh; pr.mesh=Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        pr.sharedMaterial=ParticleMat(); pr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        vfxGO.AddComponent<VfxKit>();
        // Zone ambience: normal loop until the zone is restored, then a brighter loop.
        var ambGO=new GameObject("Zone ambience"); ambGO.transform.SetParent(root);
        var amb=ambGO.AddComponent<ZoneAmbience>(); amb.world=world;
        amb.zones=new[]{
            new ZoneAmbience.Zone{name="Hub",area=new Bounds(new Vector3(0,3,0),new Vector3(30,12,30)),normal=Ambience.Hub,restored=Ambience.HubRestored,restoredWhen=new[]{gate,vent,rice}},
            new ZoneAmbience.Zone{name="Factory",area=new Bounds(new Vector3(0,4,40),new Vector3(30,14,30)),normal=Ambience.Factory,restored=Ambience.FactoryRestored,restoredWhen=new[]{gate,vent}},
            new ZoneAmbience.Zone{name="Rice Fields",area=new Bounds(new Vector3(42,3,0),new Vector3(34,12,30)),normal=Ambience.Rice,restored=Ambience.RiceRestored,restoredWhen=new[]{rice}},
        };
        world.ambience=amb;
        // Factory dressing: old leaning boilers, valves, wall pipes, gears and oil stains (decor only; no gameplay colliders on paths).
        var oil=Mat("Oil stain",new Color(.06f,.06f,.07f));
        Decor("PROP_Boiler",new[]{metal,rust,hazard},new Vector3(-12.8f,0,35.3f),90,true);
        Decor("PROP_Boiler",new[]{metal,rust,hazard},new Vector3(13,0,53),0,true);
        Decor("PROP_ValveWheel",new[]{metal,hazard},new Vector3(-11,0,37.2f),20,false);
        Decor("PROP_ValveWheel",new[]{metal,hazard},new Vector3(11.4f,0,53.8f),0,false);
        for(int i=0;i<6;i++) Decor("PROP_PipeStraight",new[]{metal,rust},new Vector3(14.45f,1.9f,28+i*2),90,false);
        Decor("PROP_PipeElbow",new[]{metal},new Vector3(14.45f,2.2f,39.6f),90,false);
        Decor("PROP_Gear",new[]{rust,dark},new Vector3(1.5f,4.2f,54.62f),0,false);
        Decor("PROP_Gear",new[]{rust,dark},new Vector3(11,4.6f,54.62f),15,false,.8f);
        Decor("PROP_Gear",new[]{rust,dark},new Vector3(-8,4.5f,41.62f),0,false,1.2f);
        foreach(var st in new[]{new Vector4(2.5f,31,30,1.3f),new Vector4(-6,36.5f,80,1),new Vector4(7,45,140,1.5f),new Vector4(-1.5f,48,10,1.2f),new Vector4(9.5f,27.5f,200,.9f),new Vector4(-9,26.5f,260,1.1f)})
            Decor("PROP_OilStain",new[]{oil},new Vector3(st.x,.012f,st.y),st.z,false,st.w);
        // Rice dressing: cute-strange scarecrows, baskets and a hoe by the pump house.
        var cloth=Mat("Cloth red",new Color(.75f,.25f,.2f)); var button=Mat("Button black",new Color(.05f,.05f,.06f));
        Decor("PROP_Scarecrow",new[]{wood,straw,cloth,button},new Vector3(50,0,-11),-90,true);
        Decor("PROP_Scarecrow",new[]{wood,straw,cloth,button},new Vector3(36,0,-12),-70,true);
        Decor("PROP_Basket",new[]{straw},new Vector3(53.2f,0,9.4f),0,true);
        Decor("PROP_Basket",new[]{straw},new Vector3(54.3f,0,9.9f),40,true);
        var hoe=Decor("PROP_Hoe",new[]{wood,metal},new Vector3(52.4f,0,10.2f),30,false); hoe.transform.rotation*=Quaternion.Euler(0,0,12);
        // Rice plants turn green as they grow back (second reaction on the rice puzzle).
        var plantRenderers=new List<Renderer>(); foreach(var pl in plants) plantRenderers.AddRange(pl.GetComponentsInChildren<Renderer>());
        var plantsGO=new GameObject("Rice plants restoration"); plantsGO.transform.SetParent(root);
        var pr2=plantsGO.AddComponent<PuzzleReaction>(); pr2.objective=rice; pr2.duration=2.5f;
        pr2.recolor=plantRenderers.ToArray(); pr2.solvedColor=new Color(.3f,.75f,.3f);
        pr2.lights=new Light[0]; pr2.showWhenSolved=new GameObject[0]; pr2.hideWhenSolved=new GameObject[0]; pr2.stopWhenSolved=new ParticleSystem[0]; pr2.grow=new Transform[0];
        // Thai power names floating over every absorbable source (shape-independent cue, not colour only).
        foreach(var s in sources) {
            string n=s.property==BlobProperty.Magnet?"แม่เหล็ก":s.property==BlobProperty.Pressure?"แรงดัน":"เด้ง";
            var b=s.indicator.bounds;
            var lg=new GameObject("Power label "+n); lg.transform.SetParent(s.transform); lg.transform.position=new Vector3(b.center.x,b.max.y+.55f,b.center.z);
            var tm=lg.AddComponent<TextMesh>(); tm.text=n; tm.font=uiFont; tm.fontSize=64; tm.characterSize=.035f; tm.anchor=TextAnchor.MiddleCenter; tm.color=BlobPlayer.PropertyColor(s.property);
            lg.GetComponent<Renderer>().sharedMaterial=uiFont.material;
            lg.AddComponent<FaceCamera>(); s.powerLabel=lg;
        }
    }
    static Material ParticleMat() {
        string path=Root+"/Materials/VFX particle.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path); if(m) return m;
        var sh=Shader.Find("Universal Render Pipeline/Particles/Unlit"); if(!sh) throw new System.Exception("URP Particles/Unlit shader missing");
        m=new Material(sh){name="VFX particle"}; m.SetColor("_BaseColor",Color.white);
        AssetDatabase.CreateAsset(m,path); return m;
    }
    static void PlayerFeedback(GameObject playerGO,BlobPlayer player,BlobPowers powers,BlobWorld world,Transform visual) {
        Transform Find(string n) { foreach(var t in visual.GetComponentsInChildren<Transform>(true)) if(t.name==n) return t; throw new System.Exception("BLOB part missing: "+n); }
        var ex=playerGO.AddComponent<BlobExpression>(); ex.player=player;
        ex.eyes=new[]{Find("BLOB_Eye_L"),Find("BLOB_Eye_R")}; ex.pupils=new[]{Find("BLOB_Pupil_L"),Find("BLOB_Pupil_R")}; ex.mouth=Find("BLOB_Mouth");
        var fb=playerGO.AddComponent<BlobFeedback>(); fb.player=player; fb.powers=powers; fb.world=world; fb.expression=ex;
        AudioSource Src() { var a=playerGO.AddComponent<AudioSource>(); a.spatialBlend=0; a.playOnAwake=false; return a; }
        fb.voice=Src(); fb.sfx=Src(); fb.magnetLoop=Src();
    }
    static void Lighting(Camera cam) {
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.62f,.7f,.85f); RenderSettings.ambientEquatorColor=new Color(.5f,.52f,.55f); RenderSettings.ambientGroundColor=new Color(.3f,.28f,.25f);
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=70; RenderSettings.fogEndDistance=200; RenderSettings.fogColor=new Color(.62f,.74f,.85f);
        cam.backgroundColor=new Color(.62f,.74f,.85f);
        var camData=cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing=true; camData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        string path=Root+"/Settings/OOPS_PostFX.asset";
        Directory.CreateDirectory(Root+"/Settings"); AssetDatabase.Refresh();
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(!profile) {
            profile=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,path);
            T Add<T>() where T:VolumeComponent { var c=profile.Add<T>(true); c.name=typeof(T).Name; AssetDatabase.AddObjectToAsset(c,profile); return c; }
            var bloom=Add<Bloom>(); bloom.threshold.Override(.9f); bloom.intensity.Override(.6f);
            Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var ca=Add<ColorAdjustments>(); ca.postExposure.Override(.25f); ca.saturation.Override(12); ca.contrast.Override(8);
            Add<Vignette>().intensity.Override(.22f);
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
        }
        var vol=new GameObject("Post FX volume").AddComponent<Volume>(); vol.isGlobal=true; vol.sharedProfile=profile;
    }
    static GameObject Ball(string name,Vector3 pos,float size,Material mat) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name=name;
        go.transform.SetParent(root); go.transform.position=pos; go.transform.localScale=Vector3.one*size;
        go.GetComponent<Renderer>().sharedMaterial=mat; Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static Light PointLight(string name,Vector3 pos) {
        var l=new GameObject(name).AddComponent<Light>(); l.transform.SetParent(root); l.transform.position=pos;
        l.type=LightType.Point; l.range=16; l.intensity=3; l.color=new Color(1,.25f,.2f);
        return l;
    }
    // Opaque grey sphere puffs (readable in greybox without transparent particle materials).
    static ParticleSystem Smoke(string name,Vector3 pos) {
        var go=new GameObject(name); go.transform.SetParent(root); go.transform.position=pos; go.transform.rotation=Quaternion.Euler(-90,0,0);
        var ps=go.AddComponent<ParticleSystem>();
        var main=ps.main; main.startLifetime=4; main.startSpeed=1.6f; main.startSize=new ParticleSystem.MinMaxCurve(.8f,1.4f); main.maxParticles=60; main.gravityModifier=-.03f;
        var em=ps.emission; em.rateOverTime=6;
        var sh=ps.shape; sh.shapeType=ParticleSystemShapeType.Cone; sh.angle=12; sh.radius=.8f;
        var sol=ps.sizeOverLifetime; sol.enabled=true; sol.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,1.8f));
        var r=go.GetComponent<ParticleSystemRenderer>(); r.renderMode=ParticleSystemRenderMode.Mesh;
        r.mesh=Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx"); r.sharedMaterial=Mat("Smoke grey",new Color(.32f,.3f,.3f));
        return ps;
    }
    static PuzzleObjective Puzzle(string name,string id,string titleTh,string hintTh,string solvedTh,Bounds area,PuzzleObjective requires,MonoBehaviour[] conditions,PuzzleItem[] items) {
        var go=new GameObject(name); go.transform.SetParent(root); go.transform.position=area.center;
        var p=go.AddComponent<PuzzleObjective>();
        p.id=id; p.titleTh=titleTh; p.hintTh=hintTh; p.solvedTh=solvedTh; p.area=area; p.requires=requires; p.conditions=conditions; p.items=items;
        return p;
    }
    static void ResetPad(string name,Vector3 pos,PuzzleObjective target) {
        var pad=Box(name,pos+Vector3.up*.03f,new Vector3(2,.06f,2),hazard,null,false);
        var r=pad.AddComponent<PuzzleResetPad>(); r.target=target; r.size=new Vector3(2,2,2);
        pad.transform.localScale=new Vector3(2,.06f,2);
        Label("รีเซ็ต RESET",pos+Vector3.up*1.2f,0,.14f);
    }
    static GameObject MagneticCube(string name,Vector3 pos,float size,float mass,Material body,Material stripe) {
        var go=Box(name,pos,Vector3.one*size,body);
        Box("Magnetic marking",pos+Vector3.up*(size/2+.01f),new Vector3(size*.8f,.03f,size*.8f),stripe,go.transform,false);
        var rb=go.AddComponent<Rigidbody>();rb.mass=mass;rb.linearDamping=.5f;rb.angularDamping=2;
        rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        go.AddComponent<BlobMagnetic>();
        return go;
    }
    static T DataAsset<T>(string name,System.Action<T> init) where T:ScriptableObject {
        string path=Root+"/Data/"+name+".asset";
        var a=AssetDatabase.LoadAssetAtPath<T>(path);
        if(a) return a;
        a=ScriptableObject.CreateInstance<T>(); init(a);
        AssetDatabase.CreateAsset(a,path);
        return a;
    }
    // Power tuning lives in ScriptableObjects under Assets/OopsPlanet/Data (created once; edit them in the Inspector).
    static PowerSet EnsurePowerData() {
        Directory.CreateDirectory(Root+"/Data"); AssetDatabase.Refresh();
        var magnet=DataAsset<PowerDefinition>("Power_Magnet",d=>{ d.property=BlobProperty.Magnet;d.nameTh="แม่เหล็ก";d.nameEn="Magnet";
            d.hintTh="กดค้างเพื่อดึงวัตถุโลหะ / Shift+ค้าง เพื่อผลัก";d.range=8;d.force=18;d.massLimit=10;d.aimCone=15;d.cooldown=0; });
        var pressure=DataAsset<PowerDefinition>("Power_Pressure",d=>{ d.property=BlobProperty.Pressure;d.nameTh="แรงดัน";d.nameEn="Pressure";
            d.hintTh="กดเพื่อเป่าแรงดันไปข้างหน้า";d.range=6;d.force=14;d.massLimit=12;d.aimCone=35;d.cooldown=1f; });
        var bounce=DataAsset<PowerDefinition>("Power_Bounce",d=>{ d.property=BlobProperty.Bounce;d.nameTh="เด้ง";d.nameEn="Bounce";
            d.hintTh="ค้าง SPACE เพื่อชาร์จ ปล่อยเพื่อเด้งสูง";d.minLaunch=9;d.maxLaunch=15;d.chargeSeconds=.8f;d.rebound=.45f;d.maxRebounds=2;d.reboundMinImpact=7; });
        var cannon=DataAsset<CombinationDefinition>("Combo_MagnetPressure",c=>{ c.a=BlobProperty.Magnet;c.b=BlobProperty.Pressure;c.effect=ComboEffect.MagnetCannon;
            c.nameTh="ปืนแม่เหล็กแรงดัน";c.nameEn="Magnet Cannon";c.howTh="ดึงวัตถุด้วยแม่เหล็ก แล้วกดแรงดันเพื่อยิงออกไป";c.speed=14;c.lift=2; });
        var dash=DataAsset<CombinationDefinition>("Combo_PressureBounce",c=>{ c.a=BlobProperty.Pressure;c.b=BlobProperty.Bounce;c.effect=ComboEffect.PressureDash;
            c.nameTh="พุ่งแรงดัน";c.nameEn="Pressure Dash";c.howTh="เด้งขึ้นแล้วกดแรงดันกลางอากาศเพื่อพุ่งไปข้างหน้า";c.speed=10;c.lift=2; });
        var set=DataAsset<PowerSet>("PowerSet_Prototype",s=>{ s.powers.AddRange(new[]{magnet,pressure,bounce}); s.combinations.AddRange(new[]{cannon,dash}); });
        AssetDatabase.SaveAssets();
        return set;
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
        uiFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansThai-Variable.ttf");
        if(!uiFont)throw new System.Exception("Thai UI font missing: "+Root+"/Fonts/NotoSansThai-Variable.ttf");
        zones=new List<BlobInteractable>();sources=new List<BlobInteractable>();
        grey=Mat("Hub concrete",new Color(.5f,.53f,.55f));dark=Mat("Structural charcoal",new Color(.15f,.2f,.25f));
        light=Mat("Grid lines",new Color(.62f,.66f,.69f));green=Mat("Checkpoint green",new Color(.17f,.8f,.38f),true);
        white=Mat("Eye white",new Color(.95f,.98f,1));black=Mat("Pupils",new Color(.02f,.025f,.035f));
        glow=Mat("BLOB core",new Color(1,.7f,.2f),true);blobBody=Mat("BLOB body",new Color(.63f,.73f,.78f));
        factoryWall=Mat("Factory floor",new Color(.38f,.36f,.34f));rust=Mat("Rust",new Color(.55f,.28f,.15f));
        metal=Mat("Metal grey",new Color(.45f,.5f,.55f));wood=Mat("Crate wood",new Color(.6f,.45f,.25f));
        water=Mat("Paddy water",new Color(.25f,.45f,.4f));paddy=Mat("Paddy soil",new Color(.42f,.36f,.25f));
        straw=Mat("Straw",new Color(.86f,.74f,.38f));rice=Mat("Rice plants (dry)",new Color(.62f,.6f,.35f));
        var crateP=PropPrefab("PROP_Crate",wood);var pipeP=PropPrefab("PROP_Pipe",metal);
        var barrelP=PropPrefab("PROP_Barrel",rust);var scrapP=PropPrefab("PROP_ScrapPile",dark);
        var magnetRed=Mat("Magnet red",BlobPlayer.PropertyColor(BlobProperty.Magnet));var tankBlue=Mat("Tank blue",new Color(.2f,.45f,.7f));
        var brass=Mat("Brass",new Color(.8f,.62f,.25f));var rubber=Mat("Rubber",new Color(.08f,.08f,.09f));
        var absScrap=AbsorbablePrefab("Absorbable_Magnet_Scrap","ABS_ScrapMetal",new[]{metal,magnetRed},BlobProperty.Magnet,AbsorbResult.Remains);
        var absTank=AbsorbablePrefab("Absorbable_Pressure_Tank","ABS_PressureTank",new[]{tankBlue,brass},BlobProperty.Pressure,AbsorbResult.Remnant,"ABS_PressureTank_Empty");
        var absTire=AbsorbablePrefab("Absorbable_Bounce_Tire","ABS_RubberTire",new[]{rubber},BlobProperty.Bounce,AbsorbResult.Disable);
        // Puzzle areas use sources that always remain, so the powers a puzzle needs can never run out (no softlock).
        var absTankRemains=AbsorbablePrefab("Absorbable_Pressure_Tank_Remains","ABS_PressureTank",new[]{tankBlue,brass},BlobProperty.Pressure,AbsorbResult.Remains);
        var absTireRemains=AbsorbablePrefab("Absorbable_Bounce_Tire_Remains","ABS_RubberTire",new[]{rubber},BlobProperty.Bounce,AbsorbResult.Remains);
        hazard=Mat("Hazard yellow",new Color(.95f,.75f,.1f));
        var powerSet=EnsurePowerData();
        var world=new GameObject("Oops Runtime").AddComponent<BlobWorld>();
        world.levelTitle="OOPS! PLANET / PROTOTYPE 01 (GREYBOX)";world.checkpointCount=2;
        world.overviewPosition=new Vector3(22,70,-40);world.overviewTarget=new Vector3(22,0,18);
        world.objective="HUB — Choose a path";

        // HUB (spawn at origin; exits north to Factory, east to Rice Fields)
        Floor("Hub floor",Vector3.zero,30,30,grey);
        Walls("Hub",Vector3.zero,30,30,"ne");
        // P05 Hub landmark: one bio-core tree whose parts/materials show the restoration stage.
        var bark=Mat("Tree bark",new Color(.42f,.3f,.2f)); var leaves=Mat("Tree leaves",new Color(.3f,.68f,.32f)); var stone=Mat("Planter stone",new Color(.45f,.45f,.47f));
        var tree=ModelInstance("HUB_Landmark",new[]{bark},root,false); tree.name="Hub landmark: bio-core tree"; tree.transform.position=new Vector3(-6,0,-6);
        Renderer treeTrunk=null, treeCore=null; GameObject canopySmall=null, canopyFull=null;
        foreach(var r in tree.GetComponentsInChildren<Renderer>(true)) {
            string n=r.name;
            if(n.Contains("Planter")) { r.sharedMaterial=stone; r.gameObject.AddComponent<BoxCollider>(); }
            else if(n.Contains("Trunk")) { treeTrunk=r; r.gameObject.AddComponent<BoxCollider>(); }
            else if(n.Contains("Core")) { r.sharedMaterial=glow; treeCore=r; }
            else if(n.Contains("Canopy_Small")) { r.sharedMaterial=leaves; canopySmall=r.gameObject; }
            else if(n.Contains("Canopy_Full")) { r.sharedMaterial=leaves; canopyFull=r.gameObject; }
        }
        var grass=Mat("Hub grass",new Color(.35f,.7f,.3f)); var grassPatches=new List<GameObject>();
        foreach(var g in new[]{new Vector3(-10,0,-11),new Vector3(-2,0,-11),new Vector3(-11,0,-2),new Vector3(9,0,10),new Vector3(-9,0,11),new Vector3(10,0,-1.5f)})
            grassPatches.Add(Box("Hub grass patch",g+Vector3.up*.015f,new Vector3(3,.03f,2.4f),grass,null,false));
        var hubLight=PointLight("Hub light",new Vector3(0,9,0)); hubLight.color=new Color(1,.92f,.8f); hubLight.intensity=1.5f; hubLight.range=26;
        Label("โรงงาน FACTORY >>",new Vector3(0,2.6f,14.4f),0,.3f);
        Label("นาข้าว RICE FIELDS >>",new Vector3(14.4f,2.6f,0),90,.3f);
        Label("OOPS! PLANET\nฮับ HUB",new Vector3(0,3.2f,-14.4f),180,.35f);
        var hubZone=Zone("Area Hub",BlobInteraction.Area,new Vector3(0,2,0),new Vector3(30,6,30),"HUB — ศูนย์กลาง: ฝึกดูดพลังที่ลาน (กด E ค้าง) แล้วไป FACTORY (ทิศเหนือ) หรือ RICE FIELDS (ทิศตะวันออก)");
        // P02 absorption yard (west half of the Hub): one source per AbsorbResult mode, an occluded source and a non-absorbable decoy.
        Label("ลานฝึกดูดพลัง\nเล็ง + กด E ค้าง",new Vector3(-8.5f,2.6f,1.5f),0,.22f);
        PlaceSource(absScrap,"Absorbable Scrap (Magnet, remains)",new Vector3(-5,0,5),20);
        PlaceSource(absTank,"Absorbable Tank (Pressure, remnant)",new Vector3(-10,0,5));
        PlaceSource(absTire,"Absorbable Tire (Bounce, disables)",new Vector3(-10,0,-1),90);
        Box("Line of sight test wall",new Vector3(-7.5f,1.25f,9),new Vector3(5,2.5f,.4f),dark);
        PlaceSource(absScrap,"Absorbable Scrap (occluded)",new Vector3(-7.5f,0,11.5f));
        Place(crateP,new Vector3(-3.5f,0,2)).name="Decoy Crate (not absorbable)";
        // P03 power lab (south-east of the Hub): magnetic cubes, an over-limit block, a non-magnetic crate, a bounce ledge.
        Label("ห้องทดลองพลัง POWER LAB",new Vector3(8,2.8f,-14.4f),180,.3f);
        foreach(float x in new[]{4f,6.5f}) MagneticCube("Magnetic cube (2 kg)",new Vector3(x,.5f,-6),1,2,metal,magnetRed);
        MagneticCube("Heavy steel block (40 kg, over limit)",new Vector3(9.5f,.7f,-6),1.4f,40,dark,magnetRed);
        var pushCrate=Place(crateP,new Vector3(4,0,-10));pushCrate.name="Pressure test crate (non-magnetic)";
        var pcRb=pushCrate.AddComponent<Rigidbody>();pcRb.mass=3;pcRb.linearDamping=.5f;pcRb.angularDamping=2;
        Box("Bounce ledge (3.5 m)",new Vector3(12,1.75f,-11),new Vector3(4,3.5f,4),dark);
        Label("แท่นเด้ง\n3.5 ม.",new Vector3(12,2.6f,-8.9f),180,.18f);
        Floor("Path to Factory",new Vector3(0,0,20),6,10,grey);
        Walls("Factory path",new Vector3(0,0,20),6,10,"ns",.8f);
        Floor("Path to Rice Fields",new Vector3(20,0,0),10,6,grey);
        Walls("Rice path",new Vector3(20,0,0),10,6,"ew",.8f);

        // ZONE 01 FACTORY (z 25..55): front yard with the Factory Gate puzzle, back room with the Ventilation Turbine.
        var fc=new Vector3(0,0,40);
        Floor("Factory floor",fc,30,30,factoryWall);
        Walls("Factory",fc,30,30,"s",2.5f);
        Zone("Area Factory",BlobInteraction.Area,fc+Vector3.up*2,new Vector3(30,6,30),"ZONE 01 FACTORY — โรงงาน: เปิดประตูโรงงาน แล้วเป่ากังหันระบายอากาศ");
        Checkpoint(1,new Vector3(0,0,28),0);
        Box("Warehouse block",new Vector3(-9,4,45),new Vector3(10,8,14),dark);
        Box("Warehouse roof lip",new Vector3(-9,8.2f,45),new Vector3(10.6f,.4f,14.6f),metal);
        Cylinder("Chimney",new Vector3(10,6,50),1.5f,12,rust);
        Cylinder("Chimney cap",new Vector3(10,12.2f,50),1.8f,.4f,dark);
        Box("Overhead pipe run",new Vector3(1,6.5f,48),new Vector3(16,.6f,.6f),metal);
        Box("Pipe support",new Vector3(1,3.25f,48),new Vector3(.3f,6.5f,.3f),dark);
        Box("Loading dock (jump test, 1 m)",new Vector3(9,.5f,38),new Vector3(6,1,6),metal);
        Label("ลานขนของ\nกระโดดขึ้น (1 ม.)",new Vector3(9,2.5f,34.8f),0,.18f);
        Place(crateP,new Vector3(-3,0,32));Place(crateP,new Vector3(3,0,49),20);Place(crateP,new Vector3(3.2f,1.2f,49),35);
        Place(barrelP,new Vector3(12,0,46));Place(barrelP,new Vector3(13.1f,0,46.6f));
        Place(pipeP,new Vector3(-2,0,52.5f));Place(scrapP,new Vector3(-4,0,29));Place(scrapP,new Vector3(12,0,30),90);
        PlaceSource(absScrap,"Factory Magnet source (remains)",new Vector3(-12,0,32),30);
        PlaceSource(absTankRemains,"Factory Pressure source (remains)",new Vector3(12,0,26.5f));
        var redLamp=Mat("Lamp red",new Color(.9f,.15f,.1f),true);
        var factoryLights=new[]{PointLight("Factory light front",new Vector3(-2,5,35)),PointLight("Factory light back",new Vector3(6,5,48))};
        // 7 m divider wall (above Bounce + dock height) with a 4 m gate opening at x=0.
        Box("Divider wall west",new Vector3(-8.8f,3.5f,42),new Vector3(12.4f,7,.6f),dark);
        Box("Divider wall east",new Vector3(8.8f,3.5f,42),new Vector3(12.4f,7,.6f),dark);
        var frame=ModelInstance("PZ_FactoryGate_Frame",new[]{metal,hazard},root,false); frame.name="Factory gate frame"; frame.transform.position=new Vector3(0,0,42);
        foreach(float x in new[]{-2.3f,2.3f}) Box("Gate post collider",new Vector3(x,3.5f,42),new Vector3(.6f,7,.7f),metal,frame.transform).GetComponent<Renderer>().enabled=false;
        var door=ModelInstance("PZ_FactoryGate_Door",new[]{dark,hazard},root,true); door.name="Factory gate door"; door.transform.position=new Vector3(0,0,42);
        var gateLamp=Ball("Gate lamp",new Vector3(0,7.5f,41.5f),.45f,redLamp);
        // Transom fills the gap between the 4.2 m door and the lintel so Bounce cannot slip over a closed door.
        Box("Factory gate transom",new Vector3(0,5.2f,42.25f),new Vector3(4,2.1f,.3f),dark);
        var socket=ModelInstance("PZ_Socket",new[]{dark,hazard},root,true); socket.name="Counterweight socket"; socket.transform.position=new Vector3(3.6f,0,39.8f);
        var socketLamp=Ball("Socket lamp",new Vector3(3.6f,1.6f,41.3f),.3f,redLamp);
        var sock=socket.AddComponent<ItemSocket>(); sock.key="counterweight"; sock.lamp=socketLamp.GetComponent<Renderer>();
        Label("วางตุ้มถ่วง\nตรงนี้",new Vector3(3.6f,2.6f,41.6f),0,.14f);
        var weight=ModelInstance("PZ_Counterweight",new[]{metal,magnetRed},root,true); weight.name="Counterweight (6 kg, magnetic)"; weight.transform.position=new Vector3(-8,0,30);
        var wrb=weight.AddComponent<Rigidbody>(); wrb.mass=6; wrb.linearDamping=.5f; wrb.angularDamping=2; wrb.interpolation=RigidbodyInterpolation.Interpolate; wrb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        weight.AddComponent<BlobMagnetic>(); var weightItem=weight.AddComponent<PuzzleItem>(); weightItem.key="counterweight";
        var gate=Puzzle("Puzzle Factory Gate","factory_gate","ประตูโรงงาน","ใช้แม่เหล็ก (กด F ค้าง) ลากตุ้มถ่วงสีแดงไปวางบนแท่นข้างประตู","ประตูเปิดแล้ว — เข้าไปที่ห้องกังหัน",
            new Bounds(new Vector3(0,2,33.5f),new Vector3(30,8,17)),null,new MonoBehaviour[]{sock},new[]{weightItem});
        gate.celebratePoint=new Vector3(0,4,42);
        var gateReaction=gate.gameObject.AddComponent<PuzzleReaction>(); gateReaction.objective=gate; gateReaction.mover=door.transform; gateReaction.moveOffset=Vector3.up*4.4f;
        gateReaction.recolor=new[]{gateLamp.GetComponent<Renderer>()}; gateReaction.solvedColor=new Color(.2f,1,.4f);
        gateReaction.lights=new Light[0]; gateReaction.showWhenSolved=new GameObject[0]; gateReaction.hideWhenSolved=new GameObject[0]; gateReaction.stopWhenSolved=new ParticleSystem[0]; gateReaction.grow=new Transform[0];
        ResetPad("Reset pad: Factory Gate",new Vector3(-11,0,27),gate);
        // Back room: Ventilation Turbine on a 7 m vent wall.
        Box("Vent wall",new Vector3(5.5f,3.5f,55),new Vector3(19,7,.5f),dark);
        var turbineGO=new GameObject("Ventilation turbine"); turbineGO.transform.SetParent(root); turbineGO.transform.position=new Vector3(6,3.2f,54.4f);
        var housing=ModelInstance("PZ_Turbine_Housing",new[]{metal,dark},turbineGO.transform,false); housing.name="Housing";
        var rotor=ModelInstance("PZ_Turbine_Rotor",new[]{dark,hazard},turbineGO.transform,false); rotor.name="Rotor";
        var tcol=turbineGO.AddComponent<BoxCollider>(); tcol.size=new Vector3(3.4f,3.4f,.6f);
        var lamps=new Renderer[3];
        for(int i=0;i<3;i++) lamps[i]=Ball("Turbine gauge lamp "+(i+1),new Vector3(8.6f,1.2f+i*.6f,54.6f),.35f,dark).GetComponent<Renderer>();
        var turbine=turbineGO.AddComponent<PressureTurbine>(); turbine.rotor=rotor.transform; turbine.gaugeLamps=lamps;
        Box("Pressure stand marker",new Vector3(6,.02f,49.5f),new Vector3(1.6f,.04f,1.6f),Mat("Pressure blue",BlobPlayer.PropertyColor(BlobProperty.Pressure),true),null,false);
        Label("ยืนตรงนี้\nเป่าแรงดัน",new Vector3(6,2.2f,48.6f),0,.14f);
        var shutter=Box("Vent shutter (closed)",new Vector3(6,6.1f,54.6f),new Vector3(4,1.2f,.25f),dark);
        var ventOpen=Box("Vent open (fresh air)",new Vector3(6,6.1f,54.6f),new Vector3(4,1.2f,.25f),Mat("Fresh air",new Color(.5f,1,.85f),true),null,false); ventOpen.SetActive(false);
        var smoke=Smoke("Chimney smoke",new Vector3(10,12.5f,50));
        var vent=Puzzle("Puzzle Ventilation Turbine","factory_turbine","กังหันระบายอากาศ","ยืนบนจุดสีฟ้า แล้วเป่าแรงดันใส่กังหันหลายครั้งจนไฟครบ 3 ดวง","อากาศถ่ายเทแล้ว ควันพิษหยุด",
            new Bounds(new Vector3(5.5f,2,48.5f),new Vector3(19,8,13)),gate,new MonoBehaviour[]{turbine},new PuzzleItem[0]);
        turbine.objective=vent; vent.celebratePoint=new Vector3(6,3.2f,53.8f);
        var ventReaction=vent.gameObject.AddComponent<PuzzleReaction>(); ventReaction.objective=vent;
        ventReaction.lights=factoryLights; ventReaction.solvedLightColor=new Color(1,.85f,.6f); ventReaction.solvedIntensity=3;
        ventReaction.hideWhenSolved=new[]{shutter}; ventReaction.showWhenSolved=new[]{ventOpen}; ventReaction.stopWhenSolved=new[]{smoke};
        ventReaction.recolor=new Renderer[0]; ventReaction.grow=new Transform[0];

        // ZONE 02 RICE FIELDS (x 25..59): straw blocks the sluice on a 3 m platform (Bounce up, Pressure it off).
        var rc=new Vector3(42,0,0);
        Floor("Rice field ground",rc,34,30,paddy);
        Walls("Rice",rc,34,30,"w",.8f);
        Zone("Area Rice",BlobInteraction.Area,rc+Vector3.up*2,new Vector3(34,6,30),"ZONE 02 RICE FIELDS — นาข้าว: เคลียร์ฟางที่อุดประตูน้ำ แล้วเดินตามคันนาไปจุดจบ");
        Checkpoint(2,new Vector3(28,0,0),90);
        Box("Main dike (walkable, 0.15 m)",new Vector3(42,.075f,0),new Vector3(30,.15f,2.2f),paddy);
        foreach(float x in new[]{34f,46f}) Box("Cross dike",new Vector3(x,.075f,0),new Vector3(1.2f,.15f,28),paddy);
        int plants=0; var plantList=new List<Transform>(); var paddyWater=new List<Renderer>();
        foreach(var q in new[]{new Vector3(30,0,8),new Vector3(40,0,8),new Vector3(52,0,8),new Vector3(30,0,-8),new Vector3(40,0,-8),new Vector3(52,0,-8)}) {
            paddyWater.Add(Box("Paddy water",q+Vector3.up*.02f,new Vector3(9,.02f,11),water,null,false).GetComponent<Renderer>());
            for(int i=0;i<3;i++) for(int j=0;j<2;j++,plants++) {
                // Pivot at the base so the plant grows upward when restored.
                var pivot=new GameObject("Rice plant"); pivot.transform.SetParent(root); pivot.transform.position=q+new Vector3(-3+i*3,0,-2.5f+j*5);
                var clump=ModelInstance("PROP_RiceClump",new[]{rice},pivot.transform,false); clump.name="Rice clump"; clump.transform.localScale=Vector3.one*1.5f; clump.transform.localRotation=Quaternion.Euler(0,(i*37+j*91)%360,0);
                plantList.Add(pivot.transform);
            }
        }
        PlaceSource(absTireRemains,"Rice Bounce source (remains)",new Vector3(30,0,4.5f),90);
        PlaceSource(absTankRemains,"Rice Pressure source (remains)",new Vector3(30,0,-4.5f));
        Box("Sluice platform (3 m)",new Vector3(42,1.5f,11),new Vector3(8,3,5),dark);
        // Back wall: straw blasted north drops into the channel behind the platform instead of leaving the level.
        Box("Sluice back wall (4 m)",new Vector3(42,2,15),new Vector3(16,4,.6f),dark);
        Label("เด้งขึ้นไป\nเป่าฟางออก",new Vector3(42,1.6f,8.4f),0,.18f);
        var strawItems=new List<PuzzleItem>();
        var strawX=new[]{39.5f,41.2f,42.9f,44.6f};
        for(int i=0;i<strawX.Length;i++) {
            var s=ModelInstance("PZ_StrawBundle",new[]{straw,wood},root,true); s.name="Straw bundle (2 kg)"; s.transform.position=new Vector3(strawX[i],3,11);
            var srb=s.AddComponent<Rigidbody>(); srb.mass=2; srb.linearDamping=.3f; srb.angularDamping=1;
            var si=s.AddComponent<PuzzleItem>(); si.key="straw"; strawItems.Add(si);
            // If blasted out of the level, land on the ground beside the platform (outside the zone) so progress is kept.
            si.useFallback=true; si.fallbackPosition=new Vector3(35.5f,.1f,9.5f+i*.9f);
        }
        var zoneGO=new GameObject("Sluice straw zone"); zoneGO.transform.SetParent(root); zoneGO.transform.position=new Vector3(42,3.6f,11);
        var clear=zoneGO.AddComponent<ClearZoneCondition>(); clear.key="straw"; clear.size=new Vector3(8,1.2f,5); clear.initialCount=4;
        var dryChannel=Box("Irrigation channel (dry)",new Vector3(42,.03f,14.3f),new Vector3(32,.03f,1.2f),Mat("Dry soil",new Color(.5f,.42f,.3f)),null,false);
        var wetChannel=Box("Irrigation channel (flowing)",new Vector3(42,.035f,14.3f),new Vector3(32,.03f,1.2f),Mat("Flowing water",new Color(.2f,.65f,.95f),true),null,false); wetChannel.SetActive(false);
        var sluice=ModelInstance("PZ_SluiceGate",new[]{wood,metal},root,false); sluice.name="Sluice gate"; sluice.transform.position=new Vector3(42,0,14.3f);
        Box("Pump house",new Vector3(56,1.5f,12),new Vector3(4,3,3),metal);
        var pumpLight=PointLight("Pump house light",new Vector3(56,3.6f,10)); pumpLight.intensity=0;
        var ricePuzzle=Puzzle("Puzzle Rice Straw Route","rice_straw","ฟางอุดประตูน้ำ","ดูดพลังเด้งและแรงดัน เด้งขึ้นแท่นประตูน้ำ แล้วเป่าฟางทั้ง 4 มัดออก","น้ำไหลเข้านาแล้ว ต้นข้าวกลับมาเขียว",
            new Bounds(rc+Vector3.up*3,new Vector3(34,10,30)),null,new MonoBehaviour[]{clear},strawItems.ToArray());
        ricePuzzle.celebratePoint=new Vector3(42,3.6f,11);
        var riceReaction=ricePuzzle.gameObject.AddComponent<PuzzleReaction>(); riceReaction.objective=ricePuzzle; riceReaction.duration=2.5f;
        riceReaction.showWhenSolved=new[]{wetChannel}; riceReaction.hideWhenSolved=new[]{dryChannel};
        riceReaction.mover=sluice.transform; riceReaction.moveOffset=Vector3.up*1.1f;
        riceReaction.grow=plantList.ToArray(); riceReaction.growScale=new Vector3(1.2f,2.4f,1.2f);
        riceReaction.recolor=paddyWater.ToArray(); riceReaction.solvedColor=new Color(.25f,.6f,.85f);
        riceReaction.lights=new[]{pumpLight}; riceReaction.solvedLightColor=new Color(.5f,1,.6f); riceReaction.solvedIntensity=4;
        riceReaction.stopWhenSolved=new ParticleSystem[0];
        ResetPad("Reset pad: Rice straw",new Vector3(36,0,7),ricePuzzle);
        world.puzzles=new[]{gate,vent,ricePuzzle};

        // P05 zone unlock: Rice Fields open once the whole Factory is restored (gate + turbine).
        var riceFrame=ModelInstance("PZ_FactoryGate_Frame",new[]{metal,hazard},root,false); riceFrame.name="Rice gate frame";
        riceFrame.transform.SetPositionAndRotation(new Vector3(20,0,0),Quaternion.Euler(0,90,0));
        foreach(float z in new[]{-2.3f,2.3f}) Box("Rice gate post collider",new Vector3(20,3.5f,z),new Vector3(.7f,7,.6f),metal,riceFrame.transform).GetComponent<Renderer>().enabled=false;
        var riceDoor=ModelInstance("PZ_FactoryGate_Door",new[]{dark,hazard},root,true); riceDoor.name="Rice gate door";
        riceDoor.transform.SetPositionAndRotation(new Vector3(20,0,0),Quaternion.Euler(0,90,0));
        Box("Rice gate transom",new Vector3(20.25f,5.2f,0),new Vector3(.3f,2.1f,4),dark);
        var riceLamp=Ball("Rice gate lamp",new Vector3(19.5f,7.5f,0),.45f,redLamp);
        Label("นาข้าว\nฟื้นฟูโรงงานก่อน",new Vector3(19.4f,2.2f,0),90,.16f);
        var riceGate=new GameObject("Zone gate: Rice Fields").AddComponent<ZoneGate>(); riceGate.transform.SetParent(root); riceGate.transform.position=new Vector3(19,0,0);
        riceGate.world=world; riceGate.zoneId="rice"; riceGate.zoneNameTh="ทางไปนาข้าว"; riceGate.requires=new[]{gate,vent};
        riceGate.door=riceDoor.transform; riceGate.openOffset=Vector3.up*4.4f; riceGate.lamp=riceLamp.GetComponent<Renderer>();
        world.zoneGates=new[]{riceGate};
        var hub=new GameObject("Hub progress").AddComponent<HubProgress>(); hub.transform.SetParent(root);
        hub.world=world; hub.stage1Requires=new[]{gate,vent}; hub.stage2Requires=new[]{gate,vent,ricePuzzle};
        hub.canopySmall=canopySmall; hub.canopyFull=canopyFull; hub.stage2Extras=grassPatches.ToArray();
        hub.trunk=treeTrunk; hub.core=treeCore; hub.hubLight=hubLight; hub.riceGate=riceGate; hub.hubAreaZone=hubZone;
        hub.labelLocked="HUB — ศูนย์กลาง: ฝึกดูดพลังที่ลาน (กด E ค้าง) แล้วไปฟื้นฟู FACTORY (ทิศเหนือ) — RICE FIELDS ยังล็อก";
        hub.labelUnlocked="HUB — ทางไป RICE FIELDS (ทิศตะวันออก) เปิดแล้ว! ไปฟื้นฟูนาข้าวต่อ";
        hub.labelComplete="HUB — ฟื้นฟูครบทุกโซนแล้ว! ไปที่ DEMO END ปลายนาข้าว";
        hubZone.label=hub.labelLocked;
        world.hub=hub;
        Polish(world,gate,vent,ricePuzzle,plantList,metal,rust,dark,hazard,wood,straw);

        // DEMO END
        Box("Demo end left pillar",new Vector3(56,1.5f,-2.2f),new Vector3(.5f,3,.5f),green);
        Box("Demo end right pillar",new Vector3(56,1.5f,2.2f),new Vector3(.5f,3,.5f),green);
        Box("Demo end lintel",new Vector3(56,3,0),new Vector3(.5f,.5f,4.9f),green);
        Label("จุดจบเดโม DEMO END",new Vector3(55.6f,2.2f,0),90,.3f,new Color(.3f,1,.5f));
        Zone("Demo end",BlobInteraction.Exit,new Vector3(56.5f,1.5f,0),new Vector3(2,3,4));

        world.sources=sources.ToArray();world.zones=zones.ToArray();
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
            r.sharedMaterial=n.Contains("Pupil")||n.Contains("Mouth")?black:n.Contains("Eye")?white:n.Contains("Core")?glow:n.Contains("PowerBand")?Mat("BLOB power band",new Color(.8f,.85f,.9f)):n.Contains("Cue_")?Mat("BLOB power cue",new Color(.9f,.9f,.9f)):blobBody;
            r.gameObject.layer=2;
        }
        var beam=playerGO.AddComponent<LineRenderer>();beam.sharedMaterial=Mat("Absorption stream",new Color(.3f,.85f,1),true);
        beam.positionCount=2;beam.startWidth=.055f;beam.endWidth=.02f;beam.enabled=false;
        playerGO.AddComponent<AudioSource>().spatialBlend=0;
        var player=playerGO.AddComponent<BlobPlayer>();player.world=world;player.view=cam;player.visual=visual.transform;world.player=player;
        var powers=playerGO.AddComponent<BlobPowers>();powers.set=powerSet;player.powers=powers;
        PlayerFeedback(playerGO,player,powers,world,visual.transform);
        playerGO.transform.position=world.spawn;
        cam.transform.position=new Vector3(.65f,3.3f,-5.5f);cam.transform.LookAt(new Vector3(0,1.2f,2));
        var sun=new GameObject("Directional Light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(50,-30,0);
        var fill=new GameObject("Soft fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.35f;fill.color=new Color(.6f,.75f,1);fill.transform.rotation=Quaternion.Euler(25,145,0);
        Lighting(cam);
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
            else if(!w.player.powers || !w.player.powers.set) errors.Add("BlobPowers / PowerSet not assigned");
            else {
                var set=w.player.powers.set;
                foreach(var prop in new[]{BlobProperty.Magnet,BlobProperty.Pressure,BlobProperty.Bounce})
                    if(!set.Get(prop)) errors.Add("PowerSet missing "+prop);
                if(set.combinations.Count<2||set.combinations.Count>3) errors.Add("expected 2-3 supported combinations, found "+set.combinations.Count);
            }
            if(w.puzzles==null || w.puzzles.Length!=3) errors.Add("expected 3 puzzles, found "+(w.puzzles==null?0:w.puzzles.Length));
            else foreach(var pz in w.puzzles) {
                if(!pz) { errors.Add("null puzzle entry"); continue; }
                int conds=0; foreach(var c in pz.conditions) if(c is IPuzzleCondition) conds++;
                if(conds==0) errors.Add("puzzle without conditions: "+pz.id);
                if(string.IsNullOrEmpty(pz.titleTh)||string.IsNullOrEmpty(pz.hintTh)) errors.Add("puzzle missing Thai text: "+pz.id);
                if(!pz.GetComponent<PuzzleReaction>()) errors.Add("puzzle without world reaction: "+pz.id);
                foreach(var i in pz.items) if(!i||!i.GetComponent<Collider>()&&!i.GetComponentInChildren<Collider>()) errors.Add("puzzle item missing/without collider in "+pz.id);
            }
            if(!w.uiFont || !w.uiFont.name.Contains("NotoSansThai")) errors.Add("UI font is not the bundled Noto Sans Thai");
            if(!w.ambience) errors.Add("zone ambience not wired");
            if(!Object.FindFirstObjectByType<VfxKit>()) errors.Add("VFX kit missing");
            if(w.player && (!w.player.GetComponent<BlobFeedback>() || !w.player.GetComponent<BlobExpression>())) errors.Add("BLOB feedback/expression missing");
            foreach(var tm in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if(tm.font!=w.uiFont && tm.gameObject.name!="Absorb target marker") errors.Add("sign not using the Thai font: "+tm.name);
            if(w.zoneGates==null || w.zoneGates.Length==0) errors.Add("no zone gates");
            else foreach(var g in w.zoneGates) if(!g || !g.door || g.requires==null || g.requires.Length==0 || !g.world) errors.Add("zone gate not wired: "+(g?g.name:"null"));
            if(!w.hub || !w.hub.canopySmall || !w.hub.canopyFull || !w.hub.trunk || !w.hub.hubAreaZone) errors.Add("hub progress not wired");
            foreach(var m in Object.FindObjectsByType<BlobMagnetic>(FindObjectsSortMode.None))
                if(!m.GetComponent<Collider>()) errors.Add("magnetic body without collider: "+m.name);
            if(w.sources==null) errors.Add("BlobWorld.sources is null");
            else foreach(var s in w.sources) {
                if(!s) { errors.Add("null source entry"); continue; }
                if(!s.Available) errors.Add("source not absorbable at start: "+s.name);
                if(!s.indicator) errors.Add("source has no indicator renderer: "+s.name);
                if(!s.GetComponentInChildren<Collider>()) errors.Add("source has no collider: "+s.name);
                if(s.onAbsorbed==AbsorbResult.Remnant && !s.remnantVisual) errors.Add("remnant source without remnant visual: "+s.name);
            }
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
            if(areas<3) errors.Add("expected Hub/Factory/Rice areas, found "+areas);
            if(checkpoints!=w.checkpointCount) errors.Add("checkpointCount "+w.checkpointCount+" != "+checkpoints);
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
    [MenuItem("Tools/OOPS Planet/Run P06 Polish Test")]
    public static void PolishTest() {
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode first");return; }
        var world=Object.FindFirstObjectByType<BlobWorld>();
        if(!world || world.GetComponent<OopsPolishTest>()) return;
        world.gameObject.AddComponent<OopsPolishTest>();
    }
    [MenuItem("Tools/OOPS Planet/Perf Sample (baseline)")]
    public static void PerfBaseline() => Perf("baseline");
    [MenuItem("Tools/OOPS Planet/Perf Sample (after P06)")]
    public static void PerfAfter() => Perf("after-p06");
    static void Perf(string label) {
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode first");return; }
        var world=Object.FindFirstObjectByType<BlobWorld>();
        if(!world || world.GetComponent<OopsPerfSample>()) return;
        world.UseSaveSlot("test_run",false);
        world.gameObject.AddComponent<OopsPerfSample>().label=label;
    }
    [MenuItem("Tools/OOPS Planet/Run P05 Progress Test (1: play + save)")]
    public static void ProgressTest1() => AddTest(1);
    [MenuItem("Tools/OOPS Planet/Run P05 Progress Test (2: after restarting Play Mode)")]
    public static void ProgressTest2() => AddTest(2);
    static void AddTest(int session) {
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode first");return; }
        var world=Object.FindFirstObjectByType<BlobWorld>();
        if(!world)throw new System.Exception("BlobWorld not found");
        if(world.GetComponent<OopsProgressTest>())throw new System.Exception("Progress test already exists");
        world.gameObject.AddComponent<OopsProgressTest>().session=session;
    }
    [MenuItem("Tools/OOPS Planet/Run P04 Puzzle Test")]
    public static void PuzzleTest() {
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode first");return; }
        var world=Object.FindFirstObjectByType<BlobWorld>();
        if(!world)throw new System.Exception("BlobWorld not found");
        if(world.GetComponent<OopsPuzzleTest>())throw new System.Exception("Puzzle test already exists");
        world.gameObject.AddComponent<OopsPuzzleTest>();
    }
    [MenuItem("Tools/OOPS Planet/Run P03 Power Test")]
    public static void PowerTest() {
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode first");return; }
        var world=Object.FindFirstObjectByType<BlobWorld>();
        if(!world)throw new System.Exception("BlobWorld not found");
        if(world.GetComponent<OopsPowerTest>())throw new System.Exception("Power test already exists");
        world.gameObject.AddComponent<OopsPowerTest>();
    }
    [MenuItem("Tools/OOPS Planet/Run P02 Absorb Test")]
    public static void AbsorbTest() {
        if(!EditorApplication.isPlaying) { Debug.LogError("Enter Play Mode first");return; }
        var world=Object.FindFirstObjectByType<BlobWorld>();
        if(!world)throw new System.Exception("BlobWorld not found");
        if(world.GetComponent<OopsAbsorbTest>())throw new System.Exception("Absorb test already exists");
        world.gameObject.AddComponent<OopsAbsorbTest>();
    }
}
}
#endif
