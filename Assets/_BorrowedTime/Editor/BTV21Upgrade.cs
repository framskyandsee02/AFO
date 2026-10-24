#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
using B=BorrowedTime.Editor.BTTrainingBuilder;
namespace BorrowedTime.Editor {
public static class BTV21Upgrade {
    public const string PrototypePath="Assets/_BorrowedTime/Scenes/FunPrototype.unity";
    static Mesh cube;
    static Transform root;
    static GameObject Deck(string n,Vector3 p,Vector3 size,Transform parent,string mat="Leaf"){
        var g=B.Shape(n,p,size,mat,parent,true);g.GetComponent<MeshFilter>().sharedMesh=cube;return g;
    }
    static void Init(BTTrainingGame g){root=B.Group("v2.1 fun-first additions");B.BeginCity(g,root,root);cube=AssetDatabase.LoadAssetAtPath<Mesh>(B.Root+"/Art/Meshes/FactoryUnitCube.asset");}
    static void Save(){EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
    static void Backup(string path){AssetDatabase.CopyAsset(path,B.Root+"/Backups/v21_"+System.IO.Path.GetFileNameWithoutExtension(path)+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity");}
    static void Sign(string words,Vector3 p){var t=B.Group(words,root);t.position=p;t.gameObject.layer=2;var m=t.gameObject.AddComponent<TextMesh>();m.text=words;m.font=Object.FindFirstObjectByType<BTTrainingGame>().uiFont;m.fontSize=52;m.characterSize=.07f;m.anchor=TextAnchor.MiddleCenter;m.color=Color.white;m.GetComponent<Renderer>().sharedMaterial=m.font.material;}
    static BTTimeTarget GrowTree(BTTrainingGame g,Vector3 p){var t=B.Target("ต้นอ่อนแห่งความหวัง",TargetKind.LivingTime,p,0,new[]{"ต้นอ่อน","โตเต็มวัย"},root);t.absorbAllowed=false;t.states=new GameObject[2];for(int i=0;i<2;i++){var s=B.Group("Wonder age "+i,t.transform);t.states[i]=s.gameObject;B.Tree("Growing hope",p,i==0?.16f:.85f,true).SetParent(s,true);}B.TargetCrystal(t,p+new Vector3(0,1.3f,-.65f),"GlowGreen");t.Apply();return t;}
    public static BTTimeTarget NaturalCrossing(Vector3 p,float length){
        var t=B.Target("รากไม้ข้ามคลอง",TargetKind.Vine,p,0,new[]{"รากอ่อน","ทางรากไม้","รกเกินข้าม"},root);t.states=new GameObject[3];
        for(int i=0;i<3;i++){var s=B.Group("Natural crossing age "+i,t.transform);t.states[i]=s.gameObject;
            if(i==1){Deck("Continuous living crossing",p+new Vector3(length*.5f,.025f,0),new Vector3(length,.25f,2.5f),s,"Moss");foreach(float z in new[]{-1.05f,1.05f}){B.Beam("Living root rail",p+new Vector3(0,.65f,z),p+new Vector3(length,.65f,z),.18f,"Bark",s,true);}}
            else {B.Shape("Root waiting to grow",p+new Vector3(.45f,.25f,0),new Vector3(.8f,.5f,1.5f),"Leaf",s);if(i==2)Deck("Overgrown entry",p+new Vector3(.5f,1,0),new Vector3(1,2,2.6f),s,"Leaf");}
        }B.TargetCrystal(t,p+new Vector3(-1,1.2f,1.7f),"GlowGreen");t.Apply();return t;
    }
    [MenuItem("Tools/Borrowed Time/Apply v2.1 fun-first upgrade")]
    public static void Apply(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode");
        foreach(var path in new[]{BTJourney.TrainingPath,BTJourney.CityPath,BTJourney.FactoryPath})Backup(path);
        EditorSceneManager.OpenScene(BTJourney.TrainingPath);var training=Object.FindFirstObjectByType<BTTrainingGame>();if(!training.wonderTree){Init(training);training.wonderTree=GrowTree(training,new Vector3(-18.3f,0,-1.4f));Sign("เร่งต้นอ่อน แล้วคืนสะพาน\nGROW • THEN REWIND",new Vector3(-18.3f,2.8f,-2));EditorUtility.SetDirty(training);Save();}
        EditorSceneManager.OpenScene(BTJourney.CityPath);var city=Object.FindFirstObjectByType<BTCityGame>();if(!city.natureCrossing){Init(city);
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){var p=r.transform.position;if(r.name.StartsWith("Fence ")&&(Mathf.Abs(p.x+4.5f)<.3f||Mathf.Abs(p.x-6.5f)<.3f)&&p.z<6)Object.DestroyImmediate(r.gameObject);}
            B.Fence(new Vector3(-4.5f,0,-22),new Vector3(-4.5f,0,2.5f),root);B.Fence(new Vector3(6.5f,0,-4.5f),new Vector3(6.5f,0,2.5f),root);
            city.natureCrossing=NaturalCrossing(new Vector3(-4,0,4.5f),10);Sign("สองทางข้าม / TWO WAYS\nสะพาน: คืนสภาพ • รากไม้: เร่งเติบโต",new Vector3(-6.2f,2.8f,6.2f));Sign("น้ำเสียทำให้รากป่วย\nอายุและสุขภาพเป็นคนละอย่าง",new Vector3(-20.5f,2,-.5f));EditorUtility.SetDirty(city);Save();}
        EditorSceneManager.OpenScene(BTJourney.FactoryPath);var factory=Object.FindFirstObjectByType<BTFactoryGame>();Init(factory);factory.funFirst=true;
        if(!factory.emergencyWalkway){
            var old=factory.ringCrossing;var t=B.Target("ทางเดินฉุกเฉินวงแหวน",TargetKind.Bridge,new Vector3(0,6,25),2,new[]{"สมบูรณ์","ชำรุด","ขาด"},root);factory.emergencyWalkway=t;t.states=new GameObject[3];old.transform.SetParent(t.transform,true);old.SetActive(true);t.states[0]=old;
            for(int i=1;i<3;i++){var state=B.Group("Emergency walkway age "+i,t.transform);t.states[i]=state.gameObject;if(i==1)Deck("Worn emergency deck",new Vector3(0,5.75f,25),new Vector3(16,.5f,4),state,"Rust");}
            B.TargetCrystal(t,new Vector3(10.5f,7.3f,23.8f),"GlowBlue");t.Apply();factory.ringCrossing=t.gameObject;factory.ringCrossing.SetActive(false);
            factory.smokestacks=new GameObject[3];for(int i=0;i<3;i++){var smoke=B.Group("Discharge stack smoke "+i,root);factory.smokestacks[i]=smoke.gameObject;for(int j=0;j<5;j++)B.Shape("Factory discharge cloud",new Vector3(-16+i*10+j*.3f,19+j*.8f,30),new Vector3(2+j*.2f,1.2f,2),"Smoke",smoke,false,PrimitiveType.Sphere);}
            Sign("แรงดันสูง • ไม่มีเวลานับถอยหลัง\nคืนทางเดิน แล้วข้ามไปห้องควบคุม",new Vector3(13,9,23));
            var growth=factory.miracleTree.states[2];foreach(Transform child in growth.transform)child.localScale*=1.6f;
        }EditorUtility.SetDirty(factory);Save();BuildPrototype();
        EditorSceneManager.OpenScene(BTJourney.TrainingPath);Debug.Log("BT_V21_UPGRADE_READY");
    }
    static void BuildPrototype(){
        if(System.IO.File.Exists(PrototypePath))return;
        EditorSceneManager.OpenScene(BTJourney.TrainingPath);var template=Object.FindFirstObjectByType<BTPlayer>();var oldGame=template.game;var oldSun=RenderSettings.sun;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);var g=new GameObject("Fun prototype systems").AddComponent<BTFunPrototypeGame>();g.uiFont=oldGame.uiFont;g.player=Object.Instantiate(template);g.player.game=g;g.player.transform.position=new Vector3(-9,.15f,-6);g.player.view=Object.Instantiate(template.view);g.player.view.name="Main Camera";g.robo=Object.Instantiate(oldGame.robo);g.robo.position=g.player.transform.position+Vector3.up*2;
        Init(g);Deck("West laboratory garden",new Vector3(-7,-.25f,0),new Vector3(10,.5f,16),root,"Moss");Deck("East laboratory garden",new Vector3(10,-.25f,0),new Vector3(12,.5f,16),root,"Moss");
        var source=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(B.Root+"/Prefabs/LivingEnergyTree.prefab")) as GameObject;source.transform.position=new Vector3(-8,0,-3);source.transform.localScale=Vector3.one*.45f;source.GetComponent<BTTimeTarget>().game=g;
        var target=B.Target("ทดลองคืนสะพาน",TargetKind.Bridge,new Vector3(0,0,-1),2,new[]{"สมบูรณ์","ผุกร่อน","ขาด"},root);g.bridge=target;target.states=new GameObject[3];for(int i=0;i<3;i++){var s=B.Group("Prototype bridge age "+i,target.transform);target.states[i]=s.gameObject;if(i<2)Deck("Prototype crossing",new Vector3(1,-.075f,-1),new Vector3(6,.25f,2.5f),s,"Wood");}B.TargetCrystal(target,new Vector3(-2.6f,1.3f,-2.4f),"GlowBlue");target.Apply();g.vine=NaturalCrossing(new Vector3(-2,0,4),6);
        var tree=B.Target("ต้นไม้ข้างท่อน้ำเสีย",TargetKind.LivingTime,new Vector3(-6,0,4),2,new[]{"ต้นอ่อน","โตเต็มวัย","เหี่ยวเฉา"},root);g.tree=tree;tree.states=new GameObject[3];for(int i=0;i<3;i++){var s=B.Group("Stress demonstration age "+i,tree.transform);tree.states[i]=s.gameObject;var t=B.Tree("Study tree",tree.transform.position,i==0?.15f:.5f);t.SetParent(s,true);if(i==2)foreach(var r in t.GetComponentsInChildren<Renderer>())if(r.name=="Rounded canopy")r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(B.Root+"/Materials/Wilt.mat");}B.TargetCrystal(tree,new Vector3(-6,1.3f,3),"GlowGreen");tree.Apply();
        g.pump=B.Target("เครื่องสูบที่พัง",TargetKind.Mechanism,new Vector3(10,0,-4),2,new[]{"ใหม่","ทำงานได้","เสีย"},root);g.pump.states=new GameObject[3];for(int i=0;i<3;i++){var s=B.Group("Pump age "+i,g.pump.transform);g.pump.states[i]=s.gameObject;Deck("Pump casing",new Vector3(10,.6f,-4),new Vector3(1.3f,1.2f,1),s,i==2?"Rust":"Metal");}B.TargetCrystal(g.pump,new Vector3(10,1.3f,-4.6f),"GlowBlue");g.pump.Apply();g.valve=B.Group("F — วาล์วน้ำเสีย",root);g.valve.position=new Vector3(12,0,-4);Deck("Valve pedestal",new Vector3(12,.5f,-4),new Vector3(.6f,1,.6f),g.valve,"Metal");
        g.polluted=B.Shape("Toxic runoff",new Vector3(1,-1.2f,0),new Vector3(6,.1f,16),"Pollution",root);g.clean=B.Shape("Clean stream",new Vector3(1,-1.2f,0),new Vector3(6,.1f,16),"Water",root);g.clean.SetActive(false);g.bloom=B.Group("Prototype restored flowers",root).gameObject;for(int i=0;i<12;i++)B.Flowers(new Vector3(-9+i*.55f,0,6));foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.name=="Flower")r.transform.SetParent(g.bloom.transform,true);g.bloom.SetActive(false);
        var sun=Object.Instantiate(oldSun);sun.name="Prototype sun";RenderSettings.sun=sun;RenderSettings.ambientLight=new Color(.65f,.75f,.7f);g.mapCameraPosition=new Vector3(-16,25,-22);g.mapCameraTarget=Vector3.zero;g.mapCameraSize=18;g.player.view.transform.position=g.mapCameraPosition;g.player.view.transform.LookAt(g.mapCameraTarget);g.player.view.orthographic=true;g.player.view.orthographicSize=18;
        Sign("FUN PROTOTYPE\nต้นเหตุ • สองทางเลือก • ผลของการฟื้นฟู",new Vector3(-7,3,-7));EditorSceneManager.SaveScene(scene,PrototypePath);EditorSceneManager.CloseScene(oldGame.gameObject.scene,true);AssetDatabase.SaveAssets();
    }
}
}
#endif
