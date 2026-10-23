using UnityEngine;
using System.Collections;
using System.IO;
namespace OopsPlanet {
// Where automated test reports go: Assets/OopsPlanet/Evidence in the Editor, <persistentDataPath>/test-reports in a build.
public static class TestReports {
    public static string Dir => Application.isEditor?Path.Combine(Application.dataPath,"OopsPlanet/Evidence"):Path.Combine(Application.persistentDataPath,"test-reports");
    public static string PathFor(string file) { Directory.CreateDirectory(Dir); return Path.Combine(Dir,file); }
}
// Runs the automated play tests inside a built player (no Unity Editor), then quits.
// Usage: OopsPlanet.exe -oopsAutoTest route,puzzle,power,absorb,screens
// Tests always use their own save slots, so a player's save is never touched.
public class OopsBuildTestRunner : MonoBehaviour {
    public string[] steps;
    public static bool Requested(out string[] steps) {
        steps=null; var args=System.Environment.GetCommandLineArgs();
        for(int i=0;i<args.Length-1;i++) if(args[i]=="-oopsAutoTest") { steps=args[i+1].Split(','); return true; }
        return false;
    }
    IEnumerator Start() {
        var w=GetComponent<BlobWorld>();
        Log("start steps="+string.Join(",",steps)+" graphics="+SystemInfo.graphicsDeviceType+" screen="+Screen.width+"x"+Screen.height);
        yield return null; yield return null;
        foreach(var step in steps) {
            switch(step) {
                case "screens": yield return Screens(w); break;
                case "route": yield return Run<OopsRouteTest>(w,"p01-route-test.json",120); break;
                case "absorb": yield return Run<OopsAbsorbTest>(w,"p02-absorb-test.json",120); break;
                case "power": yield return Run<OopsPowerTest>(w,"p03-power-test.json",150); break;
                case "puzzle": yield return Run<OopsPuzzleTest>(w,"p04-puzzle-test.json",240); break;
                case "perf": yield return RunPerf(w); break;
                case "diag": yield return Diag(w,""); break;
                // Same measurement with the frame cap removed, to prove grounding does not depend on the cap.
                case "diaguncapped": {
                    int vs=QualitySettings.vSyncCount, fr=Application.targetFrameRate;
                    QualitySettings.vSyncCount=0; Application.targetFrameRate=-1;
                    yield return Diag(w,"-uncapped");
                    QualitySettings.vSyncCount=vs; Application.targetFrameRate=fr;
                    break;
                }
                default: Log("unknown step "+step); break;
            }
        }
        Log("done");
        Application.Quit();
    }
    IEnumerator Run<T>(BlobWorld w,string report,float timeout) where T:MonoBehaviour {
        string path=TestReports.PathFor(report); if(File.Exists(path)) File.Delete(path);
        Log("run "+typeof(T).Name);
        var t=w.gameObject.AddComponent<T>();
        float until=Time.realtimeSinceStartup+timeout;
        while(!File.Exists(path) && Time.realtimeSinceStartup<until) yield return null;
        Log(typeof(T).Name+(File.Exists(path)?" report written: "+path:" TIMEOUT"));
        Destroy(t); yield return null;
    }
    IEnumerator RunPerf(BlobWorld w) {
        string path=TestReports.PathFor("perf-build.json"); if(File.Exists(path)) File.Delete(path);
        w.UseSaveSlot("test_run",false);
        w.gameObject.AddComponent<OopsPerfSample>().label="build";
        float until=Time.realtimeSinceStartup+60; while(!File.Exists(path) && Time.realtimeSinceStartup<until) yield return null;
        Log("perf "+(File.Exists(path)?"written":"TIMEOUT"));
    }
    // Diagnostic (P07): frame rate vs CharacterController grounding while idle and while charging a Bounce.
    [System.Serializable] public class DiagReport {
        public string mode, graphics; public bool batchMode; public int vSyncCount, targetFrameRate;
        public float idleFps, idleDtMinMs, idleDtAvgMs, idleGroundedRatio;
        public float chargeGroundedRatio, maxBounceCharge, bouncePeakY;
        public int idleFrames, chargeFrames;
    }
    IEnumerator Diag(BlobWorld w,string suffix) {
        w.UseSaveSlot("test_run",false); w.Restart();
        var p=w.player; p.testMode=true; p.Teleport(new Vector3(0,.1f,-6)); p.testAim=null;
        float until=Time.realtimeSinceStartup+1; while(Time.realtimeSinceStartup<until) yield return null;
        var r=new DiagReport{graphics=SystemInfo.graphicsDeviceType.ToString(),batchMode=Application.isBatchMode,vSyncCount=QualitySettings.vSyncCount,targetFrameRate=Application.targetFrameRate};
        r.mode=(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null?"headless":"windowed")+suffix;
        int frames=0,grounded=0; float sum=0,min=999; until=Time.realtimeSinceStartup+3;
        while(Time.realtimeSinceStartup<until) { float dt=Time.unscaledDeltaTime; frames++; sum+=dt; min=Mathf.Min(min,dt); if(p.Grounded) grounded++; yield return null; }
        r.idleFrames=frames; r.idleFps=frames/sum; r.idleDtAvgMs=sum/frames*1000; r.idleDtMinMs=min*1000; r.idleGroundedRatio=grounded/(float)frames;
        p.SetProperties(BlobProperty.Bounce,BlobProperty.None); yield return null;
        frames=0; grounded=0; float maxCharge=0; p.testJump=true; until=Time.realtimeSinceStartup+1;
        while(Time.realtimeSinceStartup<until) { frames++; if(p.Grounded) grounded++; maxCharge=Mathf.Max(maxCharge,p.bounceCharge); yield return null; }
        p.testJump=false; float peak=0; until=Time.realtimeSinceStartup+2.5f;
        while(Time.realtimeSinceStartup<until) { peak=Mathf.Max(peak,p.transform.position.y); yield return null; }
        r.chargeFrames=frames; r.chargeGroundedRatio=grounded/(float)frames; r.maxBounceCharge=maxCharge; r.bouncePeakY=peak;
        p.testMode=false;
        File.WriteAllText(TestReports.PathFor("diag-"+r.mode+".json"),JsonUtility.ToJson(r,true));
        Log("diag "+r.mode+" fps="+r.idleFps.ToString("F0")+" grounded="+r.idleGroundedRatio.ToString("F2")+" charge="+maxCharge.ToString("F2")+" peak="+peak.ToString("F2"));
    }
    IEnumerator Screens(BlobWorld w) {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) { Log("screens skipped (no graphics)"); yield break; }
        w.UseSaveSlot("test_run",false); w.Restart();
        var p=w.player; p.testMode=true;
        var shots=new (string name,Vector3 pos,float yaw)[]{("build-hub",new Vector3(0,.1f,-6),0),("build-factory",new Vector3(0,.1f,27),0),("build-rice",new Vector3(29,.1f,0),90)};
        foreach(var s in shots) {
            p.Teleport(s.pos); p.yaw=s.yaw; p.testAim=null;
            float until=Time.realtimeSinceStartup+1.5f; while(Time.realtimeSinceStartup<until) yield return null;
            yield return new WaitForEndOfFrame();
            var tex=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(TestReports.PathFor(s.name+".png"),tex.EncodeToPNG()); Destroy(tex);
            Log("screenshot "+s.name);
        }
        p.testMode=false;
    }
    static void Log(string m) { Debug.Log("OOPS_BUILD_TEST "+m); File.AppendAllText(TestReports.PathFor("build-test-log.txt"),System.DateTime.Now.ToString("HH:mm:ss")+" "+m+"\n"); }
}
}
