using UnityEngine;
namespace OopsPlanet {
// Intake pipe of a steam machine (turbine, steam pump): met after enough steam columns rose inside its volume
// (Water + Heat spray, boiling the feed tank with Heat + Water...).
public class SteamInlet : MonoBehaviour, IPuzzleCondition {
    public Vector3 size=new Vector3(3,4,3);
    public int needed=2;
    public int received;
    public bool IsMet => received>=needed;
    public float Progress01 => Mathf.Clamp01((float)received/needed);
    void OnEnable() { SteamSystem.ColumnRaised+=OnSteam; }
    void OnDisable() { SteamSystem.ColumnRaised-=OnSteam; }
    void OnSteam(Vector3 at) {
        var b=new Bounds(transform.position,size); b.Expand(new Vector3(0,2,0));
        if(b.Contains(at)) { received++; VfxKit.Play(k=>k.Burst(transform.position,new Color(.92f,.95f,1),12,3,.25f,.8f,Vector3.up,25)); }
    }
    public void ResetCondition() { received=0; }
}
}
