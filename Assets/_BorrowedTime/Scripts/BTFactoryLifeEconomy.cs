using System.Linq;
using UnityEngine;
namespace BorrowedTime {
// Factory-only policy: unconfirmed time changes hold energy in the object.
// Returning towards its original age releases exactly that investment.
public class BTFactoryLifeEconomy : MonoBehaviour {
 public BTFactoryGame game;
 public float recoveredEnergy;
 public BTTimeTarget[] specimens;
 public GameObject[] extractionIndicators;
 public Light[] extractionLights=new Light[0];
 public Material activeExtraction,stoppedExtraction;
 bool lastStopped;
 public bool Locked(BTTimeTarget t){var routes=GetComponent<BTFactoryRoutes>();if(routes&&routes.Committed(t))return true;
  if(t==game.miracleTree)return false;
  if(t==game.entryControl && game.entryOpen || t==game.bridge && game.transportRestored || t==game.conveyor && game.conveyorOpen || t==game.coreConsole && game.pollutionStopped)return true;
  if(t==game.liftControl && game.player.checkpoint.y>5)return true;
  var w=GetComponent<BTExpansionWorld>();return w && w.puzzles.Any(p=>p.solved && p.temporal.Contains(t));
 }
 public float Cost(BTTimeTarget t,int next){
  if(t==game.miracleTree)return 15;
  return 15*(Mathf.Abs(next-t.initialAge)-Mathf.Abs(t.age-t.initialAge));
 }
 public void Pay(float cost){
  float after=game.player.energy-cost;
  recoveredEnergy+=Mathf.Max(0,after-100);game.player.energy=Mathf.Clamp(after,0,100);
  ReleaseRecovery();
 }
 public void ReleaseRecovery(){float amount=Mathf.Min(recoveredEnergy,100-game.player.energy);game.player.energy+=amount;recoveredEnergy-=amount;}
 public void ResetEconomy(){recoveredEnergy=0;}
 void Update(){
  if(game.player.paused || game.stage==0)return;
  ReleaseRecovery();
  if(lastStopped!=game.pollutionStopped){lastStopped=game.pollutionStopped;foreach(var go in extractionIndicators)if(go)foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterial=lastStopped?stoppedExtraction:activeExtraction;foreach(var light in extractionLights)if(light)light.color=lastStopped?new Color(.15f,.75f,.60f):new Color(1,.16f,.06f);}
 }
 public string TargetHelp(BTTimeTarget t){
  if(t.kind==TargetKind.Plant)return "พลังที่นำมาใช้ได้ "+Mathf.CeilToInt(Mathf.Max(0,t.life-t.minimumLife))+" • เหลือชีวิตขั้นต่ำ 20 • ไม่ฟื้นเอง";
  if(Locked(t))return "ยืนยันระบบแล้ว • พลังถูกใช้กับระบบนี้แล้ว";
  return "เปลี่ยนเวลา 15 / ขั้น • ย้อนกลับสู่อายุเดิมคืนพลัง 15 / ขั้น";
 }
}}
