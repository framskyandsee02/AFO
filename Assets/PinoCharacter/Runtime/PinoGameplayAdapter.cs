using System;
using UnityEngine;
using OopsPlanet;

namespace WobbleWorks.Characters
{
    /// <summary>
    /// Read-only bridge from the existing BlobPlayer state to Pino's presentation.
    /// It never grants abilities, reads input, changes colliders or moves the player.
    /// Place the character beside player.visual, not below it, to avoid legacy scaling.
    /// </summary>
    [DefaultExecutionOrder(500)]
    [DisallowMultipleComponent]
    public sealed class PinoGameplayAdapter : MonoBehaviour
    {
        [SerializeField] BlobPlayer player;
        [SerializeField] PinoMorphController morph;
        [SerializeField] Transform orientationSource;
        [SerializeField] Vector3 orientationOffsetEuler;
        [SerializeField] bool hideLegacyRenderers = true;
        [Tooltip("Negative uses the existing player's morphSeconds.")]
        [SerializeField] float transitionSeconds = -1f;
        [SerializeField, Min(.01f)] float springReleaseSeconds = .28f;
        [SerializeField, Min(.1f)] float teleportDistance = 2.5f;

        Renderer[] legacyRenderers = Array.Empty<Renderer>();
        bool[] legacyEnabled = Array.Empty<bool>();
        bool legacyCaptured;
        bool synchronized;
        bool hierarchyWarning;
        BlobProperty previousProperty;
        float previousCharge;
        float previousAbsorb;
        float previousVerticalSpeed;
        bool previousGrounded;
        bool previouslyPaused;
        Vector3 previousPosition;
        float releasePulse;

        // Only authored forms are selected; Heat/Electric retain the normal mesh.
        public static BlobProperty FormFor(BlobProperty value) => value == BlobProperty.Magnet || value == BlobProperty.Bounce || value == BlobProperty.Pressure || value == BlobProperty.Water ? value : BlobProperty.None;
        public BlobPlayer Player => player;
        public PinoMorphController Morph => morph;

        public void Configure(BlobPlayer source, PinoMorphController character, Transform orientation = null)
        {
            RestoreLegacyRenderers();
            player = source;
            morph = character;
            orientationSource = orientation ? orientation : source ? source.visual : null;
            synchronized = false;
            hierarchyWarning = false;
            releasePulse = 0f;
        }

        void Awake()
        {
            if (!morph) morph = GetComponent<PinoMorphController>();
            if (!player) player = GetComponentInParent<BlobPlayer>();
            if (!orientationSource && player) orientationSource = player.visual;
        }

        void OnEnable() { synchronized = false; }

        void LateUpdate()
        {
            if (Application.isPlaying) SyncNow();
        }

        /// <summary>
        /// Samples committed gameplay state. Explicit calls also work in editor validation.
        /// Initial synchronization is immediate; later changes preserve transition continuity.
        /// </summary>
        public void SyncNow(bool immediate = false, float deltaTime = -1f)
        {
            if (!player || !morph) return;
            if (!orientationSource) orientationSource = player.visual;
            if (player.visual && (morph.transform == player.visual || morph.transform.IsChildOf(player.visual)))
            {
                RestoreLegacyRenderers();
                if (!hierarchyWarning)
                {
                    Debug.LogError("PinoVisual must be a sibling of BlobPlayer.visual; a child would inherit the legacy morph scale.", this);
                    hierarchyWarning = true;
                }
                return;
            }

            float dt = Mathf.Max(0f, deltaTime >= 0f ? deltaTime : Time.deltaTime);
            BlobProperty form = FormFor(player.property);
            float charge = Mathf.Clamp01(player.bounceCharge);
            float progress = Mathf.Clamp01(player.absorbProgress);
            bool grounded = player.Grounded;
            bool paused = player.paused || player.completed;
            Vector3 position = player.transform.position;
            bool teleported = synchronized && (position - previousPosition).sqrMagnitude > teleportDistance * teleportDistance;

            HideLegacyVisual();
            if (orientationSource && orientationSource != morph.transform)
                morph.transform.rotation = Quaternion.Euler(0f, orientationSource.eulerAngles.y, 0f) * Quaternion.Euler(orientationOffsetEuler);

            morph.SetVisualPaused(paused);
            bool firstSample = !synchronized;
            bool changed = !firstSample && form != previousProperty;
            if (firstSample || immediate)
                morph.SetMorphForm(form);
            else if (changed)
            {
                morph.BlendToMorphForm(form, transitionSeconds >= 0f ? transitionSeconds : Mathf.Max(0f, player.morphSeconds));
                if (form != BlobProperty.None && !paused && !teleported) morph.PlayAbsorptionReaction();
                releasePulse = 0f;
            }

            if (!firstSample && !paused && !previouslyPaused && !teleported && !immediate)
            {
                if (progress > 0f && previousAbsorb <= 0f && !changed) morph.PlayAbsorptionReaction();
                // Gameplay exposes no release event. Infer a visual-only pulse from
                // charge discharge AND take-off; never treat charge reset alone as a launch.
                bool springTakeoff = previousProperty == BlobProperty.Bounce && form == BlobProperty.Bounce &&
                    previousCharge > .01f && charge <= .001f && previousGrounded && !grounded && player.velocity.y > .1f;
                if (springTakeoff) releasePulse = 1f;
                else releasePulse = Mathf.MoveTowards(releasePulse, 0f, dt / Mathf.Max(.01f, springReleaseSeconds));

                if (!previousGrounded && grounded)
                    morph.PlayLandingReaction(Mathf.Clamp01(Mathf.Abs(previousVerticalSpeed) / 10f));
            }
            if (teleported || form != BlobProperty.Bounce) releasePulse = 0f;

            morph.SetSpringCompression(form == BlobProperty.Bounce ? charge : 0f);
            morph.SetSpringStretch(form == BlobProperty.Bounce ? releasePulse : 0f);
            morph.SetLiquidFlow(0f);
            morph.SetAbsorptionProgress(progress);

            previousProperty = form;
            previousCharge = charge;
            previousAbsorb = progress;
            previousGrounded = grounded;
            previousVerticalSpeed = player.velocity.y;
            previousPosition = position;
            previouslyPaused = paused;
            synchronized = true;
        }

        void HideLegacyVisual()
        {
            if (!hideLegacyRenderers || !player.visual)
            {
                RestoreLegacyRenderers();
                return;
            }
            if (!legacyCaptured)
            {
                legacyRenderers = player.visual.GetComponentsInChildren<Renderer>(true);
                legacyEnabled = new bool[legacyRenderers.Length];
                for (int i = 0; i < legacyRenderers.Length; i++) legacyEnabled[i] = legacyRenderers[i] && legacyRenderers[i].enabled;
                legacyCaptured = true;
            }
            foreach (Renderer renderer in legacyRenderers)
                if (renderer && renderer.transform != morph.transform && !renderer.transform.IsChildOf(morph.transform)) renderer.enabled = false;
        }

        void RestoreLegacyRenderers()
        {
            if (!legacyCaptured) return;
            for (int i = 0; i < legacyRenderers.Length; i++)
                if (legacyRenderers[i]) legacyRenderers[i].enabled = legacyEnabled[i];
            legacyRenderers = Array.Empty<Renderer>();
            legacyEnabled = Array.Empty<bool>();
            legacyCaptured = false;
        }

        void OnDisable()
        {
            RestoreLegacyRenderers();
            if (morph) morph.SetVisualPaused(false);
            synchronized = false;
        }

        void OnDestroy() { RestoreLegacyRenderers(); }
    }
}
