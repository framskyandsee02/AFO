#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTTrainingBuilder {
    internal static void BeginCity(BTTrainingGame city,Transform cityRoot,Transform natureRoot) {
        game=city; environment=cityRoot; details=natureRoot; meshId=1000;
        mats=new Dictionary<string,Material>();
        foreach(var id in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"})) {
            string path=AssetDatabase.GUIDToAssetPath(id); var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m) mats[System.IO.Path.GetFileNameWithoutExtension(path)]=m;
        }
        rounded=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Art/Meshes/RoundedUnit.asset");
        Mat("CityStone","C7BEA2"); Mat("CityRoof","799D84"); Mat("CityDark","405A60"); Mat("CityTerracotta","CE9671");
        Mat("Pollution","986294"); Mat("Factory","767C7D"); Mat("FactoryDark","45575D"); Mat("Rust","A77059");
        Mat("RedGlow","FF815F",true); Mat("PollutedGlow","CF8BCE",true); Mat("Smoke","58616B");
    }
    public const string Root="Assets/_BorrowedTime";
    public const string ScenePath=Root+"/Scenes/TrainingArea.unity";
    static Dictionary<string,Material> mats;
    static Mesh rounded;
    static Transform environment,details;
    static BTTrainingGame game;
    static int meshId;
    static readonly System.Random random=new System.Random(1142);
    static float Rand(float a,float b) => a+(float)random.NextDouble()*(b-a);
    static void GrassTuft(Vector3 p) {
        for(int j=0;j<3;j++) {
            var g=Shape("Meadow grass",p+new Vector3((j-1)*.12f,.15f,0),new Vector3(.09f,.4f,.13f),j%2==0?"LeafLight":"Leaf",details);
            g.transform.Rotate(0,j*70,(j-1)*22);
        }
    }
    static Color Hex(string s) { ColorUtility.TryParseHtmlString("#"+s,out Color c); return c; }
    internal static void Folder(string p) { if(!AssetDatabase.IsValidFolder(p)) { int at=p.LastIndexOf('/'); Folder(p.Substring(0,at)); AssetDatabase.CreateFolder(p.Substring(0,at),p.Substring(at+1)); } }
    internal static Material Mat(string key,string hex,bool glow=false) {
        string p=Root+"/Materials/"+key+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,p); }
        m.SetColor("_BaseColor",Hex(hex)); m.SetFloat("_Smoothness",.18f); m.enableInstancing=true;
        if(glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",Hex(hex)*1.8f); }
        mats[key]=m; return m;
    }
    internal static Transform Group(string n,Transform parent=null) { var g=new GameObject(n).transform; if(parent) g.SetParent(parent,false); return g; }
    internal static GameObject Shape(string n,Vector3 p,Vector3 size,string material,Transform parent=null,bool solid=false,PrimitiveType primitive=PrimitiveType.Cube) {
        var go=GameObject.CreatePrimitive(primitive); go.name=n; go.transform.position=p; go.transform.localScale=size;
        if(parent) go.transform.SetParent(parent,true);
        go.GetComponent<Renderer>().sharedMaterial=mats[material];
        if(primitive==PrimitiveType.Cube && rounded) go.GetComponent<MeshFilter>().sharedMesh=rounded;
        if(!solid) { Object.DestroyImmediate(go.GetComponent<Collider>()); go.layer=2; }
        return go;
    }
    internal static void Beam(string n,Vector3 a,Vector3 b,float thick,string mat,Transform par,bool solid=false) {
        var g=Shape(n,(a+b)*.5f,new Vector3(thick,thick,(b-a).magnitude),mat,par,solid); g.transform.rotation=Quaternion.LookRotation(b-a);
    }
    internal static GameObject MeshObject(string name,List<Vector3> v,List<int> triangles,string material,Transform par,bool collider) {
        var mesh=new Mesh {name=name}; mesh.SetVertices(v); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path=Root+"/Art/Meshes/"+name.Replace(" ","_")+"_"+(meshId++)+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old) { EditorUtility.CopySerialized(mesh,old); Object.DestroyImmediate(mesh); mesh=old; } else AssetDatabase.CreateAsset(mesh,path);
        var g=new GameObject(name); g.transform.SetParent(par,true); g.AddComponent<MeshFilter>().sharedMesh=mesh; g.AddComponent<MeshRenderer>().sharedMaterial=mats[material];
        if(collider) g.AddComponent<MeshCollider>().sharedMesh=mesh; else g.layer=2;
        return g;
    }
    static Vector2[] Outline(float x0,float x1,float z0,float z1) {
        float b=1.8f;
        return new[]{new Vector2(x0+b,z0),new Vector2(x1-b,z0),new Vector2(x1,z0+b),new Vector2(x1,z1-b),new Vector2(x1-b,z1),new Vector2(x0+b,z1),new Vector2(x0,z1-b),new Vector2(x0,z0+b)};
    }
    internal static void Island(string name,float x0,float x1,float z0,float z1,float top) {
        var par=Group(name,environment); var o=Outline(x0,x1,z0,z1); float cx=(x0+x1)*.5f,cz=(z0+z1)*.5f;
        var v=new List<Vector3>{new Vector3(cx,top,cz)}; foreach(var a in o) v.Add(new Vector3(a.x,top,a.y));
        var tri=new List<int>(); for(int i=0;i<8;i++) { tri.Add(0); tri.Add(1+(i+1)%8); tri.Add(1+i); }
        MeshObject(name+" meadow",v,tri,"Grass",par,true);
        for(int tier=0;tier<3;tier++) {
            float y1=top-.12f-tier*2.6f,y0=y1-2.65f;
            v=new List<Vector3>(); tri=new List<int>();
            for(int i=0;i<8;i++) {
                var a=o[i]; var b=o[(i+1)%8]; int k=v.Count;
                float inset=tier*.45f;
                var aa=Vector2.Lerp(a,new Vector2(cx,cz),inset*.025f); var bb=Vector2.Lerp(b,new Vector2(cx,cz),inset*.025f);
                v.Add(new Vector3(a.x,y1,a.y)); v.Add(new Vector3(b.x,y1,b.y)); v.Add(new Vector3(bb.x,y0,bb.y)); v.Add(new Vector3(aa.x,y0,aa.y));
                tri.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
            }
            MeshObject(name+" cliff "+tier,v,tri,tier==0?"CliffLight":"Cliff",par,false);
        }
        for(int i=0;i<8;i++) {
            Vector2 a=o[i], b=o[(i+1)%8]; int count=Mathf.CeilToInt(Vector2.Distance(a,b)/3);
            for(int j=0;j<count;j++) {
                var q=Vector2.Lerp(a,b,(j+.5f)/count);
                var g=Shape("Weathered cliff block",new Vector3(q.x,top-1.8f,q.y),new Vector3(Rand(2,3.5f),Rand(2,3.6f),Rand(1.4f,2.5f)),j%3==0?"Cliff":"CliffLight",par);
                g.transform.Rotate(0,Rand(-15,15),Rand(-6,6));
                if(j%3==0) Shape("Moss cap",new Vector3(q.x,top-.18f,q.y),new Vector3(2,.25f,1.8f),"Moss",par);
            }
        }
    }
    internal static void Path(string n,Vector3[] points,float width=3) {
        var p=Group(n,environment); var v=new List<Vector3>(); var t=new List<int>();
        for(int i=0;i<points.Length;i++) {
            Vector3 d=i==points.Length-1?points[i]-points[i-1]:points[i+1]-points[i]; d.y=0;
            var side=Vector3.Cross(d.normalized,Vector3.up)*width*.5f;
            v.Add(points[i]+side+Vector3.up*.035f); v.Add(points[i]-side+Vector3.up*.035f);
            if(i>0) { int k=i*2; t.AddRange(new[]{k-2,k,k+1,k-2,k+1,k-1}); }
        }
        MeshObject(n,v,t,"Path",p,false);
        for(int i=0;i<points.Length-1;i++) {
            int count=Mathf.CeilToInt(Vector3.Distance(points[i],points[i+1])/1.8f);
            for(int j=0;j<count;j++) {
                var q=Vector3.Lerp(points[i],points[i+1],j/(float)count); q+=new Vector3(Rand(-.7f,.7f),.07f,Rand(-.2f,.2f));
                var s=Shape("Inset paving",q,new Vector3(Rand(.45f,.9f),.1f,Rand(.4f,.8f)),"StoneCream",p); s.transform.Rotate(0,Rand(-30,30),0);
            }
        }
    }
    internal static void Ring(string n,Vector3 p,float radius,string mat,Transform parent,bool vertical=false) {
        var g=new GameObject(n); g.transform.SetParent(parent,false); var lr=g.AddComponent<LineRenderer>(); lr.sharedMaterial=mats[mat];
        lr.useWorldSpace=false; lr.positionCount=65; lr.startWidth=lr.endWidth=.065f; g.transform.position=p;
        for(int i=0;i<65;i++) { float a=i/64f*Mathf.PI*2; lr.SetPosition(i,vertical?new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0):new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); }
        g.layer=2;
    }
    internal static void Fence(Vector3 a,Vector3 b,Transform parent=null) {
        if(!parent) parent=environment;
        int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/2.8f));
        for(int i=0;i<=n;i++) {
            Vector3 p=Vector3.Lerp(a,b,i/(float)n);
            Shape("Fence post",p+Vector3.up*.68f,new Vector3(.23f,1.35f,.23f),"Wood",parent,true);
            Shape("Post cap",p+Vector3.up*1.37f,new Vector3(.3f,.12f,.3f),"WoodLight",parent);
        }
        Beam("Fence upper rail",a+Vector3.up*1.08f,b+Vector3.up*1.08f,.15f,"WoodLight",parent,true);
        Beam("Fence lower rail",a+Vector3.up*.48f,b+Vector3.up*.48f,.12f,"Wood",parent,true);
    }
    internal static Transform Tree(string name,Vector3 pos,float scale=1,bool hero=false) {
        var par=Group(name,details); par.position=pos;
        var trunk=Shape("Trunk",pos+Vector3.up*1.7f*scale,new Vector3(.7f,1.7f,.7f)*scale,"Bark",par,true,PrimitiveType.Cylinder);
        for(int i=0;i<4;i++) {
            float a=i*1.57f;
            Vector3 start=pos+new Vector3(0,1.8f,0)*scale, end=pos+new Vector3(Mathf.Cos(a)*1.3f,3,Mathf.Sin(a)*1.3f)*scale;
            Beam("Branch",start,end,.32f*scale,"Bark",par);
        }
        for(int i=0;i<7;i++) {
            float a=i*2.4f; Vector3 p=pos+new Vector3(Mathf.Cos(a)*(i==0?0:1.15f),3.5f+((i%3)*.45f),Mathf.Sin(a)*(i==0?0:1.15f))*scale;
            Shape("Rounded canopy",p,new Vector3(2.8f,2.3f,2.8f)*scale,i%3==0?"LeafLight":"Leaf",par,false,PrimitiveType.Sphere);
        }
        for(int i=0;i<5;i++) { float a=i*1.2566f; Beam("Exposed root",pos+new Vector3(0,.25f,0),pos+new Vector3(Mathf.Sin(a)*scale*1.35f,.08f,Mathf.Cos(a)*scale*1.35f),.25f*scale,"Bark",par); }
        if(hero) Ring("Living energy halo",pos+Vector3.up*.1f,2.8f,"GlowGreen",par);
        return par;
    }
    internal static void Bush(Vector3 p,float scale=1) { Shape("Pillow shrub",p+Vector3.up*.48f*scale,new Vector3(1.7f,1.1f,1.3f)*scale,"Moss",details,false,PrimitiveType.Sphere); }
    internal static void Flowers(Vector3 p) {
        for(int i=0;i<4;i++) {
            var q=p+new Vector3(Rand(-.5f,.5f),.32f,Rand(-.5f,.5f));
            Shape("Flower stem",q-Vector3.up*.13f,new Vector3(.055f,.4f,.055f),"Leaf",details);
            Shape("Flower",q,new Vector3(.24f,.13f,.24f),i%2==0?"Coral":"Gold",details,false,PrimitiveType.Sphere);
        }
    }
    internal static void Ruin(Vector3 p,int h=3) {
        for(int j=0;j<h;j++) for(int i=0;i<3;i++) {
            if(j==h-1 && i==2) continue;
            Shape("Ancient masonry",p+new Vector3(i*1.25f,j*.72f+.35f,0),new Vector3(1.2f,.68f,1),j%2==0?"Stone":"StoneCream",details,true);
        }
        Bush(p+new Vector3(.6f,h*.72f,0),.65f);
        for(int i=0;i<3;i++) Shape("Trailing ivy",p+new Vector3(.2f+i*.8f,h*.65f-.25f*i,-.56f),new Vector3(.55f,1.5f,.13f),"Leaf",details,false,PrimitiveType.Sphere);
    }
    internal static BTTimeTarget Target(string n,TargetKind kind,Vector3 p,int age,string[] labels,Transform par=null) {
        var root=Group(n,par?par:environment); root.position=p;
        var t=root.gameObject.AddComponent<BTTimeTarget>(); t.game=game; t.kind=kind; t.displayName=n; t.age=t.initialAge=age; t.stateNames=labels;
        t.aimPoint=Group("Aim point",root); t.aimPoint.position=p+Vector3.up*1.2f;
        t.focusRing=Group("Target highlight",root).gameObject; Ring("Focus",p+Vector3.up*.12f,1.15f,"GlowGold",t.focusRing.transform); t.focusRing.SetActive(false);
        return t;
    }
    internal static void TargetCrystal(BTTimeTarget t,Vector3 p,string mat) {
        var g=Shape("Time focus crystal",p,new Vector3(.48f,.75f,.48f),mat,t.transform,true,PrimitiveType.Sphere); t.aimPoint.position=p;
        Ring("Time glyph",p,.65f,mat,t.transform,true);
        Shape("Focus plinth",p-Vector3.up*.8f,new Vector3(.8f,.55f,.8f),"Stone",t.transform,true);
    }
    static void CreateBridge() {
        game.bridge=Target("สะพานข้ามธาร",TargetKind.Bridge,Vector3.zero,2,new[]{"สมบูรณ์","ผุกร่อน","พัง"});
        var t=game.bridge; t.states=new GameObject[3];
        for(int state=0;state<3;state++) {
            var par=Group("Age "+state,t.transform); t.states[state]=par.gameObject;
            for(int i=0;i<19;i++) {
                float x=-5.7f+i*.5f;
                bool missing=state==2 && i>5 && i<14;
                if(missing) continue;
                var plank=Shape("Bridge plank",new Vector3(x,.04f,4),new Vector3(.47f,.26f,3),state==0?"WoodLight":"Wood",par,true);
                if(state>0) plank.transform.Rotate(state*Rand(-8,8),0,Rand(-4,4));
            }
            if(state<2) {
                Beam("Bridge support",new Vector3(-5.9f,-.3f,3),new Vector3(3.6f,-.3f,3),.25f,"Wood",par);
                Beam("Bridge support",new Vector3(-5.9f,-.3f,5),new Vector3(3.6f,-.3f,5),.25f,"Wood",par);
            }
            par.gameObject.SetActive(state==2);
        }
        for(int side=0;side<2;side++) {
            float z=side==0?2.55f:5.45f;
            for(int i=0;i<5;i++) Shape("Crossing post",new Vector3(-5.8f+i*2.3f,.7f,z),new Vector3(.17f,1.55f,.17f),"Wood",t.transform);
            for(int j=0;j<8;j++) {
                Vector3 a=new Vector3(-5.8f+j*1.15f,1.2f-Mathf.Sin(j/8f*Mathf.PI)*.3f,z),b=new Vector3(-5.8f+(j+1)*1.15f,1.2f-Mathf.Sin((j+1)/8f*Mathf.PI)*.3f,z);
                Beam("Rope handrail",a,b,.07f,"Rope",t.transform);
            }
        }
        TargetCrystal(t,new Vector3(-5.8f,1.3f,6.3f),"GlowBlue"); t.Apply();
    }
    static void CreateVine() {
        var t=Target("เถาวัลย์ทางขึ้น",TargetKind.Vine,new Vector3(12,0,10),0,new[]{"ต้นอ่อน","ทางเถาวัลย์","เถาวัลย์หนาทึบ"}); game.vine=t; t.states=new GameObject[3];
        for(int state=0;state<3;state++) {
            var par=Group("Age "+state,t.transform); t.states[state]=par.gameObject;
            if(state==0) {
                for(int j=0;j<4;j++) { var leaf=Shape("Young leaf",new Vector3(12+(j-1.5f)*.3f,.7f,10),new Vector3(.5f,1.3f,.18f),"LeafLight",par); leaf.transform.Rotate(0,j*60,30); }
            } else {
                // A continuous collider ramp keeps walking smooth; visual leaves are separate.
                var verts=new List<Vector3>{new Vector3(10,0,9),new Vector3(14,0,9),new Vector3(14,3.1f,14.5f),new Vector3(10,3.1f,14.5f)};
                MeshObject("Vine walkable ramp "+state,verts,new List<int>{0,3,2,0,2,1},state==1?"Leaf":"Moss",par,true);
                for(int j=0;j<9;j++) {
                    float f=j/8f;
                    for(int side=0;side<2;side++) {
                        Vector3 pos=new Vector3(side==0?10.3f:13.7f,f*3.1f+.12f,9+f*5.5f);
                        var leaf=Shape("Giant vine leaf",pos,new Vector3(1.5f,.18f,1.2f),j%2==0?"LeafLight":"Leaf",par); leaf.transform.Rotate(0,side==0?25:-25,side==0?8:-8);
                    }
                }
                for(int side=0;side<2;side++) Beam("Living handrail",new Vector3(side==0?10:14,.8f,9),new Vector3(side==0?10:14,3.9f,14.5f),.18f,"Bark",par,true);
                if(state==2) for(int j=0;j<4;j++) Bush(new Vector3(9.2f,1+j*.6f,10+j),.7f);
            }
        }
        TargetCrystal(t,new Vector3(14.8f,1.3f,9),"GlowGreen"); t.Apply();
    }
    static void CreateGate() {
        var par=Group("06 Timekeeper courtyard",environment);
        foreach(float x in new[]{7.7f,16.3f}) {
            for(int h=0;h<6;h++) Shape("Gate pillar block",new Vector3(x,3.4f+h*.75f,19),new Vector3(1.25f,.7f,1.65f),h%2==0?"Stone":"StoneCream",par,true);
            Shape("Pillar moss cap",new Vector3(x,7.8f,19),new Vector3(1.7f,.35f,1.9f),"Moss",par);
        }
        Shape("Monolithic lintel",new Vector3(12,7.45f,19),new Vector3(9.6f,1.1f,1.7f),"Stone",par,true);
        Shape("Lintel grass",new Vector3(12,8.05f,19),new Vector3(9.8f,.18f,1.8f),"Grass",par);
        for(int i=0;i<3;i++) { Shape("Time rune",new Vector3(11+i,7.5f,18.12f),new Vector3(.12f,.5f,.08f),"GlowBlue",par); }
        game.door=Shape("Sealed training door",new Vector3(12,5,19),new Vector3(7.2f,4,1),"Gate",par,true).transform;
        Ring("Door clock",new Vector3(12,5.3f,18.43f),1.15f,"GlowBlue",game.door,true);
        Beam("Clock hand",new Vector3(12,5.3f,18.4f),new Vector3(12,6.1f,18.4f),.08f,"GlowBlue",game.door);
        Beam("Clock hand",new Vector3(12,5.3f,18.4f),new Vector3(12.65f,5,18.4f),.08f,"GlowBlue",game.door);
        // Physical ruins enclose the reward courtyard, so the gate is a meaningful lock.
        foreach(float x in new[]{5.8f,18.2f}) Shape("Courtyard wall",new Vector3(x,4.6f,21.3f),new Vector3(1.1f,3.2f,11),"Stone",par,true);
        Shape("Courtyard rear wall",new Vector3(12,4.6f,26.2f),new Vector3(13.5f,3.2f,1),"Stone",par,true);
        Shape("Gate wing left",new Vector3(6.6f,4.6f,19),new Vector3(2,3.2f,1.3f),"Stone",par,true);
        Shape("Gate wing right",new Vector3(17.4f,4.6f,19),new Vector3(2,3.2f,1.3f),"Stone",par,true);
        var mech=Target("กลไกประตู",TargetKind.Mechanism,new Vector3(8,3,16.6f),2,new[]{"พร้อมทำงาน","ชำรุด","เสียหาย"}); game.mechanism=mech; mech.states=new GameObject[3];
        for(int s=0;s<3;s++) {
            var g=Group("Age "+s,mech.transform); mech.states[s]=g.gameObject;
            Shape("Clock pedestal",new Vector3(8,3.6f,16.6f),new Vector3(1.2f,1.2f,1.2f),"Stone",g,true);
            for(int k=0;k<8;k++) {
                float a=k*Mathf.PI/4;
                var cog=Shape("Clock tooth",new Vector3(8+Mathf.Cos(a)*.6f,4.3f+Mathf.Sin(a)*.6f,16.45f),new Vector3(.28f,.3f,.25f),s==0?"Gold":"Metal",g); cog.transform.Rotate(0,0,k*45);
            }
            Shape("Clock core",new Vector3(8,4.3f,16.4f),new Vector3(.9f,.9f,.3f),s==0?"GlowBlue":"Metal",g,false,PrimitiveType.Sphere);
        }
        TargetCrystal(mech,new Vector3(8,4.3f,16.25f),"GlowBlue"); mech.Apply();
        var seal=Target("เถาวัลย์ผนึกประตู",TargetKind.Seal,new Vector3(16,3,16.6f),1,new[]{"อ่อนวัย","แข็งแรง","แห้งสลาย"}); game.seal=seal; seal.states=new GameObject[3];
        for(int s=0;s<3;s++) {
            var g=Group("Age "+s,seal.transform); seal.states[s]=g.gameObject;
            Shape("Root pedestal",new Vector3(16,3.35f,16.6f),new Vector3(1.2f,.6f,1.2f),"Stone",g,true);
            if(s<2) {
                for(int i=0;i<4;i++) {
                    Vector3 a=new Vector3(16,3.5f,16.6f), b=new Vector3(12+(i-1.5f)*.8f,4+i*.7f,18.35f);
                    Beam("Door binding vine",a,b,s==0?.11f:.24f,"Bark",g);
                    Shape("Binding leaf",Vector3.Lerp(a,b,.65f),new Vector3(1.1f,.22f,.65f),s==0?"LeafLight":"Leaf",g,false,PrimitiveType.Sphere);
                }
            } else for(int i=0;i<4;i++) Shape("Spent leaves",new Vector3(15.5f+i*.3f,3.1f,17),new Vector3(.35f,.08f,.2f),"WoodLight",g);
        }
        TargetCrystal(seal,new Vector3(16,4.3f,16.25f),"GlowGreen"); seal.Apply();
        Shape("Seed pedestal",new Vector3(12,3.6f,23),new Vector3(1.6f,1.1f,1.6f),"StoneCream",par,true);
        Ring("Seed cradle",new Vector3(12,4.2f,23),.65f,"GlowGold",par);
        game.seed=Shape("Miracle seed",new Vector3(12,4.65f,23),new Vector3(.45f,.85f,.38f),"GlowGreen",par,false,PrimitiveType.Sphere).transform;
        game.seed.Rotate(0,0,-25);
        game.norman=Character("Norman Scott",new Vector3(14.5f,3,23),par,true);
        Ruin(new Vector3(4.2f,3,23),3); Ruin(new Vector3(20,3,19),4);
    }
    static Transform Character(string name,Vector3 p,Transform parent,bool scientist=false) {
        var root=Group(name,parent); root.position=p;
        string coat=scientist?"Ivory":"Coat";
        Shape("Coat body",p+Vector3.up*.78f,new Vector3(.68f,.82f,.48f),coat,root,false,PrimitiveType.Capsule);
        Shape("Head",p+Vector3.up*1.45f,new Vector3(.72f,.69f,.65f),"Skin",root,false,PrimitiveType.Sphere);
        Shape("Hair",p+new Vector3(0,1.7f,-.04f),new Vector3(.78f,.38f,.67f),scientist?"Ivory":"Hair",root,false,PrimitiveType.Sphere);
        if(!scientist) for(int i=0;i<5;i++) { var g=Shape("Hair tuft",p+new Vector3((i-2)*.12f,1.82f,.02f),new Vector3(.24f,.28f,.3f),"Hair",root,false,PrimitiveType.Sphere); g.transform.Rotate(0,0,(i-2)*15); }
        foreach(float x in new[]{-.16f,.16f}) {
            Shape("Eye",p+new Vector3(x,1.48f,.302f),new Vector3(.065f,.1f,.045f),"Ink",root,false,PrimitiveType.Sphere);
            Shape(x<0?"Left boot":"Right boot",p+new Vector3(x,.21f,.05f),new Vector3(.27f,.42f,.42f),"Boot",root);
            Shape("Arm",p+new Vector3(Mathf.Sign(x)*.44f,.88f,0),new Vector3(.25f,.52f,.25f),coat,root,false,PrimitiveType.Capsule);
        }
        if(!scientist) {
            Shape("Explorer backpack",p+new Vector3(0,.9f,-.34f),new Vector3(.55f,.65f,.3f),"WoodLight",root);
            Shape("Scarf",p+new Vector3(0,1.12f,.18f),new Vector3(.6f,.2f,.32f),"Coral",root);
            Shape("Time gauntlet",p+new Vector3(.45f,.65f,.07f),new Vector3(.32f,.3f,.4f),"Metal",root);
            Shape("Gauntlet core",p+new Vector3(.45f,.7f,.29f),new Vector3(.18f,.16f,.08f),"GlowBlue",root,false,PrimitiveType.Sphere);
        }
        return root;
    }
    static void WaterAndBackdrop() {
        Shape("Turquoise river",new Vector3(-1,-5.6f,2),new Vector3(9,.3f,82),"Water",environment);
        for(int i=0;i<30;i++) Shape("River glint",new Vector3(Rand(-4,2),-5.4f,Rand(-33,36)),new Vector3(Rand(.2f,.7f),.035f,Rand(.6f,2)),"Foam",details);
        for(int i=0;i<6;i++) {
            Shape("Waterfall ribbon",new Vector3(-3.6f+i*1.1f,-1.8f,24),new Vector3(1.2f,7.2f,.25f),i%2==0?"Water":"Foam",environment);
            Shape("Waterfall splash",new Vector3(-3.6f+i*1.1f,-5.1f,23.5f),new Vector3(1.7f,.25f,1.5f),"Foam",environment,false,PrimitiveType.Sphere);
        }
        for(int i=0;i<11;i++) {
            float x=-55+i*11;
            Shape("Distant rounded peak",new Vector3(x,-7,52+Rand(-4,4)),new Vector3(Rand(12,23),Rand(15,28),Rand(10,18)),i%2==0?"Distant":"DistantLight",environment,false,PrimitiveType.Sphere);
            for(int j=0;j<3;j++) Shape("Soft cloud",new Vector3(x+j*3,18+Rand(-2,2),46),new Vector3(8,3.5f,5),"Ivory",environment,false,PrimitiveType.Sphere);
        }
    }
    [MenuItem("Tools/Borrowed Time/Build Training Area")]
    public static void Build() {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building");
        var previous=EditorSceneManager.GetActiveScene();
        if(previous.isDirty) { Folder(Root+"/Backups"); EditorSceneManager.SaveScene(previous,Root+"/Backups/BeforeTraining_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true); }
        Folder(Root+"/Scenes"); Folder(Root+"/Art/Meshes"); Folder(Root+"/Materials"); Folder(Root+"/Prefabs"); Folder(Root+"/UI"); Folder(Root+"/Evidence");
        if(System.IO.File.Exists(ScenePath)) { Folder(Root+"/Backups"); AssetDatabase.CopyAsset(ScenePath,Root+"/Backups/Training_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity"); }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        meshId=0; mats=new Dictionary<string,Material>();
        Mat("Grass","8ED35F"); Mat("Moss","64B85D"); Mat("Cliff","829D9A"); Mat("CliffLight","A5B5A1"); Mat("Stone","B8BCAA"); Mat("StoneCream","DBD6B5");
        Mat("Leaf","54AF66"); Mat("LeafLight","A6DA69"); Mat("Bark","94704D"); Mat("Wood","94734E"); Mat("WoodLight","D2A66E"); Mat("Rope","E6CCA1"); Mat("Path","EBCD94");
        Mat("Water","37AEBB"); Mat("Foam","BFF5E8"); Mat("Coral","F79685"); Mat("Gold","FDD878"); Mat("GlowGold","FFE9A0",true); Mat("GlowBlue","70E0FB",true); Mat("GlowGreen","8DFFC0",true);
        Mat("Gate","426A75"); Mat("Metal","77959B"); Mat("Ivory","FCF5DD"); Mat("Ink","233E4B"); Mat("Coat","4A9290"); Mat("Skin","F1C697"); Mat("Hair","584E42"); Mat("Boot","46616B"); Mat("Distant","8DC6B5"); Mat("DistantLight","ADD3BA"); Mat("Wilt","ABA858");
        rounded=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Art/Meshes/RoundedUnit.asset");
        if(!rounded) {
            foreach(var a in AssetDatabase.LoadAllAssetsAtPath("Assets/OopsPlanet/Models/SoftWorld/SoftWorld_Modules.fbx")) if(a is Mesh m && m.name.StartsWith("SOFT_UNIT")) {
                rounded=Object.Instantiate(m); rounded.name="BorrowedTime Rounded Unit"; var v=rounded.vertices; Bounds b=rounded.bounds;
                for(int i=0;i<v.Length;i++) { var d=v[i]-b.center; v[i]=new Vector3(d.x/b.size.x,d.y/b.size.y,d.z/b.size.z); }
                rounded.vertices=v; rounded.RecalculateBounds(); AssetDatabase.CreateAsset(rounded,Root+"/Art/Meshes/RoundedUnit.asset"); break;
            }
        }
        environment=Group("BORROWED TIME · Training Area"); details=Group("Nature and ancient garden",environment);
        game=Group("Training Systems").gameObject.AddComponent<BTTrainingGame>();
        if(!AssetDatabase.LoadAssetAtPath<Font>(Root+"/UI/NotoSansThai.ttf")) AssetDatabase.CopyAsset("Assets/OopsPlanet/Fonts/NotoSansThai-Variable.ttf",Root+"/UI/NotoSansThai.ttf");
        game.uiFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/UI/NotoSansThai.ttf");
        Island("West garden",-25,-5,-18,15,0); Island("East landing",3,25,-5,10,0); Island("Upper sanctuary",3,25,14,28,3);
        WaterAndBackdrop();
        Path("Pilgrim trail",new[]{new Vector3(-16,0,-12),new Vector3(-18,0,-7),new Vector3(-18,0,-3),new Vector3(-15,0,1),new Vector3(-11,0,4),new Vector3(-5.3f,0,4)},3.5f);
        Path("Riverside trail",new[]{new Vector3(3.4f,0,4),new Vector3(8,0,4),new Vector3(12,0,7),new Vector3(12,0,9)},3.5f);
        Path("Sanctuary trail",new[]{new Vector3(12,3,14.3f),new Vector3(12,3,18.5f)},4);
        Path("Seed courtyard paving",new[]{new Vector3(12,3,19.5f),new Vector3(12,3,25)},6);
        Shape("Arrival circle",new Vector3(-16,.035f,-12),new Vector3(6,.15f,6),"StoneCream",environment,true,PrimitiveType.Cylinder);
        Ring("Arrival inlay",new Vector3(-16,.13f,-12),2.4f,"GlowBlue",environment);
        game.moveMarker=Group("Movement objective ring",environment); Ring("Follow Robo",new Vector3(-18,.12f,-5),1.4f,"GlowGold",game.moveMarker);
        var tree=Tree("03 Living energy tree",new Vector3(-15,0,1.5f),1.05f,true);
        game.tree=tree.gameObject.AddComponent<BTTimeTarget>(); var tt=game.tree; tt.kind=TargetKind.Plant; tt.displayName="ต้นไม้แห่งชีวิต"; tt.stateNames=new[]{"โตเต็มวัย"}; tt.age=tt.initialAge=0; tt.game=game;
        tt.aimPoint=Group("Living energy core",tree); tt.aimPoint.position=new Vector3(-15,1.4f,1.5f);
        Shape("Life focus",tt.aimPoint.position,new Vector3(.45f,.6f,.45f),"GlowGreen",tree,true,PrimitiveType.Sphere);
        tt.focusRing=Group("Plant focus highlight",tree).gameObject; Ring("Living focus",new Vector3(-15,.16f,1.5f),3,"GlowGold",tt.focusRing.transform); tt.focusRing.SetActive(false);
        var foliage=new List<Renderer>(); foreach(var r in tree.GetComponentsInChildren<Renderer>()) if(r.name=="Rounded canopy") foliage.Add(r); tt.foliage=foliage.ToArray(); tt.healthyMaterial=mats["Leaf"]; tt.wiltMaterial=mats["Wilt"];
        CreateBridge(); CreateVine(); CreateGate();
        Fence(new Vector3(-23,0,-15),new Vector3(-23,0,12)); Fence(new Vector3(-23,0,12),new Vector3(-7,0,12));
        Fence(new Vector3(-7,0,-15),new Vector3(-7,0,1.6f)); Fence(new Vector3(-7,0,7),new Vector3(-7,0,12));
        Fence(new Vector3(-22,0,-16),new Vector3(-8,0,-16));
        Fence(new Vector3(4.8f,0,-3),new Vector3(23,0,-3)); Fence(new Vector3(23,0,-3),new Vector3(23,0,8.5f));
        Fence(new Vector3(4.8f,0,-3),new Vector3(4.8f,0,1.5f)); Fence(new Vector3(4.8f,0,6.7f),new Vector3(4.8f,0,9));
        Fence(new Vector3(4.8f,3,15),new Vector3(8.8f,3,15)); Fence(new Vector3(15.2f,3,15),new Vector3(23,3,15));
        var treePositions=new[]{new Vector3(-21,0,8),new Vector3(-20,0,-1),new Vector3(-9,0,10),new Vector3(-22,0,-12),new Vector3(20,0,0),new Vector3(21,0,7),new Vector3(5.6f,0,8),new Vector3(21,3,24),new Vector3(4.8f,3,26)};
        foreach(var p in treePositions) Tree("Garden tree",p,Rand(.65f,.95f));
        Ruin(new Vector3(-22,0,12),4); Ruin(new Vector3(-10,0,9),3); Ruin(new Vector3(18,0,8.5f),3);
        for(int i=0;i<85;i++) {
            bool west=i<45; float x=west?Rand(-22,-8):Rand(5,22),z=west?Rand(-14,11):Rand(-2,8);
            // Leave the authored route and tree approach clear.
            if(west && (Mathf.Abs(x+17)<3.6f || Vector2.Distance(new Vector2(x,z),new Vector2(-15,1.5f))<4 || (z>1 && z<7))) continue;
            if(!west && (Mathf.Abs(z-4)<2.3f || Mathf.Abs(x-12)<3)) continue;
            if(i%3==0) Bush(new Vector3(x,0,z),Rand(.55f,1)); else if(i%3==1) Flowers(new Vector3(x,0,z));
            else { var g=Shape("Garden rock",new Vector3(x,.3f,z),new Vector3(Rand(.6f,1.3f),.7f,Rand(.6f,1.1f)),"Stone",details); g.transform.Rotate(0,Rand(0,180),Rand(-10,10)); }
        }
        Flowers(new Vector3(-12,0,-9)); Flowers(new Vector3(-12,0,-7)); Flowers(new Vector3(19,0,4)); Flowers(new Vector3(9,3,23)); Flowers(new Vector3(15,3,25));
        for(int i=0;i<130;i++) {
            float x=i<80?Rand(-23,-7):Rand(6,22), z=i<80?Rand(-14,12):Rand(-2,8);
            if((x<0 && (Mathf.Abs(x+18)<2.6f || (z>1 && z<6.8f))) || (x>0 && (Mathf.Abs(z-4)<2.2f || Mathf.Abs(x-12)<2.8f))) continue;
            GrassTuft(new Vector3(x,0,z));
        }
        // Renewable recovery sources after each crossing keep incorrect casts recoverable.
        foreach(var p in new[]{new Vector3(8,0,7.6f),new Vector3(19.8f,3,16.6f)}) {
            var plant=Tree("Recovery sapling",p,.48f,true); var t=plant.gameObject.AddComponent<BTTimeTarget>(); t.game=game; t.kind=TargetKind.Plant; t.displayName="ต้นไม้ฟื้นพลัง"; t.stateNames=new[]{"โตเต็มวัย"};
            t.aimPoint=Group("Energy core",plant); t.aimPoint.position=p+Vector3.up; Shape("Life focus",p+Vector3.up,new Vector3(.35f,.5f,.35f),"GlowGreen",plant,true,PrimitiveType.Sphere);
            t.healthyMaterial=mats["Leaf"]; t.wiltMaterial=mats["Wilt"]; var fs=new List<Renderer>(); foreach(var r in plant.GetComponentsInChildren<Renderer>()) if(r.name=="Rounded canopy") fs.Add(r); t.foliage=fs.ToArray();
        }
        var playerRoot=Group("Protagonist"); playerRoot.position=new Vector3(-16,.15f,-12); playerRoot.gameObject.layer=2;
        var cc=playerRoot.gameObject.AddComponent<CharacterController>(); cc.height=1.85f; cc.radius=.34f; cc.center=Vector3.up*.94f; cc.stepOffset=.3f; cc.slopeLimit=48; cc.skinWidth=.035f;
        var player=playerRoot.gameObject.AddComponent<BTPlayer>(); game.player=player; player.game=game;
        player.visual=Character("Explorer visual",playerRoot.position,playerRoot);
        player.rightHand=Group("Gauntlet beam origin",player.visual); player.rightHand.position=playerRoot.position+new Vector3(.45f,.8f,.4f);
        player.leftFoot=player.visual.Find("Left boot"); player.rightFoot=player.visual.Find("Right boot");
        var beam=Group("Time beam",playerRoot).gameObject.AddComponent<LineRenderer>(); beam.sharedMaterial=mats["GlowBlue"]; beam.startWidth=.045f; beam.endWidth=.1f; beam.positionCount=2; beam.enabled=false; player.beam=beam;
        var robot=Group("ROBO",environment); robot.position=playerRoot.position+new Vector3(1.5f,2,0); game.robo=robot;
        Shape("Robo shell",robot.position,Vector3.one*.85f,"Ivory",robot,false,PrimitiveType.Sphere);
        Shape("Robo face",robot.position+new Vector3(0,0,.36f),new Vector3(.63f,.48f,.2f),"Ink",robot,false,PrimitiveType.Sphere);
        foreach(float x in new[]{-.15f,.15f}) Shape("Friendly digital eye",robot.position+new Vector3(x,.035f,.46f),new Vector3(.13f,.18f,.055f),"GlowBlue",robot,false,PrimitiveType.Sphere);
        foreach(float x in new[]{-.5f,.5f}) Shape("Robo stabilizer",robot.position+new Vector3(x,0,0),new Vector3(.25f,.18f,.4f),"Metal",robot);
        Shape("Hover thruster",robot.position-Vector3.up*.45f,new Vector3(.2f,.16f,.2f),"GlowGreen",robot,false,PrimitiveType.Sphere);
        var camera=Group("Main Camera").gameObject.AddComponent<Camera>(); camera.tag="MainCamera"; camera.gameObject.AddComponent<AudioListener>(); camera.nearClipPlane=.1f; camera.farClipPlane=220; camera.backgroundColor=Hex("B8E5EC"); camera.clearFlags=CameraClearFlags.SolidColor;
        var camData=camera.gameObject.AddComponent<UniversalAdditionalCameraData>(); camData.renderPostProcessing=true; camData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        player.view=camera; player.yaw=0; player.pitch=20; camera.transform.position=playerRoot.position+new Vector3(.55f,3.5f,-6.5f); camera.transform.rotation=Quaternion.Euler(20,0,0);
        var sun=Group("Golden afternoon sun").gameObject.AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.25f; sun.color=Hex("FFF0CF"); sun.transform.rotation=Quaternion.Euler(48,-32,0); sun.shadows=LightShadows.Soft; sun.shadowStrength=.65f;
        sun.gameObject.AddComponent<UniversalAdditionalLightData>();
        RenderSettings.sun=sun; RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=Hex("BFE9EE"); RenderSettings.ambientEquatorColor=Hex("C4E4CB"); RenderSettings.ambientGroundColor=Hex("809D8F");
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogColor=Hex("B8E5EC"); RenderSettings.fogStartDistance=100; RenderSettings.fogEndDistance=200;
        var volume=Group("Garden colour and bloom").gameObject.AddComponent<Volume>(); volume.isGlobal=true;
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Materials/GardenPostFX.asset");
        if(!profile) { profile=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,Root+"/Materials/GardenPostFX.asset"); }
        if(!profile.TryGet<Bloom>(out var bloom)) { bloom=profile.Add<Bloom>(); AssetDatabase.AddObjectToAsset(bloom,profile); }
        bloom.intensity.Override(.25f); bloom.threshold.Override(1.2f); bloom.scatter.Override(.65f);
        if(!profile.TryGet<ColorAdjustments>(out var color)) { color=profile.Add<ColorAdjustments>(); AssetDatabase.AddObjectToAsset(color,profile); }
        color.saturation.Override(8); color.contrast.Override(4); volume.sharedProfile=profile;
        game.markers=new[]{new Vector3(-16,0,-12),new Vector3(-18,0,-5),new Vector3(-15,0,1.5f),new Vector3(-5.8f,0,6.3f),new Vector3(14.8f,0,9),new Vector3(12,3,17),new Vector3(12,3,23)};
        PrefabUtility.SaveAsPrefabAsset(robot.gameObject,Root+"/Prefabs/Robo.prefab");
        PrefabUtility.SaveAsPrefabAsset(tree.gameObject,Root+"/Prefabs/LivingEnergyTree.prefab");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,5),Quaternion.Euler(40,28,0),42);
        Debug.Log("BORROWED_TIME_TRAINING_READY "+ScenePath+" | targets="+Object.FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None).Length);
    }
}
}
#endif
