using UnityEngine;
namespace OopsPlanet {
// A brittle part (rusty bolt, cracked lid) that breaks when a moving rigidbody hits it hard enough - in practice
// a Magnet Cannon shot. Breaking consumes its WorldMaterial and can open a LatchedDoor straight away.
[RequireComponent(typeof(WorldMaterial))]
public class ImpactBreak : MonoBehaviour {
    public float minSpeed=8f;
    public LatchedDoor opens;
    public int breaks;
    WorldMaterial m;
    void Awake() { m=GetComponent<WorldMaterial>(); }
    void OnCollisionEnter(Collision c) {
        if(m.consumed || c.rigidbody==null || c.relativeVelocity.magnitude<minSpeed) return;
        breaks++; m.Consume("broken");
        VfxKit.Play(k=>k.Burst(c.GetContact(0).point,new Color(.55f,.3f,.15f),24,6,.1f,.5f));
        if(opens) opens.Open();
    }
}
}
