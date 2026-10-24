#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTExpansionBuilder {
 public const string Root="Assets/_BorrowedTime/Expansion";
 public static readonly string[] Paths={BTJourney.TrainingPath,BTJourney.CityPath,BTJourney.FactoryPath};
 static Transform root;static BTExpansionWorld world;static Font font;static Mesh rounded;static int meshId;static System.Random rng;
 static Dictionary<string,Material> mats;
 static float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
 static Color ColorOf(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
 static Mesh BeveledCube(){
  string path=Root+"/Meshes/ArchitectureBeveledCube.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved)return saved;
  const int n=8;const float radius=.065f;var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();
  foreach(var axis in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}){
   Vector3 tangent=Mathf.Abs(axis.y)>.5f?Vector3.right:Vector3.up,bitangent=Vector3.Cross(axis,tangent);int offset=vertices.Count;
   for(int y=0;y<=n;y++)for(int x=0;x<=n;x++){Vector3 q=axis*.5f+tangent*(x/(float)n-.5f)+bitangent*(y/(float)n-.5f);Vector3 inner=new Vector3(Mathf.Clamp(q.x,-.5f+radius,.5f-radius),Mathf.Clamp(q.y,-.5f+radius,.5f-radius),Mathf.Clamp(q.z,-.5f+radius,.5f-radius));Vector3 normal=(q-inner).normalized;vertices.Add(inner+normal*radius);normals.Add(normal);}
   for(int y=0;y<n;y++)for(int x=0;x<n;x++){int k=offset+y*(n+1)+x;triangles.AddRange(new[]{k,k+1,k+n+2,k,k+n+2,k+n+1});}
  }
  var mesh=new Mesh{name="Subtle rounded architecture"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
 static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
 static Material Mat(string name,string color,bool natural=false,float emission=0){
  string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find(natural?"Borrowed Time/Meadow Surface":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
  m.SetColor("_BaseColor",ColorOf(color));m.SetFloat("_Smoothness",.30f);m.enableInstancing=true;
  if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",ColorOf(color)*emission);}mats[name]=m;return m;
 }
 static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent?parent:root,false);return t;}
 static GameObject Shape(string name,Vector3 p,Vector3 size,string mat,Transform parent=null,bool solid=false,PrimitiveType primitive=PrimitiveType.Cube){
  var g=GameObject.CreatePrimitive(primitive);g.name=name;g.transform.position=p;g.transform.localScale=size;g.transform.SetParent(parent?parent:root,true);
  if(primitive==PrimitiveType.Cube&&rounded)g.GetComponent<MeshFilter>().sharedMesh=rounded;
  g.GetComponent<Renderer>().sharedMaterial=mats[mat];
  if(!solid){Object.DestroyImmediate(g.GetComponent<Collider>());g.layer=2;}
  else if(primitive==PrimitiveType.Cylinder){Object.DestroyImmediate(g.GetComponent<Collider>());g.AddComponent<MeshCollider>().sharedMesh=g.GetComponent<MeshFilter>().sharedMesh;}
  return g;
 }
 static void Beam(string n,Vector3 a,Vector3 b,float width,string mat,Transform par=null,bool solid=false){var g=Shape(n,(a+b)*.5f,new Vector3(width,width,(b-a).magnitude),mat,par,solid);g.transform.rotation=Quaternion.LookRotation(b-a);}
 static void MeshObject(string name,List<Vector3> vertices,List<int> triangles,string mat,bool solid=false,Transform parent=null){
  var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
  string path=Root+"/Meshes/"+world.game.gameObject.scene.name+"_"+(meshId++)+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
  if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
  var g=new GameObject(name);g.transform.SetParent(parent?parent:root,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mats[mat];if(solid)g.AddComponent<MeshCollider>().sharedMesh=mesh;else g.layer=2;
 }
 static void Foundation(string name,float x0,float x1,float z0,float z1,float top,string mat,bool solid){
  float c=2;var outline=new[]{new Vector2(x0+c,z0),new Vector2(x1-c,z0),new Vector2(x1,z0+c),new Vector2(x1,z1-c),new Vector2(x1-c,z1),new Vector2(x0+c,z1),new Vector2(x0,z1-c),new Vector2(x0,z0+c)};
  var v=new List<Vector3>();foreach(var p in outline)v.Add(new Vector3(p.x,top,p.y));foreach(var p in outline)v.Add(new Vector3(p.x,-16,p.y));var t=new List<int>();
  for(int i=1;i<7;i++)t.AddRange(new[]{0,i+1,i});
  for(int i=0;i<8;i++){int j=(i+1)%8;t.AddRange(new[]{i,j,j+8,i,j+8,i+8});}MeshObject(name,v,t,mat,solid);
 }
 static void Ground(float x0,float x1,float z0,float z1,string mat="Meadow"){
  Foundation("Grounded bedrock",x0,x1,z0,z1,-.09f,"Rock",false);
  Shape("Connected walkable terrain",new Vector3((x0+x1)*.5f,-.20f,(z0+z1)*.5f),new Vector3(x1-x0,.4f,z1-z0),mat,null,true);
 }
 static void Trail(Vector3 a,Vector3 b,float width=3){
  var side=Vector3.Cross((b-a).normalized,Vector3.up)*width*.5f;var v=new List<Vector3>{a-side+Vector3.up*.014f,a+side+Vector3.up*.014f,b+side+Vector3.up*.014f,b-side+Vector3.up*.014f};MeshObject("Sandy trail",v,new List<int>{0,1,2,0,2,3},"Sand");
  for(int j=0;j<Mathf.CeilToInt(Vector3.Distance(a,b)/3);j++){var p=Vector3.Lerp(a,b,(j+.5f)/Mathf.CeilToInt(Vector3.Distance(a,b)/3));Shape("Inset path stone",p+new Vector3(R(-.55f,.55f),.035f,0),new Vector3(.48f,.06f,.62f),"Limestone");}
 }
 static void Tree(Vector3 p,float scale=1,bool autumn=false){
  var par=Group("Rooted broadleaf tree");
  Shape("Trunk",p+Vector3.up*1.9f*scale,new Vector3(.55f,1.9f,.55f)*scale,"Bark",par,true,PrimitiveType.Cylinder);
  for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Beam("Supporting branch",p+Vector3.up*2*scale,p+new Vector3(Mathf.Cos(a)*1.1f,3.2f,Mathf.Sin(a)*1.1f)*scale,.23f*scale,"Bark",par);Beam("Grounded root",p+Vector3.up*.20f,p+new Vector3(Mathf.Cos(a)*.8f,.05f,Mathf.Sin(a)*.8f)*scale,.22f*scale,"Bark",par);}
  for(int i=0;i<5;i++){float a=i*2.4f;Shape("Soft layered foliage",p+new Vector3(Mathf.Sin(a)*(i==0?0:.9f),3.5f+(i%2)*.4f,Mathf.Cos(a)*(i==0?0:.9f))*scale,new Vector3(3.0f,2.0f,2.8f)*scale,autumn?"Autumn":i%2==0?"Leaf":"LeafLight",par,false,PrimitiveType.Sphere);}
 }
 static void Flowers(Vector3 p,Transform parent=null){
  for(int k=0;k<3;k++){var q=p+new Vector3(R(-.5f,.5f),0,R(-.5f,.5f));Shape("Flower stem",q+Vector3.up*.18f,new Vector3(.035f,.36f,.035f),"Leaf",parent);
   for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Shape("Petal",q+new Vector3(Mathf.Cos(a)*.10f,.38f,Mathf.Sin(a)*.10f),new Vector3(.18f,.075f,.18f),k%2==0?"Petal":"Coral",parent,false,PrimitiveType.Sphere);}Shape("Pollen",q+Vector3.up*.40f,Vector3.one*.10f,"Sun",parent,false,PrimitiveType.Sphere);
  }
 }
 static void Column(Vector3 p,float height,Transform par=null){Shape("Stone foot",p+Vector3.up*.18f,new Vector3(1.3f,.36f,1.3f),"Limestone",par,true);Shape("Supported column",p+Vector3.up*(height*.5f+.2f),new Vector3(.8f,height*.5f,.8f),"Limestone",par,true,PrimitiveType.Cylinder);Shape("Column capital",p+Vector3.up*(height+.25f),new Vector3(1.3f,.3f,1.3f),"Limestone",par);}
 static void Arch(Vector3 p,float width=7,float height=4.2f,Transform par=null){Column(p+Vector3.left*width*.5f,height,par);Column(p+Vector3.right*width*.5f,height,par);Shape("Supported arch lintel",p+Vector3.up*(height+.35f),new Vector3(width+1,.7f,1.2f),"Limestone",par,true);}
 static void Sign(string text,Vector3 p,float yaw=0,float size=.035f,Transform par=null){
  Shape("Sign stone footing",p+Vector3.up*.13f,new Vector3(.9f,.26f,.8f),"Limestone",par,true);
  Shape("Sign support post",p+Vector3.up*.65f,new Vector3(.12f,1.2f,.12f),"Brass",par);
  var panel=Shape("Inlaid information panel",p+Vector3.up*1.22f,new Vector3(3.3f,1.15f,.16f),"DeepTeal",par);panel.transform.rotation=Quaternion.Euler(0,yaw,0);
  var t=Group("Etched location sign",panel.transform);t.localPosition=new Vector3(0,0,-.57f);t.localScale=new Vector3(1/3.3f,1/1.15f,1/.16f);t.localRotation=Quaternion.identity;
  var tm=t.gameObject.AddComponent<TextMesh>();tm.text=text;tm.font=font;tm.fontSize=56;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.96f,.96f,.83f);tm.GetComponent<Renderer>().sharedMaterial=font.material;t.gameObject.layer=2;
 }
 static BTExpansionInteractable Control(int index,BTExpansionRole role,int slot,Vector3 p,string label){
  var g=Group(label);g.position=p;var c=g.gameObject.AddComponent<BTExpansionInteractable>();c.world=world;c.puzzle=index;c.role=role;c.slot=slot;c.label=label;
  Shape("Anchored control plinth",p+Vector3.up*.44f,new Vector3(.8f,.88f,.7f),"Limestone",g,true);
  if(role==BTExpansionRole.Valve||role==BTExpansionRole.Chime){
   Shape("Brass mechanism mount",p+new Vector3(0,.94f,0),new Vector3(.65f,.23f,.65f),"Brass",g,false,PrimitiveType.Cylinder);
   var sphere=Shape("Readable state lamp",p+new Vector3(0,1.15f,0),Vector3.one*.39f,slot==0?"Moon":slot==1?"Sun":"LeafLamp",g,false,PrimitiveType.Sphere);
   if(role==BTExpansionRole.Valve){var puzzle=world.puzzles[index];var lamps=puzzle.lamps.ToList();lamps.Add(sphere.GetComponent<Renderer>());puzzle.lamps=lamps.ToArray();}
  }else Shape("Interactive inset",p+new Vector3(0,1.02f,0),new Vector3(.60f,.25f,.48f),role==BTExpansionRole.Reset?"Coral":role==BTExpansionRole.Reward?"LeafLamp":"Moon",g,false,PrimitiveType.Sphere);
  return c;
 }
 static BTTimeTarget Device(Vector3 p,string label,int initial,bool growing=false){
  var go=Group(label);go.position=p;var target=go.gameObject.AddComponent<BTTimeTarget>();target.game=world.game;target.kind=growing?TargetKind.LivingTime:TargetKind.Mechanism;target.absorbAllowed=false;target.displayName=label;target.age=target.initialAge=initial;target.stateNames=new[]{growing?"เมล็ด / เยาว์วัย":"ใหม่",growing?"โตเต็มวัย":"ใช้งาน",growing?"แห้ง / แก่":"พัง"};target.states=new GameObject[3];
  for(int i=0;i<3;i++){var state=Group("Temporal state "+i,go);target.states[i]=state.gameObject;Shape("Grounded device base",p+Vector3.up*.14f,new Vector3(1.6f,.28f,1.5f),"Limestone",state,true);
   if(growing){Shape("Living stalk",p+Vector3.up*(i==0?.4f:.85f),new Vector3(.25f,i==0?.6f:1.45f,.25f),"Bark",state);for(int j=0;j<(i==0?2:5);j++){float a=j*2.4f;Shape("Grown foliage",p+new Vector3(Mathf.Cos(a)*.45f,i==0?.6f:1.6f,Mathf.Sin(a)*.45f),new Vector3(.95f,.65f,.95f),i==2?"Autumn":"LeafLight",state,false,PrimitiveType.Sphere);}}
   else {Shape("Supported clockwork housing",p+Vector3.up*.85f,new Vector3(1.2f,1.35f,.65f),i==2?"Rust":"DeepTeal",state,true);Shape("Clock face",p+new Vector3(0,1.10f,-.42f),new Vector3(.78f,.10f,.78f),i==2?"Rust":"Brass",state,false,PrimitiveType.Cylinder).transform.rotation=Quaternion.Euler(90,0,i*25);}
  }
  var aim=Shape("Time energy core",p+new Vector3(0,1.25f,-.70f),Vector3.one*.35f,"Moon",go,true,PrimitiveType.Sphere);target.aimPoint=aim.transform;target.focusRing=Shape("Target highlight",p+Vector3.up*.2f,new Vector3(1.8f,.05f,1.8f),"LeafLamp",go,false,PrimitiveType.Cylinder);target.focusRing.SetActive(false);target.Apply();return target;
 }
 static void Source(Vector3 p){
  var source=Group("Renewable garden source");source.position=p;Tree(p,.60f);var target=source.gameObject.AddComponent<BTTimeTarget>();target.game=world.game;target.kind=TargetKind.Plant;target.displayName="ต้นไม้พักฟื้นพลัง";target.states=new GameObject[0];target.stateNames=new[]{"มีพลังชีวิต"};target.absorbAllowed=true;target.regenPerSecond=12;
  target.aimPoint=Shape("Living energy focus",p+new Vector3(0,1.1f,-.55f),Vector3.one*.40f,"LeafLamp",source,true,PrimitiveType.Sphere).transform;
 }
 static void Puzzle(int index,Vector3 center,string title,string clue,string lore,int type,int[] goals){
  var p=world.puzzles[index]=new BTExpansionPuzzle{id=world.game.gameObject.scene.name+"_"+index,title=title,clue=clue,lore=lore,optional=type==2,goals=goals,approach=center+new Vector3(0,0,-6),lampMaterials=new[]{mats["Moon"],mats["Sun"],mats["LeafLamp"]}};
  Shape("Circular courtyard",center+Vector3.down*.05f,new Vector3(12,.05f,12),type==2?"Sand":"Limestone",null,false,PrimitiveType.Cylinder);
  Arch(center+new Vector3(0,0,4),7,3.8f);var gate=Shape("Restoration gate",center+new Vector3(0,1.7f,4),new Vector3(5.9f,3.3f,.20f),"DeepTeal",null,true);p.gate=gate.transform;p.gateClosed=gate.transform.localPosition;
  var controls=new List<BTExpansionInteractable>();
  if(type==0){bool factory=world.game is BTFactoryGame;p.temporal=new[]{Device(center+new Vector3(-4,0,0),factory?"ปั๊มหล่อเย็น":"กังหันส่งน้ำ",2),Device(center+new Vector3(0,0,0),factory?"วาล์วเปิดท่อฉุกเฉิน":"ต้นกล้ารับแสง",0,!factory),Device(center+new Vector3(4,0,0),factory?"ตัวกรองแรงดัน":"รากเก่าปิดช่องน้ำ",0,!factory)};p.ages=factory?new[]{0,2,1}:world.game is BTCityGame?new[]{1,0,2}:new[]{0,1,2};
   for(int i=0;i<3;i++)controls.Add(Control(index,BTExpansionRole.Valve,i,center+new Vector3(-4+i*4,0,-4),"หมุนช่อง "+(i+1)));
  }else if(type==1){for(int i=0;i<3;i++)controls.Add(Control(index,BTExpansionRole.Valve,i,center+new Vector3(-4+i*4,0,-1),"หมุนวงจร "+(i+1)));
   for(int i=0;i<2;i++)Beam("Visible linked copper conduit",center+new Vector3(-4+i*4,.25f,-1),center+new Vector3(i*4,.25f,-1),.16f,"Brass");
   Beam("Return circuit conduit",center+new Vector3(4,.22f,-1),center+new Vector3(4,.22f,1.5f),.13f,"Brass");Beam("Return circuit conduit",center+new Vector3(4,.22f,1.5f),center+new Vector3(-4,.22f,1.5f),.13f,"Brass");Beam("Return circuit conduit",center+new Vector3(-4,.22f,1.5f),center+new Vector3(-4,.22f,-1),.13f,"Brass");
  }else {p.sequence=world.game is BTFactoryGame?new[]{2,0,1,2,0,1}:world.game is BTCityGame?new[]{1,0,2,1,2}:new[]{0,2,1,0};for(int i=0;i<3;i++)controls.Add(Control(index,BTExpansionRole.Chime,i,center+new Vector3(-4+i*4,0,-1),"ระฆัง "+BTExpansionWorld.Symbol(i)));
   controls.Add(Control(index,BTExpansionRole.Reward,0,center+new Vector3(0,0,7),"เก็บใบไม้แห่งความทรงจำ"));
  }
  float consoleRow=type==0?-10:-8;
  controls.Add(Control(index,BTExpansionRole.Submit,0,center+new Vector3(0,0,consoleRow),"ยืนยันการฟื้นฟู"));controls.Add(Control(index,BTExpansionRole.Reset,0,center+new Vector3(4,0,consoleRow),"รีเซ็ตวงจร"));controls.Add(Control(index,BTExpansionRole.Clue,0,center+new Vector3(-4,0,consoleRow),"อ่านบันทึกสถานที่"));
  bool mirror=type==1&&world.game is BTCityGame;var display=mirror?goals.Reverse():goals.AsEnumerable();
  Sign(title+"\n"+(type==2?string.Join(" → ",p.sequence.Select(BTExpansionWorld.Symbol)):string.Join("   /   ",display.Select(BTExpansionWorld.Symbol)))+(mirror?"\nอ่านจากขวาไปซ้าย":""),center+new Vector3(0,0,3),0,.035f);
  p.controls=controls.ToArray();var bloom=Group("Restored flowering habitat");p.restoration=bloom.gameObject;
  for(int i=0;i<18;i++){float a=i*Mathf.PI*2/18;Flowers(center+new Vector3(Mathf.Cos(a)*5.4f,0,Mathf.Sin(a)*5.4f),bloom);}bloom.gameObject.SetActive(false);
  Source(center+new Vector3(-7,0,-5));
 }
 static void Glasshouse(Vector3 p,float width=17,float depth=17,bool factory=false){
  var group=Group(factory?"Victorian seed archive greenhouse":"Abandoned conservatory");
  for(int i=0;i<4;i++)foreach(float side in new[]{-1f,1f}){
   Vector3 foot=p+new Vector3(side*width*.5f,0,-depth*.5f+i*depth/3);Shape("Rib support foot",foot+Vector3.up*.2f,Vector3.one*.8f,"Limestone",group,true);
   Beam("Glasshouse structural rib",foot,foot+Vector3.up*6,.17f,"Brass",group);Beam("Pitched roof truss",foot+Vector3.up*6,p+new Vector3(0,8,-depth*.5f+i*depth/3),.16f,"Brass",group);
  }
  foreach(float side in new[]{-1f,1f}){Beam("Longitudinal roof ridge",p+new Vector3(0,8,-depth*.5f),p+new Vector3(0,8,depth*.5f),.17f,"Brass",group);Beam("Gutter",p+new Vector3(side*width*.5f,6,-depth*.5f),p+new Vector3(side*width*.5f,6,depth*.5f),.18f,"Brass",group);}
  // Partial glazed roof preserves clear views of the player and supported ribs.
  for(int i=0;i<3;i++)foreach(float side in new[]{-1f,1f}){var roof=Shape("Opal glass roof pane",p+new Vector3(side*width*.25f,7,-depth*.33f+i*depth/3),new Vector3(width*.54f,.05f,depth*.27f),"Glass",group);roof.transform.rotation=Quaternion.Euler(0,0,side*-13);}
  for(int i=0;i<5;i++)foreach(float side in new[]{-1f,1f}){var q=p+new Vector3(side*(width*.5f-1),0,-depth*.4f+i*depth*.2f);Shape("Supported flower bed",q+Vector3.up*.22f,new Vector3(1.2f,.44f,1.5f),"Limestone",group,true);Flowers(q+Vector3.up*.44f,group);}
 }
 static void DressOutside(int level,Vector3[] centers,float x0,float x1,float z0,float z1){
  for(int i=0;i<45;i++){var p=new Vector3(R(x0+1,x1-1),0,R(z0+1,z1-1));if(centers.Any(c=>Vector2.Distance(new Vector2(c.x,c.z),new Vector2(p.x,p.z))<10))continue;
   if(level<2){if(i%3==0)Tree(p,R(.6f,1.0f),i%11==0);else if(i%3==1)Flowers(p);else Shape("Mossy grounded boulder",p+Vector3.up*.38f,new Vector3(R(.8f,1.6f),.85f,R(.8f,1.4f)),"Rock",null,false,PrimitiveType.Sphere);}
  }
  if(level==0){Glasshouse(centers[1],18,20);for(int i=0;i<5;i++)Column(centers[0]+new Vector3(-8+i*4,0,8),R(2,4));}
  if(level==1){Glasshouse(centers[0],18,19);Glasshouse(centers[2],17,17);for(int i=0;i<3;i++){
    Vector3 p=new Vector3(x0+3+i*12,0,z0+2);Shape("Old town annex facade",p+Vector3.up*4,new Vector3(8,8,3),"Plaster",null,true);Shape("Mossy terracotta cornice",p+Vector3.up*8,new Vector3(8.5f,.35f,3.5f),"Terracotta");
    for(int k=0;k<3;k++){Shape("Inset arched window",p+new Vector3(-2+k*2,4,1.55f),new Vector3(1.1f,2,.1f),"DeepTeal");Shape("Supported window ledge",p+new Vector3(-2+k*2,3,1.7f),new Vector3(1.5f,.18f,.5f),"Limestone");}Tree(p+new Vector3(4.8f,0,2),.65f);
   }}
  if(level==2){
   FactoryDoorway();
   Shape("Grounded annex foundation",new Vector3((x0+x1)*.5f,-2,(z0+z1)*.5f),new Vector3(x1-x0+3,4,z1-z0+3),"Rock",null,true);
   foreach(float x in new[]{x0,x1}){Shape("Workshop retaining wall",new Vector3(x,3,(z0+z1)*.5f),new Vector3(.6f,6,z1-z0),"DeepTeal",null,true);for(float z=z0+2;z<z1;z+=6){Column(new Vector3(x,0,z),8);Beam("Overhead steel roof truss",new Vector3(x,8,z),new Vector3((x0+x1)*.5f,10,z),.25f,"Brass");}}
   Shape("Archive rear wall",new Vector3((x0+x1)*.5f,3,z0),new Vector3(x1-x0,6,.6f),"Plaster",null,true);
   Glasshouse(centers[2],18,17,true);
   foreach(var c in centers){Beam("Suspended lamp cable",c+Vector3.up*8,c+Vector3.up*5.8f,.035f,"Brass");Shape("Protected pendant light",c+Vector3.up*5.8f,new Vector3(.7f,.18f,.7f),"Sun");var light=Group("Warm workshop practical").gameObject.AddComponent<Light>();light.transform.position=c+Vector3.up*5.5f;light.type=LightType.Point;light.range=19;light.intensity=3.2f;light.color=new Color(1,.82f,.56f);}
   foreach(var c in centers.Take(2))for(int j=0;j<3;j++){var p=c+new Vector3(-7,0,j*3-2);Shape("Anchored pressure vessel",p+Vector3.up*1.1f,new Vector3(1.1f,1.1f,1.1f),"DeepTeal",null,true,PrimitiveType.Cylinder);Shape("Pressure vessel dome",p+Vector3.up*2.2f,Vector3.one*1.1f,"Brass",null,false,PrimitiveType.Sphere);Beam("Connected service pipe",p+Vector3.up*.35f,p+new Vector3(0,.35f,3),.18f,"Brass");}
  }
 }
 static void FactoryDoorway(){
  var wall=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Factory south cutaway wall");if(wall)wall.gameObject.SetActive(false);
  if(root.Find("Research wing / west retaining wall"))return;
  Shape("Research wing / west retaining wall",new Vector3(-15.25f,2,-29),new Vector3(25.5f,4,.7f),"DeepTeal",null,true);
  Shape("Research wing / east retaining wall",new Vector3(15.25f,2,-29),new Vector3(25.5f,4,.7f),"DeepTeal",null,true);
  Arch(new Vector3(0,0,-29),5.5f,4.4f);
 }
 public static void RepairFactoryConnection(){
  var scene=EditorSceneManager.OpenScene(BTJourney.FactoryPath);world=Object.FindFirstObjectByType<BTExpansionWorld>();root=GameObject.Find("Borrowed Time / Living World expansion").transform;rounded=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/ArchitectureBeveledCube.asset");mats=new Dictionary<string,Material>();
  foreach(var id in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"})){var p=AssetDatabase.GUIDToAssetPath(id);mats[Path.GetFileNameWithoutExtension(p)]=AssetDatabase.LoadAssetAtPath<Material>(p);}
  FactoryDoorway();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
 static void Landscape(int level){
  // Extend every old cliff into continuous bedrock and a visible valley floor.
  if(level<2){
   foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.EndsWith(" meadow")).ToArray()){var b=r.bounds;Foundation("Rooted original landmass",b.min.x-.25f,b.max.x+.25f,b.min.z-.25f,b.max.z+.25f,-7.5f,"Rock",false);}
   Foundation("Continuous valley floor",-175,175,-180,160,-16.05f,"Rock",false);
   Shape("River valley water",new Vector3(0,-5,0),new Vector3(260,.04f,260),"Water");
   float edge=level==0?-54:-62;int nx=36,nz=58;var vertices=new List<Vector3>();var tris=new List<int>();
   Func<float,float,float> height=(x,z)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((edge-x)/25))*(1.5f+Mathf.PerlinNoise(x*.024f+8,z*.024f+5)*18)+.06f;
   for(int iz=0;iz<=nz;iz++)for(int ix=0;ix<=nx;ix++){float x=Mathf.Lerp(edge-78,edge,ix/(float)nx),z=Mathf.Lerp(-105,76,iz/(float)nz);vertices.Add(new Vector3(x,height(x,z),z));}
   for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){int k=iz*(nx+1)+ix;tris.AddRange(new[]{k,k+nx+1,k+1,k+1,k+nx+1,k+nx+2});}
   MeshObject("Connected rolling forest terrain",vertices,tris,"Meadow",true);
   for(int i=0;i<70;i++){float x=R(edge-63,edge-2),z=R(-84,58);Tree(new Vector3(x,height(x,z),z),R(.7f,1.4f),i%13==0);}
   foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>!r.transform.IsChildOf(root)&&(r.name=="Distant rounded peak"||r.name=="Distant city hill"||r.name=="Soft cloud"||r.name=="City cloud")))r.gameObject.SetActive(false);
   for(int i=0;i<10;i++){float angle=i*2*Mathf.PI/10;Vector3 p=new Vector3(Mathf.Cos(angle)*130,-16,Mathf.Sin(angle)*130);
    Shape("Rooted distant mountain",p+Vector3.up*R(4,12),new Vector3(R(25,45),R(25,48),R(25,40)),i%2==0?"Hill":"Rock",null,false,PrimitiveType.Sphere);}
   for(int i=0;i<5;i++){var p=new Vector3(R(-85,85),R(29,42),R(-30,90));for(int j=0;j<3;j++)Shape("Soft cloud",p+new Vector3(j*3,Mathf.Sin(j)*.5f,0),new Vector3(9,2.8f,4.5f),"Cloud",null,false,PrimitiveType.Sphere);}
  }
 }
 static void PaletteAndLighting(int level){
  var colors=new Dictionary<string,string>{{"Grass","68B54B"},{"Leaf","347943"},{"LeafLight","78B957"},{"Moss","53894B"},{"Path","DDBB85"},{"Cliff","766857"},{"CliffLight","A28C6B"},{"StoneCream","D8CAAA"},{"Stone","A6AA98"},{"Bark","7C5A3D"},{"Wood","8C6243"},{"WoodLight","B98A58"},{"CityStone","C8B995"},{"CityRoof","719069"},{"CityTerracotta","B97855"},{"River","378FAD"},{"Water","378FAD"},{"FactoryDark","263B48"},{"SteelFloor","56707B"},{"SteelEdge","819596"},{"Factory","637B82"}};
  var remap=new Dictionary<Material,Material>();
  foreach(var id in AssetDatabase.FindAssets("t:Material",new[]{"Assets/_BorrowedTime/Materials"})){
   string path=AssetDatabase.GUIDToAssetPath(id);var original=AssetDatabase.LoadAssetAtPath<Material>(path);string key=Path.GetFileNameWithoutExtension(path);if(!colors.ContainsKey(key))continue;
   remap[original]=Mat("Regraded_"+key,colors[key],key=="Grass"||key=="Moss"||key.StartsWith("Leaf"));
  }
  foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(r.GetComponentInParent<BorrowedTime.Characters.BTCharacterModel>()||r.transform.IsChildOf(root))continue;
   r.sharedMaterials=r.sharedMaterials.Select(m=>m&&remap.ContainsKey(m)?remap[m]:m).ToArray();
  }
  foreach(var t in Object.FindObjectsByType<BTTimeTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(t.healthyMaterial&&remap.ContainsKey(t.healthyMaterial))t.healthyMaterial=remap[t.healthyMaterial];if(t.wiltMaterial&&remap.ContainsKey(t.wiltMaterial))t.wiltMaterial=remap[t.wiltMaterial];}
  foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(!text.transform.IsChildOf(root))text.gameObject.SetActive(false);
  foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){light.intensity=level==2?.85f:1.10f;light.color=new Color(1,.94f,.83f);light.shadowBias=.015f;light.shadowNormalBias=.05f;light.shadowStrength=.65f;}
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.40f,.56f,.64f);RenderSettings.ambientEquatorColor=new Color(.28f,.36f,.32f);RenderSettings.ambientGroundColor=new Color(.15f,.18f,.17f);
  RenderSettings.fog=true;RenderSettings.fogColor=level==2?new Color(.13f,.22f,.26f):new Color(.61f,.79f,.83f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=level==2?60:95;RenderSettings.fogEndDistance=level==2?150:220;
  var sky=new Material(Shader.Find("Borrowed Time/Storybook Sky"));string skyPath=Root+"/Materials/Sky_"+level+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(skyPath);if(old){EditorUtility.CopySerialized(sky,old);Object.DestroyImmediate(sky);sky=old;}else AssetDatabase.CreateAsset(sky,skyPath);RenderSettings.skybox=sky;
  world.game.player.view.clearFlags=level==2?CameraClearFlags.SolidColor:CameraClearFlags.Skybox;world.game.player.view.backgroundColor=RenderSettings.fogColor;
  foreach(var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)){
   var profile=ScriptableObject.CreateInstance<VolumeProfile>();if(volume.sharedProfile)foreach(var component in volume.sharedProfile.components)profile.components.Add(Object.Instantiate(component));
   if(!profile.TryGet<ColorAdjustments>(out var color))color=profile.Add<ColorAdjustments>();color.saturation.Override(3);color.contrast.Override(10);color.postExposure.Override(level==2?.10f:-.1f);
   if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.Neutral);
   if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>();bloom.intensity.Override(.15f);bloom.threshold.Override(1.3f);
   string path=Root+"/Materials/PostFX_"+level+".asset";var previous=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);if(previous){volume.sharedProfile=previous;Object.DestroyImmediate(profile);}else{AssetDatabase.CreateAsset(profile,path);foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);volume.sharedProfile=profile;}
  }
 }
 [MenuItem("Tools/Borrowed Time/Expansion/Expand and beautify all three levels")]
 public static void BuildAll(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");Folder(Root+"/Materials");Folder(Root+"/Meshes");
  for(int level=0;level<3;level++){
   var scene=EditorSceneManager.OpenScene(Paths[level]);var old=GameObject.Find("Borrowed Time / Living World expansion");if(old)Object.DestroyImmediate(old);
   var game=Object.FindFirstObjectByType<BTTrainingGame>();world=game.GetComponent<BTExpansionWorld>();if(!world)world=game.gameObject.AddComponent<BTExpansionWorld>();world.game=game;world.font=font=game.uiFont;world.chapter=new[]{"สวนบันทึกฤดูกาล","ย่านเรือนกระจก","คลังเมล็ดและห้องกังหัน"}[level];world.puzzles=new BTExpansionPuzzle[3];
   root=new GameObject("Borrowed Time / Living World expansion").transform;meshId=0;rng=new System.Random(712+level);rounded=BeveledCube();mats=new Dictionary<string,Material>();
   Mat("Meadow","68B54B",true);Mat("Leaf","347943",true);Mat("LeafLight","79B85A",true);Mat("Hill","4F826A",true);Mat("Rock","89785F");Mat("Sand","DEC493");Mat("Limestone","D9CDAF");Mat("Bark","775137");Mat("Brass","BE985B");Mat("DeepTeal","315564");Mat("Moon","64BEE5",false,.8f);Mat("Sun","F2C86A",false,.8f);Mat("LeafLamp","8ACF82",false,.8f);Mat("Petal","F4EADD");Mat("Coral","DD8976");Mat("Autumn","C69C52",true);Mat("Rust","926A53");Mat("Plaster","C7BEA6");Mat("Terracotta","BA7854");Mat("Cloud","EFF4EC");Mat("Glass","A4D6D2");
   var glass=mats["Glass"];glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;glass.SetShaderPassEnabled("ShadowCaster",false);var gc=ColorOf("A4D6D2");gc.a=.18f;glass.SetColor("_BaseColor",gc);
   var water=new Material(Shader.Find("Borrowed Time/Soft River"));string wp=Root+"/Materials/Water.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(wp);if(!existing){AssetDatabase.CreateAsset(water,wp);existing=water;}else Object.DestroyImmediate(water);mats["Water"]=existing;
   Vector3[] c;Vector3 connection,entry;float x0,x1,z0,z1;
   if(level==0){x0=-54;x1=-6;z0=-57;z1=-17;c=new[]{new Vector3(-17,0,-34),new Vector3(-38,0,-44),new Vector3(-43,0,-26)};connection=new Vector3(-16,0,-12);entry=new Vector3(-16,0,-22);}
   else if(level==1){x0=-62;x1=-8;z0=-63;z1=-25;c=new[]{new Vector3(-21,0,-43),new Vector3(-43,0,-51),new Vector3(-51,0,-34)};connection=new Vector3(-23,0,-19);entry=new Vector3(-23,0,-30);}
   else{x0=-32;x1=32;z0=-66;z1=-30;c=new[]{new Vector3(-17,0,-48),new Vector3(7,0,-54),new Vector3(22,0,-39)};connection=new Vector3(0,0,-25);entry=new Vector3(0,0,-35);}
   Ground(x0,x1,z0,z1,level==2?"DeepTeal":"Meadow");Ground(connection.x-2.3f,connection.x+2.3f,entry.z-.4f,connection.z+.5f,level==2?"DeepTeal":"Meadow");Trail(connection,entry,3.4f);
   var approach=c.Select(center=>center+new Vector3(0,0,-8)).ToArray();Trail(entry,new Vector3(entry.x,0,approach[0].z),3.5f);Trail(new Vector3(entry.x,0,approach[0].z),approach[0],3.5f);
   Trail(approach[0],approach[1],3.4f);Trail(approach[1],approach[2],3.4f);Trail(approach[2],entry,3.4f);
   string[] names=level==0?new[]{"สวนหยดน้ำแรก","หอสะท้อนฤดูกาล","สวนเสียงใบไม้"}:level==1?new[]{"เรือนกระจกสุริยา","ชุมทางประปาเก่า","ห้องความทรงจำเมือง"}:new[]{"ห้องสมดุลหล่อเย็น","ห้องวงจรกังหัน","คลังเมล็ดสุดท้าย"};
   string temporalClue=level==0?"กังหันใหม่ส่งน้ำ / ต้นกล้าโตเต็มวัยรับแสง / รากแก่เปิดช่องน้ำ":level==1?"กังหันช่วงใช้งานส่งน้ำพอดี / ต้นกล้าระยะเยาว์ไม่บังหลังคา / รากแก่เปิดท่อน้ำ":"ปั๊มต้องใหม่ / วาล์วเก่าจนชำรุดจึงเปิดท่อฉุกเฉิน / ตัวกรองต้องอยู่ช่วงใช้งาน";
   Puzzle(0,c[0],names[0],temporalClue+"\nวาล์วอ่านซ้ายไปขวาตามป้าย เปลี่ยนอายุอย่างเดียวไม่พอ ต้องส่งน้ำถูกช่องด้วย", "Norman ทดลองฟื้นฟูระบบนิเวศด้วยน้ำ แสง และเวลา",0,level==0?new[]{1,2,0}:level==1?new[]{2,0,1}:new[]{0,2,1});
   string circuitClue=level==0?"วงจรเชื่อมเป็นวง: หมุนช่องใด ช่องนั้นและช่องถัดไปจะเปลี่ยนพร้อมกัน":level==1?"ป้ายช่างเขียนจากฝั่งกระจก ต้องอ่านเป้าหมายขวาไปซ้าย\nหมุนช่องใด ช่องนั้นและช่องถัดไปเปลี่ยนพร้อมกัน":"เปิดระบบหล่อเย็นก่อน วงจรนี้ไหลย้อน: หมุนช่องใด ช่องนั้นและช่องก่อนหน้าจะเปลี่ยนพร้อมกัน";
   Puzzle(1,c[1],names[1],circuitClue+"\nจันทร์ → อาทิตย์ → ใบไม้ → จันทร์\nต้องให้ไฟปลายทางตรงป้ายพร้อมกัน", "ปั๊มเมืองเคยกระจายน้ำให้สวนทั้งย่าน ก่อนโรงงานจะต่อท่อเสียเข้าระบบ",1,level==2?new[]{2,2,1}:level==1?new[]{0,2,1}:new[]{1,2,1});
   string melody=level==0?"คืนแรกใต้จันทร์ → ใบไม้ผลิ → ดวงอาทิตย์ → คืนที่สงบ":level==1?"อาทิตย์ → จันทร์ → ใบไม้ → อาทิตย์ → ใบไม้":"ใบไม้ → จันทร์ → อาทิตย์ แล้วเล่าซ้ำอีกหนึ่งรอบ";
   Puzzle(2,c[2],names[2],"เมื่อสายน้ำกับวงจรแสงกลับมา ให้ระฆังเล่าเรื่องฤดูกาล\n"+melody+"\nทำนองถูกต้องจะเปิดสวนลับให้เก็บใบไม้แห่งความทรงจำ", "การฟื้นฟูไม่ใช่คำสั่งเดียว แต่คือการดูแลชีวิตให้กลับมาเชื่อมต่อกัน",2,new[]{0,0,0});
   DressOutside(level,c,x0,x1,z0,z1);Landscape(level);PaletteAndLighting(level);
   Sign(world.chapter+"\nระบบน้ำ • ระบบแสง • สวนลับ",entry+new Vector3(3,0,2),0,.04f);
   game.mapCameraPosition=new Vector3(-65,90,-96);game.mapCameraTarget=new Vector3(-7,0,-10);game.mapCameraSize=65;world.Refresh();
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("EXPANDED_LEVEL_READY "+scene.name+" / three new puzzle courtyards");
  }
  AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Paths[0]);
 }
}
}
#endif
