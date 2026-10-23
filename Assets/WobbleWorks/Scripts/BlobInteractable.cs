using UnityEngine;
namespace OopsPlanet {
// GasVent / Pipe / Reset belonged to the retired Wobble greybox; kept only so serialized indices stay stable.
public enum BlobInteraction { Source, Checkpoint, GasVent, Pipe, Exit, Reset, Area }
// What happens to an absorbable Source after BLOB absorbs it. Deplete (v2): the object stays in the scene but looks
// drained and cannot be absorbed until it regenerates.
public enum AbsorbResult { Remains, Disable, Remnant, Deplete }
public class BlobInteractable : MonoBehaviour {
    public BlobInteraction kind;
    public BlobProperty property;
    public Vector3 size=Vector3.one;
    public Vector3 respawn;
    public int index;
    public string label;
    public Renderer indicator;
    [Header("Absorbable source")]
    public AbsorbResult onAbsorbed=AbsorbResult.Remains;
    public GameObject fullVisual, remnantVisual;
    public bool restoreOnRespawn=true;
    [Tooltip("v2: puzzle-critical sources come back by themselves after this many seconds (0 = only on respawn/restart).")]
    public float regenSeconds;
    public float regenAt;
    public bool consumed;
    [Tooltip("Thai power name; shown only in sense mode (AbsorbableVisual).")]
    public GameObject powerLabel;
    public Vector3 AimPoint => indicator && indicator.gameObject.activeInHierarchy ? indicator.bounds.center : transform.position;
    public bool Available => kind==BlobInteraction.Source && isActiveAndEnabled && !consumed && property!=BlobProperty.None;
    public bool Contains(Vector3 point) => new Bounds(transform.position,size).Contains(point);
    public void Highlight(bool selected) {
        if(indicator) indicator.transform.localScale=Vector3.one*(selected?1.13f:1);
    }
    public void Absorb() {
        if(onAbsorbed==AbsorbResult.Remains) return;
        consumed=true; regenAt=Time.time+regenSeconds;
        switch(onAbsorbed) {
            case AbsorbResult.Disable: gameObject.SetActive(false); break;
            case AbsorbResult.Remnant:
                if(fullVisual) fullVisual.SetActive(false);
                if(remnantVisual) remnantVisual.SetActive(true);
                break;
        }
    }
    public void Restore() {
        consumed=false;
        if(onAbsorbed==AbsorbResult.Disable) gameObject.SetActive(true);
        if(fullVisual) fullVisual.SetActive(true);
        if(remnantVisual) remnantVisual.SetActive(false);
        Highlight(false);
    }
    public bool RegenDue => consumed && regenSeconds>0 && Time.time>=regenAt;
    void OnDrawGizmosSelected() {
        Gizmos.color=BlobPlayer.PropertyColor(property);
        Gizmos.DrawWireCube(transform.position,size);
    }
}
}
