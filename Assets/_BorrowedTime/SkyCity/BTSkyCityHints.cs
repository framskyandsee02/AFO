using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime {
public class BTSkyCityHints:MonoBehaviour {
 public BTSkyCityGame game;public Font thaiFont;int[] levels=new int[4];string current="";float until;GUIStyle style,marker;
 public void ResetHints(){levels=new int[4];current="";until=0;}
 public int NearestDistrict(){if(game.puzzles.commissioned)return -1;if(!game.IntroAccess&&!game.GeneratorReady&&!game.puzzles.waterAccess)return 0;if(!game.puzzles.WaterReady)return 1;if(!game.puzzles.ClockReady)return 2;if(!game.FactoryReady)return 3;return -1;}
 public static string HintText(int district,int level){string[,] text={
 {"รากไม้ปิดเครื่องกำเนิด ดูสิ่งที่ขวางก่อน","ดูดรากให้แก่และผุ แล้วนำเวลาไปซ่อมเครื่องกำเนิด","เลื่อนลงที่รากจนสลาย จากนั้นเลื่อนขึ้นที่เครื่องจนสมบูรณ์"},
 {"คลองไม่มีน้ำถึงกังหัน มองหาไม้ขัดทางน้ำและท่อที่รั่ว","ดูดไม้ขัดให้ผุ ซ่อมท่อ แล้วเติมส่วนที่หักของกังหัน","เอาไม้ขัดออก → ซ่อมท่อและกังหันจนสมบูรณ์ → ประตูห้องเฟืองจะเปิด"},
 {"น้ำส่งแรงมาถึงแล้ว แต่เฟืองยังติดขัด มองผ่านประตูห้องเฟือง","มีคานไม้ขวางและฟันเฟืองหัก ต้องจัดการทั้งสองจุด","ดูดคานให้ผุ แล้วให้เวลาซ่อมฟันเฟือง หอนาฬิกาจะเดินและทางลาดจะยืด"},
 {"ตามทางลาดขึ้นลานควบคุม หัวล็อกสะพานยังชำรุด","ลิ่มค้ำพยุงหัวล็อกไว้ ซ่อมหัวล็อกก่อนปล่อยลิ่ม","ดูดถังสำรอง → ให้เวลาซ่อมหัวล็อก → ดูดลิ่มค้ำให้สลาย หากเอาลิ่มออกก่อน ให้เวลาคืนลิ่มแล้วซ่อมใหม่"}};
 return district<0?"รอสะพานเชื่อมฐานและประตูเปิด จากนั้นข้ามไปแล้วกด F":text[Mathf.Clamp(district,0,3),Mathf.Clamp(level,0,2)];}
 void Update(){if(Keyboard.current!=null&&Keyboard.current.hKey.wasPressedThisFrame){int d=NearestDistrict();int step=d<0?0:Mathf.Min(levels[d]++,2);current=HintText(d,step);until=Time.unscaledTime+14;}}
 void OnGUI(){
  if(game.previewOnly)return;if(style==null){style=new GUIStyle(GUI.skin.box){font=thaiFont,fontSize=18,wordWrap=true,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(16,16,12,12)};style.normal.textColor=new Color(.75f,1,1);marker=new GUIStyle(GUI.skin.box){font=thaiFont,fontSize=15};}
  if(!game.GetComponent<BTJourneyHUD>())GUI.Label(new Rect(Screen.width-235,103,215,30),"H — คำใบ้ทีละขั้น",new GUIStyle(GUI.skin.label){font=thaiFont,fontSize=17});
  if(Time.unscaledTime<until)GUI.Box(new Rect(24,267,Mathf.Min(700,Screen.width-48),115),current,style);
  int goal=game.puzzles.NextNode();var point=goal>=0?game.AimPoint(goal):game.door.position;var screen=game.cam.GetComponent<Camera>().WorldToScreenPoint(point+Vector3.up*.7f);
  if(screen.z>0&&screen.x>80&&screen.x<Screen.width-80){float distance=Vector3.Distance(game.player.position,point);if(distance>3)GUI.Label(new Rect(screen.x-105,Mathf.Clamp(Screen.height-screen.y,180,Screen.height-250),210,30),"เป้าหมายถัดไป · "+Mathf.RoundToInt(distance)+" ม.",marker);}
 }
}
}
