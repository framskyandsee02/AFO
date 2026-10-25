using System;
using UnityEngine;
namespace BorrowedTime {
public class BTSkyCityVFX:MonoBehaviour {
 [Serializable] public class Route{public Vector3[] points;public Color color;public float Length{get{float n=0;for(int i=1;i<points.Length;i++)n+=Vector3.Distance(points[i-1],points[i]);return n;}}}
 public BTSkyCityGame game;public Route[] routes;
 LineRenderer beam,leak,sparks;Transform hand;Transform[] motes=new Transform[10];Material beamMat,dustMat,waterMat,sparkMat;
 Material Mat(Color c){var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.SetColor("_BaseColor",c);return m;}
 LineRenderer Line(string name,Material mat,float width){var go=new GameObject(name);go.transform.SetParent(transform);var r=go.AddComponent<LineRenderer>();r.sharedMaterial=mat;r.startWidth=r.endWidth=width;r.positionCount=2;return r;}
 public static Vector3 PointAt(Route r,float t){float d=Mathf.Clamp01(t)*r.Length;for(int i=1;i<r.points.Length;i++){float n=Vector3.Distance(r.points[i-1],r.points[i]);if(d<=n)return Vector3.Lerp(r.points[i-1],r.points[i],n>.001f?d/n:0);d-=n;}return r.points[r.points.Length-1];}
 void Start(){beamMat=Mat(Color.cyan);dustMat=Mat(new Color(.5f,.3f,.12f));waterMat=Mat(new Color(.65f,.84f,.9f));sparkMat=Mat(new Color(1,.6f,.2f));beam=Line("TimeTransferBeam",beamMat,.055f);leak=Line("PipeLeakSpray",waterMat,.045f);leak.positionCount=9;sparks=Line("ClockFaultSpark",sparkMat,.025f);sparks.positionCount=5;
  for(int i=0;i<motes.Length;i++){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="AgeFragmentFX";Destroy(go.GetComponent<Collider>());go.transform.SetParent(transform);go.GetComponent<Renderer>().sharedMaterial=dustMat;go.transform.localScale=Vector3.one*.045f;go.SetActive(false);motes[i]=go.transform;}
  if(game.visual)foreach(var t in game.visual.GetComponentsInChildren<Transform>())if(t.name=="Hand.R"){hand=t;break;}
 }
 void LateUpdate(){var p=game.puzzles;if(!p)return;int index=game.LastTransferTarget;bool show=index>=0&&Time.time-game.LastTransferTime<.12f;beam.enabled=show;
  if(show){bool giving=game.LastTransferDirection>0;var point=game.AimPoint(index);var origin=hand?hand.position:game.player.position+Vector3.up*1.3f;beamMat.SetColor("_BaseColor",giving?new Color(1,.72f,.3f):new Color(.2f,.8f,1));dustMat.SetColor("_BaseColor",giving?new Color(.7f,.88f,1):new Color(.43f,.28f,.12f));beam.SetPosition(0,origin);beam.SetPosition(1,point);for(int i=0;i<motes.Length;i++){float t=Mathf.Repeat(Time.time*1.7f+i*.1f,1);float a=i*2.4f;motes[i].position=point+new Vector3(Mathf.Cos(a)*t*.6f,(giving?1-t:-t)*.7f,Mathf.Sin(a)*t*.6f);}}
  foreach(var m in motes)m.gameObject.SetActive(show);
  bool leaking=game.nodes[8].value<.999f&&p.Flow>.05f;leak.enabled=leaking;
  if(leaking){var source=p.crack.position;float strength=(1-game.nodes[8].value)*p.Flow;for(int i=0;i<9;i++){float t=i/8f;leak.SetPosition(i,source+new Vector3(t*1.5f*strength,Mathf.Sin(t*Mathf.PI)*.8f*strength,t*.3f));}}
  bool fault=p.WaterReady&&!p.ClockReady&&Vector3.Distance(game.player.position,p.gearB.position)<20;sparks.enabled=fault&&Mathf.Repeat(Time.time,1.6f)<.1f;
  if(sparks.enabled)for(int i=0;i<5;i++)sparks.SetPosition(i,p.gearB.position+new Vector3(i*.12f,Mathf.Sin(Time.time*60+i)*.2f,-.2f));
 }
}
}
