using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
public enum MaterialKind { Metal, Straw, Wood, Ice, Wax, Plant, Mud, WaterBody, Machine }
// A scene object that reacts to elements (Docs/DESIGN_V2_TH.md §5). State changes are raised as events so puzzles,
// VFX and audio can listen; nothing here searches objects by name. All state resets with the level.
public class WorldMaterial : MonoBehaviour {
    public static readonly List<WorldMaterial> All=new List<WorldMaterial>();
    public MaterialKind kind;
    [Tooltip("Puzzle-critical: can never catch fire or be destroyed (softlock guard).")]
    public bool critical;
    public Renderer[] visuals;
    public Collider[] colliders;
    [Header("Runtime state")]
    public float wetUntil, heatedUntil, poweredUntil;
    public bool burning, consumed, dried;
    public int growth;
    public Vector3 fireOrigin; public float fireStarted, burnEnd, ignitedAt;
    public bool everIgnited;
    public event System.Action<WorldMaterial,string> Changed;
    public bool Wet => Time.time<wetUntil;
    public bool Heated => Time.time<heatedUntil;
    public bool Powered => Time.time<poweredUntil;
    public bool Flammable => !critical && !consumed && (kind==MaterialKind.Straw||kind==MaterialKind.Wood||kind==MaterialKind.Plant);
    public bool Conductive => !consumed && (kind==MaterialKind.Metal||kind==MaterialKind.WaterBody||kind==MaterialKind.Machine||Wet);
    Vector3[] startScale; Color[] startColor; MaterialPropertyBlock mpb; float meltT=-1, nextHeatTransfer; bool wasDynamic;
    void Awake() {
        if(visuals==null||visuals.Length==0) visuals=GetComponentsInChildren<Renderer>();
        if(colliders==null||colliders.Length==0) colliders=GetComponentsInChildren<Collider>();
        startScale=new Vector3[visuals.Length]; startColor=new Color[visuals.Length];
        for(int i=0;i<visuals.Length;i++) { startScale[i]=visuals[i].transform.localScale; startColor[i]=visuals[i].sharedMaterial?visuals[i].sharedMaterial.GetColor("_BaseColor"):Color.white; }
        mpb=new MaterialPropertyBlock();
    }
    void OnEnable() { if(mpb==null || startScale==null) Awake(); if(!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }
    public Bounds Bounds { get { Bounds b=new Bounds(transform.position,Vector3.zero); bool first=true;
        foreach(var c in colliders) if(c) { if(first){b=c.bounds;first=false;} else b.Encapsulate(c.bounds); }
        if(first) foreach(var r in visuals) if(r) { if(first){b=r.bounds;first=false;} else b.Encapsulate(r.bounds); }
        return b; } }
    public void Raise(string what) { Changed?.Invoke(this,what); }
    // --- state changes (called by ElementReaction) ---
    public void SetWet(float seconds) { wetUntil=Mathf.Max(wetUntil,Time.time+seconds); Raise("wet"); }
    public void Ignite(Vector3 origin,float originTime) {
        if(!Flammable || burning || Wet) return;
        burning=true; everIgnited=true; ignitedAt=Time.time; fireOrigin=origin; fireStarted=originTime;
        burnEnd=Mathf.Min(Time.time+ElementReaction.BurnSeconds,originTime+ElementReaction.FireLifetime);
        Raise("ignited");
    }
    public void Extinguish() { if(!burning) return; burning=false; Raise("extinguished"); }
    public void Consume(string why) {
        if(consumed) return; consumed=true; burning=false;
        foreach(var c in colliders) if(c) c.enabled=false;
        // A burned / melted loose object must not fall through the floor once its collider is gone.
        var rb=GetComponent<Rigidbody>(); if(rb && !rb.isKinematic) { wasDynamic=true; rb.isKinematic=true; }
        if(why=="melted") meltT=0; else foreach(var r in visuals) if(r) r.enabled=false;
        Raise(why);
    }
    public void Heat(float seconds) { heatedUntil=Mathf.Max(heatedUntil,Time.time+seconds); Raise("heated"); }
    public void Cool() { if(Heated) { heatedUntil=0; Raise("steam"); } }
    public void Grow() { if(growth>=2) return; growth++; Raise(growth==2?"grown":"sprouted"); }
    public void Dry() { if(dried) return; dried=true; Raise("dried"); }
    public void PowerUp(float seconds) { bool was=Powered; poweredUntil=Mathf.Max(poweredUntil,Time.time+seconds); if(!was) Raise("powered"); }
    void Update() {
        if(burning) {
            if(Time.time>=burnEnd) { if(Time.time>=fireStarted+ElementReaction.FireLifetime) { burning=false; Raise("burned-out"); } else Consume("burned"); }
            else ElementReaction.SpreadFire(this);
        }
        // Red-hot metal passes its heat to what it touches (melts ice/wax, lights straw, boils water).
        if(kind==MaterialKind.Metal && Heated && !consumed && Time.time>=nextHeatTransfer) {
            nextHeatTransfer=Time.time+.5f;
            Bounds b=Bounds; b.Expand(.6f);
            foreach(var n in All.ToArray()) if(n!=this && !n.consumed && n.kind!=MaterialKind.Metal && n.kind!=MaterialKind.Machine && b.Intersects(n.Bounds)) ElementReaction.Apply(BlobProperty.Heat,n);
        }
        if(meltT>=0 && meltT<1) { meltT=Mathf.Min(1,meltT+Time.deltaTime/1.2f); for(int i=0;i<visuals.Length;i++) if(visuals[i]) visuals[i].transform.localScale=Vector3.Scale(startScale[i],new Vector3(1,1-meltT,1)); if(meltT>=1) foreach(var r in visuals) if(r) r.enabled=false; }
        if(kind==MaterialKind.Plant) for(int i=0;i<visuals.Length;i++) if(visuals[i] && !consumed) visuals[i].transform.localScale=Vector3.Lerp(visuals[i].transform.localScale,Vector3.Scale(startScale[i],new Vector3(1,1+growth*1.2f,1)),Time.deltaTime*3);
        Tint();
    }
    void Tint() {
        for(int i=0;i<visuals.Length;i++) {
            var r=visuals[i]; if(!r) continue;
            Color c=startColor[i];
            if(burning) c=Color.Lerp(c,new Color(1,.45f,.1f),.5f+.3f*Mathf.Sin(Time.time*12));
            else if(Heated) c=Color.Lerp(c,new Color(1,.35f,.1f),.7f);
            else if(dried) c=Color.Lerp(c,new Color(.75f,.65f,.5f),.6f);
            else if(kind==MaterialKind.Plant && growth>0) c=Color.Lerp(c,new Color(.3f,.8f,.3f),growth/2f);
            else if(Powered) c=Color.Lerp(c,new Color(1,.95f,.4f),.6f);
            if(Wet && !burning) c*=.7f;
            r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",c); r.SetPropertyBlock(mpb);
        }
    }
    public void ResetState() {
        wetUntil=heatedUntil=poweredUntil=0; burning=consumed=dried=everIgnited=false; growth=0; meltT=-1;
        for(int i=0;i<visuals.Length;i++) if(visuals[i]) { visuals[i].enabled=true; visuals[i].transform.localScale=startScale[i]; }
        foreach(var c in colliders) if(c) c.enabled=true;
        if(wasDynamic) { var rb=GetComponent<Rigidbody>(); if(rb) rb.isKinematic=false; wasDynamic=false; }
        Raise("reset");
    }
    public static void ResetAll() { foreach(var m in All.ToArray()) m.ResetState(); }
}
}
