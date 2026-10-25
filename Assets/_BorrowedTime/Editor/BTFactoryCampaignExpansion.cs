#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using BorrowedTime;
public static class BTFactoryCampaignExpansion {
 class Step {public string label;public Vector3 point;public BTTimeTarget target;public int age,kind;public BTExpansionInteractable control;public Action verify;}
 class Route {
  public BTTrainingGame game;public BTExpansionWorld world;public List<Step> steps=new List<Step>();public int index;public float since,castAt;public int pulseFrame=-1;
  public Queue<Vector3> path;public Vector3 pathGoal;public Dictionary<Vector2Int,bool> cells=new Dictionary<Vector2Int,bool>();
  public HashSet<string> evidence=new HashSet<string>();
 }
 static Dictionary<int,Route> routes=new Dictionary<int,Route>();
 public static readonly List<string> Log=new List<string>();
 public static Exception Failure;
 public static void Clear(){routes.Clear();Log.Clear();Failure=null;}
 static BTExpansionInteractable C(BTExpansionPuzzle p,BTExpansionRole role,int slot=0)=>p.controls.First(c=>c.role==role&&c.slot==slot);
 static void Add(Route r,string text,Vector3 point){r.steps.Add(new Step{label=text,point=point,kind=0});}
 static void Press(Route r,string text,BTExpansionInteractable control,Action verify=null){r.steps.Add(new Step{label=text,point=control.transform.position+new Vector3(0,0,-2.1f),control=control,kind=1,verify=verify});}
 static void Absorb(Route r,BTTimeTarget target){r.steps.Add(new Step{label="renewable living energy",point=target.transform.position+new Vector3(0,0,-2.6f),target=target,kind=2});}
 static Route Make(BTTrainingGame game){
  var r=new Route{game=game,world=game.GetComponent<BTExpansionWorld>(),since=Time.time};Vector3 home=game.player.transform.position;
  Vector3 connection=game is BTFactoryGame?new Vector3(0,0,-25):game is BTCityGame?new Vector3(-23,0,-19):new Vector3(-16,0,-12);
  Vector3 entry=connection+Vector3.back*(game is BTCityGame?11:10);
  Add(r,"return to expansion entrance",connection);Add(r,"walk supported connection",entry);
  var a=r.world.puzzles[0];
  Press(r,"reject unbalanced temporal system",C(a,BTExpansionRole.Submit),()=>{if(a.solved)throw new Exception("Unbalanced device accepted");});
  Press(r,"read physical clue / journal modal",C(a,BTExpansionRole.Clue),()=>{if(!r.evidence.Contains("journal")||game.player.paused||!a.clueRead)throw new Exception("Journal pause/close path failed");});
  var source=UnityEngine.Object.FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None).Where(t=>t.kind==TargetKind.Plant).OrderBy(t=>Vector3.Distance(t.transform.position,a.approach)).First();
  Absorb(r,source);
  if(game is BTFactoryGame){
   var extra=UnityEngine.Object.FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None).Where(t=>t.kind==TargetKind.Plant&&t!=source&&t.transform.position.z< -35).OrderBy(t=>Vector3.Distance(t.transform.position,a.approach)).First();Absorb(r,extra);
   r.steps.Add(new Step{label="wrong time choice spends 15",point=a.temporal[0].transform.position+new Vector3(-2,0,-3),target=a.temporal[0],kind=3,age=1});
   r.steps.Add(new Step{label="undo wrong time choice refunds 15",point=a.temporal[0].transform.position+new Vector3(-2,0,-3),target=a.temporal[0],kind=3,age=2});
  }
  for(int i=0;i<a.temporal.Length;i++)r.steps.Add(new Step{label="set temporal age "+i,point=a.temporal[i].transform.position+new Vector3(-2,0,-3),target=a.temporal[i],kind=3,age=a.ages[i]});
  for(int i=0;i<3;i++)for(int j=0;j<a.goals[i];j++)Press(r,"route water "+i,C(a,BTExpansionRole.Valve,i));
  Press(r,"latch temporal + water restoration",C(a,BTExpansionRole.Submit),()=>{if(!a.solved)throw new Exception("Temporal/water puzzle did not solve: valves="+string.Join(",",a.valves)+" ages="+string.Join(",",a.temporal.Select(t=>t.age))+" last="+r.world.LastInteraction+" feedback="+game.player.Feedback);});
  var b=r.world.puzzles[1];
  Press(r,"observe linked switch",C(b,BTExpansionRole.Valve),()=>{int neighbour=game is BTFactoryGame?2:1;if(b.valves[0]!=1||b.valves[neighbour]!=1)throw new Exception("Switch did not affect its neighbour");});
  Press(r,"reset partial circuit",C(b,BTExpansionRole.Reset),()=>{if(b.valves.Any(v=>v!=0)||a.solved==false)throw new Exception("Reset corrupted neighbouring puzzle");});
  int[] solution=null;
  for(int x=0;x<3;x++)for(int y=0;y<3;y++)for(int z=0;z<3;z++){var state=game is BTFactoryGame?new[]{(x+y)%3,(y+z)%3,(z+x)%3}:new[]{(x+z)%3,(y+x)%3,(z+y)%3};if(state.SequenceEqual(b.goals))solution=new[]{x,y,z};}
  if(solution==null)throw new Exception("Unsolvable linked circuit");
  for(int i=0;i<3;i++)for(int j=0;j<solution[i];j++)Press(r,"solve linked circuit "+i,C(b,BTExpansionRole.Valve,i));
  Press(r,"latch linked circuit",C(b,BTExpansionRole.Submit),()=>{if(!b.solved)throw new Exception("Linked circuit did not solve");});
  var secret=r.world.puzzles[2];
  Press(r,"secret begins correctly",C(secret,BTExpansionRole.Chime,secret.sequence[0]));
  Press(r,"wrong chime recovers without item loss",C(secret,BTExpansionRole.Chime,(secret.sequence[1]+1)%3),()=>{if(secret.cursor!=0||secret.collected)throw new Exception("Sequence failure did not recover");});
  foreach(int note in secret.sequence)Press(r,"reconstruct seasonal memory "+note,C(secret,BTExpansionRole.Chime,note));
  Press(r,"open secret garden",C(secret,BTExpansionRole.Submit),()=>{if(!secret.solved)throw new Exception("Secret garden did not open");r.cells.Clear();});
  r.steps.Add(new Step{label="wait for physical door clearance",kind=4});
  Press(r,"collect optional memory beyond open gate",C(secret,BTExpansionRole.Reward),()=>{if(!secret.collected||!secret.clueRead)throw new Exception("Secret reward not recorded");});
  Absorb(r,source);Add(r,"return through supported entrance",entry);Add(r,"return to original main route",connection);Add(r,"resume original puzzle approach",home);
  return r;
 }
 public static bool Done(BTTrainingGame game){return !game.GetComponent<BTExpansionWorld>()||(routes.TryGetValue(game.GetInstanceID(),out var r)&&r.index>=r.steps.Count);}
 static bool Free(Route r,Vector2Int cell){
  if(r.cells.TryGetValue(cell,out bool value))return value;
  var p=new Vector3(cell.x,0,cell.y);bool ground=Physics.Raycast(p+Vector3.up*1.7f,Vector3.down,out var hit,2.15f,~(1<<2),QueryTriggerInteraction.Ignore)&&hit.point.y<.36f&&hit.point.y>-.2f;
  bool free=ground&&!Physics.CheckCapsule(p+Vector3.up*(hit.point.y+.43f),p+Vector3.up*(hit.point.y+1.58f),.38f,~(1<<2),QueryTriggerInteraction.Ignore);r.cells[cell]=free;return free;
 }
 static Vector2Int Cell(Vector3 p)=>new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.z));
 static Queue<Vector3> Path(Route r,Vector3 start,Vector3 goal){
  var a=Cell(start);var b=Cell(goal);var frontier=new List<Vector2Int>{a};var came=new Dictionary<Vector2Int,Vector2Int>();var cost=new Dictionary<Vector2Int,float>{{a,0}};int visits=0;Vector2Int found=a;
  while(frontier.Count>0&&visits++<16000){int best=0;float score=float.MaxValue;for(int i=0;i<frontier.Count;i++){float v=cost[frontier[i]]+Vector2Int.Distance(frontier[i],b);if(v<score){score=v;best=i;}}
   var n=frontier[best];frontier.RemoveAt(best);if(Vector2Int.Distance(n,b)<1.5f){found=n;break;}
   foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var next=n+d;if(next.x< -72||next.x>45||next.y< -82||next.y>35||!Free(r,next))continue;float c=cost[n]+1;if(cost.ContainsKey(next)&&cost[next]<=c)continue;cost[next]=c;came[next]=n;if(!frontier.Contains(next))frontier.Add(next);}
  }
  if(found==a&&Vector2Int.Distance(a,b)>2)throw new Exception("No collision-safe route "+start+" -> "+goal+" step="+r.steps[r.index].label);
  var points=new List<Vector3>{goal};var node=found;while(node!=a){points.Add(new Vector3(node.x,0,node.y));if(!came.ContainsKey(node))break;node=came[node];}points.Reverse();return new Queue<Vector3>(points);
 }
 static bool Move(Route r,Vector3 point,ref BTPlayer.FrameInput f){
  var p=r.game.player;Vector3 delta=point-p.transform.position;delta.y=0;if(delta.magnitude<.40f){r.path=null;return true;}
  if(r.path==null||Vector3.Distance(r.pathGoal,point)>.05f){r.pathGoal=point;r.path=Path(r,p.transform.position,point);}
  while(r.path.Count>1){var d=r.path.Peek()-p.transform.position;d.y=0;if(d.magnitude>.35f)break;r.path.Dequeue();}
  Vector3 desired=r.path.Count>0?r.path.Peek():point;Vector3 diff=desired-p.transform.position;diff.y=0;var local=Quaternion.Inverse(Quaternion.Euler(0,p.yaw,0))*diff.normalized;f.move=new Vector2(local.x,local.z);f.sprint=true;return false;
 }
 static void Aim(Route r,BTTimeTarget target,ref BTPlayer.FrameInput f){
  var p=r.game.player;Vector3 d=target.aimPoint.position-p.view.transform.position;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
  f.look=new Vector2(Mathf.DeltaAngle(p.yaw,yaw)*.55f/.14f,-(pitch-p.pitch)*.55f/.12f);f.aim=false;
 }
 public static BTPlayer.FrameInput Input(BTTrainingGame game){try{return Drive(game);}catch(Exception e){Failure=e;return default;}}
 static BTPlayer.FrameInput Drive(BTTrainingGame game){
  if(!routes.TryGetValue(game.GetInstanceID(),out var r)){r=Make(game);routes[game.GetInstanceID()]=r;Log.Add("BEGIN "+game.gameObject.scene.name+" expanded route, controller position="+game.player.transform.position);}
  var f=new BTPlayer.FrameInput();if(r.index>=r.steps.Count)return f;var s=r.steps[r.index];
  if(Time.time-r.since>45)throw new Exception("Expanded route timeout: "+s.label+" position="+game.player.transform.position+" target="+(game.player.target?game.player.target.name:"none")+" energy="+game.player.energy+" feedback="+game.player.Feedback);
  bool done=false;
  if(s.kind==4)done=Time.time-r.since>2.2f;
  else if(Move(r,s.point,ref f)){
   if(s.kind==0)done=true;
   else if(s.kind==1){if(r.pulseFrame<0){f.interact=true;r.pulseFrame=Time.frameCount;
     if(s.control.role==BTExpansionRole.Clue){double due=EditorApplication.timeSinceStartup+.25;EditorApplication.CallbackFunction close=null;close=()=>{if(EditorApplication.timeSinceStartup<due)return;EditorApplication.update-=close;if(!r.world.journalOpen||!game.player.paused){Failure=new Exception("Journal did not pause player");return;}r.evidence.Add("journal");r.world.ToggleJournal();};EditorApplication.update+=close;}
    }else if(Time.frameCount>r.pulseFrame+1){s.verify?.Invoke();done=true;}}
   else {Aim(r,s.target,ref f);if(s.kind==2){if(game.player.target==s.target)f.absorb=true;done=game.player.energy>=99.9f || game is BTFactoryGame && s.target.life<=s.target.minimumLife+.05f;if(done&&(s.target.life<s.target.minimumLife-.01f||s.target.age!=s.target.initialAge))throw new Exception("Living energy floor / age changed during absorption");}
    else {done=s.target.age==s.age;if(!done&&game.player.target==s.target&&Time.time>r.castAt){f.scroll=s.target.age>s.age?-120:120;r.castAt=Time.time+.65f;}}
   }
  }
  if(done){Log.Add("PASS "+game.gameObject.scene.name+" / "+r.index+" "+s.label);File.WriteAllLines("Captures/TrainingComfort/Factory/ExpansionRoute.txt",Log);r.index++;r.since=Time.time;r.pulseFrame=-1;r.path=null;}
  return f;
 }
}
#endif
