using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BorrowedTime {
// Explicit data only: no scene object instance IDs are written to disk.
[Serializable] public class BTTargetSave { public string path; public int age; public float life;public bool stressed; }
[Serializable] public class BTSaveData {
    public int version=1,stage;public string scene,crossing,ending;public Vector3 checkpoint;public float energy,absorbed;
    public bool seed,fromTraining,fromCity,cityWater,cityGarden,optional,planted,pollution,nursery,completed,bridgeLearned,vineLearned;
    public bool[] flags,valves;public float[] lifts;public BTTargetSave[] targets;
}
public static class BTSaveGame {
    static BTSaveData pending;static float requested=-1;
    public static float SavedUntil;public static string Notice="";
    public static string PathName => Path.Combine(Application.persistentDataPath,"borrowed-time-v21.json");
    public static bool Exists => File.Exists(PathName);
    public static void Request(BTTrainingGame g){if(g && g.player && g.player.inputOverride==null && !(g is BTFunPrototypeGame))requested=Time.unscaledTime+.8f;}
    public static void Tick(BTTrainingGame g){
        if(pending!=null && SceneManager.GetActiveScene().path==pending.scene){var data=pending;pending=null;Restore(g,data);requested=-1;}
        if(requested>=0 && Time.unscaledTime>=requested){requested=-1;Save(g);}
    }
    public static void Save(BTTrainingGame g){
        if(!g || !g.player || g.stage==0 || g is BTFunPrototypeGame || g.player.inputOverride!=null)return;
        try {
            var d=new BTSaveData{scene=g.gameObject.scene.path,stage=g.stage,checkpoint=g.player.checkpoint,energy=g.player.energy,absorbed=g.absorbed,seed=g.hasMiracleSeed,fromTraining=BTJourney.FromTraining,fromCity=BTJourney.FromCity,cityWater=BTJourney.CityWaterRestored,cityGarden=BTJourney.CityGardenRestored,optional=BTJourney.OptionalGardenRestored,planted=BTJourney.SeedPlanted,pollution=BTJourney.PollutionStopped,nursery=BTJourney.FinalNurseryRestored,ending=BTJourney.Ending,crossing=BTJourney.CrossingMethod,completed=g.completed,bridgeLearned=g.bridgeLearned,vineLearned=g.vineLearned};
            var targets=UnityEngine.Object.FindObjectsByType<BTTimeTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None);d.targets=new BTTargetSave[targets.Length];
            for(int i=0;i<targets.Length;i++)d.targets[i]=new BTTargetSave{path=Key(targets[i].transform),age=targets[i].age,life=targets[i].life,stressed=targets[i].pollutionStress};
            var c=g as BTCityGame;if(c)d.flags=new[]{c.waterRestored,c.gardenRestored,c.optionalRestored,c.observedRelapse,c.factoryOpen,c.optionalWatered};
            var f=g as BTFactoryGame;if(f){d.flags=new[]{f.entryOpen,f.transportRestored,f.conveyorOpen,f.pressureBalanced,f.powerLatched,f.pollutionStopped,f.waterFlowRestored,f.seedPlanted,f.nurseryRestored};d.valves=(bool[])f.valvesClosed.Clone();d.lifts=new[]{f.freightLift.platform.position.y,f.coreLift.platform.position.y};}
            Directory.CreateDirectory(Application.persistentDataPath);File.WriteAllText(PathName+".tmp",JsonUtility.ToJson(d,true));File.Copy(PathName+".tmp",PathName,true);
            Notice="บันทึกแล้ว • C เล่นต่อจากเมนูพัก";SavedUntil=Time.unscaledTime+3;
        }catch(Exception e){Notice="บันทึกไม่สำเร็จ: "+e.Message;SavedUntil=Time.unscaledTime+5;Debug.LogWarning(Notice);}
    }
    public static bool Continue(){
        try{if(!Exists)return false;var d=JsonUtility.FromJson<BTSaveData>(File.ReadAllText(PathName));
            if(d==null||d.version!=1||(d.scene!=BTJourney.TrainingPath&&d.scene!=BTJourney.CityPath&&d.scene!=BTJourney.FactoryPath))return false;
            pending=d;requested=-1;SceneManager.LoadSceneAsync(d.scene);return true;
        }catch(Exception e){Debug.LogWarning("Save could not be loaded: "+e.Message);return false;}
    }
    static string Key(Transform t){string k=t.name;while(t.parent){t=t.parent;k=t.name+"/"+k;}return k;}
    static void Restore(BTTrainingGame g,BTSaveData d){
        BTJourney.HasMiracleSeed=d.seed;BTJourney.FromTraining=d.fromTraining;BTJourney.FromCity=d.fromCity;BTJourney.CityWaterRestored=d.cityWater;BTJourney.CityGardenRestored=d.cityGarden;BTJourney.OptionalGardenRestored=d.optional;BTJourney.SeedPlanted=d.planted;BTJourney.PollutionStopped=d.pollution;BTJourney.FinalNurseryRestored=d.nursery;BTJourney.Ending=d.ending;BTJourney.CrossingMethod=d.crossing;BTJourney.CarriedEnergy=d.energy;
        foreach(var t in UnityEngine.Object.FindObjectsByType<BTTimeTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var s in d.targets)if(s.path==Key(t.transform)){t.age=Mathf.Clamp(s.age,0,t.states==null?0:Mathf.Max(0,t.states.Length-1));t.life=Mathf.Clamp(s.life,t.minimumLife,t.maxLife);t.pollutionStress=s.stressed;t.Apply();break;}
        g.hasMiracleSeed=d.seed;g.absorbed=d.absorbed;g.bridgeLearned=d.bridgeLearned;g.vineLearned=d.vineLearned;g.completed=d.completed;g.stage=d.stage;
        var c=g as BTCityGame;if(c && d.flags!=null){c.waterRestored=d.flags[0];c.gardenRestored=d.flags[1];c.optionalRestored=d.flags[2];c.observedRelapse=d.flags[3];c.factoryOpen=d.flags[4];c.optionalWatered=d.flags[5];c.crossingMethod=d.crossing;c.RefreshAfterLoad();}
        var f=g as BTFactoryGame;if(f && d.flags!=null){f.entryOpen=d.flags[0];f.transportRestored=d.flags[1];f.conveyorOpen=d.flags[2];f.pressureBalanced=d.flags[3];f.powerLatched=d.flags[4];f.pollutionStopped=d.flags[5];f.waterFlowRestored=d.flags[6];f.seedPlanted=d.flags[7];f.nurseryRestored=d.flags[8];f.valvesClosed=d.valves;f.ending=d.ending;f.RefreshAfterLoad(d.lifts);}
        g.player.energy=Mathf.Clamp(d.energy,0,100);g.player.checkpoint=d.checkpoint;g.player.paused=false;g.player.Respawn();if(g.seed)g.seed.gameObject.SetActive(!g.hasMiracleSeed);
        Cursor.lockState=g.completed?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=g.completed;
        Notice="โหลดบันทึกแล้ว • กลับที่จุดพักปลอดภัย";SavedUntil=Time.unscaledTime+4;
    }
}
}
