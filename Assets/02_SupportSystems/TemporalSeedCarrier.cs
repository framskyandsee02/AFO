using UnityEngine;

/// <summary>A single restoration-triggered seed delivery using the installed Chicken asset.</summary>
public class TemporalSeedCarrier : MonoBehaviour
{
    public TimeTarget sourceTree;
    public int sourceHealthyState = 1;
    public Transform animal;
    public Animator animalAnimator;
    public Transform seed;
    public Transform[] route;
    public TimeTarget plantedTarget;
    public float travelSpeed = 2f;
    public float growthSeconds = 3f;

    public bool HasDelivered { get; private set; }
    public bool IsGrowing { get; private set; }

    private TimeTarget subscribedSource;
    private bool initialized;
    private bool started;
    private bool finished;
    private bool hasVert;
    private int waypoint;
    private Vector3 groundPosition;
    private Vector3 dropStart;
    private Vector3 dropEnd;
    private float dropElapsed;
    private float growthElapsed;
    private float growthBudget;

    private void OnEnable()
    {
        subscribedSource = sourceTree;
        if (subscribedSource != null) subscribedSource.OnAgeChanged += CheckSource;
        if (initialized) CheckSource(sourceTree);
    }

    private void Start()
    {
        initialized = true;
        if (animalAnimator != null && animalAnimator.runtimeAnimatorController != null)
            foreach (var parameter in animalAnimator.parameters)
                if (parameter.name == "Vert" && parameter.type == AnimatorControllerParameterType.Float)
                    hasVert = true;
        if (seed != null) seed.gameObject.SetActive(false);
        if (plantedTarget != null)
        {
            plantedTarget.SetInteractionLocked(true, "Wait for the bird to plant this seed.");
            plantedTarget.gameObject.SetActive(false);
        }
        if (animal != null) animal.gameObject.SetActive(false);
        CheckSource(sourceTree);
    }

    private void OnDisable()
    {
        if (subscribedSource != null) subscribedSource.OnAgeChanged -= CheckSource;
        subscribedSource = null;
        SetWalking(false);
    }

    private void CheckSource(TimeTarget source)
    {
        if (!initialized || started || source == null || !source.IsAtState(sourceHealthyState)) return;
        started = true;
        if (animal != null)
        {
            animal.gameObject.SetActive(true);
            groundPosition = animal.position;
        }
        if (seed != null)
        {
            if (animal != null) seed.SetParent(animal, true);
            seed.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (!started || finished) return;
        if (!HasDelivered)
        {
            // Skip missing waypoints without losing progress when disabled and resumed.
            while (route != null && waypoint < route.Length && route[waypoint] == null) waypoint++;
            if (animal != null && route != null && waypoint < route.Length)
            {
                Transform point = route[waypoint];
                Vector3 direction = point.position - groundPosition;
                groundPosition = Vector3.MoveTowards(groundPosition, point.position,
                    Mathf.Max(0.01f, travelSpeed) * Time.deltaTime);
                Vector3 facing = Vector3.ProjectOnPlane(direction, Vector3.up);
                if (facing.sqrMagnitude > 0.0001f) animal.rotation = Quaternion.LookRotation(facing);
                animal.position = groundPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.08f);
                SetWalking(true);
                if (groundPosition == point.position)
                {
                    animal.SetPositionAndRotation(point.position, point.rotation);
                    waypoint++;
                }
                return;
            }
            Deliver();
            return;
        }

        if (dropElapsed < 0.4f)
        {
            dropElapsed = Mathf.Min(0.4f, dropElapsed + Time.deltaTime);
            if (seed != null) seed.position = Vector3.Lerp(dropStart, dropEnd, dropElapsed / 0.4f);
            return;
        }
        if (plantedTarget == null || plantedTarget.StateCount < 2)
        {
            Finish();
            return;
        }
        if (!plantedTarget.IsUnlocked()) return;
        IsGrowing = true;
        float duration = Mathf.Max(0.01f, growthSeconds);
        float step = Mathf.Min(Time.deltaTime, duration - growthElapsed);
        // A finite budget and clock prevent rewinding the target from renewing natural growth.
        float delta = Mathf.Min(growthBudget * step / Mathf.Max(0.0001f, duration - growthElapsed),
            Mathf.Max(0f, 1f - plantedTarget.AgePosition));
        float applied = plantedTarget.ShiftAge(delta); // No EnergyManager: natural growth transfers no energy.
        growthBudget = Mathf.Max(0f, growthBudget - applied);
        growthElapsed += step;
        if (growthElapsed >= duration || plantedTarget.AgePosition >= 1f) Finish();
    }

    private void Deliver()
    {
        HasDelivered = true;
        if (plantedTarget != null) { plantedTarget.gameObject.SetActive(true); plantedTarget.SetInteractionLocked(false); }
        SetWalking(false);
        dropStart = seed != null ? seed.position : groundPosition;
        dropEnd = plantedTarget != null ? plantedTarget.transform.position : groundPosition;
        growthBudget = plantedTarget != null ? Mathf.Max(0f, 1f - plantedTarget.AgePosition) : 0f;
        if (seed != null) seed.SetParent(null, true);
    }

    private void Finish()
    {
        finished = true;
        IsGrowing = false;
        if (seed != null) seed.gameObject.SetActive(false);
    }

    private void SetWalking(bool walking)
    {
        if (hasVert && animalAnimator != null && animalAnimator.isActiveAndEnabled)
            animalAnimator.SetFloat("Vert", walking ? 1f : 0f);
    }
}
