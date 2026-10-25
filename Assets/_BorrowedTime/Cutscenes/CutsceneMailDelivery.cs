using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Cutscene 1 (storyboard panel 1): a delivery van arrives, drops a letter into the mailbox, drives away.
/// </summary>
public class CutsceneMailDelivery : MonoBehaviour
{
    [Header("Scene objects")]
    public Transform van;
    public Transform[] wheelSpinners;      // children of the wheel pivots, spun around local Y
    public float wheelRadius = 0.45f;
    public Transform letter;
    public Transform letterOrigin;         // child of van (cab window)
    public Transform mailboxSlot;
    public Transform mailbox;
    public Transform mailboxFlag;
    public Transform cameraRig;

    [Header("Timing")]
    public float startX = -22f;
    public float stopX = -3.2f;
    public float exitX = 24f;
    public float arriveTime = 3.4f;
    public float letterTime = 1.1f;
    public float exitTime = 3.0f;
    public float cameraPush = 1.6f;

    [Header("UI + flow")]
    public PanelSettings panelSettings;
    public Font subtitleFont;
    public string nextSceneName = "";      // loaded after onFinished (leave empty when panel 2 continues in this scene)
    public string skipToScene = "TrainingArea";   // where the Skip button jumps
    public UnityEngine.Events.UnityEvent onFinished;

    VisualElement _fade;
    bool _skip;
    bool _loading;
    public CutscenePresentation Presentation { get; private set; }
    public bool Skipped { get { return _skip; } }
    float _vanBaseY;
    Vector3 _camStart;
    float _camT, _camDur = 12f;

