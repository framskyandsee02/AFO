using System;
using UnityEngine;
using OopsPlanet;

namespace WobbleWorks.Characters
{
    /// <summary>Small presentation-only hops and resting jelly motion; never moves the player or collider.</summary>
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(1100)]
    public sealed class PinoLocomotionAnimator : MonoBehaviour
    {
        [SerializeField] Transform motionPivot;
        [SerializeField] PinoMorphController morph;
        [SerializeField, Min(0f)] float characterHeight = .52f;
        [SerializeField, Range(0f, .12f)] float hopHeightRatio = .055f;
        [SerializeField, Min(.1f)] float hopsPerSecond = 2.15f;
        [SerializeField, Min(.1f)] float referenceWalkSpeed = 5.5f;
        [SerializeField, Min(.01f)] float walkBlendSeconds = .18f;
        [SerializeField, Range(0f, .15f)] float walkSquash = .065f;
        [SerializeField, Range(0f, .08f)] float idleSquish = .025f;
        [SerializeField, Range(0f, 5f)] float idleSwayDegrees = 1.6f;
        [Serializable] struct MotionStyle
        {
            [Min(0f)] public float hop, squash, idle, sway, cadence;
            public MotionStyle(float hop, float squash, float idle, float sway, float cadence)
            { this.hop = hop; this.squash = squash; this.idle = idle; this.sway = sway; this.cadence = cadence; }
        }
        [Header("Form motion multipliers")]
        [SerializeField] MotionStyle magnetMotion = new MotionStyle(.68f, .72f, .72f, .7f, .88f);
        [SerializeField] MotionStyle springMotion = new MotionStyle(1.08f, 1.35f, 1.15f, .8f, 1.1f);
        [SerializeField] MotionStyle gasMotion = new MotionStyle(.75f, .4f, .8f, 1.1f, .72f);
        [SerializeField] MotionStyle liquidMotion = new MotionStyle(.15f, .4f, .65f, .18f, .9f);
        [SerializeField, HideInInspector] Vector3 restPosition;
        [SerializeField, HideInInspector] Quaternion restRotation = Quaternion.identity;
        [SerializeField, HideInInspector] Vector3 restScale = Vector3.one;
        [SerializeField, HideInInspector] bool hasRestPose;

        PinoGameplayAdapter adapter;
        Vector3 previousPlayerPosition;
        bool sampledPosition;
        float walkBlend, idleBlend, walkPhase, idleTime;

        public Transform MotionPivot => motionPivot;
        public float CurrentHopHeight { get; private set; }

        public void Configure(Transform pivot, PinoMorphController character, float height = .52f)
        {
            RestoreRestPose();
            motionPivot = pivot;
            morph = character;
            characterHeight = Mathf.Max(.001f, height);
            hasRestPose = motionPivot;
            if (motionPivot)
            {
                restPosition = motionPivot.localPosition;
                restRotation = motionPivot.localRotation;
                restScale = motionPivot.localScale;
            }
            ResetMotion();
        }

        void Awake()
        {
            if (!morph) morph = GetComponent<PinoMorphController>();
            adapter = GetComponent<PinoGameplayAdapter>();
        }

        void OnEnable() => sampledPosition = false;

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (!adapter) adapter = GetComponent<PinoGameplayAdapter>();
            BlobPlayer player = adapter ? adapter.Player : null;
            Vector3 velocity = Vector3.zero;
            float dt = Time.deltaTime;
            if (player)
            {
                Vector3 position = player.transform.position;
                Vector3 displacement = sampledPosition ? position - previousPlayerPosition : Vector3.zero;
                previousPlayerPosition = position;
                sampledPosition = true;
                // Teleport/respawn is not a walking step.
                if (displacement.sqrMagnitude > 2.5f * 2.5f) ResetMotion();
                else if (dt > .00001f) velocity = displacement / dt;
            }
            Tick(dt, velocity, !player || player.Grounded, player && (player.paused || player.completed));
        }

        /// <summary>Sample actual planar movement, not input intent. Also usable for deterministic previews.</summary>
        public void Tick(float deltaTime, Vector3 worldVelocity, bool grounded, bool paused = false)
        {
            if (!motionPivot || !morph || !hasRestPose || paused) return;
            float dt = Finite(deltaTime) ? Mathf.Max(0f, deltaTime) : 0f;
            float speed = new Vector2(worldVelocity.x, worldVelocity.z).magnitude;
            if (!Finite(speed)) speed = 0f;
            float speedRatio = Mathf.Clamp01(speed / Mathf.Max(.1f, referenceWalkSpeed));
            float magnet = morph.GetMorphWeight("Magnet"), spring = morph.GetMorphWeight("Spring");
            float gas = morph.GetMorphWeight("Gas"), liquid = morph.GetMorphWeight("Liquid");
            float total = magnet + spring + gas + liquid;
            float normalWeight = Mathf.Max(0f, 1f - total), divisor = Mathf.Max(1f, total);
            var style = new MotionStyle(normalWeight, normalWeight, normalWeight, normalWeight, normalWeight);
            Accumulate(ref style, magnetMotion, magnet / divisor);
            Accumulate(ref style, springMotion, spring / divisor);
            Accumulate(ref style, gasMotion, gas / divisor);
            Accumulate(ref style, liquidMotion, liquid / divisor);
            float flow = Mathf.Clamp01(morph.GetMorphWeight("LiquidFlow"));
            float charge = Mathf.Clamp01(morph.GetMorphWeight("SpringCompress"));
            float walkTarget = grounded && speed > .08f ? speedRatio : 0f;
            walkBlend = Mathf.MoveTowards(walkBlend, walkTarget, dt / Mathf.Max(.01f, walkBlendSeconds));
            if (!grounded) walkBlend = 0f;
            float idleTarget = (grounded || gas > .01f) && morph.IdleEnabled ? 1f - walkBlend : 0f;
            idleBlend = Mathf.MoveTowards(idleBlend, idleTarget, dt / .35f);
            walkPhase = Mathf.Repeat(walkPhase + dt * hopsPerSecond * style.cadence * Mathf.Lerp(.7f, 1f, speedRatio), 1f);
            idleTime += dt;

            // A short contact pose surrounds a soft, low arc. Feet never receive a downward translation.
            float arc = Mathf.Sin(Mathf.PI * Mathf.Clamp01((walkPhase - .16f) / .7f));
            arc = Mathf.Pow(Mathf.Max(0f, arc), 1.35f);
            float contact = 1f - Mathf.SmoothStep(0f, .3f, arc);
            // Charged spring remains readable. A flattened liquid never lifts or tilts into its ceiling.
            float walkAmount = walkBlend * (1f - charge);
            float idleAmount = idleBlend * Mathf.Lerp(1f, .18f, charge);
            float clearance = 1f - flow;
            float gasBob = characterHeight * .018f * gas * (.5f + .5f * Mathf.Sin(idleTime * 1.65f)) *
                Mathf.Clamp01(idleAmount + walkAmount) * clearance;
            CurrentHopHeight = characterHeight * hopHeightRatio * style.hop * arc * walkAmount * clearance + gasBob;
            float breathing = Mathf.Sin(idleTime * 2.3f) + .32f * Mathf.Sin(idleTime * 3.7f + .8f);
            float verticalScale = 1f + (idleSquish * style.idle * breathing * idleAmount -
                walkSquash * style.squash * contact * walkAmount + .035f * style.squash * arc * walkAmount) * clearance;
            float radialScale = 1f / Mathf.Sqrt(Mathf.Max(.5f, verticalScale));
            float jellySide = .012f * Mathf.Sin(idleTime * 2.8f + .4f) * idleAmount * style.idle +
                .025f * liquid * Mathf.Sin(walkPhase * Mathf.PI * 2f) * walkAmount;
            Vector3 scale = new Vector3(radialScale * (1f + jellySide), verticalScale, radialScale / (1f + jellySide));
            float roll = (idleSwayDegrees * Mathf.Sin(idleTime * 1.8f) * idleAmount +
                1.4f * Mathf.Sin(walkPhase * Mathf.PI * 2f) * walkAmount) * style.sway * clearance;
            float pitch = (.65f * Mathf.Sin(idleTime * 2.1f + .7f) * idleAmount - 1.1f * arc * walkAmount) * style.sway * clearance;
            float yaw = 1.1f * Mathf.Sin(idleTime * 1.3f + .4f) * idleAmount * style.sway;
            motionPivot.localPosition = restPosition + Vector3.up * CurrentHopHeight;
            motionPivot.localRotation = Quaternion.Euler(pitch, yaw, roll) * restRotation;
            motionPivot.localScale = Vector3.Scale(restScale, scale);
        }

        public void ResetMotion()
        {
            walkBlend = idleBlend = walkPhase = idleTime = CurrentHopHeight = 0f;
            sampledPosition = false;
            RestoreRestPose();
        }

        void RestoreRestPose()
        {
            if (!motionPivot || !hasRestPose) return;
            motionPivot.localPosition = restPosition;
            motionPivot.localRotation = restRotation;
            motionPivot.localScale = restScale;
        }

        void OnDisable() => ResetMotion();
        static void Accumulate(ref MotionStyle result, MotionStyle style, float weight)
        {
            result.hop += style.hop * weight; result.squash += style.squash * weight;
            result.idle += style.idle * weight; result.sway += style.sway * weight;
            result.cadence += style.cadence * weight;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
