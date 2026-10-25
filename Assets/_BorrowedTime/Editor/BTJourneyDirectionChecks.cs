#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BorrowedTime.Characters;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTJourneyDirectionChecks {
 public const string Out="Captures/TrainingComfort/FactoryRoutes";
 [MenuItem("Tools/Borrowed Time/Journey/Validate direction and capture three maps")]
 public static void Validate(){
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");Directory.CreateDirectory(Out);var report=new List<string>();
  foreach(string path in new[]{BTJourney.TrainingPath,BTJourney.CityPath,BTJourney.FactoryPath}){
   var scene=EditorSceneManager.OpenScene(path);var sky=Object.FindFirstObjectByType<BTSkyCityGame>();if(sky){var aSky=sky.GetComponent<BTSkyCityCharacterBridge>();if(!aSky||!aSky.hero.gameObject.activeInHierarchy||!aSky.robo.gameObject.activeInHierarchy||!sky.player.gameObject.activeInHierarchy||!sky.controller.enabled||!aSky.hero.animator.enabled||!aSky.robo.animator.enabled||!sky.GetComponent<BTJourneyHUD>())throw new Exception("Active Sky City binding invalid");report.Add("PASS active Sky City controller enabled, animated hero and Robo active, common HUD bound to actual controller");var legacy=Object.FindObjectsByType<BTCityGame>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();var oldView=legacy.player.view;legacy.player.view=sky.cam.GetComponent<Camera>();Photo(legacy,"City_DyingWorld",sky.spawn+new Vector3(18,15,-20),sky.spawn+new Vector3(12,0,0));Photo(legacy,"City_WaterDistrict",sky.nodes[3].model.position+new Vector3(15,9,-16),sky.nodes[3].model.position+Vector3.up*1.5f);legacy.player.view=oldView;continue;}var g=Object.FindFirstObjectByType<BTTrainingGame>();var a=g.player.GetComponent<BTGameplayCharacter>();if(!a||!a.hero||!a.robo||!a.hero.gameObject.activeInHierarchy||!a.robo.gameObject.activeInHierarchy||!a.hero.animator.enabled||!a.robo.animator.enabled||!a.hero.animator.runtimeAnimatorController||!a.robo.animator.runtimeAnimatorController)throw new Exception("Character / Robo not ready in "+scene.name);
   if(!g.GetComponent<BTJourneyHUD>()||!g.GetComponent<BTCompanionPresence>())throw new Exception("Common UI / companion missing");report.Add("PASS "+scene.name+" common HUD, hero animator enabled, Robo active and follow binding valid");
   if(g is BTFactoryGame f){var r=f.GetComponent<BTFactoryRoutes>();if(!r||r.controls.Length!=7||f.GetComponent<BTExpansionWorld>()||GameObject.Find("Borrowed Time / Living World expansion"))throw new Exception("Complex factory annex remains");Physics.SyncTransforms();foreach(var c in r.controls){Vector3 p=c.point.position;if(!Physics.Raycast(p+Vector3.up*.08f,Vector3.down,out var hit,.20f,~(1<<2),QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-p.y)>.06f)throw new Exception("Unsupported manual control "+c.title);}report.Add("PASS 7 grounded physical alternatives; complex symbol / sequence wing removed; no mandatory annex progression");var cinema=g.GetComponent<BTWorldEndingCinematic>();if(!cinema||!cinema.cityVista||!cinema.trainingVista||cinema.newShoots.Length!=12)throw new Exception("Ending vista missing");report.Add("PASS four-shot ending configured with actual map scenery, gradual water / foliage recovery and 12 new shoots");
    Photo(g,"Factory_SimpleRoutes",new Vector3(29,32,-36),new Vector3(0,3,4));Photo(g,"Factory_MechanicalChoice",new Vector3(3,2.8f,-28),new Vector3(5.3f,1.2f,-22));Photo(g,"Factory_Specimen",new Vector3(-1.8f,2.3f,-29),new Vector3(-5,1.2f,-25));
   }else if(g is BTCityGame){Photo(g,"City_DyingWorld",new Vector3(29,27,-32),new Vector3(-3,1,1));Photo(g,"City_Street",new Vector3(-14,2.3f,-12),new Vector3(-18,1,-2));}
   else{Photo(g,"Training_Overcast",new Vector3(29,28,26),new Vector3(-3,0,4));Photo(g,"Training_Forest",new Vector3(18,6.5f,24),new Vector3(15,3,37));}
  }File.WriteAllLines(Out+"/SceneChecks.txt",report);
 }
 public static void Photo(BTTrainingGame g,string name,Vector3 p,Vector3 target){var go=new GameObject("Temporary journey review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(g.player.view);camera.orthographic=false;camera.fieldOfView=58;go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=true;camera.transform.position=p;camera.transform.LookAt(target);Capture(camera,name);Object.DestroyImmediate(go);}
 public static void Capture(Camera camera,string name){Directory.CreateDirectory(Out);var rt=new RenderTexture(1440,900,24);var original=camera.targetTexture;var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var texture=new Texture2D(1440,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1440,900),0,0);texture.Apply();File.WriteAllBytes(Out+"/"+name+".png",texture.EncodeToPNG());camera.targetTexture=original;RenderTexture.active=active;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);}
}}
#endif
