using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BorrowedTime.Menu
{
    /// <summary>The original frosted glass silhouette, recolored in soft meadow greens.</summary>
    public sealed class ReferenceGlassSurface : VisualElement
    {
        static readonly CustomStyleProperty<Color> Rim = new CustomStyleProperty<Color>("--glass-rim");
        static readonly CustomStyleProperty<Color> Top = new CustomStyleProperty<Color>("--glass-top");
        static readonly CustomStyleProperty<Color> Bottom = new CustomStyleProperty<Color>("--glass-bottom");
        Color rim = new Color(.90f, 1f, .82f), top = new Color(.36f, .72f, .46f, .92f), bottom = new Color(.45f, .72f, .53f, .93f);
        readonly bool title;
        public ReferenceGlassSurface(bool title)
        {
            this.title = title; name = "reference-glass-surface";
            pickingMode = PickingMode.Ignore; focusable = false;
            style.position = Position.Absolute; style.left = style.top = style.right = style.bottom = 0;
            RegisterCallback<CustomStyleResolvedEvent>(_ =>
            {
                if (customStyle.TryGetValue(Rim, out var value)) rim = value;
                if (customStyle.TryGetValue(Top, out value)) top = value;
                if (customStyle.TryGetValue(Bottom, out value)) bottom = value;
                MarkDirtyRepaint();
            });
            generateVisualContent += Draw;
        }
        void Draw(MeshGenerationContext context)
        {
            float w = contentRect.width, h = contentRect.height;
            if (w < 24 || h < 24) return;
            float radius = Mathf.Min(title ? 44 : 42, h * .38f);
            var shape = Shape(new Rect(4, 4, w - 8, h - 8), radius);
            Fill(context, shape, h, top, bottom);
            // Original broad diagonal reflection, always clipped to the actual rounded shape.
            var sheen = Clip(new List<Vector2> { new Vector2(-w * .10f, 0), new Vector2(w * .36f, 0), new Vector2(w * .08f, h), new Vector2(-w * .38f, h) }, shape);
            Fill(context, sheen, h, new Color(1, 1, 1, .30f), new Color(.91f, 1, .89f, .025f));
            var lowerSheen = Clip(new List<Vector2> { new Vector2(w * .98f, h * .32f), new Vector2(w * 1.30f, h * .32f), new Vector2(w * .98f, h), new Vector2(w * .66f, h) }, shape);
            Fill(context, lowerSheen, h, new Color(.91f, 1, .85f, .01f), new Color(.91f, 1, .85f, .23f));
            var gloss = Clip(new List<Vector2> { new Vector2(0, 0), new Vector2(w, 0), new Vector2(w, h * .18f), new Vector2(0, h * .18f) }, shape);
            Fill(context, gloss, h * .18f, new Color(.91f, 1, .86f, .25f), new Color(.91f, 1, .86f, 0));
            for (int i = 0; i < (title ? 12 : 8); i++)
            {
                float size = (title ? 15 : 13) + (i % 3) * 4;
                float x = title ? (i < 5 ? w * (.08f + (i % 3) * .045f) : w * (.69f + (i % 4) * .045f)) : w * (.75f + (i % 3) * .043f);
                float y = h * (.12f + ((i * 3) % 8) * .09f);
                Fill(context, Clip(Rounded(new Rect(x, y, size, size), 3), shape), h, new Color(.88f, 1, .76f, .20f), new Color(.88f, 1, .76f, .09f));
            }
            var painter = context.painter2D;
            Outline(painter, shape, new Color(.61f, .89f, .57f, .22f), 10);
            Outline(painter, shape, rim, 3.5f);
            Outline(painter, Shape(new Rect(11, 11, w - 22, h - 22), Mathf.Max(4, radius - 7)), new Color(.75f, .98f, .69f, .94f), 2.1f);
            if (!title)
            {
                painter.strokeColor = new Color(.94f, 1, .87f); painter.lineWidth = 5;
                painter.lineCap = LineCap.Round; painter.lineJoin = LineJoin.Round;
                painter.BeginPath(); painter.MoveTo(new Vector2(h * .34f, h * .28f)); painter.LineTo(new Vector2(h * .34f, h * .72f)); painter.Stroke();
                float arrowX = w - h * .34f;
                painter.BeginPath(); painter.MoveTo(new Vector2(arrowX - h * .055f, h * .37f)); painter.LineTo(new Vector2(arrowX + h * .055f, h * .5f)); painter.LineTo(new Vector2(arrowX - h * .055f, h * .63f)); painter.Stroke();
            }
        }
        List<Vector2> Shape(Rect rect, float radius)
        {
            if (title) return Rounded(rect, radius);
            float cut = rect.height * .26f;
            var polygon = new List<Vector2> { new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax - cut, rect.yMin), new Vector2(rect.xMax, rect.yMin + cut), new Vector2(rect.xMax, rect.yMax - cut), new Vector2(rect.xMax - cut, rect.yMax), new Vector2(rect.xMin, rect.yMax) };
            var rounded = new List<Vector2>();
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 current = polygon[i], previous = polygon[(i + polygon.Count - 1) % polygon.Count], next = polygon[(i + 1) % polygon.Count];
                float inset = i == 0 || i == polygon.Count - 1 ? radius : 9;
                Vector2 start = current + (previous - current).normalized * Mathf.Min(inset, Vector2.Distance(current, previous) * .45f);
                Vector2 end = current + (next - current).normalized * Mathf.Min(inset, Vector2.Distance(current, next) * .45f);
                for (int step = 0; step <= 8; step++) { float t = step / 8f; rounded.Add((1-t)*(1-t)*start + 2*(1-t)*t*current + t*t*end); }
            }
            return rounded;
        }
        static List<Vector2> Rounded(Rect rect, float radius)
        {
            var points = new List<Vector2>(36);
            var centers = new[] { new Vector2(rect.xMax-radius, rect.yMin+radius), new Vector2(rect.xMax-radius, rect.yMax-radius), new Vector2(rect.xMin+radius, rect.yMax-radius), new Vector2(rect.xMin+radius, rect.yMin+radius) };
            for (int corner = 0; corner < 4; corner++) for (int step = 0; step <= 8; step++) { float a = (-90 + corner*90 + step*90f/8)*Mathf.Deg2Rad; points.Add(centers[corner] + new Vector2(Mathf.Cos(a), Mathf.Sin(a))*radius); }
            return points;
        }
        static void Outline(Painter2D painter, List<Vector2> points, Color color, float width)
        {
            painter.strokeColor = color; painter.lineWidth = width; painter.BeginPath(); painter.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++) painter.LineTo(points[i]); painter.ClosePath(); painter.Stroke();
        }
        static void Fill(MeshGenerationContext context, List<Vector2> points, float height, Color top, Color bottom)
        {
            if (points.Count < 3) return;
            var mesh = context.Allocate(points.Count, (points.Count-2)*3);
            foreach (var point in points) mesh.SetNextVertex(new Vertex { position = new Vector3(point.x, point.y, Vertex.nearZ), tint = Color.Lerp(top, bottom, Mathf.Clamp01(point.y/height)), uv = Vector2.zero });
            for (ushort i = 1; i < points.Count-1; i++) { mesh.SetNextIndex(0); mesh.SetNextIndex(i); mesh.SetNextIndex((ushort)(i+1)); }
        }
        static float Cross(Vector2 a, Vector2 b) => a.x*b.y-a.y*b.x;
        static List<Vector2> Clip(List<Vector2> polygon, List<Vector2> boundary)
        {
            for (int i = 0; i < boundary.Count && polygon.Count > 0; i++)
            {
                Vector2 a = boundary[i], edge = boundary[(i+1)%boundary.Count]-a;
                var output = new List<Vector2>(); Vector2 previous = polygon[polygon.Count-1]; float previousSide = Cross(edge, previous-a);
                foreach (var current in polygon) { float side = Cross(edge, current-a); if ((side>=0)!=(previousSide>=0)) output.Add(Vector2.Lerp(previous,current,previousSide/(previousSide-side))); if (side>=0) output.Add(current); previous=current; previousSide=side; }
                polygon=output;
            }
            return polygon;
        }
    }
}
