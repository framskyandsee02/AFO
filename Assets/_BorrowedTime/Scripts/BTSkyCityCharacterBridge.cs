using UnityEngine;
using BorrowedTime.Characters;
namespace BorrowedTime {
[DefaultExecutionOrder(80)]
public class BTSkyCityCharacterBridge : MonoBehaviour {
 public BTSkyCityGame game;public BTCharacterModel hero,robo;public Transform roboRoot;
 Vector3 previous;float poseUntil;string pose="";
 void Start(){previous=game.player.position;foreach(var m in new[]{hero,robo}){m.autoReturn=false;m.animator.enabled=true;m.animator.speed=1;m.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;m.animator.applyRootMotion=false;}hero.PlayPose("Idle");robo.PlayPose("Hover");}
 void LateUpdate(){
  if(!game||game.previewOnly)return;float dt=Time.deltaTime;Vector3 delta=game.player.position-previous;previous=game.player.position;float speed=new Vector2(delta.x,delta.z).magnitude/Mathf.Max(dt,.0001f);
  hero.animator.speed=robo.animator.speed=game.Paused?0:1;if(game.Paused)return;
  bool transferring=game.FocusedNode>=0&&Time.time-game.LastTransferTime<.3f;
  if(transferring){game.visual.rotation=Quaternion.Slerp(game.visual.rotation,Quaternion.Euler(0,game.ViewYaw,0),dt*15);pose=game.LastTransferDirection<0?"Absorb":"Rewind";poseUntil=Time.time+.25f;}
  if(Time.time<poseUntil){if(hero.currentPose!=pose)hero.PlayPose(pose);}
  else if(!game.controller.isGrounded&&!(delta.y<=0&&Physics.SphereCast(game.player.position+Vector3.up*.3f,.2f,Vector3.down,out _,.25f,~(1<<2),QueryTriggerInteraction.Ignore))&&Mathf.Abs(delta.y)>dt*.8f){string air=delta.y>0?"Jump":"Fall";if(hero.currentPose!=air)hero.PlayPose(air);}
  else hero.SetLocomotionSpeed(speed>.2f?(game.CurrentInput.sprint?2:1):0);
  string rp=transferring?"Scan":speed>.2f?"Follow":"Hover";if(robo.currentPose!=rp)robo.PlayPose(rp);
  Vector3 p=game.player.position;var desired=p+Quaternion.Euler(0,game.ViewYaw,0)*new Vector3(1.15f,1.9f,-.35f)+Vector3.up*Mathf.Sin(Time.time*2.6f)*.10f;var anchor=p+Vector3.up*1.6f;
  if(Physics.SphereCast(anchor,.22f,(desired-anchor).normalized,out var hit,Vector3.Distance(anchor,desired),~(1<<2),QueryTriggerInteraction.Ignore))desired=anchor+(desired-anchor).normalized*Mathf.Max(.3f,hit.distance-.15f);
  roboRoot.position=Vector3.Distance(roboRoot.position,desired)>5?desired:Vector3.Lerp(roboRoot.position,desired,dt*7);roboRoot.rotation=Quaternion.Slerp(roboRoot.rotation,Quaternion.Euler(0,game.ViewYaw+180,0),dt*6);
 }
}}
