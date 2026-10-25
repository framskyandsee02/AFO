#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
[InitializeOnLoad]
public static class BTDirectionPlayEvidence {
 public static bool OriginalBackground;static int shots;static bool city;
 public static bool SawAllShots=>shots==15;
 static BTDirectionPlayEvidence(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode){OriginalBackground=Application.runInBackground;shots=0;city=false;}if(s==PlayModeStateChange.ExitingPlayMode)Application.runInBackground=OriginalBackground;};}
 static void Tick(){if(!EditorApplication.isPlaying||(string.IsNullOrEmpty(BTSaveGame.TestPathOverride)&&!BTQuickSkip.PreviewSession))return;
  var sky=Object.FindFirstObjectByType<BTSkyCityGame>();if(sky&&!city&&sky.CurrentInput.move.sqrMagnitude>.5f&&Time.time>4){city=true;ScreenCapture.CaptureScreenshot(BTJourneyDirectionChecks.Out+"/City_Player_Robo_HUD.png");}
  var c=Object.FindFirstObjectByType<BTWorldEndingCinematic>();if(c&&c.Playing&&c.Elapsed>1){int flag=1<<c.Shot;if((shots&flag)==0){shots|=flag;ScreenCapture.CaptureScreenshot(BTJourneyDirectionChecks.Out+"/Ending_Shot_"+c.Shot+".png");File.WriteAllText(BTJourneyDirectionChecks.Out+"/EndingShots.txt","Observed shots bitmask="+shots+" elapsed="+c.Elapsed);}}
 }
}}
#endif
