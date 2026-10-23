using UnityEngine;

namespace WobbleWorks.Characters
{
    /// <summary>Appearance data only; gameplay remains the owner of the active property.</summary>
    [CreateAssetMenu(menuName = "Pino/Character Material Profile", fileName = "PinoMaterialProfile")]
    public sealed class CharacterMaterialProfile : ScriptableObject
    {
        public WobProperty form;
        [Tooltip("Optional replacement body material. The texture remains on this shared material.")]
        public Material material;
        public Color tint = Color.white;
        [Range(0f, 1f)] public float opacity = 1f;
    }
}
