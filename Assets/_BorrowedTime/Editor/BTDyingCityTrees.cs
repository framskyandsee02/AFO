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
public static class BTDyingCityTrees {
 [MenuItem("Tools/Borrowed Time/Journey/Ground dry city trees and refresh ending vista")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var city=EditorSceneManager.OpenScene(BTJourney.CityPath);Physics.SyncTransforms();
  var all=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);var trunks=all.Where(r=>r.name.StartsWith("TreeTrunk")).ToArray();var branches=all.Where(r=>r.name.StartsWith("TreeBranch")||r.name.StartsWith("LeafCrown")).ToArray();var groups=trunks.ToDictionary(t=>t,t=>new List<MeshRenderer>{t});
  foreach(var part in branches){var p=part.bounds.center;var nearest=trunks.OrderBy(t=>new Vector2(p.x-t.bounds.center.x,p.z-t.bounds.center.z).sqrMagnitude).First();groups[nearest].Add(part);}
  string matPath="Assets/_BorrowedTime/JourneyDirection/Materials/City_DryOchreLeaves.mat";var brown=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!brown){brown=new Material(branches.First(r=>r.name.StartsWith("LeafCrown")).sharedMaterial);brown.name="Dry ochre leaves";brown.SetColor("_BaseColor",new Color(.39f,.33f,.23f));AssetDatabase.CreateAsset(brown,matPath);}
  int moved=0,removed=0,sparse=0;var log=new List<string>();int index=0;
  foreach(var pair in groups){var b=pair.Key.bounds;var p=new Vector3(b.center.x,b.min.y,b.center.z);Vector3 desired=p;bool supported=false;
   for(int ring=0;ring<=8&&!supported;ring++)for(int i=0;i<(ring==0?1:24);i++){float a=i*Mathf.PI/12;var q=p+new Vector3(Mathf.Sin(a)*ring,0,Mathf.Cos(a)*ring);if(!Physics.Raycast(q+Vector3.up*10,Vector3.down,out var hit,18,~(1<<2),QueryTriggerInteraction.Ignore)||hit.normal.y<.85f)continue;if(hit.point.y>p.y+2.5f)continue;desired=new Vector3(q.x,hit.point.y+.015f,q.z);supported=true;break;}
   if(!supported){foreach(var part in pair.Value)part.gameObject.SetActive(false);removed++;continue;}
   Vector3 delta=desired-p;if(delta.magnitude>.04f){foreach(var part in pair.Value){part.transform.position+=delta;PrefabUtility.RecordPrefabInstancePropertyModifications(part.transform);}moved++;}
   int leaf=0;foreach(var part in pair.Value.Where(r=>r.name.StartsWith("LeafCrown"))){if(index%3==0&&leaf++>0){part.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(part.gameObject);sparse++;}else if(index%3!=1){part.sharedMaterial=brown;PrefabUtility.RecordPrefabInstancePropertyModifications(part);}}
   log.Add("PASS tree "+index+" root seated on supported surface "+desired);index++;
  }
  EditorSceneManager.MarkSceneDirty(city);EditorSceneManager.SaveScene(city);GameObject vista;
  AssetDatabase.StartAssetEditing();try{var method=typeof(BTWorldDirectionBuilder).GetMethod("Vista",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);vista=(GameObject)method.Invoke(null,new object[]{city,"SkyCityGroundedRecovery"});}finally{AssetDatabase.StopAssetEditing();}
  var factory=EditorSceneManager.OpenScene(BTJourney.FactoryPath,OpenSceneMode.Additive);SceneManager.SetActiveScene(factory);SceneManager.MoveGameObjectToScene(vista,factory);var g=factory.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BTFactoryGame>()).First();var cinema=g.GetComponent<BTWorldEndingCinematic>();var old=cinema.cityVista;vista.transform.position=old.transform.position;var obsolete=old.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).ToHashSet();var recovery=cinema.recovery.Where(r=>!obsolete.Contains(r.material)).ToList();
  foreach(var shoot in cinema.newShoots.Where(t=>t.IsChildOf(old.transform)))shoot.SetParent(vista.transform,false);
  foreach(var r in vista.GetComponentsInChildren<Renderer>()){var m=r.sharedMaterial;string n=m.name.ToLowerInvariant();if(!m.HasProperty("_BaseColor"))continue;if(n.Contains("water")||n.Contains("pollut")||n.Contains("river"))recovery.Add(new BTRecoveryMaterial{material=m,from=m.GetColor("_BaseColor"),to=new Color(.24f,.46f,.47f)});else if(n.Contains("leaf")||n.Contains("leaves")||n.Contains("grass")||n.Contains("foliage"))recovery.Add(new BTRecoveryMaterial{material=m,from=m.GetColor("_BaseColor"),to=Color.Lerp(m.GetColor("_BaseColor"),new Color(.35f,.53f,.27f),.55f)});}
  cinema.cityVista=vista;cinema.recovery=recovery.ToArray();Object.DestroyImmediate(old);vista.SetActive(false);EditorUtility.SetDirty(cinema);EditorSceneManager.CloseScene(city,true);EditorSceneManager.MarkSceneDirty(factory);EditorSceneManager.SaveScene(factory);AssetDatabase.SaveAssets();log.Insert(0,"Grounded "+moved+" tree clusters; removed "+removed+" unsupported clusters; sparse dead crowns "+sparse);File.WriteAllLines(BTJourneyDirectionChecks.Out+"/CityTreeSupport.txt",log);
 }
}}
#endif
