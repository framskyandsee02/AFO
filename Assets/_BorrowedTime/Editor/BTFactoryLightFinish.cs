#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTFactoryLightFinish {
 static Transform root;static Material iron,emissive;
 static GameObject Box(string name,Vector3 p,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube){var go=GameObject.CreatePrimitive(type);go.name=name;go.layer=2;go.transform.SetParent(root,true);go.transform.position=p;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go;}
 static void Beam(string name,Vector3 a,Vector3 b,float r){var go=Box(name,(a+b)*.5f,new Vector3(r*2,Vector3.Distance(a,b)*.5f,r*2),iron,PrimitiveType.Cylinder);go.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);}
 [MenuItem("Tools/Borrowed Time/Factory/Finish supported cinematic lighting")]
 public static void Finish(){
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");if(EditorSceneManager.GetActiveScene().path!=BTJourney.FactoryPath)EditorSceneManager.OpenScene(BTJourney.FactoryPath);
  var g=Object.FindFirstObjectByType<BTFactoryGame>();root=GameObject.Find("Factory / Life extraction facility").transform;
  iron=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BorrowedTime/FactoryArt/Materials/Extraction black iron.mat");emissive=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BorrowedTime/FactoryArt/Materials/Maintenance amber.mat");
  foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Caged wall luminaire" && t.position==Vector3.zero).ToArray()){
   var casing=t.Find("Lamp enclosure");if(!casing || casing.position.z< -29)continue;var children=t.Cast<Transform>().ToArray();var positions=children.Select(c=>c.position).ToArray();var rotations=children.Select(c=>c.rotation).ToArray();var p=casing.position;t.position=p;for(int i=0;i<children.Length;i++){children[i].position=positions[i];children[i].rotation=rotations[i];}t.rotation=Quaternion.Euler(0,Mathf.Sign(p.x)*90,0);Beam("Luminaire cantilever attached to retaining wall",p,new Vector3(Mathf.Sign(p.x)*27.7f,p.y,p.z),.055f);
  }
  if(!root.Find("Receiving hall pendant / housing")){
   foreach(var p in new[]{new Vector3(0,5.5f,-27),new Vector3(0,11,-11),new Vector3(0,18,13)}){
    Box(p.z< -20?"Receiving hall pendant / housing":"Workshop pendant housing",p,new Vector3(3.2f,.25f,.7f),iron);Box("Downward diffuser in pendant",p+Vector3.down*.14f,new Vector3(2.9f,.06f,.48f),emissive);foreach(float x in new[]{-1.25f,1.25f})Beam("Pendant cable to actual roof truss",p+new Vector3(x,.12f,0),new Vector3(x,22,p.z),.025f);
    var lamp=new GameObject("Supported warm task light").AddComponent<Light>();lamp.transform.SetParent(root,true);lamp.transform.position=p+Vector3.down*.5f;lamp.type=LightType.Point;lamp.color=new Color(.98f,.70f,.42f);lamp.intensity=7;lamp.range=15;
   }
   foreach(float x in new[]{-12f,12f})Beam("Continuous roof lighting rail",new Vector3(x,22,-27),new Vector3(x,22,29),.055f);
   foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.name=="Industrial practical light")){
    var p=light.transform.position;if(Mathf.Abs(p.x)>24)Beam("Practical light wall bracket",p,new Vector3(Mathf.Sign(p.x)*27.7f,p.y,p.z),.045f);
    else if(Mathf.Abs(Mathf.Abs(p.x)-12)<.1f)Beam("Receiving practical cable",p,new Vector3(p.x,22,p.z),.025f);
   }
  }
  RenderSettings.ambientSkyColor=new Color(.46f,.49f,.54f);RenderSettings.ambientEquatorColor=new Color(.32f,.36f,.39f);RenderSettings.ambientGroundColor=new Color(.17f,.20f,.22f);if(RenderSettings.sun)RenderSettings.sun.intensity=.65f;
  EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();BTFactoryInspection.Pictures();BTFactoryAtmosphereBuilder.Validate();
 }
}}
#endif
