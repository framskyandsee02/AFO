using UnityEngine;
namespace OopsPlanet {
// Per-zone ambience that cross-fades as BLOB moves between zones and switches to a brighter loop once the
// zone is restored (puzzle references, no per-frame searches). Scaled by the player's ambience setting.
public class ZoneAmbience : MonoBehaviour {
    [System.Serializable] public class Zone {
        public string name; public Bounds area; public Ambience normal, restored; public PuzzleObjective[] restoredWhen;
        [System.NonSerialized] public AudioSource a, b;
    }
    public BlobWorld world;
    public Zone[] zones;
    [Range(0,1)] public float ambienceVolume=1, baseVolume=.6f;
    void Awake() {
        foreach(var z in zones) { z.a=MakeSource(z.normal); z.b=MakeSource(z.restored); }
    }
    AudioSource MakeSource(Ambience amb) {
        var s=gameObject.AddComponent<AudioSource>(); s.clip=ProcAudio.Get(amb); s.loop=true; s.spatialBlend=0; s.volume=0; s.playOnAwake=false; s.Play(); return s;
    }
    static bool Restored(Zone z) { if(z.restoredWhen==null||z.restoredWhen.Length==0) return false; foreach(var p in z.restoredWhen) if(!p||p.State!=PuzzleState.Solved) return false; return true; }
    void Update() {
        if(!world || !world.player) return;
        Vector3 pos=world.player.transform.position+Vector3.up*.5f;
        foreach(var z in zones) {
            bool inside=z.area.Contains(pos), restored=Restored(z);
            float v=baseVolume*ambienceVolume;
            z.a.volume=Mathf.MoveTowards(z.a.volume,inside&&!restored?v:0,Time.deltaTime*.8f);
            z.b.volume=Mathf.MoveTowards(z.b.volume,inside&&restored?v:0,Time.deltaTime*.8f);
        }
    }
    // For tests/UI: which loop is audible in a zone (0 none, 1 normal, 2 restored).
    public int Audible(int zone) { var z=zones[zone]; return z.b.volume>.01f?2:z.a.volume>.01f?1:0; }
}
}
