using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime {
[Serializable] public class BTExpansionPuzzle {
 public string id,title,clue,lore;public bool optional,solved,collected,clueRead;
 public BTTimeTarget[] temporal=new BTTimeTarget[0];public int[] ages=new int[0];
 public int[] valves=new int[3],goals=new int[3],sequence=new int[0];public int cursor,hints;
 public Transform gate;public Vector3 gateClosed;public GameObject restoration;
 public Renderer[] lamps=new Renderer[0];public Material[] lampMaterials;public Transform[] hands=new Transform[0];
 public Vector3 approach;public BTExpansionInteractable[] controls;
}
[Serializable] public class BTExpansionPuzzleSave {public string id;public bool solved,collected,read;public int cursor;public int[] valves;}
[Serializable] public class BTExpansionSave {public BTExpansionPuzzleSave[] puzzles;}
public class BTExpansionWorld : MonoBehaviour {
 public BTTrainingGame game;public string chapter;public BTExpansionPuzzle[] puzzles;public Font font;
 public bool journalOpen;bool previousPause;int page;GUIStyle heading,body,button,small;
 public string LastInteraction {get;private set;}
 public int RequiredSolved=>puzzles.Count(p=>!p.optional&&p.solved);
 public int RequiredCount=>puzzles.Count(p=>!p.optional);
 public static bool CanLeave(BTTrainingGame game,out string reason){
  var world=game.GetComponent<BTExpansionWorld>();reason="";if(!world||world.RequiredSolved==world.RequiredCount)return true;
  reason="ยังมีระบบส่วนขยายที่ต้องฟื้นฟู: "+string.Join(" / ",world.puzzles.Where(p=>!p.optional&&!p.solved).Select(p=>p.title))+" • J เปิดบันทึก";return false;
 }
 public BTExpansionInteractable Nearest(){
  if(!game||!game.player)return null;BTExpansionInteractable best=null;float distance=2.8f;
  foreach(var p in puzzles)foreach(var c in p.controls){
   if(!c||!c.isActiveAndEnabled)continue;float d=Vector3.Distance(game.player.transform.position,c.transform.position);if(d>=distance)continue;
   Vector3 a=game.player.transform.position+Vector3.up*1.15f,b=c.transform.position+Vector3.up*.8f;
   if(Physics.Linecast(a,b,out var hit,~(1<<2),QueryTriggerInteraction.Ignore)&&!hit.collider.transform.IsChildOf(c.transform)&&hit.collider.transform!=c.transform)continue;
   best=c;distance=d;
  }return best;
 }
 public bool TryInteract(){var c=Nearest();return c&&Use(c);}
 public bool Use(BTExpansionInteractable c){
  if(!c||c.world!=this||c.puzzle<0||c.puzzle>=puzzles.Length||!game||game.player.paused||Vector3.Distance(game.player.transform.position,c.transform.position)>2.8f)return false;
  var p=puzzles[c.puzzle];
  LastInteraction=p.id+" / "+c.role+" / "+c.slot;
  #if UNITY_EDITOR
  if(game.player.inputOverride!=null)Debug.Log("EXPANSION_INTERACT "+LastInteraction+" valves="+string.Join(",",p.valves)+" ages="+string.Join(",",p.temporal.Select(t=>t.age)));
  #endif
  if(c.role==BTExpansionRole.Clue){p.clueRead=true;page=c.puzzle;ToggleJournal();return true;}
  if(c.role==BTExpansionRole.Reset){p.cursor=0;Array.Clear(p.valves,0,p.valves.Length);game.player.Message("รีเซ็ตวงจรแล้ว • ทดลองใหม่ได้ พลังและของรางวัลไม่หาย");Refresh();return true;}
  if(c.role==BTExpansionRole.Reward){
   if(!p.solved){game.player.Message("ฟื้นฟูสวนลับก่อนจึงจะเก็บบันทึกความทรงจำได้");return true;}
   if(!p.collected){p.collected=true;p.clueRead=true;game.player.Message("ได้รับใบไม้แห่งความทรงจำ • "+p.lore);BTSaveGame.Request(game);BTPresentation.Burst(c.transform.position+Vector3.up,Color.green,18);}
   else game.player.Message("บันทึกนี้อยู่ในสมุด J แล้ว");return true;
  }
  if(p.solved){game.player.Message(p.title+" ฟื้นฟูแล้ว");return true;}
  if(p.optional&&RequiredSolved<RequiredCount){game.player.Message("สวนลับต้องการน้ำและแสงจากปริศนาหลักทั้งสองจุดก่อน");return true;}
  if(c.role==BTExpansionRole.Valve){
   if(c.puzzle==1 && game is BTFactoryGame && !puzzles[0].solved){game.player.Message("เปิดระบบหล่อเย็นก่อน วงจรกังหันจึงจะรับพลังได้");return true;}
   p.valves[c.slot]=(p.valves[c.slot]+1)%3;
   if(p.temporal.Length==0){int next=(c.slot+(game is BTFactoryGame?2:1))%3;p.valves[next]=(p.valves[next]+1)%3;}
   game.player.Message("วาล์ว "+(c.slot+1)+" • วงจร "+string.Join(" / ",p.valves.Select(v=>Symbol(v))));
  }else if(c.role==BTExpansionRole.Chime){
   if(p.cursor>=p.sequence.Length){game.player.Message("ทำนองครบแล้ว • ยืนยันที่แท่นกลาง");return true;}
   if(c.slot==p.sequence[p.cursor]){p.cursor++;game.PlayTone(420+p.cursor*110);game.player.Message("ความทรงจำ "+p.cursor+" / "+p.sequence.Length);}
   else {p.cursor=0;game.PlayTone(190);game.player.Message("ทำนองเริ่มใหม่ • อ่านภาพฤดูกาลที่ป้าย ไม่เสียของหรือพลัง");}
  }else if(c.role==BTExpansionRole.Submit){
   bool correct=p.optional?p.cursor==p.sequence.Length:p.valves.SequenceEqual(p.goals)&&p.temporal.Select(t=>t.age).SequenceEqual(p.ages);
   if(correct){p.solved=true;game.PlayTone(880);game.player.Message("ฟื้นฟูสำเร็จ: "+p.title);BTPresentation.Burst(c.transform.position+Vector3.up,Color.green,24);}
   else game.player.Message(p.optional?"ทำนองยังไม่ครบ • ใช้ภาพเล่าฤดูกาลที่ป้าย":"ระบบยังไม่สมดุล • ตรวจอายุวัตถุและสัญลักษณ์ไฟทั้งสามช่อง • H คำใบ้");
  }
  Refresh();BTSaveGame.Request(game);return true;
 }
 public static string Symbol(int v)=>v==0?"จันทร์":v==1?"อาทิตย์":"ใบไม้";
 public bool TryHint(){var c=Nearest();if(!c)return false;var p=puzzles[c.puzzle];p.clueRead=true;
  string hint=p.hints++==0?p.clue:p.hints==2?(p.temporal.Length>0?"อายุที่เหมาะสมขึ้นอยู่กับหน้าที่ของวัตถุ อ่านแผ่นบันทึกของสถานที่ แล้วจัดวาล์วควบคู่กัน":"ปุ่มหนึ่งเปลี่ยนไฟของตัวเองและไฟที่เชื่อมด้วยท่อพร้อมกัน ดูทิศทางและเบาะแสบนป้าย"):(p.optional?"ลำดับ: "+string.Join(" → ",p.sequence.Select(v=>Symbol(v))):"เป้าหมายไฟ: "+string.Join(" / ",p.goals.Select(v=>Symbol(v)))+(p.temporal.Length>0?" • อายุวัตถุ: "+string.Join(" / ",p.ages):" • รีเซ็ตแล้วทดลองทีละปุ่ม"));
  game.roboLine="Robo: "+hint;game.player.Message(hint);return true;
 }
 public void ToggleJournal(){
  journalOpen=!journalOpen;if(journalOpen){previousPause=game.player.paused;game.player.paused=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  else{game.player.paused=previousPause;Cursor.lockState=game.player.paused?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=game.player.paused;}
 }
 public void ResetWorld(){
  if(journalOpen)ToggleJournal();foreach(var p in puzzles){p.solved=p.collected=p.clueRead=false;p.cursor=p.hints=0;Array.Clear(p.valves,0,p.valves.Length);foreach(var t in p.temporal)t.ResetTarget();}Refresh();
 }
 public BTExpansionSave Capture()=>new BTExpansionSave{puzzles=puzzles.Select(p=>new BTExpansionPuzzleSave{id=p.id,solved=p.solved,collected=p.collected,read=p.clueRead,cursor=p.cursor,valves=(int[])p.valves.Clone()}).ToArray()};
 public void Restore(BTExpansionSave save){if(save?.puzzles==null)return;foreach(var d in save.puzzles){var p=puzzles.FirstOrDefault(x=>x.id==d.id);if(p==null)continue;p.solved=d.solved;p.collected=d.collected;p.clueRead=d.read;p.cursor=Mathf.Clamp(d.cursor,0,p.sequence.Length);if(d.valves!=null&&d.valves.Length==3)p.valves=d.valves.Select(x=>Mathf.Clamp(x,0,2)).ToArray();}Refresh();}
 public void Refresh(){foreach(var p in puzzles){if(p.restoration)p.restoration.SetActive(p.solved);for(int i=0;i<p.lamps.Length;i++)if(p.lamps[i])p.lamps[i].sharedMaterial=p.lampMaterials[p.valves[i%3]];}}
 void Start(){Refresh();}
 void Update(){
  if(journalOpen&&!game.player.paused){journalOpen=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
  foreach(var p in puzzles){if(p.gate)p.gate.localPosition=Vector3.MoveTowards(p.gate.localPosition,p.gateClosed+Vector3.up*(p.solved?4:0),Time.deltaTime*2);for(int i=0;i<p.hands.Length;i++)if(p.hands[i])p.hands[i].localRotation=Quaternion.Euler(0,0,p.valves[i%3]*120);}
 }
 void Init(){if(body!=null)return;body=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};heading=new GUIStyle(body){fontSize=27,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=15};button=new GUIStyle(GUI.skin.button){font=font,fontSize=19,wordWrap=true};}
 void OnGUI(){
  if(!game||!game.player||game.player.hideHud)return;Init();GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));GUI.depth=-30;
  if(journalOpen){
   GUI.color=new Color(.04f,.08f,.10f,.98f);GUI.DrawTexture(new Rect(110,170,1220,525),Texture2D.whiteTexture);GUI.color=Color.white;
   GUI.Label(new Rect(145,188,1100,45),"สมุดภาคสนาม / "+chapter,heading);
   for(int i=0;i<puzzles.Length;i++){if(GUI.Button(new Rect(145,250+i*75,330,60),(puzzles[i].solved?"● ":puzzles[i].optional?"☆ ":"○ ")+puzzles[i].title,button))page=i;}
   var p=puzzles[Mathf.Clamp(page,0,puzzles.Length-1)];GUI.Label(new Rect(510,250,760,45),p.title,heading);
   GUI.Label(new Rect(510,308,760,225),p.clueRead?p.clue:"ยังไม่ได้อ่านหลักฐาน • สำรวจป้ายบันทึกประจำสถานที่ แล้วกด F",body);
   GUI.Label(new Rect(510,535,760,65),p.solved?(p.collected?p.lore:"ฟื้นฟูสำเร็จแล้ว • สำรวจต่อเพื่อเก็บความทรงจำ"):(p.optional?"พื้นที่พิเศษ: ฟื้นระบบน้ำและแสงทั้งสองจุดก่อน":"ลองได้ไม่จำกัด • ปุ่มรีเซ็ตคืนเฉพาะวงจร ไม่กินพลัง"),body);
   if(GUI.Button(new Rect(510,620,760,45),"ปิดสมุด / J",button))ToggleJournal();return;
  }
  if(game.stage==0||game.player.paused||game.completed)return;
  GUI.Box(new Rect(24,172,330,105),"");var next=puzzles.FirstOrDefault(p=>!p.optional&&!p.solved)??puzzles.FirstOrDefault(p=>p.optional&&!p.collected);
  string navigation="ระบบหลักพร้อมแล้ว";
  if(next!=null){Vector3 toward=next.approach-game.player.transform.position;Vector3 local=Quaternion.Inverse(Quaternion.Euler(0,game.player.yaw,0))*toward;string arrow=Mathf.Abs(local.x)>Mathf.Abs(local.z)?(local.x<0?"←":"→"):(local.z<0?"↓":"↑");navigation=arrow+" "+next.title+" • "+Mathf.RoundToInt(new Vector2(toward.x,toward.z).magnitude)+" ม.";}
  GUI.Label(new Rect(38,180,305,90),"ระบบพื้นที่ใหม่ "+RequiredSolved+" / "+RequiredCount+"   • J บันทึก\n"+chapter+"\n"+navigation,small);
  if(game.player.overview){foreach(var p in puzzles){var at=game.player.view.WorldToScreenPoint(p.approach+Vector3.up);if(at.z<=0)continue;float x=at.x/Screen.width*1440,y=900-at.y/Screen.height*900;GUI.Box(new Rect(x-95,y-18,190,42),"");GUI.Label(new Rect(x-88,y-13,176,36),(p.solved?"● ":p.optional?"☆ ":"○ ")+p.title,small);}return;}
  var nearest=Nearest();if(nearest){GUI.Box(new Rect(440,305,560,67),"");GUI.Label(new Rect(457,312,526,60),"F  "+nearest.label+"\n"+puzzles[nearest.puzzle].title+" • H คำใบ้",body);}
 }
}
}
