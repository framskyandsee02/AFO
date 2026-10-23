using UnityEngine;
namespace OopsPlanet {
// Canal surface BLOB can bounce on (R2 ⑤, Bounce + Water): the thin surface collider only exists while BLOB
// holds Bounce as primary with Water as secondary, so normally BLOB just falls in.
[RequireComponent(typeof(Collider))]
public class WaterSkim : MonoBehaviour {
    public BlobWorld world;
    public int skims;
    Collider col; bool wasOn;
    void Awake() { col=GetComponent<Collider>(); col.enabled=false; }
    void Update() {
        var p=world?world.player:null;
        bool on=p && p.property==BlobProperty.Bounce && p.secondary==BlobProperty.Water;
        if(on!=col.enabled) col.enabled=on;
        if(on && p.bounceAirborne && p.Grounded && !wasOn && Mathf.Abs(p.transform.position.y-col.bounds.max.y)<.4f && col.bounds.Contains(new Vector3(p.transform.position.x,col.bounds.center.y,p.transform.position.z))) skims++;
        wasOn=on && p.Grounded;
    }
}
}
