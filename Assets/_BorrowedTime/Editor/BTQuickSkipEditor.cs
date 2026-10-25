#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
[InitializeOnLoad]
public static class BTQuickSkipEditor {
 static int phase;static double due;static bool running;static string log="";
 static BTQuickSkipEditor(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/Borrowed Time/Factory/Open factory preview with F8")]
 public static void OpenFactory(){
  if(EditorApplication.isPlaying){BTQuickSkip.LoadFactoryPreview();return;}
  if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene retained");
  EditorSceneManager.OpenScene(BTJourney.FactoryPath);SessionState.SetInt("BTF8Start",1);EditorApplication.isPlaying=true;
 }
 [MenuItem("Tools/Borrowed Time/Factory/Check F8 skips and factory preview")]
 public static void Check(){if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");EditorSceneManager.OpenScene(BTJourney.FactoryPath);SessionState.SetInt("BTF8Start",2);EditorApplication.isPlaying=true;}
 [MenuItem("Tools/Borrowed Time/Factory/Test mechanical route in F8 preview")]
 public static void CheckFactory(){if(EditorApplication.isPlaying)throw new Exception("Exit Play first");EditorSceneManager.OpenScene(BTJourney.FactoryPath);SessionState.SetInt("BTF8Start",3);EditorApplication.isPlaying=true;}
 static void Note(string line){log+=line+"\n";Directory.CreateDirectory(BTJourneyDirectionChecks.Out);File.WriteAllText(BTJourneyDirectionChecks.Out+"/F8Preview.txt",log);}
 static void Tick(){
  if(EditorApplication.isCompiling||!EditorApplication.isPlaying)return;
  try{
   int start=SessionState.GetInt("BTF8Start",0);
   if(start>0&&Object.FindFirstObjectByType<BTFactoryGame>()){
    SessionState.SetInt("BTF8Start",0);BTQuickSkip.PrepareFactoryInPlace();Application.runInBackground=true;
    if(start==1)return;
    if(start==3){BTFactoryManualPlaytest.Start(Object.FindFirstObjectByType<BTFactoryGame>(),true);return;}
    running=true;phase=0;due=EditorApplication.timeSinceStartup+25;log="";
    var previous=Keyboard.current;var probe=InputSystem.AddDevice<Keyboard>("F8PreviewProbe");probe.MakeCurrent();try{InputSystem.QueueStateEvent(probe,new KeyboardState(Key.F8));InputSystem.Update();if(!BTQuickSkip.ShortcutPressed())throw new Exception("F8 input binding failed");}finally{InputSystem.RemoveDevice(probe);if(previous!=null)previous.MakeCurrent();}
    Note("PASS F8 keyboard binding; preview factory has seed and energy 100");BTQuickSkip.LoadCityPreview();return;
   }
   if(!running)return;
   if(EditorApplication.timeSinceStartup>due)throw new Exception("F8 transition timeout phase "+phase);
   if(phase==0&&!BTQuickSkip.IsLoading&&SceneManager.GetActiveScene().path==BTJourney.CityPath&&Object.FindFirstObjectByType<BTSkyCityGame>()){
    if(!BTQuickSkip.PreviewSession||!BTJourney.HasMiracleSeed)throw new Exception("Level 1 skip lost seed");Note("PASS skip Level 1 → City with seed, preview isolated from saves");phase=1;due=EditorApplication.timeSinceStartup+25;BTQuickSkip.Advance();return;
   }
   var f=Object.FindFirstObjectByType<BTFactoryGame>();if(!f)return;
   if(phase==1&&!BTQuickSkip.IsLoading&&f.stage>0&&f.hasMiracleSeed){Note("PASS F8 Level 2 → playable Factory with seed");phase=2;due=EditorApplication.timeSinceStartup+25;BTQuickSkip.Advance();return;}
   if(phase==2&&f.completed&&f.GetComponent<BTWorldEndingCinematic>().Playing){Note("PASS F8 Factory → completed game and recovery cinematic");phase=3;due=EditorApplication.timeSinceStartup+25;BTQuickSkip.Advance();return;}
   if(phase==3&&f.completed&&f.GetComponent<BTWorldEndingCinematic>().Finished){Note("PASS F8 skips cinematic to ending card");phase=4;due=EditorApplication.timeSinceStartup+25;BTQuickSkip.LoadFactoryPreview();return;}
   if(phase==4&&!BTQuickSkip.IsLoading&&!f.completed&&f.stage>0&&f.hasMiracleSeed){running=false;Note("PASS returned to fresh Factory preview; seven alternative controls available");EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorSceneManager.OpenScene(BTJourney.FactoryPath);}
  }catch(Exception e){running=false;Note("FAIL "+e);EditorApplication.isPlaying=false;}
 }
}}
#endif
