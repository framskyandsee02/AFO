using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
// A movable puzzle piece identified by key (sockets / zones match on key, never on GameObject names).
// Returns to its start pose on reset. If it falls out of the world it goes back to its start, or - for pieces
// whose start is inside the goal zone (e.g. straw to clear) - to a fallback drop point so progress is not undone.
public class PuzzleItem : MonoBehaviour {
    public static readonly List<PuzzleItem> All=new List<PuzzleItem>();
    public string key;
    public float killY=-10f;
    public bool useFallback;
    public Vector3 fallbackPosition;
    public Rigidbody Body { get; private set; }
    Vector3 startPos; Quaternion startRot;
    void Awake() { Body=GetComponent<Rigidbody>(); startPos=transform.position; startRot=transform.rotation; }
    void OnEnable() { if(!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }
    public int outOfWorldRecoveries;
    void FixedUpdate() {
        if(transform.position.y>=killY) return;
        outOfWorldRecoveries++;
        if(useFallback) PlaceAt(fallbackPosition,startRot); else ResetToStart();
    }
    public Vector3 Center => Body?Body.worldCenterOfMass:transform.position;
    public void ResetToStart() => PlaceAt(startPos,startRot);
    void PlaceAt(Vector3 pos,Quaternion rot) {
        if(Body) { Body.position=pos; Body.rotation=rot; Body.linearVelocity=Vector3.zero; Body.angularVelocity=Vector3.zero; }
        transform.SetPositionAndRotation(pos,rot);
        Physics.SyncTransforms();
    }
    public static void ResetAll() { foreach(var i in All.ToArray()) i.ResetToStart(); }
}
}
