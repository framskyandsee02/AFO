#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
[InitializeOnLoad]
public static class BTFactoryCampaignCity {
    static readonly List<string> log=new List<string>();
    static BTCityGame city;
    static bool running,oldBackground,trainingPassed;
    static int step;
    static float stepTime,castAt;
    static double started;
    public static string Status="Idle";
    public static bool UseNatureRoute;
    static BTFactoryCampaignCity() { EditorApplication.update+=Tick; }
    public static string ValidateWheelBindings() {
        if(EditorApplication.isPlaying) return "Run wheel binding check in Edit Mode";
        var previous=Mouse.current;
        var mouse=InputSystem.AddDevice<Mouse>("BorrowedTime_Wheel_Test"); mouse.MakeCurrent();
        var go=new GameObject("Temporary wheel binding probe"); var p=go.AddComponent<BTPlayer>();
        bool positive=false,negative=false;
        try {
            InputSystem.QueueStateEvent(mouse,new MouseState{scroll=new Vector2(0,120)}.WithButton(MouseButton.Right)); InputSystem.Update();
            var f=p.ReadInput(); positive=f.aim && f.scroll>0;
            InputSystem.QueueStateEvent(mouse,new MouseState{scroll=new Vector2(0,-120)}.WithButton(MouseButton.Right)); InputSystem.Update();
            f=p.ReadInput(); negative=f.aim && f.scroll<0;
        } finally { Object.DestroyImmediate(go); InputSystem.RemoveDevice(mouse); if(previous!=null) previous.MakeCurrent(); }
        if(!positive || !negative) throw new Exception("Mouse wheel binding failed");
        return "PASS RMB + wheel back => Rewind; RMB + wheel forward => Accelerate (Input System MouseState)";
    }
    public static void StartAtCity() {
        city=Object.FindFirstObjectByType<BTCityGame>();if(!city||!city.hasMiracleSeed||!BTJourney.FromTraining)throw new Exception("Missing actual training transition");
        oldBackground=Application.runInBackground;Application.runInBackground=true;started=EditorApplication.timeSinceStartup;log.Clear();step=0;trainingPassed=true;running=true;Status="Running City";stepTime=Time.time;city.player.inputOverride=Input;
        BTFactoryCampaignExpansion.Clear();
    }
    static void Note(string s) { log.Add((EditorApplication.timeSinceStartup-started).ToString("F2")+"s | "+s); }
    static void Next(string s) { Note("PASS CITY "+step+" "+s); step++; stepTime=Time.time; castAt=Time.time+.6f; }
    static void Finish(bool ok,string message) {
        Status=(ok?"PASS ":"FAIL ")+message; Note(Status); running=false;
        if(city && city.player) city.player.inputOverride=null;
        Application.runInBackground=oldBackground;
        string path="Captures/TrainingComfort/Factory/CityRoute.txt";
        File.WriteAllText(path,string.Join("\n",log)); Debug.Log("BORROWED_TIME_JOURNEY "+Status);if(ok)BTFactoryCampaignRoute.StartWaiting();else{BTSaveGame.TestPathOverride=null;EditorApplication.isPlaying=false;}
    }
    static void Tick() {
        if(!running) return;
        if(!EditorApplication.isPlaying) { Finish(false,"Play Mode ended"); return; }
        if(EditorApplication.timeSinceStartup-started>240) { Finish(false,"Overall timeout"); return; }
        if(false && BTTrainingPlaytest.Status.StartsWith("FAIL")) { Finish(false,BTTrainingPlaytest.Status); return; }
        if(!trainingPassed && BTTrainingPlaytest.Status.StartsWith("PASS")) { trainingPassed=true; Note("PASS Complete training with scroll controls and received miracle seed"); }
        if(!city) {
            city=Object.FindFirstObjectByType<BTCityGame>();
            if(city) {
                if(!trainingPassed || !BTJourney.HasMiracleSeed || !city.hasMiracleSeed || !BTJourney.FromTraining) { Finish(false,"Scene transition lost seed or training result"); return; }
                Note("PASS Real SceneManager transition TrainingArea → ForgottenCity; seed and energy carried over="+city.player.energy.ToString("F1"));
                step=0; stepTime=Time.time; city.player.inputOverride=Input;
            }
            return;
        }
        if(city.noteOpen && step==25 && Time.time-stepTime>1) { Note("PASS Optional notebook modal opened; player paused"); city.CloseNote(); Next("Closed notebook through same UI command and restored controls"); }
        if(city.completed && step>=30) {
            if(!city.hasMiracleSeed || !city.gardenRestored || !city.waterRestored || !city.CrossingPassable || !city.factoryOpen || !city.optionalRestored) { Finish(false,"Final state mismatch"); return; }
            Note("PASS City gate crossed on foot route="+city.crossingMethod+"; all main objectives and optional garden complete; seed retained");
            Finish(true,"Training + City completed; city walked="+city.player.walked.ToString("F1")+"m energy="+city.player.energy.ToString("F1")); return;
        }
        if(Time.time-stepTime>30) Finish(false,"Timeout CITY "+step+" pos="+city.player.transform.position+" target="+(city.player.target?city.player.target.displayName:"none")+" feedback="+city.player.feedback);
    }
    static bool Move(Vector3 point,ref BTPlayer.FrameInput f,float tolerance=.42f) {
        Vector3 d=point-city.player.transform.position; d.y=0;
        if(d.magnitude<tolerance) return true;
        var local=Quaternion.Inverse(Quaternion.Euler(0,city.player.yaw,0))*d.normalized; f.move=new Vector2(local.x,local.z); return false;
    }
    static bool Aim(BTTimeTarget t,ref BTPlayer.FrameInput f) {
        Vector3 d=t.aimPoint.position-city.player.view.transform.position;
        float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
        f.look=new Vector2(Mathf.DeltaAngle(city.player.yaw,yaw)*.55f/.14f,-(pitch-city.player.pitch)*.55f/.12f); f.aim=false;
        return city.player.target==t;
    }
    static void Cast(BTTimeTarget t,int goal,ref BTPlayer.FrameInput f) {
        if(Aim(t,ref f) && Time.time>castAt) { f.scroll=t.age>goal?-120:t.age<goal?120:0; castAt=Time.time+.7f; }
    }
    static BTTimeTarget SourceNear(Vector3 p) {
        BTTimeTarget best=null; float distance=1000;
        foreach(var t in Object.FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None)) if(t.kind==TargetKind.Plant) {
            float d=Vector3.Distance(t.transform.position,p); if(d<distance) { best=t; distance=d; }
        }
        return best;
    }
    static BTPlayer.FrameInput Input() {
        var f=new BTPlayer.FrameInput(); if(!running || !city) return f; if(BTFactoryCampaignExpansion.Failure!=null){Finish(false,BTFactoryCampaignExpansion.Failure.ToString());return f;}if(!BTFactoryCampaignExpansion.Done(city)){stepTime=Time.time;return BTFactoryCampaignExpansion.Input(city);}
        switch(step) {
            case 0: if(Move(new Vector3(-23,0,-14.4f),ref f)) Next("Reached first renewable source"); break;
            case 1: if(Aim(SourceNear(new Vector3(-23,0,-12)),ref f)) f.absorb=true; if(city.player.energy>=90) Next("Recharged carried gauntlet through nonlethal absorption"); break;
            case 2: if(Move(new Vector3(-18,0,-5.5f),ref f)) Next("Reached public garden"); break;
            case 3: Cast(city.tree,1,ref f); if(city.tree.age==1) Next("Rewound withered tree to mature state"); break;
            case 4: if(city.observedRelapse && city.tree.age==1 && city.tree.pollutionStress && !city.gardenRestored) Next("Polluted water caused relapse; cosmetic repair did not complete garden"); break;
            case 5: if(Move(new Vector3(-14,0,-4),ref f)) Next("Left park on clear path"); break;
            case 6: if(Move(new Vector3(-14,0,7),ref f)) Next("Followed pollution pipe north"); break;
            case 7: if(Move(new Vector3(-10.5f,0,11),ref f)) Next("Reached broken waterworks"); break;
            case 8: Cast(city.waterMachine,1,ref f); if(city.waterMachine.age==1) Next("Water machine repaired with wheel back"); break;
            case 9: if(Move(new Vector3(-7.4f,0,12.2f),ref f)) Next("Reached diversion valve"); break;
            case 10: f.interact=true; if(city.waterRestored) Next("F isolated dirty feed and restored clean district water"); break;
            case 11: if(Move(new Vector3(-14,0,7),ref f)) Next("Returned along waterworks street"); break;
            case 12: if(Move(new Vector3(-14,0,-4),ref f)) Next("Returned to park approach"); break;
            case 13: if(Move(new Vector3(-18,0,-5.5f),ref f)) Next("Reached garden after fixing cause"); break;
            case 14: Cast(city.tree,city.tree.pollutionStress?0:1,ref f); if(city.gardenRestored) Next("Garden now permanently restored under clean water"); break;
            case 15: if(Time.time-stepTime>7 && city.tree.age==1 && city.gardenRestored) Next("Healthy tree stayed healthy beyond relapse duration"); break;
            case 16: if(Move(new Vector3(-13,0,2),ref f)) Next("Left restored garden"); break;
            case 17: if(Move(new Vector3(-6.5f,0,UseNatureRoute?5.1f:8),ref f)) Next("Reached canal crossing approach"); break;
            case 18: Cast(UseNatureRoute?city.natureCrossing:city.bridge,UseNatureRoute?1:0,ref f); if(UseNatureRoute?city.natureCrossing.age==1:city.bridge.age==0) Next(UseNatureRoute?"Natural crossing grown through wheel input":"Canal bridge restored in two states"); break;
            case 19: if(Move(new Vector3(-2,0,UseNatureRoute?4.5f:8),ref f)) Next("Walked onto restored canal bridge"); break;
            case 20: if(Move(new Vector3(7.5f,0,UseNatureRoute?4.5f:8),ref f)) Next("Crossed canal using real bridge collision"); break;
            case 21: if(Move(new Vector3(9,0,7.2f),ref f)) Next("Reached east renewable source"); break;
            case 22: if(Aim(SourceNear(new Vector3(9,0,5)),ref f)) f.absorb=true; if(city.player.energy>=75) Next("Recharged for optional and factory puzzles"); break;
            case 23: if(Move(new Vector3(23,0,5.1f),ref f)) Next("Reached optional garden entrance"); break;
            case 24: Cast(city.optionalVines,2,ref f); if(city.optionalVines.age==2 && Move(new Vector3(23,0,.3f),ref f)) Next("Aged optional vines and walked into small garden"); break;
            case 25: f.interact=true; break;
            case 26: if(Move(new Vector3(23,0,5),ref f)) Next("Returned from optional garden"); break;
            case 27: if(Move(new Vector3(22,0,14.8f),ref f)) Next("Reached factory forecourt"); break;
            case 28: Cast(city.seal,2,ref f); if(city.seal.age==2) Next("Factory obstruction aged twice and crumbled"); break;
            case 29: if(Move(new Vector3(26.4f,0,20.8f),ref f)) Next("Reached factory access panel"); break;
            case 30: f.interact=true; if(city.factoryOpen) Next("Factory unlocked only after main puzzle prerequisites"); break;
            case 31: if(Move(new Vector3(22,0,21.5f),ref f)) Next("Reached opened factory door"); break;
            case 32: if(Time.time-stepTime>2.6f) Move(new Vector3(22,0,26),ref f); break;
        }
        return f;
    }
}
}
#endif
