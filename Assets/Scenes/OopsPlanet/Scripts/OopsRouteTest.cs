using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// P01 route test: drives BLOB with the real CharacterController from Hub -> Factory -> Hub -> Rice Fields -> demo end.
public class OopsRouteTest : MonoBehaviour {
    [System.Serializable] public class Check { public string name;public bool passed;public string observed; }
    [System.Serializable] public class Report { public string utc;public List<Check> checks=new List<Check>();public int passed,failed;public bool completedRoute;public List<string> diagnostics=new List<string>(); }
    BlobWorld w; BlobPlayer p; Report report=new Report();
    void CheckThat(string name,bool pass,string observed) {
        report.checks.Add(new Check{name=name,passed=pass,observed=observed});
        if(pass)report.passed++;else report.failed++;
        Debug.Log("OOPS_TEST "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
    }
    // Dock-jump diagnostics: logs take-off / landing (time, fps, position, jump input) while sampling.
    bool sampling, wasGrounded; float sampleStart, maxY; int frames;
    void Diag(string line) { report.diagnostics.Add(line); Debug.Log("OOPS_DIAG "+line); }
    void Update() {
        if(!sampling || !p) return;
        frames++; maxY=Mathf.Max(maxY,p.transform.position.y);
        bool g=p.Grounded;
        if(g!=wasGrounded) Diag((g?"LAND":"TAKEOFF")+" t="+(Time.time-sampleStart).ToString("F3")+" fps="+(1f/Mathf.Max(Time.deltaTime,1e-5f)).ToString("F0")
            +" pos="+p.transform.position.ToString("F2")+" vy="+p.velocity.y.ToString("F2")+" jumpHeld="+p.testJump);
        wasGrounded=g;
    }
    void StartSampling(string label) { sampling=true; sampleStart=Time.time; frames=0; maxY=p.transform.position.y; wasGrounded=p.Grounded;
        Diag("BEGIN "+label+" pos="+p.transform.position.ToString("F2")+" grounded="+p.Grounded+" vSync="+QualitySettings.vSyncCount+" targetFps="+Application.targetFrameRate); }
    void StopSampling(string label) { float dt=Time.time-sampleStart; sampling=false;
        Diag("END "+label+" pos="+p.transform.position.ToString("F2")+" maxY="+maxY.ToString("F2")+" frames="+frames+" avgFps="+(dt>0?frames/dt:0).ToString("F0")); }
    IEnumerator Wait(float seconds) { float until=Time.time+seconds;while(Time.time<until)yield return null; }
    IEnumerator To(Vector3 goal,bool jump=false,float timeout=10f) {
        p.testJump=jump;
        float until=Time.time+timeout;
        while(Time.time<until && !w.finished) {
            Vector3 d=goal-p.transform.position;d.y=0;
            if(d.magnitude<.25f)break;
            p.testMove=new Vector2(d.x,d.z).normalized;
            yield return null;
        }
        p.testMove=Vector2.zero;p.testJump=false;
        float dist=Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(goal.x,goal.z));
        CheckThat("Traverse to "+goal.ToString("F1"),dist<.5f || w.finished,"actual="+p.transform.position.ToString("F2"));
    }
    IEnumerator Start() {
        w=GetComponent<BlobWorld>();p=w.player;p.testMode=true;p.paused=false;w.UseSaveSlot("test_run",false);w.Restart();p.testMode=true;
        report.utc=System.DateTime.UtcNow.ToString("o");
        yield return Wait(.5f);
        CheckThat("Spawn is grounded in Hub",p.Grounded && p.transform.position.magnitude<1,"pos="+p.transform.position.ToString("F2")+" grounded="+p.Grounded);
        CheckThat("Hub objective shown",w.objective.StartsWith("HUB"),w.objective);
        Vector3 camBefore=p.view.transform.position;
        yield return To(new Vector3(0,0,10));
        CheckThat("Third-person camera follows player",Vector3.Distance(camBefore,p.view.transform.position)>5 && p.view.transform.position.z<p.transform.position.z,"cam="+p.view.transform.position.ToString("F1"));
        yield return To(new Vector3(0,0,22));
        yield return To(new Vector3(0,0,28));
        yield return Wait(.2f);
        CheckThat("Factory checkpoint reached",w.checkpointIndex==1,"checkpoint="+w.checkpointIndex);
        CheckThat("Factory objective shown",w.objective.Contains("FACTORY"),w.objective);
        yield return To(new Vector3(8,0,32));
        yield return To(new Vector3(8,0,33.5f));
        StartSampling("dock jump");
        // Jump while walking at the dock; if BLOB lands short of it, press jump again (release in the air, press on
        // the ground) so the result does not depend on frame rate.
        float jumpUntil=Time.time+4;
        while(Time.time<jumpUntil && p.transform.position.y<.9f) {
            Vector3 d=new Vector3(8,0,38)-p.transform.position; d.y=0;
            p.testMove=new Vector2(d.x,d.z).normalized;
            bool press=p.Grounded;
            if(press && !p.testJump) Diag("JUMP PRESS t="+(Time.time-sampleStart).ToString("F3")+" pos="+p.transform.position.ToString("F2"));
            p.testJump=press;
            yield return null;
        }
        p.testJump=false;
        yield return To(new Vector3(8,0,38),false,4);
        yield return Wait(.6f);
        StopSampling("dock jump");
        CheckThat("Jump onto 1 m loading dock",p.transform.position.y>.9f,"y="+p.transform.position.y.ToString("F2"));
        yield return To(new Vector3(8,0,32));
        yield return To(new Vector3(0,0,30));
        yield return To(new Vector3(0,0,18));
        yield return To(new Vector3(0,0,0));
        // P05 gating: Rice Fields open after the Factory is restored. The route test unlocks it through the
        // puzzle API (the real puzzle solutions are covered by the P04 test).
        foreach(var id in new[]{"factory_gate","factory_turbine"}) w.FindPuzzle(id).Solve();
        yield return Wait(2f);
        CheckThat("Rice gate open after Factory restored",w.zoneGates[0].Unlocked && w.zoneGates[0].Open01>=1,"open="+w.zoneGates[0].Open01.ToString("F2"));
        yield return To(new Vector3(12,0,0));
        yield return To(new Vector3(22,0,0));
        yield return To(new Vector3(28,0,0));
        yield return Wait(.2f);
        CheckThat("Rice Fields checkpoint reached",w.checkpointIndex==2,"checkpoint="+w.checkpointIndex);
        CheckThat("Rice objective shown",w.objective.Contains("RICE"),w.objective);
        yield return To(new Vector3(42,0,0));
        yield return To(new Vector3(57,0,0));
        CheckThat("Demo end completes run",w.finished,"finished="+w.finished+" time="+w.elapsed.ToString("F1"));
        CheckThat("No falls / respawns during route",w.deaths==0,"deaths="+w.deaths);
        report.completedRoute=w.finished;
        w.Restart();yield return Wait(.3f);
        CheckThat("Restart returns to Hub and clears run",!w.finished && w.checkpointIndex==0 && p.transform.position.magnitude<1,"pos="+p.transform.position.ToString("F2"));
        Finish();
    }
    void Finish() {
        p.testMove=Vector2.zero;p.testJump=false;p.testMode=false;
        string path=TestReports.PathFor("p01-route-test.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
        Debug.Log("OOPS_ROUTETEST_DONE passed="+report.passed+" failed="+report.failed+" completed="+report.completedRoute+" report="+path);
    }
}
}
