using UnityEngine;
namespace OopsPlanet {
// A point the Magnet slingshot (Magnet + Bounce) can pull BLOB toward: placed on metal beams, or left by an iron
// nail (Pressure + Magnet) in wood. Temporary anchors expire.
public class MagnetAnchor : MonoBehaviour {
    public static readonly System.Collections.Generic.List<MagnetAnchor> All=new System.Collections.Generic.List<MagnetAnchor>();
    public float expiresAt;              // 0 = permanent
    public static int created;
    void OnEnable() { if(!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }
    void Update() { if(expiresAt>0 && Time.time>=expiresAt) Destroy(gameObject); }
    public static MagnetAnchor Create(Vector3 pos,float seconds) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name="Iron nail anchor";
        Object.Destroy(go.GetComponent<Collider>()); go.layer=2;
        go.transform.position=pos; go.transform.localScale=new Vector3(.12f,.18f,.12f);
        var mpb=new MaterialPropertyBlock(); mpb.SetColor("_BaseColor",BlobPlayer.PropertyColor(BlobProperty.Magnet));
        go.GetComponent<Renderer>().SetPropertyBlock(mpb);
        var a=go.AddComponent<MagnetAnchor>(); a.expiresAt=seconds>0?Time.time+seconds:0; created++;
        return a;
    }
    public static void ClearTemporary() { foreach(var a in All.ToArray()) if(a.expiresAt>0) Destroy(a.gameObject); }
}
}
