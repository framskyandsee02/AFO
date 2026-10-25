using System;
using System.Linq;
using UnityEngine;
namespace BorrowedTime {
[Serializable] public class BTFactoryManualControl {
 public string title,help;public Transform point;public int puzzle;
 public Transform wheel;public float turned;
}
public class BTFactoryRoutes : MonoBehaviour {
 public BTFactoryGame game;
 // 0 unresolved, 1 time power, 2 physical backup. Save this with the level.
 public int[] choices=new int[8];
 public BTFactoryManualControl[] controls;
 Vector3[] cargoStart;Transform[] cargo;float cargoProgress;
 public bool LiftBackup=>choices[2]==2;
 public string NearestHelp{get;private set;}="";
 void Awake(){cargo=game.conveyor.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Conveyor cargo temporal position").ToArray();cargoStart=cargo.Select(t=>t.position).ToArray();}
 public BTFactoryManualControl Nearest(){
  BTFactoryManualControl best=null;float range=2.5f;
  foreach(var c in controls){if(!c.point)continue;float d=Vector3.Distance(game.player.transform.position,c.point.position);if(d>=range)continue;
   if(Physics.Linecast(game.player.transform.position+Vector3.up*1.2f,c.point.position+Vector3.up, out var hit,~(1<<2),QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(c.point))continue;range=d;best=c;}
  return best;
 }
 public bool TryInteract(){var c=Nearest();if(c==null)return false;
  if(choices[c.puzzle]==1){game.player.Message("ระบบนี้พร้อมแล้ว • ใช้เส้นทางต่อไปได้");return true;}
  switch(c.puzzle){
   case 0:game.entryOpen=true;break;
   case 1:if(!game.entryOpen){game.player.Message("เปิดประตูโถงเข้าก่อน");return true;}game.bridge.age=1;game.bridge.Apply();game.transportRestored=true;break;
   case 2:if(!game.transportRestored){game.player.Message("ทำทางข้ามให้พร้อมก่อน");return true;}break;
   case 3:game.conveyorOpen=true;game.pressureBalanced=true;game.powerLatched=true;break;
   case 4:if(!game.conveyorOpen){game.player.Message("เปิดทางผ่านสายพานก่อน");return true;}game.powerLatched=true;game.emergencyWalkway.age=1;game.emergencyWalkway.Apply();break;
   case 6:if(!game.pollutionStopped||!game.waterFlowRestored){game.player.Message("หยุดโรงงานและเปิดน้ำสะอาดก่อนต่อท่อโรงเพาะกล้า");return true;}game.nursery.age=1;game.nursery.Apply();break;
   case 7:
    if(!game.PlantSeed(true,out string reason)){game.player.Message(reason);return true;}
    break;
  }
  choices[c.puzzle]=2;game.RefreshRoutes();game.PlayTone(650);game.player.Message(c.title+" • ใช้แรงกล ไม่เสียพลังถุงมือ");BTSaveGame.Request(game);return true;
 }
 public bool Committed(BTTimeTarget t){
  if(t==game.bridge)return choices[1]!=0;
  if(t==game.emergencyWalkway)return choices[4]!=0;
  if(t==game.nursery)return choices[6]!=0;
  return false;
 }
 public void ResetRoutes(){growth=0;Array.Clear(choices,0,choices.Length);cargoProgress=0;foreach(var c in controls)c.turned=0;RestoreCargo();}
 void RestoreCargo(){if(cargo==null)return;for(int i=0;i<cargo.Length;i++)if(cargo[i])cargo[i].position=cargoStart[i];}
 float growth;
 void Update(){
  if(game.player.paused||game.stage==0)return;
  if(choices[0]==0&&game.entryOpen)choices[0]=1;
  if(choices[1]==0&&game.transportRestored)choices[1]=1;
  if(choices[2]==0&&game.freightLift.AtHigh)choices[2]=1;
  if(choices[3]==0&&game.conveyorOpen)choices[3]=1;
  if(choices[4]==0&&game.emergencyWalkway.age<2)choices[4]=1;
  if(choices[5]==0&&game.pollutionStopped)choices[5]=game.coreConsole.age<=1?1:2;
  if(choices[6]==0&&game.nurseryRestored)choices[6]=1;
  if(choices[7]==2&&game.seedPlanted&&!game.completed){growth+=Time.deltaTime;if(growth>2&&game.miracleTree.age==0){game.miracleTree.age=1;game.miracleTree.Apply();BTSaveGame.Request(game);}if(growth>4.5f&&game.miracleTree.age==1){game.miracleTree.age=2;game.miracleTree.Apply();BTSaveGame.Request(game);}}
  if(choices[3]==2){cargoProgress=Mathf.MoveTowards(cargoProgress,1,Time.deltaTime);for(int i=0;i<cargo.Length;i++){var p=cargoStart[i];p.x=22;cargo[i].position=Vector3.Lerp(cargoStart[i],p,Mathf.SmoothStep(0,1,cargoProgress));}}
  else if(cargoProgress>0){cargoProgress=0;RestoreCargo();}
  foreach(var c in controls)if(c.wheel){c.turned=Mathf.MoveTowards(c.turned,choices[c.puzzle]==2?260:0,Time.deltaTime*180);c.wheel.localRotation=Quaternion.Euler(90,0,c.turned);}
  var nearest=Nearest();NearestHelp=nearest==null?"":"F  "+nearest.title+"\n"+nearest.help;
 }
}}
