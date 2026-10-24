using UnityEngine;
namespace OopsPlanet {
// A seed (WorldMaterial Plant) that grows into a walkable vine bridge / ladder after enough water (growth stage 2).
// The bridge collider and visual only exist once grown; resetting the seed removes them again.
public class VineBridge : MonoBehaviour {
    public WorldMaterial seed;
    public GameObject bridge;
    public int needGrowth=2;
    public bool Grown => seed && !seed.consumed && seed.growth>=needGrowth;
    float t;
    void Update() {
        if(!bridge) return;
        bool on=Grown;
        if(bridge.activeSelf!=on) { bridge.SetActive(on); t=0; if(on) VfxKit.Play(k=>k.Burst(bridge.transform.position,new Color(.3f,.85f,.3f),40,4,.14f,.8f)); }
        if(on && t<1) { t=Mathf.Min(1,t+Time.deltaTime*1.5f); }
    }
}
}
