using System;
using UnityEngine;
using OopsPlanet;

namespace WobbleWorks.Characters
{
    /// <summary>
    /// Presentation-only blend shape controller. All public weights are normalized 0..1;
    /// Unity's 0..100 blend shape units are used only when writing to a renderer.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(1000)]
    public sealed class PinoMorphController : MonoBehaviour
    {
        public static string ShapeNameFor(BlobProperty form) => form == BlobProperty.Bounce ? "Spring" : form == BlobProperty.Pressure ? "Gas" : form == BlobProperty.Water ? "Liquid" : form.ToString();
        public static readonly string[] ShapeNames =
        {
            "Magnet", "Spring", "SpringCompress", "SpringStretch", "Gas",
            "Liquid", "LiquidFlow", "Absorb", "Idle", "Land"
        };

        const int Magnet = 0, Spring = 1, Compress = 2, Stretch = 3, Gas = 4;
        const int Liquid = 5, Flow = 6, Absorb = 7, Idle = 8, Land = 9;
        const float AbsorptionReactionSeconds = .34f, LandingReactionSeconds = .28f;

        [SerializeField] SkinnedMeshRenderer[] morphRenderers = Array.Empty<SkinnedMeshRenderer>();
        [SerializeField] Renderer[] bodyRenderers = Array.Empty<Renderer>();
        [SerializeField] Renderer[] magnetRenderers = Array.Empty<Renderer>();
        [SerializeField] Renderer[] springRenderers = Array.Empty<Renderer>();
        [SerializeField] CharacterMaterialProfile[] materialProfiles = Array.Empty<CharacterMaterialProfile>();
        // Serialized because a Normal prefab legitimately stores accessory scales of zero.
        [SerializeField, HideInInspector] Vector3[] magnetBaseScales = Array.Empty<Vector3>();
        [SerializeField, HideInInspector] Vector3[] springBaseScales = Array.Empty<Vector3>();
        [SerializeField, Min(0f)] float transitionSeconds = .4f;
        [SerializeField] bool idleEnabled = true;
        [SerializeField, Range(0f, 1f)] float idleAmplitude = .25f;
        [SerializeField, Min(0f)] float idleFrequency = .7f;
        [SerializeField, HideInInspector] BlobProperty currentForm;
        [SerializeField, HideInInspector] BlobProperty targetForm;

        readonly float[] pose = new float[10];
        readonly float[] fromPose = new float[10];
        readonly float[] desiredPose = new float[10];
        [SerializeField, HideInInspector] float[] manualWeights = new float[10];
        [SerializeField, HideInInspector] bool[] manualOverrides = new bool[10];
        [SerializeField, HideInInspector, Range(0f, 1f)] float springCompression;
        [SerializeField, HideInInspector, Range(0f, 1f)] float springStretch;
        [SerializeField, HideInInspector, Range(0f, 1f)] float liquidFlow;
        [SerializeField, HideInInspector, Range(0f, 1f)] float absorptionProgress;
        MorphBinding[] morphBindings = Array.Empty<MorphBinding>();
        BodyBinding[] bodyBindings = Array.Empty<BodyBinding>();
        CharacterMaterialProfile materialOverride;
        float elapsed, duration, visualTime;
        float absorptionAge = float.PositiveInfinity, landingAge = float.PositiveInfinity, landingStrength;
        bool bound, transitioning, visualPaused;

        sealed class MorphBinding
        {
            public SkinnedMeshRenderer renderer;
            public readonly int[] indices = new int[10];
        }

        sealed class BodyBinding
        {
            public Renderer renderer;
            public Material originalMaterial;
            public MaterialPropertyBlock block;
        }

        public BlobProperty CurrentForm => currentForm;
        public BlobProperty TargetForm => targetForm;
        public bool IsTransitioning => transitioning;
        public float TransitionSeconds
        {
            get => transitionSeconds;
            set => transitionSeconds = SafeNonnegative(value);
        }
        public bool IdleEnabled
        {
            get => idleEnabled;
            set { idleEnabled = value; ApplyIfEditing(); }
        }

