using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
// Six standalone powers and fifteen unordered combinations.
// F uses the selected single power. G uses the pair's fixed recipe; Q changes the selected single power.
// The recipe stored in ComboCell is an internal implementation detail, never a player-facing ordering.
[RequireComponent(typeof(BlobPlayer))]
public class BlobElementPowers : MonoBehaviour {
    [Header("Verbs")]
    public float sprayRange=7f, sprayRadius=.6f, sprayTick=.2f;
    public float heatRange=4.5f, heatAngle=30f, heatTick=.3f;
    public float sparkRange=9f, sparkCone=15f, sparkCooldown=.5f;
    [Header("Combos")]
    public float electromagnetSeconds=8f, electromagnetMassLimit=40f;
    public float slingRange=14f, slingCone=14f, slingSpeed=13f, slingLift=9f;
    public float landingRadius=3.5f, landingMinImpact=6f;
    public float carrierSeconds=20f, jetRange=14f, flameRange=10f, inductionRange=12f;
    [Header("Journal (discovered combo cells, saved)")]
    public List<string> discovered=new List<string>();
    [Header("Counters (tests)")]
    public int sprays, heatTicks, sparks, slings, clings, landingFx, payloads;
    public string lastCell="";
    public float electromagnetUntil;
    public bool ElectromagnetActive => Time.time<electromagnetUntil;
    public event System.Action<ComboCell> CellUsed, Discovered;
    BlobPlayer p; BlobPowers pw;
    float nextCombo;
    float nextSpray, nextHeat, nextSpark, nextBall, nextSteam, nextLanding, clingUntil;
    Vector3 clingNormal; bool cling;
    Vector3 slingGoal; bool slingArcing; float slingStarted;
    ComboTable Table => pw && pw.set ? pw.set.table : null;
    void Awake() {
        p=GetComponent<BlobPlayer>(); pw=GetComponent<BlobPowers>();


        if(pw) pw.ComboUsed+=e=>Use(e==ComboEffect.MagnetCannon?Cell(BlobProperty.Magnet,BlobProperty.Pressure):Cell(BlobProperty.Bounce,BlobProperty.Pressure));
    }
    public ComboCell Cell(BlobProperty a,BlobProperty b) => Table?Table.Get(a,b):null;
    // The cell F would use right now (primary verb + secondary element); null with fewer than two powers.
    public ComboCell ActiveCell => Cell(p.property,p.secondary);
    public void ResetState() { electromagnetUntil=0; slingArcing=false; EndCling(false); lastCell=""; }
    Vector3 Origin => transform.position+Vector3.up*.8f;
    Vector3 Aim => p.view?p.view.transform.forward:transform.forward;
    Vector3 AimFrom => p.view?p.view.transform.position:Origin;
    void Notify(string s,int tone=4) => p.Notify(s,tone);
    // ---------- input (called by BlobPowers) ----------
    // A key was pressed for power prop. isPrimary: F (prop is the primary, a combo applies when a secondary is held).
    // Returns true when the press was fully handled here.
    public bool OnPress(BlobProperty prop,bool isPrimary) {
        var second=BlobProperty.None;
        switch(prop) {
            case BlobProperty.Electric: Spark(second); return true;
            case BlobProperty.Water: if(second==BlobProperty.Bounce) { WaterBall(); return true; } nextSpray=0; return true;
            case BlobProperty.Heat: if(second==BlobProperty.Bounce) { FireBall(); return true; } nextHeat=0; return true;
            case BlobProperty.Magnet: if(second==BlobProperty.Bounce) return Slingshot();
                return false;
        }
        return false;
    }
    public bool UseCombination() {
        var c=ActiveCell;
        if(c==null) { Notify("ดูดพลังสองชนิดเพื่อผสมกัน"); return false; }
        if(c.primary==BlobProperty.Pressure) { if(Time.time<nextCombo) return false; nextCombo=Time.time+.5f; }
        switch(c.primary) {
            case BlobProperty.Magnet:
                if(c.secondary==BlobProperty.Pressure) return pw.FireComboCannon();
                if(c.secondary==BlobProperty.Bounce) { bool used=Slingshot(); if(!used) Notify("เล็งจุดยึดโลหะในระยะก่อนใช้สลิง"); return used; }
                break;
            case BlobProperty.Pressure: PressurePayload(c.secondary); break;
            case BlobProperty.Electric: Spark(c.secondary); break;
            case BlobProperty.Water: if(c.secondary==BlobProperty.Bounce) WaterBall(); else nextSpray=0; break;
            case BlobProperty.Heat: if(c.secondary==BlobProperty.Bounce) FireBall(); else nextHeat=0; break;
        }
        return false;
    }
    // Held verbs (spray / heat) tick while their key is down. Magnet + element charges the held body.
    void Update() {
        if(!pw || p.paused || p.completed) { if(cling) EndCling(false); return; }
        HoldTick(p.property,pw.PrimaryHeld,BlobProperty.None);
        var cell=ActiveCell;
        if(cell!=null) HoldTick(cell.primary,pw.SecondaryHeld,cell.secondary);
        UpdateCling();
        if(slingArcing) {
            if(Time.time-slingStarted>3.5f || (p.Grounded && Time.time-slingStarted>.2f)) { slingArcing=false; p.externalVelocity=Vector3.zero; }
            else if(transform.position.y<slingGoal.y && p.velocity.y>0) p.externalVelocity=Vector3.zero;
            else { Vector3 delta=Vector3.ProjectOnPlane(slingGoal-transform.position,Vector3.up); p.externalVelocity=delta.normalized*Mathf.Min(slingSpeed,delta.magnitude*4); }
        }
    }
    static bool IsElement(BlobProperty e) => e==BlobProperty.Water||e==BlobProperty.Electric||e==BlobProperty.Heat;
    void HoldTick(BlobProperty prop,bool held,BlobProperty second) {
        if(!held) return;
        if(prop==BlobProperty.Water) {
            if(second==BlobProperty.Bounce) { if(Time.time>=nextBall) WaterBall(); return; }
            if(Time.time>=nextSpray) { nextSpray=Time.time+sprayTick; Spray(second); }
        } else if(prop==BlobProperty.Heat) {
            if(second==BlobProperty.Bounce) { if(Time.time>=nextBall) FireBall(); return; }
            if(Time.time>=nextHeat) { nextHeat=Time.time+heatTick; HeatVerb(second); }
        }
    }
    // ---------- Water ----------
    void Spray(BlobProperty second) {
        sprays++;
        string id=second==BlobProperty.None?"":ComboTable.Id(BlobProperty.Water,second);
        Vector3 o=Origin, dir=ElementHit.AimDirection(p.view,Origin,Mathf.Max(jetRange,flameRange));
        if(second==BlobProperty.Pressure) {                       // sprinkler: wide mist, many targets
            ElementHit.Cone(o,dir,6f,60f,BlobProperty.Water,id);
            Vfx(o,dir,BlobProperty.Water,18,6,70); Use(Cell(BlobProperty.Water,second)); return;
        }
        Vector3 end=ElementHit.RayEnd(o,dir,sprayRange,out _,out _);
        ElementHit.Capsule(o,end,sprayRadius,BlobProperty.Water,id);
        Stream(o,end,BlobProperty.Water,.16f,sprayTick+.03f);
        foreach(var c in Physics.OverlapCapsule(o,end,sprayRadius,ElementHit.Mask,QueryTriggerInteraction.Ignore)) {
            var rb=c.attachedRigidbody; if(rb && !rb.isKinematic && rb.mass<=3) rb.AddForce(dir*2f,ForceMode.Impulse);
        }
        switch(second) {
            case BlobProperty.Electric: ElementHit.Capsule(o,end,sprayRadius,BlobProperty.Electric,id); break;
            case BlobProperty.Magnet:                              // magnetic stream drags floating metal toward BLOB
                foreach(var m in BlobMagnetic.All) {
                    var b=m.Body; if(b.mass>30) continue;
                    if(DistanceToSegment(b.worldCenterOfMass,o,end)>1.6f) continue;
                    Vector3 to=transform.position-b.worldCenterOfMass; to.y=0;
                    b.AddForce(to.normalized*14f,ForceMode.Acceleration);
                }
                break;
            case BlobProperty.Heat:                                // steam cloud at the end of the stream
                if(Time.time>=nextSteam) { nextSteam=Time.time+.6f; var steam=Steam(); if(steam) steam.RaiseAt(end); ElementHit.Sphere(end,1.5f,BlobProperty.Heat,id); }
                break;
        }
        Vfx(o,dir,BlobProperty.Water,6,8,8);
        if(second!=BlobProperty.None) Use(Cell(BlobProperty.Water,second));
    }
    void WaterBall() {
        nextBall=Time.time+.5f; payloads++;
        Vector3 dir=ElementHit.AimDirection(p.view,Origin,20f);
        ElementProjectile.Spawn(Origin+dir*.6f,dir*11f+Vector3.up*3f,BlobProperty.Water,ComboTable.Id(BlobProperty.Water,BlobProperty.Bounce),3,9f,BlobPlayer.PropertyColor(BlobProperty.Water));
        Use(Cell(BlobProperty.Water,BlobProperty.Bounce));
    }
    // ---------- Heat ----------
    void HeatVerb(BlobProperty second) {
        heatTicks++;
        string id=second==BlobProperty.None?"":ComboTable.Id(BlobProperty.Heat,second);
        Vector3 o=Origin, dir=ElementHit.AimDirection(p.view,Origin,Mathf.Max(jetRange,flameRange));
        switch(second) {
            case BlobProperty.Magnet: {                            // induction: a far metal piece turns red-hot
                var t=FindTarget(inductionRange,10f,c=>{ var m=c.GetComponentInParent<WorldMaterial>(); return m && m.kind==MaterialKind.Metal; });
                if(t) { ElementHit.Collider(t,BlobProperty.Heat,id,t.bounds.center); Vfx(o,(t.bounds.center-o).normalized,BlobProperty.Heat,8,10,6); Use(Cell(BlobProperty.Heat,second)); }
                return;
            }
            case BlobProperty.Pressure: {                          // flamethrower: a long line of fire
                Vector3 end=ElementHit.RayEnd(o,dir,flameRange,out _,out _);
                ElementHit.Capsule(o,end,.8f,BlobProperty.Heat,id);
                Stream(o,end,BlobProperty.Heat,.3f,heatTick+.03f);
                Vfx(o,dir,BlobProperty.Heat,14,12,6); Use(Cell(BlobProperty.Heat,second)); return;
            }
        }
        ElementHit.Cone(o,dir,heatRange,heatAngle,BlobProperty.Heat,id);
        Stream(o,ElementHit.RayEnd(o,dir,heatRange,out _,out _),BlobProperty.Heat,.24f,heatTick+.03f);
        if(second==BlobProperty.Water && Time.time>=nextSteam) {   // boil: steam where the heat lands
            nextSteam=Time.time+.8f;
            Vector3 end=ElementHit.RayEnd(o,dir,heatRange,out _,out bool hit);
            if(hit) { var steam=Steam(); if(steam) steam.RaiseAt(end); }
        }
        if(second==BlobProperty.Electric) ElementHit.Cone(o,dir,heatRange,heatAngle,BlobProperty.Electric,id);
        Vfx(o,dir,BlobProperty.Heat,8,5,heatAngle);
        if(second!=BlobProperty.None) Use(Cell(BlobProperty.Heat,second));
    }
    void FireBall() {
        nextBall=Time.time+.6f; payloads++;
        Vector3 dir=ElementHit.AimDirection(p.view,Origin,20f);
        ElementProjectile.Spawn(Origin+dir*.6f,dir*12f+Vector3.up*2f,BlobProperty.Heat,ComboTable.Id(BlobProperty.Heat,BlobProperty.Bounce),3,12f,BlobPlayer.PropertyColor(BlobProperty.Heat));
        Use(Cell(BlobProperty.Heat,BlobProperty.Bounce));
    }
    // ---------- Electric ----------
    void Spark(BlobProperty second) {
        if(Time.time<nextSpark) { Notify("ไฟฟ้า: กำลังชาร์จ...",0); return; }
        nextSpark=Time.time+sparkCooldown; sparks++;
        string id=second==BlobProperty.None?"":ComboTable.Id(BlobProperty.Electric,second);
        if(second==BlobProperty.Magnet) { electromagnetUntil=Time.time+electromagnetSeconds; Notify("แม่เหล็กไฟฟ้า! ยกของหนักได้ถึง "+electromagnetMassLimit+" kg ชั่วคราว"); Use(Cell(BlobProperty.Electric,second)); }
        float range=second==BlobProperty.Pressure?sparkRange*2:sparkRange;
        var t=FindTarget(range,sparkCone,ElementHit.IsTarget);
        if(!t) { Vfx(Origin,Aim,BlobProperty.Electric,6,9,5); if(second!=BlobProperty.Magnet) Notify("ไฟฟ้า: ไม่มีเป้าที่รับไฟได้",0); return; }
        var hit=new List<Collider>{t};
        Zap(Origin,t,second,id);
        if(second==BlobProperty.Bounce) {                          // chain to 2 more targets, nearest first
            Collider from=t;
            for(int i=0;i<3;i++) {
                Collider next=null; float best=6f;
                foreach(var c in Physics.OverlapSphere(from.bounds.center,6f,ElementHit.Mask,QueryTriggerInteraction.Collide)) {
                    if(hit.Contains(c) || !ElementHit.IsTarget(c) || SameObject(c,hit)) continue;
                    if(t.GetComponentInParent<ElectricSwitch>() && !c.GetComponentInParent<ElectricSwitch>()) continue;
                    float d=Vector3.Distance(from.bounds.center,c.bounds.center); if(d<best) { best=d; next=c; }
                }
                if(!next) break;
                hit.Add(next); Zap(from.bounds.center,next,second,id); from=next;
            }
        }
        if(second==BlobProperty.Pressure) {
            for(int i=0;i<2;i++) { var extra=FindTarget(range,45f,c=>ElementHit.IsTarget(c) && !SameObject(c,hit)); if(!extra) break; hit.Add(extra); Zap(Origin,extra,BlobProperty.None,id); }
        }
        if(second!=BlobProperty.None && second!=BlobProperty.Magnet) Use(Cell(BlobProperty.Electric,second));
    }
    static bool SameThing(Collider a,Collider b) {
        if(a==b || a.transform.IsChildOf(b.transform) || b.transform.IsChildOf(a.transform)) return true;
        var ma=a.GetComponentInParent<WorldMaterial>(); return ma && ma==b.GetComponentInParent<WorldMaterial>();
    }
    static bool SameObject(Collider c,List<Collider> list) {
        var m=c.GetComponentInParent<WorldMaterial>(); var r=c.GetComponentInParent<IElementReceiver>() as MonoBehaviour;
        foreach(var o in list) { if(m && o.GetComponentInParent<WorldMaterial>()==m) return true; if(r && (o.GetComponentInParent<IElementReceiver>() as MonoBehaviour)==r) return true; }
        return false;
    }
    void Zap(Vector3 from,Collider target,BlobProperty second,string id) {
        Vector3 pt=target.bounds.center;
        if(second==BlobProperty.Water) ElementHit.Collider(target,BlobProperty.Water,id,pt);      // soak first: everything wet conducts
        ElementHit.Collider(target,BlobProperty.Electric,id,pt);
        if(second==BlobProperty.Heat) ElementHit.Collider(target,BlobProperty.Heat,id,pt);        // short circuit: melts fuses
        VfxKit.Play(k=>k.Stroke(from,pt,BlobPlayer.PropertyColor(BlobProperty.Electric),.18f,.28f,true));
        Vfx(from,(pt-from).normalized,BlobProperty.Electric,Mathf.Clamp((int)Vector3.Distance(from,pt)*2,4,24),Vector3.Distance(from,pt)*2.5f,3);
    }
    // Best element target near the aim centre, in range and in line of sight.
    public Collider FindTarget(float range,float cone,System.Func<Collider,bool> accept) {
        Collider best=null; float bestAngle=cone; Vector3 eye=AimFrom, dir=Aim;
        foreach(var c in Physics.OverlapSphere(Origin,range,ElementHit.Mask,QueryTriggerInteraction.Collide)) {
            if(!c.enabled || !accept(c)) continue;
            Vector3 pt=c.bounds.center;
            float a=Vector3.Angle(dir,pt-eye);
            if(a>=bestAngle) { Vector3 cp=c.ClosestPoint(eye+dir*Vector3.Distance(eye,pt)); a=Vector3.Angle(dir,cp-eye); if(a>=bestAngle) continue; pt=cp; }
            if(Physics.Linecast(Origin,pt,out RaycastHit h,ElementHit.Mask,QueryTriggerInteraction.Ignore) && !SameThing(h.collider,c)) continue;
            best=c; bestAngle=a;
        }
        return best;
    }
    // ---------- Pressure payloads (BlobPowers calls this after a primary Pressure blast) ----------
    public void PressurePayload(BlobProperty second) {
        string id=ComboTable.Id(BlobProperty.Pressure,second);
        Vector3 o=Origin, dir=ElementHit.AimDirection(p.view,Origin,Mathf.Max(jetRange,flameRange)); payloads++;
        switch(second) {
            case BlobProperty.Magnet: {                            // iron nail -> anchor in wood
                var n=ElementProjectile.Spawn(o+dir*.6f,dir*24f,BlobProperty.Magnet,id,0,0,BlobPlayer.PropertyColor(BlobProperty.Magnet),.2f); n.nail=true; break;
            }
            case BlobProperty.Bounce: {                            // wind ball bouncing round corners
                var b=ElementProjectile.Spawn(o+dir*.6f,dir*14f,BlobProperty.Pressure,id,3,0,BlobPlayer.PropertyColor(BlobProperty.Pressure),.4f); b.wind=true; b.bounceDamping=1; break;
            }
            case BlobProperty.Water: {                             // high-pressure jet: far, pushes heavier things
                Vector3 end=ElementHit.RayEnd(o,dir,jetRange,out _,out _);
                ElementHit.Capsule(o,end,.7f,BlobProperty.Water,id);
                Stream(o,end,BlobProperty.Water,.26f,.3f);
                foreach(var c in Physics.OverlapCapsule(o,end,.7f,ElementHit.Mask,QueryTriggerInteraction.Ignore)) {
                    var rb=c.attachedRigidbody; if(rb && !rb.isKinematic && rb.mass<=24) rb.AddForce((Vector3.ProjectOnPlane(dir,Vector3.up).normalized+Vector3.up*.25f).normalized*10f*Mathf.Max(1,rb.mass*.4f),ForceMode.Impulse);
                }
                Vfx(o,dir,BlobProperty.Water,16,16,5); break;
            }
            case BlobProperty.Electric: {                          // lightning wind: up to 3 targets in a wide cone
                var done=new List<Collider>();
                for(int i=0;i<3;i++) {
                    var t=FindTarget(10f,45f,c=>ElementHit.IsTarget(c) && !done.Contains(c) && !SameObject(c,done));
                    if(!t) break; done.Add(t); Zap(o,t,BlobProperty.None,id);
                }
                break;
            }
            case BlobProperty.Heat: {                              // hot air: heats the cone, lifts BLOB when aimed down
                ElementHit.Cone(o,dir,6f,35f,BlobProperty.Heat,id);
                if(p.pitch>30f) { p.velocity.y=Mathf.Max(p.velocity.y,13f); Notify("ลมร้อนพา BLOB ลอยขึ้น!"); }
                Vfx(o,dir,BlobProperty.Heat,16,7,35); break;
            }
            default: payloads--; return;
        }
        Use(Cell(BlobProperty.Pressure,second));
    }
    // ---------- Magnet + Bounce: slingshot toward a metal anchor ----------
    bool Slingshot() {
        Vector3 eye=AimFrom, dir=Aim; Vector3? target=null; float best=slingCone;
        foreach(var a in MagnetAnchor.All) {
            float d=Vector3.Distance(Origin,a.transform.position); if(d>slingRange) continue;
            float ang=Vector3.Angle(dir,a.transform.position-eye); if(ang<best) { best=ang; target=a.transform.position; }
        }
        if(target==null) {
            var c=FindTarget(slingRange,slingCone,x=>{ var m=x.GetComponentInParent<WorldMaterial>(); return m && m.kind==MaterialKind.Metal && !x.attachedRigidbody; });
            if(c) target=c.ClosestPoint(Origin);
        }
        if(target==null) return false;                              // nothing to pull to: the normal Magnet hold runs
        Vector3 to=target.Value-transform.position; Vector3 flat=new Vector3(to.x,0,to.z);
        float horizontal=Mathf.Min(slingSpeed,flat.magnitude*1.6f+2f);
        p.externalVelocity=flat.normalized*horizontal;
        p.velocity.y=Mathf.Max(slingLift,Mathf.Sqrt(2*p.gravity*Mathf.Max(.5f,to.y+1.2f)));
        // Close high anchors need an arc over the wall before pulling horizontally.
        if(to.y>3f && flat.magnitude<9f) {
            slingGoal=target.Value+flat.normalized*2.3f+Vector3.up*2f;
            p.velocity.y=Mathf.Sqrt(2*p.gravity*(to.y+2.8f));
            p.externalVelocity=Vector3.zero; slingArcing=true; slingStarted=Time.time;
        }
        p.bounceAirborne=true;
        slings++; Vfx(Origin,(target.Value-Origin).normalized,BlobProperty.Magnet,Mathf.Clamp((int)to.magnitude*2,6,28),to.magnitude*2.5f,3);
        Use(Cell(BlobProperty.Magnet,BlobProperty.Bounce));
        return true;
    }
    // ---------- Bounce + Magnet: cling to metal walls and climb ----------
    void OnColliderHit(ControllerColliderHit hit) {
        if(cling || p.property!=BlobProperty.Bounce || p.secondary!=BlobProperty.Magnet || p.Grounded) return;
        if(Mathf.Abs(hit.normal.y)>.35f) return;
        if(!IsMetal(hit.collider)) return;
        cling=true; clingNormal=new Vector3(hit.normal.x,0,hit.normal.z).normalized; clingUntil=Time.time+4f;
        p.clinging=true; clings++;
        Use(Cell(BlobProperty.Bounce,BlobProperty.Magnet));
    }
    static bool IsMetal(Collider c) { var m=c.GetComponentInParent<WorldMaterial>(); return (m && (m.kind==MaterialKind.Metal||m.kind==MaterialKind.Machine)) || c.GetComponentInParent<MagnetAnchor>(); }
    void UpdateCling() {
        if(!cling) return;
        if(p.property!=BlobProperty.Bounce || p.secondary!=BlobProperty.Magnet || Time.time>clingUntil) { EndCling(true); return; }
        p.externalVelocity=-clingNormal*1.5f;                     // stay pressed against the wall while climbing
        Vector3 chest=transform.position+Vector3.up*.65f;
        bool wall=Physics.Raycast(chest,-clingNormal,out RaycastHit h,.9f,ElementHit.Mask,QueryTriggerInteraction.Ignore) && IsMetal(h.collider);
        if(!wall) EndCling(true);
    }
    void EndCling(bool hop) {
        if(!cling) return; cling=false; p.clinging=false;
        if(hop) { p.velocity.y=Mathf.Max(p.velocity.y,5.5f); p.externalVelocity=-clingNormal*4.5f; }
    }
    // ---------- Bounce + element: landing effects ----------
    void OnLanded(float impact) {
        if(!p.bounceAirborne || p.property!=BlobProperty.Bounce || impact<landingMinImpact || Time.time<nextLanding) return;
        var e=p.secondary; if(!IsElement(e)) return;
        nextLanding=Time.time+.4f; landingFx++;
        Vector3 at=transform.position+Vector3.up*.3f;
        ElementHit.Sphere(at,landingRadius,e,ComboTable.Id(BlobProperty.Bounce,e));
        VfxKit.Play(k=>k.Ring(transform.position+Vector3.up*.1f,BlobPlayer.PropertyColor(e),36,landingRadius*2.2f,.16f,.5f));
        Use(Cell(BlobProperty.Bounce,e));
    }
    // ---------- journal ----------
    public void Use(ComboCell cell) {
        if(cell==null) return;
        lastCell=cell.id; CellUsed?.Invoke(cell);
        if(discovered.Contains(cell.id)) return;
        discovered.Add(cell.id); Discovered?.Invoke(cell);
        Notify("ค้นพบคอมโบใหม่: "+cell.nameTh+"  (สมุดพลัง "+discovered.Count+"/15)");
    }
    public void RestoreDiscoveries(IEnumerable<string> ids) {
        foreach(var legacy in ids) { var id=ComboTable.NormalizeId(legacy); if(Table && Table.cells.Exists(c=>c.id==id) && !discovered.Contains(id)) discovered.Add(id); }
    }
    public bool Knows(string id) => discovered.Contains(ComboTable.NormalizeId(id));
    SteamSystem Steam() => p.world?p.world.GetComponent<SteamSystem>():null;
    static float DistanceToSegment(Vector3 x,Vector3 a,Vector3 b) { Vector3 ab=b-a; float t=Mathf.Clamp01(Vector3.Dot(x-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-4f)); return Vector3.Distance(x,a+ab*t); }
    static void Stream(Vector3 from,Vector3 end,BlobProperty element,float width,float life) {
        VfxKit.Play(k=>{ k.Stroke(from,end,BlobPlayer.PropertyColor(element),width,life);
            k.Burst(end,BlobPlayer.PropertyColor(element),8,1.5f,.16f,.25f); });
    }
    static void Vfx(Vector3 o,Vector3 dir,BlobProperty e,int count,float speed,float spread) =>
        VfxKit.Play(k=>k.Burst(o+dir*.6f,BlobPlayer.PropertyColor(e),count,speed,.09f,.35f,dir,spread));
}
}
