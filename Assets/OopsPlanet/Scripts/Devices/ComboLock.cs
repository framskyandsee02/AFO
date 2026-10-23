using UnityEngine;
namespace OopsPlanet {
// Something only one particular combo can open (hidden ship-part spots, design §7.3): a fuse that only a short
// circuit (Electric + Heat) melts, a switch round a corner only a wind ball (Pressure + Bounce) reaches, a broken
// conveyor rail only welding (Heat + Electric) repairs. Puzzle condition. Deliberately has no WorldMaterial, so the
// plain element reactions cannot open it; hideOnUnlock (the fuse, the gap) disappears when it opens.
public class ComboLock : MonoBehaviour, IElementReceiver, IPuzzleCondition {
    [Tooltip("Ordered combo ids that open it, e.g. electric+heat.")]
    public string[] acceptedCombos;
    public string label;
    public GameObject hideOnUnlock, showOnUnlock;
    public bool unlocked;
    public int wrongHits;
    public bool IsMet => unlocked;
    public float Progress01 => unlocked?1:0;
    public void OnElement(BlobProperty element,string comboId,Vector3 point) {
        if(unlocked) return;
        if(System.Array.IndexOf(acceptedCombos,comboId)<0) { if(comboId!="") wrongHits++; return; }
        unlocked=true;
        if(hideOnUnlock) hideOnUnlock.SetActive(false);
        if(showOnUnlock) showOnUnlock.SetActive(true);
        VfxKit.Play(k=>k.Burst(transform.position,new Color(1,.9f,.4f),30,5,.12f,.6f));
    }
    public void ResetCondition() { unlocked=false; wrongHits=0; if(hideOnUnlock) hideOnUnlock.SetActive(true); if(showOnUnlock) showOnUnlock.SetActive(false); }
}
}
