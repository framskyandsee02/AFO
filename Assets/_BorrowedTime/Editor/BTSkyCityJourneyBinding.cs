#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BorrowedTime.Characters;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTSkyCityJourneyBinding {
 [MenuItem("Tools/Borrowed Time/Journey/Bind active Sky City hero and Robo")]
 public static void Bind(){
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");EditorSceneManager.OpenScene(BTJourney.CityPath);
  var sky=Object.FindFirstObjectByType<BTSkyCityGame>();var legacy=Object.FindObjectsByType<BTCityGame>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();var oldPlayer=legacy.player;var a=oldPlayer.GetComponent<BTGameplayCharacter>();
  var bridge=sky.GetComponent<BTSkyCityCharacterBridge>();if(!bridge){
   var visual=a.hero.transform.parent;visual.SetParent(sky.player,false);visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;
   if(sky.visual&&sky.visual!=visual&&sky.visual!=sky.player)sky.visual.gameObject.SetActive(false);sky.visual=visual;
   var robot=legacy.robo;robot.SetParent(null,true);robot.gameObject.SetActive(true);robot.position=sky.spawn+new Vector3(1.15f,1.9f,-.35f);
   bridge=sky.gameObject.AddComponent<BTSkyCityCharacterBridge>();bridge.game=sky;bridge.hero=a.hero;bridge.robo=a.robo;bridge.roboRoot=robot;
   if(sky.cam.IsChildOf(oldPlayer.transform))sky.cam.SetParent(null,true);
   if(oldPlayer.view&&oldPlayer.view.transform!=sky.cam)oldPlayer.view.gameObject.SetActive(false);
   if(oldPlayer.transform!=sky.player)oldPlayer.gameObject.SetActive(false);legacy.gameObject.SetActive(false);
  }
  sky.player.gameObject.SetActive(true);sky.controller.enabled=true;var inactiveDriver=sky.player.GetComponent<BTPlayer>();if(inactiveDriver)inactiveDriver.enabled=false;var inactiveAdapter=sky.player.GetComponent<BTGameplayCharacter>();if(inactiveAdapter)inactiveAdapter.enabled=false;
  foreach(var model in new[]{bridge.hero,bridge.robo}){model.gameObject.SetActive(true);model.animator.enabled=true;model.animator.speed=1;model.animator.applyRootMotion=false;model.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.enabled=true;PrefabUtility.RecordPrefabInstancePropertyModifications(model);PrefabUtility.RecordPrefabInstancePropertyModifications(model.animator);}
  var hud=sky.GetComponent<BTJourneyHUD>();if(!hud)hud=sky.gameObject.AddComponent<BTJourneyHUD>();hud.sky=sky;hud.game=null;
  sky.cam.gameObject.SetActive(true);sky.cam.GetComponent<Camera>().enabled=true;
  var lines=sky.nodes.Select((n,i)=>i+" "+n.id+" "+n.label+" position="+n.model.position+" initial="+n.Initial+" units="+n.timeUnits+" ambient="+n.ambient).ToList();lines.Insert(0,"PLAYER "+sky.player.position+" spawn="+sky.spawn+" door="+sky.door.position+" controller="+sky.controller.height+" / "+sky.controller.center);Directory.CreateDirectory(BTJourneyDirectionChecks.Out);File.WriteAllLines(BTJourneyDirectionChecks.Out+"/SkyCityNodes.txt",lines);
  var water=sky.nodes[3].model.position;
  Vector3[] shoots=new Vector3[6];var plant=sky.nodes.FirstOrDefault(n=>n.id.Contains("Plant")&&n.model.position.x> -40);
  Vector3 basePoint=plant!=null?plant.model.position:water;Physics.SyncTransforms();for(int i=0;i<6;i++){var p=basePoint+new Vector3(1+i*.25f,0,-1);if(Physics.Raycast(p+Vector3.up*8,Vector3.down,out var hit,12,~(1<<2),QueryTriggerInteraction.Ignore))p.y=hit.point.y;shoots[i]=p;}
  EditorUtility.SetDirty(sky);EditorSceneManager.MarkSceneDirty(sky.gameObject.scene);EditorSceneManager.SaveScene(sky.gameObject.scene);
  // Capture the active city controller's real starting neighbourhood.
  var temporary=Object.FindObjectsByType<BTTrainingGame>(FindObjectsInactive.Include,FindObjectsSortMode.None).OfType<BTCityGame>().First();temporary.player.view=sky.cam.GetComponent<Camera>();BTJourneyDirectionChecks.Photo(temporary,"SkyCity_Overcast",sky.spawn+new Vector3(18,15,-20),sky.spawn+new Vector3(12,0,0));BTJourneyDirectionChecks.Photo(temporary,"SkyCity_WaterDistrict",water+new Vector3(15,9,-16),water+Vector3.up*1.5f);
  EditorSceneManager.OpenScene(BTJourney.FactoryPath);var factory=Object.FindFirstObjectByType<BTFactoryGame>();var cinema=factory.GetComponent<BTWorldEndingCinematic>();cinema.cityCameraStart=water+new Vector3(15,8,-16);cinema.cityCameraEnd=water+new Vector3(12,7,-14);cinema.cityLookAt=water+Vector3.up*1.5f;
  int at=0;foreach(var shoot in cinema.newShoots.Where(t=>t.IsChildOf(cinema.cityVista.transform)))shoot.localPosition=shoots[at++];EditorUtility.SetDirty(cinema);EditorSceneManager.MarkSceneDirty(factory.gameObject.scene);EditorSceneManager.SaveScene(factory.gameObject.scene);AssetDatabase.SaveAssets();
 }
}}
#endif
