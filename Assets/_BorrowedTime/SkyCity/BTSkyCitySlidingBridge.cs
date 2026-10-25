using UnityEngine;
namespace BorrowedTime {
public class BTSkyCitySlidingBridge : MonoBehaviour {
 public Transform[] segments;
 public float step;
 public Collider[] movingColliders,gateColliders;
 public Transform gateLeft,gateRight;
 public Renderer[] statusLights;
 MaterialPropertyBlock lightBlock;
 public void SetProgress(float progress) {
  float p=Mathf.Clamp01(progress);
  float travel=Mathf.SmoothStep(0,1,p)*step*segments.Length;
  for(int i=0;i<segments.Length;i++)
   segments[i].localPosition=new Vector3(0,0,-step*.5f+.1f+Mathf.Min(travel,(i+1)*step));
  bool ready=p>=.999f;
  foreach(var c in movingColliders)if(c)c.enabled=ready;
  float opening=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.9f,1,p))*3.2f;
  if(gateLeft)gateLeft.localPosition=Vector3.left*opening;
  if(gateRight)gateRight.localPosition=Vector3.right*opening;
  foreach(var c in gateColliders)if(c)c.enabled=!ready;
  if(lightBlock==null)lightBlock=new MaterialPropertyBlock();
  var color=ready?new Color(.15f,1,.65f):new Color(1,.35f,.04f);
  lightBlock.SetColor("_BaseColor",color);
  lightBlock.SetColor("_EmissionColor",color*2);
  foreach(var r in statusLights)if(r)r.SetPropertyBlock(lightBlock);
 }
}
}
