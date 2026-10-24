using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace OopsPlanet {
public class FactoryExpansionTest : MonoBehaviour {
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

    public static bool Done; public static string Result; public int roundsToRun=2;public bool bossOnly;
    Vector3 V(string room,float x,float y,float z)=>FactoryLayout.Map(new Vector3(x,y,z),room);
    IEnumerator Start(){
        Done=false;w=GetComponent<BlobWorld>();p=w.player;p.testMode=true;p.paused=false;w.UseSaveSlot("test_factory_expansion",false);
        report.utc=System.DateTime.UtcNow.ToString("o");float started=Time.time;
        for(round=1;round<=roundsToRun;round++){if(!bossOnly)yield return Routes();yield return Boss();}
        round=0;if(!bossOnly){yield return Exploration();yield return Persistence();}
        Idle();report.seconds=Time.time-started;Result=JsonUtility.ToJson(report,true);Directory.CreateDirectory("Assets/Scenes/OopsPlanet/Evidence/FactoryExpansion");File.WriteAllText("Assets/Scenes/OopsPlanet/Evidence/FactoryExpansion/"+(bossOnly?"boss-test.json":"playtest.json"),Result);Done=true;
        Debug.Log("OOPS_EXPANSION_DONE passed="+report.passed+" failed="+report.failed);p.paused=true;
    }
    IEnumerator Routes(){
        yield return Case("f1_gate","Magnet",P1Magnet,"chute_a");
        yield return Case("f1_gate","Pressure",()=>Repeat(6,1.05f,()=>Seq(Powers(P,N),At(new Vector3(-.8f,0,37),new Vector3(-.8f,.6f,40.4f)),Press(false)),()=>Solved("f1_gate")),"chute_b");
        yield return Case("f1_gate","Heat",()=>Seq(Powers(H,N),At(new Vector3(2.4f,0,37),new Vector3(2.4f,.8f,39.6f)),Hold(false,2.5f),Wait(7)),"chute_d");
        yield return Case("f4_crane","Light weight",()=>Seq(Powers(M,N),CarryTo("Crane counterweight (6 kg, magnetic)",V("Crane",16,0,42.6f),V("Crane",12.5f,1,47.6f))),"WeightPlate");
        yield return Case("f4_crane","Heavy machine",()=>Seq(Powers(E,M),At(V("Crane",9.8f,0,43)),Press(true),Powers(M,E),CarryTo("Old press machine (30 kg, magnetic)",V("Crane",9.8f,0,43),V("Crane",12.5f,1,47.6f))),"WeightPlate");
        yield return Case("f4_crane","Cannon",P2Cannon,"Rust hatch");
        yield return Case("f2_boiler","Steam",()=>Seq(Powers(W,H),At(V("Boiler",10.6f,.6f,58),V("Boiler",10.6f,.6f,59.4f)),Hold(true,.9f),Wait(.4f),WalkFor(Vector2.up,1.8f)),"balcony");
        yield return Case("f2_boiler","Lift",()=>Seq(Powers(E,N),At(V("Boiler",18.1f,0,59.55f),C("Boiler cargo lift motor (machine)")),Press(false),Wait(3.5f),WalkFor(Vector2.up,1.4f)),"balcony");
        yield return Case("f2_boiler","Sling",()=>Seq(Powers(M,B),At(V("Boiler",9.2f,0,56),V("Boiler",9.2f,5.8f,62.8f)),Press(true),Wait(2.5f)),"balcony");
        yield return Case("f5_cooling","Raft",Raft,"north bank");
        yield return Case("f5_cooling","Bridge",()=>Seq(Powers(E,N),At(V("Cooling",-15.8f,0,43.2f),C("Service bridge motor (machine)")),Press(false),Wait(1.3f),Walk(V("Cooling",-15.8f,0,54.8f),5)),"north bank");
        yield return Case("f5_cooling","Girder bounce",()=>Seq(Powers(B,N),At(V("Cooling",-18.6f,0,48.7f)),Bounce(Vector2.up),WalkFor(Vector2.up,1.2f)),"north bank");
        yield return Case("f3_turbine","Pressure",()=>Repeat(7,1.05f,()=>Seq(Powers(P,N),At(V("Turbine",-15,0,60.2f),V("Turbine",-19.2f,3.6f,60.2f)),Press(false)),()=>Solved("f3_turbine")),"PressureTurbine");
        yield return Case("f3_turbine","Steam",()=>Seq(Powers(W,H),At(V("Turbine",-15.2f,0,64.4f),V("Turbine",-17.4f,.3f,64.4f)),Hold(true,2.5f)),"SteamInlet");
        yield return Case("f3_turbine","Chain",()=>Seq(Powers(E,B),At(V("Turbine",-18,0,54.2f),C("Vent switch 1")),Press(true)),"SwitchGroup");
        yield return Case("f6_assembly","Weld",()=>Seq(Powers(H,E),At(V("Assembly",-13.4f,0,37.6f),C("Cabinet drive rail (broken)")),Hold(true,1.2f)),"ComboLock");
        yield return Case("f6_assembly","Cannon",P6Cannon,"LatchedDoor");
        yield return Case("f6_assembly","Induction",()=>Seq(Powers(H,M),At(V("Assembly",-14.6f,0,34.6f),C("Conductor rod (metal)")),Hold(true,3),Wait(2.5f)),"ice-lock");
        yield return Wait(4);var clean=Pz("f6_assembly").GetComponent<FactoryRestoration>();CheckThat("Restoration animation completed",clean.debris.Length>0&&clean.Restored&&clean.Progress>=1&&clean.debris.All(d=>!d.gameObject.activeSelf)&&clean.obstacles.All(c=>!c.enabled),"debris="+clean.debris.Length+" surfaces="+clean.surfaces.Length);
    }
    IEnumerator Raft(){
        yield return Powers(W,M);var raft=Obj("Cooling raft (20 kg, magnetic)").GetComponent<ShuttleRaft>();
        yield return At(V("Cooling",-8.6f,0,43.3f),C(raft.name));p.testSecondary=true;yield return Until(()=>raft.state==ShuttleRaft.State.Docked,9);p.testSecondary=false;
        yield return Walk(V("Cooling",-8.6f,0,45.3f),3);yield return Until(()=>raft.state==ShuttleRaft.State.Docked&&!raft.atNear,6);yield return Walk(V("Cooling",-8.6f,0,54.8f),3);
    }
    IEnumerator BossFresh(){yield return Fresh();var fixture=new SaveData();fixture.solvedPuzzles.AddRange(new[]{"f1_gate","f4_crane","f2_boiler","f5_cooling","f3_turbine","f6_assembly"});SaveSystem.Save(w.saveSlot,fixture);w.LoadFromSlot(w.saveSlot);yield return Wait(.3f);}
    IEnumerator Boss(){
        yield return BossFresh();var boss=FindFirstObjectByType<FactoryRepairBoss>();int dead=w.deaths;
        yield return At(new Vector3(0,0,155),C("Boss jammed intake - physical scrap only"));yield return Powers(H,E);yield return Hold(true,.8f);CheckThat("Boss direct power rejected",boss.stage==0,"stage="+boss.stage);
        yield return BossFresh();yield return At(new Vector3(0,0,155));yield return Powers(M,P);var rb=boss.scrap[1].GetComponent<Rigidbody>();Aim(rb.worldCenterOfMass);p.testAction=true;yield return Wait(1.3f);Aim(new Vector3(0,4,174));yield return Walk(new Vector3(0,0,166),3);Aim(C("Boss jammed intake - physical scrap only"));yield return Wait(.5f);yield return Press(true);p.testAction=false;yield return Until(()=>boss.stage==1,3);
        CheckThat("Boss physical cannon impact",boss.stage==1&&boss.physicalImpacts>0,"stage="+boss.stage+" impacts="+boss.physicalImpacts+" deaths="+(w.deaths-dead));
        // Evade during station work: each attack remains active and visible; test only starts at action positions.
        yield return At(new Vector3(-9,0,161),C("Boss environmental cooling pump"));yield return Powers(W,N);yield return Hold(false,1.5f);
        CheckThat("Boss environmental water pump",boss.stage==2,"stage="+boss.stage+" cooling="+boss.cooling);
        yield return At(new Vector3(9,0,163),boss.repairSocket.position);yield return Powers(E,H);yield return Hold(true,.6f);CheckThat("Boss weld requires actual spare part",!boss.repaired,"stage="+boss.stage);
        // Physics fixture placement tests the socket check independently from magnetic transport, already exercised by cannon.
        var gear=boss.scrap[2].GetComponent<Rigidbody>();gear.position=boss.repairSocket.position+Vector3.up*.4f;gear.linearVelocity=Vector3.zero;yield return Wait(.5f);yield return Hold(true,1);yield return Until(()=>boss.objective.State==PuzzleState.Solved,2);
        CheckThat("Boss spare gear plus weld repairs objective",boss.repaired&&boss.objective.State==PuzzleState.Solved,"stage="+boss.stage+" objective="+boss.objective.State);
        yield return BossFresh();yield return At(new Vector3(0,0,155));yield return Until(()=>boss.warning.gameObject.activeSelf,5);yield return Walk(new Vector3(6,0,155),2);yield return Wait(1);CheckThat("Boss telegraphed slam can be dodged",w.deaths==0&&boss.attacks>0,"deaths="+w.deaths+" attacks="+boss.attacks);
        yield return Until(()=>boss.warning.gameObject.activeSelf,8);yield return Wait(2);CheckThat("Boss hit returns to checkpoint",w.deaths>0&&Vector3.Distance(p.transform.position,boss.checkpoint)<2,"pos="+p.transform.position+" deaths="+w.deaths);
    }
    IEnumerator Exploration(){
        yield return Fresh();int dead=w.deaths;yield return At(new Vector3(6.5f,0,42));
        foreach(var point in new[]{new Vector3(6.5f,0,57),new Vector3(0,0,57),new Vector3(0,0,61.5f),new Vector3(-26,0,61.5f),new Vector3(0,0,61.5f),new Vector3(0,0,69.6f),new Vector3(31,0,69.6f)}){
            yield return Walk(point,12);CheckThat("Walk corridor "+point,Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(point.x,point.z))<.7f&&w.deaths==dead,"pos="+p.transform.position.ToString("F1"));}
        yield return At(new Vector3(0,0,88));foreach(var point in new[]{new Vector3(0,0,104),new Vector3(10,0,104),new Vector3(10,0,122.6f),new Vector3(5.8f,0,122.6f)}){yield return Walk(point,12);CheckThat("Walk northern loop "+point,Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(point.x,point.z))<.7f&&w.deaths==dead,"pos="+p.transform.position.ToString("F1"));}
    }
    IEnumerator Persistence(){
        yield return Fresh();var seed=new SaveData();seed.solvedPuzzles.Add("hub_test_unknown");seed.installedParts.Add("outside_part");SaveSystem.Save(w.saveSlot,seed);w.Save();SaveData data;SaveSystem.Load(w.saveSlot,out data);CheckThat("Cross scene unknown progress preserved",data.solvedPuzzles.Contains("hub_test_unknown")&&data.installedParts.Contains("outside_part"),JsonUtility.ToJson(data));
        yield return Seq(Powers(H,E),At(V("Assembly",-13.4f,0,37.6f),C("Cabinet drive rail (broken)")),Hold(true,1.2f),Until(()=>Solved("f6_assembly"),3));w.Save();w.LoadFromSlot(w.saveSlot);yield return Wait(.5f);var clean=Pz("f6_assembly").GetComponent<FactoryRestoration>();CheckThat("Save load restores clean room instantly",Solved("f6_assembly")&&clean.Progress==1,"state="+Pz("f6_assembly").State+" progress="+clean.Progress);
        w.Restart();yield return Wait(.3f);SaveSystem.Load(w.saveSlot,out data);CheckThat("New game clears all scene progress",data.solvedPuzzles.Count==0&&data.installedParts.Count==0&&!clean.Restored,"puzzles="+data.solvedPuzzles.Count);
    }
    IEnumerator P1Magnet() {
        yield return Powers(M,N);
        for(int i=0;i<6 && !Solved("f1_gate");i++) {
            BlobMagnetic best=null; float bz=99;
            foreach(var m in BlobMagnetic.All) if(m.name.StartsWith("Chute A") && m.transform.position.z>37.6f && m.transform.position.z<bz) { bz=m.transform.position.z; best=m; }
            if(!best) break;
            yield return At(new Vector3(-2.4f,0,35.2f),best.Body.worldCenterOfMass);
            p.testAction=true; yield return Wait(1.2f);
            Aim(new Vector3(-6,1,33)); yield return Walk(new Vector3(-5,0,33.2f),3); p.testAction=false; yield return Wait(.6f);
        }
    }
    IEnumerator CarryTo(string body,Vector3 standAt,Vector3 dropOver) {
        var g=Obj(body); if(!g) yield break; var rb=g.GetComponent<Rigidbody>();
        yield return At(standAt,rb.worldCenterOfMass);
        p.testAction=true; yield return Wait(1.3f);
        Aim(dropOver+Vector3.up*.2f); yield return Walk(dropOver-new Vector3(0,0,2f),5); yield return Wait(.8f);
        p.testAction=false; yield return Wait(1.5f);
    }
    IEnumerator P2Cannon() {
        yield return Powers(M,P); var g=Obj("Crane counterweight (6 kg, magnetic)"); if(!g) yield break;
        yield return At(V("Crane",16f,0,42.6f),g.GetComponent<Rigidbody>().worldCenterOfMass);
        p.testAction=true; yield return Wait(1.3f);
        Aim(V("Crane",9.25f,1.4f,49f)); yield return Walk(V("Crane",9.25f,0,46.2f),5); yield return Wait(.6f);
        Aim(C("Rusty bolt (breaks on impact)")); yield return Wait(.4f);
        yield return Press(true); yield return Wait(.6f); p.testAction=false;
    }
    IEnumerator P6Cannon() {
        yield return Powers(M,P); var g=Obj("Spare gear (3 kg, magnetic)"); if(!g) yield break;
        yield return At(V("Assembly",-13f,0,35.4f),g.GetComponent<Rigidbody>().worldCenterOfMass);
        p.testAction=true; yield return Wait(1.3f);
        Aim(C("Rusty cabinet lock")); yield return Walk(V("Assembly",-14.2f,0,33.2f),3); yield return Wait(.5f);
        Aim(C("Rusty cabinet lock")); yield return Wait(.4f);
        yield return Press(true); yield return Wait(.8f); p.testAction=false;
    }
    IEnumerator Bounce(Vector2 move,float charge=.8f) { p.testJump=true; yield return Wait(charge); p.testMove=move; p.testJump=false; yield return null; }
}
}
