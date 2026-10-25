using UnityEngine;
namespace BorrowedTime {
public class BTJourneyHUD : MonoBehaviour {
 public BTTrainingGame game;public BTSkyCityGame sky;
 GUIStyle title,body,small,large,button;Texture2D panel,accent,shade;
 Texture2D Solid(Color c){var t=new Texture2D(1,1);t.SetPixel(0,0,c);t.Apply();return t;}
 void Init(){if(title!=null)return;title=new GUIStyle(GUI.skin.label){font=game?game.uiFont:sky.hints.thaiFont,fontSize=23,fontStyle=FontStyle.Bold,wordWrap=true};title.normal.textColor=new Color(.7f,1,1);body=new GUIStyle(title){fontSize=18,fontStyle=FontStyle.Normal};small=new GUIStyle(body){fontSize=14};small.normal.textColor=new Color(.72f,.82f,.83f);large=new GUIStyle(title){fontSize=34};button=new GUIStyle(GUI.skin.button){font=game?game.uiFont:sky.hints.thaiFont,fontSize=19,wordWrap=true};panel=Solid(new Color(.035f,.075f,.095f,.95f));accent=Solid(new Color(.35f,.85f,.9f));shade=Solid(new Color(.015f,.025f,.04f,.85f));}
 void Panel(Rect r){GUI.DrawTexture(r,panel);GUI.DrawTexture(new Rect(r.x,r.y,4,r.height),accent);}
 string Chapter=>game is BTFactoryGame?"03 / THE LAST SEED":game is BTCityGame?"02 / THE FORGOTTEN CITY":"01 / TRAINING AREA";
 public string Objective{
  get{
   var f=game as BTFactoryGame;if(f){var goals=new[]{"เข้าสู่โรงงาน","สำรวจโถงรับสินค้า","เปิดประตู • ซ่อมจอ หรือ หมุนมือหมุนสำรอง","ข้ามช่องว่าง • คืนสะพาน หรือ ใช้กว้าน","ขึ้นลิฟต์ • ซ่อมวงจร หรือ ต่อมอเตอร์สำรอง","เปิดทางสายพาน • เร่งเวลา หรือ ปลดเบรก","เดินผ่านโถงหม้อไอน้ำ","ข้ามวงแหวน • คืนทางเดิน หรือ หมุนกว้าน","หยุดมลพิษ • ซ่อมจอ หรือ ปิดวาล์วสามจุด","คืนชีวิต • ถุงมือเวลา หรือ ระบบน้ำและแสง"};return goals[Mathf.Clamp(game.stage,0,9)];}
   var c=game as BTCityGame;if(c)return !c.waterRestored?"หาต้นเหตุ • แยกท่อน้ำเสียจากระบบน้ำ":!c.gardenRestored?"คืนชีวิตต้นไม้ในสวนสาธารณะ":!c.CrossingPassable?"ข้ามคลอง • สะพาน หรือ รากไม้":!c.factoryOpen?"เปิดเส้นทางเข้าสู่โรงงาน":"เดินทางต่อไปยังโรงงาน";
   return game.objectives[Mathf.Clamp(game.stage,0,game.objectives.Length-1)];
  }
 }
 void OnGUI(){
  if(sky){SkyGUI();return;}
  if(!game||!game.player||game.player.hideHud||BorrowedTime.Menu.GamePauseMenu.IsPausedFor(game.player))return;
  var cinema=game.GetComponent<BTWorldEndingCinematic>();if(cinema&&cinema.Playing)return;
  var presentation=game.GetComponent<BTPresentation>();if(game.IsGentleTraining&&game.stage==0&&presentation&&!presentation.briefingComplete)return;
  var city=game as BTCityGame;if(game.player.paused&&!(city&&city.noteOpen))return;
  Init();GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
  if(game.stage==0||game.completed||city&&city.noteOpen){
   GUI.DrawTexture(new Rect(0,0,1440,900),shade);Panel(new Rect(340,210,760,470));GUI.Label(new Rect(380,248,680,70),city&&city.noteOpen?"บันทึกของ Norman Scott":game.completed?(game is BTFactoryGame?"โลกกำลังเริ่มเปลี่ยนแปลง":"พร้อมเดินทางต่อ"):Chapter,large);
   string text=city&&city.noteOpen?city.noteText:game.completed?(game is BTFactoryGame?"เราได้หยุดต้นเหตุแล้ว\nน้ำเริ่มกลับมาใส และหน่อเล็ก ๆ เริ่มเติบโต\n\n"+((BTFactoryGame)game).ending:"เมล็ดพันธุ์และสิ่งที่เรียนรู้จะเดินทางไปกับคุณ\nกำลังเข้าสู่พื้นที่ถัดไป…"):game is BTFactoryGame?"ต้นไม้ทดลองเหลือพลังจำกัด • LMB ยืมพลังอย่างพอดี\nเปลี่ยนเวลาใช้ 15 ต่อขั้น คืนอายุเดิมเพื่อกู้พลังที่ยังไม่ยืนยัน\n\nทุกทางหลักมีทางเลือก: ใช้ถุงมือ หรือ ใช้อุปกรณ์สำรองด้วย F\nหยุดมลพิษ ฟื้นน้ำสะอาด แล้วปลูกเมล็ดพันธุ์":game is BTCityGame?"เมืองกำลังแห้งเหี่ยวจากมลพิษ\nแก้ที่ต้นเหตุ แล้วสังเกตสิ่งที่เปลี่ยนไป\n\nสะพานและรากไม้เป็นทางเลือกสำหรับข้ามคลอง":"เดินชมพื้นที่และทดลองถุงมือเวลา\nเล็งด้วยครอสแฮร์ ล้อถอยคืนสภาพ ล้อหน้าเร่งสภาพ\n\nRobo จะอยู่ข้างคุณตลอดการเดินทาง";
   GUI.Label(new Rect(380,337,680,232),text,body);
   if(city&&city.noteOpen){if(GUI.Button(new Rect(380,590,680,52),"เก็บบันทึกและสำรวจต่อ / F",button))city.CloseNote();}
   else if(!game.completed){if(GUI.Button(new Rect(380,590,680,52),"เริ่มสำรวจ / ENTER",button))game.Begin();}
   else if(game is BTFactoryGame&&GUI.Button(new Rect(380,590,680,52),"เริ่มการเดินทางใหม่",button))BTJourney.LoadTraining();return;
  }
  Panel(new Rect(24,24,650,132));GUI.Label(new Rect(44,35,610,37),Chapter,title);GUI.Label(new Rect(44,80,605,61),Objective,body);
  Panel(new Rect(1080,24,336,215));GUI.Label(new Rect(1100,40,298,35),"พลังถุงมือ  "+Mathf.RoundToInt(game.player.energy)+" / 100",title);GUI.DrawTexture(new Rect(1100,86,290,10),shade);GUI.DrawTexture(new Rect(1100,86,290*game.player.energy/100,10),accent);
  GUI.Label(new Rect(1100,111,290,66),"เวลา 15 / ขั้น\n"+(game.hasMiracleSeed?"◆ มีเมล็ดพันธุ์":game is BTFactoryGame&&((BTFactoryGame)game).seedPlanted?"◆ ปลูกเมล็ดแล้ว":"◇ เมล็ดพันธุ์ยังไม่ถึงมือ"),body);GUI.Label(new Rect(1100,187,290,38),"F5 บันทึก • H คำแนะนำ",small);
  Panel(new Rect(24,730,1392,90));GUI.Label(new Rect(44,740,1300,28),"ROBO / ผู้ช่วยร่วมทาง",small);GUI.Label(new Rect(44,772,1335,44),game.roboLine,body);
  GUI.Label(new Rect(24,846,1390,42),"WASD เดิน  Shift วิ่ง  Space กระโดด  ล้อถอย คืนสภาพ / ล้อหน้า เร่งสภาพ  LMB ดูดพลัง  F ใช้งาน  Tab แผนที่  Esc พัก",small);
  var t=game.player.target;if(t&&!game.player.overview){Panel(new Rect(425,546,590,112));GUI.Label(new Rect(445,555,550,38),t.displayName,title);string detail=t.kind==TargetKind.Plant?"พลังที่ยืมได้ "+Mathf.CeilToInt(Mathf.Max(0,t.life-t.minimumLife))+" หน่วย • LMB ค้าง":t.StateName+" • ล้อเมาส์เปลี่ยนเวลา";GUI.Label(new Rect(445,598,550,52),detail,body);}
  var routes=game.GetComponent<BTFactoryRoutes>();if(routes&&!string.IsNullOrEmpty(routes.NearestHelp)){Panel(new Rect(425,433,590,96));GUI.Label(new Rect(445,443,550,78),routes.NearestHelp,body);}
  if(!string.IsNullOrEmpty(game.player.Feedback)){Panel(new Rect(300,673,840,45));GUI.Label(new Rect(320,677,800,40),game.player.Feedback,body);}
  if(game.player.overview&&game.markers!=null)for(int i=1;i<game.markers.Length;i++){var p=game.player.view.WorldToScreenPoint(game.markers[i]+Vector3.up*1.7f);if(p.z>0)GUI.Label(new Rect(p.x/Screen.width*1440,900-p.y/Screen.height*900,120,40),"● "+i,title);}
 }
 void SkyGUI(){
  if(sky.previewOnly)return;Init();GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
  if(sky.Paused){GUI.DrawTexture(new Rect(0,0,1440,900),shade);Panel(new Rect(380,270,680,350));GUI.Label(new Rect(420,312,600,70),"พักการเดินทาง",large);if(GUI.Button(new Rect(420,405,600,55),"เดินทางต่อ / Esc",button))sky.SetPaused(false);if(GUI.Button(new Rect(420,485,600,55),"เริ่มปริศนาเมืองใหม่",button)){sky.ResetAll();sky.SetPaused(false);}return;}
  Panel(new Rect(24,24,650,132));GUI.Label(new Rect(44,35,610,37),"02 / THE FORGOTTEN CITY",title);
  GUI.Label(new Rect(44,79,605,66),sky.IntroAccess?sky.puzzles.Objective():"ดูดรากที่ขวาง แล้วให้เวลาซ่อมเครื่องกำเนิด",body);
  Panel(new Rect(1080,24,336,185));GUI.Label(new Rect(1100,40,300,37),"พลังเวลา  "+Mathf.RoundToInt(sky.energy)+" / 100",title);GUI.DrawTexture(new Rect(1100,86,290,10),shade);GUI.DrawTexture(new Rect(1100,86,290*sky.energy/100,10),accent);GUI.Label(new Rect(1100,112,290,75),"เลื่อนลง: ดูดเวลา / ให้ผุ\nเลื่อนขึ้น: ให้เวลา / ซ่อม\nH คำใบ้ทีละขั้น",small);
  Panel(new Rect(24,730,1392,90));GUI.Label(new Rect(44,740,1300,28),"ROBO / ผู้ช่วยร่วมทาง",small);GUI.Label(new Rect(44,771,1335,44),string.IsNullOrEmpty(sky.Feedback)?sky.puzzles.Objective():sky.Feedback,body);
  GUI.Label(new Rect(24,846,1390,42),"WASD เดิน  Shift วิ่ง  Space กระโดด  เลื่อนลง ดูดเวลา / เลื่อนขึ้น ให้เวลา  H คำใบ้  F เข้าโรงงาน  R เริ่มใหม่  Esc พัก",small);
  GUI.Label(new Rect(710,438,30,30),"+",title);int i=sky.FocusedNode;if(i>=0){Panel(new Rect(425,546,590,118));GUI.Label(new Rect(445,555,550,37),sky.nodes[i].label,title);GUI.Label(new Rect(445,598,550,60),sky.AgeDescription(i)+"\n"+(sky.nodes[i].ambient?"เลื่อนลงเพื่อยืมเวลา / เลื่อนขึ้นเพื่อคืนสภาพ":sky.puzzles.Clue(i)),body);}
 }

}}
