using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// P04 puzzle test: solves Factory Gate -> Ventilation Turbine -> Rice Straw with real movement, absorption and powers,
// and checks world restoration, objective UI, double-fire guard, reset pads, out-of-world recovery, respawn and restart.
public class OopsPuzzleTest : MonoBehaviour {
    [System.Serializable] public class Check { public string name;public bool passed;public string observed; }
    [System.Serializable] public class Report { public string utc;public List<Check> checks=new List<Check>();public int passed,failed;public bool allSolved;public List<string> diagnostics=new List<string>(); }
    void Diag(string line) { report.diagnostics.Add(line); Debug.Log("OOPS_P04_DIAG "+line); }
    BlobWorld w; BlobPlayer p; BlobPowers pw; Report report=new Report();
    readonly List<GameObject> aims=new List<GameObject>();
    void CheckThat(string name,bool pass,string observed) {
        report.checks.Add(new Check{name=name,passed=pass,observed=observed});
        if(pass)report.passed++;else report.failed++;
        Debug.Log("OOPS_P04 "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
    }
    IEnumerator Wait(float seconds) { float until=Time.time+seconds;while(Time.time<until)yield return null; }
    IEnumerator To(Vector3 goal,float timeout=10f) {
        p.testAim=null;
        float until=Time.time+timeout;
        while(Time.time<until) {
            Vector3 d=goal-p.transform.position;d.y=0;
            if(d.magnitude<.25f)break;
            p.testMove=new Vector2(d.x,d.z).normalized;
            yield return null;
        }
        p.testMove=Vector2.zero;
    }
    void Aim(Vector3 point) { var go=new GameObject("P04 aim");go.transform.position=point;aims.Add(go);p.testAim=go.transform;p.AimAt(go.transform); }
    IEnumerator Absorb(BlobInteractable s) { Aim(s.AimPoint); yield return Wait(.3f); p.testAbsorb=true; yield return Wait(1.1f); p.testAbsorb=false; yield return null; }
    IEnumerator Pulse(bool secondary) {
        if(secondary) p.testSecondary=true; else p.testAction=true;
        yield return null; yield return null;
        if(secondary) p.testSecondary=false; else p.testAction=false;
        yield return null;
    }
    BlobInteractable Source(string prefix) { foreach(var s in w.sources) if(s.name.StartsWith(prefix)) return s; return null; }
    PuzzleObjective Puzzle(string id) { foreach(var pz in w.puzzles) if(pz.id==id) return pz; return null; }
    PuzzleResetPad Pad(PuzzleObjective target) { foreach(var pad in Object.FindObjectsByType<PuzzleResetPad>(FindObjectsSortMode.None)) if(pad.target==target) return pad; return null; }
    static string V(Vector3 v) => v.ToString("F2");
    IEnumerator Start() {
        w=GetComponent<BlobWorld>();p=w.player;pw=p.powers;p.testMode=true;p.paused=false;w.UseSaveSlot("test_run",false);w.Restart();p.testMode=true;
        report.utc=System.DateTime.UtcNow.ToString("o");
        yield return Wait(.5f);
        var gate=Puzzle("factory_gate"); var vent=Puzzle("factory_turbine"); var rice=Puzzle("rice_straw");
        CheckThat("Three puzzles registered",gate&&vent&&rice && w.puzzles.Length==3,"count="+w.puzzles.Length);
        CheckThat("Initial states: gate active, turbine locked behind gate, rice active",
            gate.State==PuzzleState.Active && vent.State==PuzzleState.Locked && rice.State==PuzzleState.Active,gate.State+"/"+vent.State+"/"+rice.State);
        var turbine=(PressureTurbine)vent.conditions[0];
        turbine.OnPressure(Vector3.forward,1);
        CheckThat("Locked turbine ignores Pressure",turbine.spin==0,"spin="+turbine.spin);
        CheckThat("HUD shows restoration progress 0/3 in the Hub",w.PuzzlePanelText().Contains("ฟื้นฟูโลก 0 / 3"),w.PuzzlePanelText().Replace("\n"," | "));

        // ---------- Puzzle 1: Factory Gate ----------
        var sock=(ItemSocket)gate.conditions[0]; var weight=gate.items[0]; var weightStart=weight.transform.position;
        var gateReaction=gate.GetComponent<PuzzleReaction>(); var door=gateReaction.mover; float doorY0=door.position.y;
        p.Teleport(new Vector3(0,.1f,27)); yield return Wait(.3f);
        CheckThat("Objective UI shows the gate puzzle at the factory entrance",w.CurrentPuzzle==gate && w.PuzzlePanelText().Contains(gate.titleTh) && w.PuzzlePanelText().Contains(gate.hintTh),w.PuzzlePanelText().Replace("\n"," | "));
        yield return To(new Vector3(10,0,25));
        yield return Absorb(Source("Factory Pressure source"));
        yield return To(new Vector3(-10,0,28));
        var magnetSrc=Source("Factory Magnet source");
        yield return Absorb(magnetSrc);
        CheckThat("Factory sources give Magnet + Pressure",p.property==BlobProperty.Magnet && p.secondary==BlobProperty.Pressure,p.property+"/"+p.secondary);
        CheckThat("Puzzle-area source remains after absorbing",magnetSrc.Available,"available="+magnetSrc.Available);
        // Retry path: drag the weight a little, then the reset pad returns it.
        Aim(weight.Center); yield return Wait(.2f); p.testAction=true; yield return Wait(1f); p.testAction=false; yield return Wait(.5f);
        float dragged=Vector3.Distance(weight.transform.position,weightStart);
        var gatePad=Pad(gate);
        yield return To(gatePad.transform.position); yield return Wait(1.4f);
        CheckThat("Reset pad returns the misplaced counterweight",dragged>.5f && Vector3.Distance(weight.transform.position,weightStart)<.3f && gatePad.resets==1,"dragged="+dragged.ToString("F2")+" resets="+gatePad.resets);
        // Softlock: weight stranded on the warehouse roof -> reset pad; weight out of the world -> automatic return.
        weight.Body.position=new Vector3(-9,8.7f,45); weight.transform.position=weight.Body.position; Physics.SyncTransforms();
        yield return To(gatePad.transform.position+new Vector3(2.5f,0,0)); yield return Wait(2f);
        yield return To(gatePad.transform.position); yield return Wait(1.4f);
        CheckThat("No softlock: stranded counterweight recovered by reset pad",Vector3.Distance(weight.transform.position,weightStart)<.3f && gatePad.resets==2,"pos="+V(weight.transform.position)+" resets="+gatePad.resets);
        weight.Body.position=new Vector3(-8,-20,30); weight.transform.position=weight.Body.position; yield return Wait(.3f);
        CheckThat("No softlock: counterweight below the world returns to start",Vector3.Distance(weight.transform.position,weightStart)<.3f,"pos="+V(weight.transform.position));
        // Main solution: pull the weight with Magnet, walk it to the socket, release.
        yield return To(new Vector3(-3,0,34));
        Aim(weight.Center); yield return Wait(.2f); p.testAction=true; yield return Wait(1.8f);
        bool holding=pw.HeldClose;
        p.testAim=null; float until=Time.time+8;
        while(Time.time<until) {
            Vector3 d=new Vector3(5,0,41.2f)-p.transform.position; d.y=0;
            if(d.magnitude<.25f) break;
            p.testMove=new Vector2(d.x,d.z).normalized; yield return null;
        }
        p.testMove=Vector2.zero; yield return Wait(.4f);
        p.testAction=false; yield return Wait(2.2f);
        CheckThat("Factory Gate solved with Magnet (main solution)",holding && gate.State==PuzzleState.Solved,"held="+holding+" weight="+V(weight.transform.position)+" socket="+sock.Progress01);
        CheckThat("Gate door opened and lamp turned green",door.position.y>doorY0+3.5f,"doorY="+door.position.y.ToString("F2"));
        CheckThat("Turbine unlocks after the gate",vent.State==PuzzleState.Active,"turbine="+vent.State);
        yield return Wait(1.5f);
        CheckThat("Solved event fires exactly once",gate.solvedCount==1,"solvedCount="+gate.solvedCount);
        // ---------- Puzzle 2: Ventilation Turbine ----------
        var ventReaction=vent.GetComponent<PuzzleReaction>(); var smoke=ventReaction.stopWhenSolved[0];
        CheckThat("Chimney smoke is visible before the turbine",smoke.isEmitting,"emitting="+smoke.isEmitting);
        yield return To(new Vector3(0,0,40)); yield return To(new Vector3(0,0,45)); yield return To(new Vector3(6,0,49.5f));
        CheckThat("Walked through the opened gate into the turbine room",p.transform.position.z>48,"pos="+V(p.transform.position));
        CheckThat("Objective UI shows the turbine puzzle",w.CurrentPuzzle==vent && w.PuzzlePanelText().Contains(vent.titleTh),w.PuzzlePanelText().Replace("\n"," | "));
        Aim(turbine.transform.position); yield return Wait(.3f);
        int pulses=0;
        for(int i=0;i<6 && vent.State!=PuzzleState.Solved;i++) { yield return Pulse(true); pulses++; yield return Wait(1.1f); }
        yield return Wait(.6f);
        CheckThat("Ventilation Turbine solved with Pressure",vent.State==PuzzleState.Solved,"pulses="+pulses+" blasts="+turbine.blastsReceived+" spin="+turbine.spin.ToString("F2"));
        yield return Wait(2f);
        CheckThat("Factory restored: vent open, smoke stopped, lights warm",!ventReaction.hideWhenSolved[0].activeSelf && ventReaction.showWhenSolved[0].activeSelf && !smoke.isEmitting && ventReaction.lights[0].color.g>.6f,
            "smoke="+smoke.isEmitting+" light="+ventReaction.lights[0].color);
        // ---------- Puzzle 3: Rice Straw Route ----------
        var clear=(ClearZoneCondition)rice.conditions[0]; var riceReaction=rice.GetComponent<PuzzleReaction>();
        p.Teleport(new Vector3(29,.1f,0)); yield return Wait(.3f);
        CheckThat("Objective UI shows the rice puzzle",w.CurrentPuzzle==rice && w.PuzzlePanelText().Contains(rice.hintTh),w.PuzzlePanelText().Replace("\n"," | "));
        yield return To(new Vector3(31.5f,0,1.6f)); yield return Absorb(Source("Rice Bounce source"));
        yield return To(new Vector3(31.5f,0,-1.6f)); yield return Absorb(Source("Rice Pressure source"));
        CheckThat("Rice sources give Pressure + Bounce",p.property==BlobProperty.Pressure && p.secondary==BlobProperty.Bounce,p.property+"/"+p.secondary);
        yield return To(new Vector3(42,0,7.2f));
        p.testAim=null; p.yaw=0; yield return Wait(.2f);
        p.testJump=true; yield return Wait(.9f); p.testJump=false;
        until=Time.time+3;
        while(Time.time<until) {
            p.testMove=p.transform.position.z<9.4f?new Vector2(0,1):Vector2.zero;
            if(p.Grounded && p.transform.position.y>2.8f) break;
            yield return null;
        }
        p.testMove=Vector2.zero; yield return Wait(1.5f);
        CheckThat("Bounce reaches the 3 m sluice platform",p.transform.position.y>2.8f,"pos="+V(p.transform.position));
        int blasts=0;
        var zoneBounds=new Bounds(clear.transform.position,clear.size);
        System.Func<string> strawState=()=>{ var sb=new System.Text.StringBuilder(); for(int k=0;k<rice.items.Length;k++){ var s=rice.items[k]; sb.Append("#"+k+V(s.Center)+(zoneBounds.Contains(s.Center)?"[in]":"[out]")+" v="+(s.Body?s.Body.linearVelocity.magnitude.ToString("F1"):"-")+"  "); } return sb.ToString(); };
        Diag("before blasts: player="+V(p.transform.position)+" grounded="+p.Grounded+" slots="+p.property+"/"+p.secondary+" zone="+V(zoneBounds.min)+".."+V(zoneBounds.max)+" | "+strawState());
        for(int i=0;i<10 && rice.State!=PuzzleState.Solved;i++) {
            PuzzleItem next=null; int idx=-1;
            for(int k=0;k<rice.items.Length;k++) if(zoneBounds.Contains(rice.items[k].Center)) { next=rice.items[k]; idx=k; break; }
            if(!next) { Diag("blast "+i+": no straw in zone, waiting"); yield return Wait(.5f); continue; }
            Aim(next.Center); yield return Wait(.25f);
            int realBefore=pw.blasts; float cd=pw.PressureCooldown01;
            Vector3 origin=p.transform.position+Vector3.up*.8f, aim=p.view.transform.forward;
            Vector3 closest=next.GetComponentInChildren<Collider>().ClosestPoint(origin);
            float angle=Vector3.Angle(aim,closest-origin), dist=Vector3.Distance(origin,closest);
            Vector3 before=next.Center;
            yield return Pulse(false); blasts++; yield return Wait(1.1f);
            Diag("blast "+i+": target #"+idx+" player="+V(p.transform.position)+" grounded="+p.Grounded+" camFwd="+V(aim)+" dist="+dist.ToString("F2")+" coneAngle="+angle.ToString("F1")+
                " cdBefore="+cd.ToString("F2")+" realBlast="+(pw.blasts-realBefore)+" targetMoved="+Vector3.Distance(before,next.Center).ToString("F2")+" feedback="+p.feedback+" | "+strawState());
        }
        yield return Wait(1f);
        CheckThat("Rice Straw Route solved with Bounce + Pressure",rice.State==PuzzleState.Solved,"blasts="+blasts+" strawLeft="+clear.Count());
        yield return Wait(3f);
        var plant=riceReaction.grow[0];
        CheckThat("Rice restored: water flows, sluice opens, plants grow, pump light on",
            riceReaction.showWhenSolved[0].activeSelf && !riceReaction.hideWhenSolved[0].activeSelf && plant.localScale.y>2f && riceReaction.lights[0].intensity>2 && riceReaction.mover.position.y>1,
            "plantY="+plant.localScale.y.ToString("F2")+" pump="+riceReaction.lights[0].intensity.ToString("F1"));
        CheckThat("Objective UI matches state: solved + 3/3",w.PuzzlePanelText().Contains("สำเร็จ") && w.PuzzlePanelText().Contains("ฟื้นฟูโลก 3 / 3"),w.PuzzlePanelText().Replace("\n"," | "));
        report.allSolved=gate.State==PuzzleState.Solved&&vent.State==PuzzleState.Solved&&rice.State==PuzzleState.Solved;
        // ---------- Respawn / reset / restart ----------
        w.Respawn(); yield return Wait(.5f);
        CheckThat("Respawn keeps puzzles solved and the world restored",report.allSolved && gate.State==PuzzleState.Solved && vent.State==PuzzleState.Solved && rice.State==PuzzleState.Solved && door.position.y>doorY0+3.5f && riceReaction.showWhenSolved[0].activeSelf,
            "states="+gate.State+"/"+vent.State+"/"+rice.State);
        var ricePad=Pad(rice);
        p.Teleport(ricePad.transform.position+Vector3.up*.1f); yield return Wait(1.5f);
        CheckThat("Reset pad does not undo a solved puzzle",rice.State==PuzzleState.Solved && clear.Count()==0,"state="+rice.State+" straw="+clear.Count());
        w.Restart(); yield return Wait(.6f);
        CheckThat("Restart resets all puzzles and world state",gate.State==PuzzleState.Active && vent.State==PuzzleState.Locked && rice.State==PuzzleState.Active &&
            Mathf.Abs(door.position.y-doorY0)<.05f && smoke.isEmitting && clear.Count()==4 && Vector3.Distance(weight.transform.position,weightStart)<.3f && !riceReaction.showWhenSolved[0].activeSelf,
            "states="+gate.State+"/"+vent.State+"/"+rice.State+" straw="+clear.Count()+" smoke="+smoke.isEmitting);
        // Straw that leaves the level lands at its drop point outside the zone (progress kept, never back on the platform).
        var lost=rice.items[0]; lost.Body.position=new Vector3(40,-20,20); lost.transform.position=lost.Body.position; yield return Wait(.5f);
        CheckThat("No softlock/regress: straw out of the world lands at its drop point outside the zone",
            Vector3.Distance(new Vector3(lost.transform.position.x,0,lost.transform.position.z),new Vector3(lost.fallbackPosition.x,0,lost.fallbackPosition.z))<.6f && !new Bounds(clear.transform.position,clear.size).Contains(lost.Center) && clear.Count()==3,
            "pos="+V(lost.transform.position)+" inZone="+clear.Count());
        Finish();
    }
    void Finish() {
        p.testMove=Vector2.zero;p.testJump=false;p.testAction=false;p.testSecondary=false;p.testAbsorb=false;p.testMode=false;p.testAim=null;
        foreach(var a in aims) if(a) Destroy(a);
        string path=TestReports.PathFor("p04-puzzle-test.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
        Debug.Log("OOPS_P04_DONE passed="+report.passed+" failed="+report.failed+" allSolved="+report.allSolved+" report="+path);
    }
}
}
