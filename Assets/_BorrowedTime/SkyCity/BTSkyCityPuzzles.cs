using UnityEngine;
namespace BorrowedTime {
public class BTSkyCityPuzzles:MonoBehaviour {
 public BTSkyCityGame game;public bool testMode;
 public Transform water,crack,gearA,gearB,clutch,freightRail,freightLoad;
 public Renderer[] signals;public bool pipeInspected,railDropped,railAligned;public bool[] solved=new bool[3];
 public Transform wheelRotor,clockGateLeft,clockGateRight,motorRotor;public Transform[] clockHands,shafts,flowStreaks;
 public BTSkyCitySlidingBridge accessBridge;public bool waterAccess,clockAccess,commissioned;
 float gateProgress,accessProgress;Vector3 waterScale;AudioSource waterSound,gearSound,completionSound;AudioClip chime;
 const string Prefix=BTSkyCityGame.SavePrefix;
 float V(int i){return game.nodes[i].value;}
 void SaveInt(string key,bool value){if(!testMode)PlayerPrefs.SetInt(Prefix+key,value?1:0);}
 public void LoadState(){waterAccess=PlayerPrefs.GetInt(Prefix+"WaterAccess",0)==1;clockAccess=PlayerPrefs.GetInt(Prefix+"ClockAccess",0)==1;commissioned=PlayerPrefs.GetInt(Prefix+"Docked",0)==1;}
 public static bool WaterSolved(bool accessible,float obstruction,float wheel,float pipe){return accessible&&obstruction<=.001f&&wheel>=.999f&&pipe>=.999f;}
 public static bool ClockSolved(float jam,float gear,float unused){return jam<=.001f&&gear>=.999f;}
 public static bool FreightSolved(bool released,float head){return released&&head>=.999f;}
 public bool WaterReady {get{return WaterSolved(true,V(2),V(3),V(8));}}
 public bool ClockReady {get{return WaterReady&&ClockSolved(V(4),V(5),0);}}
 public bool ReadyToLaunch {get{return commissioned||(ClockReady&&FreightSolved(V(6)<=.001f,V(10)));}}
 public bool Complete(int district){if(district<0||district>2)return false;return commissioned||(district==0?WaterReady:district==1?ClockReady:ReadyToLaunch);}
 public float Flow {get{return Mathf.SmoothStep(0,1,1-V(2));}}
 public float WheelPower {get{return Flow*Mathf.Lerp(.25f,1,V(8))*V(3);}}
 void Start(){waterScale=water.localScale;gateProgress=waterAccess?1:0;accessProgress=clockAccess?1:0;InitAudio();Evaluate();ApplyMechanisms(0);}
 public void Evaluate(){
  bool beforeWater=waterAccess,beforeClock=clockAccess;
  if(WaterReady&&!waterAccess){waterAccess=true;SaveInt("WaterAccess",true);}
  if(ClockReady&&waterAccess&&!clockAccess){clockAccess=true;SaveInt("ClockAccess",true);}
  solved[0]=WaterReady;solved[1]=ClockReady;solved[2]=ReadyToLaunch;
  if(Application.isPlaying&&waterAccess&&!beforeWater){game.Say("น้ำไหลแล้ว! กังหันกำลังเปิดประตูห้องเฟือง — ไปที่หอนาฬิกา");Ping(1);}
  if(Application.isPlaying&&clockAccess&&!beforeClock){game.Say("ชุดเฟืองส่งแรงถึงสะพานแล้ว! ทางลาดกำลังยืดไปลานควบคุม");Ping(1.2f);}
 }
 public void MarkDocked(){commissioned=true;SaveInt("Docked",true);game.Say("หัวสะพานเข้าล็อกแล้ว — ประตูโรงงานกำลังเปิด");Ping(1.4f);}
 public void ResetState(){waterAccess=clockAccess=commissioned=false;solved=new bool[3];gateProgress=accessProgress=0;SaveInt("WaterAccess",false);SaveInt("ClockAccess",false);SaveInt("Docked",false);ApplyMechanisms(0);}
 public bool Transfer(int i,float direction,float dt,out string reason){
  reason="";if(i<2||i>=12||game.nodes[i].ambient)return false;
  if((i==4||i==5)&&!waterAccess){reason="ประตูห้องเฟืองยังปิด: เปิดทางน้ำ ซ่อมท่อและกังหันก่อน";return true;}
  if((i==6||i==10||i==11)&&!clockAccess){reason="ต้องให้กังหันและชุดเฟืองเปิดทางไปลานควบคุมก่อน";return true;}
  if(i==10&&direction>0&&V(6)<.95f){reason="ลิ่มค้ำถูกทำลายแล้ว: ให้เวลาคืนลิ่มก่อน เพื่อรองรับหัวล็อกระหว่างซ่อม";return true;}
  if(!game.ApplyTime(i,direction,dt)&&direction>0&&game.energy<.01f)reason="เวลาไม่พอ: ดูดจากของรอบเมือง หรือถังสำรองข้างเครื่อง";
  Evaluate();return true;
 }
 public int NextNode(){
  if(commissioned)return -1;
  if(!game.IntroAccess&&!game.GeneratorReady&&!waterAccess)return game.nodes[0].Done?1:0;
  if(!WaterReady)return V(2)>.001f?2:V(8)<.999f?8:3;
  if(!ClockReady)return V(4)>.001f?4:5;
  if(!ReadyToLaunch)return V(10)<.999f?(V(6)<.95f?6:10):6;
  return -1;
 }
 public string Objective(){
  if(commissioned)return "ประตูโรงงานเปิดแล้ว — ข้ามสะพานแล้วกด F ที่ทางเข้า";
  if(!WaterReady)return "1 / คืนทางน้ำให้กังหัน\nทางน้ำ → ท่อไม่รั่ว → กังหัน → เปิดห้องเฟือง";
  if(!ClockReady)return "2 / ต่อชุดส่งกำลัง\nเอาคานที่ขัดออก แล้วซ่อมเฟืองฟันหัก";
  if(!ReadyToLaunch)return "3 / ปล่อยสะพาน\nซ่อมหัวล็อกก่อน แล้วดูดลิ่มค้ำเพื่อปล่อยรางเลื่อน";
  return "สะพานกำลังเชื่อมฐานโรงงาน — รอหัวล็อกเข้าที่";
 }
 public string Clue(int i){
  switch(i){case 0:return "รากขวางเครื่อง: ดูดเวลาให้แก่และสลาย";case 1:return "ให้เวลาซ่อมเครื่องกำเนิด หลังเอารากที่ขวางออก";
   case 2:return "ไม้ขัดประตูน้ำ: ดูดให้ผุสลาย แล้วดูน้ำไหลเข้าคลอง";
   case 3:return "ใบกังหันพัง: ให้เวลาเพื่อประกอบกลับ น้ำจะขับกังหัน";
   case 8:return "ท่อแตก: ให้เวลาปิดรอยรั่ว เพื่อส่งน้ำถึงกังหัน";
   case 4:return "คานไม้ขัดเฟือง: ดูดให้ผุ เฟืองจึงหมุนผ่านได้";
   case 5:return "ฟันเฟืองหัก: ให้เวลาประกอบฟันกลับ แล้วแรงจากน้ำจะส่งต่อ";
   case 6:return V(10)>=.999f?"หัวล็อกพร้อมแล้ว: ดูดลิ่มค้ำให้ผุเพื่อปล่อยสะพาน":"ลิ่มค้ำพยุงหัวล็อก: เก็บไว้จนซ่อมหัวล็อกเสร็จ";
   case 10:return "ให้เวลาซ่อมแขนหัวล็อก ระหว่างที่ลิ่มค้ำยังรองรับอยู่";
   case 11:return "ถังสำรอง: ดูดเวลาไว้ใช้ซ่อมหัวล็อก";
   default:return "เลื่อนลง ดูดให้แก่ / เลื่อนขึ้น ให้เวลาซ่อม";}
 }
 void Update(){Evaluate();ApplyMechanisms(Time.deltaTime);}
 void ApplyMechanisms(float dt){
  gateProgress=Mathf.MoveTowards(gateProgress,waterAccess?1:0,dt*.45f);accessProgress=Mathf.MoveTowards(accessProgress,clockAccess?1:0,dt*.3f);
  if(clockGateLeft)clockGateLeft.localPosition=Vector3.left*3.1f*Mathf.SmoothStep(0,1,gateProgress);
  if(clockGateRight)clockGateRight.localPosition=Vector3.right*3.1f*Mathf.SmoothStep(0,1,gateProgress);
  if(accessBridge)accessBridge.SetProgress(accessProgress);
  if(wheelRotor)wheelRotor.Rotate(Vector3.right,dt*85*WheelPower,Space.Self);
  float gearPower=WaterReady?Mathf.Clamp01(1-V(4)):0;
  if(gearA)gearA.Rotate(Vector3.forward,dt*70*gearPower,Space.Self);
  if(gearB)gearB.Rotate(Vector3.forward,-dt*46*gearPower*V(5),Space.Self);
  if(clockHands!=null)for(int i=0;i<clockHands.Length;i++)if(clockHands[i])clockHands[i].Rotate(Vector3.forward,-dt*(i==0?12:1)* (ClockReady?1:0),Space.Self);
  if(shafts!=null)foreach(var shaft in shafts)if(shaft)shaft.Rotate(Vector3.right,dt*100*WheelPower,Space.Self);
  if(motorRotor)motorRotor.Rotate(Vector3.right,dt*150*(ClockReady?1:0),Space.Self);
  if(water){water.localScale=new Vector3(waterScale.x,waterScale.y,waterScale.z);water.localPosition=new Vector3(water.localPosition.x,Mathf.Lerp(.16f,.32f,Flow),water.localPosition.z);}
  if(crack)crack.gameObject.SetActive(true);
  if(flowStreaks!=null)for(int i=0;i<flowStreaks.Length;i++){var streak=flowStreaks[i];if(!streak)continue;streak.gameObject.SetActive(Flow>.05f);streak.position=new Vector3(3.1f+(i%4)*1.8f,.34f,11+Mathf.Repeat(i*.71f+Time.time*2*Flow,8));}
  if(signals!=null)for(int i=0;i<signals.Length;i++)if(signals[i]){var block=new MaterialPropertyBlock();var c=Complete(Mathf.Min(i,2))?new Color(.15f,.8f,.5f):new Color(.35f,.2f,.08f);block.SetColor("_BaseColor",c);block.SetColor("_EmissionColor",c*.3f);signals[i].SetPropertyBlock(block);}
  if(waterSound)waterSound.volume=Mathf.Lerp(waterSound.volume,WheelPower*.18f,dt*4);if(gearSound)gearSound.volume=Mathf.Lerp(gearSound.volume,gearPower*V(5)*.14f,dt*4);
 }
 AudioClip Tone(string name,bool waterLoop){int count=24000;var data=new float[count];uint seed=73;for(int i=0;i<count;i++){float t=i/24000f;seed=1664525*seed+1013904223;float noise=((seed>>8)/16777216f)*2-1;data[i]=waterLoop?(noise*.12f+Mathf.Sin(t*Mathf.PI*2*96)*.025f):Mathf.Sin(t*Mathf.PI*2*48)*.09f+noise*.025f;}var clip=AudioClip.Create(name,count,1,24000,false);clip.SetData(data,0);return clip;}
 AudioSource Loop(Transform location,AudioClip clip){var source=location.gameObject.AddComponent<AudioSource>();source.clip=clip;source.loop=true;source.spatialBlend=1;source.minDistance=3;source.maxDistance=24;source.volume=0;source.Play();return source;}
 void InitAudio(){if(!wheelRotor)return;waterSound=Loop(wheelRotor,Tone("Canal water and wheel",true));gearSound=Loop(gearB,Tone("Clock transmission",false));completionSound=game.gameObject.AddComponent<AudioSource>();completionSound.volume=.2f;var samples=new float[14400];for(int i=0;i<samples.Length;i++){float t=i/24000f;float env=Mathf.Exp(-t*6);samples[i]=(Mathf.Sin(t*2*Mathf.PI*440)+Mathf.Sin(t*2*Mathf.PI*660)*.5f)*env*.25f;}chime=AudioClip.Create("Mechanical completion",samples.Length,1,24000,false);chime.SetData(samples,0);}
 void Ping(float pitch){if(completionSound&&chime){completionSound.pitch=pitch;completionSound.PlayOneShot(chime);}}
}
}
