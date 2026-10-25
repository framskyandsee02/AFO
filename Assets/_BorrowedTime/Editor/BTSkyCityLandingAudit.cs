#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTSkyCityLandingAudit {
 [MenuItem("Tools/Borrowed Time/Journey/Inspect city landing geometry")]
 public static void Inspect(){
  if(EditorApplication.isPlaying)return;EditorSceneManager.OpenScene(BTJourney.CityPath);var g=Object.FindFirstObjectByType<BTSkyCityGame>();var bridge=g.bridgeA.GetComponent<BTSkyCitySlidingBridge>();bridge.SetProgress(1);Physics.SyncTransforms();var log=new List<string>{"DIAGNOSTIC ONLY: bridge shown deployed for geometry inspection; no quest state or playthrough assertions"};
  log.Add("BRIDGE A "+g.bridgeA.position+" forward="+g.bridgeA.forward+" step="+bridge.step+" scale="+g.bridgeA.lossyScale);foreach(var segment in bridge.segments){var renderer=segment.GetComponentInChildren<Renderer>();log.Add("SEGMENT "+segment.position+" bounds="+(renderer?renderer.bounds.ToString():"none"));}
  for(int x=-12;x<=2;x++)foreach(int z in new[]{-2,0,2}){
   var ray=new Ray(new Vector3(x,14,z),Vector3.down);var hits=Physics.RaycastAll(ray,22,~(1<<2),QueryTriggerInteraction.Ignore).OrderByDescending(h=>h.point.y);log.Add(x+","+z+" | "+string.Join(" ; ",hits.Select(h=>h.point.y.ToString("F2")+" "+h.collider.name+" n="+h.normal.y.ToString("F1"))));
  }
  foreach(int x in new[]{-9,-8,-7,-6,-5}){var p=new Vector3(x,.2f,0);log.Add("BODY "+x+" "+string.Join(" | ",Physics.OverlapCapsule(p+Vector3.up*.39f,p+Vector3.up*1.52f,.34f,~(1<<2),QueryTriggerInteraction.Ignore).Select(c=>c.name+" center="+c.bounds.center+" size="+c.bounds.size)));}
  File.WriteAllLines(BTJourneyDirectionChecks.Out+"/LandingGeometry.txt",log);bridge.SetProgress(0);EditorSceneManager.OpenScene(BTJourney.TrainingPath);
 }
}}
#endif
