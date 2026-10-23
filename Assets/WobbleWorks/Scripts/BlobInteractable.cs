using UnityEngine;
namespace OopsPlanet {
public enum BlobInteraction { Source, Checkpoint, GasVent, Pipe, Exit, Reset, Area }
public class BlobInteractable : MonoBehaviour {
    public BlobInteraction kind;
    public BlobProperty property;
    public Vector3 size=Vector3.one;
    public Vector3 respawn;
    public int index;
    public string label;
    public Renderer indicator;
    public bool Contains(Vector3 point) => new Bounds(transform.position,size).Contains(point);
    public void Highlight(bool selected) {
        if(indicator) indicator.transform.localScale=Vector3.one*(selected?1.13f:1);
    }
    void OnDrawGizmosSelected() {
        Gizmos.color=BlobPlayer.PropertyColor(property);
        Gizmos.DrawWireCube(transform.position,size);
    }
}
}
