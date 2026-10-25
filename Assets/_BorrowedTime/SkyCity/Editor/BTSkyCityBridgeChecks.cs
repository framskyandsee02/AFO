using System;
using System.Text;
using UnityEngine;
using UnityEditor;
namespace BorrowedTime {
public static class BTSkyCityBridgeChecks {
 [MenuItem("BORROWED TIME/Checks/Sliding bridges")]
 public static void RunMenu(){Debug.Log(Run());}
 static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
 public static string Run(){
  var game=UnityEngine.Object.FindFirstObjectByType<BTSkyCityGame>();
  Require(game!=null,"Sky City game is missing");
  var log=new StringBuilder();
  foreach(var root in new[]{game.bridgeA,game.bridgeB}){
   var bridge=root.GetComponent<BTSkyCitySlidingBridge>();
   Require(bridge!=null,root.name+": sliding bridge missing");
   var position=root.position;float previous=float.NegativeInfinity;
   try {
    foreach(float p in new[]{0f,.25f,.5f,.75f,1f}){
     bridge.SetProgress(p);Physics.SyncTransforms();
     Require((root.position-position).sqrMagnitude<.000001f,"Bridge root moved vertically");
     float reach=bridge.segments[4].localPosition.z;
     Require(reach>=previous,"Bridge tip moved backwards during extension");previous=reach;
     foreach(var segment in bridge.segments)Require(Mathf.Abs(segment.localPosition.y)<.0001f,"Section changed height");
     foreach(var c in bridge.movingColliders)Require(c.enabled==(p>=.999f),"Walkable before fully extended");
     foreach(var c in bridge.gateColliders)Require(c.enabled==(p<.999f),"Safety gate state incorrect");
     if(p==0)Require(reach<0,"Bridge was left across the gap while locked");
     if(p==1){
      float expected=bridge.step*4.5f+.1f;
      Require(Mathf.Abs(reach-expected)<.001f,"Bridge does not reach destination");
      for(float z=.5f;z<bridge.step*5;z+=.2f){
       RaycastHit hit;
       var point=root.TransformPoint(new Vector3(0,2,z));
       Require(Physics.Raycast(point,Vector3.down,out hit,3,~(1<<2),QueryTriggerInteraction.Ignore),"Gap in walking surface at "+z);
       Require(hit.point.y>.1f&&hit.point.y<.6f,"Wrong deck elevation at "+z);
      }
     }
    }
    bridge.SetProgress(.75f);
    foreach(var c in bridge.movingColliders)Require(!c.enabled,"Retraction kept stale deck colliders");
    log.AppendLine(root.name+": PASS level motion, staged reach, lock, retract, continuous floor");
   }finally{bridge.SetProgress(0);Physics.SyncTransforms();}
  }
  return log.ToString();
 }
}
}
