using UnityEngine;
namespace OopsPlanet {
// Open hall commissioning test: walking around the outside of the channel is exploration,
// not proof that its ferry, bridge or girder launch has been repaired/tested.
public class FactoryCrossingCondition:MonoBehaviour,IPuzzleCondition {
 public BlobWorld world;public ShuttleRaft raft;public PoweredLift bridge;public Bounds channel,northBank,girderTip;public bool reached;public string route="";float lastGirder=-100;
 public bool IsMet=>reached;public float Progress01=>reached?1:route!=""?.5f:0;
 void Update(){if(reached||!world||!world.player||world.player.paused)return;var p=world.player;var feet=p.transform.position;
  if(girderTip.Contains(feet)&&p.Grounded)lastGirder=Time.time;
  if(channel.Contains(feet)){
   if(raft&&raft.state==ShuttleRaft.State.Moving&&new Bounds(raft.transform.position+Vector3.up*.7f,raft.standSize).Contains(feet+Vector3.up*.1f))route="raft";
   if(bridge&&bridge.motor&&bridge.motor.Powered&&Mathf.Abs(feet.x-bridge.platform.position.x)<1.2f&&feet.y>-.4f&&p.Grounded)route="bridge";
   if(p.bounceAirborne&&Time.time-lastGirder<2.5f)route="girder";
  }
  if(route!=""&&northBank.Contains(feet+Vector3.up*.3f))reached=true;
 }
 public void ResetCondition(){reached=false;route="";lastGirder=-100;}
}
}