    void Awake()
    {
        var go = new GameObject("CutsceneUI"); go.transform.SetParent(transform, false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = panelSettings;
        doc.sortingOrder = 10;
        var root = doc.rootVisualElement;
        root.style.position = Position.Absolute;
        root.style.left = 0; root.style.top = 0; root.style.right = 0; root.style.bottom = 0;
        root.pickingMode = PickingMode.Ignore;

        _fade = new VisualElement();
        _fade.style.position = Position.Absolute;
        _fade.style.left = 0; _fade.style.top = 0; _fade.style.right = 0; _fade.style.bottom = 0;
        _fade.style.backgroundColor = Color.black;
        _fade.style.opacity = 1f;
        _fade.pickingMode = PickingMode.Ignore;
        root.Add(_fade);

        var skip = new Button(RequestSkip) { text = "Skip  \u25B6\u25B6" };
        skip.style.position = Position.Absolute;
        skip.style.right = 36; skip.style.bottom = 28;
        skip.style.paddingLeft = 26; skip.style.paddingRight = 26;
        skip.style.height = 62;
        skip.style.fontSize = 30;
        skip.style.unityFontStyleAndWeight = FontStyle.Bold;
        skip.style.color = new Color(0.49f, 0.28f, 0.17f);
        skip.style.backgroundColor = new Color(1f, 0.95f, 0.88f, 0.9f);
        skip.style.borderTopLeftRadius = skip.style.borderTopRightRadius =
            skip.style.borderBottomLeftRadius = skip.style.borderBottomRightRadius = 22;
        var bc = new Color(0.65f, 0.42f, 0.28f);
        skip.style.borderTopWidth = skip.style.borderBottomWidth = skip.style.borderLeftWidth = skip.style.borderRightWidth = 4;
        skip.style.borderTopColor = skip.style.borderBottomColor = skip.style.borderLeftColor = skip.style.borderRightColor = bc;
        root.Add(skip);
        Presentation = gameObject.AddComponent<CutscenePresentation>();
        Presentation.Initialize(doc, subtitleFont, RequestSkip);
    }

    public void RequestSkip()
    {
        if(_loading)return;
        _skip=true; _loading=true; StopAllCoroutines();
        Presentation.Engine(-1); Time.timeScale=1;
        SceneManager.LoadScene(skipToScene);
    }

    IEnumerator Start()
    {
        _vanBaseY = van.position.y;
        if (cameraRig) _camStart = cameraRig.position;
        letter.gameObject.SetActive(false);
        SetVanX(startX);
        SetFlag(0f);
        Presentation.SetShot("delivery", "เช้าวันหนึ่ง... จดหมายฉบับหนึ่งกำลังจะเปลี่ยนทุกอย่าง");

        yield return Fade(1f, 0f, 1.0f);

        // 1) van arrives and brakes
        float prevX = startX;
        for (float t = 0; t < arriveTime && !_skip; t += Time.deltaTime)
        {
            float k = t / arriveTime;
            float e = 1f - Mathf.Pow(1f - k, 3f);            // ease-out
            float x = Mathf.Lerp(startX, stopX, e);
            SpinWheels(x - prevX); prevX = x;
            SetVanX(x);
            float speed01 = Mathf.Pow(1f - k, 2f);
            Presentation.Engine(speed01);
            float brake = Mathf.Sin(Mathf.Clamp01((k - 0.6f) / 0.4f) * Mathf.PI) * -1.8f;   // nose dips while braking
            van.rotation = Quaternion.Euler(0, 0, brake);
            van.position += Vector3.up * (Mathf.Sin(Time.time * 17f) * 0.025f * speed01);
            yield return null;
        }
        SetVanX(stopX);
        van.rotation = Quaternion.identity;
        Presentation.Engine(.05f);

        if (!_skip)
        {
            yield return Wait(0.6f);

            // 2) letter pops out of the window and flies into the mailbox
            letter.gameObject.SetActive(true);
            Presentation.SetShot("letter-delivered", "ถึงคุณ — จาก Mr. X");
            Presentation.Cue("letter");
            Vector3 a = letterOrigin.position, b = mailboxSlot.position;
            for (float t = 0; t < letterTime && !_skip; t += Time.deltaTime)
            {
                float k = t / letterTime;
                Vector3 p = Vector3.Lerp(a, b, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.8f);
                letter.position = p;
                letter.rotation = Quaternion.Euler(k * 540f, 0, Mathf.Sin(k * 6f) * 20f);
                float s = Mathf.Lerp(1f, 0.55f, k * k);
                letter.localScale = new Vector3(0.5f, 0.03f, 0.36f) * s * 2f;
                yield return null;
            }
            letter.gameObject.SetActive(false);

            // 3) mailbox bounces and the flag pops up
            for (float t = 0; t < 0.7f && !_skip; t += Time.deltaTime)
            {
                float k = t / 0.7f;
                float sq = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * 0.12f;
                mailbox.localScale = new Vector3(1f - sq, 1f + sq, 1f - sq);
                SetFlag(1f - Mathf.Pow(1f - Mathf.Clamp01(k * 2f), 3f));
                yield return null;
            }
            mailbox.localScale = Vector3.one;
            SetFlag(1f);
            Presentation.Cue("chime");

            yield return Wait(1.0f);

            // 4) van drives away
            prevX = stopX;
            for (float t = 0; t < exitTime && !_skip; t += Time.deltaTime)
            {
                float k = t / exitTime;
                Presentation.Engine(k);
                float x = Mathf.Lerp(stopX, exitX, k * k);   // ease-in
                SpinWheels(x - prevX); prevX = x;
                SetVanX(x);
                float lift = Mathf.Sin(Mathf.Clamp01(k / 0.25f) * Mathf.PI) * 1.2f;       // nose lifts on takeoff
                van.rotation = Quaternion.Euler(0, 0, lift);
                van.position += Vector3.up * (Mathf.Sin(Time.time * 19f) * 0.02f * k);
                yield return null;
            }
            yield return Wait(0.4f);
        }

        SetVanX(exitX); Presentation.Engine(-1);

        yield return Fade(0f, 1f, 0.8f);
        Finish();
    }

    public bool cameraPushEnabled = true;

    void Update()
    {
        // slow cinematic push-in
        if (cameraRig && cameraPushEnabled)
        {
            _camT += Time.deltaTime;
            float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_camT / _camDur));
            cameraRig.position = _camStart + Vector3.forward * (cameraPush * k);
        }
    }

    void SetVanX(float x) { van.position = new Vector3(x, _vanBaseY, van.position.z); }
    void SpinWheels(float dx)
    {
        float deg = -dx / wheelRadius * Mathf.Rad2Deg;
        foreach (var w in wheelSpinners) w.Rotate(0, deg, 0, Space.Self);
    }
    void SetFlag(float k) { if (mailboxFlag) mailboxFlag.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-85f, 0f, k)); }

    IEnumerator Wait(float s)
    {
        for (float t = 0; t < s && !_skip; t += Time.deltaTime) yield return null;
    }

    public IEnumerator FadeTo(float from, float to, float dur) { return Fade(from, to, dur); }
    public IEnumerator Fade(float from, float to, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            if (_skip && to > from) break;
            _fade.style.opacity = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        _fade.style.opacity = to;
    }

    void Finish()
    {
        if (_skip && !string.IsNullOrEmpty(skipToScene)) { SceneManager.LoadScene(skipToScene); return; }
        onFinished?.Invoke();
        if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
    }
}
