using UnityEngine;
namespace OopsPlanet {
// A switch / floor pad that flips when electricity reaches it (spark, chain spark, landing shock, wet floor).
// Remembers when it was last hit; SwitchGroup turns these into "in order" or "all at once" conditions.
public class ElectricSwitch : MonoBehaviour, IElementReceiver {
    public int order;
    public Renderer lamp;
    public float lastHit=-999;
    public int hits;
    MaterialPropertyBlock mpb;
    public void OnElement(BlobProperty element,string comboId,Vector3 point) {
        if(element!=BlobProperty.Electric) return;
        lastHit=Time.time; hits++;
        Light(true);
        VfxKit.Play(k=>k.Burst(transform.position+Vector3.up*.3f,BlobPlayer.PropertyColor(BlobProperty.Electric),10,3,.08f,.3f));
    }
    public void Light(bool on) {
        if(!lamp) return; if(mpb==null) mpb=new MaterialPropertyBlock();
        lamp.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",on?new Color(1,.95f,.3f):new Color(.25f,.25f,.28f)); lamp.SetPropertyBlock(mpb);
    }
    public void ResetSwitch() { lastHit=-999; hits=0; Light(false); }
}
}
