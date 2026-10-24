#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
[InitializeOnLoad]public static class BTFunPlaytest {
    static BTFunPrototypeGame g;static bool running,nature,oldBackground,resetToYoung,pumpCleared,controlApproached;static int step;static float since,castAt;static double started;static List<string> log=new List<string>();public static string Status="Idle";
    static BTFunPlaytest(){EditorApplication.update+=Tick;}
    public static void Start(){g=Object.FindFirstObjectByType<BTFunPrototypeGame>();if(!EditorApplication.isPlaying||!g)throw new Exception("Play FunPrototype first");g.ResetTraining();nature=false;pumpCleared=controlApproached=false;step=0;since=Time.time;started=EditorApplication.timeSinceStartup;oldBackground=Application.runInBackground;Application.runInBackground=true;running=true;log.Clear();g.player.inputOverride=Input;Status="Running Bridge route";}
    static void Note(string s){log.Add((EditorApplication.timeSinceStartup-started).ToString("F2")+"s | "+s);}
    static void Next(string s){Note("PASS "+(nature?"Nature":"Bridge")+" "+step+" "+s);step++;since=Time.time;castAt=Time.time+.65f;}
    static void Finish(bool ok,string s){Status=(ok?"PASS ":"FAIL ")+s;Note(Status);running=false;if(g)g.player.inputOverride=null;Application.runInBackground=oldBackground;File.WriteAllText("Assets/_BorrowedTime/Evidence/FunPrototypePlaytest.txt",string.Join("\n",log));Debug.Log(Status);}
    static void Tick(){if(!running)return;if(!EditorApplication.isPlaying){Finish(false,"Play Mode ended");return;}if(g.completed){if(!g.waterRestored||!g.bloom.activeSelf||g.tree.pollutionStress||g.tree.age!=1||!g.crossed){Finish(false,"Final state mismatch");return;}Note("PASS actual route crossed both directions; polluted health retained temporal age; lasting restoration after source stopped");if(!nature){g.ResetTraining();nature=true;resetToYoung=pumpCleared=controlApproached=false;step=0;since=Time.time;g.player.inputOverride=Input;Status="Running Nature route";}else Finish(true,"Both real controller crossing routes and ecological cause/effect verified");return;}if(Time.time-since>35)Finish(false,"Timeout step="+step+" pos="+g.player.transform.position+" target="+(g.player.target?g.player.target.displayName:"none"));}
    static bool Move(Vector3 p,ref BTPlayer.FrameInput f){var d=p-g.player.transform.position;d.y=0;if(d.magnitude<.3f)return true;var q=Quaternion.Inverse(Quaternion.Euler(0,g.player.yaw,0))*d.normalized;f.move=new Vector2(q.x,q.z);return false;}
    static bool Aim(BTTimeTarget t,ref BTPlayer.FrameInput f){var d=t.aimPoint.position-g.player.view.transform.position;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;f.look=new Vector2(Mathf.DeltaAngle(g.player.yaw,yaw)*.5f/.14f,-(pitch-g.player.pitch)*.5f/.12f);f.aim=true;return g.player.target==t;}
    static void Cast(BTTimeTarget t,int goal,ref BTPlayer.FrameInput f){if(Aim(t,ref f)&&Time.time>castAt){f.scroll=t.age>goal?-120:t.age<goal?120:0;castAt=Time.time+.7f;}}
    static BTPlayer.FrameInput Input(){var f=new BTPlayer.FrameInput();switch(step){
        case 0:if(Move(new Vector3(-8,0,-5.3f),ref f))Next("approached living source");break;
        case 1:var source=Object.FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None);foreach(var t in source)if(t.kind==TargetKind.Plant){if(Aim(t,ref f))f.absorb=true;}if(g.player.energy>=95)Next("nonlethal refill");break;
        case 2:if(Move(new Vector3(-6,0,.5f),ref f))Next("approached park");break;
        case 3:Cast(g.tree,1,ref f);if(g.tree.age==1)Next("rewound tree one state");break;
        case 4:if(g.relapsed&&g.tree.pollutionStress&&g.tree.age==1)Next("pollution stress changed health visual without changing age");break;
        case 5:if(Move(new Vector3(-3.5f,0,nature?5.1f:-2.4f),ref f))Next("selected crossing approach");break;
        case 6:Cast(nature?g.vine:g.bridge,nature?1:0,ref f);if((nature?g.vine.age==1:g.bridge.age==0))Next("opened selected crossing through real wheel input");break;
        case 7:if(Move(new Vector3(-1,0,nature?4:-1),ref f))Next("on crossing collider");break;
        case 8:if(g.player.StandingOn(nature?g.vine:g.bridge)){float before=g.player.energy;var t=nature?g.vine:g.bridge;if(t.TryStep(1,g.player,out string why)||Mathf.Abs(g.player.energy-before)>.01f){Finish(false,"Support safety lock failed");break;}Next("support removal blocked without spending energy");}break;
        case 9:if(Move(new Vector3(6.5f,0,nature?4:-1),ref f))Next("crossed selected route on foot");break;
        case 10:if(!controlApproached){if(Move(new Vector3(6.5f,0,-6.3f),ref f))controlApproached=true;}else if(Move(new Vector3(10,0,-6.3f),ref f))Next("reached water controls");break;
        case 11:Cast(g.pump,1,ref f);if(g.pump.age==1)Next("repaired pump");break;
        case 12:if(Move(new Vector3(12,0,-6),ref f))Next("reached valve");break;
        case 13:f.interact=true;if(g.waterRestored)Next("F stopped toxic feed");break;
        case 14:if(!pumpCleared){if(Move(new Vector3(8,0,-6),ref f))pumpCleared=true;}else if(Move(new Vector3(6.5f,0,nature?4:-1),ref f))Next("returned to same route");break;
        case 15:if(Move(new Vector3(-3.5f,0,nature?4:-1),ref f))Next("crossed back using same collider");break;
        case 16:if(Move(new Vector3(-6,0,.5f),ref f))Next("returned to sick tree");break;
        case 17:Cast(g.tree,resetToYoung?1:0,ref f);if(g.tree.age==0)resetToYoung=true;if(resetToYoung&&g.tree.age==1)Next("used both powers to restore roots after clean water");break;
        case 18:if(g.waterRestored && g.tree.age==1)Next("restoration persisted");break;
    }return f;}
}
}
#endif
