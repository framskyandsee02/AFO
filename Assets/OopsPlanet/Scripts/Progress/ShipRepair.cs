using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
// v2 Hub goal (design §6): BLOB's crashed ship. Puzzles and hidden spots give ship parts; walking into the ship
// area installs what BLOB carries. Each installed part shows the next repair stage. When enough parts are
// installed - including at least one from every zone - the ship lifts off and the demo ends.
public class ShipRepair : MonoBehaviour {
    public BlobWorld world;
    public int partsToFinish=4;
    public string[] requiredZones={"hub","factory","rice"};
    public Bounds installArea=new Bounds(Vector3.zero,new Vector3(8,4,8));
    [Tooltip("Shown one after another as parts are installed (stage 1..n).")]
    public GameObject[] stageObjects;
    public Transform liftOff; public float liftHeight=14f;
    public ShipPart[] parts;
    [Header("State")]
    public List<string> collected=new List<string>(), installed=new List<string>();
    public bool launched;
    public int Stage => Mathf.Min(installed.Count,stageObjects!=null?stageObjects.Length:0);
    public event System.Action<ShipPart> Collected, Installed;
    float launchT; Vector3 liftStart; bool started;
    void Awake() { if(liftOff) liftStart=liftOff.localPosition; started=true; Apply(); }
    public ShipPart Find(string id) { if(parts!=null) foreach(var p in parts) if(p && p.id==id) return p; return null; }
    public bool Has(string id) => collected.Contains(id) || installed.Contains(id);
    public int Carried => collected.Count;
    public void Collect(ShipPart part) {
        if(!part || Has(part.id)) return;
        collected.Add(part.id); Collected?.Invoke(part);
        if(world && world.player) world.player.Notify("ได้ชิ้นส่วนยาน: "+part.nameTh+"  (นำกลับไปที่ยานใน Hub)",4);
        if(world) world.Save();
    }
    bool ZonesCovered(out string missing) {
        missing="";
        foreach(var z in requiredZones) {
            bool ok=false; foreach(var id in installed) { var p=Find(id); if(p && p.zone==z) ok=true; }
            if(!ok) missing+=(missing==""?"":", ")+ZoneTh(z);
        }
        return missing=="";
    }
    public static string ZoneTh(string z) => z=="hub"?"ฮับ":z=="factory"?"โรงงาน":z=="rice"?"นาข้าว":z;
    void Update() {
        if(!world || !world.player) return;
        if(launched) {
            if(liftOff && launchT<1) { launchT=Mathf.Min(1,launchT+Time.deltaTime/3f); liftOff.localPosition=liftStart+Vector3.up*liftHeight*Mathf.SmoothStep(0,1,launchT); }
            return;
        }
        if(collected.Count>0 && installArea.Contains(world.player.transform.position+Vector3.up*.5f)) InstallCarried();
    }
    public void InstallCarried() {
        if(collected.Count==0) return;
        foreach(var id in collected) { installed.Add(id); Installed?.Invoke(Find(id)); }
        collected.Clear(); Apply();
        string missing;
        if(installed.Count>=partsToFinish && ZonesCovered(out missing)) Launch();
        else if(world && world.player) {
            ZonesCovered(out missing);
            world.player.Notify("ซ่อมยาน "+installed.Count+" / "+partsToFinish+(missing!=""?"  •  ยังขาดชิ้นส่วนจาก: "+missing:""),4);
        }
        if(world) world.Save();
    }
    void Launch() {
        launched=true; launchT=0;
        Debug.Log("OOPS_SHIP_LAUNCH installed="+installed.Count);
        if(world) world.CompleteDemo("ยานซ่อมเสร็จแล้ว! BLOB บินขึ้นฟ้า");
    }
    void Apply() {
        if(!started) Awake();
        if(stageObjects!=null) for(int i=0;i<stageObjects.Length;i++) if(stageObjects[i]) stageObjects[i].SetActive(i<installed.Count);
        if(liftOff && !launched) liftOff.localPosition=liftStart;
    }
    public void ResetShip() { collected.Clear(); installed.Clear(); launched=false; launchT=0; Apply(); }
    public void WriteTo(SaveData d) { d.shipParts.AddRange(collected); d.installedParts.AddRange(installed); }
    public void ReadFrom(SaveData d) {
        collected.Clear(); installed.Clear();
        foreach(var id in d.shipParts) if(Find(id) && !Has(id)) collected.Add(id);
        foreach(var id in d.installedParts) if(Find(id) && !Has(id)) installed.Add(id);
        Apply();
    }
}
}
