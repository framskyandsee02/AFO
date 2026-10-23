using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// P03 power test in the Hub power lab: 2-slot rules, Magnet / Pressure / Bounce, the two supported combos,
// the unsupported-pair fallback, repeated switching and state reset.
public class OopsPowerTest : MonoBehaviour {
    [System.Serializable] public class Check { public string name;public bool passed;public string observed; }
    [System.Serializable] public class Report { public string utc;public List<Check> checks=new List<Check>();public int passed,failed; }
    BlobWorld w; BlobPlayer p; BlobPowers pw; Report report=new Report();
    readonly List<GameObject> aims=new List<GameObject>();
    float maxBodySpeed, minBodyY=999;
    void CheckThat(string name,bool pass,string observed) {
        report.checks.Add(new Check{name=name,passed=pass,observed=observed});
        if(pass)report.passed++;else report.failed++;
        Debug.Log("OOPS_P03 "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
    }
    IEnumerator Wait(float seconds) { float until=Time.time+seconds;while(Time.time<until)yield return null; }
    void Aim(Vector3 point) { var go=new GameObject("P03 aim");go.transform.position=point;aims.Add(go);p.testAim=go.transform;p.AimAt(go.transform); }
    void Idle() { p.testMove=Vector2.zero;p.testAction=false;p.testSecondary=false;p.testPush=false;p.testJump=false;p.testAbsorb=false; }
    IEnumerator Pulse(bool secondary) {
        if(secondary) p.testSecondary=true; else p.testAction=true;
        yield return null; yield return null;
        if(secondary) p.testSecondary=false; else p.testAction=false;
        yield return null;
    }
    IEnumerator Setup(BlobProperty a,BlobProperty b,Vector3 pos) {
        Idle(); pw.ResetState(); BlobMagnetic.ResetAll();
        p.Teleport(pos); p.SetProperties(a,b);
        yield return Wait(.35f);
    }
    BlobMagnetic Magnetic(string prefix,int index=0) {
        int i=0; foreach(var m in BlobMagnetic.All) if(m.name.StartsWith(prefix) && i++==index) return m; return null;
    }
    bool ValidSlots() => (p.property!=p.secondary || p.property==BlobProperty.None) && !(p.property==BlobProperty.None && p.secondary!=BlobProperty.None);
    void FixedUpdate() {
        foreach(var m in BlobMagnetic.All) { maxBodySpeed=Mathf.Max(maxBodySpeed,m.Body.linearVelocity.magnitude); minBodyY=Mathf.Min(minBodyY,m.transform.position.y); }
    }
    IEnumerator Start() {
        w=GetComponent<BlobWorld>();p=w.player;pw=p.powers;p.testMode=true;p.paused=false;w.Restart();p.testMode=true;
        report.utc=System.DateTime.UtcNow.ToString("o");
        yield return Wait(.4f);
        var starts=new Dictionary<BlobMagnetic,Vector3>();
        foreach(var m in BlobMagnetic.All) starts[m]=m.transform.position;
        CheckThat("Power data loaded (3 powers, 2 combos)",pw.set && pw.Def(BlobProperty.Magnet) && pw.Def(BlobProperty.Pressure) && pw.Def(BlobProperty.Bounce) && pw.set.combinations.Count==2,"combos="+(pw.set?pw.set.combinations.Count:0));
        // --- 2-slot rules ---
        p.ClearProperties();
        p.AddProperty(BlobProperty.Magnet); p.AddProperty(BlobProperty.Pressure);
        CheckThat("Second absorb: new primary, old moves to secondary",p.property==BlobProperty.Pressure && p.secondary==BlobProperty.Magnet,p.property+"/"+p.secondary);
        p.AddProperty(BlobProperty.Bounce);
        CheckThat("Third absorb drops the oldest (max 2 active)",p.property==BlobProperty.Bounce && p.secondary==BlobProperty.Pressure && !p.Has(BlobProperty.Magnet),p.property+"/"+p.secondary);
        p.AddProperty(BlobProperty.Pressure);
        CheckThat("Re-absorbing the secondary swaps instead of duplicating",p.property==BlobProperty.Pressure && p.secondary==BlobProperty.Bounce,p.property+"/"+p.secondary);
        p.SwapProperties();
        CheckThat("Swap exchanges primary and secondary",p.property==BlobProperty.Bounce && p.secondary==BlobProperty.Pressure,p.property+"/"+p.secondary);
        var rng=new System.Random(3); bool ok=true; var all=new[]{BlobProperty.Magnet,BlobProperty.Pressure,BlobProperty.Bounce};
        for(int i=0;i<40;i++) { if(rng.Next(3)==0) p.SwapProperties(); else p.AddProperty(all[rng.Next(3)]); ok&=ValidSlots(); yield return null; }
        CheckThat("40 random absorbs/swaps keep slots valid",ok,p.property+"/"+p.secondary);
        // --- Magnet ---
        var cube=Magnetic("Magnetic cube");var heavy=Magnetic("Heavy steel");
        var crate=GameObject.Find("Pressure test crate (non-magnetic)");
        yield return Setup(BlobProperty.Magnet,BlobProperty.None,new Vector3(5.25f,.1f,-1));
        float before=Vector3.Distance(cube.transform.position,p.transform.position);
        Aim(cube.Body.worldCenterOfMass); yield return Wait(.2f);
        p.testAction=true; yield return Wait(2f);
        float after=Vector3.Distance(cube.transform.position,p.transform.position);
        CheckThat("Magnet pulls a light magnetic cube",pw.held==cube && after<before-2,"before="+before.ToString("F1")+" after="+after.ToString("F1"));
        p.testPush=true; yield return Wait(.6f); p.testPush=false;
        float pushed=Vector3.Distance(cube.transform.position,p.transform.position);
        CheckThat("Shift + Magnet pushes it away",pushed>after+1,"after="+after.ToString("F1")+" pushed="+pushed.ToString("F1"));
        Idle(); yield return Wait(.2f);
        CheckThat("Releasing F drops the body",pw.held==null,"held="+pw.held);
        Vector3 hb=heavy.transform.position;
        Aim(heavy.Body.worldCenterOfMass); yield return Wait(.2f); p.testAction=true; yield return Wait(1.2f);
        CheckThat("Mass limit: 40 kg block is not moved",pw.held==null && Vector3.Distance(hb,heavy.transform.position)<.2f,"moved="+Vector3.Distance(hb,heavy.transform.position).ToString("F2"));
        Idle(); Vector3 cb=crate.transform.position;
        Aim(crate.GetComponentInChildren<Renderer>().bounds.center); yield return Wait(.2f); p.testAction=true; yield return Wait(1f); Idle();
        CheckThat("Non-magnetic crate ignores Magnet",Vector3.Distance(cb,crate.transform.position)<.2f,"moved="+Vector3.Distance(cb,crate.transform.position).ToString("F2"));
        // --- Pressure ---
        yield return Setup(BlobProperty.Pressure,BlobProperty.None,new Vector3(4,.1f,-7.4f));
        cb=crate.transform.position; int blasts=pw.blasts;
        Aim(crate.GetComponentInChildren<Renderer>().bounds.center); yield return Wait(.2f);
        yield return Pulse(false); yield return Wait(.7f);
        CheckThat("Pressure blast pushes the crate",pw.blasts==blasts+1 && Vector3.Distance(cb,crate.transform.position)>.8f,"moved="+Vector3.Distance(cb,crate.transform.position).ToString("F2"));
        yield return Pulse(false);
        CheckThat("Pressure cooldown blocks an immediate second blast",pw.blasts==blasts+1 && pw.PressureCooldown01>0,"blasts="+(pw.blasts-blasts)+" cd="+pw.PressureCooldown01.ToString("F2"));
        yield return Wait(1.05f); yield return Pulse(false);
        CheckThat("Pressure works again after cooldown",pw.blasts==blasts+2,"blasts="+(pw.blasts-blasts));
        // --- Bounce ---
        yield return Setup(BlobProperty.Bounce,BlobProperty.None,new Vector3(12,.1f,-6.5f));
        p.testAim=null; p.yaw=180;
        p.testJump=true; yield return Wait(.9f); p.testJump=false; p.testMove=new Vector2(0,-1);
        float top=0,until=Time.time+2.5f;
        while(Time.time<until) { top=Mathf.Max(top,p.transform.position.y); if(p.Grounded && p.transform.position.y>3.3f) break; yield return null; }
        p.testMove=Vector2.zero; yield return Wait(.5f);
        CheckThat("Charged Bounce reaches the 3.5 m ledge (normal jump is 1.4 m)",p.transform.position.y>3.3f,"y="+p.transform.position.y.ToString("F2")+" peak="+top.ToString("F2"));
        yield return Setup(BlobProperty.Bounce,BlobProperty.None,new Vector3(0,.1f,-10));
        p.testAim=null; p.testJump=true; yield return Wait(.9f); p.testJump=false;
        int maxRebounds=0; float peak=0; until=Time.time+5f; bool settled=false;
        while(Time.time<until) {
            maxRebounds=Mathf.Max(maxRebounds,p.rebounds); peak=Mathf.Max(peak,p.transform.position.y);
            if(Time.time>until-4f && p.Grounded && !p.bounceAirborne) { settled=true; break; }
            yield return null;
        }
        var bd=pw.Def(BlobProperty.Bounce);
        CheckThat("Bounce rebounds decay and settle (no infinite bounce)",settled && maxRebounds>=1 && maxRebounds<=bd.maxRebounds,"rebounds="+maxRebounds+" peak="+peak.ToString("F1")+" settled="+settled);
        CheckThat("Bounce height stays within tuned limit",peak<bd.maxLaunch*bd.maxLaunch/(2*p.gravity)+.5f,"peak="+peak.ToString("F2"));
        // --- Combo: Magnet + Pressure ---
        yield return Setup(BlobProperty.Magnet,BlobProperty.Pressure,new Vector3(5.25f,.1f,-1));
        CheckThat("Magnet + Pressure resolves to a combo",pw.ActiveCombo && pw.ActiveCombo.effect==ComboEffect.MagnetCannon,pw.ActiveCombo?pw.ActiveCombo.nameEn:"none");
        Aim(cube.Body.worldCenterOfMass); yield return Wait(.2f);
        p.testAction=true; yield return Wait(2.2f);
        bool wasHeld=pw.HeldClose; int cannons=pw.cannons;
        yield return Pulse(true); p.testAction=false;
        float launchSpeed=cube.Body.linearVelocity.magnitude;
        yield return Wait(1f);
        CheckThat("Magnet Cannon launches the held cube",wasHeld && pw.cannons==cannons+1 && pw.held==null && launchSpeed>8,"held="+wasHeld+" speed="+launchSpeed.ToString("F1")+" dist="+Vector3.Distance(cube.transform.position,p.transform.position).ToString("F1"));
        // --- Combo: Pressure + Bounce ---
        yield return Setup(BlobProperty.Bounce,BlobProperty.Pressure,new Vector3(0,.1f,-10));
        CheckThat("Pressure + Bounce resolves to a combo",pw.ActiveCombo && pw.ActiveCombo.effect==ComboEffect.PressureDash,pw.ActiveCombo?pw.ActiveCombo.nameEn:"none");
        Aim(new Vector3(0,1.4f,12)); yield return Wait(.2f);
        int b0=pw.blasts; yield return Pulse(true);
        CheckThat("On the ground Pressure is a normal blast (no dash)",pw.blasts==b0+1 && pw.dashes==0,"blasts="+(pw.blasts-b0)+" dashes="+pw.dashes);
        yield return Wait(1.1f);
        Vector3 start=p.transform.position; p.testJump=true; yield return Wait(.15f); p.testJump=false; yield return Wait(.3f);
        yield return Pulse(true);
        until=Time.time+3; while(Time.time<until && !(p.Grounded && !p.bounceAirborne)) yield return null;
        float dashDist=Vector3.Distance(new Vector3(start.x,0,start.z),new Vector3(p.transform.position.x,0,p.transform.position.z));
        CheckThat("Pressure Dash in the air moves BLOB forward",pw.dashes==1 && dashDist>2.5f,"dashes="+pw.dashes+" horizontal="+dashDist.ToString("F2"));
        // --- Unsupported pair ---
        yield return Setup(BlobProperty.Magnet,BlobProperty.Bounce,new Vector3(0,.1f,-10));
        int c0=pw.cannons,d0=pw.dashes;
        p.testJump=true; yield return Wait(.15f); p.testJump=false; yield return Wait(.25f);
        yield return Pulse(false);
        CheckThat("Magnet + Bounce is not combined, BLOB says so",pw.ActiveCombo==null && p.feedback.Contains("ผสมกันไม่ได้") && pw.cannons==c0 && pw.dashes==d0,"feedback="+p.feedback);
        yield return Wait(2f);
        // --- Reset ---
        yield return Setup(BlobProperty.Magnet,BlobProperty.Pressure,new Vector3(5.25f,.1f,-1));
        Aim(cube.Body.worldCenterOfMass); yield return Wait(.2f); p.testAction=true; yield return Wait(1f);
        bool holding=pw.held!=null;
        w.Respawn(); Idle(); yield return Wait(.3f);
        CheckThat("Respawn clears both slots and drops the held body",holding && p.property==BlobProperty.None && p.secondary==BlobProperty.None && pw.held==null,"was holding="+holding);
        w.Restart(); yield return Wait(.3f);
        bool home=true; float worst=0;
        foreach(var m in BlobMagnetic.All) {
            float off=Vector3.Distance(m.transform.position,starts[m]); worst=Mathf.Max(worst,off);
            home&=off<.3f && m.Body.linearVelocity.magnitude<.5f;
        }
        CheckThat("Restart returns magnetic bodies to their start",home,"bodies="+starts.Count+" worstOffset="+worst.ToString("F2"));
        CheckThat("Rigidbodies stayed in the world during the test",maxBodySpeed<=20.5f && minBodyY>-10.5f,"maxSpeed="+maxBodySpeed.ToString("F1")+" minY="+minBodyY.ToString("F1"));
        Finish();
    }
    void Finish() {
        Idle(); p.testMode=false; p.testAim=null;
        foreach(var a in aims) if(a) Destroy(a);
        string path=Path.Combine(Application.dataPath,"OopsPlanet/Evidence/p03-power-test.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
        Debug.Log("OOPS_P03_DONE passed="+report.passed+" failed="+report.failed+" report="+path);
    }
}
}
