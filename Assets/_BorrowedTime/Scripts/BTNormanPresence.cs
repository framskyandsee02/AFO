using UnityEngine;
namespace BorrowedTime {
// Small, readable idle gestures for the scientist, separate from quest logic.
public class BTNormanPresence : MonoBehaviour {
 public Transform head,leftEye,rightEye,body;
 Vector3 headRest,bodyRest,leftRest,rightRest;
 Quaternion headRotation;
 BTTrainingGame game;
 int leftAxis,rightAxis;
 float nextBlink,blinkStart=-100;
 void Start(){
  game=FindFirstObjectByType<BTTrainingGame>();
  if(head){headRest=head.localPosition;headRotation=head.localRotation;}
  if(body)bodyRest=body.localPosition;
  if(leftEye){leftRest=leftEye.localScale;leftAxis=VerticalAxis(leftEye);}if(rightEye){rightRest=rightEye.localScale;rightAxis=VerticalAxis(rightEye);}
  nextBlink=Time.time+3.4f;
 }
 void LateUpdate(){
  if(game&&game.player&&game.player.paused)return;
  float t=Time.time;
  if(head){head.localPosition=headRest+Vector3.up*(Mathf.Sin(t*1.3f)*.002f);head.localRotation=headRotation*Quaternion.Euler(Mathf.Sin(t*.75f)*1.0f,0,Mathf.Sin(t*.36f)*2.0f);}
  if(body)body.localPosition=bodyRest+Vector3.up*(Mathf.Sin(t*1.3f)*.002f);
  if(t>=nextBlink){blinkStart=t;nextBlink=t+Random.Range(3.2f,6.0f);}
  float phase=(t-blinkStart)/.18f,scale=phase>=0&&phase<=1?Mathf.Max(.05f,Mathf.Abs(phase*2-1)):1;
  if(leftEye){var s=leftRest;s[leftAxis]*=scale;leftEye.localScale=s;}
  if(rightEye){var s=rightRest;s[rightAxis]*=scale;rightEye.localScale=s;}
 }
 static int VerticalAxis(Transform t){float best=-1;int axis=0;for(int i=0;i<3;i++){Vector3 v=i==0?Vector3.right:i==1?Vector3.up:Vector3.forward;float alignment=Mathf.Abs(Vector3.Dot(t.TransformDirection(v),Vector3.up));if(alignment>best){best=alignment;axis=i;}}return axis;}
}
}
