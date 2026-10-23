using UnityEngine;
namespace OopsPlanet {
// Met when no PuzzleItem with the key is left inside the zone (e.g. straw blocking a sluice).
public class ClearZoneCondition : MonoBehaviour, IPuzzleCondition {
    public string key;
    public Vector3 size=new Vector3(4,2,4);
    public int initialCount;
    public bool IsMet { get { return Count()==0; } }
    public float Progress01 { get { int total=Mathf.Max(1,initialCount); return 1-(float)Count()/total; } }
    Bounds Volume => new Bounds(transform.position,size);
    public int Count() { int n=0; foreach(var i in PuzzleItem.All) { if(i.key!=key || !Volume.Contains(i.Center)) continue; var m=i.GetComponent<WorldMaterial>(); if(m && m.consumed) continue; n++; } return n; }
    void Start() { if(initialCount==0) initialCount=Count(); }
    public void ResetCondition() { }
    void OnDrawGizmosSelected() { Gizmos.color=Color.cyan; Gizmos.DrawWireCube(transform.position,size); }
}
}
