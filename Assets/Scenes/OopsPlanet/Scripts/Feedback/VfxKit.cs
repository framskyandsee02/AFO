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
    void OnDestroy() { if(Instance==this) Instance=null; if(strokeMaterial) Destroy(strokeMaterial); }
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
    // Crisp, short-lived silhouettes remain readable without bloom. Visuals never deliver gameplay hits.
    Material strokeMaterial;
    public int strokes;
    float nextInhale;
    public void Stroke(Vector3 from,Vector3 to,Color color,float width=.16f,float life=.22f,bool lightning=false) {
        if(!strokeMaterial) { var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit"); if(!shader) shader=Shader.Find("Sprites/Default"); strokeMaterial=new Material(shader); }
        var go=new GameObject("Power stroke"); go.transform.SetParent(transform,false);
        var line=go.AddComponent<LineRenderer>(); line.sharedMaterial=strokeMaterial;
        line.useWorldSpace=true; line.positionCount=lightning?13:2; line.numCapVertices=5;
        line.startWidth=width; line.endWidth=width*.45f; line.startColor=line.endColor=color;
        Vector3 side=Vector3.Cross((to-from).normalized,Vector3.up);
        for(int i=0;i<line.positionCount;i++) { float t=i/(float)(line.positionCount-1); line.SetPosition(i,Vector3.Lerp(from,to,t)+(lightning && i>0 && i<12?side*((i%2==0?1:-1)*.18f):Vector3.zero)); }
        strokes++; Destroy(go,life);
    }
    public void Inhale(Vector3 source,Vector3 mouth,Color color,float progress) {
        if(!ps || Time.time<nextInhale) return;
        nextInhale=Time.time+.025f;
        var ep=new ParticleSystem.EmitParams();
        Vector3 axis=(mouth-source).normalized, side=Vector3.Cross(axis,Vector3.up).normalized;
        Vector3 up=Vector3.Cross(axis,side);
        for(int i=0;i<5;i++) {
            float t=Mathf.Repeat(Time.time*1.8f+i*.2f,1), a=t*12+Time.time*5;
            Vector3 pos=Vector3.Lerp(source,mouth,t)+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*(1-t)*.45f;
            ep.position=pos; ep.velocity=(mouth-pos)*3; ep.startColor=color; ep.startSize=.13f+.06f*progress; ep.startLifetime=.12f; ps.Emit(ep,1);
        }
    }
    public static void Play(System.Action<VfxKit> fx) { if(Instance) fx(Instance); }
}
}
