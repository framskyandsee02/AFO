using System.Collections;
using UnityEngine;
namespace OopsPlanet {
// Repair encounter: physical scrap impact -> environmental cooling pump -> fitted spare gear + welding bench.
public class FactoryRepairBoss : MonoBehaviour,IPuzzleCondition {
    public PuzzleObjective[] requiredRepairs=new PuzzleObjective[0];
    public BlobWorld world;public PuzzleObjective objective;public Transform body,leftArm,rightArm,leftBrow,rightBrow,warning,repairSocket;
    public Renderer[] bodyRenderers=new Renderer[0];public Renderer[] eyes=new Renderer[0];public Transform[] scrap=new Transform[0];
    public Vector3 arenaCenter;public Vector3 checkpoint;public float arenaRadius=22,attackInterval=5.5f,windup=1.6f;
    public int stage;public float cooling;public bool active,repaired;public int attacks,failures,physicalImpacts;
    public bool IsMet=>repaired;public float Progress01=>repaired?1:stage/3f;
    float repairedAt=-100;
    float nextAttack,attackAt=-1,recoverUntil;Vector3 target,bodyStart;Quaternion leftStart,rightStart;Vector3[] scrapStart;MaterialPropertyBlock mpb;bool wired;int pattern;
    void Awake(){bodyStart=body.localPosition;leftStart=leftArm.localRotation;rightStart=rightArm.localRotation;scrapStart=new Vector3[scrap.Length];for(int i=0;i<scrap.Length;i++)scrapStart[i]=scrap[i].position;mpb=new MaterialPropertyBlock();world.Respawned+=OnRespawn;wired=true;if(objective){objective.Solved+=OnSolved;objective.WasReset+=OnReset;}warning.gameObject.SetActive(false);}
    void Start(){if(objective&&objective.State==PuzzleState.Solved)RestoreInstant();}
    void OnDestroy(){if(wired&&world)world.Respawned-=OnRespawn;if(objective){objective.Solved-=OnSolved;objective.WasReset-=OnReset;}}
    void OnSolved(PuzzleObjective p){if(p.LastSolveFromSave)RestoreInstant();}
    void OnReset(PuzzleObjective p){if(p.State!=PuzzleState.Solved)ResetCondition();}
    void OnRespawn(){if(!active||repaired)return;failures++;ResetCondition();active=false;world.checkpoint=checkpoint;world.player.Teleport(checkpoint);world.player.Notify("ลองใหม่ได้ — ใช้เฟือง ปั๊มน้ำ และโต๊ะซ่อมช่วยผู้ดูแล",4);}
    public void ResetCondition(){StopAllCoroutines();repairedAt=-100;attacks=0;physicalImpacts=0;pattern=0;stage=0;cooling=0;repaired=false;active=false;attackAt=-1;recoverUntil=0;warning.gameObject.SetActive(false);body.localPosition=bodyStart;leftArm.localRotation=leftStart;rightArm.localRotation=rightStart;for(int i=0;i<scrap.Length;i++)if(scrap[i]){var rb=scrap[i].GetComponent<Rigidbody>();if(rb){rb.isKinematic=false;rb.position=scrapStart[i];rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;}scrap[i].position=scrapStart[i];}Tint(false);}
    public void RestoreInstant(){repairedAt=-100;stage=3;cooling=1;repaired=true;active=false;attackAt=-1;warning.gameObject.SetActive(false);body.localPosition=bodyStart;leftArm.localRotation=leftStart;rightArm.localRotation=rightStart;Tint(true);}
    void Update(){
        if(!world||!world.player||world.player.paused||world.finished)return;var p=world.player;
        if(repaired){Tint(true);body.localPosition=bodyStart+Vector3.up*Mathf.Sin(Time.time*1.8f)*.08f;rightArm.localRotation=rightStart*Quaternion.Euler(0,0,Mathf.Sin(Time.time*2)*12);return;}
        Vector3 flat=p.transform.position-arenaCenter;flat.y=0;
        bool commissioned=true;foreach(var repair in requiredRepairs)if(repair&&repair.State!=PuzzleState.Solved)commissioned=false;
        if(!active && commissioned && flat.magnitude<arenaRadius-2 && p.transform.position.z>arenaCenter.z-15){active=true;world.checkpoint=checkpoint;nextAttack=Time.time+3;p.Notify("ผู้ดูแลเสีย! ยิงเฟืองเข้าช่องติดขัด → เปิดปั๊มน้ำ → ใส่เฟืองและเชื่อมซ่อม",6);}
        if(!active)return;
        body.localPosition=bodyStart+Vector3.up*Mathf.Sin(Time.time*4)*.13f;Tint(false);
        if(leftBrow)leftBrow.localRotation=Quaternion.Euler(0,0,18);if(rightBrow)rightBrow.localRotation=Quaternion.Euler(0,0,-18);
        if(Time.time<recoverUntil)return;
        if(attackAt<0 && Time.time>=nextAttack){target=p.transform.position;target.y=.03f;pattern=attacks%2;attackAt=Time.time+windup;warning.gameObject.SetActive(true);warning.position=target;warning.localScale=pattern==0?new Vector3(5.8f,.04f,5.8f):new Vector3(arenaRadius*1.75f,.04f,2.4f);p.Notify(pattern==0?"ระวังมือตบ! ออกจากวงเหลือง":"ระวังแขนกวาด! กระโดดหรือออกจากแถบเหลือง",2);}
        if(attackAt>=0){float t=1-Mathf.Clamp01((attackAt-Time.time)/windup);leftArm.localRotation=leftStart*Quaternion.Euler(0,Mathf.Sin(t*Mathf.PI)*18,-55*t);rightArm.localRotation=rightStart*Quaternion.Euler(0,-Mathf.Sin(t*Mathf.PI)*18,55*t);
            if(Time.time>=attackAt){attacks++;Vector3 d=p.transform.position-target;bool hit=pattern==0?new Vector2(d.x,d.z).magnitude<2.7f:Mathf.Abs(d.z)<1.2f&&Mathf.Abs(d.x)<arenaRadius*.875f;if(hit&&p.transform.position.y<1.5f){world.Respawn();return;}attackAt=-1;warning.gameObject.SetActive(false);leftArm.localRotation=leftStart;rightArm.localRotation=rightStart;recoverUntil=Time.time+1.5f;nextAttack=Time.time+attackInterval;VfxKit.Play(k=>k.Burst(target,new Color(.8f,.65f,.3f),24,3,.15f,.6f));}
        }
    }
    public void ScrapImpact(Rigidbody rb,float speed){if(!active||repaired||stage!=0||!rb||!rb.GetComponent<BossRepairPart>()||speed<4)return;physicalImpacts++;stage=1;recoverUntil=Time.time+3;world.player.Notify("เฟืองที่ติดหลุดแล้ว! เปิดปั๊มน้ำด้านซ้ายให้เครื่องเย็น",5);VfxKit.Play(k=>k.Burst(transform.position+Vector3.up*5,new Color(1,.8f,.3f),32,5,.15f,.7f));}
    public void Service(int station,BlobProperty element,string combo){if(!active||repaired)return;
        if(station==1 && stage==1 && element==BlobProperty.Water){cooling=Mathf.Min(1,cooling+.22f);VfxKit.Play(k=>k.Stroke(arenaCenter+new Vector3(-9,1,1),transform.position+new Vector3(0,4,-2),new Color(.2f,.8f,.9f),.22f,.35f));if(cooling>=1){stage=2;world.player.Notify("เครื่องเย็นแล้ว! ย้ายเฟืองอะไหล่ลงช่องโต๊ะซ่อม แล้วใช้ไฟฟ้า+ความร้อนเชื่อม",6);}}
        if(station==2 && stage==2 && ComboTable.NormalizeId(combo)==ComboTable.Id(BlobProperty.Electric,BlobProperty.Heat)){
            Rigidbody fitted=null;foreach(var part in FindObjectsByType<BossRepairPart>(FindObjectsSortMode.None)){var rb=part.GetComponent<Rigidbody>();if(part.forRepair && rb && Vector3.Distance(rb.worldCenterOfMass,repairSocket.position)<1.35f && rb.linearVelocity.magnitude<3)fitted=rb;}
            if(!fitted){world.player.Notify("ยังไม่มีเฟืองอะไหล่ในช่องซ่อม",2);return;}RestoreInstant();repairedAt=Time.time;StartCoroutine(FitGear(fitted));world.player.Notify("ซ่อมสำเร็จ! ผู้ดูแลกลับมาแล้ว ขอบคุณที่ช่วยโรงงาน",6);}
    }
    IEnumerator FitGear(Rigidbody gear){gear.isKinematic=true;Vector3 start=gear.position;for(float t=0;t<1;t+=Time.deltaTime/1.4f){gear.position=Vector3.Lerp(start,transform.position+new Vector3(0,4,-3.1f),Mathf.SmoothStep(0,1,t))+Vector3.up*Mathf.Sin(t*Mathf.PI)*3;gear.rotation=Quaternion.Euler(0,t*540,0);yield return null;}gear.position=transform.position+new Vector3(0,4,-3.1f);VfxKit.Play(k=>k.Burst(gear.position,new Color(.6f,1,.8f),30,3,.15f,.8f));}
    void Tint(bool clean){foreach(var r in bodyRenderers){if(!r)continue;r.GetPropertyBlock(mpb);mpb.SetColor("_BaseColor",clean?Color.Lerp(new Color(.72f,.52f,.3f),new Color(.55f,.76f,.72f),Mathf.Clamp01((Time.time-repairedAt)/1.4f)):Color.Lerp(new Color(.52f,.64f,.61f),new Color(.8f,.38f,.28f),stage==0?.3f:0));r.SetPropertyBlock(mpb);}foreach(var r in eyes){if(!r)continue;r.GetPropertyBlock(mpb);mpb.SetColor("_BaseColor",clean?new Color(.6f,1,.8f):new Color(1,.74f,.25f));r.SetPropertyBlock(mpb);}if(clean){if(leftBrow)leftBrow.localRotation=Quaternion.identity;if(rightBrow)rightBrow.localRotation=Quaternion.identity;}}
}
}
