#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTControlApproachFix {
 static Transform root;static Material steel;
 static GameObject Box(string name,Vector3 p,Vector3 size,bool solid){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=steel;if(!solid){Object.DestroyImmediate(go.GetComponent<Collider>());go.layer=2;}return go;}
 static void Beam(string name,Vector3 a,Vector3 b,float width,bool solid){var go=Box(name,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),solid);go.transform.rotation=Quaternion.LookRotation(b-a);}
 [MenuItem("Tools/Borrowed Time/Journey/Connect clock courtyard to control ramp")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var city=EditorSceneManager.OpenScene(BTJourney.CityPath);var g=Object.FindFirstObjectByType<BTSkyCityGame>();var previous=GameObject.Find("Supported clock courtyard approach");if(previous)Object.DestroyImmediate(previous);root=new GameObject("Supported clock courtyard approach").transform;steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BorrowedTime/FactoryArt/Materials/Worn cast steel.mat");var ramp=g.puzzles.accessBridge;var rampEntry=ramp.transform.position;var end=rampEntry-Vector3.right*3;end.y=.24f;Vector3 start=end;bool found=false;RaycastHit bank=default;
  Physics.SyncTransforms();for(float d=8;d<=15;d+=.25f){Vector3 p=end+Vector3.forward*d;foreach(var hit in Physics.RaycastAll(p+Vector3.up*8,Vector3.down,14,~(1<<2),QueryTriggerInteraction.Ignore).OrderBy(h=>Mathf.Abs(h.point.y))){if(hit.collider.transform.IsChildOf(ramp.transform)||hit.normal.y<.8f||Mathf.Abs(hit.point.y)>.5f)continue;start=hit.point+Vector3.forward*.7f;found=true;bank=hit;break;}if(found)break;}if(!found)throw new Exception("No courtyard bank found");
  float length=Mathf.Abs(start.z-end.z)+1;Vector3 center=(start+end)*.5f;center.y=.12f;Box("Continuous supported steel approach deck",center,new Vector3(3.2f,.24f,length),true);var turn=(end+rampEntry)*.5f;turn.y=.12f;Box("Front entry turning landing",turn,new Vector3(4,.24f,3.2f),true);Beam("Turning landing outer handrail",new Vector3(end.x,1.29f,end.z-1.65f),new Vector3(rampEntry.x+.2f,1.29f,end.z-1.65f),.11f,true);
  foreach(float side in new[]{-1f,1f}){float x=end.x+side*1.65f;var a=new Vector3(x,.24f,start.z);var b=new Vector3(x,.24f,side>0?end.z+1.9f:end.z-.4f);Beam("Clock approach handrail",a+Vector3.up*1.05f,b+Vector3.up*1.05f,.11f,true);Beam("Clock approach lower rail",a+Vector3.up*.5f,b+Vector3.up*.5f,.07f,true);for(float z=b.z;z<a.z;z+=2)Box("Bolted approach guard post",new Vector3(x,.75f,z),new Vector3(.1f,1.2f,.1f),true);
   Beam("Approach diagonal bearing into island bank",new Vector3(end.x+side*1.25f,0,end.z),new Vector3(start.x+side*1.25f,-3,start.z),.22f,false);}
  Beam("Approach end safety rail",new Vector3(end.x-1.65f,1.29f,end.z-.45f),new Vector3(end.x+1.65f,1.29f,end.z-.45f),.11f,true);
  Physics.SyncTransforms();for(float z=end.z;z<start.z;z+=.2f)if(!Physics.Raycast(new Vector3(end.x,2,z),Vector3.down,out var hit,2.2f,~(1<<2),QueryTriggerInteraction.Ignore))throw new Exception("Approach has a floor gap");
  string bankName=bank.collider.name;EditorSceneManager.MarkSceneDirty(city);EditorSceneManager.SaveScene(city);var copy=Object.Instantiate(root.gameObject);var factory=EditorSceneManager.OpenScene(BTJourney.FactoryPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(factory);SceneManager.MoveGameObjectToScene(copy,factory);var cinema=Object.FindFirstObjectByType<BTFactoryGame>().GetComponent<BTWorldEndingCinematic>();foreach(Transform old in cinema.cityVista.transform.Cast<Transform>().ToArray())if(old.name.StartsWith("Supported clock courtyard approach"))Object.DestroyImmediate(old.gameObject);copy.transform.SetParent(cinema.cityVista.transform,false);foreach(var c in copy.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);foreach(var t in copy.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;EditorSceneManager.CloseScene(city,true);EditorSceneManager.MarkSceneDirty(factory);EditorSceneManager.SaveScene(factory);AssetDatabase.SaveAssets();File.WriteAllText(BTJourneyDirectionChecks.Out+"/ControlApproach.txt","PASS filled missing physical courtyard-to-ramp approach: "+start+" to "+end+"; courtyard support="+bankName+"; ramp puzzle and gate unchanged; rail opening faces the moving ramp\n");
 }
}}
#endif
