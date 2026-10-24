using UnityEngine;

/// <summary>
/// AGENT 02 — Drop a trigger volume in the world; it speaks once (or every time)
/// when the player walks in. This is how the tutorial teaches Q and E.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HintTrigger : MonoBehaviour
{
    [SerializeField, TextArea] private string message = "Hold the gauntlet toward something old.";
    [SerializeField] private float duration = 3.5f;
    [SerializeField] private bool onlyOnce = true;
    [SerializeField] private string playerTag = "Player";

    private bool hasFired;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onlyOnce && hasFired) return;
        if (!other.CompareTag(playerTag)) return;

        hasFired = true;
        HintTextManager.Instance?.ShowHint(message, duration);
    }
}
