using UnityEngine;
using UnityEngine.InputSystem;
namespace OopsPlanet {
// Magnet + Pressure actions and the two supported combinations. F uses the primary power, G the secondary one.
//  Magnet   : hold to pull a BlobMagnetic body to BLOB (Shift + hold pushes it away). Range, force, mass limit, line of sight.
//  Pressure : press for a cone blast that pushes light rigidbodies and IPressureReceiver mechanisms. Has a cooldown.
//  Bounce   : passive, uses SPACE (handled by BlobPlayer movement).
//  Magnet + Pressure = MagnetCannon : press Pressure while a body is held -> it is launched along the aim.
//  Pressure + Bounce = PressureDash : press Pressure while airborne from a bounce -> one horizontal dash per air time.
//  Any other pair (Magnet + Bounce) is not combined: each power works on its own and BLOB says so.
[RequireComponent(typeof(BlobPlayer))]
public class BlobPowers : MonoBehaviour {
    public PowerSet set;
    public float holdDistance=1.9f, holdHeight=1.1f;
    public BlobMagnetic held;
    public bool pushing;
    public float pressureReadyAt;
    public bool dashUsed;
    public int blasts, cannons, dashes;
    public string lastCombo="";
    BlobPlayer p;
    LineRenderer magnetLine;
    LineRenderer[] blastLines;
    float blastUntil, losLostFor;
    bool wasPrimary, wasSecondary;
    public PowerDefinition Def(BlobProperty prop) => set?set.Get(prop):null;
    public CombinationDefinition ActiveCombo => set?set.Resolve(p.property,p.secondary):null;
    public float PressureCooldown01 { get { var d=Def(BlobProperty.Pressure); return d && d.cooldown>0?Mathf.Clamp01((pressureReadyAt-Time.time)/d.cooldown):0; } }
    void Awake() {
        p=GetComponent<BlobPlayer>();
        var mat=GetComponent<LineRenderer>()?GetComponent<LineRenderer>().sharedMaterial:null;
        magnetLine=MakeLine("Magnet line",mat,.06f);
        blastLines=new LineRenderer[7];
        for(int i=0;i<blastLines.Length;i++) blastLines[i]=MakeLine("Pressure blast "+i,mat,.08f);
    }
    LineRenderer MakeLine(string name,Material mat,float width) {
        var go=new GameObject(name); go.transform.SetParent(transform,false);
        var l=go.AddComponent<LineRenderer>(); l.sharedMaterial=mat; l.positionCount=2; l.startWidth=width; l.endWidth=width*.3f; l.enabled=false;
        return l;
    }
    public void ResetState() { Release(); dashUsed=false; pressureReadyAt=0; lastCombo=""; }
    void Update() {
        if(p.paused || p.completed || !set) { Release(); return; }
        var k=Keyboard.current;
        bool primary=p.testMode?p.testAction:k!=null&&k.fKey.isPressed;
        bool secondary=p.testMode?p.testSecondary:k!=null&&k.gKey.isPressed;
        pushing=p.testMode?p.testPush:k!=null&&k.leftShiftKey.isPressed;
        if(!p.testMode && k!=null && k.qKey.wasPressedThisFrame) p.SwapProperties();
        if(p.Grounded) dashUsed=false;
        bool magnetKey=(p.property==BlobProperty.Magnet&&primary)||(p.secondary==BlobProperty.Magnet&&secondary);
        if(!magnetKey) Release();
        if(primary&&!wasPrimary) Press(p.property);
        if(secondary&&!wasSecondary) Press(p.secondary);
        wasPrimary=primary; wasSecondary=secondary;
        if(magnetKey && !held) Grab();
        UpdateVisuals();
    }
    void Press(BlobProperty prop) {
        switch(prop) {
            case BlobProperty.Pressure: Pressure(); break;
            case BlobProperty.Bounce: p.Notify("เด้ง: กด SPACE ค้างเพื่อชาร์จ แล้วปล่อย"); break;
            case BlobProperty.Magnet:
                if(p.bounceAirborne && p.Has(BlobProperty.Bounce) && set.Resolve(BlobProperty.Magnet,BlobProperty.Bounce)==null)
                    p.Notify("แม่เหล็ก + เด้ง: ผสมกันไม่ได้ — ใช้แยกกัน",0);
                break;
        }
    }
    Vector3 Origin => transform.position+Vector3.up*.8f;
    Vector3 Aim => p.view?p.view.transform.forward:transform.forward;
    void Grab() {
        var d=Def(BlobProperty.Magnet); if(!d) return;
        // Lock onto the in-range body closest to the aim centre; if that one is too heavy or blocked,
        // refuse it instead of silently grabbing a different body nearby.
        BlobMagnetic best=null; float bestAngle=d.aimCone;
        foreach(var m in BlobMagnetic.All) {
            if((m.Body.worldCenterOfMass-Origin).magnitude>d.range) continue;
            float angle=Vector3.Angle(Aim,m.Body.worldCenterOfMass-p.view.transform.position);
            if(angle<bestAngle) { best=m; bestAngle=angle; }
        }
        held=null; losLostFor=0;
        if(!best) return;
        string reason=null;
        if(best.Body.mass>d.massLimit) reason="หนักเกินไปสำหรับแม่เหล็ก";
        else if(Physics.Linecast(Origin,best.Body.worldCenterOfMass,out RaycastHit hit,~(1<<2),QueryTriggerInteraction.Ignore) && hit.rigidbody!=best.Body) reason="มีสิ่งกีดขวาง";
        if(reason==null) held=best;
        else if(p.feedback=="") p.Notify("แม่เหล็ก: "+reason);
    }
    public void Release() { held=null; if(magnetLine) magnetLine.enabled=false; }
    void FixedUpdate() {
        if(!held) return;
        var d=Def(BlobProperty.Magnet);
        var body=held.Body;
        Vector3 to=body.worldCenterOfMass-Origin;
        bool blocked=Physics.Linecast(Origin,body.worldCenterOfMass,out RaycastHit hit,~(1<<2),QueryTriggerInteraction.Ignore) && hit.rigidbody!=body;
        losLostFor=blocked?losLostFor+Time.fixedDeltaTime:0;
        if(to.magnitude>d.range*1.3f || losLostFor>.3f) { Release(); return; }
        Vector3 force;
        if(pushing) force=Aim*d.force*1.5f;
        else {
            Vector3 flat=new Vector3(to.x,0,to.z); if(flat.sqrMagnitude<.01f) flat=transform.forward;
            Vector3 desired=transform.position+flat.normalized*holdDistance+Vector3.up*holdHeight;
            force=(desired-body.worldCenterOfMass)*d.force-body.linearVelocity*6f-Physics.gravity;
        }
        body.AddForce(Vector3.ClampMagnitude(force,70),ForceMode.Acceleration);
    }
    public bool HeldClose => held && Vector3.Distance(held.Body.worldCenterOfMass,transform.position+Vector3.up*holdHeight)<3f;
    void Pressure() {
        var d=Def(BlobProperty.Pressure); if(!d) return;
        if(Time.time<pressureReadyAt) { p.Notify("แรงดัน: กำลังชาร์จ..."); return; }
        pressureReadyAt=Time.time+d.cooldown;
        var combo=ActiveCombo;
        if(combo && combo.effect==ComboEffect.MagnetCannon && HeldClose) { MagnetCannon(combo); return; }
        if(combo && combo.effect==ComboEffect.PressureDash && p.bounceAirborne && !p.Grounded && !dashUsed) { PressureDash(combo); return; }
        Blast(d);
    }
    void Blast(PowerDefinition d) {
        blasts++;
        Vector3 dir=Aim;
        // Bodies are pushed along the flattened aim with a little lift, so a downward camera angle
        // does not just press them into the floor.
        Vector3 flat=Vector3.ProjectOnPlane(dir,Vector3.up); if(flat.sqrMagnitude<.01f) flat=transform.forward;
        Vector3 push=(flat.normalized+Vector3.up*.25f).normalized;
        foreach(var c in Physics.OverlapSphere(Origin,d.range,~(1<<2),QueryTriggerInteraction.Collide)) {
            Vector3 to=c.ClosestPoint(Origin)-Origin;
            if(to.magnitude>.05f && Vector3.Angle(dir,to)>35) continue;
            float strength=1-.5f*Mathf.Clamp01(to.magnitude/d.range);
            var receiver=c.GetComponentInParent<IPressureReceiver>();
            if(receiver!=null) receiver.OnPressure(dir,strength);
            var rb=c.attachedRigidbody;
            if(rb && !rb.isKinematic && rb.mass<=d.massLimit) rb.AddForce(push*d.force*strength*Mathf.Max(1,rb.mass*.5f),ForceMode.Impulse);
        }
        blastUntil=Time.time+.2f;
        p.Notify("แรงดัน!",2);
    }
    void MagnetCannon(CombinationDefinition combo) {
        var body=held.Body; Release();
        body.linearVelocity=Aim*combo.speed+Vector3.up*combo.lift;
        cannons++; lastCombo=combo.nameTh; blastUntil=Time.time+.2f;
        p.Notify("คอมโบ: "+combo.nameTh+"!",4);
    }
    void PressureDash(CombinationDefinition combo) {
        Vector3 flat=Vector3.ProjectOnPlane(Aim,Vector3.up).normalized;
        p.externalVelocity=flat*combo.speed;
        p.velocity.y=Mathf.Max(p.velocity.y,combo.lift);
        dashUsed=true; dashes++; lastCombo=combo.nameTh; blastUntil=Time.time+.2f;
        p.Notify("คอมโบ: "+combo.nameTh+"!",4);
    }
    void UpdateVisuals() {
        if(magnetLine) {
            magnetLine.enabled=held;
            if(held) {
                magnetLine.SetPosition(0,Origin); magnetLine.SetPosition(1,held.Body.worldCenterOfMass);
                magnetLine.startColor=magnetLine.endColor=BlobPlayer.PropertyColor(BlobProperty.Magnet);
            }
        }
        bool on=Time.time<blastUntil;
        float t=1-Mathf.Clamp01((blastUntil-Time.time)/.2f);
        Vector3 dir=Aim; Vector3 right=Vector3.Cross(Vector3.up,dir).normalized; Vector3 up=Vector3.Cross(dir,right);
        for(int i=0;i<blastLines.Length;i++) {
            var l=blastLines[i]; l.enabled=on; if(!on) continue;
            float a=i*Mathf.PI*2/blastLines.Length;
            Vector3 spread=(dir+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*.35f).normalized;
            l.SetPosition(0,Origin+spread*(.5f+t*2)); l.SetPosition(1,Origin+spread*(1.5f+t*4));
            l.startColor=l.endColor=BlobPlayer.PropertyColor(BlobProperty.Pressure);
        }
    }
}
}
