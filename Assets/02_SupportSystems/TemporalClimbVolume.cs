using System.Collections.Generic;
using UnityEngine;

/// <summary>Physical climb region for a grown vine. No teleport or global input ownership.</summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class TemporalClimbVolume : MonoBehaviour
{
    private static readonly HashSet<TemporalClimbVolume> Active = new HashSet<TemporalClimbVolume>();
    public Transform topExit;
    public float climbSpeed = 2.5f;
    private BoxCollider region;
    public Bounds RegionBounds => region != null ? region.bounds : new Bounds();
    private void Awake() { region = GetComponent<BoxCollider>(); region.isTrigger = true; }
    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);
    public static bool TryFind(CharacterController controller, out TemporalClimbVolume result)
    {
        foreach (var volume in Active)
            if (volume != null && volume.region != null && volume.region.enabled &&
                volume.region.bounds.Contains(controller.bounds.center)) { result = volume; return true; }
        result = null; return false;
    }
}