        void Awake() => EnsureBound();
        // Property blocks are not serialized by Unity. Reapply appearance when a saved
        // scene/prefab is opened, while preserving its authored morph weights.
        void OnEnable()
        {
            EnsureBound();
            ApplyAccessory(magnetRenderers, magnetBaseScales, pose[Magnet]);
            ApplyAccessory(springRenderers, springBaseScales, pose[Spring]);
            ApplyMaterials();
        }
        void LateUpdate()
        {
            if (Application.isPlaying) Tick(Time.deltaTime);
        }

        /// <summary>Assign imported morph meshes, body materials, and optional ability parts.</summary>
        public void Configure(SkinnedMeshRenderer[] morphs, Renderer[] bodies,
            Renderer[] magnets, Renderer[] springs, CharacterMaterialProfile[] profiles)
        {
            if (bound)
            {
                RestoreAccessoryScales(magnetRenderers, magnetBaseScales);
                RestoreAccessoryScales(springRenderers, springBaseScales);
            }
            morphRenderers = morphs ?? Array.Empty<SkinnedMeshRenderer>();
            bodyRenderers = bodies ?? Array.Empty<Renderer>();
            magnetRenderers = magnets ?? Array.Empty<Renderer>();
            springRenderers = springs ?? Array.Empty<Renderer>();
            materialProfiles = profiles ?? Array.Empty<CharacterMaterialProfile>();
            magnetBaseScales = CaptureAccessoryScales(magnetRenderers);
            springBaseScales = CaptureAccessoryScales(springRenderers);
            bound = false;
            EnsureBound();
            visualTime = 0f;
            absorptionAge = landingAge = float.PositiveInfinity;
            absorptionProgress = 0f;
            SetMorphForm(BlobProperty.None);
        }

        /// <summary>Immediately set a form using the existing Gameplay enum.</summary>
        public void SetMorphForm(BlobProperty form) => BlendToMorphForm(form, 0f);

        /// <summary>
        /// Retarget from the currently displayed pose. Repeated requests for the same target
        /// do not restart an active transition; use SetMorphForm for an immediate pose.
        /// </summary>
        public void BlendToMorphForm(BlobProperty form, float seconds)
        {
            EnsureBound();
            if (!KnownForm(form)) throw new ArgumentOutOfRangeException(nameof(form));
            seconds = SafeNonnegative(seconds);
            if (form == targetForm && transitioning && seconds > 0f) return;
            Array.Copy(pose, fromPose, pose.Length);
            Array.Clear(manualOverrides, 0, manualOverrides.Length);
            materialOverride = null;
            targetForm = form;
            springCompression = springStretch = liquidFlow = 0f;
            elapsed = 0f;
            duration = seconds;
            transitioning = seconds > 0f;
            if (!transitioning) currentForm = form;
            ApplyPose();
        }

        public void ReturnToNormal(float seconds)
        {
            absorptionProgress = 0f;
            absorptionAge = landingAge = float.PositiveInfinity;
            BlendToMorphForm(BlobProperty.None, seconds);
        }

        /// <summary>Set a named shape override in normalized units (0..1), never 0..100.</summary>
        public void SetMorphWeight(string shapeName, float weight)
        {
            EnsureBound();
            int index = ShapeIndex(shapeName);
            if (index < 0) throw new ArgumentException("Unknown Pino morph target: " + shapeName, nameof(shapeName));
            manualOverrides[index] = true;
            manualWeights[index] = Safe01(weight);
            ApplyPose();
        }

        /// <summary>Read the currently displayed normalized weight, including transitions.</summary>
        public float GetMorphWeight(string shapeName)
        {
            EnsureBound();
            int index = ShapeIndex(shapeName);
            if (index < 0) throw new ArgumentException("Unknown Pino morph target: " + shapeName, nameof(shapeName));
            return pose[index];
        }

