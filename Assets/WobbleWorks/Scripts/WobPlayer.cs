using UnityEngine;
using UnityEngine.InputSystem;
namespace WobbleWorks {
public enum WobProperty { None, Magnet, Spring, Gas, Liquid }
[RequireComponent(typeof(CharacterController))]
public class WobPlayer : MonoBehaviour {
    public WobWorld world;
    public Camera view;
    public Transform visual;
    public float speed=5.5f, jumpSpeed=7.6f, gravity=20f, absorbRange=8f, absorbSeconds=.8f, aimDegrees=9f;
    public float springMin=9f, springMax=18f, springChargeSeconds=1f;
    public float morphSeconds=.4f, magnetRange=8f, magnetAcceleration=18f;
    public WobProperty property;
    public WobInteractable target;
    public string targetStatus="AIM AT A PROPERTY SOURCE";
    public float absorbProgress, springCharge;
    public bool flattened, paused, completed;
    public bool testMode;
    public Vector2 testMove;
    public bool testAbsorb, testAction, testJump;
    public Transform testAim;
    public Vector3 velocity;
    public float yaw, pitch=19f;
    public Vector3 moveWorld;
    CharacterController cc;
    bool wasJump, absorbLatched;
    bool previousBackground;
    float coyote, jumpBuffer, lastAirTime, lookSensitivity=.13f;
    WobInteractable heldTarget;
    Renderer[] visualRenderers;
    Vector3 baseVisualScale;
    MaterialPropertyBlock mpb;
    LineRenderer beam;
    AudioSource audioSource;
    AudioClip[] tones;
    float feedbackUntil;
    public string feedback="";
    public bool Grounded => cc && cc.isGrounded;
    public CharacterController Controller => cc;
    public static Color PropertyColor(WobProperty p) {
        switch(p) {
            case WobProperty.Magnet:return new Color(.95f,.22f,.22f);
            case WobProperty.Spring:return new Color(1f,.76f,.12f);
            case WobProperty.Gas:return new Color(.69f,.46f,.98f);
            case WobProperty.Liquid:return new Color(.12f,.83f,.94f);
            default:return new Color(.63f,.73f,.78f);
        }
    }
    void Awake() {
        previousBackground=Application.runInBackground; Application.runInBackground=true;
        cc=GetComponent<CharacterController>();
        cc.height=1.3f; cc.radius=.36f; cc.center=Vector3.up*.65f;
        cc.stepOffset=.22f; cc.slopeLimit=48; cc.skinWidth=.035f;
        visualRenderers=visual.GetComponentsInChildren<Renderer>();
        baseVisualScale=visual.localScale; mpb=new MaterialPropertyBlock();
        beam=GetComponent<LineRenderer>();
        audioSource=GetComponent<AudioSource>();
        tones=new AudioClip[5];
        for(int i=0;i<tones.Length;i++) {
            int count=6615; float[] data=new float[count];
            for(int j=0;j<count;j++) { float t=j/44100f; data[j]=Mathf.Sin(t*(330+i*110)*Mathf.PI*2)*.15f*Mathf.Sin(j/(float)count*Mathf.PI); }
            tones[i]=AudioClip.Create("WOB cue "+i,count,1,44100,false); tones[i].SetData(data,0);
        }
        SetProperty(WobProperty.None,false);
        Teleport(world.spawn);
    }
    void OnEnable() { if(mpb==null)mpb=new MaterialPropertyBlock(); }
    void OnDisable() { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
    void OnDestroy() { Application.runInBackground=previousBackground; if(tones!=null) foreach(var t in tones) if(t) Destroy(t); }
    public void Notify(string text,int tone=0) {
        feedback=text; feedbackUntil=Time.time+3;
        if(audioSource && tones!=null) audioSource.PlayOneShot(tones[Mathf.Clamp(tone,0,4)],.7f);
    }
    public bool SetProperty(WobProperty next,bool sound=true) {
        if(property==WobProperty.Liquid && flattened && next!=WobProperty.Liquid && !CanStand()) {
            targetStatus="NO CLEARANCE TO CHANGE FORM"; return false;
        }
        property=next; springCharge=0; wasJump=false;
        if(next!=WobProperty.Liquid) SetFlat(false);
        if(sound) Notify(next.ToString().ToUpper()+" ABSORBED",1+(int)next%4);
        return true;
    }
    public void Teleport(Vector3 pos) {
        cc.enabled=false; transform.position=pos; cc.enabled=true;
        velocity=Vector3.zero; jumpBuffer=0; springCharge=0; wasJump=false;
        absorbProgress=0; heldTarget=null;
        Physics.SyncTransforms();
    }
    public void AimAt(Transform point) {
        Vector3 d=point.position-(transform.position+Vector3.up*1.4f);
        yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
        // Camera position is recalculated in LateUpdate. Test aim uses camera.LookAt below.
    }
    void Update() {
        var k=Keyboard.current; var m=Mouse.current;
        if(!testMode && k!=null && k.escapeKey.wasPressedThisFrame) {
            paused=!paused; Cursor.lockState=paused?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=paused;
        }
        if(!testMode && !paused && !completed && m!=null && m.leftButton.wasPressedThisFrame) {
            Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false;
        }
        if(paused || completed) { absorbProgress=0; heldTarget=null; if(beam)beam.enabled=false; return; }
        if(!testMode && k!=null) {
            if(k.rKey.wasPressedThisFrame) world.Respawn();
            if(k.tKey.wasPressedThisFrame) world.Restart();
            if(k.tabKey.wasPressedThisFrame) world.overview=!world.overview;
        }
        Vector2 input=testMode?testMove:Vector2.zero;
        bool jump=testMode?testJump:k!=null && k.spaceKey.isPressed;
        bool jumpDown=jump&&!wasJump;
        bool jumpUp=!jump&&wasJump;
        bool action=testMode?testAction:k!=null&&k.fKey.isPressed;
        bool absorb=testMode?testAbsorb:k!=null&&k.eKey.isPressed;
        if(!testMode && k!=null) {
            input=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
            if(m!=null && (Cursor.lockState==CursorLockMode.Locked||m.rightButton.isPressed)) {
                Vector2 delta=m.delta.ReadValue(); yaw+=delta.x*lookSensitivity; pitch=Mathf.Clamp(pitch-delta.y*lookSensitivity,-35,65);
            }
        }
        input=Vector2.ClampMagnitude(input,1);
        moveWorld=testMode?new Vector3(input.x,0,input.y):Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
        if(Grounded) coyote=.12f; else coyote-=Time.deltaTime;
        jumpBuffer=jumpDown?.12f:Mathf.Max(0,jumpBuffer-Time.deltaTime);
        if(property==WobProperty.Liquid) SetFlat(action || (flattened&&!CanStand()));
        else if(flattened && CanStand()) SetFlat(false);
        if(property==WobProperty.Spring) {
            if(jump && Grounded) springCharge=Mathf.Clamp01(springCharge+Time.deltaTime/springChargeSeconds);
            if(jumpUp && coyote>0 && springCharge>0) {
                velocity.y=Mathf.Lerp(springMin,springMax,springCharge); coyote=0; Notify("SPRING RELEASE",2); springCharge=0;
            } else if(!Grounded && !jump) springCharge=0;
        } else if(jumpBuffer>0 && coyote>0 && !flattened) {
            velocity.y=jumpSpeed; jumpBuffer=0; coyote=0;
        }
        float g=property==WobProperty.Gas?4f:gravity;
        if(Grounded && velocity.y<0) velocity.y=-2;
        if(property==WobProperty.Gas && world.InGasVent(transform.position) && jump)
            velocity.y=Mathf.MoveTowards(velocity.y,3.5f,Time.deltaTime*12);
        else velocity.y-=g*Time.deltaTime;
        if(property==WobProperty.Gas) velocity.y=Mathf.Max(velocity.y,-2.5f);
        float actualSpeed=flattened?3.5f:speed;
        CollisionFlags flags=cc.Move((moveWorld*actualSpeed+Vector3.up*velocity.y)*Time.deltaTime);
        if((flags&CollisionFlags.Above)!=0 && velocity.y>0) velocity.y=0;
        if(Grounded && lastAirTime>.35f) { lastAirTime=0; if(audioSource)audioSource.PlayOneShot(tones[0],.25f); }
        if(!Grounded) lastAirTime+=Time.deltaTime;
        if(moveWorld.sqrMagnitude>.01f) visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(moveWorld),Time.deltaTime*12);
        if(transform.position.y<-8) world.Respawn();
        UpdateAbsorb(absorb);
        UpdateMorph();
        wasJump=jump;
        if(Time.time>feedbackUntil) feedback="";
    }
    void FixedUpdate() {
        if(!world || paused||completed) return;
        bool action=testMode?testAction:Keyboard.current!=null&&Keyboard.current.fKey.isPressed;
        if(property!=WobProperty.Magnet || !action) return;
        var body=world.crate;
        Vector3 origin=transform.position+Vector3.up*.65f;
        Vector3 delta=body.worldCenterOfMass-origin;
        if(delta.magnitude>magnetRange) return;
        if(Physics.Linecast(origin,body.worldCenterOfMass,out RaycastHit hit,~(1<<2),QueryTriggerInteraction.Ignore) && hit.rigidbody!=body) return;
        Vector3 desired=origin+new Vector3(delta.x,0,delta.z).normalized*1.8f;
        desired.y=body.worldCenterOfMass.y;
        Vector3 force=(desired-body.worldCenterOfMass)*magnetAcceleration-body.linearVelocity*7f;
        force.y=0; body.AddForce(Vector3.ClampMagnitude(force,65),ForceMode.Acceleration);
    }
    public string EvaluateTarget(WobInteractable candidate,Vector3 aimOrigin,Vector3 aimDirection) {
        Vector3 origin=transform.position+Vector3.up*.8f;
        if(Vector3.Distance(origin,candidate.transform.position)>absorbRange) return "OUT OF RANGE";
        if(Vector3.Angle(aimDirection,candidate.transform.position-aimOrigin)>aimDegrees) return "AIM AT TARGET";
        if(Physics.Linecast(origin,candidate.transform.position,out RaycastHit hit,~(1<<2),QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<WobInteractable>()!=candidate) return "BLOCKED";
        return "HOLD E TO ABSORB";
    }
    void UpdateAbsorb(bool held) {
        if(!view) return;
        Vector3 rayOrigin=view.transform.position, rayDir=view.transform.forward;
        target=null; float best=aimDegrees;
        foreach(var source in world.sources) {
            float angle=Vector3.Angle(rayDir,source.transform.position-rayOrigin);
            if(angle<best) { best=angle; target=source; }
        }
        foreach(var s in world.sources) s.Highlight(s==target);
        targetStatus=target?EvaluateTarget(target,rayOrigin,rayDir):"NO ABSORBABLE PROPERTY";
        if(!held)absorbLatched=false;
        if(absorbLatched) { if(beam)beam.enabled=false; return; }
        if(!held || target==null || targetStatus!="HOLD E TO ABSORB") { absorbProgress=0; heldTarget=null; }
        else {
            if(heldTarget!=target) { heldTarget=target; absorbProgress=0; }
            absorbProgress+=Time.deltaTime/absorbSeconds;
            if(absorbProgress>=1) {
                if(SetProperty(target.property)) { world.RecordProperty(property); absorbLatched=true; }
                absorbProgress=0; heldTarget=null;
            }
        }
        if(beam) {
            beam.enabled=held && absorbProgress>0;
            if(beam.enabled) { beam.SetPosition(0,transform.position+Vector3.up*.65f); beam.SetPosition(1,target.transform.position); beam.startColor=beam.endColor=PropertyColor(target.property); }
        }
    }
    public bool CanStand() {
        var hits=Physics.OverlapCapsule(transform.position+Vector3.up*.4f,transform.position+Vector3.up*1.02f,.32f,~(1<<2),QueryTriggerInteraction.Ignore);
        return hits.Length==0;
    }
    void SetFlat(bool on) {
        if(flattened==on) return; flattened=on;
        cc.height=on?.32f:1.3f; cc.center=Vector3.up*(on?.16f:.65f); cc.stepOffset=on?.08f:.22f;
        cc.radius=on?.15f:.36f;
    }
    void UpdateMorph() {
        float t=1-Mathf.Exp(-Time.deltaTime*5/Mathf.Max(.1f,morphSeconds));
        Vector3 shape=Vector3.one;
        switch(property) {
            case WobProperty.Spring:shape=new Vector3(1+.4f*springCharge,1-.65f*springCharge,1+.4f*springCharge);break;
            case WobProperty.Gas:shape=Vector3.one*(1.15f+.05f*Mathf.Sin(Time.time*3));break;
            case WobProperty.Liquid:shape=flattened?new Vector3(1.1f,.22f,1.3f):new Vector3(1.1f,.8f,1.1f);break;
        }
        visual.localScale=Vector3.Lerp(visual.localScale,Vector3.Scale(baseVisualScale,shape),t);
        Color c=PropertyColor(property);
        foreach(var r in visualRenderers) {
            if(!r.name.Contains("Body")&&!r.name.Contains("Orbit")) continue;
            r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",c); r.SetPropertyBlock(mpb);
            if(r.name.Contains("Orbit"))r.transform.localRotation=Quaternion.AngleAxis(Time.time*45,Vector3.up);
        }
    }
    void LateUpdate() {
        if(!view || !world) return;
        if(world.overview) { view.transform.position=new Vector3(54,68,32); view.transform.LookAt(new Vector3(0,3,57)); return; }
        Vector3 pivot=transform.position+Vector3.up*(flattened?.24f:1.4f);
        Quaternion rot=Quaternion.Euler(pitch,yaw,0);
        Vector3 wanted=pivot-rot*Vector3.forward*5.8f+rot*Vector3.right*.65f;
        Vector3 delta=wanted-pivot;
        if(Physics.SphereCast(pivot,.18f,delta.normalized,out RaycastHit hit,delta.magnitude,~(1<<2),QueryTriggerInteraction.Ignore))
            wanted=pivot+delta.normalized*Mathf.Max(.3f,hit.distance-.15f);
        view.transform.position=wanted; view.transform.rotation=rot;
        if(testMode && testAim) view.transform.LookAt(testAim.position);
    }
    public void Resume() { paused=false; Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
}
}
