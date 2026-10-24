#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
using B=BorrowedTime.Editor.BTTrainingBuilder;
namespace BorrowedTime.Editor {
public static class BTCityBuilder {
    public const string ScenePath="Assets/_BorrowedTime/Scenes/ForgottenCity.unity";
    static BTCityGame city;
    static Transform root,nature;
    static Material Material(string key) => AssetDatabase.LoadAssetAtPath<Material>(B.Root+"/Materials/"+key+".mat");
    static void Sign(string words,Vector3 pos,float size=.14f,Transform parent=null) {
        var g=new GameObject("Sign · "+words); g.transform.position=pos; if(parent) g.transform.SetParent(parent,true); g.layer=2;
        var tm=g.AddComponent<TextMesh>(); tm.text=words; tm.font=city.uiFont; tm.fontSize=54; tm.characterSize=size; tm.anchor=TextAnchor.MiddleCenter; tm.alignment=TextAlignment.Center; tm.color=new Color(.12f,.24f,.27f);
        tm.GetComponent<Renderer>().sharedMaterial=city.uiFont.material;
    }
    static void House(Vector3 p,Vector3 size,int variant=0) {
        var par=B.Group("Abandoned building",root);
        B.Shape("Ruined plaster shell",p+Vector3.up*size.y*.5f,size,variant%2==0?"CityStone":"StoneCream",par,true);
        B.Shape("Moss covered roof",p+Vector3.up*(size.y+.1f),new Vector3(size.x+.3f,.25f,size.z+.3f),"CityRoof",par);
        int rows=Mathf.Max(1,Mathf.FloorToInt(size.y/2.2f));
        for(int r=0;r<rows;r++) for(int c=0;c<2;c++) {
            float x=p.x+(c==0?-.25f:.25f)*size.x,y=p.y+1.25f+r*2.2f;
            B.Shape("Dark abandoned window",new Vector3(x,y,p.z-size.z*.5f-.035f),new Vector3(.9f,1.3f,.1f),"CityDark",par);
            B.Shape("Window sill",new Vector3(x,y-.7f,p.z-size.z*.5f-.13f),new Vector3(1.2f,.16f,.32f),"Stone",par);
            if((r+c+variant)%3==0) { var board=B.Shape("Boarded window",new Vector3(x,y,p.z-size.z*.5f-.15f),new Vector3(1.3f,.16f,.12f),"Wood",par); board.transform.Rotate(0,0,24); }
        }
        for(int r=0;r<rows;r++) B.Shape("Side window",p+new Vector3(-size.x*.5f-.05f,1.3f+r*2.2f,0),new Vector3(.1f,1.3f,1),"CityDark",par);
        for(int i=0;i<4;i++) {
            B.Shape("Broken parapet",p+new Vector3(-size.x*.37f+i*size.x*.24f,size.y+.45f,-size.z*.4f),new Vector3(size.x*.18f,.7f,.45f),"CityStone",par);
            B.Shape("Hanging ivy",p+new Vector3(-size.x*.35f+i*size.x*.23f,size.y-.8f-i*.3f,-size.z*.5f-.18f),new Vector3(.6f,2.1f,.18f),i%2==0?"Leaf":"Moss",par,false,PrimitiveType.Sphere);
        }
        B.Bush(p+new Vector3(0,size.y+.2f,0),.8f);
        if(variant%2==0) {
            B.Shape("Faded awning",p+new Vector3(0,2.3f,-size.z*.5f-.7f),new Vector3(size.x*.8f,.18f,1.4f),"CityTerracotta",par);
            foreach(float x in new[]{-.33f,.33f}) B.Shape("Awning support",p+new Vector3(x*size.x,1.1f,-size.z*.5f-1.1f),new Vector3(.11f,2.2f,.11f),"Wood",par,true);
        }
    }
    static void Pipe(Vector3 a,Vector3 b,float diameter,string mat,Transform parent) { B.Beam("Water distribution pipe",a,b,diameter,mat,parent,true); }
    static BTTimeTarget Source(Vector3 p,float scale=.48f) {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(B.Root+"/Prefabs/LivingEnergyTree.prefab");
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab); g.name="Renewable city energy source"; g.transform.SetParent(nature,true); g.transform.position=p; g.transform.localScale=Vector3.one*scale;
        var t=g.GetComponent<BTTimeTarget>(); t.game=city; t.displayName="พืชฟื้นพลังในเมือง"; t.ResetTarget(); PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        return t;
    }
    static void Park() {
        var t=B.Target("ต้นไม้กลางสวน",TargetKind.LivingTime,new Vector3(-18,0,-2),2,new[]{"ต้นอ่อน","โตเต็มวัย","เหี่ยวเฉา"}); city.tree=t; t.states=new GameObject[3];
        for(int i=0;i<3;i++) {
            var state=B.Group("Tree age "+i,t.transform); t.states[i]=state.gameObject;
            if(i<2) {
                var tree=B.Tree("Park tree "+i,t.transform.position,i==0?.38f:1.15f); tree.SetParent(state,true);
            } else {
                B.Shape("Withered trunk",new Vector3(-18,1.9f,-2),new Vector3(.8f,1.9f,.8f),"Bark",state,true,PrimitiveType.Cylinder);
                for(int j=0;j<6;j++) {
                    float a=j*1.0472f;
                    Vector3 end=new Vector3(-18+Mathf.Sin(a)*2.3f,3.3f+(j%2)*.7f,-2+Mathf.Cos(a)*2.3f);
                    B.Beam("Bare branch",new Vector3(-18,1.9f,-2),end,.25f,"Bark",state);
                    B.Beam("Bare twig",Vector3.Lerp(new Vector3(-18,1.9f,-2),end,.7f),end+new Vector3(.3f,.9f,.2f),.13f,"Bark",state);
                }
            }
        }
        B.TargetCrystal(t,new Vector3(-18,1.25f,-2.55f),"GlowGreen"); t.Apply();
        B.Shape("Circular park plaza",new Vector3(-18,.015f,-2),new Vector3(10,.025f,10),"Path",root,false,PrimitiveType.Cylinder);
        B.Ring("Park stone border",new Vector3(-18,.07f,-2),5.1f,"StoneCream",root);
        city.parkBloom=B.Group("Restored public garden",root).gameObject;
        for(int i=0;i<10;i++) {
            float a=i*Mathf.PI*.2f; var p=new Vector3(-18+Mathf.Sin(a)*3.8f,0,-2+Mathf.Cos(a)*3.8f);
            var bush=B.Shape("Restored flowering shrub",p+Vector3.up*.3f,new Vector3(1.1f,.65f,.85f),"LeafLight",city.parkBloom.transform,false,PrimitiveType.Sphere);
            B.Shape("Park blossom",p+Vector3.up*.65f,new Vector3(.35f,.18f,.35f),i%2==0?"Coral":"Gold",city.parkBloom.transform,false,PrimitiveType.Sphere);
        }
        for(int side=0;side<2;side++) {
            Vector3 p=new Vector3(-23,0,side==0?-5:1);
            B.Shape("Park bench seat",p+Vector3.up*.55f,new Vector3(.75f,.18f,2.2f),"WoodLight",root,true);
            B.Shape("Park bench back",p+new Vector3(-.35f,1,0),new Vector3(.15f,.85f,2.2f),"Wood",root);
        }
        Sign("สวนสาธารณะ\nPUBLIC GARDEN",new Vector3(-21.3f,1.2f,-7.3f),.09f,root);
        city.parkBloom.SetActive(false);
    }
    static void Waterworks() {
        House(new Vector3(-12,0,20),new Vector3(9,7,6),1);
        Sign("CITY WATERWORKS",new Vector3(-12,4.4f,16.9f),.13f,root);
        var t=B.Target("เครื่องควบคุมระบบน้ำ",TargetKind.Mechanism,new Vector3(-10.5f,0,14),2,new[]{"ใหม่","ทำงานได้","พัง"}); city.waterMachine=t; t.states=new GameObject[3];
        for(int s=0;s<3;s++) {
            var par=B.Group("Water machine age "+s,t.transform); t.states[s]=par.gameObject;
            B.Shape("Pump base",new Vector3(-10.5f,.3f,14),new Vector3(2.5f,.6f,1.9f),"Stone",par,true);
            B.Shape("Pump casing",new Vector3(-10.5f,1.2f,14),new Vector3(1.5f,1.6f,1.4f),s==2?"Rust":"Metal",par,true);
            B.Shape("Pump turbine",new Vector3(-10.5f,1.5f,13.2f),new Vector3(1,.25f,1),s==2?"Rust":"Gold",par,false,PrimitiveType.Cylinder).transform.rotation=Quaternion.Euler(90,0,s==2?24:0);
            B.Shape("Status lamp",new Vector3(-11.05f,2.1f,13.3f),new Vector3(.2f,.2f,.2f),s==2?"RedGlow":"GlowGreen",par,false,PrimitiveType.Sphere);
            if(s==2) B.Beam("Broken pipe",new Vector3(-9.5f,1.1f,14),new Vector3(-8.8f,.3f,14.2f),.3f,"Rust",par);
        }
        B.TargetCrystal(t,new Vector3(-10.5f,1.5f,12.95f),"GlowBlue"); t.Apply();
        city.waterConsole=B.Group("Water diversion valve",root); city.waterConsole.position=new Vector3(-8,0,14);
        B.Shape("Valve pedestal",new Vector3(-8,.6f,14),new Vector3(.6f,1.2f,.7f),"Metal",city.waterConsole,true);
        B.Ring("Water valve wheel",new Vector3(-8,1.4f,13.6f),.45f,"Coral",city.waterConsole,true);
        Sign("F\nแยกน้ำเสีย",new Vector3(-8,2.1f,13.5f),.08f,city.waterConsole);
        Pipe(new Vector3(-9,.5f,14.5f),new Vector3(-4,.5f,14.5f),.55f,"Rust",root);
        Pipe(new Vector3(-4,.5f,14.5f),new Vector3(-4,-2,14.5f),.55f,"Rust",root);
        Pipe(new Vector3(-13,.12f,13),new Vector3(-18,.12f,4),.2f,"Rust",root);
        Pipe(new Vector3(-18,.12f,4),new Vector3(-18,.12f,2),.2f,"Rust",root);
        city.pollutedWater=B.Group("Contaminated canal and park feed",root).gameObject;
        city.cleanWater=B.Group("Clean district canal and garden feed",root).gameObject;
        foreach(bool clean in new[]{false,true}) {
            var par=clean?city.cleanWater.transform:city.pollutedWater.transform;
            B.Shape("Canal water",new Vector3(1,-4.1f,4),new Vector3(8,.2f,70),clean?"Water":"Pollution",par);
            for(int i=0;i<7;i++) B.Shape("Canal current",new Vector3(-1+(i%3)*1.7f,-3.95f,-20+i*8),new Vector3(.35f,.04f,1.7f),clean?"Foam":"PollutedGlow",par);
            B.Shape("Garden irrigation channel",new Vector3(-18,.06f,3),new Vector3(.65f,.09f,7),clean?"Water":"Pollution",par);
            if(!clean) for(int i=0;i<4;i++) B.Shape("Pollution pool",new Vector3(-18+(i%2)*1.4f,.055f,-1+i*.8f),new Vector3(1.6f,.05f,1.1f),"Pollution",par,false,PrimitiveType.Sphere);
            else B.Shape("Restored fountain water",new Vector3(-18,.1f,3.6f),new Vector3(2,.08f,1.4f),"Water",par,false,PrimitiveType.Sphere);
        }
        city.cleanWater.SetActive(false);
    }
    static void Crossing() {
        var t=B.Target("สะพานข้ามคลอง",TargetKind.Bridge,new Vector3(0,0,0),2,new[]{"สมบูรณ์","ผุกร่อน","พัง"}); city.bridge=t; t.states=new GameObject[3];
        for(int s=0;s<3;s++) {
            var par=B.Group("Canal bridge age "+s,t.transform); t.states[s]=par.gameObject;
            for(int i=0;i<21;i++) {
                if(s==2 && i>5 && i<15) continue;
                var g=B.Shape("Canal crossing deck",new Vector3(-3.7f+i*.46f,.04f,8),new Vector3(.44f,.27f,3.5f),s==0?"StoneCream":"Wood",par,true);
                if(s>0) g.transform.Rotate((i%3-1)*5,0,(i%2==0?1:-1)*s*2);
            }
            if(s<2) foreach(float z in new[]{6.4f,9.6f}) B.Beam("Canal bridge beam",new Vector3(-3.8f,-.25f,z),new Vector3(5.7f,-.25f,z),.28f,"Stone",par);
        }
        foreach(float z in new[]{6.25f,9.75f}) {
            for(int i=0;i<5;i++) B.Shape("Canal bridge post",new Vector3(-3.7f+i*2.3f,.65f,z),new Vector3(.3f,1.4f,.3f),"Stone",t.transform);
            B.Beam("Canal bridge rope",new Vector3(-3.7f,1.1f,z),new Vector3(5.5f,1.1f,z),.08f,"Rope",t.transform);
        }
        B.TargetCrystal(t,new Vector3(-4.2f,1.25f,10.3f),"GlowBlue"); t.Apply();
    }
    static void OptionalGarden() {
        var par=B.Group("Optional memorial garden",root);
        foreach(float x in new[]{18,28}) B.Shape("Small garden boundary",new Vector3(x,1.3f,-1),new Vector3(.6f,2.6f,8),"Stone",par,true);
        B.Shape("Garden rear wall",new Vector3(23,1.3f,-4.6f),new Vector3(10,2.6f,.6f),"Stone",par,true);
        foreach(float x in new[]{19.5f,26.5f}) B.Shape("Garden front wing",new Vector3(x,1.3f,3),new Vector3(3,2.6f,.6f),"Stone",par,true);
        var t=B.Target("เถาวัลย์สวนเล็ก",TargetKind.Seal,new Vector3(23,0,3),1,new[]{"อ่อนวัย","ขวางทาง","แห้งสลาย"}); city.optionalVines=t; t.states=new GameObject[3];
        for(int s=0;s<3;s++) {
            var state=B.Group("Garden vine age "+s,t.transform); t.states[s]=state.gameObject;
            if(s<2) {
                B.Shape("Vine barrier collider",new Vector3(23,1.3f,3),new Vector3(4,2.6f,.45f),"Leaf",state,true);
                for(int i=0;i<4;i++) B.Shape("Garden entwined leaves",new Vector3(21.7f+i*.85f,1.2f+(i%2)*.5f,2.7f),new Vector3(.8f,1.6f,.22f),"LeafLight",state,false,PrimitiveType.Sphere);
            } else B.Shape("Garden dry roots",new Vector3(23,.1f,3),new Vector3(3.5f,.12f,.5f),"Bark",state);
        }
        B.TargetCrystal(t,new Vector3(24.6f,1.3f,2.65f),"GlowGreen"); t.Apply();
        city.note=B.Group("Norman field notebook",par); city.note.position=new Vector3(23,.8f,-1.3f);
        B.Shape("Memorial table",new Vector3(23,.45f,-1.3f),new Vector3(1.7f,.9f,1.2f),"StoneCream",par,true);
        B.Shape("Field notebook",new Vector3(23,.96f,-1.3f),new Vector3(.7f,.09f,.55f),"Ivory",city.note);
        B.Ring("Optional reward halo",new Vector3(23,1.1f,-1.3f),.6f,"GlowGold",city.note);
        city.optionalBloom=B.Group("Restored memorial flowers",par).gameObject;
        for(int i=0;i<8;i++) B.Shape("Restored memorial flower",new Vector3(19.5f+(i%4)*2.2f,.4f,-3+(i/4)*3),new Vector3(.65f,.8f,.65f),i%2==0?"Coral":"Gold",city.optionalBloom.transform,false,PrimitiveType.Sphere);
        city.optionalBloom.SetActive(false);
        Sign("สวนเล็ก / บันทึก Norman",new Vector3(23,3.2f,2.7f),.08f,par);
    }
    static void Factory() {
        var par=B.Group("Factory approach · Level 3 boundary",root);
        B.Shape("Factory main hall",new Vector3(22,4,30),new Vector3(19,8,7),"Factory",par,true);
        B.Shape("Factory roof",new Vector3(22,8.2f,30),new Vector3(20,.45f,8),"FactoryDark",par);
        foreach(float x in new[]{16,28}) B.Shape("Factory gate wing",new Vector3(x,3,24),new Vector3(6,6,1.2f),"FactoryDark",par,true);
        B.Shape("Factory lintel",new Vector3(22,5.7f,24),new Vector3(7,1.4f,1.4f),"Rust",par,true);
        city.door=B.Shape("Locked factory door",new Vector3(22,2.5f,24),new Vector3(6,5,.8f),"Gate",par,true).transform;
        foreach(float x in new[]{19.4f,24.6f}) {
            B.Shape("Red gate signal",new Vector3(x,3.2f,23.5f),new Vector3(.14f,3.5f,.12f),"RedGlow",par);
            B.Shape("Factory entry pier",new Vector3(x,3,24.5f),new Vector3(.75f,6.3f,1.8f),"Stone",par,true);
        }
        for(int i=0;i<5;i++) B.Shape("Factory window",new Vector3(15+i*3.4f,6.6f,26.45f),new Vector3(1.2f,1.4f,.12f),"RedGlow",par);
        foreach(float x in new[]{16.8f,27.5f}) {
            B.Shape("Factory chimney",new Vector3(x,12,30),new Vector3(1.6f,9,1.6f),"Rust",par,true);
            B.Shape("Chimney collar",new Vector3(x,16.4f,30),new Vector3(2,.6f,2),"FactoryDark",par);
            for(int i=0;i<6;i++) B.Shape("Factory smoke",new Vector3(x+i*.4f,17+i*1.2f,30+i*.3f),new Vector3(2+i*.3f,1.8f+i*.2f,2+i*.2f),"Smoke",par,false,PrimitiveType.Sphere);
        }
        for(int i=0;i<7;i++) B.Shape("Factory parapet",new Vector3(13.8f+i*2.7f,8.8f,26.8f),new Vector3(.5f,1.4f,.6f),"FactoryDark",par);
        city.factoryConsole=B.Group("Factory access console",par); city.factoryConsole.position=new Vector3(27,0,21);
        B.Shape("Gate console stand",new Vector3(27,.6f,21),new Vector3(1.2f,1.2f,.85f),"Metal",city.factoryConsole,true);
        B.Shape("Gate console screen",new Vector3(27,1.4f,20.9f),new Vector3(1.1f,.7f,.18f),"GlowBlue",city.factoryConsole);
        Sign("F\nประตูโรงงาน",new Vector3(27,2.3f,20.7f),.09f,city.factoryConsole);
        Sign("ERI  /  INDUSTRIAL DISTRICT",new Vector3(22,5.7f,23.24f),.095f,par);
        var t=B.Target("สิ่งกีดขวางหน้าโรงงาน",TargetKind.Seal,new Vector3(22,0,18.5f),0,new[]{"แข็งแรง","เปราะบาง","สลายแล้ว"}); city.seal=t; t.states=new GameObject[3];
        for(int s=0;s<3;s++) {
            var state=B.Group("Obstruction age "+s,t.transform); t.states[s]=state.gameObject;
            if(s<2) {
                B.Shape("Old fallen gate",new Vector3(22,s==0?1.1f:.85f,18.5f),new Vector3(5,s==0?2.2f:1.7f,1),s==0?"Stone":"Rust",state,true);
                for(int i=0;i<4;i++) B.Shape("Cracked gate block",new Vector3(20.3f+i*1.1f,s==0?2.2f:1.7f,18.5f),new Vector3(.75f,.45f,1.1f),"StoneCream",state);
            } else for(int i=0;i<5;i++) B.Shape("Crumbled gate stone",new Vector3(20.4f+i*.8f,.12f,18.5f),new Vector3(.6f,.2f,.65f),"Stone",state);
        }
        B.TargetCrystal(t,new Vector3(24.8f,1.3f,17.8f),"GlowGreen"); t.Apply();
    }
    [MenuItem("Tools/Borrowed Time/Build Forgotten City")]
    public static void Build() {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building");
        B.Folder(B.Root+"/Backups");
        var original=EditorSceneManager.GetActiveScene(); if(original.isDirty) EditorSceneManager.SaveScene(original);
        if(System.IO.File.Exists(ScenePath)) AssetDatabase.CopyAsset(ScenePath,B.Root+"/Backups/City_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity");
        var training=EditorSceneManager.OpenScene(BTJourney.TrainingPath,OpenSceneMode.Single);
        var template=Object.FindFirstObjectByType<BTPlayer>();
        if(!template || !template.game.robo) throw new InvalidOperationException("Training player and Robo are required");
        var oldCamera=template.view; var oldRobot=template.game.robo; var font=template.game.uiFont;
        var sunTemplate=RenderSettings.sun; var oldVolume=Object.FindFirstObjectByType<Volume>();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
        city=new GameObject("Forgotten City Systems").AddComponent<BTCityGame>(); city.uiFont=font;
        var player=Object.Instantiate(template.gameObject).GetComponent<BTPlayer>(); player.name="Protagonist"; player.transform.position=city.spawn; player.game=city; city.player=player;
        var camera=Object.Instantiate(oldCamera.gameObject).GetComponent<Camera>(); camera.name="Main Camera"; player.view=camera;
        var robot=Object.Instantiate(oldRobot.gameObject).transform; robot.name="ROBO"; robot.SetParent(null,true); robot.position=city.spawn+new Vector3(1.2f,2,0); city.robo=robot;
        var sun=Object.Instantiate(sunTemplate.gameObject).GetComponent<Light>(); sun.name="City afternoon sun"; sun.intensity=1.3f; RenderSettings.sun=sun;
        if(oldVolume) { var vol=Object.Instantiate(oldVolume.gameObject); vol.name="City colour and bloom"; }
        root=B.Group("BORROWED TIME · The Forgotten City"); nature=B.Group("Nature reclaiming the city",root); B.BeginCity(city,root,nature);
        B.Island("City west district",-31,-3,-25,24,0); B.Island("City east district",5,33,-7,35,0);
        B.Path("City arrival street",new[]{new Vector3(-23,0,-19),new Vector3(-22,0,-13),new Vector3(-19,0,-8),new Vector3(-18,0,-4)},4);
        B.Path("Public garden to waterworks",new[]{new Vector3(-18,0,2),new Vector3(-16,0,7),new Vector3(-12,0,11),new Vector3(-10,0,14)},4);
        B.Path("West canal crossing street",new[]{new Vector3(-15,0,8),new Vector3(-8,0,8),new Vector3(-3.5f,0,8)},4);
        B.Path("East factory boulevard",new[]{new Vector3(5.3f,0,8),new Vector3(12,0,8),new Vector3(17,0,10),new Vector3(22,0,15),new Vector3(22,0,23.5f)},4.5f);
        B.Path("Optional garden side street",new[]{new Vector3(16,0,8),new Vector3(22,0,5),new Vector3(23,0,1)},3.2f);
        Park(); Waterworks(); Crossing(); OptionalGarden(); Factory();
        House(new Vector3(-27,0,-18),new Vector3(5,7,6),0); House(new Vector3(-12,0,-18),new Vector3(7,8,6),1);
        House(new Vector3(-27,0,-8),new Vector3(5,9,6),2); House(new Vector3(-7.5f,0,-10),new Vector3(5,7,7),3);
        House(new Vector3(-27,0,3),new Vector3(4.5f,6,8),3); House(new Vector3(-24,0,14),new Vector3(5,7,6),1);
        House(new Vector3(-23,0,21),new Vector3(6,9,4),2);
        House(new Vector3(10,0,-2),new Vector3(6,6,6),1); House(new Vector3(29,0,8),new Vector3(5,9,7),3);
        House(new Vector3(10,0,19),new Vector3(5,9,8),1); House(new Vector3(15,0,17),new Vector3(4,6,4),0);
        foreach(var p in new[]{new Vector3(-28,0,-1),new Vector3(-9,0,-20),new Vector3(-25,0,9),new Vector3(-20,0,16),new Vector3(7,0,13),new Vector3(31,0,15),new Vector3(17,0,-4)}) B.Tree("City shade tree",p,.75f);
        Source(new Vector3(-23,0,-12),.5f); Source(new Vector3(-13.5f,0,10),.48f); Source(new Vector3(9,0,5),.5f); Source(new Vector3(29.5f,0,17),.48f);
        B.Fence(new Vector3(-4.5f,0,-22),new Vector3(-4.5f,0,5.7f)); B.Fence(new Vector3(-4.5f,0,11),new Vector3(-4.5f,0,21));
        B.Fence(new Vector3(6.5f,0,-4.5f),new Vector3(6.5f,0,5.7f)); B.Fence(new Vector3(6.5f,0,11),new Vector3(6.5f,0,29));
        for(int i=0;i<9;i++) {
            float x=i<5?-29+i*5.2f:7+(i-5)*7;
            float z=i<5?-22:-5;
            B.Bush(new Vector3(x,0,z),.8f); B.Flowers(new Vector3(x+1,0,z+.3f));
        }
        B.Ruin(new Vector3(-29,0,-24),3); B.Ruin(new Vector3(-7,0,3),3); B.Ruin(new Vector3(29,0,-5),4);
        // A recognizable canal fall and skyline keep the route visible from the garden.
        for(int i=0;i<5;i++) B.Shape("Canal waterfall",new Vector3(-1.7f+i*1.25f,-1.9f,24),new Vector3(1.1f,4,.2f),"Water",root);
        foreach(float x in new[]{-45,-23,0,25,47}) {
            B.Shape("Distant city hill",new Vector3(x,-6,54),new Vector3(18,21+(x%3)*2,12),"Distant",root,false,PrimitiveType.Sphere);
            for(int i=0;i<3;i++) B.Shape("City cloud",new Vector3(x+i*2.6f,19+(i%2),57),new Vector3(7,3,4),"Ivory",root,false,PrimitiveType.Sphere);
        }
        Sign("BORROWED TIME\nTHE FORGOTTEN CITY",new Vector3(-23,3,-22.5f),.16f,root);
        city.mapCameraPosition=new Vector3(-52,59,-62); city.mapCameraTarget=new Vector3(0,1,5); city.mapCameraSize=45;
        city.markers=new[]{city.spawn,new Vector3(-18,0,-2),new Vector3(-10,0,14),new Vector3(-18,0,-2),new Vector3(-4.2f,0,10.3f),new Vector3(24,0,20)};
        camera.transform.position=new Vector3(-46,48,-52); camera.transform.LookAt(new Vector3(0,1,5)); camera.orthographic=true; camera.orthographicSize=39;
        RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.7f,.86f,.9f); RenderSettings.ambientEquatorColor=new Color(.65f,.77f,.69f); RenderSettings.ambientGroundColor=new Color(.42f,.54f,.49f);
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogColor=new Color(.67f,.82f,.85f); RenderSettings.fogStartDistance=120; RenderSettings.fogEndDistance=230;
        EditorSceneManager.SaveScene(scene,ScenePath); EditorSceneManager.CloseScene(training,true); SceneManager.SetActiveScene(scene);
        RegisterJourneyScenes(); AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,1,5),Quaternion.Euler(43,28,0),48);
        Debug.Log("BORROWED_TIME_CITY_READY "+ScenePath);
    }
    [MenuItem("Tools/Borrowed Time/Register journey scenes")]
    public static void RegisterJourneyScenes() {
        // Scene transitions require registration. Preserve every existing entry and append our two scenes.
        var builds=new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach(var path in new[]{BTJourney.TrainingPath,ScenePath,BTJourney.FactoryPath}) {
            if(!System.IO.File.Exists(path)) continue;
            var found=builds.Find(s=>s.path==path); if(found==null) builds.Add(new EditorBuildSettingsScene(path,true)); else found.enabled=true;
        }
        EditorBuildSettings.scenes=builds.ToArray();
    }
}
}
#endif