        public void SetCharacterMaterial(CharacterMaterialProfile profile)
        {
            EnsureBound();
            materialOverride = profile;
            ApplyMaterials();
        }

        public void SetSpringCompression(float normalized)
        {
            springCompression = Safe01(normalized);
            ApplyIfEditing();
        }
        public void SetSpringStretch(float normalized)
        {
            springStretch = Safe01(normalized);
            ApplyIfEditing();
        }
        public void SetLiquidFlow(float normalized)
        {
            liquidFlow = Safe01(normalized);
            ApplyIfEditing();
        }
        public void SetAbsorptionProgress(float normalized)
        {
            absorptionProgress = Safe01(normalized);
            ApplyIfEditing();
        }
        public void PlayAbsorptionReaction()
        {
            absorptionAge = 0f;
            ApplyIfEditing();
        }
        public void PlayLandingReaction(float strength = 1f)
        {
            landingAge = 0f;
            landingStrength = Safe01(strength);
            ApplyIfEditing();
        }
        public void SetVisualPaused(bool paused) => visualPaused = paused;

        /// <summary>Advance presentation deterministically; performs no gameplay or input work.</summary>
        public void Tick(float deltaTime)
        {
            EnsureBound();
            if (visualPaused) return;
            float dt = SafeNonnegative(deltaTime);
            visualTime += dt;
            if (!float.IsPositiveInfinity(absorptionAge)) absorptionAge += dt;
            if (!float.IsPositiveInfinity(landingAge)) landingAge += dt;
            if (transitioning)
            {
                elapsed = Mathf.Min(duration, elapsed + dt);
                if (elapsed >= duration)
                {
                    transitioning = false;
                    currentForm = targetForm;
                }
            }
            ApplyPose();
        }

        void ApplyIfEditing()
        {
            if (!Application.isPlaying)
            {
                EnsureBound();
                ApplyPose();
            }
        }

        void EnsureBound()
        {
            if (bound) return;
            if (manualWeights == null || manualWeights.Length != ShapeNames.Length)
                manualWeights = new float[ShapeNames.Length];
            if (manualOverrides == null || manualOverrides.Length != ShapeNames.Length)
                manualOverrides = new bool[ShapeNames.Length];
            if (morphRenderers == null || morphRenderers.Length == 0)
                morphRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            morphBindings = new MorphBinding[morphRenderers.Length];
            for (int r = 0; r < morphRenderers.Length; r++)
            {
                var binding = new MorphBinding { renderer = morphRenderers[r] };
                Mesh mesh = binding.renderer ? binding.renderer.sharedMesh : null;
                for (int s = 0; s < ShapeNames.Length; s++)
                    binding.indices[s] = FindBlendShape(mesh, ShapeNames[s]);
                morphBindings[r] = binding;
            }
            bodyRenderers = bodyRenderers ?? Array.Empty<Renderer>();
            bodyBindings = new BodyBinding[bodyRenderers.Length];
            for (int r = 0; r < bodyRenderers.Length; r++)
                bodyBindings[r] = new BodyBinding
                {
                    renderer = bodyRenderers[r],
                    originalMaterial = bodyRenderers[r] ? bodyRenderers[r].sharedMaterial : null,
                    block = new MaterialPropertyBlock()
                };
            magnetRenderers = magnetRenderers ?? Array.Empty<Renderer>();
            springRenderers = springRenderers ?? Array.Empty<Renderer>();
            if (magnetBaseScales == null || magnetBaseScales.Length != magnetRenderers.Length)
                magnetBaseScales = CaptureAccessoryScales(magnetRenderers);
            if (springBaseScales == null || springBaseScales.Length != springRenderers.Length)
                springBaseScales = CaptureAccessoryScales(springRenderers);
            materialProfiles = materialProfiles ?? Array.Empty<CharacterMaterialProfile>();
            // Importers may prepend the mesh/key datablock name to each shape name.
            for (int s = 0; s < ShapeNames.Length; s++)
            {
                for (int r = 0; r < morphBindings.Length; r++)
                {
                    var binding = morphBindings[r];
                    if (binding.renderer && binding.indices[s] >= 0)
                    {
                        pose[s] = Safe01(binding.renderer.GetBlendShapeWeight(binding.indices[s]) / 100f);
                        break;
                    }
                }
            }
            if (!KnownForm(targetForm)) targetForm = BlobProperty.None;
            if (!KnownForm(currentForm)) currentForm = BlobProperty.None;
            bound = true;
        }

