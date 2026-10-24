using UnityEngine;
using TMPro;

/// <summary>
/// AGENT 02 — The game's only narrator: a single line of text, no voice, no NPCs.
/// Carries tutorial prompts, rejection reasons and payoff lines.
/// </summary>
public class HintTextManager : MonoBehaviour
{
    public static HintTextManager Instance { get; private set; }

    [Header("Display")]
    [SerializeField] private TextMeshProUGUI hintLabel;
    [SerializeField] private CanvasGroup hintGroup;
    [SerializeField] private float fadeSpeed = 6f;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 2.5f;

    private float hideAtTime;
    private float targetAlpha;
    private TimeGauntletController observedGauntlet;

    private void OnEnable() => BindGauntlet();

    private void BindGauntlet()
    {
        if (observedGauntlet == TimeGauntletController.Instance) return;
        if (observedGauntlet != null) observedGauntlet.OnUseRejected -= HandleRejection;
        observedGauntlet = TimeGauntletController.Instance;
        if (observedGauntlet != null) observedGauntlet.OnUseRejected += HandleRejection;
    }

    private void OnDisable()
    {
        if (observedGauntlet != null) observedGauntlet.OnUseRejected -= HandleRejection;
        observedGauntlet = null;
    }

    private void HandleRejection(string reason)
    {
        ShowHint(reason, 4f);
        Debug.Log("[BORROWED TIME] " + reason);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (hintGroup != null) hintGroup.alpha = 0f;
    }

    private void Update()
    {
        BindGauntlet();
        if (targetAlpha > 0f && Time.unscaledTime >= hideAtTime) targetAlpha = 0f;

        if (hintGroup != null && !Mathf.Approximately(hintGroup.alpha, targetAlpha))
            hintGroup.alpha = Mathf.MoveTowards(
                hintGroup.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
    }

    /// <summary>Show a line for a given number of seconds. Replaces whatever is on screen.</summary>
    public void ShowHint(string message, float duration = -1f)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (hintLabel != null) hintLabel.text = message;

        hideAtTime = Time.unscaledTime + (duration > 0f ? duration : defaultDuration);
        targetAlpha = 1f;
    }

    /// <summary>Hide immediately (used when entering menus).</summary>
    public void ClearHint() => targetAlpha = 0f;
}
