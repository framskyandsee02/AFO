using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
public enum PuzzleState { Locked, Active, Solved }
// All: every condition must hold (prototype puzzles). Any: one condition is enough - each condition is one solution (v2).
public enum ConditionMode { All, Any }
// A condition a PuzzleObjective waits for (socket filled, turbine spinning, zone cleared...).
public interface IPuzzleCondition {
    bool IsMet { get; }
    float Progress01 { get; }
    void ResetCondition();
}
// Reusable puzzle objective: Locked until its prerequisite is solved, Active while its conditions are checked,
// Solved exactly once (the Solved event cannot fire twice until a full reset). Nothing here looks objects up by name.
public class PuzzleObjective : MonoBehaviour {
    public static readonly List<PuzzleObjective> All=new List<PuzzleObjective>();
    public string id;
    public string titleTh, hintTh, solvedTh;
    public PuzzleObjective requires;
    [Tooltip("World-space area where the HUD shows this objective.")]
    public Bounds area=new Bounds(Vector3.zero,Vector3.one*10);
    [Tooltip("Components implementing IPuzzleCondition; all must be met for holdSeconds.")]
    public MonoBehaviour[] conditions;
    [Tooltip("Movable pieces returned to their start by the reset pad / restart.")]
    public PuzzleItem[] items;
    [Tooltip("v2: world materials put back by the reset pad / restart (burned straw, melted latch...).")]
    public WorldMaterial[] materials=new WorldMaterial[0];
    [Tooltip("v2: hidden spot (ship part / shortcut); not counted in 'restore the world x/7'.")]
    public bool isSecret;
    public float holdSeconds=.5f;
    public ConditionMode mode=ConditionMode.All;
    [Tooltip("Which condition solved it (Any mode): type + object name. Used by tests and the power journal.")]
    public string solvedBy="";
    string metBy="";
    [Tooltip("Where the restoration celebration plays.")]
    public Vector3 celebratePoint;
    public int solvedCount;
    public event System.Action<PuzzleObjective> Solved, WasReset;
    public PuzzleState State { get; private set; }
    float metFor;
    AudioSource audioSource;
    void OnEnable() { if(!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }
    void Awake() { audioSource=gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend=0; audioSource.playOnAwake=false; RefreshLock(); }
    void RefreshLock() {
        if(State==PuzzleState.Solved) return;
        State=requires && requires.State!=PuzzleState.Solved?PuzzleState.Locked:PuzzleState.Active;
    }
    IEnumerable<IPuzzleCondition> Conditions() {
        foreach(var c in conditions) if(c is IPuzzleCondition pc) yield return pc;
    }
    public float Progress01 {
        get {
            if(State==PuzzleState.Solved) return 1;
            float sum=0; int n=0;
            foreach(var c in Conditions()) { sum+=c.Progress01; n++; }
            return n==0?0:sum/n;
        }
    }
    void Update() {
        if(State==PuzzleState.Solved) return;
        RefreshLock();
        if(State!=PuzzleState.Active) { metFor=0; return; }
        bool met=mode==ConditionMode.All; int n=0; metBy="";
        foreach(var c in Conditions()) {
            n++; bool ok=c.IsMet;
            if(mode==ConditionMode.All) met&=ok;
            else if(ok && !met) { met=true; metBy=Describe(c); }
        }
        if(mode==ConditionMode.All && met) metBy="all";
        metFor=met&&n>0?metFor+Time.deltaTime:0;
        if(metFor>=holdSeconds) Solve();
    }
    // fromSave: restoring a saved game (no jingle; world reactions snap to their solved look).
    public bool LastSolveFromSave { get; private set; }
    public void Solve() => Solve(false);
    public void Solve(bool fromSave) {
        if(State==PuzzleState.Solved) return;
        State=PuzzleState.Solved; solvedCount++; LastSolveFromSave=fromSave; solvedBy=fromSave?"save":(metBy==""?"api":metBy);
        if(audioSource && !fromSave) audioSource.PlayOneShot(ProcAudio.Jingle(),.8f);
        Debug.Log("OOPS_PUZZLE_SOLVED "+id+(fromSave?" (from save)":""));
        if(fromSave) foreach(var i in items) if(i && i.useFallback) i.PlaceAtFallback();
        Solved?.Invoke(this);
    }
    // Retry path: put movable pieces back. A full reset (level restart) also un-solves the puzzle.
    public void ResetPuzzle(bool full) {
        if(State==PuzzleState.Solved && !full) return;
        foreach(var i in items) if(i) i.ResetToStart();
        if(materials!=null) foreach(var m in materials) if(m) m.ResetState();
        foreach(var c in Conditions()) c.ResetCondition();
        metFor=0;
        if(full) { solvedBy=""; State=PuzzleState.Active; solvedCount=0; RefreshLock(); }
        WasReset?.Invoke(this);
    }
    static string Describe(IPuzzleCondition c) { var mb=c as MonoBehaviour; var msc=c as MaterialStateCondition;
        return msc&&!string.IsNullOrEmpty(msc.label)?msc.label:(mb?mb.GetType().Name+":"+mb.name:c.GetType().Name); }
    public string StatusTh {
        get {
            switch(State) {
                case PuzzleState.Locked: return "ล็อกอยู่ — ต้องแก้ \""+(requires?requires.titleTh:"?")+"\" ก่อน";
                case PuzzleState.Solved: return "สำเร็จ! "+solvedTh;
                default: return hintTh;
            }
        }
    }
}
}
