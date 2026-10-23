using UnityEngine;
namespace OopsPlanet {
// An element carried by an object (Magnet + Water / Electric / Heat): the held object gets wet, charged or red-hot
// and passes that element to whatever it touches until the charge runs out.
public class ElementCarrier : MonoBehaviour {
    public BlobProperty element;
    public string comboId="";
    public float until, tick=.35f;
    public int deliveries;
    float next; Collider[] own;
    public bool Active => Time.time<until;
    public static ElementCarrier Charge(GameObject target,BlobProperty element,string comboId,float seconds) {
        var c=target.GetComponent<ElementCarrier>(); if(!c) c=target.AddComponent<ElementCarrier>();
        if(c.element!=element) c.deliveries=0;
        c.element=element; c.comboId=comboId; c.until=Mathf.Max(c.until,Time.time+seconds);
        return c;
    }
    void Awake() { own=GetComponentsInChildren<Collider>(); }
    void Update() {
        if(!Active) return;
        if(Time.time<next) return; next=Time.time+tick;
        Bounds b=new Bounds(transform.position,Vector3.zero); bool first=true;
        foreach(var c in own) if(c && c.enabled) { if(first){ b=c.bounds; first=false; } else b.Encapsulate(c.bounds); }
        if(first) return;
        var self=GetComponentInParent<WorldMaterial>();
        if(self && element==BlobProperty.Heat && self.kind==MaterialKind.Metal) self.Heat(1f);
        var touching=new System.Collections.Generic.List<Collider>();
        foreach(var c in Physics.OverlapBox(b.center,b.extents+Vector3.one*.25f,Quaternion.identity,ElementHit.Mask,QueryTriggerInteraction.Collide))
            if(!c.transform.IsChildOf(transform)) touching.Add(c);
        if(touching.Count>0 && ElementHit.Colliders(touching,element,comboId,b.center)>0) deliveries++;
        VfxKit.Play(k=>k.Burst(b.center+Random.insideUnitSphere*b.extents.magnitude*.5f,BlobPlayer.PropertyColor(element),2,1,.07f,.3f));
    }
    public void Clear() { until=0; }
}
}
