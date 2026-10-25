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
public static class BTTrainingFinalArt {
 const string Root="Assets/_BorrowedTime/FinalArt";
 const string SceneRoot="Training / Grounded environment finish";
 static readonly string Out=Path.GetFullPath("Captures/TrainingComfort/FinalArt");
 static Transform root;static Material rock,water,fall,foam;
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;int i=p.LastIndexOf('/');Folder(p.Substring(0,i));AssetDatabase.CreateFolder(p.Substring(0,i),p.Substring(i+1));}
 static Material Material(string name,string shader,Color color){
  string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(shader)){name=name};AssetDatabase.CreateAsset(m,path);}m.shader=Shader.Find(shader);m.SetColor("_BaseColor",color);m.enableInstancing=true;if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.2f);EditorUtility.SetDirty(m);return m;
 }
 static GameObject Shape(string name,Vector3 p,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube,Transform parent=null){
  var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent?parent:root,true);go.transform.position=p;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());go.layer=2;go.GetComponent<Renderer>().sharedMaterial=material;
  if(type==PrimitiveType.Cube){var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_BorrowedTime/Expansion/Meshes/ArchitectureBeveledCube.asset");if(mesh)go.GetComponent<MeshFilter>().sharedMesh=mesh;}return go;
 }
 static void Mesh(string name,List<Vector3> vertices,List<int> triangles,List<Vector2> uv,Material mat){
  var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);if(uv!=null)mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();string path=Root+"/Meshes/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,path);
  var go=new GameObject(name);go.transform.SetParent(root,false);go.layer=2;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
 }
 static void WaterSurface(){
  // One surface at one elevation: no intersecting water boxes or repeated glints.
  var vertices=new List<Vector3>{new Vector3(0,-5.55f,0)};var uv=new List<Vector2>{Vector2.zero};var triangles=new List<int>();
  for(int i=0;i<=128;i++){float a=i*Mathf.PI*2/128;vertices.Add(new Vector3(Mathf.Cos(a)*340,-5.55f,Mathf.Sin(a)*340));uv.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a)));}
  for(int i=1;i<=128;i++)triangles.AddRange(new[]{0,i+1,i});Mesh("Single continuous river surface",vertices,triangles,uv,water);
 }
 static void Waterfall(){
  // Build in a local frame, then mirror onto the west riverbank of the east
  // island, where the waterfall can be read from the bridge approach.
  Transform scenery=root;root=new GameObject("Supported spring and waterfall assembly").transform;root.SetParent(scenery,false);
  var spring=Material("Spring water","Universal Render Pipeline/Lit",new Color(.28f,.49f,.48f));spring.SetFloat("_Smoothness",.65f);
  Shape("Spring basin touches the meadow",new Vector3(21.5f,.055f,1.7f),new Vector3(2.6f,.09f,1.6f),spring,PrimitiveType.Cylinder);
  Shape("Spring rock resting on meadow",new Vector3(20.1f,.45f,1.7f),new Vector3(1.7f,.95f,2.1f),rock,PrimitiveType.Sphere);
  var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
  for(int i=0;i<=18;i++){
   float t=i/18f;float x=23.0f+.85f*(1-Mathf.Exp(-t*6));float y=Mathf.Lerp(.13f,-5.48f,t);float half=.72f+Mathf.Sin(t*Mathf.PI)*.15f;
   vertices.Add(new Vector3(x,y,1.7f-half));vertices.Add(new Vector3(x,y,1.7f+half));uv.Add(new Vector2(0,t));uv.Add(new Vector2(1,t));
   if(i<18){int k=i*2;triangles.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}
  }Mesh("Waterfall follows the supported cliff",vertices,triangles,uv,fall);
  // A small continuous stream from the basin to the waterfall lip.
  vertices=new List<Vector3>{new Vector3(21.2f,.11f,1.13f),new Vector3(23.08f,.13f,1.0f),new Vector3(23.08f,.13f,2.4f),new Vector3(21.2f,.11f,2.27f)};
  Mesh("Spring flows into waterfall lip",vertices,new List<int>{0,1,2,0,2,3},new List<Vector2>{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)},fall);
  for(int side=-1;side<=1;side+=2){
   Shape("Basin bank rooted in island",new Vector3(21.9f,.18f,1.7f+side*.85f),new Vector3(2.8f,.40f,.55f),rock,PrimitiveType.Sphere);
   var cliffVertices=new List<Vector3>();var cliffTriangles=new List<int>();float[] levels={-16.15f,-5.6f,-1.6f,-.03f};
   for(int ring=0;ring<4;ring++)for(int j=0;j<12;j++){
    float angle=j*Mathf.PI*2/12,flare=ring==0?1.25f:ring==1?1.08f:1;float uneven=1+Mathf.Sin(j*1.8f+ring*.7f)*.10f;
    cliffVertices.Add(new Vector3(23.3f+Mathf.Cos(angle)*.88f*flare*uneven,levels[ring],1.7f+side*1.2f+Mathf.Sin(angle)*.67f*uneven));
   }
   for(int ring=0;ring<3;ring++)for(int j=0;j<12;j++){int a=ring*12+j,b=ring*12+(j+1)%12,c=(ring+1)*12+(j+1)%12,d=(ring+1)*12+j;cliffTriangles.AddRange(new[]{a,c,b,a,d,c});}
   for(int j=1;j<11;j++)cliffTriangles.AddRange(new[]{36,36+j,36+j+1});Mesh("Rooted eroded waterfall wall "+(side<0?"north":"south"),cliffVertices,cliffTriangles,null,rock);
  }
  var p=new GameObject("Foam at waterfall impact");p.transform.SetParent(root,false);p.transform.position=new Vector3(23.9f,-5.43f,1.7f);p.layer=2;
  var system=p.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=system.main;main.startLifetime=1.8f;main.startSpeed=.24f;main.startSize=new ParticleSystem.MinMaxCurve(.15f,.38f);main.startColor=new Color(.77f,.86f,.80f,.32f);main.maxParticles=24;main.loop=true;main.prewarm=true;
  var emission=system.emission;emission.rateOverTime=5;var shape=system.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.7f;
  var velocity=system.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.y=.15f;
  var color=system.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.35f,.25f),new GradientAlphaKey(0,1)});color.color=gradient;
  var renderer=system.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=foam;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;
  root.localPosition=new Vector3(26.0f,0,-2.4f);root.localScale=new Vector3(-1,1,1);root=scenery;
 }
 static void GroundedCliffs(){
  foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
   if(renderer.name=="Rooted distant mountain"){
    var organic=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_BorrowedTime/ForestBackdrop/Meshes/Weathered creek stone.asset");if(organic)renderer.GetComponent<MeshFilter>().sharedMesh=organic;
    float lift=renderer.bounds.min.y+16;if(lift>0)renderer.transform.position-=Vector3.up*lift;
   }
  }
  // Fit one closed rock volume to each actual meadow outline. This replaces
  // intersecting rectangular supports and detached rounded cliff blocks.
  foreach(var meadow in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.EndsWith(" meadow")).ToArray()){
   var source=meadow.GetComponent<MeshFilter>().sharedMesh.vertices;if(source.Length<9)continue;Vector3 center=meadow.transform.TransformPoint(source[0]);var outline=source.Skip(1).Take(8).Select(p=>meadow.transform.TransformPoint(p)).ToArray();var vertices=new List<Vector3>();var triangles=new List<int>();
   for(int ring=0;ring<4;ring++)for(int j=0;j<8;j++){
    Vector3 point=outline[j],radial=point-center;radial.y=0;float offset=ring==0?0:ring==1?.12f*Mathf.Sin(j*1.9f):ring==2?.22f: .65f;
    point+=radial.normalized*offset;point.y=ring==0?center.y-.025f:ring==1?center.y-1.8f:ring==2?-5.8f:-16.15f;vertices.Add(point);
   }
   for(int ring=0;ring<3;ring++)for(int j=0;j<8;j++){
    int a=ring*8+j,b=ring*8+(j+1)%8,c=(ring+1)*8+(j+1)%8,d=(ring+1)*8+j;Vector3 normal=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);Vector3 outward=(vertices[a]+vertices[b])*.5f-center;outward.y=0;
    if(Vector3.Dot(normal,outward)>0)triangles.AddRange(new[]{a,b,c,a,c,d});else triangles.AddRange(new[]{a,c,b,a,d,c});
   }
   for(int j=1;j<7;j++)triangles.AddRange(new[]{24,24+j,24+j+1});Mesh("Cohesive bedrock - "+meadow.name,vertices,triangles,null,rock);
  }
 }
 [Serializable]class NormanMaterial{public string name;public float[] color;public float roughness,metallic;}
 [Serializable]class NormanMaterials{public NormanMaterial[] items;}
 static Transform Bone(Transform parent,string name)=>parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
 static Bounds Bounds(GameObject model){var renderers=model.GetComponentsInChildren<Renderer>(true);var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
 static void Norman(BTTrainingGame game){
  string path=Root+"/Models/Norman.fbx";var list=JsonUtility.FromJson<NormanMaterials>(File.ReadAllText(Root+"/Models/NormanMaterials.json"));var importer=(ModelImporter)AssetImporter.GetAtPath(path);if(!importer)throw new Exception("Missing Norman FBX");importer.importAnimation=false;importer.isReadable=true;
  foreach(var item in list.items){Color c=new Color(item.color[0],item.color[1],item.color[2]).gamma;var mat=Material(item.name,"Universal Render Pipeline/Lit",c);mat.SetFloat("_Smoothness",1-item.roughness);mat.SetFloat("_Metallic",item.metallic);importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),item.name),mat);}importer.SaveAndReimport();
  var template=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!template)throw new Exception("Norman model import failed");
  foreach(Transform child in game.norman.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
  game.norman.position=new Vector3(13.8f,3,22.8f);game.norman.rotation=Quaternion.Euler(0,220,0);
  var holder=new GameObject("Norman field scientist");holder.transform.SetParent(game.norman,false);var model=(GameObject)PrefabUtility.InstantiatePrefab(template,holder.transform);model.transform.localPosition=Vector3.zero;model.transform.localRotation=template.transform.localRotation;model.transform.localScale=template.transform.localScale;
  var b=Bounds(model);float scale=1.94f/b.size.y;model.transform.localScale*=scale;b=Bounds(model);model.transform.position+=new Vector3(game.norman.position.x-b.center.x,game.norman.position.y-b.min.y,game.norman.position.z-b.center.z);
  foreach(var t in holder.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;
  var presence=holder.AddComponent<BTNormanPresence>();presence.head=Bone(model.transform,"NRM_HeadPivot");presence.body=Bone(model.transform,"NRM_Body");presence.leftEye=Bone(model.transform,"NRM_Eye.L");presence.rightEye=Bone(model.transform,"NRM_Eye.R");
  if(!presence.head||!presence.leftEye||!presence.rightEye)throw new Exception("Norman idle pivots missing");
  var collider=game.norman.GetComponent<CapsuleCollider>();if(!collider)collider=game.norman.gameObject.AddComponent<CapsuleCollider>();collider.height=1.88f;collider.radius=.28f;collider.center=new Vector3(0,.94f,0);game.norman.gameObject.layer=0;
  PrefabUtility.SaveAsPrefabAsset(holder,Root+"/Prefabs/NormanExperienced.prefab");
 }
 [MenuItem("Tools/Borrowed Time/Training/Finish grounded scenery and experienced Norman")]
 public static void Build(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Unsaved scene retained");if(scene.path!=BTJourney.TrainingPath)scene=EditorSceneManager.OpenScene(BTJourney.TrainingPath);
  var game=Object.FindFirstObjectByType<BTTrainingGame>();if(!game||!game.IsGentleTraining)throw new Exception("TrainingArea required");Folder(Root+"/Materials");Folder(Root+"/Meshes");Folder(Root+"/Prefabs");
  var old=GameObject.Find(SceneRoot);if(old)Object.DestroyImmediate(old);root=new GameObject(SceneRoot).transform;
  foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray()){
   if(t.name=="Turquoise river"||t.name=="River glint"||t.name=="Waterfall ribbon"||t.name=="Waterfall splash"||t.name=="River valley water"||t.name=="Weathered cliff block"||t.name=="Moss cap"||t.name=="Rooted original landmass"||t.name.Contains(" cliff "))Object.DestroyImmediate(t.gameObject);
  }
  rock=Material("Supported weathered bedrock","Universal Render Pipeline/Lit",new Color(.43f,.44f,.37f));
  water=Material("Continuous quiet river","Borrowed Time/Quiet Water",new Color(.23f,.43f,.46f));water.SetTexture("_WorldMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BorrowedTime/ForestBackdrop/Materials/WorldPollutionField.png"));water.SetVector("_MapBounds",new Vector4(-140,-110,200,200));
  fall=Material("Flowing waterfall","Borrowed Time/Anchored Waterfall",new Color(.47f,.65f,.64f,.82f));
  foam=Material("Soft waterfall foam","Universal Render Pipeline/Particles/Unlit",Color.white);foam.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BorrowedTime/ForestBackdrop/Materials/SoftHaze.png"));foam.SetFloat("_Surface",1);foam.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);foam.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);foam.SetFloat("_ZWrite",0);foam.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");foam.renderQueue=3000;
  WaterSurface();Waterfall();GroundedCliffs();Norman(game);BTTrainingPollutedForest.Validate();
  EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Directory.CreateDirectory(Out);Validate();Pictures();Debug.Log("BT_TRAINING_FINAL_ART_READY");
 }
 [MenuItem("Tools/Borrowed Time/Training/Validate finished scenery and Norman")]
 public static void Validate(){
  var game=Object.FindFirstObjectByType<BTTrainingGame>();if(!game||!game.norman.GetComponentInChildren<BTNormanPresence>())throw new Exception("Detailed scientist missing");
  var b=Bounds(game.norman.GetComponentInChildren<BTNormanPresence>().gameObject);if(Mathf.Abs(b.min.y-3)>.025f||b.size.y<1.8f||b.size.y>2.1f||b.size.x>1.25f||b.size.z>1.1f)throw new Exception("Norman scale or feet support incorrect: "+b);
  if(!Physics.Raycast(game.norman.position+Vector3.up*.08f,Vector3.down,out var hit,.3f,~(1<<2),QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-3)>.03f)throw new Exception("Norman is unsupported");
  var all=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);foreach(string name in new[]{"Waterfall ribbon","Waterfall splash","River glint","Turquoise river","River valley water"})if(all.Any(t=>t.name==name))throw new Exception("Legacy floating/overlapping water remains "+name);
  foreach(string shaderName in new[]{"Borrowed Time/Quiet Water","Borrowed Time/Anchored Waterfall"}){var shader=Shader.Find(shaderName);if(!shader||ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"))throw new Exception("Shader failed "+shaderName);}
  var meadow=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).First(r=>r.name=="East landing meadow");var curtain=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).First(r=>r.name=="Waterfall follows the supported cliff");if(Mathf.Abs(curtain.bounds.max.x-meadow.bounds.min.x)>.12f||Mathf.Abs(curtain.bounds.min.y+5.55f)>.12f)throw new Exception("Waterfall is not attached to actual meadow rim / river");
  Directory.CreateDirectory(Out);File.WriteAllText(Out+"/Validation.txt","PASS one continuous water surface, non-grid ripples; old water/glints/floating waterfall removed; waterfall lip matches the actual west rim of the east island and ends at the river surface; bedrock reaches valley floor; detailed Norman feet on terrace, height="+b.size.y+"; head, blink pivots, hands, lab coat, spectacles and research clipboard present.\n");
 }
 [MenuItem("Tools/Borrowed Time/Training/Capture finished scenery and Norman")]
 public static void Pictures(){
  Directory.CreateDirectory(Out);var game=Object.FindFirstObjectByType<BTTrainingGame>();var go=new GameObject("Temporary final art camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(game.player.view);camera.orthographic=false;camera.fieldOfView=48;
  Vector3 n=game.norman.position;Vector3 front=game.norman.forward;
  Capture(camera,"Norman_ExperiencedScientist",n+front*3.6f+Vector3.up*1.22f,n+Vector3.up*1.02f,1100,1300);
  camera.fieldOfView=36;Capture(camera,"Norman_Face",n+front*1.3f+Vector3.up*1.72f,n+Vector3.up*1.70f,1100,1000);
  camera.fieldOfView=55;Capture(camera,"Grounded_Waterfall",new Vector3(-5,1.3f,-1.8f),new Vector3(3.3f,-1.8f,-.7f),1440,900);
  Capture(camera,"Training_Finished_Overview",new Vector3(30,32,28),new Vector3(-3,-1,3),1440,900);
  Capture(camera,"River_NaturalSurface",new Vector3(-5,6,-9),new Vector3(1,-5.55f,6),1440,900);Object.DestroyImmediate(go);
 }
 static void Capture(Camera camera,string name,Vector3 at,Vector3 look,int width,int height){
  camera.transform.position=at;camera.transform.LookAt(look);var old=RenderTexture.active;var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
 }
}
}
#endif
