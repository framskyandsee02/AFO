using UnityEngine;

/// <summary>Gradual cross-dissolve of Claude's authored tree phases; gameplay remains in TimeTarget.</summary>
public sealed class TreeAgeVisuals : MonoBehaviour
{
    public TimeTarget target;
    public GameObject[] phases = new GameObject[0];
    public Shader transitionShader;
    private Renderer[][] renderers;
    private Collider[][] colliders;
    private Material[][][] originals;
    private readonly System.Collections.Generic.List<Material> temporary = new System.Collections.Generic.List<Material>();
    private MaterialPropertyBlock block;
    private void Awake()
    {
        if (target == null) target = GetComponent<TimeTarget>();
        block = new MaterialPropertyBlock(); renderers = new Renderer[phases.Length][];
        colliders = new Collider[phases.Length][]; originals = new Material[phases.Length][][];
        for (int i = 0; i < phases.Length; i++)
        {
            renderers[i] = phases[i].GetComponentsInChildren<Renderer>(true);
            colliders[i] = phases[i].GetComponentsInChildren<Collider>(true);
            originals[i] = new Material[renderers[i].Length][];
            for (int j = 0; j < renderers[i].Length; j++)
            {
                var slots = renderers[i][j].sharedMaterials; originals[i][j] = (Material[])slots.Clone();
                if (transitionShader == null) continue;
                for (int k = 0; k < slots.Length; k++)
                {
                    var original = slots[k]; if (original == null) continue;
                    Color color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.color;
                    var material = new Material(transitionShader); material.SetColor("_BaseColor", color);
                    if (original.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
                    else if (original.HasProperty("_MainTex")) material.SetTexture("_BaseMap", original.mainTexture);
                    slots[k] = material; temporary.Add(material);
                }
                renderers[i][j].sharedMaterials = slots;
            }
        }
    }
    private void OnEnable() { if (target != null) target.OnAgeChanged += Refresh; }
    private void Start() { if (target != null) Refresh(target); }
    private void OnDisable() { if (target != null) target.OnAgeChanged -= Refresh; }
    private void Refresh(TimeTarget value)
    {
        if (renderers == null || phases.Length == 0) return;
        int lower = Mathf.Clamp(Mathf.FloorToInt(value.AgePosition), 0, phases.Length - 1);
        int upper = Mathf.Min(lower + 1, phases.Length - 1);
        float t = Mathf.SmoothStep(0f, 1f, value.AgePosition - lower);
        for (int i = 0; i < phases.Length; i++)
        {
            float visibility = i == lower ? 1f - t : i == upper ? t : 0f;
            if (lower == upper && i == lower) visibility = 1f;
            phases[i].SetActive(visibility > 0f);
            foreach (var renderer in renderers[i])
            {
                renderer.GetPropertyBlock(block); block.SetFloat("_PhaseVisibility", visibility); renderer.SetPropertyBlock(block);
            }
            // Showing two meshes must never enable two physical supports.
            foreach (var collider in colliders[i]) collider.enabled = i == value.CurrentStateIndex;
        }
    }
    private void OnDestroy()
    {
        if (renderers != null)
            for (int i = 0; i < renderers.Length; i++)
                for (int j = 0; j < renderers[i].Length; j++)
                    if (renderers[i][j] != null) renderers[i][j].sharedMaterials = originals[i][j];
        foreach (var material in temporary) if (material != null) Destroy(material);
    }
}
