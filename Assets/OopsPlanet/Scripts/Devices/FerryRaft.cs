using UnityEngine;
namespace OopsPlanet {
// Iron raft on the canal (R2, Water + Magnet). It floats at the far bank; the magnetic water stream drags it
// (BlobMagnetic + forces) to the near dock. Docked, it ferries BLOB across when BLOB stands on it, then waits at
// the far bank. The raft only moves along the canal crossing line, never sinks or leaves the canal.
[RequireComponent(typeof(Rigidbody))]
public class FerryRaft : MonoBehaviour {
    public BlobWorld world;
    public Vector3 nearDock, farDock;
    public float dockRadius=1.2f, ferrySpeed=2.5f;
    public Vector3 standSize=new Vector3(2.6f,1.2f,2.6f);
    public enum State { Floating, Docked, Ferrying, Arrived }
    public State state;
    public int crossings;
    public static readonly System.Collections.Generic.List<FerryRaft> All=new System.Collections.Generic.List<FerryRaft>();
    Rigidbody rb; Vector3 axis; float waterY; Vector3 start;
    void OnEnable() { if(!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }
    public static void ResetAll() { foreach(var r in All.ToArray()) r.ResetRaft(r.start); }
    void Awake() {
        rb=GetComponent<Rigidbody>(); waterY=transform.position.y; start=transform.position;
        axis=(farDock-nearDock); axis.y=0; axis.Normalize();
        rb.useGravity=false; rb.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotation; rb.linearDamping=2f;
    }
    void FixedUpdate() {
        if(state!=State.Floating) return;
        // stay on the crossing line
        Vector3 p=rb.position; Vector3 rel=p-nearDock; float along=Mathf.Clamp(Vector3.Dot(rel,axis),0,Vector3.Distance(Flat(nearDock),Flat(farDock)));
        Vector3 onLine=nearDock+axis*along; onLine.y=waterY;
        rb.position=Vector3.Lerp(p,onLine,.5f);
        if(Vector3.Distance(Flat(rb.position),Flat(nearDock))<dockRadius) Dock();
    }
    static Vector3 Flat(Vector3 v) { v.y=0; return v; }
    void Dock() { state=State.Docked; rb.isKinematic=true; rb.position=new Vector3(nearDock.x,waterY,nearDock.z); }
    void Update() {
        if(!world || !world.player) return;
        var pl=world.player;
        bool on=new Bounds(transform.position+Vector3.up*standSize.y/2,standSize).Contains(pl.transform.position+Vector3.up*.1f);
        if(state==State.Docked && on) state=State.Ferrying;
        if(state!=State.Ferrying) return;
        Vector3 goal=new Vector3(farDock.x,waterY,farDock.z);
        Vector3 before=transform.position;
        Vector3 next=Vector3.MoveTowards(before,goal,ferrySpeed*Time.deltaTime);
        rb.MovePosition(next); transform.position=next;
        if(on && pl.Controller) pl.Controller.Move(next-before);
        Physics.SyncTransforms();
        if(Vector3.Distance(next,goal)<.05f) { state=State.Arrived; crossings++; }
    }
    public void ResetRaft(Vector3 start) {
        state=State.Floating; rb.isKinematic=false; transform.position=start; rb.position=start; rb.linearVelocity=Vector3.zero;
    }
}
}
