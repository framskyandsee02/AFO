using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
// Devices that react to an element hit (switches, pads, weld points, steam inlets...). comboId is the ordered
// combo cell that produced the hit ("" for a plain power), so a device can ask for a specific combination.
public interface IElementReceiver { void OnElement(BlobProperty element,string comboId,Vector3 point); }
// One place that delivers an element to the world: the material reaction table (ElementReaction) plus every
// IElementReceiver on the touched objects. Each material / receiver is hit at most once per call.
public static class ElementHit {
    public const int Mask=~(1<<2);                       // everything except BLOB (Ignore Raycast layer)
    public static event System.Action<BlobProperty,string,Vector3> Delivered;
    static readonly HashSet<Object> seen=new HashSet<Object>();
    static readonly List<IElementReceiver> receivers=new List<IElementReceiver>();
    public static int Colliders(IEnumerable<Collider> cols,BlobProperty e,string comboId,Vector3 point) {
        seen.Clear(); int n=0;
        foreach(var c in cols) n+=Apply(c,e,comboId,point,seen);
        if(n>0) Delivered?.Invoke(e,comboId,point);
        return n;
    }
    public static int Collider(Collider c,BlobProperty e,string comboId,Vector3 point) => Colliders(new[]{c},e,comboId,point);
    static int Apply(Collider c,BlobProperty e,string comboId,Vector3 point,HashSet<Object> done) {
        if(!c) return 0; int n=0;
        var m=c.GetComponentInParent<WorldMaterial>();
        if(m && done.Add(m)) { ElementReaction.Apply(e,m); n++; }
        receivers.Clear(); c.GetComponentsInParent(false,receivers);
        foreach(var r in receivers) { var mb=r as MonoBehaviour; if(mb && done.Add(mb)) { r.OnElement(e,comboId,point); n++; } }
        return n;
    }
    public static int Sphere(Vector3 center,float radius,BlobProperty e,string comboId) =>
        Colliders(Physics.OverlapSphere(center,radius,Mask,QueryTriggerInteraction.Collide),e,comboId,center);
    public static int Capsule(Vector3 a,Vector3 b,float radius,BlobProperty e,string comboId) =>
        Colliders(Physics.OverlapCapsule(a,b,radius,Mask,QueryTriggerInteraction.Collide),e,comboId,b);
    // Everything whose closest point lies inside a cone (origin, direction, range, half-angle in degrees), plus the
    // surface the centre ray lands on: something BLOB stands on or leans over has its closest point outside the cone
    // even though the stream visibly hits it.
    public static int Cone(Vector3 origin,Vector3 dir,float range,float angle,BlobProperty e,string comboId) {
        var hits=new List<Collider>();
        foreach(var c in Physics.OverlapSphere(origin,range,Mask,QueryTriggerInteraction.Collide)) {
            Vector3 to=c.ClosestPoint(origin)-origin;
            if(to.magnitude<.05f || Vector3.Angle(dir,to)<=angle) hits.Add(c);
        }
        var along=Physics.RaycastAll(origin,dir,range,Mask,QueryTriggerInteraction.Collide);
        System.Array.Sort(along,(x,y)=>x.distance.CompareTo(y.distance));
        foreach(var h in along) {
            if(IsTarget(h.collider) && !hits.Contains(h.collider)) hits.Add(h.collider);
            if(!h.collider.isTrigger) break;               // the first solid surface ends the stream
        }
        return Colliders(hits,e,comboId,origin+dir*range);
    }
    // First solid surface along a ray (BLOB ignored, triggers ignored); returns the ray end when nothing is hit.
    public static Vector3 RayEnd(Vector3 origin,Vector3 dir,float range,out RaycastHit hit,out bool didHit) {
        didHit=Physics.Raycast(origin,dir,out hit,range,Mask,QueryTriggerInteraction.Ignore);
        return didHit?hit.point:origin+dir*range;
    }
    // Converge the emission ray on the camera reticle instead of firing a parallel ray.
    public static Vector3 AimDirection(Camera view,Vector3 origin,float range,Rigidbody ignore=null) {
        if(!view) return Vector3.forward;
        Vector3 eye=view.transform.position, forward=view.transform.forward;
        float cameraOffset=Vector3.Distance(eye,origin);
        Vector3 point=eye+forward*(range+cameraOffset); float closest=range+cameraOffset;
        foreach(var hit in Physics.RaycastAll(eye,forward,closest,Mask,QueryTriggerInteraction.Ignore)) {
            if(ignore && hit.rigidbody==ignore) continue;
            if(hit.distance<closest) { closest=hit.distance; point=hit.point; }
        }
        Vector3 to=point-origin;
        return Vector3.Dot(to,forward)>.05f?to.normalized:forward;
    }
    public static bool IsTarget(Collider c) => c && (c.GetComponentInParent<WorldMaterial>() || c.GetComponentInParent<IElementReceiver>()!=null);
}
}
