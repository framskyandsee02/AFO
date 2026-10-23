using UnityEngine;
namespace OopsPlanet {
// Pressure plate: met while resting bodies on it weigh at least minMass (a 6 kg counterweight dragged by Magnet,
// or a 30 kg old machine lifted with Electric + Magnet). The plate sinks a little and its lamp turns green.
public class WeightPlate : MonoBehaviour, IPuzzleCondition {
    public Vector3 size=new Vector3(2,1.2f,2);
    public float minMass=5f, maxSpeed=.8f;
    public Renderer lamp; public Transform plateVisual;
    public float massOnPlate;
    public bool IsMet { get; private set; }
    public float Progress01 => Mathf.Clamp01(massOnPlate/minMass);
    MaterialPropertyBlock mpb; Vector3 visualStart; bool hasStart;
    void Update() {
        massOnPlate=0;
        var seen=new System.Collections.Generic.HashSet<Rigidbody>();
        foreach(var c in Physics.OverlapBox(transform.position+Vector3.up*size.y/2,size/2,transform.rotation,ElementHit.Mask,QueryTriggerInteraction.Ignore)) {
            var rb=c.attachedRigidbody; if(!rb || rb.isKinematic || !seen.Add(rb)) continue;
            if(rb.linearVelocity.magnitude<=maxSpeed) massOnPlate+=rb.mass;
        }
        IsMet=massOnPlate>=minMass;
        if(plateVisual) { if(!hasStart) { visualStart=plateVisual.localPosition; hasStart=true; } plateVisual.localPosition=visualStart+Vector3.down*(IsMet?.08f:0); }
        if(lamp) { if(mpb==null) mpb=new MaterialPropertyBlock(); lamp.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",IsMet?new Color(.2f,1,.4f):massOnPlate>0?new Color(1,.7f,.1f):new Color(.9f,.15f,.1f)); lamp.SetPropertyBlock(mpb); }
    }
    public void ResetCondition() { IsMet=false; massOnPlate=0; }
}
}