        void ApplyPose()
        {
            Array.Clear(desiredPose, 0, desiredPose.Length);
            int formIndex = FormShapeIndex(targetForm);
            if (formIndex >= 0) desiredPose[formIndex] = 1f;
            if (targetForm == BlobProperty.Bounce)
            {
                // Compression and extension are mutually exclusive offsets from Spring.
                desiredPose[Compress] = springCompression;
                desiredPose[Stretch] = springStretch * (1f - springCompression);
            }
            if (targetForm == BlobProperty.Water) desiredPose[Flow] = liquidFlow;
            desiredPose[Absorb] = Mathf.Clamp01(absorptionProgress + .7f * Pulse(absorptionAge, AbsorptionReactionSeconds));
            desiredPose[Idle] = idleEnabled
                ? idleAmplitude * (.5f + .5f * Mathf.Sin(visualTime * idleFrequency * Mathf.PI * 2f))
                : 0f;
            desiredPose[Land] = landingStrength * Pulse(landingAge, LandingReactionSeconds);
            float blend = transitioning ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)) : 1f;
            for (int s = 0; s < ShapeNames.Length; s++)
                pose[s] = manualOverrides[s] ? manualWeights[s] : Mathf.Lerp(fromPose[s], desiredPose[s], blend);
            for (int r = 0; r < morphBindings.Length; r++)
            {
                var binding = morphBindings[r];
                if (!binding.renderer || !binding.renderer.sharedMesh) continue;
                for (int s = 0; s < ShapeNames.Length; s++)
                    if (binding.indices[s] >= 0) binding.renderer.SetBlendShapeWeight(binding.indices[s], pose[s] * 100f);
            }
            ApplyAccessory(magnetRenderers, magnetBaseScales, pose[Magnet]);
            ApplyAccessory(springRenderers, springBaseScales, pose[Spring]);
            ApplyMaterials();
        }

        void ApplyMaterials()
        {
            CharacterMaterialProfile normal = FindProfile(BlobProperty.None);
            Color normalTint = normal ? normal.tint : Color.white;
            float normalOpacity = normal ? normal.opacity : 1f;
            float total = pose[Magnet] + pose[Spring] + pose[Gas] + pose[Liquid];
            float remainder = Mathf.Max(0f, 1f - total);
            float divisor = Mathf.Max(1f, total);
            Color tint = normalTint * remainder;
            float opacity = normalOpacity * remainder;
            AccumulateAppearance(BlobProperty.Magnet, pose[Magnet] / divisor, normalTint, normalOpacity, ref tint, ref opacity);
            AccumulateAppearance(BlobProperty.Bounce, pose[Spring] / divisor, normalTint, normalOpacity, ref tint, ref opacity);
            AccumulateAppearance(BlobProperty.Pressure, pose[Gas] / divisor, normalTint, normalOpacity, ref tint, ref opacity);
            AccumulateAppearance(BlobProperty.Water, pose[Liquid] / divisor, normalTint, normalOpacity, ref tint, ref opacity);
            CharacterMaterialProfile selected = materialOverride ? materialOverride : FindProfile(targetForm);
            // Keep a transparent gas material while fading out Gas, so opacity is meaningful.
            if (!materialOverride && pose[Gas] > .001f) selected = FindProfile(BlobProperty.Pressure) ?? selected;
            if (materialOverride)
            {
                tint = materialOverride.tint;
                opacity = materialOverride.opacity;
            }
            tint.a *= Mathf.Clamp01(opacity);
            for (int r = 0; r < bodyBindings.Length; r++)
            {
                BodyBinding binding = bodyBindings[r];
                if (!binding.renderer) continue;
                Material desiredMaterial = selected && selected.material ? selected.material
                    : normal && normal.material ? normal.material : binding.originalMaterial;
                if (desiredMaterial && binding.renderer.sharedMaterial != desiredMaterial)
                    binding.renderer.sharedMaterial = desiredMaterial;
                binding.renderer.GetPropertyBlock(binding.block);
                binding.block.SetColor("_BaseColor", tint);
                binding.block.SetColor("_Color", tint);
                binding.renderer.SetPropertyBlock(binding.block);
            }
        }

        void AccumulateAppearance(BlobProperty form, float weight, Color fallbackTint,
            float fallbackOpacity, ref Color tint, ref float opacity)
        {
            CharacterMaterialProfile profile = FindProfile(form);
            tint += (profile ? profile.tint : fallbackTint) * weight;
            opacity += (profile ? profile.opacity : fallbackOpacity) * weight;
        }

        CharacterMaterialProfile FindProfile(BlobProperty form)
        {
            for (int i = 0; i < materialProfiles.Length; i++)
                if (materialProfiles[i] && materialProfiles[i].form == form) return materialProfiles[i];
            return null;
        }

        static void ApplyAccessory(Renderer[] renderers, Vector3[] scales, float weight)
        {
            float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(weight));
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer) continue;
                renderer.enabled = amount > .001f;
                renderer.transform.localScale = scales[i] * amount;
            }
        }

        static Vector3[] CaptureAccessoryScales(Renderer[] renderers)
        {
            var scales = new Vector3[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                scales[i] = renderers[i] ? renderers[i].transform.localScale : Vector3.one;
            return scales;
        }
        static void RestoreAccessoryScales(Renderer[] renderers, Vector3[] scales)
        {
            if (renderers == null || scales == null) return;
            for (int i = 0; i < Mathf.Min(renderers.Length, scales.Length); i++)
                if (renderers[i]) renderers[i].transform.localScale = scales[i];
        }

        static int FindBlendShape(Mesh mesh, string name)
        {
            if (!mesh) return -1;
            int exact = mesh.GetBlendShapeIndex(name);
            if (exact >= 0) return exact;
            string suffix = "." + name;
            for (int i = 0; i < mesh.blendShapeCount; i++)
                if (mesh.GetBlendShapeName(i).EndsWith(suffix, StringComparison.Ordinal)) return i;
            return -1;
        }
        static int ShapeIndex(string name)
        {
            for (int i = 0; i < ShapeNames.Length; i++)
                if (string.Equals(ShapeNames[i], name, StringComparison.Ordinal)) return i;
            return -1;
        }
        static int FormShapeIndex(BlobProperty form)
        {
            switch (form)
            {
                case BlobProperty.Magnet: return Magnet;
                case BlobProperty.Bounce: return Spring;
                case BlobProperty.Pressure: return Gas;
                case BlobProperty.Water: return Liquid;
                default: return -1;
            }
        }
        static bool KnownForm(BlobProperty form) => form == BlobProperty.None || form == BlobProperty.Magnet || form == BlobProperty.Bounce || form == BlobProperty.Pressure || form == BlobProperty.Water;
        static float Pulse(float age, float seconds) => age >= 0f && age < seconds ? Mathf.Sin(age / seconds * Mathf.PI) : 0f;
        static float Safe01(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp01(value);
        static float SafeNonnegative(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
}
