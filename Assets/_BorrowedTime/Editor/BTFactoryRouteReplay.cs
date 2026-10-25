#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
[InitializeOnLoad]
public static class BTFactoryRouteReplay {
 const string Incoming="Library/BTFactory_ActualIncoming.json",Working="Library/BTFactory_ReplayWorking.json";
 public static bool Replaying{get;private set;}static bool loading;
 static BTFactoryRouteReplay(){EditorApplication.update+=Tick;}
 public static void Capture(BTFactoryGame game){if(Replaying||string.IsNullOrEmpty(BTSaveGame.TestPathOverride))return;BTSaveGame.Save(game);if(File.Exists(BTSaveGame.PathName))File.Copy(BTSaveGame.PathName,Incoming,true);}
 [MenuItem("Tools/Borrowed Time/Factory/Test/Replay factory from actual incoming checkpoint")]
 public static void Replay(){if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");if(!File.Exists(Incoming))throw new Exception("Complete the real Training to City to Factory transition first");EditorSceneManager.OpenScene(BTJourney.FactoryPath);SessionState.SetBool("BTFactoryReplayPending",true);EditorApplication.isPlaying=true;}
 static void Tick(){
  if(!EditorApplication.isPlaying)return;
  if(SessionState.GetBool("BTFactoryReplayPending",false)&&!loading){SessionState.SetBool("BTFactoryReplayPending",false);Replaying=true;Application.runInBackground=true;File.Copy(Incoming,Working,true);BTSaveGame.TestPathOverride=Path.GetFullPath(Working);if(!BTSaveGame.Continue())throw new Exception("Actual incoming checkpoint could not load");loading=true;return;}
  if(loading&&BTSaveGame.Notice.StartsWith("โหลดบันทึก")){loading=false;BTFactoryCampaignRoute.StartWaiting();}
 }
}}
#endif
