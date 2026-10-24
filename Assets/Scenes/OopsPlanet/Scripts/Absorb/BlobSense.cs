using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
namespace OopsPlanet {
// Sense mode (hold right mouse): the world dims slightly, absorbables within Range glow and show their Thai name,
// BLOB walks slower. Also tracks the nearest available absorbable, which BLOB's eyes/antenna and voice react to
// even when not sensing (design §3.3 points 2–3).
[RequireComponent(typeof(BlobPlayer))]
public class BlobSense : MonoBehaviour {
    public static bool Active { get; private set; }
    public static Vector3 Origin { get; private set; }
    public const float Range=12f, CueRange=6f;
    public Volume senseVolume;
    public float walkScale=.6f;
    public BlobInteractable nearest;
    public float nearestDistance=999;
    public int senseUses;
    BlobPlayer p; bool was;
    void Awake() { p=GetComponent<BlobPlayer>(); }
    void OnDisable() { Active=false; }
    void Update() {
        bool hold=!p.paused && !p.completed && (p.testMode?p.testSense:Mouse.current!=null && Mouse.current.rightButton.isPressed && Cursor.lockState==CursorLockMode.Locked);
        if(hold && !was) senseUses++;
        was=hold; Active=hold; Origin=p.transform.position+Vector3.up*.8f;
        p.speedScale=hold?walkScale:1;
        if(senseVolume) senseVolume.weight=Mathf.MoveTowards(senseVolume.weight,hold?1:0,Time.deltaTime*5);
        nearest=null; nearestDistance=999;
        if(p.world && p.world.sources!=null) foreach(var s in p.world.sources) {
            if(!s || !s.Available) continue;
            float d=Vector3.Distance(Origin,s.AimPoint);
            if(d<nearestDistance && d<=CueRange) { nearestDistance=d; nearest=s; }
        }
    }
}
}
