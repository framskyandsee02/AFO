using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BorrowedTime.Menu
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BorrowedMainMenu : MonoBehaviour
    {
        [Tooltip("Existing gameplay scene in Build Settings. Start is the temporary destination.")]
        public string playSceneName = "Start";
        [Tooltip("Use the painted corner foliage overlay; disable for a clear overhead 3D meadow.")]
        public bool showForegroundFoliage = true;
        VisualElement root, overlay;
        Button play, setting, quit, back;
        Toggle mute, fullscreen;
        DropdownField resolution;
        Label status;
        List<Vector2Int> sizes;
        VolumeRow[] rows;
        public bool SettingsVisible { get; private set; }

        sealed class VolumeRow
        {
            readonly SliderInt slider; readonly IntegerField number; readonly MenuAudioChannel channel;
            public VolumeRow(VisualElement root, string prefix, MenuAudioChannel channel)
            {
                this.channel = channel; slider = root.Q<SliderInt>(prefix + "-volume"); number = root.Q<IntegerField>(prefix + "-number");
                slider.RegisterValueChangedCallback(Change); number.RegisterValueChangedCallback(Change);
            }
            void Change(ChangeEvent<int> e) => MenuAudioSettings.Set(channel, e.newValue);
            public void Sync() { int value = MenuAudioSettings.Get(channel); slider.SetValueWithoutNotify(value); number.SetValueWithoutNotify(value); }
            public void Release() { slider.UnregisterValueChangedCallback(Change); number.UnregisterValueChangedCallback(Change); }
        }
        void Awake()
        {
            if (!Application.isEditor && PlayerPrefs.HasKey("BorrowedTime.Display.Width"))
                Screen.SetResolution(Mathf.Max(640, PlayerPrefs.GetInt("BorrowedTime.Display.Width")), Mathf.Max(480, PlayerPrefs.GetInt("BorrowedTime.Display.Height")), PlayerPrefs.GetInt("BorrowedTime.Display.Fullscreen", 1) != 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement.Q("menu-root");
            InstallArtwork(root);
            root.Q("foreground-foliage").style.display = showForegroundFoliage ? DisplayStyle.Flex : DisplayStyle.None;
            overlay = root.Q("settings-overlay"); status = root.Q<Label>("menu-status");
            play = root.Q<Button>("play-button"); setting = root.Q<Button>("setting-button"); quit = root.Q<Button>("quit-button"); back = root.Q<Button>("back-button");
            play.clicked += Play; setting.clicked += OpenSettings; back.clicked += CloseSettings; quit.clicked += Quit;
            rows = new[] { new VolumeRow(root, "master", MenuAudioChannel.Master), new VolumeRow(root, "music", MenuAudioChannel.Music), new VolumeRow(root, "effects", MenuAudioChannel.Effects) };
            mute = root.Q<Toggle>("mute-toggle"); fullscreen = root.Q<Toggle>("fullscreen-toggle"); resolution = root.Q<DropdownField>("resolution-dropdown");
            MenuAudioSettings.Reload(); MenuAudioSettings.Changed += SyncAudio; SyncAudio();
            mute.RegisterValueChangedCallback(MuteChanged);
            sizes = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Where(s => s.x >= 640 && s.y >= 480).Distinct().ToList();
            if (sizes.Count == 0) sizes.AddRange(new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080) });
            var current = new Vector2Int(Screen.width, Screen.height); if (!sizes.Contains(current)) sizes.Add(current);
            sizes = sizes.OrderBy(s => s.x).ThenBy(s => s.y).ToList();
            resolution.choices = sizes.Select(s => s.x + " × " + s.y).ToList(); resolution.SetValueWithoutNotify(resolution.choices[sizes.IndexOf(current)]);
            fullscreen.SetValueWithoutNotify(Screen.fullScreen);
            resolution.RegisterValueChangedCallback(ResolutionChanged); fullscreen.RegisterValueChangedCallback(FullscreenChanged);
            root.RegisterCallback<KeyDownEvent>(KeyDown); SettingsVisible = false; overlay.AddToClassList("hidden"); root.RemoveFromClassList("settings-open");
        }
        void OnDisable()
        {
            if (root == null) return;
            play.clicked -= Play; setting.clicked -= OpenSettings; back.clicked -= CloseSettings; quit.clicked -= Quit;
            foreach (var row in rows) row.Release(); MenuAudioSettings.Changed -= SyncAudio;
            mute.UnregisterValueChangedCallback(MuteChanged); resolution.UnregisterValueChangedCallback(ResolutionChanged); fullscreen.UnregisterValueChangedCallback(FullscreenChanged);
            root.UnregisterCallback<KeyDownEvent>(KeyDown); PlayerPrefs.Save();
        }
        public static void InstallArtwork(VisualElement root)
        {
            var leaves = root.Q("falling-leaves"); if (leaves.Q<FallingLeavesLayer>() == null) leaves.Add(new FallingLeavesLayer());
            var title = root.Q("game-title");
            if (title.Q<ReferenceGlassSurface>() == null) title.Insert(0, new ReferenceGlassSurface(true));
            string[] names = { "play-button", "setting-button", "quit-button" };
            foreach (string name in names)
            {
                var button = root.Q<Button>(name);
                button.Q<BotanicalIcon>()?.RemoveFromHierarchy();
                button.Q<GreenGlassButtonTrim>()?.RemoveFromHierarchy();
                if (button.Q<ReferenceGlassSurface>() == null) button.Insert(0, new ReferenceGlassSurface(false));
                if (button.Q<Label>("reference-button-label") == null)
                {
                    var label = new Label(button.text) { name = "reference-button-label", pickingMode = PickingMode.Ignore, focusable = false };
                    label.AddToClassList("reference-button-label"); button.Add(label);
                }
            }
            MenuPlayfulMotion.Install(root);
            if (!root.ClassListContains("layout-ready")) { root.AddToClassList("layout-ready"); root.RegisterCallback<GeometryChangedEvent>(_ => UpdateLayout(root)); }
            UpdateLayout(root);
        }
        static void UpdateLayout(VisualElement root)
        {
            root.EnableInClassList("compact", root.layout.width > 0 && root.layout.width < 1400);
            root.EnableInClassList("portrait", root.layout.width > 0 && root.layout.height > root.layout.width * 1.1f);
        }
        void SyncAudio() { foreach (var row in rows) row.Sync(); mute.SetValueWithoutNotify(MenuAudioSettings.Muted); }
        void MuteChanged(ChangeEvent<bool> e) => MenuAudioSettings.SetMuted(e.newValue);
        void ResolutionChanged(ChangeEvent<string> e) => ApplyDisplay();
        void FullscreenChanged(ChangeEvent<bool> e) => ApplyDisplay();
        void ApplyDisplay()
        {
            int index = resolution.choices.IndexOf(resolution.value); if (index < 0) return; var size = sizes[index];
            Screen.SetResolution(size.x, size.y, fullscreen.value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            PlayerPrefs.SetInt("BorrowedTime.Display.Width", size.x); PlayerPrefs.SetInt("BorrowedTime.Display.Height", size.y); PlayerPrefs.SetInt("BorrowedTime.Display.Fullscreen", fullscreen.value ? 1 : 0); PlayerPrefs.Save();
        }
        void OpenSettings() { SettingsVisible = true; root.AddToClassList("settings-open"); overlay.RemoveFromClassList("hidden"); root.Q<SliderInt>("master-volume").Focus(); }
        void CloseSettings() { SettingsVisible = false; overlay.AddToClassList("hidden"); root.RemoveFromClassList("settings-open"); PlayerPrefs.Save(); setting.Focus(); }
        void KeyDown(KeyDownEvent e) { if (SettingsVisible && e.keyCode == KeyCode.Escape) { CloseSettings(); e.StopPropagation(); } }
        void Play()
        {
            if (!Application.CanStreamedLevelBeLoaded(playSceneName)) { status.text = "Choose a gameplay scene in the menu Inspector and Build Settings."; return; }
            PlayerPrefs.Save(); SceneManager.LoadScene(playSceneName);
        }
        void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnApplicationFocus(bool focused) { if (!focused) PlayerPrefs.Save(); }
        void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
        void OnApplicationQuit() => PlayerPrefs.Save();
    }
}
