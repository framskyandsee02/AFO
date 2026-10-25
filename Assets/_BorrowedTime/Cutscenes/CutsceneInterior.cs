using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using BorrowedTime.Characters;

/// <summary>
/// Storyboard panels 3-6 in one scene:
///  3 hero walks in and sits at the table, 4 the letter is shown close up,
///  5 the drawer opens (blue glow), 6 the hero dives into the drawer like Doraemon.
/// </summary>
public class CutsceneInterior : MonoBehaviour
{
    [Header("Scene refs")]
    public Camera cam;
    public Transform hero;
    public BTCharacterModel model;
    public Transform doorHinge;
    public Transform letter;
    public Transform drawer;
    public Transform swirl;
    public Light swirlLight;
    public Vector3[] walkPath;           // door -> chair
    public Vector3 seatPosition = new Vector3(-1.2f, -0.3f, 3.5f);
    public Vector3 letterOnTable = new Vector3(-1.2f, 1.07f, 2.45f);
    public Vector3 drawerClosed, drawerOpen;
    public Vector3 heroStand = new Vector3(3.9f, 0f, 1.9f);

    [Header("UI")]
    public PanelSettings panelSettings;
    public Font thaiFont;
    [TextArea] public string letterText = "เราอยากรู้ถึงความสามารถของเทคโนโลยีของคุณ\nพวกเราขอยืมเวลาคุณหน่อย";
    public string letterSign = "- Mr.X";

    [Header("Flow")]
    public string nextSceneName = "TrainingArea";
    public string skipToScene = "TrainingArea";

    VisualElement _fade, _overlay, _burstA, _burstB, _card;
    bool _skip, _loading, _burstOn;
    float _burstAngle;
    Coroutine _dolly;
    public CutscenePresentation Presentation { get; private set; }
    UnityEngine.TextCore.Text.FontAsset letterFontAsset;

    // ------------------------------------------------------------------ UI
    void Awake()
    {
        var go = new GameObject("CutsceneUI"); go.transform.SetParent(transform, false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = panelSettings;
        doc.sortingOrder = 10;
        var root = doc.rootVisualElement;
        Stretch(root); root.pickingMode = PickingMode.Ignore;

        // letter close-up overlay (panel 4)
        _overlay = new VisualElement(); Stretch(_overlay);
        _overlay.style.top=Length.Percent(7);_overlay.style.bottom=Length.Percent(13);_overlay.style.overflow=Overflow.Hidden;
        _overlay.style.backgroundColor = new Color(0.20f, 0.10f, 0.35f, 0.55f);
        _overlay.style.alignItems = Align.Center; _overlay.style.justifyContent = Justify.Center;
        _overlay.style.display = DisplayStyle.None; _overlay.pickingMode = PickingMode.Ignore;
        root.Add(_overlay);

        _burstA = MakeBurst(1100); _burstB = MakeBurst(1100);
        _overlay.Add(_burstA); _overlay.Add(_burstB);

        _card = new VisualElement();
        _card.style.width = Length.Percent(82); _card.style.maxWidth = 1100; _card.style.paddingTop = 46; _card.style.paddingBottom = 40;
        _card.style.paddingLeft = 52; _card.style.paddingRight = 52;
        _card.style.backgroundColor = new Color(1f, 0.78f, 0.86f);
        _card.style.borderTopWidth = _card.style.borderBottomWidth = _card.style.borderLeftWidth = _card.style.borderRightWidth = 6;
        var bc = new Color(0.12f, 0.08f, 0.14f);
        _card.style.borderTopColor = _card.style.borderBottomColor = _card.style.borderLeftColor = _card.style.borderRightColor = bc;
        _card.style.borderTopLeftRadius = _card.style.borderTopRightRadius = _card.style.borderBottomLeftRadius = _card.style.borderBottomRightRadius = 10;
        var body = MakeLabel(letterText, 40, FontStyle.Normal, TextAnchor.UpperLeft);
        var sign = MakeLabel(letterSign, 40, FontStyle.Bold, TextAnchor.UpperRight);
        sign.style.marginTop = 26;
        _card.Add(body); _card.Add(sign);
        _overlay.Add(_card);

        // fade + skip
        _fade = new VisualElement(); Stretch(_fade);
        _fade.style.backgroundColor = Color.black; _fade.style.opacity = 1f; _fade.pickingMode = PickingMode.Ignore;
        root.Add(_fade);

        var skip = new Button(RequestSkip) { text = "Skip  \u25B6\u25B6" };
        skip.style.position = Position.Absolute; skip.style.right = 36; skip.style.bottom = 28;
        skip.style.paddingLeft = 26; skip.style.paddingRight = 26; skip.style.height = 62; skip.style.fontSize = 30;
        skip.style.unityFontStyleAndWeight = FontStyle.Bold;
        skip.style.color = new Color(0.49f, 0.28f, 0.17f);
        skip.style.backgroundColor = new Color(1f, 0.95f, 0.88f, 0.9f);
        skip.style.borderTopLeftRadius = skip.style.borderTopRightRadius = skip.style.borderBottomLeftRadius = skip.style.borderBottomRightRadius = 22;
        var bc2 = new Color(0.65f, 0.42f, 0.28f);
        skip.style.borderTopWidth = skip.style.borderBottomWidth = skip.style.borderLeftWidth = skip.style.borderRightWidth = 4;
        skip.style.borderTopColor = skip.style.borderBottomColor = skip.style.borderLeftColor = skip.style.borderRightColor = bc2;
        root.Add(skip);
        Presentation=gameObject.AddComponent<CutscenePresentation>();
        Presentation.Initialize(doc,thaiFont,RequestSkip);
    }

    public void RequestSkip()
    {
        if(_loading)return;
        _skip=true;_loading=true;StopAllCoroutines();Time.timeScale=1;
        SceneManager.LoadScene(skipToScene);
    }

    static void Stretch(VisualElement e)
    {
        e.style.position = Position.Absolute;
        e.style.left = 0; e.style.top = 0; e.style.right = 0; e.style.bottom = 0;
    }

    VisualElement MakeBurst(float size)
    {
        var e = new LetterRays();
        e.style.position = Position.Absolute;
        e.style.width = size; e.style.height = size;
        e.pickingMode = PickingMode.Ignore;
        return e;
    }

    sealed class LetterRays : VisualElement
    {
        public LetterRays(){generateVisualContent+=Draw;}
        void Draw(MeshGenerationContext context)
        {
            var painter=context.painter2D;var center=contentRect.center;
            painter.fillColor=new Color(.67f,.42f,.84f,.24f);
            float radius=Mathf.Max(contentRect.width,contentRect.height);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6,b=a+.11f;
                painter.BeginPath();painter.MoveTo(center);
                painter.LineTo(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);
                painter.LineTo(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius);
                painter.ClosePath();painter.Fill();
            }
        }
    }

