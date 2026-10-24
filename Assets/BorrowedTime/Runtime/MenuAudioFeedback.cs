using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BorrowedTime.Menu
{
    /// <summary>Continuous soundtrack and UI feedback routed through the existing audio settings.</summary>
    public sealed class MenuAudioFeedback : MonoBehaviour
    {
        public UIDocument menuDocument;
        public AudioSource music, hover, click;
        [Range(0, 1)] public float hoverLoudness = .35f;
        [Range(0, 1)] public float clickLoudness = .65f;
        static MenuAudioFeedback active;
        readonly List<Button> buttons = new List<Button>();
        readonly Dictionary<int, Button> pressed = new Dictionary<int, Button>();
        VisualElement boundRoot;
        bool ownsPlayback;
        float lastHover = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active = null;

        void Awake()
        {
            if (active && active != this)
            {
                active.SetDocument(menuDocument);
                Destroy(gameObject);
                return;
            }
            active = this; ownsPlayback = true;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (!ownsPlayback) return;
            if (music && music.clip && !music.isPlaying) music.Play();
            BindButtons();
        }

        void Update() { if (ownsPlayback) BindButtons(); }

        void SetDocument(UIDocument document)
        {
            ReleaseButtons(); menuDocument = document;
        }

        void BindButtons()
        {
            if (!menuDocument) { if (boundRoot != null) ReleaseButtons(); return; }
            var root = menuDocument.rootVisualElement;
            if (root == boundRoot && buttons.Count > 0) return;
            ReleaseButtons(); boundRoot = root;
            foreach (string name in new[] { "play-button", "setting-button", "quit-button", "back-button" })
            {
                var button = root.Q<Button>(name);
                if (button == null) continue;
                buttons.Add(button);
                button.RegisterCallback<PointerEnterEvent>(PointerEntered);
                button.RegisterCallback<PointerDownEvent>(PointerDown, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerUpEvent>(PointerUp, TrickleDown.TrickleDown);
                button.RegisterCallback<PointerCancelEvent>(PointerCancelled);
                button.RegisterCallback<NavigationSubmitEvent>(Submitted, TrickleDown.TrickleDown);
            }
        }

        void PointerEntered(PointerEnterEvent e)
        {
            if (!((Button)e.currentTarget).enabledInHierarchy || Time.unscaledTime - lastHover < .045f) return;
            lastHover = Time.unscaledTime;
            if (hover && hover.clip) hover.PlayOneShot(hover.clip, hoverLoudness);
        }

        void PointerDown(PointerDownEvent e)
        {
            var button = (Button)e.currentTarget;
            if (e.button == 0 && button.enabledInHierarchy) pressed[e.pointerId] = button;
        }

        void PointerUp(PointerUpEvent e)
        {
            if (e.button != 0) return;
            var button = (Button)e.currentTarget;
            bool valid = pressed.TryGetValue(e.pointerId, out var origin) && origin == button && button.enabledInHierarchy &&
                button.worldBound.Contains(new Vector2(e.position.x, e.position.y));
            pressed.Remove(e.pointerId);
            if (valid) PlayClick();
        }

        void PointerCancelled(PointerCancelEvent e) => pressed.Remove(e.pointerId);
        void Submitted(NavigationSubmitEvent e) { if (((Button)e.currentTarget).enabledInHierarchy) PlayClick(); }
        void PlayClick() { if (click && click.clip) click.PlayOneShot(click.clip, clickLoudness); }

        internal static void PlayTitleClick(VisualElement title)
        {
            if (active && active.boundRoot != null && title.panel == active.boundRoot.panel)
                active.PlayClick();
        }

        void ReleaseButtons()
        {
            foreach (var button in buttons)
            {
                button.UnregisterCallback<PointerEnterEvent>(PointerEntered);
                button.UnregisterCallback<PointerDownEvent>(PointerDown, TrickleDown.TrickleDown);
                button.UnregisterCallback<PointerUpEvent>(PointerUp, TrickleDown.TrickleDown);
                button.UnregisterCallback<PointerCancelEvent>(PointerCancelled);
                button.UnregisterCallback<NavigationSubmitEvent>(Submitted, TrickleDown.TrickleDown);
            }
            buttons.Clear(); pressed.Clear(); boundRoot = null;
        }

        void OnApplicationFocus(bool focused) { if (!focused) pressed.Clear(); }
        void OnDisable() => ReleaseButtons();
        void OnDestroy() { if (active == this) active = null; }
    }
}
