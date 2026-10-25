using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime {
[RequireComponent(typeof(CharacterController))]
public class BTPlayer : MonoBehaviour {
    public BTTrainingGame game;
    public Camera view;
    public Transform visual, rightHand, leftFoot, rightFoot;
    public LineRenderer beam;
    public float energy=50, yaw=0, pitch=22, speed=4.7f, walked;
    public BTTimeTarget target;
    public CharacterController Controller {get;private set;}
    public Vector3 checkpoint, velocity;
    public bool overview, paused, hideHud;
    public string feedback="";
    float feedbackUntil, nextCast, gait, nextAbsorbTone;
    Vector3 cameraVelocity;
    BTLifeFlow lifeFlow;
    public bool IsAbsorbing => lifeFlow && lifeFlow.IsFlowing;
    public struct FrameInput { public Vector2 move, look; public float scroll; public bool aim, absorb, jump, sprint, interact; }
    // Editor playtest injects input into the same movement and targeting path as the keyboard.
    [System.NonSerialized] public System.Func<FrameInput> inputOverride;
    void Awake() {
        Controller=GetComponent<CharacterController>(); checkpoint=transform.position;
        BorrowedTime.Menu.GamePauseMenu.Attach(this);
    }
    void Start() { lifeFlow=GetComponent<BTLifeFlow>(); if(!lifeFlow)lifeFlow=gameObject.AddComponent<BTLifeFlow>(); UpdateCamera(true); }
    public void Message(string s) { feedback=s; feedbackUntil=Time.unscaledTime+3.2f; }
    public bool StandingOn(BTTimeTarget t) {
        var hits=Physics.RaycastAll(transform.position+Vector3.up*.18f,Vector3.down,.65f,~(1<<2),QueryTriggerInteraction.Ignore);
        foreach(var h in hits) if(h.collider.GetComponentInParent<BTTimeTarget>()==t) return true;
        return false;
    }
    public FrameInput CurrentInput {get;private set;}
    public FrameInput ReadInput() {
        if(inputOverride!=null) return inputOverride();
        var f=new FrameInput(); var k=Keyboard.current; var m=Mouse.current;
        if(k!=null) {
            f.move=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
            f.jump=k.spaceKey.wasPressedThisFrame; f.sprint=k.shiftKey.isPressed;
            f.interact=k.fKey.wasPressedThisFrame;
        }
        if(m!=null) { f.look=m.delta.ReadValue(); f.aim=false; f.absorb=m.leftButton.isPressed; f.scroll=m.scroll.ReadValue().y; }
        return f;
    }
    void Update() {
        if(BorrowedTime.Menu.GamePauseMenu.BlocksInputFor(this)) { CurrentInput=default; if(beam)beam.enabled=false; ClearTarget(); return; }
        var k=Keyboard.current;
        if(inputOverride==null && k!=null) {
            if(k.tabKey.wasPressedThisFrame && !paused && game.stage>0) overview=!overview;
            if(k.jKey.wasPressedThisFrame && game.stage>0) {var w=game.GetComponent<BTExpansionWorld>();if(w)w.ToggleJournal();}
            if(k.hKey.wasPressedThisFrame && !paused) {var w=game.GetComponent<BTExpansionWorld>();if(!w||!w.TryHint())game.ShowHint();}
            if(k.rKey.wasPressedThisFrame && !paused && game.stage>0) game.ResetTraining();
            if(k.f5Key.wasPressedThisFrame && game.stage>0) BTSaveGame.Save(game);
            if(k.cKey.wasPressedThisFrame && (game.stage==0 || paused)) BTSaveGame.Continue();
            if(k.f2Key.wasPressedThisFrame) hideHud=!hideHud;
        }
        if(beam) beam.enabled=false;
        CurrentInput=default;
        if(paused || game.stage==0 || game.completed || overview) { ClearTarget(); return; }
        var f=ReadInput(); CurrentInput=f;
        yaw+=f.look.x*.14f; pitch=Mathf.Clamp(pitch-f.look.y*.12f,-12,62);
        var q=Quaternion.Euler(0,yaw,0);
        Vector3 move=q*new Vector3(f.move.x,0,f.move.y); move=Vector3.ClampMagnitude(move,1);
        float dt=Time.deltaTime;
        if(Controller.isGrounded && velocity.y<0) velocity.y=-3;
        if(f.jump && Controller.isGrounded) velocity.y=7;
        velocity.y-=22*dt;
        Vector3 before=transform.position;
        Controller.Move((move*speed*(f.sprint?1.5f:1)+velocity)*dt);
        walked+=Vector3.Distance(new Vector3(before.x,0,before.z),new Vector3(transform.position.x,0,transform.position.z));
        if(move.sqrMagnitude>.02f) {
            visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(move),12*dt);
            gait+=dt*(f.sprint?16:11);
        }
        if(f.absorb || Mathf.Abs(f.scroll)>.01f) visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.Euler(0,yaw,0),15*dt);
        if(leftFoot) leftFoot.localRotation=Quaternion.Euler(Mathf.Sin(gait)*move.magnitude*22,0,0);
        if(rightFoot) rightFoot.localRotation=Quaternion.Euler(-Mathf.Sin(gait)*move.magnitude*22,0,0);
        if(transform.position.y<-7) Respawn();
        UpdateCamera(false); FindTarget(true);
        // The camera-centred reticle always selects the visible target. Aim is
        // presentation state only; no mouse button is required to acquire it.
        f.aim=target && (f.absorb || Mathf.Abs(f.scroll)>.01f); CurrentInput=f;
        if(target) {
            if(f.absorb) TryAbsorb(dt);
            if(f.scroll<-.01f) Cast(-1);
            if(f.scroll>.01f) Cast(1);
        } else if(f.absorb || Mathf.Abs(f.scroll)>.01f) Message("เล็งวงแหวนของวัตถุที่มีพลังเวลา");
        if(f.interact) {var w=game.GetComponent<BTExpansionWorld>();if(!w||!w.TryInteract())game.Interact();BTSaveGame.Request(game);}
    }
    void LateUpdate() {
        if(BorrowedTime.Menu.GamePauseMenu.IsPausedFor(this))return;
        if(overview) {
            view.transform.position=game.mapCameraPosition; view.transform.LookAt(game.mapCameraTarget); view.orthographic=true; view.orthographicSize=game.mapCameraSize;
        } else if(!game.completed) UpdateCamera(false);
    }
    public void UpdateCamera(bool snap) {
        if(!view || overview) return;
        view.orthographic=false; view.fieldOfView=55;
        Vector3 pivot=transform.position+Vector3.up*1.45f;
        Quaternion r=Quaternion.Euler(pitch,yaw,0);
        Vector3 desired=pivot+r*new Vector3(.55f,0,-6.5f);
        Vector3 delta=desired-pivot;
        if(Physics.SphereCast(pivot,.22f,delta.normalized,out RaycastHit hit,delta.magnitude,~(1<<2),QueryTriggerInteraction.Ignore)) desired=pivot+delta.normalized*Mathf.Max(.5f,hit.distance-.15f);
        view.transform.position=snap?desired:Vector3.SmoothDamp(view.transform.position,desired,ref cameraVelocity,.055f);
        view.transform.rotation=r;
    }
    void ClearTarget() { if(target && target.focusRing) target.focusRing.SetActive(false); target=null; }
    public void FindTarget(bool aiming) {
        ClearTarget(); if(!aiming) return;
        var ray=view.ViewportPointToRay(new Vector3(.5f,.5f));
        if(Physics.SphereCast(ray,.18f,out RaycastHit hit,20,~(1<<2),QueryTriggerInteraction.Ignore)) {
            var t=hit.collider.GetComponentInParent<BTTimeTarget>(); if(!t || !t.isActiveAndEnabled) return;
            Vector3 point=t.aimPoint?t.aimPoint.position:hit.point;
            if(Vector3.Distance(transform.position+Vector3.up,point)>8) return;
            Vector3 start=rightHand?rightHand.position:transform.position+Vector3.up;
            if(Physics.Linecast(start,point,out RaycastHit obstruction,~(1<<2),QueryTriggerInteraction.Ignore) && obstruction.collider.GetComponentInParent<BTTimeTarget>()!=t) return;
            target=t; if(t.focusRing) t.focusRing.SetActive(true);
        }
    }
    void DrawBeam(Color color) {
        if(!beam || !target) return;
        beam.enabled=true; beam.startColor=beam.endColor=color;
        beam.SetPosition(0,rightHand.position); beam.SetPosition(1,target.aimPoint.position);
    }
    public void TryAbsorb(float dt) {
        if(!target.IsLiving) { Message("ดูดพลังได้จากพืชเท่านั้น"); return; }
        if(!target.absorbAllowed) { Message("พืชนี้เป็นเป้าหมายเวลา ไม่ใช่แหล่งเติมพลัง"); return; }
        if(Vector3.Distance(transform.position,target.transform.position)>3f) { Message("เข้าใกล้ต้นไม้อีกนิด (ระยะดูดพลัง 3 เมตร)"); return; }
        float amount=target.Absorb(Mathf.Min(10*dt,100-energy)); energy+=amount;
        if(amount>0) { if(Time.time>nextAbsorbTone){game.PlayTone(330);nextAbsorbTone=Time.time+.9f;} game.EnergyAbsorbed(amount); if(lifeFlow)lifeFlow.Feed(target); }
        else Message(energy>=99.9f?"พลังถุงมือเต็มแล้ว":(target.regenPerSecond<=0?"เหลือชีวิตขั้นต่ำแล้ว • หาต้นไม้ทดลองอื่น หรือกู้พลังจากวัตถุที่ยังไม่ยืนยัน":"พักต้นไม้ไว้ก่อน พืชจะฟื้นพลังได้เอง"));
    }
    public void Cast(int direction) {
        if(Time.time<nextCast) return;
        if(!target) return;
        nextCast=Time.time+.5f;
        bool ok=target.TryStep(direction,this,out string reason); Message(reason);
        if(!ok) game.PlayTone(170);
        if(ok) { DrawBeam(direction<0?Color.cyan:Color.green); game.PlayTone(direction<0?460:670); }
    }
    public void Respawn() {
        Controller.enabled=false; transform.position=checkpoint; Controller.enabled=true; velocity=Vector3.zero; UpdateCamera(true);
        Message("กลับมาที่จุดพักล่าสุดแล้ว — พลังและปริศนายังอยู่");
    }
    public string Feedback => Time.unscaledTime<feedbackUntil?feedback:"";
    void OnDisable() { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
}
}
