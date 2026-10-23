using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
// Marks a rigidbody the Magnet power may move. Keeps it inside the world: speed is clamped and
// it returns to its start pose if it falls out of the level or the level restarts.
[RequireComponent(typeof(Rigidbody))]
public class BlobMagnetic : MonoBehaviour {
    public static readonly List<BlobMagnetic> All=new List<BlobMagnetic>();
    public float maxSpeed=20f, killY=-10f;
    public Rigidbody Body { get; private set; }
    Vector3 startPos; Quaternion startRot;
    void Awake() { Body=GetComponent<Rigidbody>(); startPos=transform.position; startRot=transform.rotation; }
    void OnEnable() { if(!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }
    void FixedUpdate() {
        if(Body.linearVelocity.sqrMagnitude>maxSpeed*maxSpeed) Body.linearVelocity=Body.linearVelocity.normalized*maxSpeed;
        if(transform.position.y<killY) ResetToStart();
    }
    public void ResetToStart() {
        Body.position=startPos; Body.rotation=startRot; transform.SetPositionAndRotation(startPos,startRot);
        Body.linearVelocity=Vector3.zero; Body.angularVelocity=Vector3.zero;
    }
    public static void ResetAll() { foreach(var m in All.ToArray()) m.ResetToStart(); Physics.SyncTransforms(); }
}
// Mechanisms that react to a Pressure blast (P04 puzzles: turbine, vents, straw...).
public interface IPressureReceiver { void OnPressure(Vector3 direction,float strength); }
}
