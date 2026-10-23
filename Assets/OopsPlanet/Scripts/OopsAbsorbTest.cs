using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// P02 absorption test in the Hub absorption yard: targeting feedback, range / line-of-sight / decoy rejection,
// the three AbsorbResult modes, the Absorbed event, and state recovery on respawn / restart.
public class OopsAbsorbTest : MonoBehaviour {
    [System.Serializable] public class Check { public string name;public bool passed;public string observed; }
    [System.Serializable] public class Report { public string utc;public List<Check> checks=new List<Check>();public int passed,failed; }
    BlobWorld w; BlobPlayer p; Report report=new Report();
    readonly List<string> events=new List<string>();
    readonly List<GameObject> aims=new List<GameObject>();
    void CheckThat(string name,bool pass,string observed) {
        report.checks.Add(new Check{name=name,passed=pass,observed=observed});
        if(pass)report.passed++;else report.failed++;
        Debug.Log("OOPS_P02 "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
    }
    IEnumerator Wait(float seconds) { float until=Time.time+seconds;while(Time.time<until)yield return null; }
    IEnumerator To(Vector3 goal,float timeout=8f) {
        p.testAim=null;p.testAbsorb=false;
        float until=Time.time+timeout;
        while(Time.time<until) {
            Vector3 d=goal-p.transform.position;d.y=0;
            if(d.magnitude<.22f)break;
            p.testMove=new Vector2(d.x,d.z).normalized;
            yield return null;
        }
        p.testMove=Vector2.zero;
    }
    Transform AimHelper(Vector3 point) {
        var go=new GameObject("P02 aim helper");go.transform.position=point;aims.Add(go);return go.transform;
    }
    IEnumerator AimAt(Vector3 point) {
        var t=AimHelper(point); p.testAim=t; p.AimAt(t); yield return Wait(.25f);
    }
    IEnumerator Hold(float seconds) { p.testAbsorb=true; yield return Wait(seconds); p.testAbsorb=false; yield return null; }
    BlobInteractable Find(string prefix) { foreach(var s in w.sources) if(s.name.StartsWith(prefix)) return s; return null; }
    IEnumerator Start() {
        w=GetComponent<BlobWorld>();p=w.player;p.testMode=true;p.paused=false;w.UseSaveSlot("test_run",false);w.Restart();p.testMode=true;
        report.utc=System.DateTime.UtcNow.ToString("o");
        p.Absorbed+=(s,prop)=>events.Add(s.name+"|"+prop);
        var scrap=Find("Absorbable Scrap (Magnet");var tank=Find("Absorbable Tank");var tire=Find("Absorbable Tire");var hidden=Find("Absorbable Scrap (occluded)");
        var decoy=GameObject.Find("Decoy Crate (not absorbable)");
        CheckThat("Yard sources found",scrap&&tank&&tire&&hidden&&decoy,"sources="+w.sources.Length);
        yield return Wait(.4f);
        Vector3 cam=p.view.transform.position;
        string status=p.EvaluateTarget(tire,cam,(tire.AimPoint-cam).normalized);
        CheckThat("Out of range rejected",status=="OUT OF RANGE",status);
        yield return To(new Vector3(-6,0,1));
        // Aiming at nothing absorbable.
        p.testAim=null;p.yaw=180;yield return Wait(.25f);
        yield return Hold(1f);
        CheckThat("Invalid aim cannot absorb",p.property==BlobProperty.None && p.target==null,"property="+p.property+" status="+p.targetStatus);
        // Non-absorbable decoy object.
        yield return AimAt(decoy.GetComponentInChildren<Renderer>().bounds.center);
        CheckThat("Decoy reports not absorbable",p.target==null && p.targetStatus==BlobPlayer.NotAbsorbable,"status="+p.targetStatus);
        yield return Hold(1f);
        CheckThat("Decoy cannot be absorbed",p.property==BlobProperty.None && events.Count==0,"property="+p.property);
        // Target feedback on a valid source.
        yield return AimAt(scrap.AimPoint);
        CheckThat("Valid target highlighted with marker",p.target==scrap && p.targetStatus==BlobPlayer.Absorbable && p.targetMarker.activeSelf && scrap.indicator.transform.localScale.x>1.1f,
            "target="+(p.target?p.target.name:"none")+" status="+p.targetStatus+" marker="+p.targetMarker.activeSelf);
        yield return Hold(.25f);yield return Wait(.15f);
        CheckThat("Early release cancels absorption",p.property==BlobProperty.None && p.absorbProgress==0,"property="+p.property+" progress="+p.absorbProgress);
        yield return Hold(1.1f);
        CheckThat("Hold absorbs Magnet from scrap",p.property==BlobProperty.Magnet,"property="+p.property);
        CheckThat("Absorbed event carries source + property",events.Count==1 && events[0]==scrap.name+"|Magnet",string.Join(",",events));
        CheckThat("Remains mode: scrap still absorbable",scrap.Available && scrap.gameObject.activeSelf,"available="+scrap.Available);
        // Movement while holding absorb.
        Vector3 before=p.transform.position;
        p.testAim=AimHelper(tank.AimPoint);p.testAbsorb=true;p.testMove=Vector2.left;yield return Wait(.35f);p.testMove=Vector2.zero;p.testAbsorb=false;
        CheckThat("Movement continues while holding absorb",Vector3.Distance(before,p.transform.position)>.8f,"moved="+Vector3.Distance(before,p.transform.position).ToString("F2"));
        yield return To(new Vector3(-6,0,1));
        // Remnant mode.
        yield return AimAt(tank.AimPoint);yield return Hold(1.1f);
        CheckThat("Absorb Pressure from tank",p.property==BlobProperty.Pressure,"property="+p.property);
        CheckThat("Remnant mode: tank swaps to empty remnant",tank.consumed && !tank.fullVisual.activeSelf && tank.remnantVisual.activeSelf,"consumed="+tank.consumed);
        yield return AimAt(tank.remnantVisual.GetComponentInChildren<Renderer>().bounds.center);yield return Hold(1f);
        CheckThat("Consumed remnant is not targetable",p.target!=tank && p.property==BlobProperty.Pressure && events.Count==2,"target="+(p.target?p.target.name:"none")+" events="+events.Count);
        // Disable mode.
        yield return AimAt(tire.AimPoint);yield return Hold(1.1f);
        CheckThat("Absorb Bounce from tire",p.property==BlobProperty.Bounce,"property="+p.property);
        CheckThat("Disable mode: tire removed",!tire.gameObject.activeSelf && tire.consumed,"active="+tire.gameObject.activeSelf);
        // P03 rule: at most two active properties (newest primary, previous primary secondary, oldest dropped).
        CheckThat("Max two properties: newest primary, oldest dropped",p.property==BlobProperty.Bounce && p.secondary==BlobProperty.Pressure && !p.Has(BlobProperty.Magnet) && events.Count==3,"slots="+p.property+"/"+p.secondary+" events="+events.Count);
        // Identity: power band takes the property colour, body keeps BLOB colour.
        var mpb=new MaterialPropertyBlock();Color band=Color.clear,body=Color.clear;
        foreach(var r in p.visual.GetComponentsInChildren<Renderer>()) {
            r.GetPropertyBlock(mpb);
            if(r.name.Contains("PowerBand")) band=mpb.GetColor("_BaseColor");
            if(r.name.Contains("Body")) body=mpb.GetColor("_BaseColor");
        }
        Color bounce=BlobPlayer.PropertyColor(BlobProperty.Bounce);
        CheckThat("Power band shows property, body stays BLOB",Vector4.Distance(band,bounce)<.05f && Vector4.Distance(body,bounce)>.3f,"band="+band+" body="+body);
        // Line of sight.
        yield return To(new Vector3(-7.5f,0,4));
        cam=p.view.transform.position;
        status=p.EvaluateTarget(hidden,cam,(hidden.AimPoint-cam).normalized);
        CheckThat("Line of sight through wall rejected",status=="BLOCKED",status);
        yield return AimAt(hidden.AimPoint);yield return Hold(1.1f);
        CheckThat("Occluded source cannot be absorbed",p.property==BlobProperty.Bounce && events.Count==3,"property="+p.property);
        // Recovery.
        w.Respawn();yield return Wait(.3f);
        CheckThat("Respawn clears property and restores sources",p.property==BlobProperty.None && tank.Available && tank.fullVisual.activeSelf && !tank.remnantVisual.activeSelf && tire.gameObject.activeSelf && tire.Available,
            "property="+p.property+" tank="+tank.Available+" tire="+tire.Available);
        CheckThat("No absorb state stuck after respawn",p.absorbProgress==0 && p.secondary==BlobProperty.None && !p.completed,"progress="+p.absorbProgress);
        yield return To(new Vector3(-6,0,1));
        yield return AimAt(tank.AimPoint);yield return Hold(1.1f);
        CheckThat("Restored tank absorbable again",p.property==BlobProperty.Pressure && tank.consumed,"property="+p.property);
        w.Restart();yield return Wait(.3f);
        bool allBack=true;foreach(var s in w.sources) allBack&=s.Available;
        CheckThat("Restart restores every source",allBack && p.property==BlobProperty.None && w.checkpointIndex==0,"all="+allBack+" property="+p.property);
        Finish();
    }
    void Finish() {
        p.testMove=Vector2.zero;p.testAbsorb=false;p.testMode=false;p.testAim=null;
        foreach(var a in aims) if(a) Destroy(a);
        string path=Path.Combine(Application.dataPath,"OopsPlanet/Evidence/p02-absorb-test.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
        Debug.Log("OOPS_P02_DONE passed="+report.passed+" failed="+report.failed+" report="+path);
    }
}
}
