using UnityEngine;
namespace OopsPlanet {
// Shows overall restoration in the Hub by re-using one landmark's parts and materials:
//  stage 0: bare dead tree, dim core   stage 1 (factory restored): small canopy, glowing core
//  stage 2 (all zones restored): full canopy, grass patches, warmer hub light.
// When BLOB walks back into the Hub after the stage went up, BLOB is told what changed.
public class HubProgress : MonoBehaviour {
    public BlobWorld world;
    public PuzzleObjective[] stage1Requires, stage2Requires;
    public GameObject canopySmall, canopyFull;
    public GameObject[] stage2Extras;
    public Renderer trunk, core;
    public Light hubLight;
    public Bounds hubArea=new Bounds(Vector3.up*2,new Vector3(30,6,30));
    public BlobInteractable hubAreaZone;
    public string labelLocked, labelUnlocked, labelComplete;
    public ZoneGate riceGate;
    public int Stage { get; private set; }
    public int lastSeenStage;
    public int announcements;
    MaterialPropertyBlock mpb; float baseLight; bool inHubBefore=true;
    void Awake() { mpb=new MaterialPropertyBlock(); if(hubLight) baseLight=hubLight.intensity; Apply(0); }
    static bool All(PuzzleObjective[] list) { foreach(var p in list) if(!p || p.State!=PuzzleState.Solved) return false; return true; }
    public int ComputeStage() => All(stage2Requires)?2:All(stage1Requires)?1:0;
    public void Refresh(bool silentSeen) {
        Apply(ComputeStage());
        if(silentSeen) lastSeenStage=Stage;
    }
    void Apply(int stage) {
        Stage=stage;
        if(canopySmall) canopySmall.SetActive(stage==1);
        if(canopyFull) canopyFull.SetActive(stage==2);
        foreach(var g in stage2Extras) if(g) g.SetActive(stage==2);
        Tint(trunk,stage==0?new Color(.45f,.45f,.45f):new Color(.42f,.3f,.2f));
        Tint(core,stage==0?new Color(.25f,.3f,.28f):stage==1?new Color(.4f,1,.7f):new Color(.6f,1,.5f));
        if(hubLight) hubLight.intensity=baseLight*(stage==2?1.25f:1f);
        if(hubAreaZone) hubAreaZone.label=stage==2?labelComplete:riceGate&&riceGate.Unlocked?labelUnlocked:labelLocked;
    }
    void Tint(Renderer r,Color c) { if(!r) return; r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor",c); r.SetPropertyBlock(mpb); }
    void Update() {
        if(!world || !world.player) return;
        bool inHub=hubArea.Contains(world.player.transform.position+Vector3.up*.5f);
        if(inHub && !inHubBefore && Stage>lastSeenStage) {
            announcements++;
            world.player.Notify(Stage==2?"ฮับฟื้นฟูสมบูรณ์! ต้นไม้ชีวภาพผลิบานเต็มที่":"ฮับเปลี่ยนไป: ต้นไม้ชีวภาพเริ่มผลิใบ และทางไปนาข้าวเปิดแล้ว",4);
            lastSeenStage=Stage;
        }
        inHubBefore=inHub;
    }
}
}
