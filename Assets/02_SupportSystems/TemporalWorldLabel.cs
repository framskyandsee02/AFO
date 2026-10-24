using UnityEngine;

/// <summary>Prototype puzzle sign. Uses the existing main camera without changing its ownership.</summary>
public sealed class TemporalWorldLabel : MonoBehaviour
{
    private void LateUpdate()
    {
        var camera = Camera.main;
        if (camera != null) transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
    }
}
