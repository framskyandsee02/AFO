using UnityEngine;
namespace OopsPlanet {
// Met when a PuzzleItem with the matching key rests inside the socket volume (not hovering on a magnet, not moving).
// The lamp is the visual clue: red empty, amber item arriving, green settled.
public class ItemSocket : MonoBehaviour, IPuzzleCondition {
    public string key;
    public Vector3 size=new Vector3(1.6f,1.2f,1.6f);
    public float maxRestY=.9f, maxSpeed=.6f;
    public Renderer lamp;
    public bool IsMet { get; private set; }
    public float Progress01 { get; private set; }
    MaterialPropertyBlock mpb;
    Bounds Volume => new Bounds(transform.position+Vector3.up*size.y/2,size);
    void Update() {
        bool inside=false, settled=false;
        foreach(var i in PuzzleItem.All) {
            if(i.key!=key || !Volume.Contains(i.Center)) continue;
            inside=true;
            float speed=i.Body?i.Body.linearVelocity.magnitude:0;
            if(i.Center.y-transform.position.y<=maxRestY && speed<=maxSpeed) settled=true;
        }
        IsMet=settled; Progress01=settled?1:inside?.5f:0;
        if(lamp) {
            if(mpb==null) mpb=new MaterialPropertyBlock();
            lamp.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor",settled?new Color(.2f,1,.4f):inside?new Color(1,.7f,.1f):new Color(.9f,.15f,.1f));
            lamp.SetPropertyBlock(mpb);
        }
    }
    public void ResetCondition() { IsMet=false; Progress01=0; }
    void OnDrawGizmosSelected() { Gizmos.color=Color.yellow; Gizmos.DrawWireCube(Volume.center,Volume.size); }
}
}
