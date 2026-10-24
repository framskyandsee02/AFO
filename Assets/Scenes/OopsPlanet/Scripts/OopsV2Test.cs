using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// Design v2 play test (OopsPlanet_V2 scene). One case per puzzle solution (design §6: every way has a test), the
// hidden spots (each uses one of the leftover combos), power / combo checks, the power journal reaching 15/15,
// save v2 round trip and the ship ending. Every case starts from a full restart; powers are set directly and
// driven with the same test inputs as the prototype tests (F = testAction, G = testSecondary, SPACE = testJump).
public class OopsV2Test : MonoBehaviour {
    [System.Serializable] public class Check { public string name; public bool passed; public string observed; }
    [System.Serializable] public class Report { public string utc; public bool completed; public List<Check> checks=new List<Check>(); public int passed, failed; public List<string> combosUsed=new List<string>(); }
    const BlobProperty M=BlobProperty.Magnet, P=BlobProperty.Pressure, B=BlobProperty.Bounce, W=BlobProperty.Water, E=BlobProperty.Electric, H=BlobProperty.Heat, N=BlobProperty.None;
    BlobWorld w; BlobPlayer p; BlobPowers pw; BlobElementPowers el; Report report=new Report();
    readonly List<GameObject> aims=new List<GameObject>();
    readonly HashSet<string> combosUsed=new HashSet<string>();
    public bool focusedFixesOnly;
    public string caseFilter="";
    float yaw0, pitch0;
    void CheckThat(string name,bool pass,string observed) {
        report.checks.Add(new Check{name=name,passed=pass,observed=observed});
        if(pass) report.passed++; else report.failed++;
        Debug.Log("OOPS_V2 "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
    }
    // ---------- input helpers ----------
    IEnumerator Wait(float s) { float until=Time.time+s; while(Time.time<until) yield return null; }
    void Idle() { p.testMove=Vector2.zero; p.testAction=false; p.testSecondary=false; p.testPush=false; p.testJump=false; p.testAbsorb=false; p.testSense=false; p.testAim=null; p.pitch=19; }
    void Aim(Vector3 pt) { var g=new GameObject("V2 test aim"); g.transform.position=pt; aims.Add(g); p.testAim=g.transform; p.AimAt(g.transform); }
    IEnumerator At(Vector3 pos,Vector3? aim=null) { Idle(); p.Teleport(pos+Vector3.up*.1f); if(aim.HasValue) Aim(aim.Value); yield return Wait(.35f); }
    IEnumerator PressF() { bool combo=p.secondary!=N; if(combo)p.testSecondary=true;else p.testAction=true; yield return null; yield return null; p.testAction=false;p.testSecondary=false;yield return null; }
    IEnumerator PressG() { p.testSecondary=true; yield return null; yield return null; p.testSecondary=false; yield return null; }
    IEnumerator HoldF(float s) { if(p.secondary!=N)p.testSecondary=true;else p.testAction=true; yield return Wait(s);p.testAction=false;p.testSecondary=false;yield return null; }
    IEnumerator HoldG(float s) { p.testSecondary=true; yield return Wait(s); p.testSecondary=false; yield return null; }
    IEnumerator Bounce(Vector2 move,float charge=.8f) { p.testJump=true; yield return Wait(charge); p.testMove=move; p.testJump=false; yield return null; }
    IEnumerator Walk(Vector3 goal,float timeout=6f) {
        float until=Time.time+timeout;
        while(Time.time<until) { Vector3 d=goal-p.transform.position; d.y=0; if(d.magnitude<.3f) break; p.testMove=new Vector2(d.x,d.z).normalized; yield return null; }
        p.testMove=Vector2.zero;
    }
    IEnumerator Until(System.Func<bool> cond,float timeout) { float until=Time.time+timeout; while(Time.time<until && !cond()) yield return null; }
    void Powers(BlobProperty a,BlobProperty b) => p.SetProperties(a,b);
    PuzzleObjective Pz(string id) { foreach(var o in w.puzzles) if(o && o.id==id) return o; return null; }
    WorldMaterial Mat(string name) { foreach(var m in WorldMaterial.All) if(m.name==name) return m; return null; }
    GameObject Obj(string name) { var g=GameObject.Find(name); return g; }
    Vector3 Center(string name) { var g=Obj(name); if(!g) return Vector3.zero; var c=g.GetComponentInChildren<Collider>(); return c?c.bounds.center:g.transform.position; }
    IEnumerator Fresh() {
        Idle(); w.Restart(); p.testMode=true; foreach(var a in aims) if(a) Destroy(a); aims.Clear();
        yield return Wait(.3f);
    }
    // One solution of one puzzle: restart, unlock prerequisites, run the steps, then expect Solved (+ solvedBy text).
    IEnumerator Case(string id,string way,System.Func<IEnumerator> steps,string expectBy=null,float settle=4f) {
        if(caseFilter!="" && !(caseFilter=="secrets" && id.StartsWith("s_")) && !(id+" "+way).Contains(caseFilter)) yield break;
        yield return Fresh();
        var pz=Pz(id); if(!pz) { CheckThat(id+" "+way,false,"puzzle missing"); yield break; }
        if(pz.requires) pz.requires.Solve();
        int deaths=w.deaths;
        yield return StartCoroutine(steps());
        yield return Until(()=>pz.State==PuzzleState.Solved,settle);
        bool ok=w.deaths==deaths && pz.State==PuzzleState.Solved && (expectBy==null || pz.solvedBy.Contains(expectBy));
        CheckThat((pz.isSecret?"Hidden ":"")+id+" — "+way,ok,"state="+pz.State+" solvedBy="+pz.solvedBy+" pos="+p.transform.position.ToString("F1")+" respawns="+(w.deaths-deaths)+" combo="+el.lastCell);
        Debug.Log("OOPS_V2_DIAG held="+(pw.held?pw.held.name:"none")+" weight="+Center("Counterweight (6 kg, magnetic)")+" machine="+Center("Old machine (30 kg, magnetic)")+" cannons="+pw.cannons);
        Idle();
    }
    IEnumerator Start() {
        w=GetComponent<BlobWorld>(); p=w.player; pw=p.powers; el=p.GetComponent<BlobElementPowers>();
        yaw0=p.yaw; pitch0=p.pitch;
        p.testMode=true; p.paused=false; w.UseSaveSlot("test_v2",false);
        report.utc=System.DateTime.UtcNow.ToString("o");
        el.CellUsed+=c=>combosUsed.Add(c.id);
        CheckThat("V2 scene wired (7 main puzzles, ship, 6 powers, journal)",w.MainPuzzleCount==7 && w.ship && el && pw.set.table && pw.set.table.cells.Count==15,"main="+w.MainPuzzleCount+" parts="+(w.ship?w.ship.parts.Length:0));
        if(focusedFixesOnly) {
            if(report.failed==0) yield return SaveRoundTrip();
            Finish(); yield break;
        }
        if(caseFilter!="") {
            yield return FactoryCases();
            yield return RiceCases();
            if(caseFilter=="secrets") yield return SecretCases();
            Finish(); yield break;
        }
        yield return HubCases();
        yield return FactoryCases();
        yield return RiceCases();
        yield return SecretCases();
        yield return PowerChecks();
        yield return SaveAndShip();
        var missing=new List<string>(); foreach(var c in pw.set.table.cells) if(!combosUsed.Contains(c.id)) missing.Add(c.id);
        report.combosUsed.AddRange(combosUsed);
        CheckThat("All 15 unordered combos were used in the V2 map (journal 15/15)",missing.Count==0,"used="+combosUsed.Count+" missing="+string.Join(",",missing));
        Finish();
    }
    // ---------- HUB ----------
    IEnumerator HubCases() {
        Vector3[] junk={new Vector3(-14.5f,1.35f,-8),new Vector3(-15.1f,.45f,-8),new Vector3(-13.9f,.45f,-8)};
        yield return Case("h1_pit_exit","(T) Magnet drags the junk out",()=>H1Magnet(junk),"junk");
        yield return Case("h1_pit_exit","Pressure blows the junk away",()=>Repeat(6,1.05f,()=>Seq(At(new Vector3(-14,0,-10.6f),new Vector3(-14,.9f,-8)),PowerSet(P,N),PressF()),()=>Pz("h1_pit_exit").State==PuzzleState.Solved),"junk");
        yield return Case("h1_pit_exit","Heat burns the cardboard junk",()=>Seq(PowerSet(H,N),At(new Vector3(-14,0,-10.3f),new Vector3(-14,.9f,-8)),HoldF(2.5f),Wait(7f)),"junk",6);
        yield return Case("h1_pit_exit","Bounce over the 3.2 m pit wall",()=>Seq(PowerSet(B,N),At(new Vector3(-11,0,-11)),Bounce(new Vector2(0,1)),Wait(2.5f)),"north");
    }
    IEnumerator H1Magnet(Vector3[] junk) {
        Powers(M,N);
        for(int i=0;i<6 && Pz("h1_pit_exit").State!=PuzzleState.Solved;i++) {
            BlobMagnetic best=null; float bd=99;
            foreach(var m in BlobMagnetic.All) if(m.name.StartsWith("Junk bundle")) { float d=Mathf.Abs(m.transform.position.z+8); if(d<1.4f && Mathf.Abs(m.transform.position.x+14)<2.4f && d<bd) { bd=d; best=m; } }
            if(!best) break;
            yield return At(new Vector3(-14,0,-12.5f),best.Body.worldCenterOfMass);
            p.testAction=true; yield return Wait(1.2f);
            yield return Walk(new Vector3(-14,0,-16)); p.testAction=false; yield return Wait(.6f);
        }
    }
    // ---------- FACTORY ----------
    IEnumerator FactoryCases() {
        yield return Case("f1_gate","(T) Electric powers the gate motor",()=>Seq(PowerSet(E,N),At(new Vector3(3.2f,0,39.5f),Center("F1 gate motor (machine)")),PressF()),"motor");
        yield return Case("f1_gate","Magnet drags the 6 kg counterweight onto the plate",F1Counterweight,"WeightPlate");
        yield return Case("f1_gate","Electric + Magnet lifts the 30 kg old machine onto the plate",F1OldMachine,"WeightPlate",5);
        yield return Case("f1_gate","Heat melts the wax latch, Pressure blows the hatch open",()=>Seq(PowerSet(H,N),At(new Vector3(-7.4f,0,41.6f),new Vector3(-7.4f,1.1f,43.75f)),HoldF(1.2f),PowerSet(P,N),At(new Vector3(-8,0,41.8f),new Vector3(-8,1.1f,44)),PressF()),"LatchedDoor");
        yield return Case("f1_gate","Magnet + Pressure cannon knocks the rusty latch off",F1Cannon,"LatchedDoor");
        yield return Case("f2_boiler","(T) Water + Heat steam lifts BLOB to the walkway",()=>Seq(PowerSet(W,H),At(new Vector3(-12.5f,.6f,56),new Vector3(-12.5f,.6f,58.5f)),HoldF(.8f),Wait(.6f),WalkFor(new Vector2(-1,0),1.6f)),"walkway",3);
        yield return Case("f2_boiler","Electric powers the cargo lift",()=>Seq(PowerSet(E,N),At(new Vector3(-17.5f,.35f,45.6f),Center("F2 cargo lift motor (machine)")),PressF(),Wait(3.6f),WalkFor(new Vector2(0,1),1.2f)),"walkway",3);
        yield return Case("f2_boiler","Magnet + Bounce slingshot from the steel beam",()=>Seq(PowerSet(M,B),At(new Vector3(-10,0,56),new Vector3(-17,7.6f,58)),PressF(),Wait(.3f),Until(()=>p.Grounded,3.5f),Walk(new Vector3(-17.5f,6,58),2)),"walkway",3);
        yield return Case("f3_turbine","(T) Pressure blasts spin the turbine",()=>Repeat(6,1.05f,()=>Seq(At(new Vector3(10,0,61),new Vector3(10,4,65.4f)),PowerSet(P,N),PressF()),()=>Pz("f3_turbine").State==PuzzleState.Solved),"PressureTurbine");
        yield return Case("f3_turbine","Electric + Pressure long spark powers the high motor",()=>Seq(PowerSet(E,P),At(new Vector3(16,0,52),Center("F3 turbine motor (machine, high)")),PressF()),"turbine-motor");
        yield return Case("f3_turbine","Water + Heat steam into the intake pipe",()=>Seq(PowerSet(W,H),At(new Vector3(4,0,60.5f),new Vector3(4,.3f,64)),HoldF(2.5f)),"SteamInlet");
        yield return Case("f3_turbine","Electric + Bounce chain spark hits switches 1-2-3 in order",()=>Seq(PowerSet(E,B),At(new Vector3(15.5f,0,59.5f),Center("F3 sequence switch 1")),PressF()),"SwitchGroup");
    }
    IEnumerator F1Counterweight() {
        Powers(M,N); var weight=Obj("Counterweight (6 kg, magnetic)");
        yield return At(new Vector3(-9,0,36.2f),weight.transform.position);
        p.testAction=true; yield return Wait(1.2f);
        Aim(Center("F1 weight plate")); yield return Walk(new Vector3(-9,0,37.6f)); yield return Walk(new Vector3(-4,0,37.6f)); yield return Wait(2f);
        p.testAction=false; yield return Wait(1.2f);
    }
    IEnumerator F1OldMachine() {
        Powers(E,M); var mach=Obj("Old machine (30 kg, magnetic)");
        yield return At(new Vector3(5,0,39.4f),mach.transform.position+Vector3.up*.75f);
        yield return PressF(); p.SwapProperties();
        p.testAction=true; yield return Wait(1.5f);             // G = Magnet hold (secondary)
        Aim(Center("F1 weight plate")); yield return Walk(new Vector3(-1,0,37.6f)); yield return Walk(new Vector3(-4,0,37.6f)); yield return Wait(2f);
        p.testAction=false; yield return Wait(1.2f);
    }
    IEnumerator F1Battery() {
        Powers(M,E); var weight=Obj("Counterweight (6 kg, magnetic)");
        yield return At(new Vector3(-9,0,36.2f),weight.transform.position);
        p.testAction=true; yield return Wait(1.2f);
        Aim(Center("F1 remote power socket (machine)")); yield return Walk(new Vector3(-17,0,36.2f)); yield return Walk(new Vector3(-17,0,39.6f)); yield return Wait(2.5f);
        p.testAction=false; yield return Wait(.5f);
    }
    IEnumerator F1Cannon() {
        Powers(M,P); var weight=Obj("Counterweight (6 kg, magnetic)");
        yield return At(new Vector3(-8.2f,0,36.2f),weight.transform.position);
        p.testAction=true; yield return Wait(1.3f);
        Aim(new Vector3(-7.4f,1.1f,43.75f)); yield return Walk(new Vector3(-7.4f,0,39.8f)); yield return Wait(.8f);
        yield return PressG(); yield return Wait(.6f); p.testAction=false;
        Powers(P,N); yield return Wait(1.1f); yield return At(new Vector3(-8,0,41.8f),new Vector3(-8,1.1f,44)); yield return PressF();
    }
    // ---------- RICE ----------
    IEnumerator RiceCases() {
        yield return Case("r1_sluice","(T) Pressure blasts the straw off the platform",()=>R1Blow(false),"all",3);
        yield return Case("r1_sluice","Heat burns the straw after wetting the drying rack (Water)",R1Burn,"all",3);
        yield return Case("r1_sluice","Pressure + Water jet pushes the straw",()=>R1Blow(true),"all",3);
        yield return Case("r1_sluice","Heat + Pressure flamethrower (rack wetted first)",R1Flame,"all",3);
        yield return Case("r2_canal","(T) Water grows the vine bridge",()=>Seq(PowerSet(W,N),At(new Vector3(49.6f,0,6),Center("R2 vine seed")),HoldF(1.2f),Wait(.6f),Walk(new Vector3(63,0,6),6)),"east");
        yield return Case("r2_canal","Magnet + Bounce slingshot to the iron lamp post",()=>Seq(PowerSet(M,B),At(new Vector3(50,0,-6),new Vector3(61.5f,5.2f,-6)),PressF(),WalkFor(new Vector2(1,0),2.5f)),"east");
        yield return Case("r2_canal","Water + Magnet stream drags the iron raft, then ferry",R2Raft,"east",4);
        yield return Case("r3_pump","(T) Electric powers the pump",()=>Seq(PowerSet(E,N),At(new Vector3(64,0,14.5f),Center("R3 irrigation pump (machine)")),PressF()),"pump-power");
        yield return Case("r3_pump","Pressure spins the windmill",()=>Repeat(6,1.05f,()=>Seq(At(new Vector3(69.5f,0,16),new Vector3(69.5f,4,19)),PowerSet(P,N),PressF()),()=>Pz("r3_pump").State==PuzzleState.Solved),"PressureTurbine");
        yield return Case("r3_pump","Electric + Water shocks through the pond to the submerged terminal",()=>Seq(PowerSet(E,W),At(new Vector3(64,0,8.4f),new Vector3(64,.2f,4)),PressF()),"submerged-terminal");
        yield return Case("r3_pump","Water + Electric electric stream into the pond",()=>Seq(PowerSet(W,E),At(new Vector3(64,0,8.2f),new Vector3(64,.2f,4.6f)),HoldF(.8f)),"submerged-terminal");
        yield return Case("r3_pump","Heat + Water boils the steam-pump feed tank",()=>Seq(PowerSet(H,W),At(new Vector3(67.2f,0,7.5f),new Vector3(70,.3f,7.5f)),HoldF(2f)),"SteamInlet");
        yield return Case("r3_pump","Water + Pressure jet waters all 6 plots",R3Jet,"fields-watered",3);
    }
    IEnumerator R1Blow(bool water) {
        Powers(P,water?W:N);
        for(int attempt=0;attempt<12 && Pz("r1_sluice").State!=PuzzleState.Solved;attempt++) {
            PuzzleItem straw=null;
            foreach(var item in PuzzleItem.All) if(item.key=="straw" && new Bounds(new Vector3(42,3.6f,16),new Vector3(9,1.4f,5)).Contains(item.Center)) { var mat=item.GetComponent<WorldMaterial>(); if(!mat || !mat.consumed) { straw=item; break; } }
            if(!straw) break;
            yield return At(new Vector3(Mathf.Clamp(straw.Center.x,38,46),3.1f,17.8f),straw.Center);
            yield return PressF(); yield return Wait(1.1f);
        }
    }
    IEnumerator R1Burn() {
        Powers(W,N); yield return At(new Vector3(46.8f,3.1f,14.2f),Center("Rice drying rack (burns!)")); yield return HoldF(.8f);
        Powers(H,N);
        foreach(var x in new[]{39.5f,41.2f,42.9f,44.6f}) { yield return At(new Vector3(x,3.1f,14.3f),new Vector3(x,3.4f,16)); yield return HoldF(.7f); }
        yield return Wait(7f);
    }
    IEnumerator R1Flame() {
        Powers(W,N); yield return At(new Vector3(46.8f,3.1f,14.2f),Center("Rice drying rack (burns!)")); yield return HoldF(.8f);
        Powers(H,P);
        foreach(float x in new[]{39.5f,41.2f,42.9f,44.6f}) { yield return At(new Vector3(x,3.1f,17.8f),new Vector3(x,3.4f,16)); yield return HoldF(.7f); }
        yield return Wait(7f);
    }
    IEnumerator R1HotMetal() {
        BlobMagnetic near=null; foreach(var m in BlobMagnetic.All) if(Vector3.Distance(m.transform.position,new Vector3(42,3,16))<15f && m.Body.mass<=10) near=m;
        if(!near) { Debug.Log("OOPS_V2 note: no magnetic body within 15 m of R1 for Magnet + Heat"); yield break; }
        Powers(W,N); yield return At(new Vector3(46,3.1f,14.2f),Center("Rice drying rack (burns!)")); yield return HoldF(.8f);
        Powers(M,H); yield return At(new Vector3(38.2f,3.1f,14.2f),near.Body.worldCenterOfMass); p.testAction=true; yield return Wait(1f);
        foreach(float x in new[]{39.5f,41.2f,42.9f,44.6f}) { Aim(new Vector3(x,3.4f,16)); yield return Walk(new Vector3(x,3.1f,14.2f)); yield return Wait(1f); }
        p.testAction=false; yield return Wait(7f);
    }
    IEnumerator R2Raft() {
        Powers(W,M); yield return At(new Vector3(51,0,-12),new Vector3(58.7f,-.2f,-12));
        var raft=Obj("R2 iron raft (25 kg)").GetComponent<FerryRaft>();
        p.testSecondary=true; yield return Until(()=>raft.state==FerryRaft.State.Docked,8f); p.testSecondary=false;
        yield return Walk(new Vector3(53.3f,0,-12),3); yield return Until(()=>raft.state==FerryRaft.State.Arrived,5f);
        yield return Walk(new Vector3(62,0,-12),3);
    }
    IEnumerator R3Jet() {
        Powers(W,P);
        foreach(var x in new[]{62f,66f,70f}) foreach(var z in new[]{-10f,-15.5f}) { yield return At(new Vector3(x,0,z+2),new Vector3(x,0,z)); yield return PressF(); yield return Wait(.6f); }
    }
    IEnumerator R3Splash() {
        Powers(B,W);
        foreach(var x in new[]{62f,66f,70f}) foreach(var z in new[]{-10f,-15.5f}) { yield return At(new Vector3(x,.15f,z)); yield return Bounce(Vector2.zero,.4f); yield return Until(()=>p.Grounded && !p.bounceAirborne,3f); yield return Wait(.3f); }
    }
    // ---------- hidden spots ----------
    IEnumerator SecretCases() {
        yield return Case("s_hub_pads","Electric + Bounce chain spark hits all 4 pads",()=>Seq(PowerSet(B,E),At(new Vector3(12,.12f,-12),new Vector3(10.8f,.1f,-10.8f)),PressF(),Wait(1f)));
        yield return Case("s_hub_lights","Pressure + Electric lightning wind lights 3 street lights",()=>Seq(PowerSet(P,E),At(new Vector3(14,0,11.5f),new Vector3(14,3.2f,17)),PressF()));
        yield return Case("s_storage","Pressure + Bounce wind ball hits the corner switch",()=>Seq(PowerSet(P,B),At(new Vector3(12,0,37.1f),Center("Storage switch (round the corner)")),PressF(),Wait(1.5f)));
        yield return Case("s_fuse","Electric + Heat short circuit melts the fuse",()=>Seq(PowerSet(E,H),At(new Vector3(-3.5f,0,62),Center("Fuse (short-circuit lock)")),PressF()));
        yield return Case("s_beam_ice","Heat + Magnet induction heats the beam under the ice",()=>Seq(PowerSet(H,M),At(new Vector3(10,0,53.5f),new Vector3(12,8,57)),HoldF(3f)),null,4);
        yield return Case("s_conveyor_weld","Heat + Electric welds the broken conveyor rail",()=>Seq(PowerSet(H,E),At(new Vector3(16.2f,0,55),Center("Broken conveyor rail (weld me)")),HoldF(1f)));
        yield return Case("s_kiln","Heat + Bounce fireball lobbed over the kiln wall",KilnLob,null,3);
        // Parts that are simply hidden (no objective): reach and collect them.
        yield return Fresh();
        Powers(M,B); yield return At(new Vector3(-10.5f,0,33.5f),new Vector3(-13.9f,6.3f,33.5f)); yield return PressF(); yield return Wait(.3f); yield return Until(()=>p.Grounded,3.5f); yield return Walk(new Vector3(-16.5f,7,33.5f),2); yield return Wait(.5f);
        CheckThat("Hidden shed roof — slingshot to the permanent metal roof anchor",w.ship.Has("part_shed_roof"),"pos="+p.transform.position.ToString("F1"));
        yield return Fresh();
        Powers(B,H); yield return At(new Vector3(12,1.7f,33),Center("Cold room ice floor")); yield return PressF(); yield return Wait(2.5f);
        CheckThat("Hidden cold room — Bounce + Heat fireball melts the ice floor",w.ship.Has("part_cold_room"),"ice consumed="+(Mat("Cold room ice floor")?Mat("Cold room ice floor").consumed:false)+" pos="+p.transform.position.ToString("F1"));
        yield return Fresh();
        var seed=Mat("Roof vine seed"); Powers(W,B);
        yield return At(new Vector3(62.5f,0,9.5f));
        for(int i=0;i<3 && seed.growth<2;i++) yield return LobAt(seed,true);
        bool stair=seed.growth>=2;
        if(stair) { yield return At(new Vector3(60.1f,0,13)); yield return Walk(new Vector3(67.6f,6,13),8); yield return Walk(new Vector3(68,5,12),3); yield return Walk(new Vector3(69.8f,5,12.2f),3); yield return Wait(.5f); }
        CheckThat("Hidden pump roof — Water + Bounce water ball grows the vine stair",stair && w.ship.Has("part_pump_roof"),"growth="+seed.growth+" pos="+p.transform.position.ToString("F1"));
    }
    IEnumerator KilnLob() {
        var kiln=Mat("Charcoal kiln (wood)"); Powers(H,B);
        yield return At(new Vector3(46,0,-10.5f));
        yield return LobAt(kiln,false);
    }
    // Search actual collision trajectories before firing; never move a target or mark it solved.
    // Uses the same sphere radius, time integration and bounce rules as ElementProjectile.
    IEnumerator LobAt(WorldMaterial target,bool water) {
        Vector3 origin=p.transform.position+Vector3.up*.8f;
        Vector3 flat=Vector3.ProjectOnPlane(target.Bounds.center-origin,Vector3.up).normalized;
        bool found=false;
        for(float angle=15;angle<=80;angle+=.5f) {
            Aim(origin+flat*12+Vector3.up*(Mathf.Tan(angle*Mathf.Deg2Rad)*12));
            yield return null; yield return null;
            Vector3 dir=ElementHit.AimDirection(p.view,origin,20f);
            Vector3 pos=origin+dir*.6f,vel=dir*(water?11f:12f)+Vector3.up*(water?3f:2f);
            float gravity=water?9f:12f; int bounces=3;
            for(int i=0;i<200;i++) {
                vel+=Vector3.down*gravity*.02f; Vector3 step=vel*.02f;
                if(Physics.SphereCast(pos,.21f,step.normalized,out RaycastHit hit,step.magnitude,ElementHit.Mask,QueryTriggerInteraction.Ignore)) {
                    if(hit.collider.GetComponentInParent<WorldMaterial>()==target) { found=true; break; }
                    if(bounces--<=0) break;
                    pos=hit.point+hit.normal*.23f; vel=Vector3.Reflect(vel,hit.normal)*.8f;
                } else pos+=step;
            }
            if(found) break;
        }
        if(!found) { Debug.Log("OOPS_V2_DIAG no valid lob to "+target.name); yield break; }
        yield return PressF(); yield return Wait(2.5f);
    }

    // ---------- powers / combos ----------
    IEnumerator PowerChecks() {
        yield return Fresh();
        // Electromagnet mass limit.
        Powers(M,N); var mach=Obj("Old machine (30 kg, magnetic)");
        yield return At(new Vector3(5,0,39.4f),mach.transform.position+Vector3.up*.75f); p.testAction=true; yield return Wait(1f);
        bool plainRefused=pw.held==null; p.testAction=false; yield return Wait(.2f);
        CheckThat("Plain Magnet refuses the 30 kg machine (limit 10 kg)",plainRefused,"held="+(pw.held?pw.held.name:"none"));
        // Water spray puts out a fire and wets; Electric conducts; Heat dries / melts (single verbs).
        yield return Fresh();
        var junk=Mat("Junk bundle (cardboard + scrap wire, 1.5 kg)"); ElementReaction.Apply(H,junk);
        Powers(W,N); yield return At(new Vector3(junk.Bounds.center.x,0,-10.6f),junk.Bounds.center); yield return HoldF(.6f);
        CheckThat("Water spray puts out a burning bundle and wets it",!junk.burning && junk.Wet,"burning="+junk.burning+" wet="+junk.Wet);
        Powers(E,N); int sparks=el.sparks; yield return At(new Vector3(-14,0,-10.6f),new Vector3(-14,8,10)); yield return PressF(); yield return PressF();
        CheckThat("Electric spark has a cooldown and fizzles with no target",el.sparks==sparks+1,"sparks="+(el.sparks-sparks));
        // Projectiles never pile up.
        Powers(H,B); for(int i=0;i<4;i++) { yield return PressF(); yield return Wait(.65f); }
        yield return Wait(4.5f);
        CheckThat("Combo projectiles expire (no build-up)",ElementProjectile.alive==0,"alive="+ElementProjectile.alive+" spawned="+ElementProjectile.spawned);
        // Journal records discoveries and the HUD names the active cell.
        Powers(W,E);
        CheckThat("HUD resolves Water+Electric and Electric+Water to the same combo",el.ActiveCell!=null && el.ActiveCell.id=="water+electric" && el.Cell(E,W)==el.ActiveCell,"active="+(el.ActiveCell!=null?el.ActiveCell.id:"none"));
    }
    // ---------- save v2 + ship ending ----------
    IEnumerator SaveRoundTrip() {
        yield return Fresh();
        w.UseSaveSlot("test_v2",false);
        Pz("f1_gate").Solve(); el.Use(el.Cell(H,E)); w.ship.Collect(w.ship.Find("part_h1")); w.Save();
        yield return Wait(.2f);
        // Restart saves cleared progress: keep the round-trip file in a different slot.
        w.UseSaveSlot("test_v2_reset",false);
        w.Restart(); yield return Wait(.2f);
        bool cleared=Pz("f1_gate").State!=PuzzleState.Solved && !w.ship.Has("part_h1") && !el.Knows("heat+electric");
        var status=w.LoadFromSlot("test_v2"); yield return Wait(.3f);
        CheckThat("Save v2 round trip: puzzles, ship parts and discovered combos come back",cleared && status==SaveSystem.LoadStatus.Ok && Pz("f1_gate").State==PuzzleState.Solved && w.ship.collected.Contains("part_h1") && el.Knows("heat+electric"),
            "cleared="+cleared+" status="+status+" carried="+w.ship.collected.Count+" journal="+el.discovered.Count);
    }
    IEnumerator SaveAndShip() {
        yield return SaveRoundTrip();
        // Install: 3 parts (no rice) -> not finished; 4th from rice -> the ship launches and the demo ends.
        yield return Fresh();
        foreach(var id in new[]{"part_h1","part_f1","part_f2"}) w.ship.Collect(w.ship.Find(id));
        yield return At(new Vector3(8,0,0.5f)); yield return Walk(new Vector3(8,0,2.2f),2); yield return Wait(.4f);
        bool notYet=!w.finished && w.ship.installed.Count==3;
        w.ship.Collect(w.ship.Find("part_r1")); yield return At(new Vector3(-1,0,0)); yield return Walk(new Vector3(3.5f,0,4),3); yield return Wait(.6f);
        CheckThat("Ship: 3 parts without a rice part do not finish; the 4th (rice) launches the ship and ends the demo",notYet && w.ship.launched && w.finished,"installed="+w.ship.installed.Count+" notYet="+notYet+" finished="+w.finished);
        // Zone gate rule: rice opens with F1 + one of F2/F3.
        yield return Fresh();
        ZoneGate rice=null; foreach(var g in w.zoneGates) if(g.zoneId=="rice") rice=g;
        Pz("f1_gate").Solve(); yield return null; bool onlyF1=!rice.Unlocked;
        Pz("f3_turbine").Solve(); yield return Wait(.2f);
        CheckThat("Rice gate: F1 alone keeps it shut, F1 + F3 opens it",onlyF1 && rice.Unlocked,"onlyF1Shut="+onlyF1+" unlocked="+rice.Unlocked);
    }
    // ---------- small combinators ----------
    IEnumerator Seq(params IEnumerator[] steps) { foreach(var s in steps) yield return StartCoroutine(s); }
    IEnumerator PowerSet(BlobProperty a,BlobProperty b) { Powers(a,b); yield return null; }
    IEnumerator Pitch(float deg) { p.pitch=deg; yield return null; }
    IEnumerator WalkFor(Vector2 dir,float s) { p.testMove=dir; yield return Wait(s); p.testMove=Vector2.zero; }
    IEnumerator Repeat(int times,float gap,System.Func<IEnumerator> step,System.Func<bool> done) {
        for(int i=0;i<times && !done();i++) { yield return StartCoroutine(step()); yield return Wait(gap); }
    }
    void Finish() {
        report.completed=true;
        Idle(); p.yaw=yaw0; p.pitch=pitch0; p.testMode=false;
        foreach(var a in aims) if(a) Destroy(a);
        w.Restart();
        string path=TestReports.PathFor(caseFilter!=""?"v2-selected.json":focusedFixesOnly?"v2-focused-fixes.json":"v2-test.json");
        File.WriteAllText(path,JsonUtility.ToJson(report,true));
        foreach(var f in new[]{SaveSystem.PathFor("test_v2"),SaveSystem.PathFor("test_v2_reset")}) if(File.Exists(f)) File.Delete(f);
        Debug.Log("OOPS_V2_DONE passed="+report.passed+" failed="+report.failed+" report="+path);
    }
}
}
