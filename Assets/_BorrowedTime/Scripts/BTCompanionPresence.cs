using UnityEngine;
using BorrowedTime.Characters;
namespace BorrowedTime {
[DefaultExecutionOrder(70)]
public class BTCompanionPresence : MonoBehaviour {
 public BTTrainingGame game;
 void Start(){EnsureVisible();}
 void EnsureVisible(){
  var adapter=game.player.GetComponent<BTGameplayCharacter>();if(!adapter)return;
  foreach(var model in new[]{adapter.hero,adapter.robo})if(model){for(Transform t=model.transform;t;t=t.parent){t.gameObject.SetActive(true);if(t==game.player.transform||t==game.robo)break;}if(model.animator){model.animator.enabled=true;model.animator.speed=1;model.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;model.animator.applyRootMotion=false;}foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.enabled=true;}
 }
 void LateUpdate(){
  if(!game||!game.player||game.player.paused||game.completed)return;
  var adapter=game.player.GetComponent<BTGameplayCharacter>();if(!adapter||!adapter.robo)return;
  if(!game.robo)game.robo=adapter.robo.transform.parent;
  if(!game.robo.gameObject.activeInHierarchy)EnsureVisible();
  Vector3 anchor=game.player.transform.position+Vector3.up*1.55f;
  Vector3 desired=game.player.transform.position+Quaternion.Euler(0,game.player.yaw,0)*new Vector3(1.25f,1.95f,-.35f)+Vector3.up*Mathf.Sin(Time.time*2.5f)*.08f;
  if(Physics.SphereCast(anchor,.23f,(desired-anchor).normalized,out var hit,Vector3.Distance(anchor,desired),~(1<<2),QueryTriggerInteraction.Ignore))desired=anchor+(desired-anchor).normalized*Mathf.Max(.3f,hit.distance-.15f);
  game.robo.position=Vector3.Distance(game.robo.position,desired)>5?desired:Vector3.Lerp(game.robo.position,desired,Time.deltaTime*8);
  game.robo.rotation=Quaternion.Slerp(game.robo.rotation,Quaternion.Euler(0,game.player.yaw+180,0),Time.deltaTime*6);
 }
}}
