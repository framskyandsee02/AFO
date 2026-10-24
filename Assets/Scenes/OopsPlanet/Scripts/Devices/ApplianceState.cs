using UnityEngine;
namespace OopsPlanet {
// Kirby-style disguised source (owner brief 2026-10-24): an ordinary factory appliance of the source's element.
// FULL look while the power can be absorbed, DRAINED look (deflated extinguisher, dark panel, cold stove...) while
// it recovers. The swap pops with a small squash; in the last seconds before it recovers the drained model wobbles
// so the player can see it coming back. Shimmer / sense highlight stay on AbsorbableVisual.
public class ApplianceState : MonoBehaviour {
    public BlobInteractable source;
    public GameObject fullArt, drainedArt;
    public float popSeconds=.3f, wobbleBefore=3f;
    public bool drained;                 // read by tests
    Vector3 fullScale, drainedScale; float popAt=-9;
    void Awake() {
        if(!source) source=GetComponent<BlobInteractable>();
        if(fullArt) fullScale=fullArt.transform.localScale;
        if(drainedArt) drainedScale=drainedArt.transform.localScale;
        Apply(source && !source.Available,false);
    }
    void Apply(bool d,bool pop) {
        drained=d;
        if(fullArt) fullArt.SetActive(!d);
        if(drainedArt) drainedArt.SetActive(d);
        if(pop) popAt=Time.time;
    }
    void Update() {
        if(!source) return;
        bool d=!source.Available;
        if(d!=drained) Apply(d,true);
        var art=drained?drainedArt:fullArt; if(!art) return;
        Vector3 baseScale=drained?drainedScale:fullScale;
        float k=1;
        float t=(Time.time-popAt)/popSeconds;
        if(t<1) k=1+Mathf.Sin(t*Mathf.PI)*.18f;                                        // squash pop on swap
        else if(drained && source.regenSeconds>0 && source.regenAt-Time.time<wobbleBefore)
            k=1+Mathf.Sin(Time.time*18f)*.035f;                                         // about to recover
        art.transform.localScale=new Vector3(baseScale.x*(2-k),baseScale.y*k,baseScale.z*(2-k));
    }
}
}
