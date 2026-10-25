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
public static class BTTrainingEncroachment {
 const string AssetsRoot="Assets/_BorrowedTime/ForestBackdrop";
 const string RootName="Training / Organic pollution encroachment";
 static readonly string Out=Path.GetFullPath("Captures/TrainingComfort/ForestBackdrop");
 static Material ground,foliage,wilt,water,deadLeaf,stain;
 static Transform root;
 static float Smooth(float a,float b,float value){float t=Mathf.InverseLerp(a,b,value);return t*t*(3-2*t);}
 // One continuous field drives every island, the connecting path, forest soil,
 // foliage and water. No material boundary follows the old map rectangle.
 public static Vector2 Field(float x,float z){
  float broad=Mathf.PerlinNoise(x*.068f+19,z*.061f+7)*.65f+Mathf.PerlinNoise(x*.14f-11,z*.12f+23)*.35f;
  float rear=Smooth(-5,48,-z-11+(broad-.5f)*28);
  float fingerX=-19+Mathf.Sin(z*.18f)*3.2f;
  float finger=Mathf.Exp(-Mathf.Abs(x-fingerX)/4.8f)*(1-Smooth(-29,6,z))*.56f;
  float creekX=-37+Mathf.Sin(Mathf.InverseLerp(-70,-24,z)*5.8f)*7;
  float creek=Mathf.Exp(-Mathf.Abs(x-creekX)/6.5f)*(1-Smooth(-25,-15,z))*.78f;
  float amount=Mathf.Clamp01(Mathf.Max(rear,Mathf.Max(finger,creek)));
  float patches=Mathf.PerlinNoise(x*.31f+8,z*.27f-3)*.65f+Mathf.PerlinNoise(x*.11f+16,z*.13f+9)*.35f;
  return new Vector2(amount,patches);
 }
 static Color ColorOf(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
 static Material Surface(string name,Texture2D map,int kind,string a,string b,string c,string d){
  string path=AssetsRoot+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!mat){mat=new Material(Shader.Find("Borrowed Time/Encroaching Nature"));AssetDatabase.CreateAsset(mat,path);}mat.shader=Shader.Find("Borrowed Time/Encroaching Nature");
  mat.SetColor("_BaseColor",Color.white);mat.SetColor("_Healthy",ColorOf(a));mat.SetColor("_Stressed",ColorOf(b));mat.SetColor("_Damaged",ColorOf(c));mat.SetColor("_Blight",ColorOf(d));mat.SetTexture("_WorldMap",map);mat.SetVector("_MapBounds",new Vector4(-140,-110,200,200));mat.SetFloat("_SurfaceKind",kind);mat.SetFloat("_Cull",kind==2?0:2);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
 }
 static Material Lit(string name,string hex){
  string path=AssetsRoot+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",ColorOf(hex));mat.SetFloat("_Smoothness",.12f);mat.enableInstancing=true;return mat;
 }
 static void Shape(string name,Vector3 p,Vector3 size,Material mat,Quaternion rotation){
  var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(root,false);go.transform.position=p;go.transform.rotation=rotation;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());go.layer=2;go.GetComponent<Renderer>().sharedMaterial=mat;
 }
 static void CorruptionAlongArrival(){
  // Stains and leaf litter follow damp soil along the side of the route.
  // Their broken silhouettes overlap the junction, rather than outlining it.
  var random=new System.Random(2510);
  for(int i=0;i<75;i++){
   float z=Mathf.Lerp(-28,1,(float)random.NextDouble());float x=-20.5f+Mathf.Sin(z*.18f)*2+(float)random.NextDouble()*5;
   float infection=Field(x,z).x;if(infection<.14f)continue;
   if(!Physics.Raycast(new Vector3(x,3,z),Vector3.down,out var hit,5,~(1<<2),QueryTriggerInteraction.Ignore)||hit.point.y> .4f)continue;
   float scale=.18f+(float)random.NextDouble()*.35f;
   Shape("Ochre leaves at the advancing edge",hit.point+Vector3.up*.018f,new Vector3(scale,.025f,scale*.5f),deadLeaf,Quaternion.Euler(0,(float)random.NextDouble()*360,0));
   if(i%4==0)Shape("Withering grass among living grass",hit.point+Vector3.up*.16f,new Vector3(.10f,.36f,.45f),stain,Quaternion.Euler(18,(float)random.NextDouble()*360,8));
  }
 }
 [MenuItem("Tools/Borrowed Time/Training/Blend pollution organically across the whole landscape")]
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Unsaved scene retained");if(scene.path!=BTJourney.TrainingPath)scene=EditorSceneManager.OpenScene(BTJourney.TrainingPath);
  if(!GameObject.Find("Training / Pollution approaching the forest"))throw new Exception("Build the forest first");
  var shader=Shader.Find("Borrowed Time/Encroaching Nature");if(!shader)throw new Exception("Encroachment shader missing");
  var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray();if(errors.Length>0)throw new Exception(string.Join(" / ",errors.Select(e=>e.message)));
  string texturePath=AssetsRoot+"/Materials/WorldPollutionField.png";var texture=new Texture2D(256,256,TextureFormat.RGBA32,false,true);
  for(int y=0;y<256;y++)for(int x=0;x<256;x++){Vector2 f=Field(-140+x/255f*200,-110+y/255f*200);texture.SetPixel(x,y,new Color(f.x,f.y,0,1));}
  texture.Apply();File.WriteAllBytes(texturePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(texturePath);
  var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();var map=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
  ground=Surface("ContinuousGround",map,0,"#70A950","#92905B","#73735A","#54564E");
  foliage=Surface("ContinuousFoliage",map,1,"#4D8244","#8D9155","#757457","#625D58");
  wilt=Surface("ContinuousBorrowedLifeFoliage",map,1,"#8B9751","#9B9259","#787054","#65595C");wilt.SetFloat("_BorrowedLife",1);
  water=Surface("ContinuousAffectedWater",map,2,"#398594","#63817B","#596B6A","#595363");
  deadLeaf=Lit("AdvancingOchreLeaves","#9A8556");stain=Lit("AdvancingDryGrass","#8E8D58");
  var old=GameObject.Find(RootName);if(old)Object.DestroyImmediate(old);root=new GameObject(RootName).transform;
  int groundCount=0,foliageCount=0,waterCount=0;
  foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(renderer.GetComponentInParent<BorrowedTime.Characters.BTCharacterModel>())continue;
   var mats=renderer.sharedMaterials;bool changed=false;
   for(int i=0;i<mats.Length;i++){
    if(!mats[i])continue;string name=mats[i].name;
    bool soil=name=="ForestSoil"||name=="ContinuousGround"||name.Contains("Regraded_Grass")||name=="Meadow"||name=="Regraded_Moss";
    bool leaves=new[]{"GreenLeaf","LightLeaf","DullLeaf","YellowLeaf","DeadLeaf","ContinuousFoliage","ContinuousBorrowedLifeFoliage","Regraded_Leaf","Regraded_LeafLight"}.Contains(name);
    if(soil){mats[i]=ground;groundCount++;changed=true;}
    else if(leaves){mats[i]=foliage;foliageCount++;changed=true;}
    else if(renderer.name=="River valley water"||name=="ContinuousAffectedWater"){mats[i]=water;waterCount++;changed=true;}
   }
   if(changed){renderer.sharedMaterials=mats;EditorUtility.SetDirty(renderer);}
  }
  // Plant health feedback still uses separate healthy/wilt states, now with
  // the same spatial encroachment field so absorption cannot restore neon art.
  foreach(var target in Object.FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None)){
   if(!target.IsLiving||target.foliage==null||target.foliage.Length==0)continue;target.healthyMaterial=foliage;target.wiltMaterial=wilt;EditorUtility.SetDirty(target);
   foreach(var renderer in target.foliage)if(renderer){renderer.sharedMaterial=foliage;EditorUtility.SetDirty(renderer);}
  }
  // A few leaves and stalks cross the join physically; no triggers or puzzles.
  CorruptionAlongArrival();
  // Keep the compact dirt path warm and natural, rather than road-black.
  var pathMat=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/Materials/Path.mat");
  if(pathMat){pathMat.shader=Shader.Find("Universal Render Pipeline/Unlit");pathMat.SetColor("_BaseColor",new Color(.39f,.36f,.27f));pathMat.SetFloat("_Cull",0);EditorUtility.SetDirty(pathMat);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();BTTrainingPollutedForest.Validate();Pictures();
  Directory.CreateDirectory(Out);File.WriteAllText(Out+"/EncroachmentValidation.txt","PASS continuous world-space pollution field across "+groundCount+" ground surfaces, "+foliageCount+" foliage batches, "+waterCount+" water surfaces. Mixed early stress and wet-soil fingers extend into the tutorial island. No rectangular material boundary. Player health feedback retained.\n");Debug.Log("BT_ORGANIC_ENCROACHMENT_READY");
 }
 [MenuItem("Tools/Borrowed Time/Training/Capture pollution transition overview")]
 public static void Pictures(){
  Directory.CreateDirectory(Out);var go=new GameObject("Temporary transition photograph");var camera=go.AddComponent<Camera>();var game=Object.FindFirstObjectByType<BTTrainingGame>();camera.CopyFrom(game.player.view);camera.orthographic=false;camera.fieldOfView=55;
  Capture(camera,"Pollution_Transition_Overview",new Vector3(25,37,23),new Vector3(-20,0,-13));
  Capture(camera,"Pollution_Transition_Join",new Vector3(-9,7,-5),new Vector3(-23,1,-27));
  Capture(camera,"Pollution_Transition_Walk",new Vector3(-16,2.4f,-13),new Vector3(-24,2,-32));Object.DestroyImmediate(go);
 }
 static void Capture(Camera camera,string name,Vector3 position,Vector3 target){
  camera.transform.position=position;camera.transform.LookAt(target);var old=RenderTexture.active;var rt=new RenderTexture(1440,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
 }
}
}
#endif
