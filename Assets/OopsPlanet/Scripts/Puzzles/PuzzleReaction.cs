using UnityEngine;
namespace OopsPlanet {
// World restoration driven by a PuzzleObjective's events (no per-frame object searches):
// show/hide objects, slide a mover, recolour lights and renderers, stop smoke, grow plants.
// A full reset (level restart) snaps everything back to the unsolved look.
public class PuzzleReaction : MonoBehaviour {
    public PuzzleObjective objective;
    public float duration=1.5f;
    public GameObject[] showWhenSolved, hideWhenSolved;
    public Transform mover; public Vector3 moveOffset;
    public Light[] lights; public Color solvedLightColor=new Color(1,.85f,.6f); public float solvedIntensity=2f;
    public Renderer[] recolor; public Color solvedColor=new Color(.3f,.75f,.35f);
    public ParticleSystem[] stopWhenSolved;
    public Transform[] grow; public Vector3 growScale=new Vector3(1,2.2f,1);
    public bool IsApplied { get; private set; }
    float t; Vector3 moverStart; Color[] lightStart; float[] intensityStart; Color[] colorStart; Vector3[] growStart;
    MaterialPropertyBlock mpb;
    void Awake() {
        if(mover) moverStart=mover.localPosition;
        lightStart=new Color[lights.Length]; intensityStart=new float[lights.Length];
        for(int i=0;i<lights.Length;i++) { lightStart[i]=lights[i].color; intensityStart[i]=lights[i].intensity; }
        colorStart=new Color[recolor.Length];
        for(int i=0;i<recolor.Length;i++) colorStart[i]=recolor[i].sharedMaterial.GetColor("_BaseColor");
        growStart=new Vector3[grow.Length];
        for(int i=0;i<grow.Length;i++) growStart[i]=grow[i].localScale;
        mpb=new MaterialPropertyBlock();
        if(objective) { objective.Solved+=_=>Apply(); objective.WasReset+=o=>{ if(o.State!=PuzzleState.Solved) Revert(); }; }
    }
    public void Apply() {
        IsApplied=true;
        foreach(var g in showWhenSolved) if(g) g.SetActive(true);
        foreach(var g in hideWhenSolved) if(g) g.SetActive(false);
        foreach(var p in stopWhenSolved) if(p) p.Stop(true,ParticleSystemStopBehavior.StopEmitting);
    }
    public void Revert() {
        IsApplied=false; t=0; Blend(0);
        foreach(var g in showWhenSolved) if(g) g.SetActive(false);
        foreach(var g in hideWhenSolved) if(g) g.SetActive(true);
        foreach(var p in stopWhenSolved) if(p) { p.Clear(); p.Play(); }
    }
    void Update() {
        if(!IsApplied || t>=1) return;
        t=Mathf.Min(1,t+Time.deltaTime/Mathf.Max(.01f,duration));
        Blend(Mathf.SmoothStep(0,1,t));
    }
    void Blend(float k) {
        if(mover) mover.localPosition=moverStart+moveOffset*k;
        for(int i=0;i<lights.Length;i++) { lights[i].color=Color.Lerp(lightStart[i],solvedLightColor,k); lights[i].intensity=Mathf.Lerp(intensityStart[i],solvedIntensity,k); }
        for(int i=0;i<recolor.Length;i++) { recolor[i].GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",Color.Lerp(colorStart[i],solvedColor,k)); recolor[i].SetPropertyBlock(mpb); }
        for(int i=0;i<grow.Length;i++) grow[i].localScale=Vector3.Lerp(growStart[i],Vector3.Scale(growStart[i],growScale),k);
    }
    public float Blend01 => IsApplied?t:0;
}
}
