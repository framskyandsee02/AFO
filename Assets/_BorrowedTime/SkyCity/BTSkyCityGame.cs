using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime {
public class BTSkyCityGame:MonoBehaviour {
 [Serializable] public class TimeNode {
  public string id,label; public Transform model; public Renderer[] surfaces;
  public bool drain,ambient; public bool targetable=true; public int prerequisite=-1;
  [Range(0,1)] public float value; public float initialValue=-1,timeUnits=100;
  public Vector3 originalScale;
  public bool Done {get{return drain?value<=.001f:value>=.999f;}}
  public float Initial {get{return initialValue>=0?initialValue:(drain?1:0);}}
 }
 public const string SavePrefix="SkyCityChain1.";
 public TimeNode[] nodes;
 public Transform player,visual,cam,bridgeA,bridgeB,door;
 public CharacterController controller;public BTSkyCityPuzzles puzzles;public BTSkyCityHints hints;
 public Vector3 spawn=new Vector3(-87,.35f,0);public float energy;public bool previewOnly;
 float yaw=90,pitch=22,vy,scrollIntent,scrollUntil,bridgeProgressA,bridgeProgressB,doorProgress;
 int focused=-1;bool pause,introOpened;string message="";float messageUntil;Vector3 doorStart;
 GUIStyle title,body,small;Texture2D panel;
 public int FocusedNode {get{return focused;}}
 public bool IntroAccess {get{return introOpened;}}
 public float LastTransferTime {get;private set;}=-100;
 public float LastTransferDirection {get;private set;}
 public int LastTransferTarget {get;private set;}=-1;
 public float BridgeProgress {get{return bridgeProgressB;}}
 public bool GeneratorReady {get{return nodes!=null&&nodes.Length>=2&&nodes[0].Done&&nodes[1].Done;}}
 public bool DistrictComplete(int d){return puzzles&&puzzles.Complete(d);}
 public bool FactoryReady {get{return puzzles&&puzzles.ReadyToLaunch;}}
 public bool FactoryDocked {get{return FactoryReady&&bridgeProgressB>=.999f;}}
 bool Testing {get{return puzzles&&puzzles.testMode;}}
 void Start(){
  if(previewOnly)return;
  foreach(var n in nodes){n.originalScale=n.model.localScale;n.value=PlayerPrefs.GetFloat(SavePrefix+n.id,n.Initial);}
  energy=PlayerPrefs.GetFloat(SavePrefix+"Energy",0);introOpened=PlayerPrefs.GetInt(SavePrefix+"Intro",0)==1;
  if(puzzles)puzzles.LoadState();doorStart=door.localPosition;player.position=spawn;
  var camera=cam.GetComponent<Camera>();camera.orthographic=false;camera.fieldOfView=55;
  cam.rotation=Quaternion.Euler(pitch,yaw,0);cam.position=player.position+Vector3.up*1.6f+cam.rotation*new Vector3(.5f,0,-4.5f);
  bridgeProgressA=introOpened?1:0;bridgeProgressB=puzzles&&puzzles.commissioned?1:0;doorProgress=bridgeProgressB;
  SetBridge(bridgeA,bridgeProgressA);SetBridge(bridgeB,bridgeProgressB);door.localPosition=doorStart+Vector3.up*10*doorProgress;
  Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
 }
 public static float MoveFreshness(float value,float direction,float amount,float energy,float units,out float remaining){
  value=Mathf.Clamp01(value);remaining=Mathf.Clamp(energy,0,100);amount=Mathf.Max(0,amount);units=Mathf.Max(.01f,units);
  if(direction<0){float delta=Mathf.Min(value,amount);remaining=Mathf.Min(100,remaining+delta*units);return value-delta;}
  if(direction>0){float delta=Mathf.Min(1-value,amount,remaining/units);remaining=Mathf.Max(0,remaining-delta*units);return value+delta;}
  return value;
 }
 public bool ApplyTime(int index,float direction,float amount){
  var n=nodes[index];float before=n.value;float left;n.value=MoveFreshness(before,direction,amount,energy,n.timeUnits,out left);energy=left;
  bool changed=Mathf.Abs(n.value-before)>.000001f;
  if(changed){LastTransferTime=Time.time;LastTransferDirection=direction;LastTransferTarget=index;if(n.value>=.999f&&before<.999f)Say(n.label+" — ฟื้นสภาพสมบูรณ์แล้ว");else if(n.value<=.001f&&before>.001f)Say(n.label+" — ผุสลายแล้ว");if(!Testing){PlayerPrefs.SetFloat(SavePrefix+n.id,n.value);PlayerPrefs.SetFloat(SavePrefix+"Energy",energy);}}
  return changed;
 }
 public void ResetAll(){
  foreach(var n in nodes){n.value=n.Initial;if(!Testing)PlayerPrefs.DeleteKey(SavePrefix+n.id);}
  if(puzzles)puzzles.ResetState();if(hints)hints.ResetHints();energy=0;vy=0;scrollUntil=0;LastTransferTime=-100;introOpened=false;
  if(!Testing){PlayerPrefs.DeleteKey(SavePrefix+"Energy");PlayerPrefs.DeleteKey(SavePrefix+"Intro");}
  bridgeProgressA=bridgeProgressB=doorProgress=0;SetBridge(bridgeA,0);SetBridge(bridgeB,0);door.localPosition=doorStart;
  controller.enabled=false;player.position=spawn;controller.enabled=true;Say("เริ่มปริศนาเมืองใหม่แล้ว");
 }
 public void Say(string text){message=text;messageUntil=Time.unscaledTime+4;}
 public Vector3 AimPoint(int index){var c=nodes[index].model.GetComponent<BoxCollider>();return c?c.bounds.center:nodes[index].model.position;}
 public int FindTarget(Ray ray){
  var hits=Physics.SphereCastAll(ray,.18f,16,~(1<<2),QueryTriggerInteraction.Collide);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
  foreach(var hit in hits){for(int i=0;i<nodes.Length;i++){var n=nodes[i];if(!n.targetable)continue;if(hit.transform==n.model||hit.transform.IsChildOf(n.model))return Vector3.Distance(player.position,AimPoint(i))<7?i:-1;}if(!hit.collider.isTrigger)return -1;}
  return -1;
 }
 void Update(){
  if(previewOnly)return;var k=Keyboard.current;var m=Mouse.current;
  if(k!=null&&k.escapeKey.wasPressedThisFrame){pause=!pause;Cursor.lockState=pause?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=pause;}
  if(pause)return;if(k!=null&&k.rKey.wasPressedThisFrame)ResetAll();float dt=Time.deltaTime;
  if(m!=null){var look=m.delta.ReadValue();yaw+=look.x*.12f;pitch=Mathf.Clamp(pitch-look.y*.1f,-8,65);}
  Vector2 input=k==null?Vector2.zero:new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
  var move=Vector3.ClampMagnitude(Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y),1);
  if(controller.isGrounded&&vy<0)vy=-2;if(k!=null&&k.spaceKey.wasPressedThisFrame&&controller.isGrounded)vy=7;vy-=22*dt;
  controller.Move((move*(k!=null&&k.leftShiftKey.isPressed?8:5.2f)+Vector3.up*vy)*dt);
  if(visual&&move.sqrMagnitude>.02f)visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(move),dt*12);
  if(player.position.y<-25){controller.enabled=false;player.position=spawn;controller.enabled=true;vy=0;}
  var pivot=player.position+Vector3.up*1.6f;var rotation=Quaternion.Euler(pitch,yaw,0);var desired=pivot+rotation*new Vector3(.5f,0,-4.5f);RaycastHit wall;
  if(Physics.Linecast(pivot,desired,out wall,~(1<<2),QueryTriggerInteraction.Ignore))desired=wall.point+(pivot-wall.point).normalized*.25f;
  cam.position=Vector3.Lerp(cam.position,desired,1-Mathf.Exp(-dt*18));cam.rotation=rotation;focused=FindTarget(new Ray(cam.position,cam.forward));
  if(m!=null&&Mathf.Abs(m.scroll.ReadValue().y)>.01f){scrollIntent=Mathf.Sign(m.scroll.ReadValue().y);scrollUntil=Time.time+.20f;}
  if(focused>=0&&Time.time<scrollUntil){string reason="";bool handled=puzzles&&puzzles.Transfer(focused,scrollIntent,dt,out reason);if(!handled){var n=nodes[focused];if(n.prerequisite>=0&&!nodes[n.prerequisite].Done)reason="ดูดรากที่ขวางก่อน แล้วซ่อมเครื่องกำเนิด";else if(!ApplyTime(focused,scrollIntent,dt)&&scrollIntent>0&&energy<.01f)reason="เวลาไม่พอ: ดูดจากลัง ม้านั่ง โคมไฟ หรือพุ่มไม้รอบเมือง";}if(reason!="")Say(reason);}
  if(GeneratorReady&&!introOpened){introOpened=true;if(!Testing)PlayerPrefs.SetInt(SavePrefix+"Intro",1);Say("เครื่องกำเนิดทำงานแล้ว — สะพานเข้าเมืองกำลังเลื่อนออก");}
  bridgeProgressA=Mathf.MoveTowards(bridgeProgressA,introOpened?1:0,dt*.16f);bridgeProgressB=Mathf.MoveTowards(bridgeProgressB,FactoryReady?1:0,dt*.16f);
  SetBridge(bridgeA,bridgeProgressA);SetBridge(bridgeB,bridgeProgressB);
  if(FactoryDocked&&puzzles&&!puzzles.commissioned)puzzles.MarkDocked();
  doorProgress=Mathf.MoveTowards(doorProgress,FactoryDocked?1:0,dt*.32f);door.localPosition=doorStart+Vector3.up*10*doorProgress;
  if(k!=null&&k.fKey.wasPressedThisFrame&&Vector3.Distance(player.position,new Vector3(door.position.x+2,0,door.position.z))<5){if(FactoryDocked&&doorProgress>.95f)BTJourney.LoadFactory(energy,true,true);else Say("รอสะพานเชื่อมฐานและกลอนประตูถอนออกก่อน");}
 }
 void SetBridge(Transform b,float progress){if(b){var sliding=b.GetComponent<BTSkyCitySlidingBridge>();if(sliding)sliding.SetProgress(progress);}}
 public string AgeDescription(int i){float v=nodes[i].value;return v<=.001f?"ผุพัง / หมดอายุ":v>=.999f?"สมบูรณ์ / ฟื้นสภาพ":v<.4f?"เก่าและเสียหาย":"สภาพกำลังเปลี่ยน";}
 void OnGUI(){
  if(previewOnly)return;
  if(title==null){Font font=hints?hints.thaiFont:null;title=new GUIStyle(GUI.skin.label){font=font,fontSize=23,fontStyle=FontStyle.Bold};title.normal.textColor=new Color(.7f,1,1);body=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};body.normal.textColor=Color.white;small=new GUIStyle(body){fontSize=15};panel=new Texture2D(1,1);panel.SetPixel(0,0,new Color(.025f,.07f,.1f,.9f));panel.Apply();}
  GUI.DrawTexture(new Rect(24,24,510,145),panel);GUI.Label(new Rect(40,32,480,35),"BORROWED TIME / เมืองลอยฟ้า",title);
  string objective=!introOpened?"ดูดรากที่ขวาง แล้วให้เวลาซ่อมเครื่องกำเนิด":puzzles.Objective();GUI.Label(new Rect(40,76,470,85),objective,body);
  GUI.DrawTexture(new Rect(Screen.width-235,24,211,70),panel);GUI.Label(new Rect(Screen.width-217,37,195,35),"เวลา  "+Mathf.RoundToInt(energy)+" / 100",body);
  GUI.Label(new Rect(Screen.width/2-8,Screen.height/2-12,30,30),"+",title);
  if(focused>=0){var n=nodes[focused];GUI.DrawTexture(new Rect(Screen.width/2-285,Screen.height-204,570,116),panel);GUI.Label(new Rect(Screen.width/2-267,Screen.height-200,536,108),n.label+" — "+AgeDescription(focused)+"\n"+(n.ambient?"เลื่อนลง: ดูดให้ผุ / เลื่อนขึ้น: ให้เวลาซ่อมกลับ":puzzles.Clue(focused)),body);}
  GUI.Label(new Rect(24,Screen.height-43,Screen.width-32,35),"WASD เดิน | Shift วิ่ง | Space กระโดด | เลื่อนลง ดูดเวลา / ขึ้น ให้เวลา | H ใบ้ | F เข้าโรงงาน | R เริ่มใหม่ | Esc พัก",small);
  if(Time.unscaledTime<messageUntil){GUI.DrawTexture(new Rect(24,180,650,75),panel);GUI.Label(new Rect(40,188,618,65),message,body);}
 }
 void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
}
}
