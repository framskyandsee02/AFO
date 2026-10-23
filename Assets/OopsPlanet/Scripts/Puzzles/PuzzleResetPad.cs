using UnityEngine;
namespace OopsPlanet {
// Retry path: standing on the pad for a moment returns the target puzzle's movable pieces to their start
// (only while the puzzle is unsolved), so a misplaced or stuck piece can never softlock the player.
public class PuzzleResetPad : MonoBehaviour {
    public PuzzleObjective target;
    public Vector3 size=new Vector3(2,2,2);
    public float standSeconds=1f;
    public int resets;
    BlobPlayer player; float stood, cooldown;
    void Update() {
        if(!player) player=Object.FindFirstObjectByType<BlobPlayer>();
        if(!player || !target) return;
        cooldown-=Time.deltaTime;
        bool on=new Bounds(transform.position+Vector3.up*size.y/2,size).Contains(player.transform.position+Vector3.up*.2f);
        stood=on?stood+Time.deltaTime:0;
        if(stood>=standSeconds && cooldown<=0) {
            stood=0; cooldown=2;
            if(target.State==PuzzleState.Solved) { player.Notify("ปริศนานี้สำเร็จแล้ว"); return; }
            target.ResetPuzzle(false); resets++;
            player.Notify("รีเซ็ต \""+target.titleTh+"\" แล้ว",3);
        }
    }
}
// Procedurally generated cues (no external audio assets / licences needed).
public static class ProcAudio {
    static AudioClip jingle, hum;
    static AudioClip Make(string name,float seconds,System.Func<float,float> f) {
        int n=Mathf.CeilToInt(44100*seconds); var data=new float[n];
        for(int i=0;i<n;i++) data[i]=f(i/44100f);
        var c=AudioClip.Create(name,n,1,44100,false); c.SetData(data,0); return c;
    }
    public static AudioClip Jingle() {
        if(jingle) return jingle;
        float[] notes={523.3f,659.3f,784f,1046.5f};
        return jingle=Make("Puzzle solved",1f,t=>{ int k=Mathf.Min(3,(int)(t/.2f)); float lt=t-k*.2f; return Mathf.Sin(t*notes[k]*Mathf.PI*2)*.2f*Mathf.Exp(-lt*6); });
    }
    public static AudioClip Hum() {
        if(hum) return hum;
        return hum=Make("Turbine hum",1f,t=>(Mathf.Sin(t*110*Mathf.PI*2)*.6f+Mathf.Sin(t*220*Mathf.PI*2)*.3f)*.3f);
    }
}
}
