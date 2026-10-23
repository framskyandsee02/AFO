using UnityEngine;
namespace OopsPlanet {
// Blocks a zone entrance until all required puzzles are solved, then slides its door open.
// Approaching while locked tells the player (in Thai) what is still needed.
public class ZoneGate : MonoBehaviour {
    public BlobWorld world;
    public string zoneId;
    public string zoneNameTh;
    public PuzzleObjective[] requires;
    public PuzzleObjective[] requiresAnyOf=new PuzzleObjective[0];
    public int anyCount;
    public Transform door; public Vector3 openOffset=Vector3.up*4.4f;
    public Renderer lamp;
    public float openSeconds=1.5f, hintRadius=4f;
    public bool Unlocked { get; private set; }
    float t, hintCooldown; Vector3 doorStart; bool started; MaterialPropertyBlock mpb;
    void Awake() { if(door) doorStart=door.localPosition; started=true; ApplyLamp(); }
    public bool RequirementsMet { get {
        foreach(var r in requires) if(!r || r.State!=PuzzleState.Solved) return false;
        int n=0; if(requiresAnyOf!=null) foreach(var r in requiresAnyOf) if(r && r.State==PuzzleState.Solved) n++;
        return n>=anyCount; } }
    public void Unlock(bool instant) {
        if(!started) Awake();
        Unlocked=true; if(instant) { t=1; Blend(); }
        ApplyLamp();
    }
    public void Lock() { if(!started) Awake(); Unlocked=false; t=0; Blend(); ApplyLamp(); }
    void Update() {
        if(Unlocked && t<1) { t=Mathf.Min(1,t+Time.deltaTime/openSeconds); Blend(); }
        hintCooldown-=Time.deltaTime;
        if(!Unlocked && hintCooldown<=0) {
            var w=world;
            if(w && w.player && Vector3.Distance(w.player.transform.position,transform.position)<hintRadius) {
                hintCooldown=4;
                string need=""; foreach(var r in requires) if(r && r.State!=PuzzleState.Solved) need+=(need==""?"":", ")+r.titleTh;
                if(anyCount>0 && requiresAnyOf!=null) { string any=""; int n=0; foreach(var r in requiresAnyOf) if(r) { if(r.State==PuzzleState.Solved) n++; any+=(any==""?"":" / ")+r.titleTh; } if(n<anyCount) need+=(need==""?"":" และ ")+anyCount+" ใน ("+any+")"; }
                w.player.Notify(zoneNameTh+" ยังล็อกอยู่ — ต้องแก้: "+need);
            }
        }
    }
    void Blend() { if(door) door.localPosition=doorStart+openOffset*Mathf.SmoothStep(0,1,t); }
    void ApplyLamp() {
        if(!lamp) return; if(mpb==null) mpb=new MaterialPropertyBlock();
        lamp.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",Unlocked?new Color(.2f,1,.4f):new Color(.9f,.15f,.1f)); lamp.SetPropertyBlock(mpb);
    }
    public float Open01 => t;
}
}
