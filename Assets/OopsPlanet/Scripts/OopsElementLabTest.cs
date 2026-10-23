using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// v2 stage B test in the dev Element Lab: element x material reactions, fire spread limits, conduction, steam lift,
// sense mode range, aim marker, Deplete + regen, and an Any-mode puzzle recording which solution solved it.
public class OopsElementLabTest : MonoBehaviour {
    [System.Serializable] public class Check { public string name;public bool passed;public string observed; }
    [System.Serializable] public class Report { public string utc;public List<Check> checks=new List<Check>();public int passed,failed; }
    float yaw0, pitch0;
    static readonly Vector3 Lab=new Vector3(-60,0,-60);
    BlobWorld w; BlobPlayer p; Report report=new Report();
    readonly List<GameObject> aims=new List<GameObject>();
    readonly HashSet<string> ignited=new HashSet<string>();
    void CheckThat(string name,bool pass,string observed) {
        report.checks.Add(new Check{name=name,passed=pass,observed=observed});
        if(pass)report.passed++;else report.failed++;
        Debug.Log("OOPS_LAB "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
    }
    IEnumerator Wait(float s) { float until=Time.time+s; while(Time.time<until) yield return null; }
    void Aim(Vector3 pt) { var g=new GameObject("Lab aim"); g.transform.position=pt; aims.Add(g); p.testAim=g.transform; p.AimAt(g.transform); }
    static WorldMaterial M(string name) { foreach(var m in WorldMaterial.All) if(m.name==name) return m; return null; }
    BlobInteractable Src(string prefix) { foreach(var s in w.sources) if(s && s.name.StartsWith(prefix)) return s; return null; }
    static void A(BlobProperty e,WorldMaterial m) => ElementReaction.Apply(e,m);
    IEnumerator Start() {
        w=GetComponent<BlobWorld>(); p=w.player;
        yaw0=p.yaw; pitch0=p.pitch;
        p.testMode=true; p.paused=false; w.UseSaveSlot("test_run",false); w.Restart(); p.testMode=true;
        report.utc=System.DateTime.UtcNow.ToString("o");
        p.Teleport(Lab+Vector3.up*.1f); yield return Wait(.4f);
        foreach(var m in WorldMaterial.All) { var mm=m; mm.Changed+=(x,what)=>{ if(what=="ignited") ignited.Add(x.name); }; }
        int reactions0=ElementReaction.reactions;
        // --- Fire spread: 3 m from the origin, burns out, wet / critical never catch ---
        var fire=new WorldMaterial[5]; for(int i=0;i<5;i++) fire[i]=M("Lab straw fire "+i);
        CheckThat("Lab materials found",fire[4] && M("Lab ice") && M("Lab steam pool") && M("Lab any: ice door"),"materials="+WorldMaterial.All.Count);
        A(BlobProperty.Water,M("Lab straw wet"));
        A(BlobProperty.Heat,fire[0]); A(BlobProperty.Heat,M("Lab straw wet neighbour")); A(BlobProperty.Heat,M("Lab straw critical neighbour"));
        A(BlobProperty.Heat,M("Lab straw wet")); A(BlobProperty.Heat,M("Lab straw critical"));
        yield return Wait(ElementReaction.SpreadDelay*2+1f);   // fire waits SpreadDelay before each jump
        int row=0; for(int i=0;i<4;i++) if(ignited.Contains(fire[i].name)) row++;
        CheckThat("Fire spreads to straw within 3 m",row==4,"ignited in row="+row);
        float until=Time.time+ElementReaction.FireLifetime; bool anyBurning;
        while(Time.time<until) yield return null;
        yield return Wait(.5f);
        anyBurning=false; foreach(var m in WorldMaterial.All) anyBurning|=m.burning;
        CheckThat("Fire never passes 3 m (5th straw 4 m away untouched)",!ignited.Contains(fire[4].name) && !fire[4].consumed,"ignited="+string.Join(",",ignited));
        CheckThat("Every fire is out within 15 s",!anyBurning,"burning="+anyBurning);
        CheckThat("Wet straw does not catch fire",!ignited.Contains("Lab straw wet") && ignited.Contains("Lab straw wet neighbour"),"wet consumed="+M("Lab straw wet").consumed);
        CheckThat("Puzzle-critical straw never burns",!ignited.Contains("Lab straw critical") && !M("Lab straw critical").consumed && ignited.Contains("Lab straw critical neighbour"),"critical consumed="+M("Lab straw critical").consumed);
        // --- Ice, wax, mud, plant ---
        var ice=M("Lab ice"); var wax=M("Lab wax latch"); var mudDry=M("Lab mud dry"); var mudWash=M("Lab mud wash"); var plant=M("Lab plant");
        A(BlobProperty.Heat,ice); A(BlobProperty.Heat,wax); yield return Wait(1.6f);
        bool iceGone=ice.consumed && !ice.visuals[0].enabled && !ice.colliders[0].enabled;
        CheckThat("Heat melts ice and wax (collider off, visual shrinks away)",iceGone && wax.consumed,"ice="+ice.consumed+" wax="+wax.consumed);
        A(BlobProperty.Heat,mudDry); A(BlobProperty.Water,mudDry); A(BlobProperty.Water,mudWash); yield return null;
        CheckThat("Heat dries mud (stays as ground); water washes wet mud away",mudDry.dried && !mudDry.consumed && mudWash.consumed,"dried="+mudDry.dried+" washed="+mudWash.consumed);
        A(BlobProperty.Water,plant); A(BlobProperty.Water,plant); yield return Wait(.6f);
        CheckThat("Water grows plants (2 stages)",plant.growth==2 && plant.visuals[0].transform.localScale.y>1.6f,"growth="+plant.growth+" scaleY="+plant.visuals[0].transform.localScale.y.ToString("F2"));
        // --- Electricity ---
        A(BlobProperty.Electric,M("Lab metal bar 0")); yield return null;
        CheckThat("Electric runs along touching metal to a machine",M("Lab machine A (via metal)").Powered && !M("Lab machine far (not touching)").Powered,"A="+M("Lab machine A (via metal)").Powered+" far="+M("Lab machine far (not touching)").Powered);
        A(BlobProperty.Electric,M("Lab puddle")); yield return null;
        CheckThat("Electric runs through water to a machine standing in it",M("Lab machine B (in water)").Powered,"B="+M("Lab machine B (in water)").Powered);
        // --- Steam lift ---
        var steam=w.GetComponent<SteamSystem>(); int cols=steam.columnsRaised;
        var pool=M("Lab steam pool"); p.Teleport(pool.Bounds.center+Vector3.up*.2f); yield return Wait(.5f);
        float y0=p.transform.position.y, yMax=y0;
        A(BlobProperty.Heat,pool); until=Time.time+2f; while(Time.time<until) { yMax=Mathf.Max(yMax,p.transform.position.y); yield return null; }
        CheckThat("Heat on water raises steam that lifts BLOB",steam.columnsRaised==cols+1 && yMax-y0>1.5f,"columns="+(steam.columnsRaised-cols)+" lift="+(yMax-y0).ToString("F2"));
        yield return Wait(3f);
        // --- Sense mode ---
        var near=Src("Lab source near"); var far=Src("Lab source far"); var sense=p.GetComponent<BlobSense>();
        p.Teleport(Lab+Vector3.up*.1f); yield return Wait(.5f);
        var nv=near.GetComponent<AbsorbableVisual>(); var fv=far.GetComponent<AbsorbableVisual>();
        bool labelsOffBefore=!near.powerLabel.activeSelf && !far.powerLabel.activeSelf;
        int uses=sense.senseUses; p.testSense=true; yield return Wait(.5f);
        CheckThat("Sense mode highlights absorbables within 12 m only",nv.sensed && !fv.sensed && near.powerLabel.activeSelf && !far.powerLabel.activeSelf && labelsOffBefore,
            "near="+Vector3.Distance(BlobSense.Origin,near.AimPoint).ToString("F1")+"m sensed="+nv.sensed+" far="+Vector3.Distance(BlobSense.Origin,far.AimPoint).ToString("F1")+"m sensed="+fv.sensed);
        CheckThat("Sense mode dims the world and slows BLOB",sense.senseVolume.weight>.9f && Mathf.Approximately(p.speedScale,sense.walkScale) && sense.senseUses==uses+1,"weight="+sense.senseVolume.weight.ToString("F2")+" speed="+p.speedScale);
        p.testSense=false; yield return Wait(.5f);
        CheckThat("Releasing sense hides labels and restores the view",!nv.sensed && !near.powerLabel.activeSelf && sense.senseVolume.weight<.05f && p.speedScale==1,"weight="+sense.senseVolume.weight.ToString("F2"));
        // --- Marker only on a valid target ---
        Aim(far.AimPoint); yield return Wait(.3f);
        bool farMarker=p.targetMarker.activeSelf; string farStatus=p.targetStatus;
        var dep=Src("Lab source deplete");
        p.testAim=null; p.Teleport(Lab+new Vector3(0,.1f,-1.5f)); yield return Wait(.3f);
        Aim(dep.AimPoint); yield return Wait(.3f);
        CheckThat("[E] marker only on a valid, in-range target",!farMarker && p.targetMarker.activeSelf && p.target==dep,"far status="+farStatus+" marker="+farMarker+" dep marker="+p.targetMarker.activeSelf);
        // --- Deplete + regen ---
        p.testAbsorb=true; yield return Wait(1.1f); p.testAbsorb=false; yield return null;
        var dv=dep.GetComponent<AbsorbableVisual>();
        bool drained=dep.consumed && !dep.Available && dep.gameObject.activeSelf && dep.fullVisual.activeSelf;
        CheckThat("Deplete: source stays in place but is drained",drained && p.property==BlobProperty.Magnet,"consumed="+dep.consumed+" property="+p.property);
        p.testAim=null;
        yield return Wait(18f);
        bool stillDrained=!dep.Available;
        yield return Wait(2.6f);
        CheckThat("Drained source regenerates after 20 s",stillDrained && dep.Available,"at18s="+(!stillDrained?"back":"drained")+" at20.6s="+dep.Available);
        // --- Any-mode puzzle, both solutions ---
        PuzzleObjective any=null; foreach(var o in PuzzleObjective.All) if(o.id=="lab_any") any=o;
        A(BlobProperty.Heat,M("Lab any: ice door")); yield return Wait(1.2f);
        bool s1=any.State==PuzzleState.Solved; string by1=any.solvedBy;
        any.ResetPuzzle(true); M("Lab any: ice door").ResetState(); yield return Wait(.2f);
        A(BlobProperty.Electric,M("Lab any: lift motor")); yield return Wait(1.2f);
        bool s2=any.State==PuzzleState.Solved; string by2=any.solvedBy;
        CheckThat("Any-mode puzzle: either solution solves it and solvedBy records which",s1 && by1=="melt-ice" && s2 && by2=="power-motor","1="+s1+"/"+by1+" 2="+s2+"/"+by2);
        CheckThat("Reactions counted",ElementReaction.reactions>reactions0+15,"reactions="+(ElementReaction.reactions-reactions0));
        // --- Restart resets every material ---
        w.Restart(); yield return Wait(.3f);
        bool clean=true; foreach(var m in WorldMaterial.All) clean&=!m.consumed && !m.burning && !m.dried && m.growth==0 && !m.Wet && !m.Powered;
        CheckThat("Restart resets every material",clean,"clean="+clean);
        Finish();
    }
    void Finish() {
        p.yaw=yaw0; p.pitch=pitch0; // leave the camera as found: tests run back to back in one Play session
        p.testMove=Vector2.zero; p.testAbsorb=false; p.testSense=false; p.testMode=false; p.testAim=null;
        foreach(var a in aims) if(a) Destroy(a);
        w.Restart();
        string path=TestReports.PathFor("stageB-element-lab-test.json");
        File.WriteAllText(path,JsonUtility.ToJson(report,true));
        Debug.Log("OOPS_LAB_DONE passed="+report.passed+" failed="+report.failed+" report="+path);
    }
}
}
