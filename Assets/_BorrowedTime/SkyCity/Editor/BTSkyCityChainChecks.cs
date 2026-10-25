using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
namespace BorrowedTime {
public static class BTSkyCityChainChecks {
 static void Require(bool pass,string message){if(!pass)throw new Exception(message);}
 [MenuItem("BORROWED TIME/Checks/Causal chain and age transfer")]
 public static void Menu(){Debug.Log(Run());}
 public static string Run(){
  var g=UnityEngine.Object.FindFirstObjectByType<BTSkyCityGame>();Require(g!=null,"Missing city game");var p=g.puzzles;
  float[] values=g.nodes.Select(n=>n.value).ToArray();float energy=g.energy;bool test=p.testMode,w=p.waterAccess,c=p.clockAccess,d=p.commissioned;var solved=(bool[])p.solved.Clone();var report=new StringBuilder();var intro=typeof(BTSkyCityGame).GetField("introOpened",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);bool introBefore=(bool)intro.GetValue(g);
  try{
   p.testMode=true;p.waterAccess=p.clockAccess=p.commissioned=false;foreach(var n in g.nodes)n.value=n.Initial;g.energy=0;
   intro.SetValue(g,true);Require(g.hints.NearestDistrict()==1&&p.NextNode()==2,"Guidance ignored intro access latch");p.commissioned=true;Require(g.hints.NearestDistrict()==-1&&p.NextNode()==-1,"Commissioned factory guidance pointed back to repairs");p.commissioned=false;intro.SetValue(g,introBefore);report.AppendLine("PASS: guidance respects intro latch and commissioned factory");
   float left;float f=BTSkyCityGame.MoveFreshness(1,-1,.4f,100,100,out left);Require(Mathf.Abs(f-.6f)<.00001f&&left==100,"Full meter prevented ageing");
   f=BTSkyCityGame.MoveFreshness(.3f,1,1,12,100,out left);Require(Mathf.Abs(f-.42f)<.00001f&&left<.0001f,"Empty meter allowed free repair");
   f=BTSkyCityGame.MoveFreshness(.37f,0,1,50,100,out left);Require(f==.37f&&left==50,"Idle changed state");
   f=BTSkyCityGame.MoveFreshness(.8f,-1,.2f,20,100,out left);f=BTSkyCityGame.MoveFreshness(f,1,.2f,left,100,out left);Require(Mathf.Abs(f-.8f)<.0001f&&Mathf.Abs(left-20)<.001f,"Reverse did not conserve state/energy");report.AppendLine("PASS: cap, empty, idle and reversal");
   string reason;float before=g.nodes[5].value;p.Transfer(5,1,1,out reason);Require(g.nodes[5].value==before&&reason!="","Skipped water gate");before=g.nodes[10].value;p.Transfer(10,1,1,out reason);Require(g.nodes[10].value==before&&reason!="","Skipped clock gate");
   p.Transfer(2,-1,1,out reason);p.Transfer(8,1,1,out reason);p.Transfer(3,1,1,out reason);Require(p.WaterReady&&p.waterAccess&&!p.ClockReady,"Water did not unlock gears");Require(g.energy<.01f,"P1 resource budget inconsistent");report.AppendLine("PASS: P1 supplied by its obstruction and opens P2");
   p.Transfer(4,-1,1,out reason);p.Transfer(5,1,1,out reason);Require(p.ClockReady&&p.clockAccess&&!p.ReadyToLaunch,"P2 did not unlock control bay");report.AppendLine("PASS: P2 needs P1 and opens P3");
   p.Transfer(11,-1,1,out reason);p.Transfer(6,-1,1,out reason);p.Transfer(10,1,1,out reason);Require(g.nodes[10].value<.001f&&reason!="","Head repaired without support");p.Transfer(6,1,1,out reason);Require(g.nodes[6].value>=.999f&&g.energy<.01f,"Wrong-order support restoration bypassed cost");
   for(int i=12;i<g.nodes.Length&&g.energy<99.999f;i++)if(g.nodes[i].ambient)g.ApplyTime(i,-1,1);Require(g.energy>99.99f,"Ambient resources cannot recover wrong-order sequence");
   p.Transfer(10,1,1,out reason);p.Transfer(6,-1,1,out reason);Require(p.ReadyToLaunch,"P3 did not release bridge");Require(!g.FactoryDocked,"Door opened before bridge docked");
   g.nodes[3].value=0;p.Evaluate();Require(!p.ReadyToLaunch,"P3 ignored upstream water power");Require(p.waterAccess&&p.clockAccess,"Loss of power trapped player behind access gates");g.nodes[3].value=1;p.Evaluate();Require(p.ReadyToLaunch,"Restoring upstream power did not recover chain");report.AppendLine("PASS: P3 requires BOTH upstream systems; wrong order recoverable; door waits for docking");
   int crate=Array.FindIndex(g.nodes,n=>n.id=="CityCrate_0");Require(crate>=12,"Optional crate missing");var age=g.nodes[crate].model.GetComponent<BTSkyCityAgeVisual>();g.nodes[crate].value=1;age.Apply();var fresh=age.fragments.Select(t=>t.localPosition).ToArray();var scale=g.nodes[crate].model.localScale;
   g.nodes[crate].value=.95f;age.Apply();Require((age.fragments[age.fragments.Length-1].localPosition-fresh[fresh.Length-1]).sqrMagnitude>.00000001f,"All geometry finished before repair endpoint");
   g.nodes[crate].value=.4f;age.Apply();Require(age.fragments.Where((t,i)=>(t.localPosition-fresh[i]).sqrMagnitude>.001f).Any(),"No per-part age animation");var mid=age.fragments.Select(t=>t.localPosition).ToArray();age.Apply();Require(age.fragments.Where((t,i)=>(t.localPosition-mid[i]).sqrMagnitude>.000001f).Count()==0,"Partial age drifted while idle");
   g.nodes[crate].value=1;age.Apply();var fixedAim=g.AimPoint(crate);g.nodes[crate].value=0;age.Apply();Require(age.solids.All(x=>!x.enabled),"Decayed wood still blocks walking");Require(g.nodes[crate].model.GetComponent<BoxCollider>().enabled,"Decayed object cannot be restored");Physics.SyncTransforms();Require(g.nodes[crate].model.GetComponent<BoxCollider>().bounds.Contains(fixedAim),"Ageing lost the original aim point before completion");g.nodes[crate].value=1;age.Apply();Require(age.fragments.Where((t,i)=>(t.localPosition-fresh[i]).sqrMagnitude>.000001f).Count()==0,"Repair did not reconstruct original shape");Require(g.nodes[crate].model.localScale==scale,"Entire target was squashed");report.AppendLine("PASS: per-part animation, frozen midpoint, exact reconstruction, decay collision, stable aim");
   Require(g.nodes.Count(n=>n.ambient)>=30,"Too few optional world targets");Require(g.nodes.Select(n=>n.id).Distinct().Count()==g.nodes.Length,"Duplicate persistent node IDs");Require(g.nodes.Where(n=>n.targetable).All(n=>n.model&&n.model.GetComponent<BoxCollider>()),"Target collider missing");report.AppendLine("PASS: "+g.nodes.Count(n=>n.ambient)+" optional objects, unique IDs, target colliders");
   return report.ToString();
  }finally{for(int i=0;i<values.Length;i++)g.nodes[i].value=values[i];g.energy=energy;p.testMode=test;p.waterAccess=w;p.clockAccess=c;p.commissioned=d;p.solved=solved;intro.SetValue(g,introBefore);foreach(var age in UnityEngine.Object.FindObjectsOfType<BTSkyCityAgeVisual>())age.Apply();}
 }
}
}
