#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BorrowedTime;
using BorrowedTime.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

/// <summary>Runs the complete opening and both Skip paths in the live Editor.</summary>
[InitializeOnLoad]
public static class OpeningCutsceneValidation
{
    const string Active="BorrowedTime.Opening.Validation", Folder="Art/BorrowedTime/Opening";
    [Serializable]class Check { public string name; public bool passed; }
    [Serializable]class Report { public bool completed,success;public List<Check> checks=new List<Check>(); }
    static Report report;
    static int stage;
    static double started;
    static double captureAt;
    static string pendingCapture;
    static readonly HashSet<string> shots=new HashSet<string>();
    static OpeningCutsceneValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived+=Logged;}
    [MenuItem("Tools/Borrowed Time/Validate Opening Cutscene")]
    public static void Begin()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save the open scene before running the opening validation.");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("Temp/OpeningValidation");
        SessionState.SetString(Active+".Scene",EditorSceneManager.GetActiveScene().path);
        SessionState.SetBool(Active+".Background",Application.runInBackground);
        Application.runInBackground=true;report=new Report();Save();shots.Clear();stage=0;started=0;
        SessionState.SetBool(Active,true);
        EditorSceneManager.OpenScene("Assets/Scenes/BorrowedTimeMainMenu.unity");EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        const string request="Temp/OpeningCutscene.request.txt";
        if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(request))
        {File.Delete(request);try{Begin();}catch(Exception e){File.WriteAllText("Temp/OpeningCutscene.error.txt",e.ToString());Debug.LogException(e);}}
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(report==null)report=JsonUtility.FromJson<Report>(File.ReadAllText(Folder+"/Validation.json"));
        if(started==0)started=EditorApplication.timeSinceStartup;
        BTSaveGame.TestPathOverride=Path.GetFullPath("Temp/OpeningValidation/save.json");
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if(EditorApplication.timeSinceStartup-started>145)throw new Exception("Opening sequence did not reach gameplay within 145 seconds");
            var presentation=Object.FindFirstObjectByType<CutscenePresentation>();
            if(presentation && !string.IsNullOrEmpty(presentation.Shot) && shots.Add(presentation.Shot))
            {CheckResult(true,"Reached shot: "+presentation.Shot);pendingCapture=presentation.Shot;captureAt=EditorApplication.timeSinceStartup+(pendingCapture=="delivery"?1.7:1);}
            if(stage==1&&pendingCapture!=null&&presentation&&presentation.Shot==pendingCapture&&EditorApplication.timeSinceStartup>=captureAt)
            {ScreenCapture.CaptureScreenshot(Folder+"/"+pendingCapture+".png");pendingCapture=null;}
            string scene=SceneManager.GetActiveScene().name;
            if(stage==0)
            {
                var menu=Object.FindFirstObjectByType<BorrowedMainMenu>();if(!menu||Time.frameCount<5)return;
                CheckResult(menu.playSceneName=="Cutscene01_Mail","New Play opens the first cutscene");
                var button=menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>("play-button");
                using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}stage=1;
            }
            else if(stage==1)
            {
                if(scene=="TrainingArea")
                {
                    foreach(string shot in new[]{"delivery","letter-delivered","collect-letter","return-home","reading-room","read-letter","time-door","new-journey","arrival"})
                        CheckResult(shots.Contains(shot),"Full opening includes "+shot);
                    CheckResult(Mathf.Approximately(Time.timeScale,1),"Opening restores normal gameplay time");
                    var game=Object.FindFirstObjectByType<BTTrainingGame>();
                    CheckResult(game&&game.player,"Opening hands control to the existing training game");
                    SceneManager.LoadScene("Cutscene01_Mail");stage=2;
                }
            }
            else if(stage==2)
            {var mail=Object.FindFirstObjectByType<CutsceneMailDelivery>();if(mail&&Time.timeSinceLevelLoad>.5f){mail.Presentation.RequestSkip();stage=3;}}
            else if(stage==3&&scene=="TrainingArea")
            {CheckResult(true,"Escape/Skip during delivery goes directly to training");SceneManager.LoadScene("Cutscene01_Mail");stage=4;}
            else if(stage==4)
            {var exterior=Object.FindFirstObjectByType<CutscenePanel2Exterior>();if(exterior&&exterior.p1.Presentation.Shot=="collect-letter"){exterior.p1.Presentation.RequestSkip();stage=5;}}
            else if(stage==5&&scene=="TrainingArea")
            {CheckResult(true,"Escape/Skip remains active during mailbox pickup");SceneManager.LoadScene("Cutscene02_Interior");stage=6;}
            else if(stage==6)
            {var interior=Object.FindFirstObjectByType<CutsceneInterior>();if(interior&&Time.timeSinceLevelLoad>.5f){interior.Presentation.RequestSkip();stage=7;}}
            else if(stage==7&&scene=="TrainingArea")
            {CheckResult(true,"Escape/Skip during the interior goes directly to training");Finish();}
        }
        catch(Exception e){CheckResult(false,e.ToString());Finish();Debug.LogException(e);}
    }
    static void CheckResult(bool passed,string name){report.checks.Add(new Check{passed=passed,name=name});Save();}
    static void Logged(string message,string stack,LogType type)
    {if(SessionState.GetBool(Active,false)&&report!=null&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))CheckResult(false,message);}
    static void Save()=>File.WriteAllText(Folder+"/Validation.json",JsonUtility.ToJson(report,true));
    static void Finish()
    {
        report.completed=true;report.success=report.checks.All(c=>c.passed);Save();
        SessionState.SetBool(Active,false);SessionState.SetBool(Active+".Restore",true);
        Application.runInBackground=SessionState.GetBool(Active+".Background",false);EditorApplication.isPlaying=false;
        Debug.Log("[Borrowed Time] Opening validation: "+report.success);
    }
    static void Changed(PlayModeStateChange change)
    {
        if(change!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Active+".Restore",false))return;
        SessionState.SetBool(Active+".Restore",false);string scene=SessionState.GetString(Active+".Scene","");
        if(File.Exists(scene))EditorSceneManager.OpenScene(scene);
    }
}
#endif
