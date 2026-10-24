using UnityEngine;
namespace OopsPlanet {
// v2 seamless absorbables (Docs/DESIGN_V2_TH.md §3.3): no permanent labels. An available source gives a faint
// "breathing" shimmer every few seconds; in sense mode (BlobSense) it glows in its element colour and shows its
// Thai name; a drained (Deplete/consumed) source looks greyed out. Colours are set per material slot.
[RequireComponent(typeof(BlobInteractable))]
public class AbsorbableVisual : MonoBehaviour {
    public float shimmerPeriod=3.5f, shimmerStrength=.22f;
    [Tooltip("Grey a drained source out. Off for appliances that swap to their own drained model (ApplianceState).")]
    public bool greyWhenDrained=true;
    public BlobInteractable source;
    public Renderer[] renderers;
    public bool sensed;          // currently highlighted by sense mode (read by tests)
    Color[][] baseColors, baseEmission; MaterialPropertyBlock mpb; float phase;
    void Awake() {
        if(!source) source=GetComponent<BlobInteractable>();
        if(renderers==null||renderers.Length==0) renderers=(source.fullVisual?source.fullVisual:gameObject).GetComponentsInChildren<Renderer>(true);
        baseColors=new Color[renderers.Length][];baseEmission=new Color[renderers.Length][];
        for(int i=0;i<renderers.Length;i++) {
            var mats=renderers[i].sharedMaterials; baseColors[i]=new Color[mats.Length];baseEmission[i]=new Color[mats.Length];
            for(int j=0;j<mats.Length;j++) baseColors[i][j]=mats[j]&&mats[j].HasProperty("_BaseColor")?mats[j].GetColor("_BaseColor"):Color.white;
        }
        for(int i=0;i<renderers.Length;i++) { var mats=renderers[i].sharedMaterials;for(int j=0;j<mats.Length;j++)baseEmission[i][j]=mats[j] && mats[j].HasProperty("_EmissionColor")?mats[j].GetColor("_EmissionColor"):Color.black; }
        mpb=new MaterialPropertyBlock(); phase=Random.value*shimmerPeriod;
        if(source.powerLabel) source.powerLabel.SetActive(false);
    }
    void OnEnable() { if(mpb==null || baseColors==null || baseEmission==null) Awake(); }
    void Update() {
        if(!source) return;
        if(mpb==null || baseColors==null || baseEmission==null) Awake();
        bool available=source.Available;
        sensed=available && BlobSense.Active && Vector3.Distance(BlobSense.Origin,source.AimPoint)<=BlobSense.Range;
        if(source.powerLabel && source.powerLabel.activeSelf!=sensed) source.powerLabel.SetActive(sensed);
        Color element=BlobPlayer.PropertyColor(source.property);
        float t=((Time.time+phase)%shimmerPeriod)/shimmerPeriod;
        float shimmer=available && t<.18f ? Mathf.Sin(t/.18f*Mathf.PI)*shimmerStrength : 0;
        for(int i=0;i<renderers.Length;i++) {
            var r=renderers[i]; if(!r) continue;
            for(int j=0;j<baseColors[i].Length;j++) {
                Color c=baseColors[i][j];
                if(sensed) c=Color.Lerp(c,element,.55f)*1.35f;
                else if(!available && greyWhenDrained) c=Color.Lerp(c,new Color(.4f,.4f,.42f),.6f);
                else if(shimmer>0) c=Color.Lerp(c,Color.white,shimmer);
                r.GetPropertyBlock(mpb,j); mpb.SetColor("_BaseColor",c); mpb.SetColor("_EmissionColor",available?baseEmission[i][j]:Color.black); r.SetPropertyBlock(mpb,j);
            }
        }
    }
}
}
