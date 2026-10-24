using UnityEngine;
namespace OopsPlanet {
// Pressure-driven turbine: each blast adds spin, spin decays, the condition is met at full spin.
// Clues: rotor speed, three gauge lamps and a hum whose pitch follows the spin.
public class PressureTurbine : MonoBehaviour, IPuzzleCondition, IPressureReceiver {
    public PuzzleObjective objective;
    public Transform rotor;
    public Renderer[] gaugeLamps;
    public float perBlast=.6f, decayPerSecond=.08f, maxDegreesPerSecond=720f;
    public float spin;
    public int blastsReceived;
    public bool IsMet => spin>=1f;
    public float Progress01 => Mathf.Clamp01(spin);
    AudioSource hum; MaterialPropertyBlock mpb;
    void Awake() {
        hum=gameObject.AddComponent<AudioSource>(); hum.clip=ProcAudio.Hum(); hum.loop=true; hum.spatialBlend=.7f; hum.volume=0; hum.playOnAwake=false; hum.Play();
    }
    public void OnPressure(Vector3 direction,float strength) {
        if(objective && objective.State==PuzzleState.Locked) return;
        blastsReceived++;
        spin=Mathf.Min(1.2f,spin+perBlast*Mathf.Max(.2f,strength));
    }
    void Update() {
        bool solved=objective && objective.State==PuzzleState.Solved;
        if(solved) spin=Mathf.Max(spin,1f); else spin=Mathf.Max(0,spin-decayPerSecond*Time.deltaTime);
        if(rotor) rotor.Rotate(Vector3.forward,Mathf.Clamp01(spin)*maxDegreesPerSecond*Time.deltaTime,Space.Self);
        if(hum) { hum.volume=Mathf.Clamp01(spin)*.25f; hum.pitch=.6f+Mathf.Clamp01(spin)*.9f; }
        if(gaugeLamps!=null) {
            if(mpb==null) mpb=new MaterialPropertyBlock();
            for(int i=0;i<gaugeLamps.Length;i++) {
                bool on=spin>=(i+1)/(float)gaugeLamps.Length-.001f;
                gaugeLamps[i].GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",on?new Color(.3f,1,.5f):new Color(.25f,.25f,.25f)); gaugeLamps[i].SetPropertyBlock(mpb);
            }
        }
    }
    public void ResetCondition() { spin=0; blastsReceived=0; }
}
}
