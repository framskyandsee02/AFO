using UnityEngine;
namespace OopsPlanet {
// Cargo lift driven by a motor (WorldMaterial Machine). While the motor is powered the platform rises to the top
// and waits; when power runs out it comes back down. BLOB standing on it is carried along.
// waitForRider: a powered lift stays at the bottom until BLOB steps on, so it can be powered from a distance.
public class PoweredLift : MonoBehaviour {
    public WorldMaterial motor;
    public Transform platform;
    public float height=6f, speed=2f;
    public Vector3 standSize=new Vector3(2.6f,1.2f,2.6f);
    public BlobWorld world;
    public bool waitForRider;
    public int rides;
    public float Level01 => level/height;
    float level; Vector3 start; bool started, wasMoving;
    void Awake() { if(platform) start=platform.position; started=true; }
    void LateUpdate() {
        if(!platform || !started) return;
        var rider=world?world.player:null;
        bool riderOn=rider && new Bounds(platform.position+Vector3.up*standSize.y/2,standSize).Contains(rider.transform.position+Vector3.up*.1f);
        float target=motor && motor.Powered && (!waitForRider || level>0 || riderOn)?height:0;
        float before=level;
        level=Mathf.MoveTowards(level,target,speed*Time.deltaTime);
        float delta=level-before;
        bool moving=Mathf.Abs(delta)>0;
        if(moving && !wasMoving && target>0) rides++;
        wasMoving=moving;
        if(!moving) return;
        Vector3 top=platform.position;
        platform.position=start+Vector3.up*level;
        var p=world?world.player:null;
        if(p && new Bounds(top+Vector3.up*standSize.y/2,standSize).Contains(p.transform.position+Vector3.up*.1f) && p.Controller)
            p.Controller.Move(Vector3.up*delta);
        Physics.SyncTransforms();
    }
    public void ResetLift() { level=0; if(platform && started) platform.position=start; }
}
}
