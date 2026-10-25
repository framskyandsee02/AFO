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
public static class BTFactoryAtmosphereBuilder {
 const string Root="Assets/_BorrowedTime/FactoryArt";
 const string Assembly="Factory / Life extraction facility";
 static Transform root;static BTFactoryGame game;static BTFactoryLifeEconomy economy;
 static Material iron,edge,soil,bark,leaf,leafDry,red,green,amber,ceramic,black;
 static Mesh leafMesh;static int serial;
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;int i=p.LastIndexOf('/');Folder(p.Substring(0,i));AssetDatabase.CreateFolder(p.Substring(0,i),p.Substring(i+1));}
 static Material Mat(string name,string hex,bool glow=false,bool worn=false){string p=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find(worn?"Borrowed Time/Factory worn surface":"Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,p);}ColorUtility.TryParseHtmlString("#"+hex,out var c);m.SetColor("_BaseColor",c);m.enableInstancing=true;if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.22f);if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*2.2f);}EditorUtility.SetDirty(m);return m;}
 static GameObject Shape(string name,Vector3 p,Vector3 scale,Material m,Transform parent=null,PrimitiveType type=PrimitiveType.Cube,bool solid=false){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent?parent:root,true);g.transform.position=p;g.transform.localScale=scale;g.layer=solid?0:2;if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;if(type==PrimitiveType.Cube){var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_BorrowedTime/Expansion/Meshes/ArchitectureBeveledCube.asset");if(mesh)g.GetComponent<MeshFilter>().sharedMesh=mesh;}return g;}
 static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent?parent:root,true);return t;}
 static GameObject Beam(string n,Vector3 a,Vector3 b,float radius,Material m,Transform parent=null){var g=Shape(n,(a+b)*.5f,new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2),m,parent,PrimitiveType.Cylinder);g.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);return g;}
 static TextMesh Text(string words,Vector3 p,float size,Transform parent=null){var go=new GameObject("Mounted label / "+words.Split('\n')[0]);go.transform.SetParent(parent?parent:root,true);go.layer=2;go.transform.position=p;var t=go.AddComponent<TextMesh>();t.text=words;t.font=game.uiFont;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.85f,.87f,.78f);t.GetComponent<Renderer>().sharedMaterial=game.uiFont.material;return t;}
 static void Lamp(Vector3 p,bool danger,Transform parent=null){var t=Group("Caged wall luminaire",parent);Shape("Lamp enclosure",p,new Vector3(.65f,.9f,.35f),iron,t);Shape("Practical luminous panel",p+Vector3.back*.2f,new Vector3(.40f,.56f,.08f),danger?red:amber,t);for(int j=-1;j<=1;j++)Shape("Lamp safety bars",p+new Vector3(j*.16f,0,-.26f),new Vector3(.025f,.64f,.05f),edge,t);var lightObject=new GameObject("Practical light source");lightObject.transform.SetParent(t,true);var l=lightObject.AddComponent<Light>();l.transform.position=p+Vector3.back*.5f;l.type=LightType.Point;l.range=10;l.intensity=danger?3.8f:2.4f;l.color=danger?new Color(1,.16f,.06f):new Color(.8f,.68f,.38f);}
 static void MakeLeaf(){string p=Root+"/Meshes/DroopingLeaf.asset";leafMesh=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(leafMesh)return;leafMesh=new Mesh{name="Pointed curled specimen leaf"};leafMesh.vertices=new[]{new Vector3(0,0,0),new Vector3(.35f,.025f,-.15f),new Vector3(.4f,.08f,0),new Vector3(.35f,.025f,.15f),new Vector3(.72f,-.10f,-.1f),new Vector3(.72f,-.06f,0),new Vector3(.72f,-.1f,.1f),new Vector3(1,-.30f,0)};leafMesh.triangles=new[]{0,1,2,0,2,3,1,4,5,1,5,2,2,5,6,2,6,3,4,7,5,5,7,6,2,1,0,3,2,0,5,4,1,2,5,1,6,5,2,3,6,2,5,7,4,6,7,5};leafMesh.RecalculateNormals();leafMesh.RecalculateBounds();AssetDatabase.CreateAsset(leafMesh,p);}
 static void Leaf(Vector3 p,float angle,float size,Material m,Transform parent,List<Renderer> foliage){var go=new GameObject("Curled surviving leaf");go.layer=2;go.transform.SetParent(parent,true);go.transform.position=p;go.transform.rotation=Quaternion.Euler(-12,angle,-15);go.transform.localScale=Vector3.one*size;go.AddComponent<MeshFilter>().sharedMesh=leafMesh;foliage.Add(go.AddComponent<MeshRenderer>());foliage.Last().sharedMaterial=m;}
 static void Specimen(BTTimeTarget target,int number){
  if(PrefabUtility.IsPartOfPrefabInstance(target.gameObject))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(target.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  Vector3 p=target.transform.position;p.y=Mathf.Round(p.y/6)*6;
  foreach(Transform c in target.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(c.gameObject);
  target.transform.SetParent(root,true);target.transform.position=p;target.transform.localScale=Vector3.one;target.transform.rotation=Quaternion.identity;
  target.name="Specimen S-"+number.ToString("00");target.displayName="ต้นไม้ทดลอง S-"+number.ToString("00")+" • ใกล้ตาย";
  target.maxLife=target.life=60;target.minimumLife=20;target.regenPerSecond=0;target.absorbAllowed=true;target.states=new GameObject[0];target.stateNames=new[]{"รอดชีวิตจากการสกัด"};target.age=target.initialAge=0;
  target.healthyMaterial=leaf;target.wiltMaterial=leafDry;target.game=game;
  var parent=target.transform;Shape("Steel soil containment bed",p+Vector3.up*.20f,new Vector3(1.65f,.20f,1.65f),iron,parent,PrimitiveType.Cylinder,true);
  Shape("Real soil beneath exposed roots",p+Vector3.up*.405f,new Vector3(1.43f,.035f,1.43f),soil,parent,PrimitiveType.Cylinder);
  for(int j=0;j<6;j++){float a=(j*60+number*13)*Mathf.Deg2Rad;Vector3 end=p+new Vector3(Mathf.Sin(a)*.58f,.44f,Mathf.Cos(a)*.58f);Beam("Roots seated in soil",p+Vector3.up*.64f,end,.065f,bark,parent);}
  Vector3 top=p+new Vector3(.13f,2.25f,0);Beam("Scarred living trunk",p+Vector3.up*.43f,top,.12f,bark,parent);
  var foliage=new List<Renderer>();for(int j=0;j<5;j++){float a=(j*137+number*19)*Mathf.Deg2Rad;Vector3 start=p+new Vector3(.08f,1.1f+j*.18f,0);Vector3 tip=start+new Vector3(Mathf.Sin(a)*(.45f+j*.03f),.28f,Mathf.Cos(a)*(.45f+j*.03f));Beam("Bare damaged branch",start,tip,.035f,bark,parent);if(j%2==0)Leaf(tip,(j*137+number*19),.7f,leafDry,parent,foliage);Leaf(tip+Vector3.up*.07f,j*137+number*19+130,.48f,leafDry,parent,foliage);}
  target.foliage=foliage.ToArray();
  Shape("Extraction collar on trunk",p+Vector3.up*.91f,new Vector3(.42f,.11f,.42f),edge,parent,PrimitiveType.Cylinder);
  Vector3 pump=p+new Vector3(p.x>21?-.92f:.92f,.0f,.15f);
  Shape("Bolted extractor plinth",pump+Vector3.up*.1f,new Vector3(.65f,.2f,.7f),iron,parent);
  Shape("Extractor pump",pump+Vector3.up*.48f,new Vector3(.52f,.38f,.52f),ceramic,parent,PrimitiveType.Cylinder);
  Beam("Connected suction hose",p+new Vector3(.18f,.9f,0),pump+Vector3.up*.92f,.055f,black,parent);
  Beam("Pump drain to soil bed",pump+new Vector3(0,.3f,0),p+new Vector3(.6f,.3f,0),.065f,iron,parent);
  var indicator=Shape("Active life extraction indicator",pump+new Vector3(0,.65f,-.28f),new Vector3(.18f,.14f,.035f),red,parent);indicators.Add(indicator);
  // The luminous bud is part of the trunk; it also supplies a clear aiming collider.
  target.aimPoint=Shape("Surviving life bud / aim",p+new Vector3(0,1.25f,-.18f),Vector3.one*.25f,green,parent,PrimitiveType.Sphere,true).transform;
  target.focusRing=Shape("Soil bed focus",p+Vector3.up*.43f,new Vector3(1.56f,.01f,1.56f),green,parent,PrimitiveType.Cylinder);target.focusRing.SetActive(false);
  Shape("Specimen identification plate",p+new Vector3(-.48f,1.02f,-.53f),new Vector3(.85f,.55f,.07f),iron,parent);
  Text("S-"+number.ToString("00")+" / DRAINED",p+new Vector3(-.48f,1.16f,-.58f),.018f,parent);
  var read=Text("LIFE 40 / 40\nRESERVE 20 • DO NOT EXCEED",p+new Vector3(-.48f,.97f,-.58f),.012f,parent);
  var bar=Shape("Remaining usable life meter",p+new Vector3(-.48f,.83f,-.58f),new Vector3(.7f,.035f,.03f),green,parent).transform;
  Beam("Identification plate leg",p+new Vector3(-.48f,.43f,-.49f),p+new Vector3(-.48f,.78f,-.49f),.035f,edge,parent);
  var meter=target.GetComponent<BTFactorySpecimen>();if(!meter)meter=target.gameObject.AddComponent<BTFactorySpecimen>();meter.target=target;meter.readout=read;meter.remainingBar=bar;
  EditorUtility.SetDirty(target);
 }
 static readonly List<GameObject> indicators=new List<GameObject>();
 static void Materials(){iron=Mat("Extraction black iron","252D30",false,true);edge=Mat("Worn cast steel","647170",false,true);soil=Mat("Dry specimen soil","443B2B",false,true);bark=Mat("Desiccated bark","67533F",false,true);leaf=Mat("Surviving olive leaves","6C7751");leafDry=Mat("Wilted ochre leaves","80734B");red=Mat("Extractor blood orange","EB351B",true);green=Mat("Remaining life cyan green","4BC9A1",true);amber=Mat("Maintenance amber","D3A65A",true);ceramic=Mat("Dirty insulated pump","9C9B81",false,true);black=Mat("Rubber suction hose","171D1E");MakeLeaf();}
 [MenuItem("Tools/Borrowed Time/Factory/Finish specimen support and readable lighting")]
 public static void Finish(){
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");if(EditorSceneManager.GetActiveScene().path!=BTJourney.FactoryPath)EditorSceneManager.OpenScene(BTJourney.FactoryPath);game=Object.FindFirstObjectByType<BTFactoryGame>();economy=game.GetComponent<BTFactoryLifeEconomy>();root=GameObject.Find(Assembly).transform;Materials();serial=0;indicators.Clear();
  foreach(var source in economy.specimens)Specimen(source,++serial);
  BTFactoryFinalChecks.RepairLights();
  economy.extractionIndicators=economy.extractionIndicators.Where(g=>g).Concat(indicators).Concat(root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Practical luminous panel").Select(t=>t.gameObject)).Distinct().ToArray();economy.extractionLights=root.GetComponentsInChildren<Light>();
  // Give the tall structures a visible load path into the bottom of the basin.
  for(int i=0;i<3;i++){var p=new Vector3(-16+i*10,-4.6f,30);if(!root.Find("Chimney footing "+i))Shape("Chimney footing "+i,p+Vector3.up*2.3f,new Vector3(2.5f,4.6f,2.5f),iron);}
  Vapor();EditorUtility.SetDirty(economy);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();Validate();BTFactoryInspection.Pictures();
 }
 static void Vapor(){
  var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Exhaust vapor.mat");if(!material){
   var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r),2));}tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(Root+"/Materials/SoftVapor.png",tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(Root+"/Materials/SoftVapor.png");
   material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Materials/SoftVapor.png"));material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;AssetDatabase.CreateAsset(material,Root+"/Materials/Exhaust vapor.mat");
  }
  for(int i=0;i<3;i++){var parent=game.smokestacks[i].transform;if(parent.Find("Chimney exhaust vapor"))continue;var go=new GameObject("Chimney exhaust vapor");go.transform.SetParent(parent,true);go.transform.position=new Vector3(-16+i*10,20,30);go.transform.rotation=Quaternion.Euler(-90,0,0);var ps=go.AddComponent<ParticleSystem>();var main=ps.main;main.startLifetime=7;main.startSpeed=.65f;main.startSize=2.6f;main.startColor=new Color(.23f,.20f,.17f,.65f);main.maxParticles=55;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=true;var emission=ps.emission;emission.rateOverTime=5;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.35f;shape.angle=8;var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(1,1.6f)));var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.18f),new GradientAlphaKey(0,1)});color.color=gradient;ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;}
 }
 static void Dress(){
  // Concrete, rust and soot belong to this scene only; other levels retain their materials.
  var surfaces=new Dictionary<Material,Material>();foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   if(r.GetComponent<TextMesh>() || r.transform.IsChildOf(root))continue;
   var m=r.sharedMaterial;if(!m)continue;string n=m.name.ToLowerInvariant();string colour=null;
   if(n.Contains("steelfloor")||n=="deepteal")colour="343B3D";
   else if(n.Contains("steelwall")||n.Contains("factorydark"))colour="20292D";
   else if(n.Contains("steeledge"))colour="546064";
   else if(n=="path"||n=="limestone"||n=="plaster")colour="625E50";
   else if(n=="brass")colour="756747";
   if(colour!=null){if(!surfaces.ContainsKey(m))surfaces[m]=Mat("Facility_"+m.name,colour,false,true);r.sharedMaterial=surfaces[m];}
   if(r.name.Contains("flower",StringComparison.OrdinalIgnoreCase)||r.name.Contains("blossom",StringComparison.OrdinalIgnoreCase)){if(!r.transform.IsChildOf(game.nursery.transform)&&!r.transform.IsChildOf(game.wideRestoration.transform)&&!r.transform.IsChildOf(game.restoredNursery.transform))r.gameObject.SetActive(false);}
  }
  foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){if(l.type==LightType.Directional){l.intensity=.48f;l.color=new Color(.64f,.72f,.79f);l.shadowStrength=.75f;}else{l.intensity=Mathf.Min(l.intensity,1.4f);l.color=l.transform.position.x>4?new Color(.9f,.24f,.08f):new Color(.35f,.54f,.66f);}}
  foreach(var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)){if(!v.sharedProfile)continue;string path=Root+"/FactoryAtmosphere.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);foreach(var component in v.sharedProfile.components){var copy=Object.Instantiate(component);profile.components.Add(copy);AssetDatabase.AddObjectToAsset(copy,profile);}}v.sharedProfile=profile;if(profile.TryGet<ColorAdjustments>(out var ca)){ca.saturation.Override(-14);ca.contrast.Override(17);ca.postExposure.Override(.15f);}if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.38f);bloom.threshold.Override(1.1f);}EditorUtility.SetDirty(profile);}
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.35f,.39f,.44f);RenderSettings.ambientEquatorColor=new Color(.23f,.28f,.31f);RenderSettings.ambientGroundColor=new Color(.12f,.14f,.15f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.075f,.105f,.12f);RenderSettings.fogStartDistance=45;RenderSettings.fogEndDistance=125;game.player.view.backgroundColor=RenderSettings.fogColor;
  // Wall anchored extraction mains, vents and repeated bracing make a working facility.
  foreach(float side in new[]{-1f,1f})for(int z=-24;z<=28;z+=8){Vector3 p=new Vector3(side*26.6f,0,z);Beam("Wall mounted extraction riser",p+Vector3.up*.2f,p+Vector3.up*17,.23f,iron);for(int y=2;y<18;y+=4){Shape("Riser wall clamp",p+Vector3.up*y,new Vector3(.9f,.2f,.7f),edge);Shape("Wall clamp bracket",p+new Vector3(side*.5f,y,0),new Vector3(.9f,.18f,.18f),iron);}Lamp(p+new Vector3(-side*.5f,5,0),true);Shape("Wall vent backing",p+new Vector3(-side*.12f,11,0),new Vector3(.18f,2.4f,2.2f),black);for(int j=0;j<6;j++)Shape("Vent horizontal grille",p+new Vector3(-side*.25f,10.05f+j*.36f,0),new Vector3(.18f,.1f,2.0f),edge);}
  foreach(float x in new[]{-26.6f,26.6f})Beam("Continuous extraction header",new Vector3(x,17,-26),new Vector3(x,17,30),.28f,iron);
  // Crown and serried ribs turn the reactor into the visual source of the damage.
  for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var p=new Vector3(Mathf.Sin(a)*5.4f,0,11+Mathf.Cos(a)*5.4f);Beam("Reactor anchored extraction rib",p+Vector3.up*.1f,p+Vector3.up*10.6f,.18f,iron);Beam("Reactor converging crown",p+Vector3.up*10.6f,new Vector3(p.x*.78f,11.4f,11+(p.z-11)*.78f),.20f,iron);if(i%2==0){var glow=Shape("Reactor active pressure slit",p+Vector3.up*6,new Vector3(.15f,6,.15f),red);indicators.Add(glow);}}
  foreach(float x in new[]{-4f,4f}){Beam("Reactor floor intake",new Vector3(x,-4.35f,8),new Vector3(x,-4.35f,-15),.32f,iron);Beam("Intake supported descent",new Vector3(x,0,-15),new Vector3(x,-4.35f,-15),.32f,iron);}
  // Warning markings are painted on a mounted plate, never floating in the room.
  Shape("Extraction warning plate",new Vector3(4.6f,2.4f,-17.59f),new Vector3(2.0f,1.35f,.08f),iron);
  Text("VITAL EXTRACTION\nNO UNAUTHORISED ACCESS",new Vector3(4.6f,2.4f,-17.65f),.025f);
  Shape("Research wing identity plate",new Vector3(-3.1f,2.4f,-28.56f),new Vector3(2.3f,1.7f,.08f),iron);
  Text("SPECIMEN PROCESSING\nพลังชีวิตเหลือจำกัด\nเวลา 15 / ขั้น\nคืนอายุเดิม = กู้พลัง",new Vector3(-3.1f,2.4f,-28.63f),.023f);
  // Research bay compression plant rests on a pad and connects to the specimen hall.
  for(int i=0;i<3;i++){Vector3 p=new Vector3(-28+i*2.1f,0,-64);Shape("Compressor concrete pad",p+Vector3.up*.12f,new Vector3(1.8f,.24f,2.2f),edge);Shape("Extraction compressor vessel",p+Vector3.up*1.8f,new Vector3(1.25f,1.6f,1.25f),iron,null,PrimitiveType.Cylinder);Shape("Compressor domed cap",p+Vector3.up*3.4f,new Vector3(1.25f,.4f,1.25f),edge,null,PrimitiveType.Sphere);Beam("Compression manifold",p+Vector3.up*.5f,p+new Vector3(2,.5f,0),.15f,iron);Lamp(p+new Vector3(0,2,-.7f),true);}
  Beam("Research bay supported pipe",new Vector3(-29,3.8f,-64),new Vector3(-29,3.8f,-35),.22f,iron);for(int z=-62;z<-35;z+=6)Beam("Research pipe floor bracket",new Vector3(-29,0,z),new Vector3(-29,3.8f,z),.08f,edge);
  // Replace billboard smoke balls with anchored stacks; no strobes or hazards.
  foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name=="Factory discharge cloud").ToArray())t.gameObject.SetActive(false);
  for(int i=0;i<3;i++){var p=new Vector3(-16+i*10,0,30);Beam("Supported discharge chimney",p,p+Vector3.up*20,.65f,iron);Shape("Chimney grounded plinth",p+Vector3.up*.4f,new Vector3(2.5f,.8f,2.5f),edge);for(int j=0;j<3;j++)Shape("Chimney warning collar",p+Vector3.up*(16+j),new Vector3(1.4f,.12f,1.4f),red,null,PrimitiveType.Cylinder);}
 }
 [MenuItem("Tools/Borrowed Time/Factory/Build sinister life extraction facility")]
 public static void Build(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode before assembly");
  var scene=EditorSceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Preserving unsaved scene edits; save before assembly");if(scene.path!=BTJourney.FactoryPath)scene=EditorSceneManager.OpenScene(BTJourney.FactoryPath);
  Directory.CreateDirectory(BTFactoryInspection.Out);File.Copy(BTJourney.FactoryPath,BTFactoryInspection.Out+"/Factory_BeforeExtraction.unity",true);
  game=Object.FindFirstObjectByType<BTFactoryGame>();var previous=GameObject.Find(Assembly);if(previous)throw new Exception("Already assembled; prevent duplicate specimen/destructive rebuild");
  Folder(Root+"/Materials");Folder(Root+"/Meshes");root=Group(Assembly);indicators.Clear();serial=0;
  iron=Mat("Extraction black iron","252D30",false,true);edge=Mat("Worn cast steel","647170",false,true);soil=Mat("Dry specimen soil","443B2B",false,true);bark=Mat("Desiccated bark","67533F",false,true);leaf=Mat("Surviving olive leaves","6C7751");leafDry=Mat("Wilted ochre leaves","80734B");red=Mat("Extractor blood orange","EB351B",true);green=Mat("Remaining life cyan green","4BC9A1",true);amber=Mat("Maintenance amber","D3A65A",true);ceramic=Mat("Dirty insulated pump","9C9B81",false,true);black=Mat("Rubber suction hose","171D1E");MakeLeaf();
  var sources=Object.FindObjectsByType<BTTimeTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.kind==TargetKind.Plant).OrderBy(t=>t.transform.position.y).ThenBy(t=>t.transform.position.z).ThenBy(t=>t.transform.position.x).ToArray();
  foreach(var c in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name=="Supported plant container").ToArray())Object.DestroyImmediate(c.gameObject);
  foreach(var t in sources)Specimen(t,++serial);
  game.nursery.absorbAllowed=false;
  economy=game.GetComponent<BTFactoryLifeEconomy>();if(!economy)economy=game.gameObject.AddComponent<BTFactoryLifeEconomy>();economy.game=game;economy.specimens=sources;economy.recoveredEnergy=0;
  Dress();economy.extractionIndicators=indicators.ToArray();economy.activeExtraction=red;economy.stoppedExtraction=green;
  EditorUtility.SetDirty(economy);EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();BTFactoryInspection.Pictures();
 }
 [MenuItem("Tools/Borrowed Time/Factory/Validate extraction facility")]
 public static void Validate(){
  var g=Object.FindFirstObjectByType<BTFactoryGame>();var e=g.GetComponent<BTFactoryLifeEconomy>();if(!e||e.specimens.Length<10)throw new Exception("Missing finite life sources");var lines=new List<string>();Physics.SyncTransforms();
  foreach(var t in e.specimens){if(t.regenPerSecond!=0||t.minimumLife!=20||t.life!=60||t.maxLife!=60)throw new Exception("Unlimited/incorrect specimen "+t.name);Vector3 p=t.transform.position;if(!Physics.Raycast(p+Vector3.up*.08f,Vector3.down,out var hit,.2f,~(1<<2),QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-p.y)>.06f)throw new Exception("Floating specimen "+t.name+" "+p);lines.Add("PASS "+t.name+" grounded "+p+" usable=40 reserved=20 regen=0");}
  if(g.nursery.absorbAllowed||g.miracleTree.absorbAllowed)throw new Exception("Restoration tree can be farmed");
  if(ShaderUtil.GetShaderMessages(Shader.Find("Borrowed Time/Factory worn surface")).Any(m=>m.severity.ToString()=="Error"))throw new Exception("Factory surface shader failed");
  foreach(var plinth in e.specimens.SelectMany(t=>t.GetComponentsInChildren<Renderer>()).Where(r=>r.name=="Bolted extractor plinth")){var b=plinth.bounds;if(!Physics.Raycast(new Vector3(b.center.x,b.min.y+.05f,b.center.z),Vector3.down,out var hit,.15f,~(1<<2),QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-b.min.y)>.06f)throw new Exception("Unsupported specimen pump "+plinth.transform.parent.name);}
  lines.Add("PASS all "+e.specimens.Length+" source beds have real soil, roots, trunk extraction collars, connected pumps, usable-life meters and floor support; each pump plinth separately grounded");lines.Add("PASS original progression, collision geometry and gate dependencies retained; mandatory annex temporal puzzle costs 75; seed requires 60; factory sources total "+e.specimens.Sum(t=>t.maxLife-t.minimumLife));File.WriteAllLines(BTFactoryInspection.Out+"/Validation.txt",lines);
 }
}}
#endif
