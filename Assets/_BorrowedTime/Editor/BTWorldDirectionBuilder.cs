#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using BorrowedTime.Characters;
using BorrowedTime.Characters.Editor;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTWorldDirectionBuilder {
 const string Root="Assets/_BorrowedTime/JourneyDirection";
 const string Out="Captures/TrainingComfort/FactoryRoutes";
 static int materialNumber;
 static Transform props;static Material iron,gold,handle;
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;int i=p.LastIndexOf('/');Folder(p.Substring(0,i));AssetDatabase.CreateFolder(p.Substring(0,i),p.Substring(i+1));}
 static GameObject Box(string name,Vector3 p,Vector3 scale,Material m,Transform parent=null,PrimitiveType type=PrimitiveType.Cube,bool solid=false){var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent?parent:props,true);go.transform.position=p;go.transform.localScale=scale;go.layer=solid?0:2;if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=m;return go;}
 static void Beam(string name,Vector3 a,Vector3 b,float r,Material m,Transform parent=null){var go=Box(name,(a+b)*.5f,new Vector3(r*2,Vector3.Distance(a,b)*.5f,r*2),m,parent,PrimitiveType.Cylinder);go.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);}
 static BTFactoryManualControl Manual(BTFactoryGame g,int id,Vector3 p,string title,string help){
  var t=new GameObject("Mechanical backup / "+title).transform;t.SetParent(props,true);t.position=p;
  Box("Floor bolted manual pedestal",p+Vector3.up*.5f,new Vector3(.75f,1,.65f),iron,t,PrimitiveType.Cube,true);
  var wheel=new GameObject("Manual crank wheel").transform;wheel.SetParent(t,false);wheel.localPosition=new Vector3(0,1.05f,-.39f);wheel.localRotation=Quaternion.Euler(90,0,0);
  for(int i=0;i<12;i++){float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;var a3=wheel.TransformPoint(new Vector3(Mathf.Sin(a)*.36f,0,Mathf.Cos(a)*.36f));var b3=wheel.TransformPoint(new Vector3(Mathf.Sin(b)*.36f,0,Mathf.Cos(b)*.36f));Beam("Handwheel rim",a3,b3,.04f,gold,wheel);}
  for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Beam("Handwheel spoke",wheel.position,wheel.TransformPoint(new Vector3(Mathf.Sin(a)*.33f,0,Mathf.Cos(a)*.33f)),.025f,handle,wheel);}
  Box("Mounted instruction plate",p+new Vector3(0,1.83f,.05f),new Vector3(1.7f,.75f,.08f),iron,t);
  Beam("Instruction plate support",p+new Vector3(0,1,.10f),p+new Vector3(0,1.48f,.10f),.045f,iron,t);
  var text=new GameObject("Backup operation label").AddComponent<TextMesh>();text.transform.SetParent(t,true);text.transform.position=p+new Vector3(0,1.85f,-.01f);text.text="F  "+title+"\nไม่เสียพลังถุงมือ";text.font=g.uiFont;text.fontSize=64;text.characterSize=.026f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.85f,.8f,.6f);text.GetComponent<Renderer>().sharedMaterial=g.uiFont.material;text.gameObject.layer=2;
  return new BTFactoryManualControl{puzzle=id,point=t,wheel=wheel,title=title,help=help};
 }
 static void Factory(){
  var g=Object.FindFirstObjectByType<BTFactoryGame>();var previous=GameObject.Find("Factory / Simple alternative puzzle routes");if(previous)throw new Exception("Already installed; preventing duplicate puzzle controls");
  var wing=GameObject.Find("Borrowed Time / Living World expansion");if(wing)Object.DestroyImmediate(wing);
  var world=g.GetComponent<BTExpansionWorld>();if(world)Object.DestroyImmediate(world);
  var wall=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Factory south cutaway wall");if(wall)wall.gameObject.SetActive(true);
  var facility=GameObject.Find("Factory / Life extraction facility").transform;var e=g.GetComponent<BTFactoryLifeEconomy>();
  foreach(var t in facility.Cast<Transform>().ToArray()){
   var renderers=t.GetComponentsInChildren<Renderer>(true);if(t.position.z< -29.4f||renderers.Length>0&&renderers.All(r=>r.bounds.max.z< -30))Object.DestroyImmediate(t.gameObject);
  }
  e.specimens=e.specimens.Where(t=>t).ToArray();e.extractionIndicators=e.extractionIndicators.Where(t=>t).ToArray();e.extractionLights=e.extractionLights.Where(t=>t).ToArray();
  props=new GameObject("Factory / Simple alternative puzzle routes").transform;
  iron=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BorrowedTime/FactoryArt/Materials/Extraction black iron.mat");gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BorrowedTime/Materials/HazardGold.mat");handle=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BorrowedTime/FactoryArt/Materials/Worn cast steel.mat");
  var r=g.gameObject.AddComponent<BTFactoryRoutes>();r.game=g;r.controls=new[]{
   Manual(g,0,new Vector3(5.3f,0,-22),"เปิดประตูด้วยมือ","ทางเลือก: คืนสภาพแผงควบคุมด้วยล้อเมาส์ถอย"),
   Manual(g,1,new Vector3(-12,0,-11.5f),"กว้านทางขนส่ง","ทางเลือก: คืนสภาพสะพานหนึ่งขั้น"),
   Manual(g,2,new Vector3(-15,0,6),"ต่อมอเตอร์ลิฟต์สำรอง","จากนั้นยืนบนแท่นแล้วกด F • หรือคืนสภาพแผงลิฟต์"),
   Manual(g,3,new Vector3(22,6,-6),"ปลดเบรกสายพาน","ทางเลือก: เร่งระบบสายพานสองขั้นแล้วกดแผงปล่อย"),
   Manual(g,4,new Vector3(11,6,21.3f),"กว้านสะพานวงแหวน","ทางเลือก: คืนสภาพทางเดินหนึ่งขั้น"),
   Manual(g,6,new Vector3(-5,12,17.5f),"ต่อท่อน้ำโรงเพาะกล้า","ทางเลือก: คืนสภาพโรงเพาะกล้าหนึ่งขั้น"),
   Manual(g,7,new Vector3(4,12,10.8f),"เปิดน้ำและแสงปลูกเมล็ด","ไม่เสียพลัง • หรือปลูกที่ดินด้วย F แล้วเร่งสองขั้น")};
  // Ground and connect the winches to the actual bridge anchors.
  Beam("Transport winch cable",new Vector3(-12,1,-11.5f),new Vector3(-14.8f,.1f,-7.3f),.025f,handle);
  Beam("Ring winch cable",new Vector3(11,7,21.3f),new Vector3(8,6.1f,23.8f),.025f,handle);
  Beam("Nursery clean-water hose",new Vector3(-5,12.2f,17.5f),new Vector3(-5,12.3f,14),.06f,handle);
  Beam("Seed tray irrigation supply",new Vector3(4,12.2f,10.8f),new Vector3(1.4f,12.2f,12),.06f,handle);
  g.boilerPanel=null;g.powerPanel=null;
  foreach(var t in g.boilers.Concat(g.entryRelays).Concat(new[]{g.powerCoupler})){t.enabled=false;if(t.aimPoint)t.aimPoint.gameObject.SetActive(false);if(t.focusRing)t.focusRing.SetActive(false);}
  foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)){
   if(text.text.Contains("F / ระบายแรงดัน"))text.text="ระบบระบายแรงดันอัตโนมัติ";
   if(text.text.Contains("F / เปิดสะพานวงแหวน"))text.text="สะพาน • ถุงมือ หรือ กว้านสำรอง";
   if(text.text.Contains("SPECIMEN PROCESSING"))text.text="โรงงานสกัดชีวิต\nใช้พลังเวลา หรือ อุปกรณ์สำรอง\nF ใช้งาน • H คำแนะนำ";
  }
  EditorUtility.SetDirty(g);EditorUtility.SetDirty(e);EditorUtility.SetDirty(r);
 }
 static Color Desaturate(Color c,float saturation=.38f,float value=.86f){Color.RGBToHSV(c,out float h,out float s,out float v);var n=Color.HSVToRGB(h,s*saturation,v*value);n.a=c.a;return n;}
 static void DyingLook(BTTrainingGame g){
  var remap=new Dictionary<Material,Material>();string name=g is BTCityGame?"City":"Training";
  foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(renderer.gameObject.scene!=g.gameObject.scene||renderer.GetComponentInParent<BTCharacterModel>()||renderer.GetComponent<TextMesh>()||renderer.GetComponent<LineRenderer>()||renderer.GetComponentInParent<BTNormanPresence>())continue;
   var originals=renderer.sharedMaterials;bool changed=false;for(int i=0;i<originals.Length;i++){
    var m=originals[i];if(!m||m.IsKeywordEnabled("_EMISSION")||m.name.Contains("Glow")||m.name.Contains("Signal"))continue;
    if(!remap.ContainsKey(m)){string path=Root+"/Materials/"+name+"_"+(materialNumber++).ToString("000")+".mat";var copy=new Material(m){name=name+" / dying world / "+m.name};foreach(string prop in new[]{"_BaseColor","_Healthy","_Stressed","_Damaged","_Blight"})if(copy.HasProperty(prop))copy.SetColor(prop,Desaturate(copy.GetColor(prop)));AssetDatabase.CreateAsset(copy,path);remap[m]=copy;}
    originals[i]=remap[m];changed=true;
   }if(changed)renderer.sharedMaterials=originals;
  }
  foreach(var t in Object.FindObjectsByType<BTTimeTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.gameObject.scene==g.gameObject.scene)){if(t.healthyMaterial&&remap.ContainsKey(t.healthyMaterial))t.healthyMaterial=remap[t.healthyMaterial];if(t.wiltMaterial&&remap.ContainsKey(t.wiltMaterial))t.wiltMaterial=remap[t.wiltMaterial];EditorUtility.SetDirty(t);}
  RenderSettings.skybox=null;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.37f,.41f,.43f);RenderSettings.fogStartDistance=38;RenderSettings.fogEndDistance=125;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.52f,.56f,.60f);RenderSettings.ambientEquatorColor=new Color(.38f,.41f,.43f);RenderSettings.ambientGroundColor=new Color(.22f,.23f,.24f);if(RenderSettings.sun){RenderSettings.sun.color=new Color(.75f,.81f,.86f);RenderSettings.sun.intensity=.74f;}
  g.player.view.clearFlags=CameraClearFlags.SolidColor;g.player.view.backgroundColor=RenderSettings.fogColor;
  foreach(var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Where(v=>v.gameObject.scene==g.gameObject.scene))if(volume.sharedProfile){var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/"+name+"Atmosphere.asset");foreach(var c in volume.sharedProfile.components){var copy=Object.Instantiate(c);profile.components.Add(copy);AssetDatabase.AddObjectToAsset(copy,profile);}volume.sharedProfile=profile;if(profile.TryGet<ColorAdjustments>(out var ca)){ca.saturation.Override(-12);ca.contrast.Override(8);ca.postExposure.Override(0);}EditorUtility.SetDirty(profile);}
 }
 static void CharacterRepair(Scene scene){
  var player=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BTPlayer>(true)).Single();var g=player.game;var adapter=player.GetComponent<BTGameplayCharacter>();
  if(g is BTCityGame&&adapter){var legacy=adapter.legacyVisual;if(!legacy)legacy=new GameObject("Legacy player reference").transform;if(adapter.hero)Object.DestroyImmediate(adapter.hero.transform.parent.gameObject);if(adapter.robo)Object.DestroyImmediate(adapter.robo.gameObject);player.visual=legacy;Object.DestroyImmediate(adapter);adapter=null;}
  if(!g.robo)g.robo=new GameObject("ROBO").transform;g.robo.gameObject.SetActive(true);
  if(!adapter)adapter=BTGameplayCharacterInstaller.Install(scene);
  foreach(var model in new[]{adapter.hero,adapter.robo}){
   for(var t=model.transform;t;t=t.parent){t.gameObject.SetActive(true);if(t==player.transform||t==g.robo)break;}
   model.animator.enabled=true;model.animator.speed=1;model.animator.applyRootMotion=false;model.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;model.autoReturn=false;
   foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){renderer.enabled=true;if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;}
   PrefabUtility.RecordPrefabInstancePropertyModifications(model);PrefabUtility.RecordPrefabInstancePropertyModifications(model.animator);
  }
  g.robo.position=player.transform.position+new Vector3(1.25f,1.95f,-.35f);var companion=g.GetComponent<BTCompanionPresence>();if(!companion)companion=g.gameObject.AddComponent<BTCompanionPresence>();companion.game=g;
  var hud=g.GetComponent<BTJourneyHUD>();if(!hud)hud=g.gameObject.AddComponent<BTJourneyHUD>();hud.game=g;EditorUtility.SetDirty(g);EditorUtility.SetDirty(player);EditorUtility.SetDirty(adapter);
 }
 static GameObject Vista(Scene scene,string name){
  var result=new GameObject(name);var grouped=new Dictionary<Material,List<CombineInstance>>();
  foreach(var mf in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>())){
   var renderer=mf.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||!mf.sharedMesh||!mf.gameObject.activeInHierarchy||mf.GetComponent<TextMesh>()||mf.GetComponentInParent<BTCharacterModel>()||mf.name.Contains("focus",StringComparison.OrdinalIgnoreCase)||mf.name.Contains("crystal",StringComparison.OrdinalIgnoreCase))continue;
   var mesh=mf.sharedMesh;for(int sub=0;sub<mesh.subMeshCount;sub++){var mats=renderer.sharedMaterials;if(mats.Length==0)continue;var m=mats[Mathf.Min(sub,mats.Length-1)];if(!m)continue;if(!grouped.ContainsKey(m))grouped[m]=new List<CombineInstance>();grouped[m].Add(new CombineInstance{mesh=mesh,subMeshIndex=sub,transform=mf.transform.localToWorldMatrix});}
  }
  int number=0;foreach(var pair in grouped){var mesh=new Mesh{name=name+" batch "+number,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+name+"_"+number+".asset");var go=new GameObject(name+" / "+pair.Key.name);go.layer=2;go.transform.SetParent(result.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var mat=new Material(pair.Key){name=name+" / "+pair.Key.name};if(mat.HasProperty("_WorldMap"))mat.SetTexture("_WorldMap",Texture2D.blackTexture);AssetDatabase.CreateAsset(mat,Root+"/Materials/"+name+"_"+number+".mat");go.AddComponent<MeshRenderer>().sharedMaterial=mat;number++;}
  return result;
 }
 static void Cinema(GameObject training,GameObject city,BTFactoryGame g){
  SceneManager.MoveGameObjectToScene(training,g.gameObject.scene);SceneManager.MoveGameObjectToScene(city,g.gameObject.scene);training.transform.position=new Vector3(400,0,0);city.transform.position=new Vector3(800,0,0);
  var c=g.gameObject.AddComponent<BTWorldEndingCinematic>();c.game=g;c.trainingVista=training;c.cityVista=city;var recovery=new List<BTRecoveryMaterial>();
  foreach(var r in training.GetComponentsInChildren<MeshRenderer>().Concat(city.GetComponentsInChildren<MeshRenderer>())){
   var m=r.sharedMaterial;string name=m.name.ToLowerInvariant();bool water=name.Contains("water")||name.Contains("pollut")||name.Contains("river");bool plant=name.Contains("leaf")||name.Contains("foliage")||name.Contains("meadow")||name.Contains("grass")||m.HasProperty("_Healthy");
   if(water&&m.HasProperty("_BaseColor"))recovery.Add(new BTRecoveryMaterial{material=m,from=m.GetColor("_BaseColor"),to=new Color(.24f,.46f,.47f)});
   else if(plant)foreach(string prop in new[]{"_Healthy","_BaseColor"})if(m.HasProperty(prop)){var from=m.GetColor(prop);var to=Color.Lerp(from,new Color(.35f,.53f,.27f),.50f);recovery.Add(new BTRecoveryMaterial{material=m,property=prop,from=from,to=to});}
  }
  var shoots=new List<Transform>();iron=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BorrowedTime/Materials/Bark.mat");gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BorrowedTime/Materials/LeafLight.mat");
  foreach(var home in new[]{training,city})for(int i=0;i<6;i++){var t=new GameObject("A small sign of life").transform;t.SetParent(home.transform,false);t.localPosition=home==city?new Vector3(-20+i*.7f,0,-3.6f):new Vector3(6+i*.45f,3,23);Vector3 p=t.position;Beam("New shoot stem",p,p+Vector3.up*.65f,.025f,iron,t);Box("New tender leaf",p+new Vector3(.13f,.55f,0),new Vector3(.36f,.08f,.18f),gold,t,PrimitiveType.Sphere);Box("New tender leaf",p+new Vector3(-.12f,.4f,.02f),new Vector3(.32f,.07f,.16f),gold,t,PrimitiveType.Sphere);t.localScale=Vector3.zero;shoots.Add(t);}
  c.newShoots=shoots.ToArray();c.recovery=recovery.ToArray();training.SetActive(false);city.SetActive(false);EditorUtility.SetDirty(c);
 }
 [MenuItem("Tools/Borrowed Time/Journey/Build simple factory routes and dying world")]
 public static void Build(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode before editing");if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene retained");Folder(Root+"/Materials");Folder(Root+"/Meshes");Directory.CreateDirectory(Out);materialNumber=0;
  var factory=EditorSceneManager.OpenScene(BTJourney.FactoryPath);Factory();CharacterRepair(factory);EditorSceneManager.MarkSceneDirty(factory);EditorSceneManager.SaveScene(factory);
  var training=EditorSceneManager.OpenScene(BTJourney.TrainingPath);CharacterRepair(training);DyingLook(Object.FindFirstObjectByType<BTTrainingGame>());EditorSceneManager.MarkSceneDirty(training);EditorSceneManager.SaveScene(training);var t=Vista(training,"TrainingRecoveryVista");
  // Keep the static meshes across editor scene loads without running game scripts.
  var holding=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.MoveGameObjectToScene(t,holding);EditorSceneManager.CloseScene(training,true);
  var city=EditorSceneManager.OpenScene(BTJourney.CityPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(city);CharacterRepair(city);DyingLook(city.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BTCityGame>()).First());EditorSceneManager.MarkSceneDirty(city);EditorSceneManager.SaveScene(city);var c=Vista(city,"CityRecoveryVista");SceneManager.MoveGameObjectToScene(c,holding);
  EditorSceneManager.CloseScene(city,true);factory=EditorSceneManager.OpenScene(BTJourney.FactoryPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(factory);Cinema(t,c,factory.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BTFactoryGame>()).First());EditorSceneManager.CloseScene(holding,true);EditorSceneManager.MarkSceneDirty(factory);EditorSceneManager.SaveScene(factory);AssetDatabase.SaveAssets();
  File.WriteAllText(Out+"/Build.txt","PASS removed factory matching/sequence annex; 7 physical backup controls; common HUD in 3 maps; City hero and Robo reinstalled and animator enabled; desaturated dying world in Training and City; four-shot 20-second ending with new shoots and water recovery.\n");
 }
}}
#endif
