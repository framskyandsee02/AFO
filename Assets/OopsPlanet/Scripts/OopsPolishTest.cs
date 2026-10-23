using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// P06 polish test: Thai UI/font, settings, feedback (sound cue counters, VFX bursts, BLOB moods), blink and
// squash, shape-based power cues, source labels, zone ambience switching, restoration colours, materials, licences.
// Audio is not audible in test mode; the test checks which cue would have played (counters) not the speaker.
public class OopsPolishTest : MonoBehaviour {
    [System.Serializable] public class Check { public string name;public bool passed;public string observed; }
    [System.Serializable] public class Report { public string utc;public List<Check> checks=new List<Check>();public int passed,failed; }
    float yaw0, pitch0;
    BlobWorld w; BlobPlayer p; BlobPowers pw; BlobFeedback fb; BlobExpression ex; Report report=new Report();
    readonly List<GameObject> aims=new List<GameObject>();
    void CheckThat(string name,bool pass,string observed) {
        report.checks.Add(new Check{name=name,passed=pass,observed=observed});
        if(pass)report.passed++;else report.failed++;
        Debug.Log("OOPS_P06 "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
    }
    IEnumerator Wait(float s) { float until=Time.time+s; while(Time.time<until) yield return null; }
    IEnumerator To(Vector3 goal,float timeout=8f) { p.testAim=null; float until=Time.time+timeout; while(Time.time<until) { Vector3 d=goal-p.transform.position; d.y=0; if(d.magnitude<.25f) break; p.testMove=new Vector2(d.x,d.z).normalized; yield return null; } p.testMove=Vector2.zero; }
    void Aim(Vector3 pt) { var g=new GameObject("P06 aim"); g.transform.position=pt; aims.Add(g); p.testAim=g.transform; p.AimAt(g.transform); }
    IEnumerator Pulse() { p.testAction=true; yield return null; yield return null; p.testAction=false; yield return null; }
    BlobInteractable Src(string prefix) { foreach(var s in w.sources) if(s.name.StartsWith(prefix)) return s; return null; }
    static bool Thai(string s) { foreach(char c in s) if(c>='฀'&&c<='๿') return true; return false; }
    bool CueOn(string part) { foreach(var r in p.visual.GetComponentsInChildren<Renderer>(true)) if(r.name.Contains(part)) return r.gameObject.activeSelf; return false; }
    IEnumerator Start() {
        w=GetComponent<BlobWorld>(); p=w.player; pw=p.powers; fb=p.GetComponent<BlobFeedback>(); ex=p.GetComponent<BlobExpression>();
        yaw0=p.yaw; pitch0=p.pitch;
        p.testMode=true; p.paused=false; w.UseSaveSlot("test_run",false); w.Restart(); p.testMode=true;
        report.utc=System.DateTime.UtcNow.ToString("o");
        yield return Wait(.5f);
        // --- Thai UI / font ---
        bool signsOk=true; int signs=0;
        foreach(var tm in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) { if(tm.name=="Absorb target marker") continue; signs++; signsOk&=tm.font==w.uiFont; }
        CheckThat("Bundled Noto Sans Thai font drives HUD and all world signs",w.uiFont && w.uiFont.name.Contains("NotoSansThai") && signsOk,"font="+(w.uiFont?w.uiFont.name:"none")+" signs="+signs);
        bool statusThai=true;
        foreach(var code in new[]{BlobPlayer.Absorbable,BlobPlayer.NotAbsorbable,"OUT OF RANGE","AIM AT TARGET","BLOCKED","NO ABSORBABLE PROPERTY"}) { p.targetStatus=code; statusThai&=Thai(w.StatusTh()); }
        CheckThat("Every absorb status, control hint and objective is shown in Thai",statusThai && Thai(BlobWorld.ControlsTh) && Thai(w.PuzzlePanelText()) && Thai(w.objective),"objective="+w.objective);
        // --- Settings (pause menu sliders call ApplySettings) ---
        w.ApplySettings(.2f,.7f,.5f); yield return null;
        SaveSystem.Load("test_run",out var sd);
        CheckThat("Volume / ambience / sensitivity settings apply and save",Mathf.Abs(AudioListener.volume-.7f)<.01f && Mathf.Abs(w.ambience.ambienceVolume-.5f)<.01f && Mathf.Abs(p.lookSensitivity-.2f)<.01f && Mathf.Abs(sd.ambienceVolume-.5f)<.01f,
            "vol="+AudioListener.volume+" amb="+w.ambience.ambienceVolume+" saved amb="+sd.ambienceVolume);
        w.ApplySettings(.13f,1,1);
        // --- Blink + idle ---
        yield return Wait(5.5f);
        CheckThat("BLOB blinks on its own",ex.blinks>=1,"blinks="+ex.blinks);
        // --- Absorb feedback ---
        var scrap=Src("Absorbable Scrap (Magnet"); var tank=Src("Absorbable Tank"); var hidden=Src("Absorbable Scrap (occluded)");
        int bursts=VfxKit.Instance.bursts;
        yield return To(new Vector3(-6,0,1)); Aim(scrap.AimPoint); yield return Wait(.3f);
        p.testAbsorb=true; yield return Wait(1.1f);
        var moodAfterAbsorb=ex.mood;
        p.testAbsorb=false; yield return null;
        CheckThat("Absorb: chirp cue, particle stream, happy face",fb.Count(Cue.Absorb)==1 && VfxKit.Instance.bursts>bursts && moodAfterAbsorb==BlobExpression.Mood.Happy,"cue="+fb.Count(Cue.Absorb)+" bursts="+(VfxKit.Instance.bursts-bursts)+" mood="+moodAfterAbsorb);
        CheckThat("Power shown by shape: Magnet cue part on, others off",CueOn("Cue_Magnet") && !CueOn("Cue_Pressure") && !CueOn("Cue_Bounce"),"magnet="+CueOn("Cue_Magnet"));
        // Refused (line of sight blocked).
        yield return To(new Vector3(-7.5f,0,4)); Aim(hidden.AimPoint); yield return Wait(.3f);
        p.testAbsorb=true; yield return Wait(.4f); var moodRefused=ex.mood; p.testAbsorb=false; yield return null;
        CheckThat("Refused absorb: 'hm' cue and sad face",fb.Count(Cue.Refused)>=1 && moodRefused==BlobExpression.Mood.Sad,"cue="+fb.Count(Cue.Refused)+" mood="+moodRefused);
        // Remnant hides the power label.
        yield return To(new Vector3(-6,0,1)); Aim(tank.AimPoint); yield return Wait(.3f); p.testAbsorb=true; yield return Wait(1.1f); p.testAbsorb=false; yield return null;
        // v2: labels are hidden until sense mode, then show the Thai name; a used-up source never shows one.
        bool hiddenNormally=scrap.powerLabel && !scrap.powerLabel.activeSelf;
        p.testSense=true; yield return Wait(.3f);
        CheckThat("Source labels: hidden normally, Thai power name in sense mode, none once the tank is used up",hiddenNormally && scrap.powerLabel.activeSelf && Thai(scrap.powerLabel.GetComponent<TextMesh>().text) && tank.powerLabel && !tank.powerLabel.activeSelf,
            "scrap="+(scrap.powerLabel?scrap.powerLabel.GetComponent<TextMesh>().text:"none")+" hiddenNormally="+hiddenNormally);
        p.testSense=false; yield return Wait(.3f);
        CheckThat("Two powers -> both shape cues visible (Pressure + Magnet)",CueOn("Cue_Pressure") && CueOn("Cue_Magnet") && !CueOn("Cue_Bounce"),"pressure="+CueOn("Cue_Pressure"));
        // Swap.
        int swap=fb.Count(Cue.Swap); bursts=VfxKit.Instance.bursts; p.SwapProperties(); yield return null;
        CheckThat("Swap: cue + ring pulse",fb.Count(Cue.Swap)==swap+1 && VfxKit.Instance.bursts>bursts,"cue="+fb.Count(Cue.Swap));
        // Pressure blast.
        p.SetProperties(BlobProperty.Pressure,BlobProperty.Bounce); Aim(new Vector3(-6,1,8)); yield return Wait(.2f);
        bursts=VfxKit.Instance.bursts; yield return Pulse(); yield return null;
        CheckThat("Pressure blast: whoosh cue + puff particles",fb.Count(Cue.PressureWhoosh)>=1 && VfxKit.Instance.bursts>bursts,"cue="+fb.Count(Cue.PressureWhoosh));
        // Bounce launch + landing squash.
        p.testAim=null; float maxSquash=0,minSquash=0;
        p.testJump=true; yield return Wait(.8f); p.testJump=false;
        float until=Time.time+3; while(Time.time<until) { maxSquash=Mathf.Max(maxSquash,ex.squash); minSquash=Mathf.Min(minSquash,ex.squash); if(p.Grounded && !p.bounceAirborne && Time.time>until-2.2f) break; yield return null; }
        yield return Wait(.3f);
        CheckThat("Bounce: 'wheee' + landing 'pof' cues, stretch then squash",fb.Count(Cue.BounceLaunch)>=1 && fb.Count(Cue.Land)>=1 && minSquash<-.3f && maxSquash>.2f,
            "launch="+fb.Count(Cue.BounceLaunch)+" land="+fb.Count(Cue.Land)+" squash="+minSquash.ToString("F2")+".."+maxSquash.ToString("F2"));
        // Magnet hold: buzz loop + sparks.
        p.SetProperties(BlobProperty.Magnet,BlobProperty.None); p.Teleport(new Vector3(5.25f,.1f,-1)); yield return Wait(.3f);
        BlobMagnetic cube=null; foreach(var m in BlobMagnetic.All) if(m.name.StartsWith("Magnetic cube")) { cube=m; break; }
        Aim(cube.Body.worldCenterOfMass); yield return Wait(.2f); bursts=VfxKit.Instance.bursts;
        p.testAction=true; yield return Wait(1.2f);
        CheckThat("Magnet: buzz loop fades in + sparks along the beam",pw.held && fb.magnetLoop.volume>.1f && VfxKit.Instance.bursts>bursts+5,"vol="+fb.magnetLoop.volume.ToString("F2")+" sparks="+(VfxKit.Instance.bursts-bursts));
        p.testAction=false; yield return Wait(.5f);
        // Ambience by zone, restoration switch.
        p.Teleport(new Vector3(0,.1f,-2)); yield return Wait(1.6f);
        int hubAmb=w.ambience.Audible(0);
        p.Teleport(new Vector3(0,.1f,30)); yield return Wait(1.6f);
        int facAmb=w.ambience.Audible(1), hubAfter=w.ambience.Audible(0);
        int happy=fb.Count(Cue.Happy), unlock=fb.Count(Cue.Unlock); bursts=VfxKit.Instance.bursts;
        w.FindPuzzle("factory_gate").Solve(); w.FindPuzzle("factory_turbine").Solve(); yield return Wait(1.8f);
        int facRestored=w.ambience.Audible(1);
        CheckThat("Ambience cross-fades per zone and brightens after restoration",hubAmb==1 && facAmb==1 && hubAfter==0 && facRestored==2,"hub="+hubAmb+" factory="+facAmb+" hubAfterLeaving="+hubAfter+" factoryRestored="+facRestored);
        CheckThat("Puzzle solved / zone unlocked: happy trill, fanfare, celebration particles",fb.Count(Cue.Happy)==happy+2 && fb.Count(Cue.Unlock)==unlock+1 && VfxKit.Instance.bursts>=bursts+5,
            "happy="+(fb.Count(Cue.Happy)-happy)+" unlock="+(fb.Count(Cue.Unlock)-unlock)+" bursts="+(VfxKit.Instance.bursts-bursts));
        // Rice plants turn green when restored.
        var rice=w.FindPuzzle("rice_straw"); rice.Solve(); yield return Wait(3f);
        Renderer plant=null; foreach(var r in Object.FindObjectsByType<PuzzleReaction>(FindObjectsSortMode.None)) if(r.name=="Rice plants restoration") plant=r.recolor[0];
        var mpb=new MaterialPropertyBlock(); plant.GetPropertyBlock(mpb); Color pc=mpb.GetColor("_BaseColor");
        CheckThat("Rice plants grow back green (before: dry yellow)",pc.g>.6f && pc.r<.5f,"color="+pc);
        // Respawn mood/cue.
        int resp=fb.Count(Cue.Respawn); w.Respawn(); yield return null;
        CheckThat("Respawn: 'aww' cue and sad face",fb.Count(Cue.Respawn)==resp+1 && ex.mood==BlobExpression.Mood.Sad,"cue="+(fb.Count(Cue.Respawn)-resp));
        // Materials and licences.
        int bad=0; string badName="";
        foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) foreach(var m in r.sharedMaterials) if(!m || m.shader.name.Contains("InternalErrorShader")) { bad++; badName=r.name; }
        CheckThat("No missing / error (pink) materials",bad==0,"bad="+bad+" "+badName);
        string lic=Path.Combine(Application.dataPath,"OopsPlanet/ASSET_LICENSES.md");
        CheckThat("Asset licence list present (font OFL + self-made art/audio)",File.Exists(lic) && File.ReadAllText(lic).Contains("SIL Open Font License") && File.Exists(Path.Combine(Application.dataPath,"OopsPlanet/Fonts/OFL.txt")),"path="+lic);
        Finish();
    }
    void Finish() {
        p.yaw=yaw0; p.pitch=pitch0; // leave the camera as found: tests run back to back in one Play session
        p.testMove=Vector2.zero; p.testAction=false; p.testAbsorb=false; p.testJump=false; p.testMode=false; p.testAim=null;
        foreach(var a in aims) if(a) Destroy(a);
        w.Restart();
        string path=TestReports.PathFor("p06-polish-test.json");
        File.WriteAllText(path,JsonUtility.ToJson(report,true));
        Debug.Log("OOPS_P06_DONE passed="+report.passed+" failed="+report.failed+" report="+path);
    }
}
}
