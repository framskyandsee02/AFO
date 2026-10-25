using UnityEngine;
using UnityEngine.Rendering;

namespace BorrowedTime {
// Pooled visual feedback: life travels from the plant into the gauntlet.
// This component never changes energy, target health or puzzle state.
[DefaultExecutionOrder(100)]
public class BTLifeFlow : MonoBehaviour {
    public Material flowMaterial;
    const int Strands=3, Samples=28, MotesPerStrand=6;
    BTPlayer player;
    BTTimeTarget source;
    LineRenderer[] ribbons;
    Transform[] motes;
    GameObject effectRoot;
    Material material;
    float lastFeed=-100, visibility;
    public bool IsFlowing => source && Time.time-lastFeed<.10f && CanShow();
    bool CanShow() => player && player.game && !player.paused && !player.overview && player.game.stage>0 && !player.game.completed && !BorrowedTime.Menu.GamePauseMenu.BlocksInputFor(player);
    void Awake(){player=GetComponent<BTPlayer>();}
    public void Feed(BTTimeTarget plant){source=plant;lastFeed=Time.time;}
    void CreateEffect(){
        effectRoot=new GameObject("Life energy flowing from plant");effectRoot.layer=2;
        var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if(!shader)shader=Shader.Find("Sprites/Default");
        material=flowMaterial?new Material(flowMaterial):new Material(shader);
        material.name="Living energy soft additive";
        if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",new Color(.45f,1,.62f,.9f));
        if(material.HasProperty("_Surface"))material.SetFloat("_Surface",1);
        if(material.HasProperty("_SrcBlend"))material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        if(material.HasProperty("_DstBlend"))material.SetFloat("_DstBlend",(float)BlendMode.One);
        if(material.HasProperty("_ZWrite"))material.SetFloat("_ZWrite",0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
        ribbons=new LineRenderer[Strands];motes=new Transform[Strands*MotesPerStrand];
        for(int i=0;i<Strands;i++){
            var go=new GameObject("Curved life stream "+i);go.layer=2;go.transform.SetParent(effectRoot.transform,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.positionCount=Samples;line.useWorldSpace=true;
            line.widthCurve=new AnimationCurve(new Keyframe(0,.025f),new Keyframe(.35f,.048f),new Keyframe(1,.012f));
            line.numCapVertices=3;line.numCornerVertices=2;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.62f,1,.22f),0),new GradientColorKey(new Color(.25f,1,.65f),.55f),new GradientColorKey(new Color(.70f,1,.93f),1)},new[]{new GradientAlphaKey(.15f,0),new GradientAlphaKey(.85f,.22f),new GradientAlphaKey(.60f,.85f),new GradientAlphaKey(.05f,1)});line.colorGradient=gradient;ribbons[i]=line;
            for(int j=0;j<MotesPerStrand;j++){
                var mote=GameObject.CreatePrimitive(PrimitiveType.Sphere);mote.name="Life mote toward gauntlet";mote.layer=2;
                Destroy(mote.GetComponent<Collider>());mote.transform.SetParent(effectRoot.transform,false);
                var renderer=mote.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                motes[i*MotesPerStrand+j]=mote.transform;
            }
        }
    }
    Vector3 Point(Vector3 start,Vector3 end,float t,int strand){
        Vector3 delta=end-start;
        Vector3 side=Vector3.Cross(delta.normalized,Vector3.up);
        if(side.sqrMagnitude<.01f)side=Vector3.right;else side.Normalize();
        float envelope=Mathf.Sin(t*Mathf.PI),phase=t*Mathf.PI*3.2f-Time.time*3.3f+strand*2.0944f;
        return Vector3.Lerp(start,end,t)+Vector3.up*(envelope*(.27f+.10f*Mathf.Sin(phase)))+side*(Mathf.Sin(phase)*envelope*.16f);
    }
    void LateUpdate(){
        bool flowing=IsFlowing;
        if(!CanShow()){visibility=0;if(effectRoot)effectRoot.SetActive(false);return;}
        visibility=Mathf.MoveTowards(visibility,flowing?1:0,Time.deltaTime*9);
        if(visibility<=0 || !source){if(effectRoot)effectRoot.SetActive(false);return;}
        if(!effectRoot)CreateEffect();effectRoot.SetActive(true);
        Vector3 start=source.aimPoint?source.aimPoint.position:source.transform.position+Vector3.up;
        Vector3 end=player.rightHand?player.rightHand.position:player.transform.position+Vector3.up;
        for(int i=0;i<Strands;i++){
            ribbons[i].widthMultiplier=visibility;
            // Draw life leaving different parts of the plant's living crown,
            // converging into the gauntlet, rather than firing from the hand.
            Vector3 origin=start+Vector3.up*(.45f+i*.35f)+Vector3.right*((i-1)*.32f);
            for(int j=0;j<Samples;j++)ribbons[i].SetPosition(j,Point(origin,end,j/(Samples-1f),i));
            for(int j=0;j<MotesPerStrand;j++){
                float t=Mathf.Repeat(Time.time*.75f+j/(float)MotesPerStrand+i*.043f,1);
                var mote=motes[i*MotesPerStrand+j];mote.position=Point(origin,end,t,i);
                mote.localScale=Vector3.one*((.035f+.045f*Mathf.Sin(t*Mathf.PI))*visibility);
            }
        }
    }
    void OnGUI(){
        if(!CanShow() || player.hideHud)return;
        Matrix4x4 matrix=GUI.matrix;Color previous=GUI.color;int depth=GUI.depth;
        GUI.matrix=Matrix4x4.identity;GUI.depth=-40;
        float scale=Mathf.Clamp(Screen.height/900f,.7f,1.5f),cx=Screen.width*.5f,cy=Screen.height*.5f;
        var target=player.target;
        Color color=target?(target.IsLiving&&target.absorbAllowed?new Color(.58f,1,.65f):new Color(.55f,.88f,1)):new Color(.96f,.98f,.94f);
        float gap=5*scale,length=7*scale,width=2*scale;
        Rect[] bars={new Rect(cx-gap-length,cy-width*.5f,length,width),new Rect(cx+gap,cy-width*.5f,length,width),new Rect(cx-width*.5f,cy-gap-length,width,length),new Rect(cx-width*.5f,cy+gap,width,length),new Rect(cx-scale,cy-scale,2*scale,2*scale)};
        foreach(var bar in bars){GUI.color=new Color(.025f,.075f,.065f,.8f);GUI.DrawTexture(new Rect(bar.x-scale,bar.y-scale,bar.width+2*scale,bar.height+2*scale),Texture2D.whiteTexture);GUI.color=color;GUI.DrawTexture(bar,Texture2D.whiteTexture);}
        GUI.matrix=matrix;GUI.color=previous;GUI.depth=depth;
    }
    void OnDisable(){visibility=0;lastFeed=-100;if(effectRoot)effectRoot.SetActive(false);}
    void OnDestroy(){if(effectRoot)Destroy(effectRoot);if(material)Destroy(material);}
}
}
