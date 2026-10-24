using UnityEngine;
using UnityEngine.UIElements;

namespace BorrowedTime.Menu
{
    /// <summary>Gentle label motion and a tactile title toy, independent of game actions.</summary>
    public sealed class MenuPlayfulMotion
    {
        readonly VisualElement title;
        readonly Label[] labels;
        readonly ReferenceGlassSurface surface;
        bool held, hovered;
        float compression, velocity, lastTime;

        MenuPlayfulMotion(VisualElement root)
        {
            title = root.Q("game-title");
            surface = title.Q<ReferenceGlassSurface>();
            labels = new[] {
                root.Q<Label>("title-borrowed"), root.Q<Label>("title-time"),
                root.Q("play-button").Q<Label>("reference-button-label"),
                root.Q("setting-button").Q<Label>("reference-button-label"),
                root.Q("quit-button").Q<Label>("reference-button-label")
            };
            title.pickingMode = PickingMode.Position;
            title.focusable = true; title.tabIndex = 0;
            title.tooltip = "BORROWED TIME — Click for a little bounce";
            title.AddManipulator(new Clickable(Activate));
            title.RegisterCallback<PointerEnterEvent>(_ => hovered = true);
            title.RegisterCallback<PointerLeaveEvent>(_ => hovered = false);
            // Observe the press before Clickable consumes the pointer event at the target.
            title.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { held = true; title.Focus(); } }, TrickleDown.TrickleDown);
            title.RegisterCallback<PointerUpEvent>(_ => held = false, TrickleDown.TrickleDown);
            title.RegisterCallback<PointerCancelEvent>(_ => held = false);
            title.RegisterCallback<PointerCaptureOutEvent>(_ => held = false);
            title.RegisterCallback<BlurEvent>(_ => held = false);
            title.RegisterCallback<NavigationSubmitEvent>(e => { Activate(); e.StopPropagation(); });
            title.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Space && e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter) return;
                Activate(); e.StopPropagation();
            });
            lastTime = Time.realtimeSinceStartup;
            root.schedule.Execute(Tick).Every(16);
        }

        public static void Install(VisualElement root)
        {
            if (root.ClassListContains("playful-motion-ready")) return;
            root.AddToClassList("playful-motion-ready");
            new MenuPlayfulMotion(root);
        }

        void Activate()
        {
            MenuAudioFeedback.PlayTitleClick(title);
            held = false;
            velocity = -10f;
            surface.PlayLeafBounce();
        }

        void Tick()
        {
            float now = Time.realtimeSinceStartup;
            float dt = Mathf.Clamp(now - lastTime, 0, .025f); lastTime = now;
            float target = held ? .7f : 0;
            velocity += ((target - compression) * 160f - velocity * 14f) * dt;
            compression = Mathf.Clamp(compression + velocity * dt, -.8f, .8f);
            float hoverScale = hovered ? 1.012f : 1;
            title.style.scale = new Scale(new Vector3(hoverScale + compression * .05f, hoverScale - compression * .09f, 1));
            title.style.translate = new Translate(0, -Mathf.Max(0, -compression) * 16f, 0);
            title.style.rotate = new Rotate(new Angle(compression * 2.4f, AngleUnit.Degree));
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i]; if (label == null) continue;
                float phase = i * 1.35f;
                label.style.translate = new Translate(Mathf.Sin(now * 1.1f + phase) * .8f, Mathf.Sin(now * 2f + phase) * 2.5f, 0);
                label.style.rotate = new Rotate(new Angle(Mathf.Sin(now * 1.5f + phase) * .85f, AngleUnit.Degree));
            }
        }
    }
}
