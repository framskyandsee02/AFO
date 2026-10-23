using UnityEngine;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// What persists between game sessions. Deliberately small: puzzle completion, zone unlocks and settings.
// Not persisted (by design for the prototype): BLOB position/checkpoint, held powers, loose object positions.
[System.Serializable]
public class SaveData {
    // v2 (design v2): + discovered combo cells, ship parts collected / installed. Shortcuts are ZoneGates (unlockedZones).
    public const int CurrentVersion=2;
    public int version=CurrentVersion;
    public List<string> solvedPuzzles=new List<string>();
    public List<string> unlockedZones=new List<string>();
    public float lookSensitivity=.13f;
    public float masterVolume=1f;
    public float ambienceVolume=1f;
    public List<string> discoveredCombos=new List<string>();
    public List<string> shipParts=new List<string>();
    public List<string> installedParts=new List<string>();
}
// JSON save in Application.persistentDataPath/<slot>.json. Writes go through a temp file so a crash mid-write
// cannot leave a half-written save. A missing, corrupt or wrong-version file never throws: the game starts
// fresh and a corrupt file is kept as <slot>.corrupt.json for inspection (it is never deleted).
public static class SaveSystem {
    public const string DefaultSlot="oops_save";
    public enum LoadStatus { Ok, Missing, Corrupt, WrongVersion }
    public static string PathFor(string slot) => Path.Combine(Application.persistentDataPath,slot+".json");
    public static LoadStatus Load(string slot,out SaveData data) {
        data=new SaveData();
        string path=PathFor(slot);
        if(!File.Exists(path)) return LoadStatus.Missing;
        SaveData parsed=null;
        try {
            string json=File.ReadAllText(path);
            if(!string.IsNullOrWhiteSpace(json)) parsed=JsonUtility.FromJson<SaveData>(json);
        } catch(System.Exception e) { Debug.LogWarning("OOPS_SAVE corrupt ("+e.GetType().Name+"): "+path); }
        if(parsed==null) { KeepCorrupt(path); return LoadStatus.Corrupt; }
        if(parsed.version==1) { parsed.version=SaveData.CurrentVersion; Debug.Log("OOPS_SAVE migrated v1 -> v"+SaveData.CurrentVersion); }   // v1 had no v2 lists: they start empty
        if(parsed.version!=SaveData.CurrentVersion) { Debug.LogWarning("OOPS_SAVE version "+parsed.version+" != "+SaveData.CurrentVersion+": starting fresh"); return LoadStatus.WrongVersion; }
        if(parsed.discoveredCombos==null) parsed.discoveredCombos=new List<string>();
        if(parsed.shipParts==null) parsed.shipParts=new List<string>();
        if(parsed.installedParts==null) parsed.installedParts=new List<string>();
        if(parsed.solvedPuzzles==null) parsed.solvedPuzzles=new List<string>();
        if(parsed.unlockedZones==null) parsed.unlockedZones=new List<string>();
        parsed.lookSensitivity=Mathf.Clamp(parsed.lookSensitivity<=0?.13f:parsed.lookSensitivity,.02f,1f);
        parsed.masterVolume=Mathf.Clamp01(parsed.masterVolume);
        parsed.ambienceVolume=Mathf.Clamp01(parsed.ambienceVolume);
        data=parsed;
        return LoadStatus.Ok;
    }
    static void KeepCorrupt(string path) {
        try { File.Copy(path,Path.ChangeExtension(path,".corrupt.json"),true); } catch(System.Exception e) { Debug.LogWarning("OOPS_SAVE could not back up corrupt save: "+e.Message); }
    }
    public static bool Save(string slot,SaveData data) {
        string path=PathFor(slot), tmp=path+".tmp";
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(tmp,JsonUtility.ToJson(data,true));
            if(File.Exists(path)) File.Replace(tmp,path,null); else File.Move(tmp,path);
            return true;
        } catch(System.Exception e) { Debug.LogWarning("OOPS_SAVE write failed: "+e.Message); return false; }
    }
}
}
