using UnityEngine;
namespace OopsPlanet {
// BLOB never speaks; it emotes. Blinks, pupils that look at the current target, a mouth that changes with mood,
// and a damped squash & stretch spring kicked by jumps, bounces and landings (fed into BlobPlayer.extraShape).
public class BlobExpression : MonoBehaviour {
    public enum Mood { Neutral, Happy, Surprised, Sad }
    public BlobPlayer player;
    public Transform[] eyes, pupils;
    public Transform mouth;
    [Tooltip("Wiggles when an absorbable is near (v2 diegetic cue).")]
    public Transform antenna;
    public float antennaWiggle;
    BlobSense sense; Quaternion antennaBase;
    public Mood mood;
    public int blinks;
    public float squash;
    Vector3[] eyeScale, pupilScale, pupilPos; Vector3 mouthScale;
    float nextBlink, blinkLeft, squashVel, moodUntil;
    void Awake() {
        eyeScale=new Vector3[eyes.Length]; for(int i=0;i<eyes.Length;i++) eyeScale[i]=eyes[i].localScale;
        pupilScale=new Vector3[pupils.Length]; pupilPos=new Vector3[pupils.Length];
        for(int i=0;i<pupils.Length;i++) { pupilScale[i]=pupils[i].localScale; pupilPos[i]=pupils[i].localPosition; }
        if(mouth) mouthScale=mouth.localScale;
        sense=player.GetComponent<BlobSense>(); if(antenna) antennaBase=antenna.localRotation;
        nextBlink=Time.time+Random.Range(1.5f,3f);
        player.Launched+=b=>Kick(b?-1f:-.5f);
        player.Rebounded+=()=>Kick(-.6f);
        player.Landed+=impact=>Kick(Mathf.Clamp(impact/12f,.15f,1f));
    }
    public void Kick(float amount) { squash=amount; squashVel=0; }
    public void SetMood(Mood m,float seconds) { mood=m; moodUntil=Time.time+seconds; }
    void LateUpdate() {
        float dt=Time.deltaTime;
        squashVel+=(-squash*140f-squashVel*11f)*dt; squash+=squashVel*dt;
        float breathe=1+.015f*Mathf.Sin(Time.time*2.2f);
        player.extraShape=new Vector3((1+.22f*squash)*breathe,(1-.3f*squash)/breathe,(1+.22f*squash)*breathe);
        if(Time.time>=nextBlink) { blinkLeft=.13f; nextBlink=Time.time+Random.Range(2.5f,5f); blinks++; }
        float lid=blinkLeft>0?.1f:1f; blinkLeft-=dt;
        for(int i=0;i<eyes.Length;i++) eyes[i].localScale=Vector3.Scale(eyeScale[i],new Vector3(1,lid,1));
        // Eyes follow the aimed target, otherwise the nearest absorbable (v2 cue), otherwise the camera direction.
        Vector3 look=player.target?player.target.AimPoint-eyes[0].position:
            sense&&sense.nearest?sense.nearest.AimPoint-eyes[0].position:(player.view?player.view.transform.forward:player.transform.forward);
        Vector3 local=player.visual.InverseTransformDirection(look.normalized);
        Vector3 offset=new Vector3(Mathf.Clamp(local.x*.05f,-.035f,.035f),Mathf.Clamp(local.y*.04f,-.025f,.025f),0);
        for(int i=0;i<pupils.Length;i++) {
            pupils[i].localScale=Vector3.Scale(pupilScale[i],new Vector3(1,lid,1));
            pupils[i].localPosition=Vector3.Lerp(pupils[i].localPosition,pupilPos[i]+offset,dt*10);
        }
        if(antenna) {
            bool near=sense && sense.nearest && sense.nearestDistance<4f;
            antennaWiggle=near?Mathf.Sin(Time.time*18)*14f:0;
            antenna.localRotation=antennaBase*Quaternion.Euler(antennaWiggle,0,antennaWiggle*.5f);
        }
        if(Time.time>moodUntil) mood=Mood.Neutral;
        if(mouth) {
            Vector3 target=mood==Mood.Happy?new Vector3(1.35f,1.4f,1):mood==Mood.Surprised?new Vector3(.55f,1.9f,1):mood==Mood.Sad?new Vector3(1f,-1f,1):new Vector3(1,.7f,1);
            mouth.localScale=Vector3.Lerp(mouth.localScale,Vector3.Scale(mouthScale,target),dt*12);
        }
    }
}
}
