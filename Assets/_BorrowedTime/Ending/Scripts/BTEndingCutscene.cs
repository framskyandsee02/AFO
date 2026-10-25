using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace BorrowedTime.Ending {
public sealed class BTEndingCutscene : MonoBehaviour {
 public const float Duration=76;
 public const string Message="เราเห็นถึงความสามารถของเทคโนโลยีของคุณ\nพวกเราต้องการเวลาของคุณ\n\nส่งโดย _Pukker";
 public Camera shotCamera;
 public Transform tree,seed,glove,handPivot,letter,flap,greenery;
 public Transform upperArm,lowerArm,actorHand;
 public Quaternion upperRest,lowerRest;
 public Light sunLight,gloveLight;
 public Material glow;
 public LineRenderer[] streams;
 public Image fade,letterPanel;
 public Text letterText,title,subtitle,credits;
 public float elapsed;
 public bool manualPreview;
 public bool Finished => elapsed>=Duration;
 bool leaving;
 GUIStyle actionStyle;
 public string creditNames="BORROWED TIME\n\nA world given another tomorrow\n\nCreated by the Borrowed Time team\n\nThank you for playing";
 static Vector3 V(float x,float y,float z){return new Vector3(x,y,z);}
 static float S(float t,float a,float b){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));}
 void Start(){if(!manualPreview){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;elapsed=0;Evaluate(0);}}
 void Update(){if(!manualPreview){if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)SkipToCredits();elapsed=Mathf.Min(Duration,elapsed+Time.unscaledDeltaTime);Evaluate(elapsed);}}
 public void Replay(){elapsed=0;manualPreview=false;Evaluate(0);}
 public void SkipToCredits(){elapsed=Duration;Evaluate(Duration);var audio=GetComponent<BTEndingAudio>();if(audio)audio.Stop();}
 public void ReturnToMainMenu(){if(leaving)return;leaving=true;BTSaveGame.CancelPendingContinue();BTQuickSkip.ExitPreview();Time.timeScale=1;SceneManager.LoadScene("BorrowedTimeMainMenu");}
 void OnGUI(){
  if(manualPreview)return;
  GUI.depth=-200;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1920f,Screen.height/1080f,1));
  if(actionStyle==null)actionStyle=new GUIStyle(GUI.skin.button){font=letterText?letterText.font:null,fontSize=25};
  if(!Finished){if(GUI.Button(new Rect(1640,28,250,52),"ข้าม / ESC",actionStyle))SkipToCredits();}
  else{
   if(GUI.Button(new Rect(500,970,440,62),"ดูคัตซีนอีกครั้ง",actionStyle))Replay();
   if(GUI.Button(new Rect(980,970,440,62),"กลับหน้าเมนู",actionStyle))ReturnToMainMenu();
  }
 }
 public string Beat(float t){return t<9?"Seed":t<20?"Borrow sunlight":t<36?"Yggdrasil grows":t<46?"A living world":t<49?"Black":t<55?"Letter on desk":t<66?"Read letter":t<70?"End of DEMO":"Credits";}
 void Shot(Vector3 p,Vector3 target,float fov){shotCamera.transform.position=p;shotCamera.transform.LookAt(target);shotCamera.fieldOfView=fov;}
 public void Evaluate(float t){
 elapsed=Mathf.Clamp(t,0,Duration);t=elapsed;
 float grow=S(t,22,35),renew=S(t,30,44),charge=S(t,10,19)*(1-S(t,29,36));
 tree.localScale=Vector3.one*Mathf.Lerp(.006f,1,grow);seed.localScale=V(.22f,.38f,.22f)*(1-S(t,23,26));
 greenery.localScale=Vector3.one*Mathf.Max(.001f,renew);
 if(upperArm && lowerArm && actorHand){
 upperArm.localRotation=upperRest;lowerArm.localRotation=lowerRest;
 Vector3 aim=Vector3.Lerp(new Vector3(24,32,30),new Vector3(0,1.4f,0),S(t,19,23));
 float lift=S(t,8,11)*(1-S(t,35,40));
 upperArm.rotation=Quaternion.Slerp(upperArm.rotation,Quaternion.FromToRotation(lowerArm.position-upperArm.position,aim-upperArm.position)*upperArm.rotation,lift*.82f);
 lowerArm.rotation=Quaternion.Slerp(lowerArm.rotation,Quaternion.FromToRotation(actorHand.position-lowerArm.position,aim-lowerArm.position)*lowerArm.rotation,lift);
 }
 gloveLight.intensity=charge*1.2f;glow.SetColor("_EmissionColor",new Color(.2f,.85f,1)*charge*5);glow.color=Color.Lerp(new Color(.12f,.22f,.25f),new Color(.5f,.95f,1),charge);
 sunLight.color=Color.Lerp(new Color(1,.56f,.29f),new Color(1,.94f,.74f),renew);sunLight.intensity=Mathf.Lerp(1.5f,2.2f,renew);
 shotCamera.backgroundColor=Color.Lerp(new Color(.33f,.24f,.18f),new Color(.49f,.72f,.81f),renew);
 RenderSettings.fogColor=Color.Lerp(new Color(.23f,.16f,.13f),new Color(.48f,.68f,.69f),renew);
 RenderSettings.ambientLight=Color.Lerp(new Color(.23f,.19f,.18f),new Color(.42f,.58f,.5f),renew);
 for(int j=0;j<streams.Length;j++){
  var line=streams[j];line.enabled=t>=10&&t<34;
  Vector3 from=t<20?V(24,32,30):glove.position,to=t<20?glove.position:V(0,1.4f+grow*8,0);
  line.positionCount=64;line.startWidth=.035f+charge*.025f;line.endWidth=.015f;
  for(int k=0;k<64;k++){float u=k/63f;Vector3 p=Vector3.Lerp(from,to,u);float r=Mathf.Sin(u*Mathf.PI)*(.3f+j*.14f);p+=V(Mathf.Sin(u*22-t*4+j*2)*r,Mathf.Cos(u*18-t*3+j)*r,Mathf.Sin(u*19-t*4+j)*r);line.SetPosition(k,p);}
 }
 if(t<9) Shot(Vector3.Lerp(V(.6f,1.1f,-1.2f),V(6,4,-10),S(t,0,9)),V(0,.5f,0),42);
 else if(t<20) Shot(Vector3.Lerp(V(3,2.4f,-4),V(4,2.8f,-5),S(t,9,20)),V(-1,2.6f,1),50);
 else if(t<36) Shot(Vector3.Lerp(V(7,4,-11),V(28,17,-36),S(t,21,36)),V(0,Mathf.Lerp(2,17,grow),0),53);
 else if(t<49) Shot(Vector3.Lerp(V(28,19,-36),V(37,46,-43),S(t,36,46)),Vector3.Lerp(V(0,21,0),V(0,29,4),S(t,36,46)),57);
 else Shot(Vector3.Lerp(V(101.8f,3.6f,-2.6f),V(100,3.4f,-.65f),S(t,49,56)),V(100,1.6f,0),39);
 flap.localRotation=Quaternion.Euler(-155*S(t,53,55),0,0);
 letter.localPosition=V(100,1.54f+S(t,54,56)*.14f,0);
 float black=t<1?1-S(t,0,1):t>=44&&t<50?S(t,44,46)*(1-S(t,49,50)):t>=65?S(t,65,66):0;
 fade.color=new Color(0,0,0,black);
 letterPanel.gameObject.SetActive(t>=56&&t<65.7f);letterText.text=Message;
 title.text=t>=66&&t<70?"End of DEMO":"";
 subtitle.text=t<9?"Y G G D R A S I L  /  THE LAST SEED":t<20?"BORROWED FROM THE SUN":t<36?"ONE MOMENT. A THOUSAND YEARS.":t<44?"TOMORROW BEGINS AGAIN":"";
 credits.text=t>=70?creditNames:"";credits.rectTransform.anchoredPosition=V(0,Mathf.Lerp(-480,150,S(t,70,76)),0);
 }
}
}
