#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
[InitializeOnLoad]
public static class BTFullGamePlaytest {
    static readonly List<string> log=new List<string>();
    static BTFactoryGame factory;
    static bool running,oldBackground,journeyPassed,secondLeg,awaitingSecond,seedAbsorbAttempted;
    static bool boilerControlCleared,valveCageCleared;
    static int step;
    static float stepTime,castAt,protectedEnergy;
    static double started;
    public static string Status="Idle";
    static BTFullGamePlaytest(){EditorApplication.update+=Tick;}
    public static void Start(){
        if(!EditorApplication.isPlaying || Object.FindFirstObjectByType<BTCityGame>() || Object.FindFirstObjectByType<BTFactoryGame>())throw new Exception("Begin in Play Mode from TrainingArea");
        oldBackground=Application.runInBackground;Application.runInBackground=true;started=EditorApplication.timeSinceStartup;log.Clear();factory=null;step=-1;journeyPassed=secondLeg=awaitingSecond=false;running=true;Status="Running all three levels";
        boilerControlCleared=valveCageCleared=false;BTJourneyPlaytest.Start();Note("Fresh full game started from training; actual wheel controls");
    }
    static void Note(string s){log.Add((EditorApplication.timeSinceStartup-started).ToString("F2")+"s | "+s);}
    static void Save(){File.WriteAllText("Assets/_BorrowedTime/Evidence/FullGamePlaytest.txt",string.Join("\n",log));}
    static void Next(string s){Note("PASS FACTORY "+(secondLeg?"B":"A")+" / "+step+" "+s);step++;stepTime=Time.time;castAt=Time.time+.6f;}
    static void Finish(bool ok,string s){
        Status=(ok?"PASS ":"FAIL ")+s;Note(Status);running=false;
        if(factory&&factory.player)factory.player.inputOverride=null;
        Application.runInBackground=oldBackground;Save();Debug.Log("BORROWED_TIME_FULL_GAME "+Status);
    }
    public static void ContinueForSecondEnding(){
        if(!awaitingSecond || !factory)throw new Exception("First ending has not completed");
        factory.ResetTraining();
        if(!factory.hasMiracleSeed || !BTJourney.HasMiracleSeed || !BTJourney.OptionalGardenRestored)throw new Exception("Factory restart lost actual journey seed / city choice");
        Note("PASS User R reset path restored factory initial state and preserved incoming seed and city restoration");
        started=EditorApplication.timeSinceStartup;secondLeg=true;awaitingSecond=false;boilerControlCleared=valveCageCleared=false;step=0;stepTime=Time.time;factory.player.inputOverride=Input;Status="Running factory again for Fragile Tomorrow";
    }
    static void Tick(){
        if(!running)return;
        if(!EditorApplication.isPlaying){Finish(false,"Play Mode ended");return;}
        if(awaitingSecond)return;
        if(EditorApplication.timeSinceStartup-started>700){Finish(false,"Overall timeout");return;}
        if(!journeyPassed && BTJourneyPlaytest.Status.StartsWith("FAIL")){Finish(false,BTJourneyPlaytest.Status);return;}
        if(!journeyPassed && BTJourneyPlaytest.Status.StartsWith("PASS")){journeyPassed=true;Note("PASS Training and City completed on foot; optional city garden restored");}
        if(!factory){
            var f=Object.FindFirstObjectByType<BTFactoryGame>();if(!f || f.stage==0)return;
            factory=f;
            if(!journeyPassed || !BTJourney.FromCity || !factory.hasMiracleSeed || !BTJourney.CityWaterRestored || !BTJourney.CityGardenRestored){Finish(false,"City → Factory transition contract failed");return;}
            Note("PASS Real scene transition City → Factory preserved seed, water, garden and energy="+factory.player.energy.ToString("F1"));
            if(factory.CanPlant(out string reason)){Finish(false,"Planting accepted before factory shutdown");return;}
            if(factory.miracleTree.Absorb(100)!=0){Finish(false,"Miracle seed allowed absorption");return;}
            Note("PASS Planting blocked before pollution / water prerequisites; miracle seed cannot supply energy");
            step=0;stepTime=Time.time;factory.player.inputOverride=Input;return;
        }
        if(factory.completed){
            string expected=secondLeg?"Fragile Tomorrow":"Hopeful Renewal";
            if(factory.ending!=expected || !factory.seedPlanted || !factory.pollutionStopped || !factory.waterFlowRestored || factory.miracleTree.age!=2 || BTJourney.HasMiracleSeed || !BTJourney.SeedPlanted){Finish(false,"Ending state mismatch "+factory.ending);return;}
            Note("PASS "+expected+" reached through actual controls; walked="+factory.player.walked.ToString("F1")+"m energy="+factory.player.energy.ToString("F1"));
            factory.player.inputOverride=null;
            if(!secondLeg){awaitingSecond=true;Status="AWAIT_SECOND_ENDING · Hopeful Renewal passed";Save();return;}
            Finish(true,"Training → City → Factory → planted seed; both ending variants verified");return;
        }
        if(Time.time-stepTime>32)Finish(false,"Timeout FACTORY "+step+" pos="+factory.player.transform.position+" target="+(factory.player.target?factory.player.target.displayName:"none")+" feedback="+factory.player.feedback);
    }
    static bool Move(Vector3 point,ref BTPlayer.FrameInput f,float tolerance=.33f){
        Vector3 d=point-factory.player.transform.position;d.y=0;if(d.magnitude<tolerance)return true;
        var local=Quaternion.Inverse(Quaternion.Euler(0,factory.player.yaw,0))*d.normalized;f.move=new Vector2(local.x,local.z);return false;
    }
    static bool Aim(BTTimeTarget t,ref BTPlayer.FrameInput f){
        Vector3 d=t.aimPoint.position-factory.player.view.transform.position;
        float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
        f.look=new Vector2(Mathf.DeltaAngle(factory.player.yaw,yaw)*.5f/.14f,-(pitch-factory.player.pitch)*.5f/.12f);f.aim=true;return factory.player.target==t;
    }
    static void Cast(BTTimeTarget t,int goal,ref BTPlayer.FrameInput f){if(Aim(t,ref f)&&Time.time>castAt){f.scroll=t.age>goal?-120:t.age<goal?120:0;castAt=Time.time+.7f;}}
    static BTTimeTarget Source(Vector3 p){
        BTTimeTarget best=null;float min=1000;foreach(var t in Object.FindObjectsByType<BTTimeTarget>(FindObjectsSortMode.None))if(t.kind==TargetKind.Plant){float d=Vector3.Distance(t.transform.position,p);if(d<min){min=d;best=t;}}return best;
    }
    static BTPlayer.FrameInput Input(){
        var f=new BTPlayer.FrameInput();if(!running||!factory||awaitingSecond)return f;
        switch(step){
            case 0:if(Move(new Vector3(-5,0,-23),ref f))Next("Reached lobby living source");break;
            case 1:if(Aim(Source(new Vector3(-5,0,-25)),ref f))f.absorb=true;if(factory.player.energy>=90)Next("Refilled incoming gauntlet through living energy");break;
            case 2:if(factory.funFirst){Note("PASS v2.1 entry does not require optional relay repairs");step=8;stepTime=Time.time;break;}if(Move(new Vector3(-10,0,-25),ref f))Next("Reached left relay");break;
            case 3:Cast(factory.entryRelays[0],0,ref f);if(factory.entryRelays[0].age==0)Next("Left relay rewound one state");break;
            case 4:if(Move(new Vector3(-7,0,-24),ref f))Next("Cleared left relay casing");break;
            case 5:if(Move(new Vector3(-7,0,-21),ref f))Next("Moved along control aisle");break;
            case 6:if(Move(new Vector3(10,0,-21),ref f))Next("Reached right relay");break;
            case 7:Cast(factory.entryRelays[1],0,ref f);if(factory.entryRelays[1].age==0)Next("Right relay rewound one state");break;
            case 8:if(Move(new Vector3(-1.5f,0,-24.4f),ref f))Next("Reached intake panel approach");break;
            case 9:Cast(factory.entryControl,1,ref f);if(factory.entryControl.age==1)Next("Intake control repaired");break;
            case 10:f.interact=true;if(factory.entryOpen)Next("F opened intake gate after all relay prerequisites");break;
            case 11:if(Move(new Vector3(1.5f,0,-22),ref f))Next("Went around control cabinet");break;
            case 12:if(Move(new Vector3(0,0,-15),ref f))Next("Walked through intake gate");break;
            case 13:if(Move(new Vector3(-16,0,-10.7f),ref f))Next("Reached transport bridge approach");break;
            case 14:Cast(factory.bridge,0,ref f);if(factory.bridge.age==0)Next("Transport bridge rewound twice");break;
            case 15:if(Move(new Vector3(-17,0,-6.5f),ref f))Next("Entered real bridge deck");break;
            case 16:if(Move(new Vector3(-17,0,2.5f),ref f))Next("Crossed restored transport bridge");break;
            case 17:if(Move(new Vector3(-21,0,3),ref f))Next("Reached raw material recovery planter");break;
            case 18:if(Aim(Source(new Vector3(-22,0,4)),ref f))f.absorb=true;if(factory.player.energy>=85)Next("Recharged before elevator");break;
            case 19:if(Move(new Vector3(-20.9f,0,7),ref f))Next("Reached elevator controls");break;
            case 20:Cast(factory.liftControl,1,ref f);if(factory.liftControl.age==1)Next("Repaired freight elevator control");break;
            case 21:if(Move(new Vector3(-18,0,7),ref f))Next("Approached open side of elevator");break;
            case 22:if(Move(new Vector3(-18,0,10),ref f))Next("Walked onto freight platform");break;
            case 23:f.interact=true;if(factory.freightLift.moving)Next("F started freight lift");break;
            case 24:if(factory.freightLift.AtHigh && !factory.freightLift.moving && factory.player.transform.position.y>5.9f)Next("Moving platform carried real CharacterController to floor 6");break;
            case 25:if(Move(new Vector3(-18,6,6.5f),ref f))Next("Walked off lift onto upper landing");break;
            case 26:if(Move(new Vector3(-21,6,6.5f),ref f))Next("Reached upper service planter");break;
            case 27:if(Aim(Source(new Vector3(-22.5f,6,6)),ref f))f.absorb=true;if(factory.player.energy>=99.9f)Next("Upper source filled gauntlet");break;
            case 28:if(Move(new Vector3(-18,6,5),ref f))Next("Entered upper catwalk");break;
            case 29:if(Move(new Vector3(-18,6,-11),ref f))Next("Walked west catwalk");break;
            case 30:if(Move(new Vector3(16,6,-11),ref f))Next("Crossed south transfer catwalk");break;
            case 31:if(Move(new Vector3(11,6,-7.5f),ref f))Next("Reached conveyor controller");break;
            case 32:Cast(factory.conveyor,2,ref f);if(factory.conveyor.age==2)Next("Accelerated conveyor twice; cargo moved into bay");break;
            case 33:if(Move(new Vector3(20,6,-2.7f),ref f))Next("Reached conveyor release panel");break;
            case 34:f.interact=true;if(factory.conveyorOpen)Next("F opened conveyor access door");break;
            case 35:if(Move(new Vector3(16,6,-3),ref f))Next("Aligned with access doorway");break;
            case 36:if(Move(new Vector3(16,6,4.5f),ref f))Next("Walked into boiler hall");break;
            case 37:if(Move(new Vector3(21,6,4.5f),ref f))Next("Reached boiler recovery planter");break;
            case 38:if(Aim(Source(new Vector3(23,6,4.5f)),ref f))f.absorb=true;if(factory.player.energy>=99.9f)Next("Recharged for three pressure repairs");break;
            case 39:if(factory.funFirst){if(Move(new Vector3(18.5f,6,5.5f),ref f)){Note("PASS boiler repairs remain optional; critical route open");step=44;stepTime=Time.time;}}else if(Move(new Vector3(16,6,5.5f),ref f))Next("Reached boiler targeting aisle");break;
            case 40:Cast(factory.boilers[0],0,ref f);if(factory.boilers[0].age==0)Next("Boiler 1 rewound twice");break;
            case 41:Cast(factory.boilers[1],0,ref f);if(factory.boilers[1].age==0)Next("Boiler 2 rewound twice");break;
            case 42:Cast(factory.boilers[2],0,ref f);if(factory.boilers[2].age==0)Next("Boiler 3 rewound twice");break;
            case 43:if(Move(new Vector3(18.5f,6,5.5f),ref f))Next("Entered gap between boilers");break;
            case 44:if(Move(new Vector3(18.5f,6,13.3f),ref f))Next("Walked behind pressure vessels");break;
            case 45:if(Move(new Vector3(16,6,13.3f),ref f))Next("Reached vent controls");break;
            case 46:if(factory.funFirst){step=47;stepTime=Time.time;break;}f.interact=true;if(factory.pressureBalanced)Next("F balanced pressure after all three repairs");break;
            case 47:
                if(!boilerControlCleared){if(Move(new Vector3(18.5f,6,16.5f),ref f))boilerControlCleared=true;}
                else if(Move(new Vector3(16,6,21.5f),ref f))Next("Walked around vent desk and through pressure door");break;
            case 48:if(factory.funFirst){step=56;stepTime=Time.time;break;}if(Move(new Vector3(22,6,21.5f),ref f))Next("Went around power coupler");break;
            case 49:if(Move(new Vector3(22,6,24.2f),ref f))Next("Reached power room living source");break;
            case 50:if(Aim(Source(new Vector3(22,6,26.5f)),ref f))f.absorb=true;if(factory.player.energy>=85)Next("Recharged after boiler repairs");break;
            case 51:if(Move(new Vector3(22,6,21.5f),ref f))Next("Returned to power aisle");break;
            case 52:if(Move(new Vector3(17,6,21.5f),ref f))Next("Reached coupler approach");break;
            case 53:Cast(factory.powerCoupler,2,ref f);if(factory.powerCoupler.age==2)Next("Accelerated power coupler to stable state");break;
            case 54:if(Move(new Vector3(16,6,22.3f),ref f))Next("Reached ring bridge control");break;
            case 55:f.interact=true;if(factory.powerLatched)Next("F energized and extended ring bridge");break;
            case 56:if(factory.emergencyWalkway && factory.emergencyWalkway.age!=0){Cast(factory.emergencyWalkway,0,ref f);break;}if(Move(new Vector3(12,6,25),ref f))Next("Reached ring bridge threshold");break;
            case 57:if(Move(new Vector3(0,6,25),ref f))Next("Walked onto ring bridge collider");break;
            case 58:if(Move(new Vector3(-12,6,25),ref f))Next("Crossed into main control room");break;
            case 59:if(Move(new Vector3(-22,6,25.7f),ref f))Next("Reached control room living source");break;
            case 60:if(Aim(Source(new Vector3(-22,6,28)),ref f))f.absorb=true;if(factory.player.energy>=90)Next("Recharged for shutdown operations");break;
            case 61:if(factory.funFirst){Note("PASS direct shutdown path does not require three auxiliary valves");step=69;stepTime=Time.time;break;}if(Move(new Vector3(-19,6,21.8f),ref f))Next("Reached shutoff valve 1");break;
            case 62:f.interact=true;if(factory.valvesClosed[0])Next("Closed valve 1");break;
            case 63:
                if(!valveCageCleared){if(Move(new Vector3(-19,6,22.7f),ref f))valveCageCleared=true;}
                else if(Move(new Vector3(-16.5f,6,22.7f),ref f))Next("Exited lift cage opening and cleared valve 1 pipe");break;
            case 64:if(Move(new Vector3(-16.5f,6,26.2f),ref f))Next("Reached rear valve aisle");break;
            case 65:if(Move(new Vector3(-19,6,26.2f),ref f))Next("Reached shutoff valve 2");break;
            case 66:f.interact=true;if(factory.valvesClosed[1])Next("Closed valve 2");break;
            case 67:if(Move(new Vector3(-14,6,26.5f),ref f))Next("Reached shutoff valve 3");break;
            case 68:f.interact=true;if(factory.valvesClosed[2])Next("Closed valve 3");break;
            case 69:if(Move(new Vector3(-10.5f,6,21.8f),ref f))Next("Reached damaged main console");break;
            case 70:Cast(factory.coreConsole,1,ref f);if(factory.coreConsole.age==1)Next("Repaired main pollution control console");break;
            case 71:f.interact=true;if(factory.pollutionStopped)Next("F stopped active factory pollution after repaired control module");break;
            case 72:if(factory.funFirst && factory.waterFlowRestored){Note("PASS clean water carried from city after pollution shutdown");step=76;stepTime=Time.time;break;}if(Move(new Vector3(-12.5f,6,22),ref f))Next("Cleared shutdown console");break;
            case 73:if(Move(new Vector3(-12.5f,6,27.5f),ref f))Next("Reached clean water aisle");break;
            case 74:if(Move(new Vector3(-9.5f,6,27.4f),ref f))Next("Reached water distribution panel");break;
            case 75:f.interact=true;if(factory.waterFlowRestored)Next("F restored safe water to planting chamber");break;
            case 76:if(Move(new Vector3(-16.5f,6,25),ref f))Next("Returned through control aisle");break;
            case 77:if(Move(new Vector3(-21,6,25),ref f))Next("Went around shutoff valves");break;
            case 78:if(Move(new Vector3(-20,6,23),ref f))Next("Reached upper elevator threshold");break;
            case 79:if(Move(new Vector3(-20,6,20),ref f))Next("Walked onto core lift");break;
            case 80:f.interact=true;if(factory.coreLift.moving)Next("F started core lift after shutdown and water restoration");break;
            case 81:if(factory.coreLift.AtHigh&&!factory.coreLift.moving&&factory.player.transform.position.y>11.8f)Next("Second real lift carried player to floor 12");break;
            case 82:if(Move(new Vector3(-20,12,24),ref f))Next("Exited core elevator");break;
            case 83:if(Move(new Vector3(0,12,24),ref f))Next("Crossed upper core walkway");break;
            case 84:if(Move(new Vector3(0,12,18),ref f))Next("Entered planting chamber on bridge");break;
            case 85:if(Move(new Vector3(5,12,19.2f),ref f))Next("Reached final living energy source");break;
            case 86:if(Aim(Source(new Vector3(5,12,17)),ref f))f.absorb=true;if(factory.player.energy>=99.9f)Next("Prepared enough living energy for seed and growth");break;
            case 87:if(secondLeg){Next("Chose direct planting without restoring optional nursery");}else if(Move(new Vector3(-5,12,10.5f),ref f))Next("Reached optional nursery");break;
            case 88:if(secondLeg){Next("Nursery remains unrestored through player choice");}else{Cast(factory.nursery,1,ref f);if(factory.nurseryRestored)Next("Restored optional nursery using real time power");}break;
            case 89:if(Move(new Vector3(0,12,9.5f),ref f))Next("Reached seed planting approach");break;
            case 90:f.interact=true;if(factory.seedPlanted){protectedEnergy=factory.player.energy;seedAbsorbAttempted=false;Next("F planted and consumed carried seed only after valid prerequisites");}break;
            case 91:
                if(Aim(factory.miracleTree,ref f)){f.absorb=true;seedAbsorbAttempted=true;}
                if(seedAbsorbAttempted&&Time.time-stepTime>.9f){if(Mathf.Abs(factory.player.energy-protectedEnergy)>.01f||factory.miracleTree.age!=0){Finish(false,"Seed absorption protection failed");break;}Next("Actual aimed absorption from miracle seed refused; energy and age unchanged");}break;
            case 92:Cast(factory.miracleTree,2,ref f);break;
        }
        return f;
    }
}
}
#endif
