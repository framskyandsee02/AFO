using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// Real absorb-input and renderer/collider checks for the six newly modeled source classes.
public class OopsWorldArtSmokeTest : MonoBehaviour {
    [System.Serializable] public class Check {public string name;public bool passed;public string observed;}
    [System.Serializable] public class Report {public string utc;public int passed,failed;public List<Check> checks=new List<Check>();}
    Report report=new Report(); BlobWorld w;BlobPlayer p;GameObject aim;
    IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
    void CheckThat(string name,bool ok,string observed){report.checks.Add(new Check{name=name,passed=ok,observed=observed});if(ok)report.passed++;else report.failed++;Debug.Log("WORLD_ART "+(ok?"PASS ":"FAIL ")+name+" "+observed);}
    IEnumerator Start(){
        w=GetComponent<BlobWorld>();p=w.player;w.UseSaveSlot("test_world_art",false);w.Restart();p.testMode=true;p.paused=false;
        report.utc=System.DateTime.UtcNow.ToString("o");aim=new GameObject("World art smoke aim");p.testAim=aim.transform;
        foreach(var prop in ComboTable.Powers){
            p.testAbsorb=false;p.testAction=p.testSecondary=false;p.testMove=Vector2.zero;
            BlobInteractable source=null;foreach(var s in w.sources)if(s.property==prop && s.Available){source=s;break;}
            if(!source){CheckThat(prop+" source available",false,"none");continue;}
            Vector3 colliderScale=source.fullVisual.transform.localScale;
            bool located=false;
            foreach(var offset in new[]{Vector3.left,Vector3.back,Vector3.right,Vector3.forward}){
                Vector3 pos=source.transform.position+offset*2.3f+Vector3.up*.12f;
                if(Physics.CheckCapsule(pos+Vector3.up*.4f,pos+Vector3.up*.95f,.36f,ElementHit.Mask,QueryTriggerInteraction.Ignore))continue;
                p.Teleport(pos);aim.transform.position=source.AimPoint;p.AimAt(aim.transform);yield return Wait(.3f);
                if(p.target==source && p.targetStatus==BlobPlayer.Absorbable){located=true;break;}
            }
            CheckThat(prop+" target can be aimed",located,p.targetStatus);
            if(!located)continue;
            var art=source.transform.Find("World polish source");var fx=source.GetComponent<AbsorbableVisual>();
            Bounds artBounds=new Bounds();if(fx && fx.renderers.Length>0){artBounds=fx.renderers[0].bounds;foreach(var r in fx.renderers)artBounds.Encapsulate(r.bounds);}
            CheckThat(prop+" modeled visual and highlight keep collider scale",art && fx && fx.renderers.Length>1 && artBounds.size.y>.4f && source.fullVisual.transform.localScale==colliderScale,"renderers="+(fx?fx.renderers.Length:0)+" height="+artBounds.size.y);
            p.testAbsorb=true;yield return Wait(p.absorbSeconds+.25f);p.testAbsorb=false;yield return Wait(.15f);
            CheckThat(prop+" absorbs through actual E input",p.Has(prop) && source.consumed,"consumed="+source.consumed+" selected="+p.property);
            source.Restore();yield return Wait(.15f);
            CheckThat(prop+" restores modeled source",source.Available && fx.renderers.Length>0 && fx.renderers[0].enabled,"available="+source.Available);
        }
        bool linked=true;foreach(var part in w.ship.parts)if(part){if(!part.visual || part.visual.name!="World polish ship part")linked=false;var old=part.transform.Find("Part visual");if(old)foreach(var r in old.GetComponentsInChildren<Renderer>(true))if(r.enabled)linked=false;}
        CheckThat("Ship parts use modeled visual as animated pickup",linked,"parts="+w.ship.parts.Length);
        var pickup=w.ship.parts[0];pickup.gameObject.SetActive(true);w.ship.Collect(pickup);yield return Wait(.15f);
        CheckThat("Collected modeled ship part disappears",!pickup.visual.gameObject.activeSelf,"active="+pickup.visual.gameObject.activeSelf);
        w.ship.ResetShip();yield return Wait(.15f);
        CheckThat("Reset restores modeled ship part",pickup.visual.gameObject.activeSelf,"active="+pickup.visual.gameObject.activeSelf);
        p.testAbsorb=false;p.testAim=null;Destroy(aim);w.Restart();p.testMode=false;
        Directory.CreateDirectory("Assets/Scenes/OopsPlanet/Evidence");File.WriteAllText("Assets/Scenes/OopsPlanet/Evidence/world-art-smoke.json",JsonUtility.ToJson(report,true));
        string path=SaveSystem.PathFor("test_world_art");if(File.Exists(path))File.Delete(path);
        Debug.Log("WORLD_ART_DONE passed="+report.passed+" failed="+report.failed);
        Destroy(this);
    }
}
}
