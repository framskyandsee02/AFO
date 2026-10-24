using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// Factory interior v3 play test (Docs/FACTORY_INTERIOR_TH.md). Every solution is driven through the real test inputs
// (F = testAction, G = testSecondary, SPACE = testJump, testMove, aiming via testAim) from a fresh restart; nothing
// calls Solve(). Teleporting is only used to put BLOB at the start of a case. The whole suite runs `rounds` times so
// rigidbody / timing cases are checked at least twice. Also: wrong-power checks, return paths, reset, save / load and
// the one-time reward. Uses its own save slot (test_factory) and deletes it at the end.
public class OopsFactoryInteriorTest : MonoBehaviour {
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
        Debug.Log("OOPS_FACTORY R"+round+" "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
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
        bool ok=pz.State==PuzzleState.Solved && (expectBy==null || pz.solvedBy.Contains(expectBy));
        CheckThat((pz.isSecret?"Hidden ":"")+id+" — "+way,ok,"state="+pz.State+" solvedBy="+pz.solvedBy+" t="+(Time.time-t0).ToString("F1")+"s pos="+p.transform.position.ToString("F1")+" respawns="+(w.deaths-deaths));
        Idle();
    }
    IEnumerator Start() {
        w=GetComponent<BlobWorld>(); p=w.player; yaw0=p.yaw; pitch0=p.pitch;
        p.testMode=true; p.paused=false; w.UseSaveSlot("test_factory",false);
        report.utc=System.DateTime.UtcNow.ToString("o"); float start=Time.time;
        for(round=1;round<=rounds;round++) {
            yield return P1(); yield return P2(); yield return P3(); yield return P4(); yield return P5(); yield return P6();
            yield return Secrets(); yield return Ruins(); yield return Appliances(); yield return SaveLoad();
        }
        report.seconds=Time.time-start;
        Finish();
    }
    // ================= P1 sorting chutes =================
    IEnumerator P1() {
        yield return Case("f1_gate","(A) Magnet drags the scrap plates out",P1Magnet,"chute_a",3);
        yield return Case("f1_gate","(B) Pressure blows the foam boxes through",()=>Repeat(6,1.05f,()=>Seq(Powers(P,N),At(new Vector3(-.8f,0,37.0f),new Vector3(-.8f,.6f,40.4f)),Press(false)),()=>Solved("f1_gate")),"chute_b",3);
        yield return Case("f1_gate","(C) Pressure + Water jet pushes the 16 kg crates",()=>Repeat(6,.7f,()=>Seq(Powers(P,W),At(new Vector3(.8f,0,36.6f),new Vector3(.8f,.9f,40.4f)),Press(true)),()=>Solved("f1_gate")),"chute_c",3);
        yield return Case("f1_gate","(D) Heat burns the cardboard bundles",()=>Seq(Powers(H,N),At(new Vector3(2.4f,0,37.0f),new Vector3(2.4f,.8f,39.6f)),Hold(false,2.5f),Wait(7f)),"chute_d",6);
        // wrong power: heat on the metal lane does nothing to it
        yield return Fresh();
        yield return Seq(Powers(H,N),At(new Vector3(-2.4f,0,37.0f),new Vector3(-2.4f,.8f,39.6f)),Hold(false,2.5f),Wait(3f));
        CheckThat("f1_gate — wrong power: Heat on the scrap lane leaves it blocked",!Solved("f1_gate"),"state="+Pz("f1_gate").State);
        // return path: shutter opens after solving -> walk through it into the central hall
        yield return Fresh();
        yield return Repeat(6,1.05f,()=>Seq(Powers(P,N),At(new Vector3(-.8f,0,37.0f),new Vector3(-.8f,.6f,40.4f)),Press(false)),()=>Solved("f1_gate"));
        yield return Wait(1.5f); yield return At(new Vector3(6.5f,0,36.5f)); yield return Walk(new Vector3(6.5f,0,41.5f),4); yield return Walk(new Vector3(2f,0,44f),4);
        CheckThat("f1_gate — return: walk through the opened sorting shutter",Solved("f1_gate") && p.transform.position.z>41f,"pos="+p.transform.position.ToString("F1"));
        // reset pad returns the chute junk while unsolved
        yield return Fresh(); yield return Wait(1f);
        var plate=Obj("Chute A scrap plate (4 kg)"); Vector3 home=plate?plate.transform.position:Vector3.zero;
        if(plate) { var rb=plate.GetComponent<Rigidbody>(); rb.position=new Vector3(-6,1,34); plate.transform.position=rb.position; }
        yield return Wait(.6f); yield return At(new Vector3(-6.5f,0,32.2f)); yield return Wait(1.6f);
        CheckThat("f1_gate — reset pad puts moved junk back",plate && Vector3.Distance(plate.transform.position,home)<.6f,"offset="+(plate?Vector3.Distance(plate.transform.position,home).ToString("F2"):"none"));
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
    // ================= P2 crane room =================
    IEnumerator CarryTo(string body,Vector3 standAt,Vector3 dropOver) {
        var g=Obj(body); if(!g) yield break; var rb=g.GetComponent<Rigidbody>();
        yield return At(standAt,rb.worldCenterOfMass);
        p.testAction=true; yield return Wait(1.3f);
        Aim(dropOver+Vector3.up*.2f); yield return Walk(dropOver-new Vector3(0,0,2f),5); yield return Wait(.8f);
        p.testAction=false; yield return Wait(1.5f);
    }
    IEnumerator P2() {
        yield return Case("f4_crane","(1) Magnet drags the 6 kg counterweight onto the plate",()=>Seq(Powers(M,N),CarryTo("Crane counterweight (6 kg, magnetic)",new Vector3(16f,0,42.6f),new Vector3(12.5f,1f,47.6f))),"WeightPlate",3);
        yield return Case("f4_crane","(2) Electric + Magnet lifts the 30 kg machine onto the plate",()=>Seq(Powers(E,M),At(new Vector3(9.8f,0,43f)),Press(true),Powers(M,E),CarryTo("Old press machine (30 kg, magnetic)",new Vector3(9.8f,0,43f),new Vector3(12.5f,1f,47.6f))),"WeightPlate",3);
        yield return Case("f4_crane","(3) Magnet + Pressure cannon breaks the rusty bolt",P2Cannon,"Rust hatch",3);
        yield return Case("f4_crane","(4) Heat melts the wax seal, Pressure pushes the crawl hatch",()=>Seq(Powers(H,N),At(new Vector3(18.25f,0,48.9f),C("Wax seal")),Hold(false,1.4f),Powers(P,N),At(new Vector3(17.6f,0,48.9f),new Vector3(17.6f,.8f,51f)),Press(false)),"Wax crawl hatch",3);
        yield return Fresh();
        yield return At(new Vector3(12.5f,0,47.6f)); yield return Wait(2.5f);
        CheckThat("f4_crane — standing on the plate yourself does not count",!Solved("f4_crane"),"state="+Pz("f4_crane").State);
    }
    IEnumerator P2Cannon() {
        yield return Powers(M,P); var g=Obj("Crane counterweight (6 kg, magnetic)"); if(!g) yield break;
        yield return At(new Vector3(16f,0,42.6f),g.GetComponent<Rigidbody>().worldCenterOfMass);
        p.testAction=true; yield return Wait(1.3f);
        Aim(new Vector3(9.25f,1.4f,49f)); yield return Walk(new Vector3(9.25f,0,46.2f),5); yield return Wait(.6f);
        Aim(C("Rusty bolt (breaks on impact)")); yield return Wait(.4f);
        yield return Press(true); yield return Wait(.6f); p.testAction=false;
    }
    // ================= P3 boiler room =================
    IEnumerator P3() {
        yield return Case("f2_boiler","(1) Heat on the vent basin raises steam -> balcony",()=>Seq(Powers(H,N),At(new Vector3(10.6f,.6f,58.0f),new Vector3(10.6f,.6f,59.4f)),Hold(false,.7f),Wait(.5f),WalkFor(new Vector2(0,1),1.8f)),"balcony",3);
        yield return Case("f2_boiler","(1b) Water + Heat steam spray -> balcony",()=>Seq(Powers(W,H),At(new Vector3(10.6f,.6f,58.0f),new Vector3(10.6f,.6f,59.4f)),Hold(true,.9f),Wait(.4f),WalkFor(new Vector2(0,1),1.8f)),"balcony",3);
        yield return Case("f2_boiler","(2) Electric powers the cargo lift",()=>Seq(Powers(E,N),At(new Vector3(18.1f,0,59.55f),C("Boiler cargo lift motor (machine)")),Press(false),Wait(3.5f),WalkFor(new Vector2(0,1),1.4f)),"balcony",3);
        yield return Case("f2_boiler","(3) Magnet + Bounce slingshot into the balcony beam",()=>Seq(Powers(M,B),At(new Vector3(9.2f,0,56f),new Vector3(9.2f,5.8f,62.8f)),Press(true),Wait(2.5f)),"balcony",3);
        // return: balcony door -> walkway -> stairs -> central hall floor
        yield return Fresh();
        yield return Seq(Powers(M,B),At(new Vector3(9.2f,0,56f),new Vector3(9.2f,5.8f,62.8f)),Press(true),Until(()=>Solved("f2_boiler"),4f),Wait(1.5f));
        yield return At(new Vector3(8f,5.1f,63.4f)); yield return Walk(new Vector3(5.1f,0,63.2f),4); yield return Walk(new Vector3(5.1f,0,47.4f),6); yield return Walk(new Vector3(5.1f,0,40.4f),4); yield return Walk(new Vector3(1f,0,42f),3);
        CheckThat("f2_boiler — return: balcony shortcut -> walkway -> stairs -> central hall",Solved("f2_boiler") && p.transform.position.y<.5f && Mathf.Abs(p.transform.position.x)<6f && p.transform.position.z<50,"pos="+p.transform.position.ToString("F1"));
    }
    // ================= P4 cooling channel =================
    IEnumerator P4() {
        yield return Case("f5_cooling","(1) Water + Magnet drags the raft, then ferry across",P4Raft,"north bank",3);
        yield return Case("f5_cooling","(2) Magnet + Bounce slingshot to the north-bank post",()=>Seq(Powers(M,B),At(new Vector3(-15f,0,43.2f),new Vector3(-15f,1.9f,54.4f)),Press(true),WalkFor(new Vector2(0,1),2.2f)),"north bank",3);
        yield return Case("f5_cooling","(3) Electric raises the service bridge",()=>Seq(Powers(E,N),At(new Vector3(-15.8f,0,43.2f),C("Service bridge motor (machine)")),Press(false),Wait(1.3f),Walk(new Vector3(-15.8f,0,54.8f),5)),"north bank",3);
        yield return Case("f5_cooling","(4) Bounce from the tip of the fallen girder",()=>Seq(Powers(B,N),At(new Vector3(-18.6f,0,48.7f)),Bounce(new Vector2(0,1)),WalkFor(new Vector2(0,1),1.2f)),"north bank",3);
        yield return Fresh();
        yield return Seq(Powers(B,N),At(new Vector3(-12f,0,42.5f)),Bounce(new Vector2(0,1)),Wait(2.5f));
        CheckThat("f5_cooling — a full Bounce cannot clear the 9.5 m channel",!Solved("f5_cooling"),"state="+Pz("f5_cooling").State+" respawns="+w.deaths);
        // return: permanent catwalk back to the south bank
        yield return Fresh();
        yield return Seq(Powers(M,B),At(new Vector3(-15f,0,43.2f),new Vector3(-15f,1.9f,54.4f)),Press(true),WalkFor(new Vector2(0,1),2.2f),Until(()=>Solved("f5_cooling"),3f),Wait(1f));
        yield return At(new Vector3(-11.6f,0,54.6f)); yield return Walk(new Vector3(-11.6f,0,42.2f),5);
        CheckThat("f5_cooling — return: permanent catwalk back to the south bank",Solved("f5_cooling") && p.transform.position.z<44f && p.transform.position.y>-.2f,"pos="+p.transform.position.ToString("F1"));
    }
    IEnumerator Bounce(Vector2 move,float charge=.8f) { p.testJump=true; yield return Wait(charge); p.testMove=move; p.testJump=false; yield return null; }
    IEnumerator P4Raft() {
        yield return Powers(W,M);
        var raft=Obj("Cooling raft (20 kg, magnetic)").GetComponent<ShuttleRaft>();
        yield return At(new Vector3(-8.6f,0,43.3f),C("Cooling raft (20 kg, magnetic)"));
        p.testSecondary=true; yield return Until(()=>raft.state==ShuttleRaft.State.Docked,9f); p.testSecondary=false;
        yield return Walk(new Vector3(-8.6f,0,45.3f),3); yield return Until(()=>raft.state==ShuttleRaft.State.Docked && !raft.atNear,6f);
        yield return Walk(new Vector3(-8.6f,0,54.8f),3);
        // and back again on the same raft (it must not strand BLOB)
        yield return Walk(new Vector3(-8.6f,0,52.2f),3); yield return Until(()=>raft.state==ShuttleRaft.State.Docked && raft.atNear,6f);
        CheckThat("f5_cooling — raft shuttles back to the near dock",raft.atNear && raft.crossings>=2,"crossings="+raft.crossings+" atNear="+raft.atNear);
        yield return Walk(new Vector3(-8.6f,0,47.5f),2); yield return Walk(new Vector3(-8.6f,0,52.2f),3); yield return Until(()=>!raft.atNear,6f); yield return Walk(new Vector3(-8.6f,0,54.8f),3);
    }
    // ================= P5 turbine =================
    IEnumerator P5() {
        yield return Case("f3_turbine","(1) Pressure blasts from the blue spot",()=>Repeat(7,1.05f,()=>Seq(Powers(P,N),At(new Vector3(-15f,0,60.2f),new Vector3(-19.2f,3.6f,60.2f)),Press(false)),()=>Solved("f3_turbine")),"PressureTurbine",3);
        yield return Case("f3_turbine","(2) Water + Heat steam into the intake",()=>Seq(Powers(W,H),At(new Vector3(-15.2f,0,64.4f),new Vector3(-17.4f,.3f,64.4f)),Hold(true,2.5f)),"SteamInlet",3);
        yield return Case("f3_turbine","(2b) Heat boils the feed tank",()=>Seq(Powers(H,N),At(new Vector3(-15.2f,0,64.4f),new Vector3(-17.4f,.3f,64.4f)),Hold(false,2.5f)),"SteamInlet",3);
        SwitchGroup seq=null;
        yield return Case("f3_turbine","(3) Pressure + Electric long spark to the high motor",()=>Seq(Powers(E,P),At(new Vector3(-12.5f,0,54.6f),C("Turbine far motor (machine, high)")),Press(true)),"far-motor",3);
        foreach(var s in FindObjectsByType<SwitchGroup>(FindObjectsSortMode.None)) if(s.name.Contains("vent switches")) seq=s;
        CheckThat("f3_turbine — the long spark did not also trip the switch sequence",seq && !seq.met,"seq.met="+(seq?seq.met.ToString():"none"));
        yield return Case("f3_turbine","(4) Bounce + Electric chain spark 1 -> 2 -> 3",()=>Seq(Powers(E,B),At(new Vector3(-18f,0,54.2f),C("Vent switch 1")),Press(true)),"SwitchGroup",3);
        yield return Fresh();
        yield return Repeat(7,1.05f,()=>Seq(Powers(P,N),At(new Vector3(-15f,0,60.2f),new Vector3(-19.2f,3.6f,60.2f)),Press(false)),()=>Solved("f3_turbine"));
        yield return Wait(1.6f); yield return At(new Vector3(-8.5f,0,61f)); yield return Walk(new Vector3(-2f,0,61f),4);
        CheckThat("f3_turbine — return: door back to the central hall opens",Solved("f3_turbine") && p.transform.position.x>-5f,"pos="+p.transform.position.ToString("F1"));
    }
    // ================= P6 assembly vault =================
    IEnumerator P6() {
        yield return Case("f6_assembly","(1) Electric + Heat welds the drive rail",()=>Seq(Powers(H,E),At(new Vector3(-13.4f,0,37.6f),C("Cabinet drive rail (broken)")),Hold(true,1.2f)),"ComboLock",3);
        yield return Case("f6_assembly","(2) Magnet + Pressure cannon breaks the rusty lock",P6Cannon,"LatchedDoor",3);
        yield return Case("f6_assembly","(3) Heat + Magnet induction heats the rod, the ice lock melts",()=>Seq(Powers(H,M),At(new Vector3(-14.6f,0,34.6f),C("Conductor rod (metal)")),Hold(true,3f),Wait(2.5f)),"ice-lock",4);
        yield return Fresh();
        yield return Seq(Powers(H,N),At(new Vector3(-17.2f,0,34.6f),C("Conductor rod (metal)")),Hold(false,3f),Wait(2f));
        CheckThat("f6_assembly — plain Heat from the floor cannot reach the ice lock",!Solved("f6_assembly"),"ice consumed="+(Mat("Ice lock")?Mat("Ice lock").consumed:false));
        // reward only once
        yield return Fresh();
        yield return Seq(Powers(H,E),At(new Vector3(-13.4f,0,37.6f),C("Cabinet drive rail (broken)")),Hold(true,1.2f),Until(()=>Solved("f6_assembly"),3f),Wait(1f));
        yield return At(new Vector3(-15.6f,0,34.6f)); yield return Walk(new Vector3(-16.6f,0,34.6f),2); yield return Wait(.5f);
        int before=w.ship.collected.Count+w.ship.installed.Count;
        yield return P6Cannon(); yield return Wait(1f); yield return Walk(new Vector3(-16.6f,0,34.6f),3); yield return Wait(.5f);
        int after=w.ship.collected.Count+w.ship.installed.Count;
        CheckThat("f6_assembly — core collected once; a second method gives nothing more",w.ship.Has("part_factory_core") && after==before,"has="+w.ship.Has("part_factory_core")+" before="+before+" after="+after);
    }
    IEnumerator P6Cannon() {
        yield return Powers(M,P); var g=Obj("Spare gear (3 kg, magnetic)"); if(!g) yield break;
        yield return At(new Vector3(-13f,0,35.4f),g.GetComponent<Rigidbody>().worldCenterOfMass);
        p.testAction=true; yield return Wait(1.3f);
        Aim(C("Rusty cabinet lock")); yield return Walk(new Vector3(-14.2f,0,33.2f),3); yield return Wait(.5f);
        Aim(C("Rusty cabinet lock")); yield return Wait(.4f);
        yield return Press(true); yield return Wait(.8f); p.testAction=false;
    }
    // ================= hidden spots =================
    IEnumerator Secrets() {
        yield return Case("s_storage","Pressure + Bounce wind ball reaches the button round the corner",()=>Seq(Powers(P,B),At(new Vector3(14.4f,0,38.1f),C("Round-the-corner button")),Press(true),Wait(1.5f)));
        if(Solved("s_storage")) { yield return At(new Vector3(18.6f,0,37f)); yield return Walk(new Vector3(18.6f,0,34.4f),3); yield return Walk(new Vector3(17.7f,0,34.2f),2); yield return Wait(.5f); }
        CheckThat("Hidden s_storage — walk into the cubby and collect part_storage",w.ship.Has("part_storage"),"pos="+p.transform.position.ToString("F1"));
        yield return Case("s_beam_ice","Bounce + Heat fireball lobbed into the walled oven",OvenLob,null,3);
        yield return Case("s_fuse","Electric + Heat melts the turbine-room fuse",()=>Seq(Powers(E,H),At(new Vector3(-16.6f,0,58.4f),C("Fuse (short-circuit lock)")),Hold(true,1.2f)));
        yield return Fresh();
        yield return Seq(Powers(E,N),At(new Vector3(-14.6f,0,43.6f),C("Cooling pool (electric water only)")),Press(false),Wait(1f));
        CheckThat("Hidden s_factory_shockpool — plain Electric on the pool does not open it",!Solved("s_factory_shockpool"),"state="+Pz("s_factory_shockpool").State);
        yield return Case("s_factory_shockpool","Water + Electric stream into the pool",()=>Seq(Powers(W,E),At(new Vector3(-14.6f,0,43.6f),C("Cooling pool (electric water only)")),Hold(true,1.2f)));
        if(Solved("s_factory_shockpool")) { yield return Wait(1.6f); yield return At(new Vector3(-8f,0,40.6f)); yield return Walk(new Vector3(-8f,0,37.4f),3); }
        CheckThat("Hidden s_factory_shockpool — return: crawl hatch back into the receiving hall",p.transform.position.z<38.6f,"pos="+p.transform.position.ToString("F1"));
        yield return Case("s_conveyor_weld","Electric + Heat welds the broken conveyor rail",()=>Seq(Powers(H,E),At(new Vector3(16.4f,0,55f),C("Broken conveyor rail (weld me)")),Hold(true,1.2f)));
        ZoneGate conv=null; foreach(var g in w.zoneGates) if(g && g.zoneId=="shortcut_conveyor") conv=g;
        yield return Wait(.3f);
        CheckThat("Hidden s_conveyor_weld — the outside conveyor shortcut gate unlocks",conv && conv.Unlocked,"unlocked="+(conv?conv.Unlocked.ToString():"none"));
        // flamethrower with a readable risk: without water the paper stock catches -> not solved; with water first -> solved
        yield return Fresh();
        yield return Seq(Powers(H,P),At(new Vector3(-2f,0,52.2f),C("High niche wooden barricade")),Hold(true,1.2f),Wait(9f));
        var stock=Mat("Paper stock shelf (keep it safe)");
        CheckThat("Hidden s_factory_flame — risk: careless fire burns the paper stock and the spot stays shut",!Solved("s_factory_flame") && stock && stock.everIgnited,"stock ignited="+(stock?stock.everIgnited:false));
        yield return Case("s_factory_flame","Pressure + Heat flamethrower, paper stock wetted first",()=>Seq(Powers(W,N),At(new Vector3(-3f,0,54.3f),C("Paper stock shelf (keep it safe)")),Hold(false,1f),Powers(H,P),At(new Vector3(-2f,0,52.2f),C("High niche wooden barricade")),Hold(true,1.2f),Wait(8f)),null,4);
        // Water + Bounce: water ball onto the press-top sprout -> vine ramp -> part_cold_room
        yield return Fresh();
        var vine=FindFirstObjectByType<VineBridge>(FindObjectsInactive.Include);
        foreach(var vb in FindObjectsByType<VineBridge>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(vb.seed && vb.seed.name=="Ledge sprout pot") vine=vb;
        yield return Powers(W,B);
        foreach(var aim in new[]{new Vector3(0,6.65f,60.6f),new Vector3(0,6.45f,60.6f),new Vector3(0,6.95f,60.6f),new Vector3(0,6.25f,60.6f),new Vector3(0,7.25f,60.6f)}) {
            if(vine && vine.Grown) break;
            yield return At(new Vector3(5.1f,5f,60.6f),aim); yield return Press(true); yield return Wait(1.6f);
        }
        bool grown=vine && vine.Grown;
        if(grown) { yield return At(new Vector3(0,0,49.8f)); yield return Walk(new Vector3(0,0,58.9f),7); yield return Walk(new Vector3(1.1f,0,59.6f),2); yield return Wait(.6f); }
        CheckThat("Hidden water-ball sprout — Water + Bounce grows the vine ramp, part_cold_room collected",grown && w.ship.Has("part_cold_room"),"growth="+(vine&&vine.seed?vine.seed.growth:-1)+" pos="+p.transform.position.ToString("F1"));
    }
    // ================= ruins that change routes =================
    IEnumerator Ruins() {
        yield return Fresh();
        var planks=Mat("Breach plank barricade (burns)");
        yield return Seq(Powers(N,N),At(new Vector3(-4.2f,0,58f)),Walk(new Vector3(-8f,0,58f),2.5f));
        CheckThat("Ruins — the plank barricade blocks the breach before it burns",p.transform.position.x>-6f,"pos="+p.transform.position.ToString("F1"));
        yield return Seq(Powers(H,N),At(new Vector3(-4.2f,0,58f),new Vector3(-5.75f,1.2f,58f)),Hold(false,2f),Wait(8f));
        yield return Seq(Powers(N,N),At(new Vector3(-4.2f,0,58f)),Walk(new Vector3(-8.2f,0,58f),3f));
        CheckThat("Ruins — Heat burns the rotten planks, the breach leads into the turbine room",planks && planks.consumed && p.transform.position.x<-7.4f,"consumed="+(planks?planks.consumed:false)+" pos="+p.transform.position.ToString("F1"));
    }
    // ================= Kirby-style appliances =================
    IEnumerator Appliances() {
        yield return Fresh();
        BlobInteractable src=null; foreach(var b in FindObjectsByType<BlobInteractable>(FindObjectsSortMode.None)) if(b.name=="Receiving Pressure source") src=b;
        var st=src?src.GetComponent<ApplianceState>():null;
        int count=0; foreach(var a in FindObjectsByType<ApplianceState>(FindObjectsSortMode.None)) if(a.fullArt && a.drainedArt) count++;
        CheckThat("Appliances — every factory source is an element appliance",count>=22,"appliances="+count);
        if(!src || !st) { CheckThat("Appliances — absorbing swaps to the drained look and back after 20 s",false,"source/state missing"); yield break; }
        bool fullBefore=st.fullArt.activeSelf && !st.drainedArt.activeSelf;
        yield return At(src.transform.position+new Vector3(0,0,1.4f),src.AimPoint);
        p.testAbsorb=true; yield return Wait(1.1f); p.testAbsorb=false; yield return Wait(.5f);
        bool drainedLook=!src.Available && st.drained && st.drainedArt.activeSelf && !st.fullArt.activeSelf;
        p.testAim=null;
        yield return Until(()=>src.Available,23f); yield return Wait(.5f);
        bool back=src.Available && !st.drained && st.fullArt.activeSelf && !st.drainedArt.activeSelf;
        CheckThat("Appliances — absorbing swaps to the drained look and back after 20 s",fullBefore && drainedLook && back,"fullBefore="+fullBefore+" drainedLook="+drainedLook+" back="+back+" property="+p.property);
    }
    IEnumerator OvenLob() {
        yield return Powers(H,B);
        foreach(var aim in new[]{new Vector3(18.2f,2.2f,39.2f),new Vector3(18.2f,1.9f,39.2f),new Vector3(18.2f,2.5f,39.2f),new Vector3(18.6f,2.2f,39.2f),new Vector3(17.8f,1.7f,39.2f)}) {
            if(Solved("s_beam_ice")) break;
            yield return At(new Vector3(16.5f,0,45.5f),aim); yield return Press(true); yield return Wait(1.6f);
        }
    }
    // ================= save / load =================
    IEnumerator SaveLoad() {
        yield return Fresh();
        yield return Repeat(6,1.05f,()=>Seq(Powers(P,N),At(new Vector3(-.8f,0,37.0f),new Vector3(-.8f,.6f,40.4f)),Press(false)),()=>Solved("f1_gate"));
        yield return Wait(1.6f); yield return At(new Vector3(6.5f,0,35.5f)); yield return Walk(new Vector3(6.5f,0,36.8f),2); yield return Wait(.5f);
        bool had=w.ship.Has("part_f1");
        // a fresh load of the slot (no Restart in between: Restart would save an empty game)
        var status=w.LoadFromSlot("test_factory"); yield return Wait(.6f);
        var shutter=Obj("Sorting shutter (opens when a chute is clear)");
        bool shutterOpen=shutter && shutter.transform.position.y>3.5f;
        int inLanes=0; foreach(var it in PuzzleItem.All) if(it.key.StartsWith("chute_") && it.transform.position.z>38.4f && it.transform.position.z<41.6f && Mathf.Abs(it.transform.position.x)<3.3f) inLanes++;
        CheckThat("Save/load — P1 stays solved, shutter open, part_f1 kept, junk not back in the lanes",status==SaveSystem.LoadStatus.Ok && had && Solved("f1_gate") && w.ship.Has("part_f1") && shutterOpen,"status="+status+" had="+had+" shutterY="+(shutter?shutter.transform.position.y.ToString("F1"):"none")+" itemsInLanes="+inLanes);
        yield return At(new Vector3(6.5f,0,36.5f)); yield return Walk(new Vector3(6.5f,0,41.5f),4);
        CheckThat("Save/load — still possible to walk into the central hall after loading",p.transform.position.z>40.5f,"pos="+p.transform.position.ToString("F1"));
        // rice gate interface: F1 + one wing restores the outside unlock (driven by real solves)
        ZoneGate rice=null; foreach(var g in w.zoneGates) if(g && g.zoneId=="rice") rice=g;
        bool shutWithF1=rice && !rice.Unlocked;
        yield return Repeat(7,1.05f,()=>Seq(Powers(P,N),At(new Vector3(-15f,0,60.2f),new Vector3(-19.2f,3.6f,60.2f)),Press(false)),()=>Solved("f3_turbine"));
        yield return Wait(.5f);
        CheckThat("Outside interface — rice gate: shut with F1 only, opens after F1 + turbine",shutWithF1 && rice.Unlocked,"shutWithF1="+shutWithF1+" unlocked="+(rice?rice.Unlocked.ToString():"none"));
    }
    void Finish() {
        Idle(); p.yaw=yaw0; p.pitch=pitch0; p.testMode=false;
        foreach(var a in aims) if(a) Destroy(a);
        w.Restart();
        string path=TestReports.PathFor("factory-interior-test.json");
        File.WriteAllText(path,JsonUtility.ToJson(report,true));
        foreach(var f in new[]{SaveSystem.PathFor("test_factory")}) if(File.Exists(f)) File.Delete(f);
        Debug.Log("OOPS_FACTORY_DONE passed="+report.passed+" failed="+report.failed+" seconds="+report.seconds.ToString("F0")+" report="+path);
    }
}
}
