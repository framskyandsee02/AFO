using UnityEngine;
namespace OopsPlanet {
// Tunable data for one absorbable power. Logic lives in BlobPowers (Magnet, Pressure) and BlobPlayer (Bounce movement).
[CreateAssetMenu(menuName="OOPS Planet/Power Definition",fileName="Power_")]
public class PowerDefinition : ScriptableObject {
    public BlobProperty property;
    public string nameTh, nameEn;
    [TextArea] public string hintTh;
    [Header("Magnet / Pressure")]
    public float range=8f;
    public float force=18f;
    [Tooltip("Heaviest rigidbody (kg) this power can move.")]
    public float massLimit=10f;
    [Tooltip("Degrees from the camera centre a target may be.")]
    public float aimCone=15f;
    public float cooldown;
    [Header("Bounce")]
    public float minLaunch=9f, maxLaunch=16f, chargeSeconds=.8f;
    [Tooltip("Fraction of landing speed returned on an automatic rebound (<1 so bounces always decay).")]
    [Range(0,.9f)] public float rebound=.45f;
    public int maxRebounds=2;
    public float reboundMinImpact=7f;
}
}
