using UnityEngine;
namespace BorrowedTime {
public class BTSkyCityAgeVisual:MonoBehaviour {
 public enum Form {Wood,Metal,Plant,Lamp}
 public BTSkyCityGame game;public int nodeIndex;public Form form;
 public Transform[] fragments;public Renderer[] surfaces;public Collider[] solids;public Light[] lamps;
 [SerializeField] Vector3[] positions,scales,ups;[SerializeField] Quaternion[] rotations;[SerializeField] Color[] colors;
 [SerializeField] Vector3 boxCenter,boxSize;[SerializeField] bool captured;BoxCollider target;
 bool initialized;MaterialPropertyBlock block;
 public void Initialize(){
  if(initialized&&block!=null)return;initialized=true;block=new MaterialPropertyBlock();target=GetComponent<BoxCollider>();if(captured)return;if(target){boxCenter=target.center;boxSize=target.size;}
  if(fragments==null)fragments=new Transform[0];if(surfaces==null)surfaces=GetComponentsInChildren<Renderer>();
  positions=new Vector3[fragments.Length];scales=new Vector3[fragments.Length];rotations=new Quaternion[fragments.Length];ups=new Vector3[fragments.Length];
  for(int i=0;i<fragments.Length;i++){var t=fragments[i];positions[i]=t.localPosition;scales[i]=t.localScale;rotations[i]=t.localRotation;ups[i]=t.parent?t.parent.InverseTransformDirection(Vector3.up):Vector3.up;}
  colors=new Color[surfaces.Length];for(int i=0;i<surfaces.Length;i++){var m=surfaces[i].sharedMaterial;colors[i]=m&&m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.gray;}captured=true;
 }
 void Awake(){Initialize();}
 void LateUpdate(){Apply();}
 public void Apply(){
  if(!game||game.nodes==null||nodeIndex>=game.nodes.Length)return;Initialize();float f=game.nodes[nodeIndex].value;
  for(int i=0;i<fragments.Length;i++)if(fragments[i]){bool finalPiece=i==fragments.Length-1;float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(finalPiece?0:.08f+(i%3)*.08f,finalPiece?1:.82f+(i%4)*.05f,f));float scatter=((i%2)*2-1)*.2f;var old=positions[i]-ups[i]*(form==Form.Plant?.18f:.3f+(i%3)*.12f)+new Vector3(scatter,0,scatter*.4f);fragments[i].localPosition=Vector3.Lerp(old,positions[i],t);fragments[i].localRotation=Quaternion.Slerp(rotations[i]*Quaternion.Euler(18+i%4*13,scatter*70,scatter*85),rotations[i],t);float minimum=form==Form.Plant?.3f:.06f;fragments[i].localScale=scales[i]*Mathf.Lerp(minimum,1,t);}
  Color oldColor=form==Form.Plant?new Color(.34f,.25f,.09f):form==Form.Wood?new Color(.19f,.12f,.065f):new Color(.28f,.16f,.09f);
  for(int i=0;i<surfaces.Length;i++)if(surfaces[i]){surfaces[i].GetPropertyBlock(block);block.SetColor("_BaseColor",Color.Lerp(oldColor,colors[i],Mathf.SmoothStep(0,1,f)));bool emissive=surfaces[i].name.Contains("LampGlow")||surfaces[i].name.Contains("CoreLight");block.SetColor("_EmissionColor",emissive?colors[i]*f*f*2:Color.black);surfaces[i].SetPropertyBlock(block);}
  if(solids!=null)foreach(var c in solids)if(c)c.enabled=form==Form.Metal||form==Form.Lamp||f>.18f;
  if(lamps!=null)foreach(var light in lamps)if(light){light.intensity=f*f*1.4f;light.enabled=f>.05f;}
  if(target&&target.isTrigger&&(form==Form.Wood||form==Form.Plant)){float height=Mathf.Lerp(.62f,1,Mathf.SmoothStep(0,1,f));target.size=new Vector3(boxSize.x,boxSize.y*height,boxSize.z);target.center=new Vector3(boxCenter.x,boxCenter.y-boxSize.y*.5f+boxSize.y*height*.5f,boxCenter.z);}
 }
}
}
