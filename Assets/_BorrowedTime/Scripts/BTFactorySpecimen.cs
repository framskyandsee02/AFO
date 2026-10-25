using UnityEngine;
namespace BorrowedTime {
public class BTFactorySpecimen : MonoBehaviour {
 public BTTimeTarget target;
 public Transform remainingBar;
 public TextMesh readout;
 float last=-1;
 void Update(){
  float left=Mathf.Max(0,target.life-target.minimumLife);
  if(Mathf.Abs(last-left)<.05f)return;last=left;
  var s=remainingBar.localScale;s.x=Mathf.Max(.015f,left/(target.maxLife-target.minimumLife)*.7f);remainingBar.localScale=s;
  readout.text="LIFE "+Mathf.CeilToInt(left)+" / "+Mathf.RoundToInt(target.maxLife-target.minimumLife)+"\nRESERVE 20 • DO NOT EXCEED";
 }
}}
