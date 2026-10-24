using UnityEngine;
namespace OopsPlanet {
// Condition over several ElectricSwitches:
//  Sequence  - hit in their 'order' within window seconds (Electric + Bounce chain spark, or quick single sparks)
//  Together  - all hit within window seconds of each other (Bounce + Electric landing shock on 4 floor pads)
public class SwitchGroup : MonoBehaviour, IPuzzleCondition {
    public enum Mode { Sequence, Together }
    public Mode mode;
    public ElectricSwitch[] switches;
    public float window=3f;
    public bool met;
    public bool IsMet => met;
    public float Progress01 { get { if(met) return 1; int n=0; foreach(var s in switches) if(s && Time.time-s.lastHit<=window) n++; return switches.Length==0?0:(float)n/switches.Length; } }
    void Update() {
        if(met || switches==null || switches.Length==0) return;
        float first=float.MaxValue, last=float.MinValue;
        foreach(var s in switches) { if(!s) return; first=Mathf.Min(first,s.lastHit); last=Mathf.Max(last,s.lastHit); }
        if(first<0 || last-first>window || Time.time-last>window) { foreach(var s in switches) if(s) s.Light(Time.time-s.lastHit<=window); return; }
        if(mode==Mode.Sequence) {
            var sorted=(ElectricSwitch[])switches.Clone(); System.Array.Sort(sorted,(a,b)=>a.order.CompareTo(b.order));
            for(int i=1;i<sorted.Length;i++) if(sorted[i].lastHit<sorted[i-1].lastHit) return;
        }
        met=true; foreach(var s in switches) s.Light(true);
    }
    public void ResetCondition() { met=false; if(switches!=null) foreach(var s in switches) if(s) s.ResetSwitch(); }
}
}
