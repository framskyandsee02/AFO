using UnityEngine;
namespace OopsPlanet {
// Met once BLOB has stood inside the area (e.g. the far canal bank, the boiler-room walkway, outside the crash pit).
// Latches until the puzzle is reset, so stepping back out does not un-solve it.
public class AreaReachedCondition : MonoBehaviour, IPuzzleCondition {
    public BlobWorld world;
    public Vector3 size=new Vector3(4,3,4);
    public bool reached;
    public bool IsMet => reached;
    public float Progress01 => reached?1:0;
    public Bounds Area => new Bounds(transform.position,size);
    void Update() {
        if(reached || !world || !world.player) return;
        if(Area.Contains(world.player.transform.position+Vector3.up*.3f)) reached=true;
    }
    public void ResetCondition() { reached=false; }
    void OnDrawGizmosSelected() { Gizmos.color=Color.green; Gizmos.DrawWireCube(transform.position,size); }
}
}
