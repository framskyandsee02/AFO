using UnityEngine;
namespace BorrowedTime {
public enum TargetKind { Plant, Bridge, Vine, Mechanism, Seal, LivingTime }
public class BTTimeTarget : MonoBehaviour {
    public TargetKind kind;
    public string displayName;
    public string[] stateNames;
    public GameObject[] states;
    public int age, initialAge;
    public float life=100, maxLife=100, minimumLife=20;
    public Transform aimPoint;
    public GameObject focusRing;
    public Renderer[] foliage;
    public Material healthyMaterial, wiltMaterial;
    public float regenPerSecond=7;
    public bool absorbAllowed=true;
    public bool pollutionStress;
    public BTTrainingGame game;
    public float timeSinceAbsorb=100;
    public bool IsSupport => kind==TargetKind.Bridge || kind==TargetKind.Vine;
    public bool IsLiving => kind==TargetKind.Plant || kind==TargetKind.LivingTime;
    public string StateName => stateNames!=null && age<stateNames.Length ? stateNames[age] : age.ToString();
    void Start() { Apply(); }
    void Update() {
        if(game && game.player && game.player.paused) return;
        timeSinceAbsorb+=Time.deltaTime;
        if(IsLiving && timeSinceAbsorb>3) life=Mathf.Min(maxLife,life+regenPerSecond*Time.deltaTime);
        if(foliage!=null) foreach(var r in foliage) if(r) r.sharedMaterial=life<85?wiltMaterial:healthyMaterial;
    }
    public void Apply() {
        if(states!=null) for(int i=0;i<states.Length;i++) if(states[i]) states[i].SetActive(i==(pollutionStress && age==1 && states.Length>2?2:age));
    }
    public void ResetTarget() { age=initialAge; pollutionStress=false; life=maxLife; timeSinceAbsorb=100; Apply(); }
    public bool TryStep(int direction, BTPlayer player, out string reason) {
        if(kind==TargetKind.Plant) { reason="ต้นไม้นี้เป็นแหล่งพลังชีวิต — กด LMB ค้างเพื่อดูดพลัง"; return false; }
        int next=age+direction;
        if(next<0 || next>=states.Length) { reason="ถึงขอบเขตเวลาแล้ว ลองใช้อีกพลังหนึ่ง"; return false; }
        if(IsSupport && player.StandingOn(this)) { reason="ระบบนิรภัย: ลงจากทางเดินก่อนเปลี่ยนเวลา"; return false; }
        var economy=game?game.GetComponent<BTFactoryLifeEconomy>():null;
        if(economy && economy.Locked(this)){reason="ระบบนี้ยืนยันแล้ว พลังถูกใช้กับเส้นทางที่เปิดไว้แล้ว";return false;}
        float cost=economy?economy.Cost(this,next):15;
        if(player.energy<cost) { reason=economy?"ต้องใช้พลัง 15 • หาต้นไม้ทดลอง หรือคืนวัตถุที่ยังไม่ยืนยันสู่อายุเดิมเพื่อกู้พลัง":"พลังไม่พอ กลับไปดูดจากพืชที่ฟื้นตัวแล้ว"; return false; }
        if(economy)economy.Pay(cost);else player.energy-=cost; age=next; Apply(); BTPresentation.Burst(aimPoint.position,direction<0?Color.cyan:Color.green,12); reason=displayName+" → "+StateName+(cost<0?" • คืนพลัง 15":" • ใช้พลัง 15");
        game.TargetChanged(this); BTSaveGame.Request(game); return true;
    }
    public float Absorb(float requested) {
        if(!IsLiving || !absorbAllowed) return 0;
        float actual=Mathf.Clamp(requested,0,Mathf.Max(0,life-minimumLife));
        life-=actual; if(actual>0) timeSinceAbsorb=0; return actual;
    }
}
}
