using UnityEngine;
using UnityEngine.InputSystem;
namespace OopsPlanet {
// OOPS! PLANET powers. Explicit values keep the integers already serialized in scenes/prefabs (legacy 2-4 were retired in P03).
public enum BlobProperty { None=0, Magnet=1, Pressure=5, Bounce=6 }
[RequireComponent(typeof(CharacterController))]
public class BlobPlayer : MonoBehaviour {
    public BlobWorld world;
    public Camera view;
    public Transform visual;
    public BlobPowers powers;
    public float speed=5.5f, jumpSpeed=7.6f, gravity=20f, absorbRange=8f, absorbSeconds=.8f, aimDegrees=9f;
    public float morphSeconds=.4f;
    // Two active properties at most: the newest absorb becomes primary (F), the previous primary moves to secondary (G).
    public BlobProperty property, secondary;
    public BlobInteractable target;
    public string targetStatus="AIM AT A PROPERTY SOURCE";
    public float absorbProgress, bounceCharge;
    public bool bounceAirborne;
    public int rebounds;
    public bool paused, completed;
    public bool testMode;
    public Vector2 testMove;
    public bool testAbsorb, testAction, testSecondary, testPush, testJump;
    public Transform testAim;
    public Vector3 velocity, externalVelocity;
    public float yaw, pitch=19f;
    public Vector3 moveWorld;
    CharacterController cc;
    bool wasJump, absorbLatched;
    bool previousBackground;
    float coyote, jumpBuffer, lastAirTime;
    public float lookSensitivity=.13f;
    BlobInteractable heldTarget;
    Renderer[] visualRenderers;
    Vector3 baseVisualScale;
    MaterialPropertyBlock mpb;
    LineRenderer beam;
    AudioSource audioSource;
    AudioClip[] tones;
    float feedbackUntil;
    public string feedback="";
    // Raised after a successful absorb: the source absorbed and the property BLOB gained.
    public event System.Action<BlobInteractable,BlobProperty> Absorbed;
    public GameObject targetMarker;
    TextMesh markerText;
    BlobInteractable lastCuedTarget;
    bool hasPowerBand;
    public bool Grounded => cc && cc.isGrounded;
    public CharacterController Controller => cc;
    public bool Has(BlobProperty p) => p!=BlobProperty.None && (property==p || secondary==p);
    public static Color PropertyColor(BlobProperty p) {
        switch(p) {
            case BlobProperty.Magnet:return new Color(.95f,.22f,.22f);
            case BlobProperty.Pressure:return new Color(.25f,.55f,1f);
            case BlobProperty.Bounce:return new Color(1f,.42f,.75f);
            default:return new Color(.63f,.73f,.78f);
        }
    }
    public string NameTh(BlobProperty p) {
        var d=powers?powers.Def(p):null;
        return d?d.nameTh:p==BlobProperty.None?"ว่าง":p.ToString();
    }
    void Awake() {
        previousBackground=Application.runInBackground; Application.runInBackground=true;
        cc=GetComponent<CharacterController>();
        cc.height=1.3f; cc.radius=.36f; cc.center=Vector3.up*.65f;
        cc.stepOffset=.22f; cc.slopeLimit=48; cc.skinWidth=.035f;
        if(!powers) powers=GetComponent<BlobPowers>();
        visualRenderers=visual.GetComponentsInChildren<Renderer>(true);
        foreach(var r in visualRenderers) if(r.name.Contains("PowerBand")) hasPowerBand=true;
        targetMarker=new GameObject("Absorb target marker");
        markerText=targetMarker.AddComponent<TextMesh>();
        markerText.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        markerText.GetComponent<Renderer>().sharedMaterial=markerText.font.material;
        markerText.fontSize=64; markerText.characterSize=.06f; markerText.anchor=TextAnchor.MiddleCenter; markerText.fontStyle=FontStyle.Bold;
        targetMarker.SetActive(false);
        baseVisualScale=visual.localScale; mpb=new MaterialPropertyBlock();
        beam=GetComponent<LineRenderer>();
        audioSource=GetComponent<AudioSource>();
        tones=new AudioClip[5];
        for(int i=0;i<tones.Length;i++) {
            int count=6615; float[] data=new float[count];
            for(int j=0;j<count;j++) { float t=j/44100f; data[j]=Mathf.Sin(t*(330+i*110)*Mathf.PI*2)*.15f*Mathf.Sin(j/(float)count*Mathf.PI); }
            tones[i]=AudioClip.Create("BLOB cue "+i,count,1,44100,false); tones[i].SetData(data,0);
        }
        ClearProperties();
        Teleport(world.spawn);
    }
    void OnEnable() { if(mpb==null)mpb=new MaterialPropertyBlock(); }
    void OnDisable() { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
    void OnDestroy() { Application.runInBackground=previousBackground; if(tones!=null) foreach(var t in tones) if(t) Destroy(t); if(targetMarker) Destroy(targetMarker); }
    public void Notify(string text,int tone=0) {
        feedback=text; feedbackUntil=Time.time+3;
        if(audioSource && tones!=null) audioSource.PlayOneShot(tones[Mathf.Clamp(tone,0,4)],.7f);
    }
    // Absorb result: newest is primary, previous primary becomes secondary, an older secondary is dropped (max 2).
    public bool AddProperty(BlobProperty next) {
        if(next==BlobProperty.None) return false;
        if(property==next) { Notify("มีพลัง"+NameTh(next)+"อยู่แล้ว"); return true; }
        if(secondary==next) { SwapProperties(); return true; }
        BlobProperty dropped=secondary;
        secondary=property; property=next;
        OnPropertiesChanged();
        Notify("ดูดซับ"+NameTh(next)+"แล้ว"+(dropped!=BlobProperty.None?" (ทิ้ง"+NameTh(dropped)+")":""),1+(int)next%4);
        return true;
    }
    public void SwapProperties() {
        if(secondary==BlobProperty.None) { Notify("ต้องมี 2 พลังจึงจะสลับได้"); return; }
        (property,secondary)=(secondary,property);
        OnPropertiesChanged();
        Notify("สลับพลัง: หลัก = "+NameTh(property)+", เสริม = "+NameTh(secondary),3);
    }
    public void ClearProperties() { property=BlobProperty.None; secondary=BlobProperty.None; OnPropertiesChanged(); }
    // Test / tool hook: set both slots directly.
    public void SetProperties(BlobProperty primary,BlobProperty second) { property=primary; secondary=second==primary?BlobProperty.None:second; OnPropertiesChanged(); }
    void OnPropertiesChanged() {
        bounceCharge=0;
        if(powers && !Has(BlobProperty.Magnet)) powers.Release();
    }
    public void Teleport(Vector3 pos) {
        cc.enabled=false; transform.position=pos; cc.enabled=true;
        velocity=Vector3.zero; externalVelocity=Vector3.zero; jumpBuffer=0; bounceCharge=0; wasJump=false;
        bounceAirborne=false; rebounds=0; absorbProgress=0; heldTarget=null;
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
        UpdateJumpAndBounce(jump,jumpUp);
        bool groundedBefore=Grounded; float fallSpeed=-velocity.y;
        CollisionFlags flags=cc.Move((moveWorld*speed+externalVelocity+Vector3.up*velocity.y)*Time.deltaTime);
        if((flags&CollisionFlags.Above)!=0 && velocity.y>0) velocity.y=0;
        if(Grounded && !groundedBefore) OnLanded(fallSpeed,jump);
        externalVelocity=Vector3.MoveTowards(externalVelocity,Vector3.zero,(Grounded?40:12)*Time.deltaTime);
        if(Grounded && lastAirTime>.35f) { lastAirTime=0; if(audioSource)audioSource.PlayOneShot(tones[0],.25f); }
        if(!Grounded) lastAirTime+=Time.deltaTime;
        if(moveWorld.sqrMagnitude>.01f) visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(moveWorld),Time.deltaTime*12);
        if(transform.position.y<-8) world.Respawn();
        UpdateAbsorb(absorb);
        UpdateMorph();
        wasJump=jump;
        if(Time.time>feedbackUntil) feedback="";
    }
    // Normal jump, or with Bounce: hold SPACE on the ground to charge, release to launch; landings rebound
    // with a decaying factor (<1) and a hard cap on rebounds, so bouncing always settles.
    // Called on the frame Move() first reports ground contact, with the downward speed just before landing.
    void OnLanded(float impact,bool jump) {
        if(!bounceAirborne) return;
        var d=Has(BlobProperty.Bounce)&&powers?powers.Def(BlobProperty.Bounce):null;
        if(d && rebounds<d.maxRebounds && impact>=d.reboundMinImpact && !jump) {
            velocity.y=impact*d.rebound; rebounds++; coyote=0;
            if(audioSource) audioSource.PlayOneShot(tones[2],.35f);
            return;
        }
        bounceAirborne=false; rebounds=0;
    }
    void UpdateJumpAndBounce(bool jump,bool jumpUp) {
        var d=Has(BlobProperty.Bounce)&&powers?powers.Def(BlobProperty.Bounce):null;
        if(d) {
            if(jump && Grounded) bounceCharge=Mathf.Clamp01(bounceCharge+Time.deltaTime/d.chargeSeconds);
            if(jumpUp && coyote>0) {
                velocity.y=Mathf.Lerp(d.minLaunch,d.maxLaunch,bounceCharge); coyote=0; jumpBuffer=0;
                bounceAirborne=true; rebounds=0; bounceCharge=0;
                Notify("เด้ง!",2);
            } else if(!Grounded && !jump) bounceCharge=0;
        } else if(jumpBuffer>0 && coyote>0) {
            velocity.y=jumpSpeed; jumpBuffer=0; coyote=0;
        }
        if(Grounded && velocity.y<0) velocity.y=-2;
        velocity.y-=gravity*Time.deltaTime;
    }
    public const string Absorbable="HOLD E TO ABSORB", NotAbsorbable="CAN'T ABSORB THIS";
    public string EvaluateTarget(BlobInteractable candidate,Vector3 aimOrigin,Vector3 aimDirection) {
        if(!candidate || !candidate.Available) return NotAbsorbable;
        Vector3 origin=transform.position+Vector3.up*.8f, point=candidate.AimPoint;
        if(Vector3.Distance(origin,point)>absorbRange) return "OUT OF RANGE";
        if(Vector3.Angle(aimDirection,point-aimOrigin)>aimDegrees) return "AIM AT TARGET";
        if(Physics.Linecast(origin,point,out RaycastHit hit,~(1<<2),QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<BlobInteractable>()!=candidate) return "BLOCKED";
        return Absorbable;
    }
    void UpdateAbsorb(bool held) {
        if(!view) return;
        Vector3 rayOrigin=view.transform.position, rayDir=view.transform.forward;
        target=null; float best=aimDegrees;
        foreach(var source in world.sources) {
            if(!source || !source.Available) continue;
            float angle=Vector3.Angle(rayDir,source.AimPoint-rayOrigin);
            if(angle<best) { best=angle; target=source; }
        }
        foreach(var s in world.sources) if(s) s.Highlight(s==target);
        if(target) targetStatus=EvaluateTarget(target,rayOrigin,rayDir);
        // Aiming straight at a non-absorbable object (not the floor) says so explicitly.
        else if(Physics.Raycast(rayOrigin,rayDir,out RaycastHit hit,absorbRange+6,~(1<<2),QueryTriggerInteraction.Ignore) && hit.normal.y<.7f && !hit.collider.GetComponentInParent<BlobInteractable>())
            targetStatus=NotAbsorbable;
        else targetStatus="NO ABSORBABLE PROPERTY";
        UpdateTargetCue();
        if(!held)absorbLatched=false;
        if(absorbLatched) { if(beam)beam.enabled=false; return; }
        if(!held || target==null || targetStatus!=Absorbable) { absorbProgress=0; heldTarget=null; }
        else {
            if(heldTarget!=target) { heldTarget=target; absorbProgress=0; }
            absorbProgress+=Time.deltaTime/absorbSeconds;
            if(absorbProgress>=1) {
                var source=target;
                if(AddProperty(source.property)) {
                    world.RecordProperty(source.property); absorbLatched=true;
                    source.Absorb(); Absorbed?.Invoke(source,source.property);
                    if(!source.Available) { target=null; UpdateTargetCue(); }
                }
                absorbProgress=0; heldTarget=null;
            }
        }
        if(beam) {
            beam.enabled=held && absorbProgress>0 && target;
            if(beam.enabled) { beam.SetPosition(0,transform.position+Vector3.up*.65f); beam.SetPosition(1,target.AimPoint); beam.startColor=beam.endColor=PropertyColor(target.property); }
        }
    }
    // Aim feedback: floating [E] marker over the targeted source plus a short cue when a new valid target is acquired.
    void UpdateTargetCue() {
        bool valid=target && targetStatus==Absorbable;
        if(targetMarker) {
            targetMarker.SetActive(target);
            if(target) {
                var b=target.indicator?target.indicator.bounds:new Bounds(target.AimPoint,Vector3.one);
                targetMarker.transform.position=b.center+Vector3.up*(b.extents.y+.45f);
                targetMarker.transform.rotation=view.transform.rotation;
                markerText.text=valid?"[ E ]":"[ - ]";
                markerText.color=valid?PropertyColor(target.property):new Color(.6f,.6f,.6f);
            }
        }
        if(valid && target!=lastCuedTarget && audioSource && !testMode) audioSource.PlayOneShot(tones[0],.3f);
        lastCuedTarget=valid?target:null;
    }
    // Shape + parts show the powers without relying on colour: Cue_* parts appear for each held power,
    // Bounce squashes while charging, Pressure puffs up while its blast recharges.
    void UpdateMorph() {
        float t=1-Mathf.Exp(-Time.deltaTime*5/Mathf.Max(.1f,morphSeconds));
        Vector3 shape=Vector3.one;
        if(Has(BlobProperty.Bounce)) shape=new Vector3(1+.35f*bounceCharge,1-.5f*bounceCharge,1+.35f*bounceCharge);
        if(property==BlobProperty.Pressure && powers) shape*=1.04f+.08f*powers.PressureCooldown01;
        visual.localScale=Vector3.Lerp(visual.localScale,Vector3.Scale(baseVisualScale,shape),t);
        Color c=PropertyColor(property);
        foreach(var r in visualRenderers) {
            string n=r.name;
            if(n.Contains("Cue_")) {
                var cue=n.Contains("Magnet")?BlobProperty.Magnet:n.Contains("Pressure")?BlobProperty.Pressure:n.Contains("Bounce")?BlobProperty.Bounce:BlobProperty.None;
                bool on=Has(cue);
                if(r.gameObject.activeSelf!=on) r.gameObject.SetActive(on);
                if(on) { r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",PropertyColor(cue)); r.SetPropertyBlock(mpb); }
                continue;
            }
            bool band=n.Contains("PowerBand"), body=n.Contains("Body")||n.Contains("Drip");
            if(!band&&!body) continue;
            // The band carries the primary property colour; the body keeps most of BLOB's own colour.
            Color col=band||!hasPowerBand?c:Color.Lerp(PropertyColor(BlobProperty.None),c,.3f);
            r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",col); r.SetPropertyBlock(mpb);
        }
    }
    void LateUpdate() {
        if(!view || !world) return;
        if(world.overview) { view.transform.position=world.overviewPosition; view.transform.LookAt(world.overviewTarget); return; }
        Vector3 pivot=transform.position+Vector3.up*1.4f;
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
