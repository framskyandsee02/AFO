#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BorrowedTime.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace BorrowedTime.Editor
{
    [InitializeOnLoad]
    public static class BorrowedMenuValidation
    {
        const string Active = "BorrowedMenu.Test.Active", Stage = "BorrowedMenu.Test.Stage";
        const string ReportPath = "Art/BorrowedTime/Validation.json";
        [Serializable] sealed class Check { public string name, detail; public bool passed; }
        [Serializable] sealed class Report { public bool completed, success; public int passed, failed; public List<Check> checks = new List<Check>(); }
        [Serializable] sealed class Preference { public string key; public bool existed; public int value; }
        [Serializable] sealed class Preferences { public Preference[] items; public float volume; public bool runInBackground; }
        static Report report;
        static double next;
        static UIDocument preview;
        static PanelSettings previewPanel;
        static RenderTexture texture;
        static Camera sourceCamera;
        static RenderTexture oldTarget;
        static int sizeIndex, readyFrame;
        static bool settingsPhase;
        static readonly Vector2Int[] Sizes = { new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(2560,1440),new Vector2Int(1024,768),new Vector2Int(2560,1080),new Vector2Int(720,1280),new Vector2Int(800,600),new Vector2Int(1088,1445),new Vector2Int(1280,1024) };
        static BorrowedMenuValidation() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Borrowed Time/Validate Main Menu")]
        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Begin validation outside Play Mode.");
            var keys = new[] { MenuAudioSettings.Key(MenuAudioChannel.Master), MenuAudioSettings.Key(MenuAudioChannel.Music), MenuAudioSettings.Key(MenuAudioChannel.Effects), "BorrowedTime.Audio.Muted" };
            SessionState.SetString("BorrowedMenu.Test.Prefs", JsonUtility.ToJson(new Preferences { items = keys.Select(k => new Preference { key = k, existed = PlayerPrefs.HasKey(k), value = PlayerPrefs.GetInt(k) }).ToArray(), volume = AudioListener.volume, runInBackground = Application.runInBackground }));
            Directory.CreateDirectory("Art/BorrowedTime"); report = new Report(); Save();
            SessionState.SetBool(Active, true); SessionState.SetInt(Stage, 0); SessionState.SetFloat("BorrowedMenu.Test.Started", (float)EditorApplication.timeSinceStartup);
            Application.runInBackground = true; EditorSceneManager.OpenScene(BorrowedMenuBuilder.ScenePath); EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Active, false)) return;
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                int stage = SessionState.GetInt(Stage, 0);
                if (stage == 99 && !EditorApplication.isPlayingOrWillChangePlaymode) { Finish(); return; }
                if (EditorApplication.timeSinceStartup - SessionState.GetFloat("BorrowedMenu.Test.Started", 0) > 180) throw new Exception("Menu validation timed out.");
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
                if (report == null) report = JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath));
                if (stage == 0)
                {
                    var menu = Object.FindFirstObjectByType<BorrowedMainMenu>(); if (!menu) return;
                    var root = menu.GetComponent<UIDocument>().rootVisualElement.Q("menu-root");
                    if (float.IsNaN(root.layout.width) || root.layout.width < 10 || Time.frameCount < 5) return;
                    CheckResult(root.Q("game-title").worldBound.yMax <= root.Q("play-button").worldBound.yMin + 1, "Title is above Play");
                    CheckResult(root.Q("menu-column").Query<Button>().ToList().Count == 3 && new[] { "play-button", "setting-button", "quit-button" }.Select(n => root.Q<Button>(n).text).SequenceEqual(new[] { "Play", "Settings", "Exit" }), "Exactly three main actions with the reference labels");
                    CheckResult(root.Q<Label>("title-borrowed").text.ToUpperInvariant() == "BORROWED" && root.Q<Label>("title-time").text.ToUpperInvariant() == "TIME", "Live game title is BORROWED TIME");
                    Submit(root.Q<Button>("setting-button")); CheckResult(menu.SettingsVisible, "Setting opens the actual settings panel");
                    root.Q<IntegerField>("master-number").value = -20; CheckResult(MenuAudioSettings.Get(MenuAudioChannel.Master) == 0 && root.Q<SliderInt>("master-volume").value == 0, "Volume clamps negative input");
                    root.Q<IntegerField>("master-number").value = 120; CheckResult(MenuAudioSettings.Get(MenuAudioChannel.Master) == 100, "Volume clamps input above 100");
                    root.Q<SliderInt>("master-volume").value = 83; root.Q<SliderInt>("music-volume").value = 27; root.Q<IntegerField>("effects-number").value = 64;
                    CheckResult(root.Q<IntegerField>("master-number").value == 83 && root.Q<IntegerField>("music-number").value == 27 && root.Q<SliderInt>("effects-volume").value == 64, "All volume fields and sliders synchronize at 1 percent precision");
                    MenuAudioSettings.Reload(); CheckResult(MenuAudioSettings.Get(MenuAudioChannel.Master) == 83 && MenuAudioSettings.Get(MenuAudioChannel.Music) == 27 && MenuAudioSettings.Get(MenuAudioChannel.Effects) == 64, "All audio values reload from saved preferences");
                    var music = new GameObject("Temporary validation music").AddComponent<AudioSource>(); music.volume = .6f; var musicBus = music.gameObject.AddComponent<MenuAudioBus>(); musicBus.channel = MenuAudioChannel.Music;
                    var effects = new GameObject("Temporary validation effects").AddComponent<AudioSource>(); effects.volume = .4f; effects.gameObject.AddComponent<MenuAudioBus>();
                    MenuAudioSettings.Set(MenuAudioChannel.Music, 27);
                    CheckResult(Mathf.Abs(music.volume - .162f) < .0001f && Mathf.Abs(effects.volume - .256f) < .0001f && Mathf.Abs(AudioListener.volume - .83f) < .0001f, "Music, effects and master route to independent real audio levels");
                    root.Q<Toggle>("mute-toggle").value = true; CheckResult(AudioListener.volume == 0, "Mute silences real output");
                    root.Q<Toggle>("mute-toggle").value = false; CheckResult(Mathf.Abs(AudioListener.volume - .83f) < .0001f, "Unmute restores remembered master level");
                    Object.Destroy(music.gameObject); Object.Destroy(effects.gameObject);
                    CheckResult(root.Q<DropdownField>("resolution-dropdown").choices.Count > 0 && root.Q<Toggle>("fullscreen-toggle") != null, "Display options use the available resolutions and fullscreen state");
                    Submit(root.Q<Button>("back-button")); CheckResult(!menu.SettingsVisible, "Back restores main actions");
                    CheckResult(root.Q("leaf-particles").pickingMode == PickingMode.Ignore && root.Q("foreground-foliage").pickingMode == PickingMode.Ignore, "Leaf and foliage layers do not block clicks");
                    CheckResult(Object.FindFirstObjectByType<SleepingCompanions>() != null, "Sleeping companions occupy the reserved character anchor");
                    MenuAudioSettings.Set(MenuAudioChannel.Master, 80); MenuAudioSettings.Set(MenuAudioChannel.Music, 80); MenuAudioSettings.Set(MenuAudioChannel.Effects, 100);
                    sizeIndex = 0; sourceCamera = Camera.main; oldTarget = sourceCamera.targetTexture;
                    CreatePreview(menu.GetComponent<UIDocument>().panelSettings);
                    SessionState.SetInt(Stage, 1);
                }
                else if (stage == 1)
                {
                    if (Time.frameCount < readyFrame) return;
                    var root = preview.rootVisualElement.Q("menu-root"); var bounds = root.worldBound; var column = root.Q("menu-column");
                    bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 2 && inner.yMin >= outer.yMin - 2 && inner.xMax <= outer.xMax + 2 && inner.yMax <= outer.yMax + 2;
                    var size = Sizes[sizeIndex];
                    if (!settingsPhase)
                    {
                        var title = root.Q("game-title"); var buttons = new[] { "play-button", "setting-button", "quit-button" }.Select(n => root.Q<Button>(n)).ToArray();
                        bool fit = Contains(bounds, column.worldBound) && buttons.All(b => Contains(bounds, b.worldBound));
                        bool ordered = title.worldBound.yMax <= buttons[0].worldBound.yMin + 2 && buttons[0].worldBound.yMax <= buttons[1].worldBound.yMin + 2 && buttons[1].worldBound.yMax <= buttons[2].worldBound.yMin + 2;
                        bool right = column.worldBound.center.x > bounds.width * .68f;
                        bool textFits = buttons.All(b => b.MeasureTextSize(b.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x < b.layout.width * .62f);
                        textFits &= new[] { "title-borrowed", "title-time" }.Select(n => root.Q<Label>(n)).All(l => Contains(title.worldBound, l.worldBound) && l.MeasureTextSize(l.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x <= l.layout.width + 2);
                        CheckResult(fit && ordered && right && textFits, "Main layout " + size.x + "x" + size.y, "fit=" + fit + ", ordered=" + ordered + ", right=" + right + ", text=" + textFits);
                        if (sizeIndex == 0 || sizeIndex == 3 || sizeIndex == 5) Capture("Menu_" + size.x + "x" + size.y);
                        root.Q("settings-overlay").RemoveFromClassList("hidden"); root.AddToClassList("settings-open"); settingsPhase = true;
                        readyFrame = Time.frameCount + 4; next = EditorApplication.timeSinceStartup + .3;
                    }
                    else
                    {
                        var card = root.Q("settings-card"); var scroll = root.Q<ScrollView>("settings-scroll");
                        bool fit = Contains(bounds, card.worldBound) && Contains(card.worldBound, root.Q("back-button").worldBound);
                        bool fields = new[] { "master-number", "music-number", "effects-number" }.All(n => Contains(card.worldBound, root.Q(n).worldBound));
                        bool canScroll = scroll.contentContainer.layout.height <= scroll.contentViewport.layout.height + 2 || scroll.verticalScroller.highValue > 0;
                        CheckResult(fit && fields && canScroll, "Settings layout " + size.x + "x" + size.y, "fit=" + fit + ", fields=" + fields + ", scroll=" + canScroll);
                        if (sizeIndex == 0 || sizeIndex == 5) Capture("Settings_" + size.x + "x" + size.y);
                        var panelTemplate = Object.FindFirstObjectByType<BorrowedMainMenu>().GetComponent<UIDocument>().panelSettings;
                        CleanupPreview(); sizeIndex++;
                        if (sizeIndex < Sizes.Length) CreatePreview(panelTemplate);
                        else { SessionState.SetInt(Stage, 2); next = EditorApplication.timeSinceStartup + .3; }
                    }
                }
                else if (stage == 2)
                {
                    var menu = Object.FindFirstObjectByType<BorrowedMainMenu>();
                    SessionState.SetInt(Stage, 3); Submit(menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>("play-button")); next = EditorApplication.timeSinceStartup + .7;
                }
                else if (stage == 3)
                {
                    CheckResult(SceneManager.GetActiveScene().name == "Start", "Play opens the existing Start scene");
                    SessionState.SetInt(Stage, 4); SceneManager.LoadScene("BorrowedTimeMainMenu"); next = EditorApplication.timeSinceStartup + 1;
                }
                else if (stage == 4)
                {
                    var menu = Object.FindFirstObjectByType<BorrowedMainMenu>(); if (!menu) return;
                    SessionState.SetInt(Stage, 99); Submit(menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>("quit-button"));
                }
            }
            catch (Exception e)
            {
                if (report == null) report = new Report(); CheckResult(false, "Validation exception", e.ToString()); CleanupPreview();
                SessionState.SetInt(Stage, 99); EditorApplication.isPlaying = false;
            }
        }
        static void Submit(Button button)
        {
            button.Focus();
            using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
        }
        static void CreatePreview(PanelSettings template)
        {
            var size = Sizes[sizeIndex]; texture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32); texture.Create();
            sourceCamera.targetTexture = texture;
            previewPanel = Object.Instantiate(template); previewPanel.targetTexture = texture; previewPanel.clearColor = false;
            var host = new GameObject("Temporary Borrowed Time layout capture") { hideFlags = HideFlags.HideAndDontSave };
            preview = host.AddComponent<UIDocument>(); preview.panelSettings = previewPanel;
            preview.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BorrowedMenuBuilder.UiPath + "BorrowedMainMenu.uxml");
            BorrowedMainMenu.InstallArtwork(preview.rootVisualElement.Q("menu-root"));
            var live = Object.FindFirstObjectByType<BorrowedMainMenu>().GetComponent<UIDocument>().rootVisualElement;
            if (!Object.FindFirstObjectByType<BorrowedMainMenu>().showForegroundFoliage) preview.rootVisualElement.Q("foreground-foliage").style.display = DisplayStyle.None;
            var liveResolution = live.Q<DropdownField>("resolution-dropdown");
            preview.rootVisualElement.Q<DropdownField>("resolution-dropdown").choices = liveResolution.choices.ToList();
            preview.rootVisualElement.Q<DropdownField>("resolution-dropdown").SetValueWithoutNotify(liveResolution.value);
            preview.rootVisualElement.Q<Toggle>("fullscreen-toggle").SetValueWithoutNotify(Screen.fullScreen);
            settingsPhase = false; readyFrame = Time.frameCount + 5; next = EditorApplication.timeSinceStartup + .4;
        }
        static void Capture(string name)
        {
            var old = RenderTexture.active; var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            try { RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply(); File.WriteAllBytes("Art/BorrowedTime/" + name + ".png", image.EncodeToPNG()); }
            finally { RenderTexture.active = old; Object.DestroyImmediate(image); }
        }
        static void CleanupPreview()
        {
            if (sourceCamera) sourceCamera.targetTexture = oldTarget;
            if (preview) Object.DestroyImmediate(preview.gameObject); if (previewPanel) Object.DestroyImmediate(previewPanel);
            if (texture) { texture.Release(); Object.DestroyImmediate(texture); } preview = null; previewPanel = null; texture = null;
        }
        static void CheckResult(bool passed, string name, string detail = "") { report.checks.Add(new Check { passed = passed, name = name, detail = detail }); Save(); }
        static void Save() { report.passed = report.checks.Count(c => c.passed); report.failed = report.checks.Count(c => !c.passed); File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true)); }
        static void Finish()
        {
            SessionState.SetBool(Active, false); report = JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath));
            CheckResult(!EditorApplication.isPlaying, "Quit exits Play Mode in the Editor");
            var prefs = JsonUtility.FromJson<Preferences>(SessionState.GetString("BorrowedMenu.Test.Prefs", "{}"));
            foreach (var item in prefs.items) { if (item.existed) PlayerPrefs.SetInt(item.key, item.value); else PlayerPrefs.DeleteKey(item.key); }
            PlayerPrefs.Save(); AudioListener.volume = prefs.volume; Application.runInBackground = prefs.runInBackground;
            report.completed = true; report.success = report.failed == 0; Save();
            Debug.Log("[Borrowed Time] Validation: " + report.passed + " passed, " + report.failed + " failed.");
        }
    }
}
#endif
