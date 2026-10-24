using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace OopsPlanet {
public class FactorySupplementTest:MonoBehaviour {
    [System.Serializable] public class Check { public int round; public string name; public bool passed; public string observed; }
    [System.Serializable] public class Report { public string utc; public List<Check> checks=new List<Check>(); public int passed, failed; public float seconds; }
    const BlobProperty M=BlobProperty.Magnet, P=BlobProperty.Pressure, B=BlobProperty.Bounce, W=BlobProperty.Water, E=BlobProperty.Electric, H=BlobProperty.Heat, N=BlobProperty.None;
    public int rounds=2;
    BlobWorld w; BlobPlayer p; Report report=new Report(); int round;
    readonly List<GameObject> aims=new List<GameObject>();
    float yaw0, pitch0;
    void CheckThat(string name,bool pass,string observed) {
        report.checks.Add(new Check{round=round,name=name,passed=pass,observed=observed});
        if(pass) report.passed++; else report.failed++;
        Debug.Log("OOPS_EXPANSION R"+round+" "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
    }
    // ---------- inputs ----------
    IEnumerator Wait(float s) { float until=Time.time+s; while(Time.time<until) yield return null; }
    void Idle() { p.testMove=Vector2.zero; p.testAction=false; p.testSecondary=false; p.testPush=false; p.testJump=false; p.testAbsorb=false; p.testSense=false; p.testAim=null; p.pitch=19; }
    void Aim(Vector3 pt) { var g=new GameObject("Factory test aim"); g.transform.position=pt; aims.Add(g); p.testAim=g.transform; p.AimAt(g.transform); }
    IEnumerator At(Vector3 pos,Vector3? aim=null) { Idle(); p.Teleport(pos+Vector3.up*.1f); if(aim.HasValue) Aim(aim.Value); yield return Wait(.35f); }
    IEnumerator Press(bool g) { if(g) p.testSecondary=true; else p.testAction=true; yield return null; yield return null; p.testSecondary=false; p.testAction=false; yield return null; }
    IEnumerator Hold(bool g,float s) { if(g) p.testSecondary=true; else p.testAction=true; yield return Wait(s); p.testSecondary=false; p.testAction=false; yield return null; }
    IEnumerator Walk(Vector3 goal,float timeout=6f) {
        float until=Time.time+timeout;
        while(Time.time<until) { Vector3 d=goal-p.transform.position; d.y=0; if(d.magnitude<.3f) break; p.testMove=new Vector2(d.x,d.z).normalized; yield return null; }
        p.testMove=Vector2.zero;
    }
    IEnumerator WalkFor(Vector2 dir,float s) { p.testMove=dir; yield return Wait(s); p.testMove=Vector2.zero; }
    IEnumerator Until(System.Func<bool> c,float t) { float until=Time.time+t; while(Time.time<until && !c()) yield return null; }
    IEnumerator Seq(params IEnumerator[] steps) { foreach(var s in steps) yield return StartCoroutine(s); }
    IEnumerator Powers(BlobProperty a,BlobProperty b) { p.SetProperties(a,b); yield return null; }
    IEnumerator Repeat(int n,float gap,System.Func<IEnumerator> step,System.Func<bool> done) { for(int i=0;i<n && !done();i++) { yield return StartCoroutine(step()); yield return Wait(gap); } }
    // ---------- lookups ----------
    PuzzleObjective Pz(string id) { foreach(var o in w.puzzles) if(o && o.id==id) return o; return null; }
    GameObject Obj(string name) => GameObject.Find(name);
    Vector3 C(string name) { var g=Obj(name); if(!g) return Vector3.zero; var c=g.GetComponentInChildren<Collider>(); return c?c.bounds.center:g.transform.position; }
    WorldMaterial Mat(string name) { foreach(var m in WorldMaterial.All) if(m.name==name) return m; return null; }
    bool Solved(string id) { var z=Pz(id); return z && z.State==PuzzleState.Solved; }
    IEnumerator Fresh() { Idle(); w.Restart(); p.testMode=true; foreach(var a in aims) if(a) Destroy(a); aims.Clear(); yield return Wait(.3f); }
    IEnumerator Case(string id,string way,System.Func<IEnumerator> steps,string expectBy=null,float settle=4f) {
        yield return Fresh();
        var pz=Pz(id); if(!pz) { CheckThat(id+" — "+way,false,"puzzle missing"); yield break; }
        int deaths=w.deaths; float t0=Time.time;
        yield return StartCoroutine(steps());
        yield return Until(()=>pz.State==PuzzleState.Solved,settle);
        bool ok=w.deaths==deaths && pz.State==PuzzleState.Solved && (expectBy==null || pz.solvedBy.Contains(expectBy));
        CheckThat((pz.isSecret?"Hidden ":"")+id+" — "+way,ok,"state="+pz.State+" solvedBy="+pz.solvedBy+" t="+(Time.time-t0).ToString("F1")+"s pos="+p.transform.position.ToString("F1")+" respawns="+(w.deaths-deaths));
        Idle();
    }


    public static bool Done;public static string Summary;
    IEnumerator Start(){
        w=GetComponent<BlobWorld>();p=w.player;p.testMode=true;p.paused=false;w.UseSaveSlot("test_factory_supplement",false);round=0;yield return Fresh();yield return Wait(5);
        CheckThat("Receiving does not solve by falling off missing floor",!Solved("f1_gate"),"state="+Pz("f1_gate").State);
        yield return At(new Vector3(-36,0,75.5f));yield return Wait(1);CheckThat("Walking around trench does not commission a crossing system",!Solved("f5_cooling"),"state="+Pz("f5_cooling").State);
        yield return At(new Vector3(0,0,155));yield return Wait(1);CheckThat("Boss waits for all six repair points",!FindFirstObjectByType<FactoryRepairBoss>().active,"active="+FindFirstObjectByType<FactoryRepairBoss>().active);
        var seed=new SaveData();seed.solvedPuzzles.AddRange(new[]{"f1_gate","f4_crane","f2_boiler","f5_cooling","f3_turbine","f6_assembly","factory_boss_repaired"});SaveSystem.Save(w.saveSlot,seed);w.LoadFromSlot(w.saveSlot);yield return Wait(.5f);
        var restorations=FindObjectsByType<FactoryRestoration>(FindObjectsSortMode.None);foreach(var c in restorations){CheckThat("Loaded clean "+c.roomName,c.Restored&&c.Progress==1&&c.debris.Length>0,"debris="+c.debris.Length+" colliders="+c.obstacles.Length);c.Apply(false);}yield return Wait(1);
        CheckThat("Cleaning has intermediate animation frames",restorations.All(c=>c.Progress>0&&c.Progress<1),"progress="+restorations[0].Progress);yield return Wait(4);
        CheckThat("All rooms finish clean with no invisible ruin collider",restorations.Sum(c=>c.obstacles.Length)>0&&restorations.All(c=>c.Progress==1&&c.debris.All(t=>!t.gameObject.activeSelf)&&c.obstacles.All(t=>!t.enabled)),"rooms="+restorations.Length);
        foreach(var c in restorations)c.objective.ResetPuzzle(false);CheckThat("Puzzle reset keeps completed room restored",restorations.All(c=>c.Restored),"rooms="+restorations.Length);
        // Travel through solved doorways and the long upper loop using movement, no waypoint teleports inside a leg.
        foreach(var route in new[]{
          new[]{new Vector3(6.5f,0,36.5f),new Vector3(6.5f,0,43)},
          new[]{new Vector3(35.5f,0,71),new Vector3(35.5f,0,78),new Vector3(35.5f,0,98)},
          new[]{new Vector3(31,5.1f,108.5f),new Vector3(27,5,108.5f),new Vector3(5.1f,5,108.5f),new Vector3(5.1f,5,83.5f),new Vector3(5.1f,5,67.4f),new Vector3(5.1f,0,60.4f),new Vector3(1,0,62)},
          new[]{new Vector3(-36,0,75),new Vector3(-36,0,98)},
          new[]{new Vector3(-31.5f,0,104),new Vector3(-25,0,104)},
          new[]{new Vector3(2,0,123),new Vector3(2,0,124.7f),new Vector3(0,0,124.7f),new Vector3(0,0,140)}
        }){
            yield return At(route[0]);int deaths=w.deaths;for(int i=1;i<route.Length;i++){yield return Walk(route[i],15);Vector3 d=p.transform.position-route[i];d.y=0;CheckThat("Door/return walk "+route[i],d.magnitude<.8f&&Mathf.Abs(p.transform.position.y-route[i].y)<1.5f&&w.deaths==deaths,"pos="+p.transform.position.ToString("F1"));}
        }
        // A real magnetic transport into the repair bench, separately from combat timing.
        var boss=FindFirstObjectByType<FactoryRepairBoss>();var gear=boss.scrap[2].GetComponent<Rigidbody>();yield return At(new Vector3(5,0,153),gear.worldCenterOfMass);yield return Powers(M,N);p.testAction=true;yield return Wait(1.3f);Aim(boss.repairSocket.position);yield return Walk(new Vector3(9,0,164),5);yield return Wait(.8f);CheckThat("Spare gear held above tray",p.powers.held&&p.powers.held.Body==gear,"gear="+gear.position);p.testAction=false;yield return Wait(1);
        CheckThat("Real magnet carries spare gear onto repair bench",Vector3.Distance(gear.worldCenterOfMass,boss.repairSocket.position)<1.35f&&gear.linearVelocity.magnitude<3,"gear="+gear.position+" distance="+Vector3.Distance(gear.worldCenterOfMass,boss.repairSocket.position));
        w.Restart();yield return Wait(.2f);CheckThat("New game restores room debris and blockers",restorations.All(c=>!c.Restored&&c.debris.All(t=>t.gameObject.activeSelf)&&c.obstacles.All(t=>t.enabled)),"rooms="+restorations.Length);
        Idle();p.paused=true;Summary=JsonUtility.ToJson(report,true);Directory.CreateDirectory("Assets/Scenes/OopsPlanet/Evidence/FactoryExpansion");File.WriteAllText("Assets/Scenes/OopsPlanet/Evidence/FactoryExpansion/supplement-test.json",Summary);Done=true;Debug.Log("FACTORY_SUPPLEMENT_DONE passed="+report.passed+" failed="+report.failed);
    }
}
}
