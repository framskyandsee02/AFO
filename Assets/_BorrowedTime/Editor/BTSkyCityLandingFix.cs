#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTSkyCityLandingFix {
 [MenuItem("Tools/Borrowed Time/Journey/Join deployed bridges to the island banks")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var scene=EditorSceneManager.OpenScene(BTJourney.CityPath);var g=Object.FindFirstObjectByType<BTSkyCityGame>();if(GameObject.Find("Supported city bridge landings"))throw new Exception("Landings already installed");var root=new GameObject("Supported city bridge landings");var report=new List<string>();int n=0;
  var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BorrowedTime/FactoryArt/Materials/Worn cast steel.mat");
  foreach(var bridge in new[]{g.bridgeA.GetComponent<BTSkyCitySlidingBridge>(),g.bridgeB.GetComponent<BTSkyCitySlidingBridge>(),g.puzzles.accessBridge}){
   bridge.SetProgress(1);Physics.SyncTransforms();var bank=bridge.transform;float reach=bridge.step*bridge.segments.Length+.1f;Vector3 direction=bank.forward;direction.y=0;direction.Normalize();Vector3 edge=bank.TransformPoint(new Vector3(0,0,reach));Vector3 from=edge-direction*.65f;
   var deckSamples=Physics.RaycastAll(new Vector3(from.x,14,from.z),Vector3.down,22,~(1<<2),QueryTriggerInteraction.Ignore).Where(h=>h.collider.transform.IsChildOf(bank)&&h.normal.y>.8f).OrderByDescending(h=>h.point.y).ToArray();if(deckSamples.Length==0){report.Add("No deck sample: "+bank.name);bridge.SetProgress(0);continue;}from.y=deckSamples[0].point.y;
   RaycastHit ground=default;float distance=0;bool found=false;for(float d=0;d<=6;d+=.2f){Vector3 p=edge+direction*d;var hits=Physics.RaycastAll(p+Vector3.up*6,Vector3.down,12,~(1<<2),QueryTriggerInteraction.Ignore);foreach(var h in hits.OrderByDescending(h=>h.point.y))if(!h.collider.transform.IsChildOf(bank)&&h.normal.y>.8f&&Mathf.Abs(h.point.y-from.y)<.8f){ground=h;distance=d;found=true;break;}if(found)break;}
   if(!found){report.Add("No bank within six metres: "+bank.name);bridge.SetProgress(0);continue;}Vector3 to=ground.point+direction*.7f;Vector3 right=Vector3.Cross(Vector3.up,direction)*1.55f;Vector3[] v={from-right,from+right,to-right,to+right,from-right-Vector3.up*.2f,from+right-Vector3.up*.2f,to-right-Vector3.up*.2f,to+right-Vector3.up*.2f};var mesh=new Mesh{name="Shore landing "+n};mesh.vertices=v;mesh.triangles=new[]{0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,"Assets/_BorrowedTime/JourneyDirection/Meshes/BridgeBankLanding_"+n+".asset");var go=new GameObject("Shore landing / "+bank.name);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<MeshCollider>().sharedMesh=mesh;
   foreach(float side in new[]{-1f,1f}){var a=from+right*side*.8f-Vector3.up*.2f;var b=to+right*side*.8f-Vector3.up*1.8f;var strut=GameObject.CreatePrimitive(PrimitiveType.Cylinder);strut.name="Abutment strut seated in island bank";strut.layer=2;Object.DestroyImmediate(strut.GetComponent<Collider>());strut.transform.SetParent(root.transform,false);strut.transform.position=(a+b)*.5f;strut.transform.localScale=new Vector3(.16f,Vector3.Distance(a,b)*.5f,.16f);strut.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);strut.GetComponent<Renderer>().sharedMaterial=material;}
   Physics.SyncTransforms();for(float t=0;t<=1;t+=.1f){var p=Vector3.Lerp(from,to,t);if(!Physics.Raycast(p+Vector3.up,Vector3.down,out var hit,1.3f,~(1<<2),QueryTriggerInteraction.Ignore))throw new Exception("Landing surface has a gap");}
   report.Add("PASS "+bank.name+" destination gap="+distance.ToString("F2")+"m, supported apron joins deck y="+from.y.ToString("F2")+" to island y="+ground.point.y.ToString("F2"));bridge.SetProgress(0);n++;
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);var copy=Object.Instantiate(root);var factory=EditorSceneManager.OpenScene(BTJourney.FactoryPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(factory);SceneManager.MoveGameObjectToScene(copy,factory);var cinematic=Object.FindFirstObjectByType<BTFactoryGame>().GetComponent<BTWorldEndingCinematic>();copy.transform.SetParent(cinematic.cityVista.transform,false);foreach(var collider in copy.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);foreach(var t in copy.GetComponentsInChildren<Transform>())t.gameObject.layer=2;EditorSceneManager.CloseScene(scene,true);EditorSceneManager.MarkSceneDirty(factory);EditorSceneManager.SaveScene(factory);AssetDatabase.SaveAssets();File.WriteAllLines(BTJourneyDirectionChecks.Out+"/BridgeLandings.txt",report);
 }
}}
#endif
