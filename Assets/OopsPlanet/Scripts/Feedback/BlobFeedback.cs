using UnityEngine;
namespace OopsPlanet {
// Turns gameplay events into sound, light VFX and BLOB moods. Gameplay never depends on this component.
public class BlobFeedback : MonoBehaviour {
    public BlobPlayer player; public BlobPowers powers; public BlobWorld world; public BlobExpression expression;
    public AudioSource voice, sfx, magnetLoop;
    public int[] cueCounts=new int[System.Enum.GetValues(typeof(Cue)).Length];
    float nextSpark;
    void Start() {
        player.Absorbed+=(src,prop)=>{ Play(Cue.Absorb,voice); Mood(BlobExpression.Mood.Happy,1);
            Vfx(k=>k.Burst(src.AimPoint,BlobPlayer.PropertyColor(prop),24,6,.12f,.45f,player.transform.position+Vector3.up*.7f-src.AimPoint,25)); };
        player.Swapped+=()=>{ Play(Cue.Swap,sfx); Vfx(k=>k.Ring(player.transform.position+Vector3.up*.3f,BlobPlayer.PropertyColor(player.property),18,4,.1f,.4f)); };
        player.AbsorbRefused+=status=>{ Play(Cue.Refused,voice); Mood(BlobExpression.Mood.Sad,1.2f); };
        player.Launched+=bounce=>{ if(!bounce) return; Play(Cue.BounceLaunch,voice); Mood(BlobExpression.Mood.Surprised,1);
            Vfx(k=>k.Ring(player.transform.position+Vector3.up*.05f,BlobPlayer.PropertyColor(BlobProperty.Bounce),16,5,.12f,.35f)); };
        player.Rebounded+=()=>Play(Cue.Rebound,sfx);
        player.Landed+=impact=>{ if(impact<6) return; Play(Cue.Land,sfx);
            Vfx(k=>k.Ring(player.transform.position+Vector3.up*.05f,new Color(.6f,.58f,.55f),14,Mathf.Min(6,impact*.4f),.14f,.35f)); };
        if(powers) {
            powers.Blasted+=(origin,dir)=>{ Play(Cue.PressureWhoosh,sfx); Vfx(k=>k.Burst(origin+dir*.6f,BlobPlayer.PropertyColor(BlobProperty.Pressure),30,9,.1f,.45f,dir,30)); };
            powers.ComboUsed+=fx=>{ Play(Cue.Combo,sfx); Mood(BlobExpression.Mood.Happy,1.2f); Vfx(k=>k.Burst(player.transform.position+Vector3.up*.8f,new Color(1,.85f,.3f),26,5,.1f,.5f)); };
        }
        if(world) {
            world.Respawned+=()=>{ if(world.deaths<=0) return; Play(Cue.Respawn,voice); Mood(BlobExpression.Mood.Sad,1.5f); };
            world.ZoneUnlocked+=g=>{ Play(Cue.Unlock,sfx); Vfx(k=>k.Burst(g.transform.position+Vector3.up*4,new Color(.3f,1,.5f),60,7,.16f,1f)); };
            if(world.puzzles!=null) foreach(var pz in world.puzzles) if(pz) pz.Solved+=o=>{ if(o.LastSolveFromSave) return;
                Play(Cue.Happy,voice); Mood(BlobExpression.Mood.Happy,2.5f);
                Vfx(k=>k.Burst(o.celebratePoint,new Color(.5f,1,.4f),70,7,.18f,1.2f)); Vfx(k=>k.Burst(o.celebratePoint,new Color(1,.9f,.35f),40,5,.12f,1f)); };
        }
        if(magnetLoop) { magnetLoop.clip=ProcAudio.MagnetBuzz(); magnetLoop.loop=true; magnetLoop.volume=0; magnetLoop.Play(); }
    }
    void Update() {
        bool magnet=powers && powers.held;
        if(magnetLoop) magnetLoop.volume=Mathf.MoveTowards(magnetLoop.volume,magnet?.35f:0,Time.deltaTime*3);
        if(magnet && Time.time>nextSpark) {
            nextSpark=Time.time+.07f;
            Vector3 a=transform.position+Vector3.up*.8f, b=powers.held.Body.worldCenterOfMass;
            Vfx(k=>k.Burst(Vector3.Lerp(a,b,Random.value),BlobPlayer.PropertyColor(BlobProperty.Magnet),1,1.5f,.07f,.25f));
        }
    }
    void Play(Cue cue,AudioSource src) {
        cueCounts[(int)cue]++;
        if(src && !player.testMode) { src.pitch=Random.Range(.94f,1.06f); src.PlayOneShot(ProcAudio.Get(cue)); }
    }
    void Mood(BlobExpression.Mood m,float seconds) { if(expression) expression.SetMood(m,seconds); }
    static void Vfx(System.Action<VfxKit> fx) => VfxKit.Play(fx);
    public int Count(Cue cue) => cueCounts[(int)cue];
}
}
