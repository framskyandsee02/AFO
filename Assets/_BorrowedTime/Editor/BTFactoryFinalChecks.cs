#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
[InitializeOnLoad]
public static class BTFactoryFinalChecks {
 static bool continuing,finalCheck;static BTSaveData saved;static double due;static bool oldBackground;
 static BTFactoryFinalChecks(){EditorApplication.update+=Tick;}
 public static void CheckContinue(BTFactoryGame game,bool final=false){finalCheck=final;
  game.player.inputOverride=null;BTSaveGame.Save(game);saved=JsonUtility.FromJson<BTSaveData>(File.ReadAllText(BTSaveGame.PathName));
  oldBackground=Application.runInBackground;Application.runInBackground=true;due=EditorApplication.timeSinceStartup+25;continuing=true;
  if(!BTSaveGame.Continue())End(false,"Continue command rejected the isolated test save");
 }
 static string Key(Transform t){string k=t.name;while(t.parent){t=t.parent;k=t.name+"/"+k;}return k;}
 static void Tick(){
  if(!continuing)return;
  if(!EditorApplication.isPlaying){End(false,"Play Mode interrupted during Continue");return;}
  if(EditorApplication.timeSinceStartup>due){End(false,"Save restore timed out");return;}
  var g=Object.FindFirstObjectByType<BTFactoryGame>();if(!g||!g.completed||!BTSaveGame.Notice.StartsWith("โหลดบันทึก"))return;
  try{
   if(g.ending!="Hopeful Renewal"||g.miracleTree.age!=2||g.hasMiracleSeed||!g.seedPlanted||!g.pollutionStopped||!g.waterFlowRestored)throw new Exception("Restored ending / seed / prerequisite mismatch");
   if(saved.factoryRoutes==null||!g.GetComponent<BTFactoryRoutes>().choices.SequenceEqual(saved.factoryRoutes))throw new Exception("Alternative route choices were lost on Continue");
   if(Mathf.Abs(g.player.energy-saved.energy)>.1f)throw new Exception("Restored energy differs");
   if(Mathf.Abs(g.GetComponent<BTFactoryLifeEconomy>().recoveredEnergy-saved.factoryRecovery)>.01f)throw new Exception("Restored recovery balance differs");
   foreach(var t in g.GetComponent<BTFactoryLifeEconomy>().specimens){var d=saved.targets.Single(s=>s.path==Key(t.transform));if(Mathf.Abs(t.life-d.life)>.01f||t.regenPerSecond!=0)throw new Exception("Depleted source regenerated on Continue: "+t.name);}
   End(true,"Save / Continue preserved finite specimen life, player energy, recovery balance, alternative route choices, consumed seed and Hopeful Renewal ending");
  }catch(Exception e){End(false,e.ToString());}
 }
 static void End(bool ok,string message){continuing=false;Directory.CreateDirectory(BTFactoryInspection.Out);File.AppendAllText(BTFactoryInspection.Out+"/SaveContinue.txt",(ok?"PASS ":"FAIL ")+(finalCheck?"manual route: ":"time route: ")+message+"\n");Application.runInBackground=oldBackground;BTSaveGame.CancelPendingContinue();if(ok&&!finalCheck){BTFactoryManualPlaytest.Start(Object.FindFirstObjectByType<BTFactoryGame>());return;}BTSaveGame.TestPathOverride=null;EditorApplication.isPlaying=false;}
 [MenuItem("Tools/Borrowed Time/Factory/Repair practical light mounting")]
 public static void RepairLights(){
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
  var g=Object.FindFirstObjectByType<BTFactoryGame>();var root=GameObject.Find("Factory / Life extraction facility");
  foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Caged wall luminaire")){
   var l=t.GetComponent<Light>();if(!l)continue;Vector3 at=l.transform.position;Color color=l.color;float intensity=l.intensity,range=l.range;t.position=Vector3.zero;Object.DestroyImmediate(l);
   var child=new GameObject("Practical light source");child.transform.SetParent(t,true);child.transform.position=at;var point=child.AddComponent<Light>();point.type=LightType.Point;point.color=color;point.intensity=intensity;point.range=range;
  }
  EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);AssetDatabase.SaveAssets();
 }
 [MenuItem("Tools/Borrowed Time/Factory/Check finite energy conservation")]
 public static void Economy(){
  var g=Object.FindFirstObjectByType<BTFactoryGame>();var e=g.GetComponent<BTFactoryLifeEconomy>();var log=new List<string>();float energy=g.player.energy,reserve=e.recoveredEnergy;int age=g.entryRelays[0].age;
  try{
   var t=g.entryRelays[0];g.player.energy=15;e.recoveredEnergy=0;t.age=t.initialAge;e.Pay(e.Cost(t,0));t.age=0;if(g.player.energy!=0)throw new Exception("First step did not spend 15");e.Pay(e.Cost(t,t.initialAge));t.age=t.initialAge;if(g.player.energy!=15)throw new Exception("Undo at zero energy did not refund 15");log.Add("PASS zero-energy recovery: 15 → wrong choice → 0 → undo → 15");
   for(int i=0;i<20;i++){e.Pay(e.Cost(t,0));t.age=0;e.Pay(e.Cost(t,t.initialAge));t.age=t.initialAge;}if(g.player.energy!=15||e.recoveredEnergy!=0)throw new Exception("Undo loop generated or destroyed energy");log.Add("PASS 20 alternating casts conserve energy; no refund farming");
   g.player.energy=100;e.Pay(-15);if(g.player.energy!=100||e.recoveredEnergy!=15)throw new Exception("Overflow refund lost");e.Pay(15);if(g.player.energy!=100||e.recoveredEnergy!=0)throw new Exception("Overflow not recovered once");log.Add("PASS refund at full capacity is stored once and restored when space becomes available");
   var sources=e.specimens;foreach(var source in sources){float life=source.life;try{float drawn=source.Absorb(1000);if(drawn!=life-20||source.life!=20||source.Absorb(1)!=0||source.regenPerSecond!=0)throw new Exception("Finite floor failed");}finally{source.life=life;source.timeSinceAbsorb=100;}}log.Add("PASS every finite source returns at most 40, retains 20 and cannot be redrained; regeneration disabled");
   File.WriteAllLines(BTFactoryInspection.Out+"/EconomyTests.txt",log);
  }finally{g.player.energy=energy;e.recoveredEnergy=reserve;g.entryRelays[0].age=age;}
 }
}}
#endif
