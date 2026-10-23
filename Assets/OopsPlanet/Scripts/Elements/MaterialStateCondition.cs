using UnityEngine;
namespace OopsPlanet {
public enum MaterialGoal { Burned, Melted, Powered, Grown, Dried, Washed, Extinguished, Watered, Ignited, Intact }
// Puzzle condition met when the target materials reach a state (e.g. ice melted, machine powered).
// Used with PuzzleObjective.ConditionMode.Any to build multi-solution puzzles.
public class MaterialStateCondition : MonoBehaviour, IPuzzleCondition {
    public string label;
    public MaterialGoal goal;
    public WorldMaterial[] targets;
    public bool requireAll=true;
    bool extinguishedSeen;
    void Awake() { foreach(var t in targets) if(t) t.Changed+=(m,what)=>{ if(what=="extinguished") extinguishedSeen=true; }; }
    bool Reached(WorldMaterial m) {
        switch(goal) {
            case MaterialGoal.Burned: return m.consumed && !m.burning;
            case MaterialGoal.Melted: return m.consumed && (m.kind==MaterialKind.Ice||m.kind==MaterialKind.Wax);
            case MaterialGoal.Powered: return m.Powered;
            case MaterialGoal.Grown: return m.growth>=2;
            case MaterialGoal.Dried: return m.dried;
            case MaterialGoal.Washed: return m.consumed && m.kind==MaterialKind.Mud;
            case MaterialGoal.Watered: return m.growth>=1;
            case MaterialGoal.Ignited: return m.everIgnited;
            case MaterialGoal.Intact: return !m.consumed && !m.burning;
            default: return extinguishedSeen;
        }
    }
    public bool IsMet { get { int n=0,ok=0; foreach(var t in targets) if(t) { n++; if(Reached(t)) ok++; } return n>0 && (requireAll?ok==n:ok>0); } }
    public float Progress01 { get { int n=0,ok=0; foreach(var t in targets) if(t) { n++; if(Reached(t)) ok++; } return n==0?0:(float)ok/n; } }
    public void ResetCondition() { extinguishedSeen=false; }
}
}
