using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime {
[Serializable] public class BTRecoveryMaterial {public Material material;public Color from,to;public string property="_BaseColor";}
public class BTWorldEndingCinematic : MonoBehaviour {
 public BTFactoryGame game;
 public GameObject trainingVista,cityVista;
 public Vector3 cityCameraStart=new Vector3(-7,6,-16),cityCameraEnd=new Vector3(-10,5,-13),cityLookAt=new Vector3(-18,1,-2);
 public Transform[] newShoots;
 public BTRecoveryMaterial[] recovery;
 public bool Playing{get;private set;}public bool Finished{get;private set;}
 public int Shot{get;private set;}
 public float Elapsed{get;private set;}
 bool started;float oldFar;Vector3 cameraPosition;Quaternion cameraRotation;GUIStyle subtitle,small;
 void Awake(){
  var map=new System.Collections.Generic.Dictionary<Material,Material>();
  foreach(var item in recovery)if(item.material&&!map.ContainsKey(item.material))map[item.material]=new Material(item.material);
  foreach(var home in new[]{trainingVista,cityVista})foreach(var renderer in home.GetComponentsInChildren<Renderer>(true)){var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]&&map.ContainsKey(materials[i]))materials[i]=map[materials[i]];renderer.sharedMaterials=materials;}
  foreach(var item in recovery)if(item.material)item.material=map[item.material];
 }
 void Update(){
  if(!game.completed){if(started){trainingVista.SetActive(false);cityVista.SetActive(false);started=Playing=Finished=false;Elapsed=0;foreach(var m in recovery)if(m.material)m.material.SetColor(m.property,m.from);foreach(var shoot in newShoots)shoot.localScale=Vector3.zero;}return;}
  if(!started){started=true;Playing=true;Elapsed=0;oldFar=game.player.view.farClipPlane;game.player.view.farClipPlane=180;cameraPosition=game.player.view.transform.position;cameraRotation=game.player.view.transform.rotation;game.player.ClearPresentationTarget();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  if(!Playing)return;
  if(BorrowedTime.Menu.GamePauseMenu.IsPausedFor(game.player))return;
  Elapsed+=Time.deltaTime;
  if(game.player.inputOverride==null&&Keyboard.current!=null&&Elapsed>1&&(Keyboard.current.spaceKey.wasPressedThisFrame||Keyboard.current.fKey.wasPressedThisFrame)){Finish();return;}
  Shot=Elapsed<5?0:Elapsed<10?1:Elapsed<16?2:3;
  cityVista.SetActive(Shot==1);trainingVista.SetActive(Shot==2);
  float phase=Mathf.SmoothStep(0,1,Mathf.Clamp01((Elapsed-5)/11));
  foreach(var m in recovery)if(m.material)m.material.SetColor(m.property,Color.Lerp(m.from,m.to,phase));
  foreach(var shoot in newShoots)if(shoot)shoot.localScale=Vector3.one*Mathf.SmoothStep(0,.75f,phase);
  Vector3 at,look;
  if(Shot==0){at=Vector3.Lerp(new Vector3(9,15,18),new Vector3(7,15,17),Elapsed/5);look=new Vector3(0,14.7f,12);}
  else if(Shot==1){at=cityVista.transform.position+Vector3.Lerp(cityCameraStart,cityCameraEnd,(Elapsed-5)/5);look=cityVista.transform.position+cityLookAt;}
  else if(Shot==2){at=trainingVista.transform.position+Vector3.Lerp(new Vector3(31,15,24),new Vector3(27,13,23),(Elapsed-10)/6);look=trainingVista.transform.position+new Vector3(-2,1,5);}
  else{at=Vector3.Lerp(new Vector3(1,14,20),new Vector3(1,14,18),(Elapsed-16)/4);look=new Vector3(0,14.2f,12);}
  var camera=game.player.view;camera.orthographic=false;camera.fieldOfView=Shot==1?54:58;camera.transform.position=at;camera.transform.LookAt(look);
  RenderSettings.fogColor=Color.Lerp(new Color(.17f,.21f,.24f),new Color(.49f,.59f,.57f),phase);RenderSettings.ambientSkyColor=Color.Lerp(new Color(.4f,.44f,.49f),new Color(.64f,.68f,.61f),phase);
  if(Elapsed>=20)Finish();
 }
 public void Finish(){game.player.view.farClipPlane=oldFar;Playing=false;Finished=true;trainingVista.SetActive(false);cityVista.SetActive(false);game.player.view.transform.position=cameraPosition;game.player.view.transform.rotation=cameraRotation;}
 void OnGUI(){
  if(!Playing||game.player.hideHud)return;
  if(subtitle==null){subtitle=new GUIStyle(GUI.skin.label){font=game.uiFont,fontSize=25,alignment=TextAnchor.MiddleCenter,wordWrap=true};subtitle.normal.textColor=new Color(.96f,.96f,.88f);small=new GUIStyle(subtitle){fontSize=14,alignment=TextAnchor.MiddleRight};}
  GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,1440,70),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(0,790,1440,110),Texture2D.whiteTexture);GUI.color=Color.white;
  string[] lines={"เมื่อเครื่องจักรเงียบลง…","น้ำที่เคยมืดหม่น เริ่มเปลี่ยนไป","ท่ามกลางป่าที่แห้งเหี่ยว ชีวิตเล็ก ๆ กำลังกลับมา","เราอาจไม่ได้คืนโลกทั้งใบในวันเดียว แต่บางสิ่งได้เริ่มต้นขึ้นแล้ว"};GUI.Label(new Rect(110,806,1220,55),lines[Shot],subtitle);GUI.Label(new Rect(1030,866,365,25),"SPACE / F ข้ามคัตซีน",small);
 }
}}
