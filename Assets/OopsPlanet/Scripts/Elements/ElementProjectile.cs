using UnityEngine;
namespace OopsPlanet {
// Small element ball used by combos: water ball, fireball, wind ball, iron nail. Moves by sphere casts (no
// rigidbody), may bounce a few times, and delivers its element on every hit. A nail sticks into wood/straw and
// leaves a MagnetAnchor behind. Always dies after lifetime, so nothing can pile up.
public class ElementProjectile : MonoBehaviour {
    public static int alive, spawned;
    public BlobProperty element;
    public string comboId="";
    public Vector3 velocity;
    public float gravity, radius=.22f, lifetime=4f, bounceDamping=.8f;
    public int bounces;
    public bool wind, nail;
    public int hits;
    float dieAt; Color color;
    public static ElementProjectile Spawn(Vector3 pos,Vector3 velocity,BlobProperty element,string comboId,int bounces,float gravity,Color color,float size=.35f) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name="Element ball ("+(comboId==""?element.ToString():comboId)+")";
        Object.Destroy(go.GetComponent<Collider>()); go.layer=2;
        go.transform.position=pos; go.transform.localScale=Vector3.one*size;
        var r=go.GetComponent<Renderer>(); var mpb=new MaterialPropertyBlock(); mpb.SetColor("_BaseColor",color); r.SetPropertyBlock(mpb);
        r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var p=go.AddComponent<ElementProjectile>();
        p.velocity=velocity; p.element=element; p.comboId=comboId; p.bounces=bounces; p.gravity=gravity; p.color=color; p.radius=size*.6f;
        spawned++;
        return p;
    }
    void OnEnable() { alive++; dieAt=Time.time+lifetime; }
    void OnDisable() { alive--; }
    void Update() {
        if(Time.time>=dieAt) { Destroy(gameObject); return; }
        velocity+=Vector3.down*gravity*Time.deltaTime;
        Vector3 step=velocity*Time.deltaTime;
        if(Physics.SphereCast(transform.position,radius,step.normalized,out RaycastHit hit,step.magnitude,ElementHit.Mask,QueryTriggerInteraction.Ignore)) {
            hits++;
            transform.position=hit.point+hit.normal*(radius+.02f);
            ElementHit.Collider(hit.collider,element,comboId,hit.point);
            if(wind) {
                var rcv=hit.collider.GetComponentInParent<IPressureReceiver>(); if(rcv!=null) rcv.OnPressure(velocity.normalized,1);
                var rb=hit.collider.attachedRigidbody; if(rb && !rb.isKinematic && rb.mass<=12) rb.AddForce(velocity.normalized*6*Mathf.Max(1,rb.mass*.5f),ForceMode.Impulse);
            }
            VfxKit.Play(k=>k.Burst(hit.point,color,10,3,.1f,.35f,hit.normal,60));
            if(nail) {
                var m=hit.collider.GetComponentInParent<WorldMaterial>();
                if(m && (m.kind==MaterialKind.Wood||m.kind==MaterialKind.Straw)) MagnetAnchor.Create(hit.point+hit.normal*.1f,30f);
                Destroy(gameObject); return;
            }
            if(bounces<=0) { Destroy(gameObject); return; }
            bounces--; velocity=Vector3.Reflect(velocity,hit.normal)*bounceDamping;
            return;
        }
        transform.position+=step;
    }
}
}
