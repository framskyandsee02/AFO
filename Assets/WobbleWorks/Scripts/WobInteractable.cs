using UnityEngine;
namespace WobbleWorks {
public enum WobInteraction { Source, Checkpoint, GasVent, Pipe, Exit, Reset }
public class WobInteractable : MonoBehaviour {
    public WobInteraction kind;
    public WobProperty property;
    public Vector3 size=Vector3.one;
    public Vector3 respawn;
    public int index;
    public Renderer indicator;
    public bool Contains(Vector3 point) => new Bounds(transform.position,size).Contains(point);
    public void Highlight(bool selected) {
        if(indicator) indicator.transform.localScale=Vector3.one*(selected?1.13f:1);
    }
    void OnDrawGizmosSelected() {
        Gizmos.color=WobPlayer.PropertyColor(property);
        Gizmos.DrawWireCube(transform.position,size);
    }
}
}
