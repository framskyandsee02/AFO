using UnityEngine;
namespace OopsPlanet {
// One pooled particle system for every light-weight effect (absorb, swap, powers, landing, restoration).
// Particles are small opaque unlit spheres coloured per burst, so no transparent/sorting cost and no extra assets.
[RequireComponent(typeof(ParticleSystem))]
public class VfxKit : MonoBehaviour {
    public static VfxKit Instance { get; private set; }
    public int bursts, particlesEmitted;
    ParticleSystem ps;
    void Awake() { Instance=this; ps=GetComponent<ParticleSystem>(); }
    void OnDestroy() { if(Instance==this) Instance=null; }
    // Emit count particles at pos. dir=null -> spherical; otherwise inside a cone of spread degrees around dir.
    public void Burst(Vector3 pos,Color color,int count,float speed,float size,float life,Vector3? dir=null,float spread=180,float jitter=.1f) {
        if(!ps) return;
        bursts++; particlesEmitted+=count;
        var ep=new ParticleSystem.EmitParams();
        for(int i=0;i<count;i++) {
            Vector3 d=Random.onUnitSphere;
            if(dir.HasValue) { Vector3 f=dir.Value.normalized; d=Vector3.Slerp(f,d,spread/180f).normalized; if(Vector3.Dot(d,f)<0) d=-d; }
            ep.position=pos+Random.insideUnitSphere*jitter;
            ep.velocity=d*speed*Random.Range(.6f,1.1f);
            ep.startColor=color; ep.startSize=size*Random.Range(.7f,1.2f); ep.startLifetime=life*Random.Range(.7f,1.1f);
            ps.Emit(ep,1);
        }
    }
    // Ring of particles on the ground plane (landing dust, swap pulse).
    public void Ring(Vector3 pos,Color color,int count,float speed,float size,float life) {
        if(!ps) return;
        bursts++; particlesEmitted+=count;
        var ep=new ParticleSystem.EmitParams();
        for(int i=0;i<count;i++) {
            float a=i*Mathf.PI*2/count;
            Vector3 d=new Vector3(Mathf.Cos(a),.15f,Mathf.Sin(a));
            ep.position=pos+d*.2f; ep.velocity=d*speed; ep.startColor=color; ep.startSize=size; ep.startLifetime=life;
            ps.Emit(ep,1);
        }
    }
    public static void Play(System.Action<VfxKit> fx) { if(Instance) fx(Instance); }
}
}
