using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// Frame-time sampler at fixed viewpoints (Hub, Factory front/back, Rice Fields). Used to compare before/after
// the P06 art pass. Editor Play Mode numbers are indicative only (editor overhead, Game view size).
public class OopsPerfSample : MonoBehaviour {
    [System.Serializable] public class Sample { public string view; public float avgMs, p95Ms, maxMs; public int frames; }
    [System.Serializable] public class Report { public string utc, label; public int screenW, screenH; public List<Sample> samples=new List<Sample>(); }
    public string label="baseline";
    IEnumerator Start() {
        var w=GetComponent<BlobWorld>(); var p=w.player; p.testMode=true; p.paused=false;
        var report=new Report{utc=System.DateTime.UtcNow.ToString("o"),label=label,screenW=Screen.width,screenH=Screen.height};
        var views=new (string name,Vector3 pos,float yaw)[]{("Hub",new Vector3(0,.1f,-6),0),("Factory front",new Vector3(0,.1f,28),0),("Factory back",new Vector3(6,.1f,47),0),("Rice Fields",new Vector3(30,.1f,0),90)};
        foreach(var v in views) {
            p.Teleport(v.pos); p.yaw=v.yaw; p.testAim=null;
            float until=Time.realtimeSinceStartup+1; while(Time.realtimeSinceStartup<until) yield return null;
            var ms=new List<float>(); until=Time.realtimeSinceStartup+3;
            while(Time.realtimeSinceStartup<until) { ms.Add(Time.unscaledDeltaTime*1000); yield return null; }
            ms.Sort(); float sum=0; foreach(var m in ms) sum+=m;
            report.samples.Add(new Sample{view=v.name,frames=ms.Count,avgMs=sum/ms.Count,p95Ms=ms[Mathf.Min(ms.Count-1,(int)(ms.Count*.95f))],maxMs=ms[ms.Count-1]});
        }
        p.testMode=false;
        string path=TestReports.PathFor("perf-"+label+".json");
        File.WriteAllText(path,JsonUtility.ToJson(report,true));
        Debug.Log("OOPS_PERF_DONE "+label+" report="+path);
    }
}
}