    Label MakeLabel(string text, int size, FontStyle fs, TextAnchor anchor)
    {
        var l = new Label(text);
        l.style.fontSize = size; l.style.unityFontStyleAndWeight = fs; l.style.unityTextAlign = anchor;
        l.style.color = new Color(0.12f, 0.08f, 0.14f);
        l.style.whiteSpace = WhiteSpace.Normal;
        if (thaiFont != null)
        {
            try
            {
                if(!letterFontAsset)letterFontAsset=UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(thaiFont);
                l.style.unityFontDefinition = FontDefinition.FromSDFFont(letterFontAsset);
            }
            catch (System.Exception e) { Debug.LogWarning("Thai font asset failed: " + e.Message); }
        }
        return l;
    }

    // ------------------------------------------------------------------ flow
    void Update()
    {
        if (_skip && !_loading) { _loading = true; SceneManager.LoadScene(skipToScene); return; }

        if (swirl && swirl.gameObject.activeSelf) swirl.Rotate(0, 140f * Time.deltaTime, 0, Space.Self);
        if (_burstOn)
        {
            _burstAngle += 22f * Time.deltaTime;
            _burstA.style.rotate = new StyleRotate(new Rotate(new Angle(_burstAngle, AngleUnit.Degree)));
            _burstB.style.rotate = new StyleRotate(new Rotate(new Angle(45f - _burstAngle * 0.6f, AngleUnit.Degree)));
        }
    }

