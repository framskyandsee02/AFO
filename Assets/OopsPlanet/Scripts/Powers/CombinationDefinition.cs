using UnityEngine;
namespace OopsPlanet {
public enum ComboEffect { MagnetCannon, PressureDash }
// One supported pair of active properties. Order does not matter; see PowerCombos.Resolve.
[CreateAssetMenu(menuName="OOPS Planet/Combination Definition",fileName="Combo_")]
public class CombinationDefinition : ScriptableObject {
    public BlobProperty a, b;
    public ComboEffect effect;
    public string nameTh, nameEn;
    [TextArea] public string howTh;
    [Tooltip("MagnetCannon: launch speed of the held object. PressureDash: horizontal dash speed.")]
    public float speed=14f;
    [Tooltip("MagnetCannon: extra upward speed. PressureDash: minimum upward speed after the dash.")]
    public float lift=2f;
}
}
