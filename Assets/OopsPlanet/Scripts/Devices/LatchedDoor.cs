using UnityEngine;
namespace OopsPlanet {
// A hatch held shut by a latch (wax seal / rusty bolt). Once the latch is gone (melted by Heat, or knocked off by
// a Magnet Cannon hit through ImpactBreak) a Pressure blast swings the hatch open. Met while open.
public class LatchedDoor : MonoBehaviour, IPuzzleCondition, IPressureReceiver {
    public WorldMaterial latch;
    public Transform hinge; public float openAngle=-100f, openSeconds=.6f;
    public Collider blocker;
    public bool open;
    public int pushesWhileLatched;
    public bool IsMet => open;
    public float Progress01 => open?1:LatchGone?.5f:0;
    public bool LatchGone => !latch || latch.consumed;
    float t; Quaternion closed; bool hasClosed;
    void Awake() { if(hinge) { closed=hinge.localRotation; hasClosed=true; } }
    public void OnPressure(Vector3 direction,float strength) {
        if(open) return;
        if(!LatchGone) { pushesWhileLatched++; return; }
        Open();
    }
    public void Open() { if(open) return; open=true; if(blocker) blocker.enabled=false; }
    void Update() {
        if(!hinge || !hasClosed) return;
        t=Mathf.MoveTowards(t,open?1:0,Time.deltaTime/openSeconds);
        hinge.localRotation=closed*Quaternion.Euler(0,openAngle*Mathf.SmoothStep(0,1,t),0);
    }
    public void ResetCondition() { open=false; t=0; if(blocker) blocker.enabled=true; if(hinge && hasClosed) hinge.localRotation=closed; if(latch) latch.ResetState(); }
}
}
