#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace BorrowedTime.Editor {
[InitializeOnLoad]
public static class BTFactoryManualPlaytest {
 class Step{public string text;public Vector3 point;public Func<bool> ready;public bool interact;}
 static BTFactoryGame game;static bool running,oldBackground,preview;static int index;static float since,initialEnergy;static double began;static List<Step> steps;static readonly List<string> log=new List<string>();
 static BTFactoryManualPlaytest(){EditorApplication.update+=Tick;}
 public static void Start(BTFactoryGame restored,bool previewRun=false){preview=previewRun;game=restored;game.ResetTraining();if(!game.hasMiracleSeed)throw new Exception("Public factory reset lost actual carried seed");oldBackground=Application.runInBackground;Application.runInBackground=true;initialEnergy=game.player.energy;began=EditorApplication.timeSinceStartup;index=0;since=Time.time;log.Clear();steps=new List<Step>();running=true;
  void Go(float x,float y,float z,string text){steps.Add(new Step{text=text,point=new Vector3(x,y,z)});}
  void F(string text,Func<bool> ready){steps.Add(new Step{text=text,ready=ready,interact=true});}
  void Wait(string text,Func<bool> ready){steps.Add(new Step{text=text,ready=ready});}
  var routes=game.GetComponent<BTFactoryRoutes>();
  Go(5.3f,0,-24,"approach manual intake");F("open intake mechanically",()=>game.entryOpen&&routes.choices[0]==2);
  Go(0,0,-15,"walk through intake");Go(-12,0,-13.7f,"approach transport winch");F("winch transport bridge without time power",()=>routes.choices[1]==2&&game.transportRestored);
  Go(-17,0,-10.7f,"align bridge");Go(-17,0,2.5f,"cross physically restored bridge");Go(-15,0,3.8f,"approach lift backup");F("connect backup lift motor",()=>routes.LiftBackup);
  Go(-18,0,7,"approach freight cage");Go(-18,0,10,"board lift");F("activate freight lift using backup",()=>game.freightLift.moving);Wait("ride actual freight platform",()=>game.freightLift.AtHigh&&!game.freightLift.moving&&game.player.transform.position.y>5.8f);
  Go(-18,6,6,"exit lift");Go(-18,6,-11,"west catwalk");Go(16,6,-11,"south catwalk");Go(22,6,-8.3f,"approach hand brake");F("release conveyor mechanically",()=>routes.choices[3]==2&&game.conveyorOpen);
  Go(16,6,-3,"align conveyor doorway");Go(16,6,5,"enter boiler hall");Go(18.5f,6,5.5f,"enter maintenance aisle");Go(18.5f,6,16.5f,"pass boilers");Go(16,6,21.5f,"enter power room");Go(11,6,19.9f,"approach ring winch");F("haul ring bridge mechanically",()=>routes.choices[4]==2);
  Go(14,6,20,"clear winch pedestal");Go(14,6,25,"align ring crossing");Go(-12,6,25,"cross real ring bridge");
  Go(-19,6,21.8f,"approach pollution valve 1");F("close valve 1",()=>game.valvesClosed[0]);Go(-19,6,22.7f,"clear valve");Go(-16.5f,6,22.7f,"exit lift cage aisle");Go(-16.5f,6,26.2f,"rear valve aisle");Go(-19,6,26.2f,"approach valve 2");F("close valve 2",()=>game.valvesClosed[1]);Go(-14,6,26.5f,"approach valve 3");F("close valve 3",()=>game.valvesClosed[2]);
  Go(-10.5f,6,21.8f,"approach shutdown");F("stop factory through valves with console still damaged",()=>game.pollutionStopped&&game.coreConsole.age==2);
  Go(-16.5f,6,25,"return control aisle");Go(-21,6,25,"avoid valve equipment");Go(-20,6,23,"core elevator threshold");Go(-20,6,20,"board core lift");F("start core lift",()=>game.coreLift.moving);Wait("ride actual core platform",()=>game.coreLift.AtHigh&&!game.coreLift.moving&&game.player.transform.position.y>11.8f);
  Go(-20,12,24,"exit upper lift");Go(0,12,24,"cross upper bridge");Go(0,12,18,"enter seed chamber");Go(-5,12,19.6f,"approach nursery irrigation");F("restore nursery through clean water",()=>routes.choices[6]==2&&game.nurseryRestored);Go(0,12,19,"return centre");Go(4,12,12.5f,"approach seed irrigation");F("plant and grow using safe factory water and light",()=>routes.choices[7]==2&&game.seedPlanted);Wait("reach ending and watch complete recovery cutscene",()=>game.completed&&game.GetComponent<BTWorldEndingCinematic>().Finished);
  game.player.inputOverride=Input;Note(preview?"START isolated F8 Factory preview through actual controller; no absorption or time casts":"START second route through public R reset after Continue; no absorption or time casts");
 }
 static void Note(string s){log.Add((EditorApplication.timeSinceStartup-began).ToString("F2")+"s | "+s);File.WriteAllLines(BTJourneyDirectionChecks.Out+"/ManualFactoryRoute.txt",log);}
 static void Finish(bool ok,string message){running=false;if(game)game.player.inputOverride=null;Note((ok?"PASS ":"FAIL ")+message);Application.runInBackground=oldBackground;BTSaveGame.CancelPendingContinue();if(ok){if(preview){BTSaveGame.TestPathOverride=null;return;}BTFactoryFinalChecks.CheckContinue(game,true);return;}BTSaveGame.TestPathOverride=null;EditorApplication.isPlaying=false;}
 static void Tick(){if(!running)return;if(!EditorApplication.isPlaying){Finish(false,"Play Mode interrupted");return;}if(game.completed&&game.GetComponent<BTWorldEndingCinematic>().Finished){if(Mathf.Abs(game.player.energy-initialEnergy)>.01f){Finish(false,"Mechanical route changed glove energy");return;}Finish(true,"All seven alternatives, three-valve shutdown, lifts and ending completed on foot; glove energy unchanged="+initialEnergy);return;}if(Time.time-since>38)Finish(false,"Manual route timeout "+index+" "+steps[index].text+" pos="+game.player.transform.position+" feedback="+game.player.Feedback);}
 static BTPlayer.FrameInput Input(){var f=new BTPlayer.FrameInput();if(!running)return f;try{
  if(index>=steps.Count){if(Mathf.Abs(game.player.energy-initialEnergy)>.01f)throw new Exception("Mechanical route consumed or generated glove energy");if(game.ending!="Hopeful Renewal")throw new Exception("Irrigated nursery did not affect ending");Finish(true,"All seven alternatives, three-valve shutdown, lifts and ending completed on foot; glove energy unchanged="+initialEnergy);return f;}
  var s=steps[index];bool done=false;if(s.ready!=null){done=s.ready();if(!done)f.interact=s.interact;}else{Vector3 d=s.point-game.player.transform.position;d.y=0;done=d.magnitude<.33f;if(!done){var q=Quaternion.Inverse(Quaternion.Euler(0,game.player.yaw,0))*d.normalized;f.move=new Vector2(q.x,q.z);f.sprint=true;}}
  if(done){Note("PASS "+index+" "+s.text);index++;since=Time.time;}
 }catch(Exception e){Finish(false,e.ToString());}return f;}
}}
#endif
