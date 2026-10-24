using UnityEngine;
namespace BorrowedTime.Characters {
// Presentation model only; it has no dependency on the level or the Time Gauntlet gameplay.
public class BTCharacterModel : MonoBehaviour {
    public Animator animator;
    public SkinnedMeshRenderer face;
    public Transform rightHand;
    public LineRenderer chronoDial;
    public bool robot;
    public BTAnimationSpec[] clips=new BTAnimationSpec[0];
    public bool autoReturn=true,autoPoseFace=true;
    MaterialPropertyBlock dialProperties;
    public BTAnimationSpec GetClip(string name){foreach(var spec in clips)if(spec.name==name)return spec;return null;}
    public bool autoBlink=true;
    public Vector2 blinkInterval=new Vector2(2.8f,5.2f);
    public float blinkDuration=.18f;
    public float BlinkWeight {get;private set;}
    float nextBlink,blinkStart=-1;
    public string currentPose="Idle",currentFace="Neutral";
    void OnEnable(){nextBlink=Time.unscaledTime+Random.Range(blinkInterval.x,blinkInterval.y);blinkStart=-1;BlinkWeight=0;}
    public void BlinkNow(){blinkStart=Time.unscaledTime;}
    void UpdateBlink(){
        float now=Time.unscaledTime;
        if(autoBlink && blinkStart<0 && now>=nextBlink && currentFace!="Blink" && currentFace!="Sleep")BlinkNow();
        BlinkWeight=0;
        if(blinkStart<0)return;
        float phase=(now-blinkStart)/Mathf.Max(.06f,blinkDuration);
        if(phase>=1){blinkStart=-1;nextBlink=now+Random.Range(blinkInterval.x,blinkInterval.y);return;}
        BlinkWeight=phase<.4f?Mathf.SmoothStep(0,100,phase/.4f):Mathf.SmoothStep(100,0,(phase-.4f)/.6f);
    }
    public void PlayPose(string pose){
        if(!animator || !animator.runtimeAnimatorController)return;
        if(!animator.HasState(0,Animator.StringToHash(pose))){Debug.LogWarning("Unknown character animation: "+pose,this);return;}
        currentPose=pose;var spec=GetClip(pose);if(spec!=null && autoPoseFace && !string.IsNullOrEmpty(spec.face))SetFace(spec.face);
        if(!robot && (pose=="Idle"||pose=="Walk"||pose=="Run"))animator.SetFloat("Speed",pose=="Run"?2:pose=="Walk"?1:0);
        animator.CrossFadeInFixedTime(pose,.18f,0);
    }
    public void SetLocomotionSpeed(float speed){
        if(robot || !animator || !animator.runtimeAnimatorController)return;
        string pose=Mathf.Abs(speed)>1.5f?"Run":Mathf.Abs(speed)>.08f?"Walk":"Idle";
        if(currentPose!=pose)PlayPose(pose);animator.SetFloat("Speed",Mathf.Abs(speed));
    }
    public void SetLocomotion(Vector2 direction,bool running=false,bool crouching=false){
        string pose=direction.sqrMagnitude<.01f?(crouching?"CrouchIdle":"Idle"):crouching?"CrouchWalk":running?"Run":Mathf.Abs(direction.x)>Mathf.Abs(direction.y)?(direction.x<0?"StrafeLeft":"StrafeRight"):direction.y<0?"WalkBackward":"Walk";
        if(currentPose!=pose)PlayPose(pose);
    }
    public void SetFace(string expression){
        currentFace=expression;ApplyFace();
    }
    void ApplyFace(){
        if(!face || !face.sharedMesh)return;string expression=currentFace;
        for(int i=0;i<face.sharedMesh.blendShapeCount;i++){
            string name=face.sharedMesh.GetBlendShapeName(i);
            bool selected=name==expression||name.EndsWith("."+expression);
            bool blink=name=="Blink"||name.EndsWith(".Blink");
            float weight=selected?100:0;
            if(expression!="Blink" && expression!="Sleep")weight=blink?BlinkWeight:weight*(1-BlinkWeight/100);
            face.SetBlendShapeWeight(i,weight);
        }
    }
    void LateUpdate(){
        var spec=GetClip(currentPose);
        if(autoReturn && spec!=null && !spec.loop && !string.IsNullOrEmpty(spec.returnTo) && animator && animator.enabled && !animator.IsInTransition(0)){
            var state=animator.GetCurrentAnimatorStateInfo(0);if(state.IsName(currentPose) && state.normalizedTime>=1){PlayPose(spec.returnTo);spec=GetClip(currentPose);}
        }
        UpdateBlink();RefreshPresentation();
    }
    public void RefreshPresentation(){ApplyFace();UpdateDial();}
    void UpdateDial(){
        var spec=GetClip(currentPose);
        if(!chronoDial)return;bool visible=spec!=null && !string.IsNullOrEmpty(spec.power);chronoDial.enabled=visible;
        if(!visible||!rightHand)return;chronoDial.transform.position=rightHand.position+new Vector3(0,.04f,.075f);
        Color color=spec.power=="green"?new Color(.13f,1,.35f):spec.power=="purple"?new Color(.73f,.28f,1):spec.power=="gold"?new Color(1,.68f,.12f):new Color(.12f,.70f,1);
        if(dialProperties==null)dialProperties=new MaterialPropertyBlock();chronoDial.GetPropertyBlock(dialProperties);dialProperties.SetColor("_BaseColor",color);dialProperties.SetColor("_EmissionColor",color*2.5f);chronoDial.SetPropertyBlock(dialProperties);
        var camera=Camera.main;if(camera)chronoDial.transform.rotation=Quaternion.LookRotation(camera.transform.position-chronoDial.transform.position);
        chronoDial.transform.Rotate(0,0,Time.time*25,Space.Self);
    }
}
}
