using UnityEngine;
namespace OopsPlanet {
// Only decorative debris moves. Physical routes, sources and puzzle pieces are never cleanup targets.
public class FactoryRestoration : MonoBehaviour {
    public PuzzleObjective objective;public Transform[] debris=new Transform[0];public Vector3[] destinations=new Vector3[0];
    public Collider[] obstacles=new Collider[0];bool[] obstacleStates;
    public Renderer[] surfaces=new Renderer[0];public Light[] roomLights=new Light[0];
    public Transform arm,rotor;public ParticleSystem dust;public float seconds=3.5f;
    public string roomName="";public bool Restored{get;private set;}public float Progress{get;private set;}
    Vector3[] start,scales;Color[] colors;float[] brightness;Quaternion armStart;MaterialPropertyBlock block;bool captured;
    void Awake(){Capture();if(objective){objective.Solved+=Solved;objective.WasReset+=Reset;} }
    void Start(){if(objective && objective.State==PuzzleState.Solved)Apply(true);}
    void Capture(){if(captured)return;captured=true;obstacleStates=new bool[obstacles.Length];for(int i=0;i<obstacles.Length;i++)if(obstacles[i])obstacleStates[i]=obstacles[i].enabled;block=new MaterialPropertyBlock();start=new Vector3[debris.Length];scales=new Vector3[debris.Length];for(int i=0;i<debris.Length;i++)if(debris[i]){start[i]=debris[i].position;scales[i]=debris[i].localScale;}colors=new Color[surfaces.Length];for(int i=0;i<surfaces.Length;i++)if(surfaces[i]&&surfaces[i].sharedMaterial)colors[i]=surfaces[i].sharedMaterial.GetColor("_BaseColor");brightness=new float[roomLights.Length];for(int i=0;i<roomLights.Length;i++)if(roomLights[i])brightness[i]=roomLights[i].intensity;if(arm)armStart=arm.localRotation;}
    void Solved(PuzzleObjective p){Apply(p.LastSolveFromSave);}
    void Reset(PuzzleObjective p){if(p.State==PuzzleState.Solved)return;Restored=false;Progress=0;Blend(0);if(dust)dust.Play();}
    void OnDestroy(){if(objective){objective.Solved-=Solved;objective.WasReset-=Reset;}}
    public void Apply(bool instant){Capture();Restored=true;if(instant){Progress=1;Blend(1);}else{Progress=0;if(dust)dust.Play();}}
    void Update(){if(!Restored)return;if(Progress<1){Progress=Mathf.Min(1,Progress+Time.deltaTime/Mathf.Max(.1f,seconds));Blend(Mathf.SmoothStep(0,1,Progress));}else if(rotor)rotor.Rotate(Vector3.up,25*Time.deltaTime,Space.Self);}
    void Blend(float t){
        for(int i=0;i<obstacles.Length;i++)if(obstacles[i])obstacles[i].enabled=t<1 && obstacleStates[i];
        for(int i=0;i<debris.Length;i++){var d=debris[i];if(!d)continue;d.gameObject.SetActive(t<1);Vector3 end=i<destinations.Length?destinations[i]:start[i]+Vector3.down*.2f;d.position=Vector3.Lerp(start[i],end,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.65f;d.localScale=scales[i]*Mathf.Lerp(1,.12f,t);}
        for(int i=0;i<surfaces.Length;i++){var r=surfaces[i];if(!r)continue;Color clean=Color.Lerp(colors[i],new Color(.87f,.88f,.77f),.23f);r.GetPropertyBlock(block);block.SetColor("_BaseColor",Color.Lerp(colors[i],clean,t));r.SetPropertyBlock(block);}
        for(int i=0;i<roomLights.Length;i++)if(roomLights[i]){roomLights[i].intensity=Mathf.Lerp(brightness[i],brightness[i]*1.7f,t);roomLights[i].color=Color.Lerp(new Color(.55f,.67f,.7f),new Color(1,.84f,.61f),t);}
        if(arm)arm.localRotation=armStart*Quaternion.Euler(0,Mathf.Sin(t*Mathf.PI)*25,Mathf.Sin(t*Mathf.PI*2)*14);
        if(dust){var emission=dust.emission;emission.rateOverTime=9*(1-t);if(t>=1)dust.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
    }
}
}
