#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BorrowedTime;
using BorrowedTime.Menu;
using BorrowedTime.Ending;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class EndingIntegrationValidation
{
    const string Active="BorrowedTime.EndingIntegration",Folder="Art/BorrowedTime/EndingIntegration";
    [Serializable]class Check{public string name;public bool passed;}
    [Serializable]class Report{public bool completed,success;public List<Check> checks=new List<Check>();}
    static Report report;static int stage;static double started,next;static int captured;
    static string SavePath=>Path.GetFullPath("Temp/EndingIntegration/save.json");
    static EndingIntegrationValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived+=Logged;}
    [MenuItem("Tools/Borrowed Time/Validate Ending Integration")]
    public static void Begin(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save the current scene before validation.");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("Temp/EndingIntegration");
        SessionState.SetString(Active+".Scene",EditorSceneManager.GetActiveScene().path);
        SessionState.SetBool(Active+".Background",Application.runInBackground);Application.runInBackground=true;
        report=new Report();Save();stage=0;started=next=0;captured=0;SessionState.SetBool(Active,true);
        EditorSceneManager.OpenScene("Assets/Scenes/BorrowedTimeMainMenu.unity");EditorApplication.isPlaying=true;
    }
    static void Tick(){
        const string request="Temp/EndingIntegration.request.txt";
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&File.Exists(request)){
            File.Delete(request);try{Begin();}catch(Exception e){File.WriteAllText("Temp/EndingIntegration.error.txt",e.ToString());Debug.LogException(e);}
        }
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        EditorApplication.QueuePlayerLoopUpdate();BTSaveGame.TestPathOverride=SavePath;
        if(report==null)report=JsonUtility.FromJson<Report>(File.ReadAllText(Folder+"/Validation.json"));
        if(started==0)started=EditorApplication.timeSinceStartup;
        try{
            if(EditorApplication.timeSinceStartup-started>150)throw new Exception("Ending integration timed out");
            if(EditorApplication.timeSinceStartup<next)return;
            var film=Object.FindFirstObjectByType<BTEndingCutscene>();var game=Object.FindFirstObjectByType<BTFactoryGame>();
            if(stage==0){
                var menu=Object.FindFirstObjectByType<BorrowedMainMenu>();if(!menu||Time.frameCount<8)return;
                CheckResult(!film&&menu.playSceneName=="Cutscene01_Mail","Main-menu Play still targets the opening film");
                BTJourney.HasMiracleSeed=true;BTJourney.LoadFactory(100,true,true);stage=1;next=EditorApplication.timeSinceStartup+.5;
            }
            else if(stage==1){
                if(!game||game.stage==0)return;
                CheckResult(!game.completed&&!film,"An unfinished factory does not start the ending");Complete(game,true);stage=2;
            }
            else if(stage==2){
                if(!film||film.elapsed<2)return;
                CheckResult(SceneManager.GetActiveScene().path==BTWorldEndingCinematic.EndingScenePath,"Finishing the factory loads the new 3D ending scene");
                CheckResult(!game&&!Object.FindFirstObjectByType<BTWorldEndingCinematic>(),"The old recovery cinematic is replaced");
                CheckResult(BTJourney.Ending=="Hopeful Renewal","Hopeful ending choice survives the scene transition");
                var data=JsonUtility.FromJson<BTSaveData>(File.ReadAllText(SavePath));
                CheckResult(data.completed&&data.scene==BTJourney.FactoryPath&&data.planted&&BTSaveGame.CanContinue,"Completion is saved before leaving the factory");
                var audio=film.GetComponent<BTEndingAudio>();
                CheckResult(audio.music.isPlaying&&audio.soundDesign.isPlaying&&audio.soundDesign.clip.length>=75,"The supplied music and rebuilt SFX play together");
                CheckResult(audio.music.GetComponent<MenuAudioBus>().channel==MenuAudioChannel.Music&&audio.soundDesign.GetComponent<MenuAudioBus>().channel==MenuAudioChannel.Effects,"Ending audio uses the existing Music and Effects settings");
                var menuAudio=Object.FindFirstObjectByType<MenuAudioFeedback>();
                CheckResult(menuAudio&&menuAudio.music&&!menuAudio.music.isPlaying,"Menu music pauses during the ending");stage=3;
            }
            else if(stage==3){
                float[] moments={3,32,58,68,75};
                if(captured<moments.Length&&film.elapsed>=moments[captured]){ScreenCapture.CaptureScreenshot(Folder+"/Ending_"+(int)moments[captured]+"s.png");captured++;}
                if(!film.Finished)return;
                var audio=film.GetComponent<BTEndingAudio>();
                CheckResult(audio.restarts==1,"Audio plays through all 76 seconds without repeated restarts");
                CheckResult(!audio.music.isPlaying&&!audio.soundDesign.isPlaying&&film.credits.text.Contains("Thank you"),"The film finishes on credits with audio stopped");
                film.Replay();stage=4;next=EditorApplication.timeSinceStartup+1;
            }
            else if(stage==4){
                CheckResult(!film.Finished&&film.GetComponent<BTEndingAudio>().music.isPlaying,"Replay restarts the film and soundtrack");
                film.SkipToCredits();CheckResult(film.Finished,"Skip reaches credits");film.ReturnToMainMenu();stage=5;
            }
            else if(stage==5){
                if(!Object.FindFirstObjectByType<BorrowedMainMenu>())return;
                var audio=Object.FindFirstObjectByType<MenuAudioFeedback>();
                CheckResult(Time.timeScale==1&&audio&&audio.music.isPlaying,"Returning to the main menu restores time and menu music");
                BTJourney.HasMiracleSeed=true;BTJourney.LoadFactory(100,true,false);stage=6;next=EditorApplication.timeSinceStartup+.5;
            }
            else if(stage==6){if(!game||game.stage==0)return;Complete(game,false);stage=7;}
            else if(stage==7){
                if(!film||film.elapsed<.5f)return;
                CheckResult(BTJourney.Ending=="Fragile Tomorrow","The alternate factory ending also loads the replacement film");
                BTQuickSkip.Advance();CheckResult(film.Finished,"F8 uses the new ending's Skip action");film.ReturnToMainMenu();stage=8;
            }
            else if(stage==8&&Object.FindFirstObjectByType<BorrowedMainMenu>())Finish();
        }catch(Exception e){CheckResult(false,e.ToString());Finish();Debug.LogException(e);}
    }
    static void Complete(BTFactoryGame game,bool hopeful){
        BTQuickSkip.ExitPreview();BTJourney.OptionalGardenRestored=false;BTJourney.CityGardenRestored=hopeful;
        game.stage=9;game.pollutionStopped=game.waterFlowRestored=true;game.nursery.age=hopeful?1:2;game.nursery.Apply();
        game.hasMiracleSeed=true;game.RefreshRoutes();
        if(!game.PlantSeed(true,out var reason))throw new Exception(reason);
        game.miracleTree.age=2;game.miracleTree.Apply();
    }
    static void CheckResult(bool value,string name){report.checks.Add(new Check{passed=value,name=name});Save();}
    static void Save()=>File.WriteAllText(Folder+"/Validation.json",JsonUtility.ToJson(report,true));
    static void Logged(string message,string stack,LogType type){if(SessionState.GetBool(Active,false)&&report!=null&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))CheckResult(false,message);}
    static void Finish(){
        report.completed=true;report.success=report.checks.All(c=>c.passed);Save();SessionState.SetBool(Active,false);SessionState.SetBool(Active+".Restore",true);
        Application.runInBackground=SessionState.GetBool(Active+".Background",false);EditorApplication.isPlaying=false;Debug.Log("[Borrowed Time] Ending integration: "+report.success);
    }
    static void Changed(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Active+".Restore",false))return;SessionState.SetBool(Active+".Restore",false);var path=SessionState.GetString(Active+".Scene","");if(File.Exists(path))EditorSceneManager.OpenScene(path);}
}
#endif
