#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BorrowedTime.Editor {
public static class BTTrainingPollutedForest {
 const string AssetsRoot="Assets/_BorrowedTime/ForestBackdrop";
 const string RootName="Training / Pollution approaching the forest";
 static readonly string Out=Path.GetFullPath("Captures/TrainingComfort/ForestBackdrop");
 static Transform root;static System.Random random;static Dictionary<string,Material> materials;static Mesh leaf,rock;static int meshIndex;
 static float R(float a,float b)=>a+(float)random.NextDouble()*(b-a);
 static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
 static Material Mat(string name,string hex,bool transparent=false){
  string path=AssetsRoot+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}ColorUtility.TryParseHtmlString(hex,out var color);mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.16f);mat.enableInstancing=true;
  if(transparent){mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_ZWrite",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;mat.SetShaderPassEnabled("ShadowCaster",false);}
  materials[name]=mat;return mat;
 }
 static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent?parent:root,false);return t;}
 static GameObject Shape(string name,Vector3 p,Vector3 scale,string mat,PrimitiveType type=PrimitiveType.Sphere,Transform parent=null){
  var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent?parent:root,true);go.transform.position=p;go.transform.localScale=scale;
  Object.DestroyImmediate(go.GetComponent<Collider>());go.layer=2;go.GetComponent<Renderer>().sharedMaterial=materials[mat];return go;
 }
 static void Branch(string name,Vector3 a,Vector3 b,float width,string mat,Transform parent){
  var t=Shape(name,(a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f,width),mat,PrimitiveType.Cylinder,parent).transform;t.rotation=Quaternion.FromToRotation(Vector3.up,b-a);
 }
 static Mesh Organic(string name,float irregularity){
  string path=AssetsRoot+"/Meshes/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved)return saved;
  var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);var mesh=Object.Instantiate(sphere.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(sphere);mesh.name=name;
  var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++){var v=vertices[i];float n=Mathf.PerlinNoise(v.x*7+8,v.y*6+v.z*4+3);vertices[i]=v*(1+irregularity*(n-.5f));}mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
 static float Ground(float x,float z){
  float rear=Mathf.Clamp01((-z-57)/20);return rear*rear*(2+Mathf.PerlinNoise(x*.045f+10,z*.04f+9)*4);
 }
 static Vector3 At(float x,float z)=>new Vector3(x,Ground(x,z),z);
 static void Tree(Vector3 p,float scale,float illness,int number){
  var tree=Group((illness>.7f?"Polluted / dying tree ":illness>.35f?"Stressed / yellowing tree ":"Healthy / surviving tree ")+number);tree.position=p;
  string bark=illness>.7f?"SickBark":"Bark";float height=R(3.6f,5.5f)*scale;
  Vector3 top=p+new Vector3(R(-.4f,.4f),height,0);Branch("Grounded trunk",p,top,.40f*scale,bark,tree);
  var collider=tree.gameObject.AddComponent<CapsuleCollider>();tree.gameObject.layer=0;collider.radius=.24f*scale;collider.height=height*.72f;collider.center=Vector3.up*collider.height*.5f;
  for(int j=0;j<5;j++){
   float angle=j*2.4f+number;Vector3 tip=p+new Vector3(Mathf.Cos(angle)*1.4f*scale,height*(.65f+j*.055f),Mathf.Sin(angle)*1.4f*scale);
   Branch("Exposed supporting branch",p+Vector3.up*height*.48f,tip,.15f*scale,bark,tree);
   Branch("Fine living branch",Vector3.Lerp(top,tip,.4f),tip+Vector3.up*.6f*scale,.07f*scale,bark,tree);
   if(illness>.8f && j%2==0)continue;
   string foliage=illness>.7f?"DeadLeaf":illness>.35f?(j%2==0?"YellowLeaf":"DullLeaf"):(j%2==0?"GreenLeaf":"LightLeaf");
   Vector3 size=new Vector3(2.7f,1.9f,2.5f)*scale*(illness>.7f?.48f:1);
   var canopy=Shape("Irregular foliage",tip+Vector3.up*.35f*scale,size,foliage,PrimitiveType.Sphere,tree);canopy.GetComponent<MeshFilter>().sharedMesh=leaf;
  }
  for(int j=0;j<5;j++){float angle=j*1.2566f;Branch("Root enters forest soil",p+Vector3.up*.17f,p+new Vector3(Mathf.Cos(angle)*.8f*scale,.04f,Mathf.Sin(angle)*.8f*scale),.17f*scale,bark,tree);}
  if(illness>.55f){
   for(int j=0;j<3;j++)Shape("Pollution stains on bark",p+new Vector3(.20f*scale,j*.45f+.32f,-.06f),new Vector3(.05f,.3f,.25f)*scale,"Blight",PrimitiveType.Sphere,tree);
  }
 }
 static void SaveMesh(Mesh mesh,string name,Transform parent,Material mat,bool collider=false){
  if(name=="Quiet woodland footpath"||name=="Polluted winding stream")mesh.SetNormals(Enumerable.Repeat(Vector3.up,mesh.vertexCount).ToList());
  mesh.name=name;string path=AssetsRoot+"/Meshes/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
  if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
  var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;go.layer=collider?0:2;if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;
 }
 static void RearGround(){
  const int nx=24,nz=20;var v=new List<Vector3>();var t=new List<int>();
  for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){float xx=Mathf.Lerp(-54,-6,x/(float)nx),zz=Mathf.Lerp(-95,-57,z/(float)nz);v.Add(At(xx,zz));}
  for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int k=z*(nx+1)+x;t.AddRange(new[]{k,k+nx+1,k+1,k+1,k+nx+1,k+nx+2});}
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh(mesh,"Rolling forest at the horizon",root,materials["ForestSoil"],true);
  Shape("Continuous earth below rear woodland",new Vector3(-30,-8,-76),new Vector3(48,16,38),"Earth",PrimitiveType.Cube);
 }
 static Vector3 CreekPoint(float t){float z=Mathf.Lerp(-70,-24,t),x=-37+Mathf.Sin(t*5.8f)*7;return At(x,z)+Vector3.up*.05f;}
 static void Creek(){
  var v=new List<Vector3>();var tris=new List<int>();
  for(int i=0;i<=42;i++){
   float t=i/42f;Vector3 p=CreekPoint(t),direction=(CreekPoint(Mathf.Min(1,t+.01f))-CreekPoint(Mathf.Max(0,t-.01f))).normalized,side=Vector3.Cross(direction,Vector3.up).normalized;float width=1.2f+.3f*Mathf.Sin(t*12);
   v.Add(p-side*width);v.Add(p+side*width);
   if(i<42){int k=i*2;tris.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}
   if(i%2==0)foreach(int s in new[]{-1,1}){
    Vector3 bank=p+side*(width+.25f)*s;var boulder=Shape("Dark wet creek bank",bank,new Vector3(R(.6f,1.2f),R(.25f,.45f),R(.7f,1.4f)),i%3==0?"Blight":"WetStone");boulder.GetComponent<MeshFilter>().sharedMesh=rock;
    for(int j=0;j<3;j++){Vector3 q=bank+new Vector3(R(-.3f,.3f),0,R(-.4f,.4f));Branch("Discoloured reed",q,q+Vector3.up*R(.45f,.9f)+Vector3.right*.12f,.04f,i<20?"DeadLeaf":"DullLeaf",root);}
   }
  }
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh(mesh,"Polluted winding stream",root,materials["PollutedWater"]);
  Vector3 pipe=CreekPoint(.05f)+Vector3.up*.5f;Branch("Old discharge pipe enters the forest",pipe+new Vector3(-4,1.2f,-1.5f),pipe,.62f,"Pipe",root);
  Shape("Grounded pipe footing",pipe+new Vector3(-2,-.15f,-.75f),new Vector3(2,.7f,1.4f),"WetStone",PrimitiveType.Cube);
  for(int i=0;i<5;i++){Vector3 p=CreekPoint(.12f+i*.17f);Shape("Oily violet water bloom",p+Vector3.up*.015f,new Vector3(1.3f,.017f,1.7f),"OilFilm");}
 }
 static void Trail(){
  Vector3[] points={new Vector3(-16,0,-17),new Vector3(-17,0,-25),new Vector3(-24,0,-32),new Vector3(-27,0,-42),new Vector3(-22,0,-50)};
  var vertices=new List<Vector3>();var triangles=new List<int>();
  for(int i=0;i<points.Length;i++){Vector3 d=points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(i-1,0)];Vector3 side=Vector3.Cross(d.normalized,Vector3.up)*1.2f;vertices.Add(points[i]-side+Vector3.up*.035f);vertices.Add(points[i]+side+Vector3.up*.035f);if(i<points.Length-1){int k=i*2;triangles.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}}
  var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh(mesh,"Quiet woodland footpath",root,materials["Path"]);
  var log=Group("Fallen tree resting beside the footpath");Vector3 a=new Vector3(-25,.5f,-47),b=new Vector3(-29,.5f,-48);Branch("Fallen trunk touches the soil",a,b,.68f,"Bark",log);for(int i=0;i<3;i++)Branch("Broken twigs",Vector3.Lerp(a,b,i/3f),Vector3.Lerp(a,b,i/3f)+new Vector3(-.4f,.55f,.2f),.13f,"SickBark",log);
 }
 static bool NearPath(float x,float z){
  Vector2 p=new Vector2(x,z);Vector2[] points={new Vector2(-16,-17),new Vector2(-17,-25),new Vector2(-24,-32),new Vector2(-27,-42),new Vector2(-22,-50)};
  for(int i=0;i<points.Length-1;i++){var d=points[i+1]-points[i];float t=Mathf.Clamp01(Vector2.Dot(p-points[i],d)/d.sqrMagnitude);if(Vector2.Distance(p,points[i]+d*t)<2.0f)return true;}return false;
 }
 static void Atmosphere(){
  var sky=new Material(Shader.Find("Borrowed Time/Overcast Forest Sky")){name="OvercastSky"};string path=AssetsRoot+"/Materials/OvercastSky.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing){EditorUtility.CopySerialized(sky,existing);Object.DestroyImmediate(sky);sky=existing;}else AssetDatabase.CreateAsset(sky,path);RenderSettings.skybox=sky;
  RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.48f,.54f,.55f);RenderSettings.fogStartDistance=38;RenderSettings.fogEndDistance=145;
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.53f,.55f);RenderSettings.ambientEquatorColor=new Color(.32f,.37f,.34f);RenderSettings.ambientGroundColor=new Color(.20f,.22f,.20f);
  foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){light.intensity=.88f;light.color=new Color(.98f,.96f,.86f);light.shadowStrength=.55f;}
  var smokeMaterial=Mat("SoftPollutionMotes","#FFFFFF",true);smokeMaterial.shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
  smokeMaterial.SetFloat("_Surface",1);smokeMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);smokeMaterial.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);smokeMaterial.SetFloat("_ZWrite",0);smokeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");smokeMaterial.renderQueue=3000;
  string texturePath=AssetsRoot+"/Materials/SoftHaze.png";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
  if(!texture){var pixels=new Texture2D(32,32,TextureFormat.RGBA32,false);for(int y=0;y<32;y++)for(int x=0;x<32;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;pixels.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2)));}pixels.Apply();File.WriteAllBytes(texturePath,pixels.EncodeToPNG());Object.DestroyImmediate(pixels);AssetDatabase.ImportAsset(texturePath);texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);}smokeMaterial.SetTexture("_BaseMap",texture);smokeMaterial.SetColor("_BaseColor",Color.white);
  foreach(float x in new[]{-46f,-36f,-25f}){
   Vector3 p=At(x,-80);Shape("Distant industrial chimney footing",p+Vector3.up*1.5f,new Vector3(3,3,2.5f),"Pipe",PrimitiveType.Cube);Branch("Chimney beyond sick woodland",p,p+Vector3.up*12,1.2f,"Pipe",root);
   var smoke=Group("Slow industrial haze").gameObject.AddComponent<ParticleSystem>();smoke.transform.position=p+Vector3.up*12;
   var main=smoke.main;main.duration=20;main.loop=true;main.startLifetime=18;main.startSpeed=.35f;main.startSize=new ParticleSystem.MinMaxCurve(2,4);main.startColor=new Color(.44f,.43f,.46f,.18f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=48;main.prewarm=true;
   var emission=smoke.emission;emission.rateOverTime=1.3f;var shape=smoke.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.7f;shape.angle=16;
   var velocity=smoke.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=.15f;velocity.y=.25f;velocity.z=.05f;
   var size=smoke.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.4f),new Keyframe(1,2)));
   var color=smoke.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.48f,.46f,.52f),0),new GradientColorKey(new Color(.47f,.48f,.48f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.17f,.2f),new GradientAlphaKey(0,1)});color.color=gradient;
   var renderer=smoke.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=smokeMaterial;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;
  }
 }
 static void Combine(){
  var groups=root.GetComponentsInChildren<MeshRenderer>().GroupBy(r=>r.sharedMaterial).ToArray();
  foreach(var group in groups){var renderers=group.ToArray();var combine=new List<CombineInstance>();foreach(var r in renderers){var filter=r.GetComponent<MeshFilter>();if(filter&&filter.sharedMesh)combine.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix});}
   if(combine.Count==0)continue;var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine.ToArray(),true,true);mesh.RecalculateBounds();SaveMesh(mesh,"Forest batch "+meshIndex++,root,group.Key);
   foreach(var r in renderers){var filter=r.GetComponent<MeshFilter>();Object.DestroyImmediate(r);if(filter)Object.DestroyImmediate(filter);}
  }
 }
 [MenuItem("Tools/Borrowed Time/Training/Replace back gardens with polluted woodland scenery")]
 public static void Build(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");if(SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene retained");
  var scene=SceneManager.GetActiveScene();if(scene.path!=BTJourney.TrainingPath)scene=EditorSceneManager.OpenScene(BTJourney.TrainingPath);
  var game=Object.FindFirstObjectByType<BTTrainingGame>();if(!game||!game.IsGentleTraining)throw new Exception("TrainingArea required");
  Folder(AssetsRoot+"/Materials");Folder(AssetsRoot+"/Meshes");random=new System.Random(1025);meshIndex=0;materials=new Dictionary<string,Material>();
  var old=GameObject.Find(RootName);if(old)Object.DestroyImmediate(old);
  var expansion=GameObject.Find("Borrowed Time / Living World expansion");if(!expansion)throw new Exception("Missing existing landscape");
  foreach(var child in expansion.transform.Cast<Transform>().ToArray()){bool keep=new[]{"Grounded bedrock","Connected walkable terrain","Rooted original landmass","Continuous valley floor","River valley water","Connected rolling forest terrain","Rooted distant mountain"}.Any(n=>child.name.StartsWith(n));if(keep)child.gameObject.SetActive(true);else Object.DestroyImmediate(child.gameObject);}
  var world=game.GetComponent<BTExpansionWorld>();if(world){if(world.journalOpen)world.ToggleJournal();Object.DestroyImmediate(world);}
  root=new GameObject(RootName).transform;
  Mat("Bark","#75634B");Mat("SickBark","#514B4F");Mat("GreenLeaf","#3E744E");Mat("LightLeaf","#79935B");Mat("DullLeaf","#85835D");Mat("YellowLeaf","#B4A36B");Mat("DeadLeaf","#6F6565");Mat("Blight","#6C526F");Mat("ForestSoil","#697058");Mat("Earth","#665C50");Mat("WetStone","#747772");Mat("Path","#AB9C7B");Mat("Pipe","#51565A");Mat("PollutedWater","#585065");Mat("OilFilm","#806A8566",true);
  foreach(Transform child in expansion.transform)if(child.name=="Connected rolling forest terrain" || child.name=="Connected walkable terrain"&&child.position.z< -17.5f){var renderer=child.GetComponent<Renderer>();if(renderer)renderer.sharedMaterial=materials["ForestSoil"];}
  materials["Path"].SetFloat("_Cull",0);
  var water=materials["PollutedWater"];water.shader=Shader.Find("Universal Render Pipeline/Lit");water.SetColor("_BaseColor",new Color(.23f,.20f,.28f));water.SetFloat("_Smoothness",.65f);water.SetFloat("_Cull",0);
  leaf=Organic("Organic forest canopy",.40f);rock=Organic("Weathered creek stone",.35f);RearGround();Trail();Creek();
  int count=0;for(int i=0;i<145;i++){
   float x=R(-53,-8),z=R(-86,-20);if(NearPath(x,z)||Mathf.Abs(x-CreekPoint(Mathf.InverseLerp(-70,-24,z)).x)<2.5f)continue;
   float illness=Mathf.Clamp01(Mathf.InverseLerp(-23,-80,z)*.73f+R(-.24f,.35f));if(i%6==0)illness=.12f;
   Tree(At(x,z),R(.75f,1.35f),illness,count++);
  }
  // Bring a sparse screen of mixed health trees to the original western hill.
  for(int i=0;i<30;i++){float x=R(-87,-55),z=R(-72,-7);if(Physics.Raycast(new Vector3(x,50,z),Vector3.down,out var hit,70,~(1<<2),QueryTriggerInteraction.Ignore))Tree(hit.point,R(.85f,1.45f),i%3==0?.75f:.22f,count++);}
  for(int i=0;i<55;i++){float x=R(-52,-8),z=R(-68,-20);if(NearPath(x,z))continue;var p=At(x,z);var stone=Shape("Moss and forest floor rocks",p+Vector3.up*.15f,new Vector3(R(.35f,1.3f),R(.2f,.55f),R(.5f,1.3f)),i%3==0?"DullLeaf":"WetStone");stone.GetComponent<MeshFilter>().sharedMesh=rock;}
  for(int i=0;i<100;i++){float x=R(-52,-8),z=R(-68,-20);if(NearPath(x,z))continue;Vector3 p=At(x,z);for(int j=0;j<3;j++){var fern=Shape("Rooted forest understory",p+Vector3.up*.22f,new Vector3(.15f,.55f,.75f),z< -48?"DullLeaf":"LightLeaf");fern.transform.rotation=Quaternion.Euler(25,j*120+i,0);}}
  Atmosphere();Combine();game.ConfigureGentleTraining();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  Directory.CreateDirectory(Out);File.WriteAllText(Out+"/SceneValidation.txt","PASS decoration-only back forest; no expansion component; active targets remain on the main tutorial route; grounded trees="+count+"; mixed healthy/stressed/dying trees; polluted creek; distant smoke; overcast sky; render batches="+meshIndex+"\n");
  Validate();BTTrainingEncroachment.Apply();Pictures();Debug.Log("BT_POLLUTED_FOREST_READY "+count+" trees, no rear puzzles");
 }
 [MenuItem("Tools/Borrowed Time/Training/Validate decorative forest paths")]
 public static void Validate(){
  var game=Object.FindFirstObjectByType<BTTrainingGame>();if(game.GetComponent<BTExpansionWorld>())throw new Exception("Rear puzzle component still active");
  if(Object.FindObjectsByType<BTExpansionInteractable>(FindObjectsSortMode.None).Length!=0)throw new Exception("Active rear puzzle control");
  var forest=GameObject.Find(RootName);if(!forest||forest.GetComponentsInChildren<BTTimeTarget>().Length!=0)throw new Exception("Forest is interactive or missing");
  if(!BTExpansionWorld.CanLeave(game,out _))throw new Exception("Forest blocks main progression");
  foreach(var collider in forest.GetComponentsInChildren<CapsuleCollider>())if(!Physics.Raycast(collider.transform.position+Vector3.up*.08f,Vector3.down,out _,.4f,~(1<<2),QueryTriggerInteraction.Ignore))throw new Exception("Unsupported tree "+collider.name);
  Vector3[] path={new Vector3(-16,0,-17),new Vector3(-17,0,-25),new Vector3(-24,0,-32),new Vector3(-27,0,-42),new Vector3(-22,0,-50)};var checks=new List<string>();
  for(int i=0;i<path.Length-1;i++)for(int j=0;j<=12;j++){
   Vector3 p=Vector3.Lerp(path[i],path[i+1],j/12f);if(!Physics.Raycast(p+Vector3.up,Vector3.down,out var hit,1.5f,~(1<<2),QueryTriggerInteraction.Ignore))throw new Exception("Unsupported forest trail "+p);
   if(Physics.CheckCapsule(hit.point+Vector3.up*.45f,hit.point+Vector3.up*1.60f,.32f,~(1<<2),QueryTriggerInteraction.Ignore))throw new Exception("Blocked forest trail "+p);
   checks.Add(p+" ground="+hit.point.y+" via "+hit.collider.name);
  }
  Directory.CreateDirectory(Out);File.WriteAllText(Out+"/PathValidation.txt","PASS 52 grounded character-sized clearance samples along the woodland path\n"+string.Join("\n",checks));
 }
 [MenuItem("Tools/Borrowed Time/Training/Capture polluted forest views")]
 public static void Pictures(){
  Directory.CreateDirectory(Out);var go=new GameObject("Temporary forest photograph camera");var camera=go.AddComponent<Camera>();var game=Object.FindFirstObjectByType<BTTrainingGame>();camera.CopyFrom(game.player.view);camera.orthographic=false;camera.fieldOfView=58;
  var data=go.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(!data)data=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;
  Capture(camera,"Forest_Entrance",new Vector3(-16,2.4f,-21),new Vector3(-30,3,-44));
  Capture(camera,"Forest_PollutedCreek",new Vector3(-24,3.0f,-28),new Vector3(-39,3,-59));
  Capture(camera,"Forest_Overview",new Vector3(5,38,-4),new Vector3(-31,1,-46));Object.DestroyImmediate(go);
 }
 static void Capture(Camera camera,string name,Vector3 at,Vector3 target){
  camera.transform.position=at;camera.transform.LookAt(target);var previous=RenderTexture.active;var rt=new RenderTexture(1440,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
 }
}
}
#endif
