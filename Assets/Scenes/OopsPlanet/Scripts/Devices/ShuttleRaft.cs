using UnityEngine;
namespace OopsPlanet {
// Factory cooling-channel raft (P4). Unlike the canal FerryRaft it shuttles both ways and can never be lost:
//  Floating : starts at the far dock; the magnetic water stream (Water + Magnet) drags it along the crossing line only.
//  Docked   : reaching either dock snaps it there (kinematic). Stepping on carries BLOB to the other dock.
// The raft stays at water level (no gravity, Y and rotation frozen) and Reset() puts it back at its start.
[RequireComponent(typeof(Rigidbody))]
public class ShuttleRaft : MonoBehaviour {
    public static readonly System.Collections.Generic.List<ShuttleRaft> All=new System.Collections.Generic.List<ShuttleRaft>();
    public BlobWorld world;
    public Vector3 nearDock, farDock;
    public float dockRadius=1.1f, speed=2.6f;
    public Vector3 standSize=new Vector3(2.4f,1.4f,2.4f);
    public enum State { Floating, Docked, Moving }
    public State state;
    public bool atNear;
    public int crossings;
    [Tooltip("The reset pad / restart of this puzzle also brings the raft back.")]
    public PuzzleObjective resetWith;
    Rigidbody rb; Vector3 start, goal; float waterY; bool wasOn;
    void OnEnable() { if(!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }
    void Awake() {
        rb=GetComponent<Rigidbody>(); start=transform.position; waterY=start.y;
        rb.useGravity=false; rb.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotation; rb.linearDamping=2.5f;
        if(resetWith) resetWith.WasReset+=o=>ResetRaft();
    }
    Vector3 Flat(Vector3 v) { v.y=waterY; return v; }
    void FixedUpdate() {
        if(state!=State.Floating) return;
        Vector3 a=Flat(nearDock), b=Flat(farDock), ab=b-a; float t=Mathf.Clamp01(Vector3.Dot(rb.position-a,ab)/ab.sqrMagnitude);
        rb.position=Vector3.Lerp(rb.position,a+ab*t,.5f);
        if(Vector3.Distance(rb.position,a)<dockRadius) Dock(true);
    }
    void Dock(bool near) { state=State.Docked; atNear=near; rb.isKinematic=true; var p=Flat(near?nearDock:farDock); rb.position=p; transform.position=p; }
    bool PlayerOn(out BlobPlayer p) {
        p=world?world.player:null; if(!p) return false;
        return new Bounds(transform.position+Vector3.up*standSize.y/2,standSize).Contains(p.transform.position+Vector3.up*.1f);
    }
    void Update() {
        bool on=PlayerOn(out var pl);
        if(state==State.Docked && on && !wasOn) { state=State.Moving; goal=Flat(atNear?farDock:nearDock); }
        if(state==State.Moving) {
            Vector3 before=transform.position, next=Vector3.MoveTowards(before,goal,speed*Time.deltaTime);
            rb.MovePosition(next); transform.position=next;
            if(on && pl.Controller) pl.Controller.Move(next-before);
            Physics.SyncTransforms();
            if(Vector3.Distance(next,goal)<.03f) { crossings++; Dock(!atNear); }
        }
        wasOn=on;
    }
    public void ResetRaft() { state=State.Floating; rb.isKinematic=false; transform.position=start; rb.position=start; rb.linearVelocity=Vector3.zero; wasOn=false; }
    public static void ResetAll() { foreach(var r in All.ToArray()) r.ResetRaft(); }
}
}
