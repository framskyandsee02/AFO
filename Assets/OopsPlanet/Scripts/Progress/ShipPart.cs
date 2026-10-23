using UnityEngine;
namespace OopsPlanet {
// A ship part BLOB picks up by touching it. Puzzle parts start hidden and are revealed by the puzzle's
// PuzzleReaction (showWhenSolved); hidden-spot parts are placed in the world from the start.
// Once collected (or loaded as collected / installed) the part stays hidden.
public class ShipPart : MonoBehaviour {
    public string id, nameTh, zone;
    public ShipRepair ship;
    public float pickupRadius=1.4f;
    public Transform visual;
    float baseY; bool hasBase;
    void Update() {
        if(!ship) return;
        if(ship.Has(id)) { if(visual && visual.gameObject.activeSelf) visual.gameObject.SetActive(false); return; }
        if(visual && !visual.gameObject.activeSelf) visual.gameObject.SetActive(true);
        if(visual) {
            if(!hasBase) { baseY=visual.localPosition.y; hasBase=true; }
            visual.Rotate(Vector3.up,90*Time.deltaTime,Space.World);
            var lp=visual.localPosition; lp.y=baseY+.15f*Mathf.Sin(Time.time*2.5f); visual.localPosition=lp;
        }
        var w=ship.world; if(!w || !w.player) return;
        if(Vector3.Distance(w.player.transform.position+Vector3.up*.6f,transform.position)<pickupRadius) ship.Collect(this);
    }
}
}
