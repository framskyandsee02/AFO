using UnityEngine;
using UnityEngine.UIElements;

namespace BorrowedTime.Menu
{
    /// <summary>White inlays matching the green glass reference, drawn independently of text.</summary>
    public sealed class GreenGlassButtonTrim : VisualElement
    {
        public GreenGlassButtonTrim()
        {
            AddToClassList("glass-button-trim"); pickingMode = PickingMode.Ignore; focusable = false;
            generateVisualContent += Draw;
        }
        void Draw(MeshGenerationContext context)
        {
            float w = contentRect.width, h = contentRect.height;
            if (w < 10 || h < 10) return;
            var p = context.painter2D; p.strokeColor = new Color(.96f, 1, .84f);
            p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round; p.lineWidth = Mathf.Clamp(h * .035f, 3, 6);
            p.BeginPath(); p.MoveTo(new Vector2(w * .13f, h * .34f)); p.LineTo(new Vector2(w * .13f, h * .67f)); p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(w * .84f, h * .40f)); p.LineTo(new Vector2(w * .87f, h * .51f)); p.LineTo(new Vector2(w * .84f, h * .62f)); p.Stroke();
        }
    }

    /// <summary>Native UI icons; decoration never participates in hit testing or navigation.</summary>
    public sealed class BotanicalIcon : VisualElement
    {
        readonly int kind;
        static readonly Color Ink = new Color(0.32f, 0.41f, 0.20f);
        public BotanicalIcon(int kind)
        {
            this.kind = kind; AddToClassList("button-icon"); pickingMode = PickingMode.Ignore; focusable = false;
            generateVisualContent += Draw;
        }
        void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D; float s = Mathf.Min(contentRect.width, contentRect.height);
            if (s < 1) return;
            p.fillColor = Ink; p.strokeColor = Ink; p.lineWidth = s * .075f; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            if (kind == 0)
            {
                p.BeginPath(); p.MoveTo(new Vector2(s * .16f, s * .77f));
                p.BezierCurveTo(new Vector2(-s * .06f, s * .16f), new Vector2(s * .67f, s * .20f), new Vector2(s * .89f, s * .02f));
                p.BezierCurveTo(new Vector2(s * 1.04f, s * .62f), new Vector2(s * .48f, s * 1.02f), new Vector2(s * .16f, s * .77f)); p.Fill();
                p.BeginPath(); p.MoveTo(new Vector2(s * .06f, s * .98f)); p.LineTo(new Vector2(s * .65f, s * .30f)); p.Stroke();
            }
            else if (kind == 1)
            {
                p.BeginPath();
                for (int i = 0; i < 40; i++)
                {
                    float a = i * Mathf.PI * 2 / 40f, r = (i % 5 == 0 || i % 5 == 4) ? .34f : .47f;
                    var v = new Vector2(.5f + Mathf.Cos(a) * r, .5f + Mathf.Sin(a) * r) * s;
                    if (i == 0) p.MoveTo(v); else p.LineTo(v);
                }
                p.ClosePath(); p.Fill(); p.fillColor = new Color(.98f, .95f, .82f);
                p.BeginPath(); p.Arc(new Vector2(s * .5f, s * .5f), s * .16f, 0, 360); p.Fill();
            }
            else
            {
                p.BeginPath(); p.MoveTo(new Vector2(s * .52f, s * .12f)); p.LineTo(new Vector2(s * .13f, s * .12f)); p.LineTo(new Vector2(s * .13f, s * .89f)); p.LineTo(new Vector2(s * .52f, s * .89f)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(s * .40f, s * .50f)); p.LineTo(new Vector2(s * .95f, s * .50f)); p.LineTo(new Vector2(s * .74f, s * .29f)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(s * .95f, s * .50f)); p.LineTo(new Vector2(s * .74f, s * .71f)); p.Stroke();
            }
        }
    }

    /// <summary>A separate reusable UI particle layer with gentle drift, flutter and rotation.</summary>
    public sealed class FallingLeavesLayer : VisualElement
    {
        struct Leaf { public float x, phase, speed, size, spin, age; public Color color; }
        readonly Leaf[] leaves = new Leaf[18];
        float last;
        public FallingLeavesLayer()
        {
            name = "leaf-particles"; pickingMode = PickingMode.Ignore; focusable = false;
            style.position = Position.Absolute; style.left = style.right = style.top = style.bottom = 0;
            var random = new System.Random(2731);
            for (int i = 0; i < leaves.Length; i++) leaves[i] = new Leaf {
                x = (float)random.NextDouble(), phase = (float)random.NextDouble() * 6.28f,
                speed = .024f + (float)random.NextDouble() * .021f, size = 18 + (float)random.NextDouble() * 24,
                spin = .25f + (float)random.NextDouble() * .55f, age = (float)random.NextDouble() * 1.25f,
                color = i % 3 == 0 ? new Color(.96f, .87f, .43f, .78f) : new Color(.68f, .78f, .33f, .72f)
            };
            last = Time.realtimeSinceStartup;
            schedule.Execute(Tick).Every(16); generateVisualContent += Draw;
        }
        void Tick()
        {
            float now = Time.realtimeSinceStartup, dt = Mathf.Min(.05f, Mathf.Max(0, now - last)); last = now;
            for (int i = 0; i < leaves.Length; i++) { var leaf = leaves[i]; leaf.age += dt * leaf.speed; if (leaf.age > 1.3f) leaf.age -= 1.3f; leaves[i] = leaf; }
            MarkDirtyRepaint();
        }
        void Draw(MeshGenerationContext context)
        {
            float w = contentRect.width, h = contentRect.height, time = Time.realtimeSinceStartup;
            var p = context.painter2D;
            foreach (var leaf in leaves)
            {
                float angle = leaf.phase + time * leaf.spin, flutter = .45f + .55f * Mathf.Abs(Mathf.Sin(time * .8f + leaf.phase));
                var center = new Vector2(w * (leaf.x + .045f * Mathf.Sin(leaf.age * 9 + leaf.phase)), h * (leaf.age - .13f));
                Vector2 Point(float x, float y) { x *= flutter; return center + new Vector2(Mathf.Cos(angle) * x - Mathf.Sin(angle) * y, Mathf.Sin(angle) * x + Mathf.Cos(angle) * y) * leaf.size; }
                p.fillColor = leaf.color;
                p.BeginPath(); p.MoveTo(Point(-.65f, 0)); p.BezierCurveTo(Point(-.2f, -.47f), Point(.34f, -.39f), Point(.65f, 0));
                p.BezierCurveTo(Point(.25f, .42f), Point(-.35f, .43f), Point(-.65f, 0)); p.Fill();
                p.strokeColor = new Color(.49f, .59f, .25f, .38f); p.lineWidth = 1;
                p.BeginPath(); p.MoveTo(Point(-.6f, 0)); p.LineTo(Point(.7f, 0)); p.Stroke();
            }
        }
    }
}
