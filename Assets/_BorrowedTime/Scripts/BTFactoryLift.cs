using UnityEngine;
namespace BorrowedTime {
// A real moving platform carries the same CharacterController used for walking.
public class BTFactoryLift : MonoBehaviour {
    public BTFactoryGame game;
    public Transform platform;
    public float lowHeight,highHeight,travelSpeed=1.8f;
    public bool moving;
    public float targetHeight;
    public float Top => platform.position.y+.25f;
    public bool AtHigh => Mathf.Abs(Top-highHeight)<.04f;
    public bool PlayerOnPlatform() {
        Vector3 p=game.player.transform.position-platform.position;
        return Mathf.Abs(p.x)<1.8f && Mathf.Abs(p.z)<1.8f && p.y>.18f && p.y<.7f;
    }
    public void Toggle() {
        if(moving) return;
        targetHeight=AtHigh?lowHeight:highHeight; moving=true;
    }
    public void ResetLift() {
        moving=false; targetHeight=lowHeight;
        platform.position=new Vector3(platform.position.x,lowHeight-.25f,platform.position.z);
    }
    void Update() {
        if(!moving || game.player.paused || game.completed) return;
        float next=Mathf.MoveTowards(Top,targetHeight,travelSpeed*Time.deltaTime),dy=next-Top;
        bool rider=PlayerOnPlatform();
        if(rider) game.player.Controller.Move(Vector3.up*dy);
        platform.position+=Vector3.up*dy;
        if(Mathf.Abs(next-targetHeight)<.001f) moving=false;
    }
}
}
