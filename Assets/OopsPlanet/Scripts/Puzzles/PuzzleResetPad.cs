using UnityEngine;
namespace OopsPlanet {
// Retry path: standing on the pad for a moment returns the target puzzle's movable pieces to their start
// (only while the puzzle is unsolved), so a misplaced or stuck piece can never softlock the player.
public class PuzzleResetPad : MonoBehaviour {
    public PuzzleObjective target;
    public Vector3 size=new Vector3(2,2,2);
    public float standSeconds=1f;
    public int resets;
    BlobPlayer player; float stood, cooldown;
    void Update() {
        if(!player) player=Object.FindFirstObjectByType<BlobPlayer>();
        if(!player || !target) return;
        cooldown-=Time.deltaTime;
        bool on=new Bounds(transform.position+Vector3.up*size.y/2,size).Contains(player.transform.position+Vector3.up*.2f);
        stood=on?stood+Time.deltaTime:0;
        if(stood>=standSeconds && cooldown<=0) {
            stood=0; cooldown=2;
            if(target.State==PuzzleState.Solved) { player.Notify("ปริศนานี้สำเร็จแล้ว"); return; }
            target.ResetPuzzle(false); resets++;
            player.Notify("รีเซ็ต \""+target.titleTh+"\" แล้ว",3);
        }
    }
}
}