    IEnumerator Start()
    {
        // initial state
        swirl.gameObject.SetActive(false);
        if (swirlLight) swirlLight.intensity = 0;
        drawer.localPosition = drawerClosed;
        letter.gameObject.SetActive(true);
        letter.localScale = new Vector3(0.5f, 0.04f, 0.36f);
        hero.position = walkPath[0];
        hero.rotation = Quaternion.Euler(0, 180f, 0);
        model.PlayPose("Idle"); model.SetFace("Neutral");
        model.autoReturn=false;
        Presentation.SetShot("reading-room", "จดหมายปริศนา... จากคนที่ไม่เคยรู้จัก");
        SetCam(new Vector3(-0.4f, 2.9f, -4.6f), new Vector3(-0.4f, 1.5f, 3f), 40f);
        letter.position = new Vector3(0, -50, 0);          // hidden until hero brings it

        yield return Fade(1f, 0f, 0.9f);

        // ---------- PANEL 3: back home, sit at the table ----------
        StartDolly(new Vector3(0.2f, 2.8f, -3.8f), new Vector3(-0.8f, 1.5f, 3f), 38f, 7f);
        for (float k = 0; k < 0.9f; k += Time.deltaTime)
        { if (doorHinge) doorHinge.localRotation = Quaternion.Euler(0, Mathf.SmoothStep(0, 80f, k / 0.9f), 0); yield return null; }

        model.PlayPose("Walk");
        float nextStep=0;
        for (int i = 1; i < walkPath.Length; i++)
        {
            Vector3 target = walkPath[i];
            while ((hero.position - target).sqrMagnitude > 0.02f)
            {
                Vector3 dir = target - hero.position; dir.y = 0;
                hero.rotation = Quaternion.Slerp(hero.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 7f);
                hero.position = Vector3.MoveTowards(hero.position, target, 2.3f * Time.deltaTime);
                if(Time.time>=nextStep){nextStep=Time.time+.42f;Presentation.Cue("step");}
                if (model.rightHand) { letter.position = model.rightHand.position; letter.rotation = model.rightHand.rotation; }
                yield return null;
            }
        }
        // turn toward the camera and sit down
        model.PlayPose("CrouchIdle"); model.SetFace("Thinking");
        Vector3 from = hero.position; Quaternion fromRot = hero.rotation; Vector3 letterFrom = letter.position;
        for (float k = 0; k < 0.8f; k += Time.deltaTime)
        {
            float e = Mathf.SmoothStep(0, 1, k / 0.8f);
            hero.position = Vector3.Lerp(from, seatPosition, e);
            hero.rotation = Quaternion.Slerp(fromRot, Quaternion.Euler(0, 180f, 0), e);
            letter.position = Vector3.Lerp(letterFrom, letterOnTable, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.2f;
            letter.rotation = Quaternion.Slerp(letter.rotation, Quaternion.Euler(0, 12f, 0), e);
            yield return null;
        }
        yield return new WaitForSeconds(1.2f);

        // ---------- PANEL 4: zoom into the letter ----------
        StartDolly(new Vector3(-0.9f, 2.0f, 0.6f), letterOnTable, 24f, 1.0f);
        yield return new WaitForSeconds(0.7f);
        _overlay.style.display = DisplayStyle.Flex; _overlay.style.opacity = 0f;
        Presentation.SetShot("read-letter", "พวกเราขอยืมเวลาคุณหน่อย — Mr. X");
        Presentation.Cue("letter");
        _burstOn = true; _card.style.scale = new StyleScale(new Scale(new Vector3(0.4f, 0.4f, 1f)));
        for (float k = 0; k < 0.7f; k += Time.deltaTime)
        {
            float u = k / 0.7f;
            float back = 1f + 2.4f * Mathf.Pow(u - 1f, 3f) + 1.4f * Mathf.Pow(u - 1f, 2f);   // ease-out-back
            float s = Mathf.Lerp(0.4f, 1f, back);
            _overlay.style.opacity = Mathf.Clamp01(u * 2f);
            _card.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
            yield return null;
        }
        _card.style.scale = new StyleScale(new Scale(Vector3.one));
        for (float k = 0; k < 4.6f; k += Time.deltaTime)       // gentle wobble while reading
        {
            float sw = Mathf.Sin(k * 2.2f) * 1.2f;
            _card.style.rotate = new StyleRotate(new Rotate(new Angle(sw, AngleUnit.Degree)));
            yield return null;
        }
        for (float k = 0; k < 0.4f; k += Time.deltaTime) { _overlay.style.opacity = 1f - k / 0.4f; yield return null; }
        _overlay.style.display = DisplayStyle.None; _burstOn = false;

        // ---------- PANEL 5: the drawer opens ----------
        yield return Fade(0f, 1f, 0.3f);
        hero.position = new Vector3(2.4f, 0, 0.8f);                                      // off screen while we look at the drawer
        model.PlayPose("Idle");
        SetCam(new Vector3(3.2f, 2.5f, -0.9f), new Vector3(4.0f, 1.2f, 3.7f), 32f);
        StartDolly(new Vector3(3.5f, 2.55f, -0.2f), new Vector3(4.0f, 1.25f, 3.3f), 30f, 4.2f);
        yield return Fade(1f, 0f, 0.3f);
        yield return new WaitForSeconds(0.5f);
        swirl.gameObject.SetActive(true);
        Presentation.SetShot("time-door", "เสียงนั้น... มาจากลิ้นชัก?");
        Presentation.Cue("drawer");Presentation.Cue("portal");
        for (float k = 0; k < 1.5f; k += Time.deltaTime)
        {
            float e = Mathf.SmoothStep(0, 1, k / 1.5f);
            drawer.localPosition = Vector3.Lerp(drawerClosed, drawerOpen, e);
            swirl.localScale = Vector3.one * Mathf.Clamp01((e - 0.35f) / 0.65f);
            if (swirlLight) swirlLight.intensity = 6f * Mathf.Clamp01((e - 0.2f) / 0.8f);
            yield return null;
        }
        yield return new WaitForSeconds(1.5f);

        // ---------- PANEL 6: the hero jumps in like Doraemon ----------
        yield return Fade(0f, 1f, 0.25f);
        hero.position = heroStand; hero.rotation = Quaternion.identity; hero.localScale = Vector3.one;
        model.PlayPose("Idle"); model.SetFace("Surprised");
        Presentation.SetShot("new-journey", "อีกฟากหนึ่ง จะมีอะไรรออยู่กันนะ?");
        SetCam(new Vector3(0.9f, 2.9f, -1.0f), new Vector3(3.8f, 1.3f, 3.6f), 40f);
        StartDolly(new Vector3(1.4f, 2.7f, -0.4f), new Vector3(3.9f, 1.3f, 3.3f), 36f, 3.5f);
        yield return Fade(1f, 0f, 0.25f);
        yield return new WaitForSeconds(0.9f);
        model.SetFace("Smile");
        yield return new WaitForSeconds(0.5f);

        model.PlayPose("JumpStart");
        Presentation.Cue("portal");
        Vector3 start = hero.position;
        Vector3 end = swirl.position;
        for (float k = 0; k < 1.5f; k += Time.deltaTime)
        {
            float u = k / 1.5f; float e = Mathf.SmoothStep(0, 1, u);
            Vector3 p = Vector3.Lerp(start, end, e) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.9f;
            hero.position = p;
            hero.rotation = Quaternion.Euler(0, u * 720f, 0);
            hero.localScale = Vector3.one * Mathf.Lerp(1f, 0.04f, Mathf.Pow(u, 1.6f));
            if (swirlLight) swirlLight.intensity = 6f + u * 14f;
            yield return null;
        }
        hero.gameObject.SetActive(false);
        Presentation.SetShot("arrival", "การเดินทางครั้งใหม่... เริ่มต้นขึ้นแล้ว");
        // blue-white flash
        _fade.style.backgroundColor = new Color(0.85f, 0.95f, 1f);
        yield return Fade(0f, 1f, 0.6f);
        yield return new WaitForSeconds(0.3f);
        _loading = true;
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy(){if(letterFontAsset)Destroy(letterFontAsset);}

    // ------------------------------------------------------------------ helpers
    IEnumerator Fade(float from, float to, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime) { _fade.style.opacity = Mathf.Lerp(from, to, t / dur); yield return null; }
        _fade.style.opacity = to;
    }

    void SetCam(Vector3 pos, Vector3 look, float fov)
    {
        if (_dolly != null) StopCoroutine(_dolly);
        cam.transform.position = pos; cam.transform.rotation = Quaternion.LookRotation(look - pos); cam.fieldOfView = fov;
    }

    void StartDolly(Vector3 pos, Vector3 look, float fov, float dur)
    {
        if (_dolly != null) StopCoroutine(_dolly);
        _dolly = StartCoroutine(Dolly(pos, look, fov, dur));
    }

    IEnumerator Dolly(Vector3 pos, Vector3 look, float fov, float dur)
    {
        Vector3 p0 = cam.transform.position; float f0 = cam.fieldOfView;
        Vector3 curLook = p0 + cam.transform.forward * Vector3.Distance(p0, look);
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float e = Mathf.SmoothStep(0, 1, t / dur);
            Vector3 p = Vector3.Lerp(p0, pos, e);
            Vector3 l = Vector3.Lerp(curLook, look, e);
            cam.transform.position = p; cam.transform.rotation = Quaternion.LookRotation(l - p);
            cam.fieldOfView = Mathf.Lerp(f0, fov, e);
            yield return null;
        }
        cam.transform.position=pos;cam.transform.rotation=Quaternion.LookRotation(look-pos);cam.fieldOfView=fov;
    }
}
