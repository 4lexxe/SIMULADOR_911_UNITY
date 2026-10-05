using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    // Resolution-independent, cut-corner panels shared by the desk and debrief HUD.
    public sealed class HudShape : Image
    {
        public float cut = 10, border = 1.5f;
        public Color edge = new Color(.08f, .55f, .8f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (sprite != null) { base.OnPopulateMesh(vh); return; }
            vh.Clear();
            Rect r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            float c = Mathf.Min(cut, Mathf.Min(r.width, r.height) * .3f);
            var outer = Outline(r, c);
            var inner = Outline(new Rect(r.x + border, r.y + border, Mathf.Max(0, r.width - border * 2), Mathf.Max(0, r.height - border * 2)), Mathf.Max(0, c - border * .6f));
            vh.AddVert(r.center, color, Vector2.zero);
            for (int i = 0; i < 8; i++) vh.AddVert(inner[i], color, Vector2.zero);
            for (int i = 0; i < 8; i++) vh.AddTriangle(0, i + 1, (i + 1) % 8 + 1);
            Color stroke = edge; stroke.a *= color.a;
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8, start = vh.currentVertCount;
                vh.AddVert(outer[i], stroke, Vector2.zero); vh.AddVert(outer[next], stroke, Vector2.zero);
                vh.AddVert(inner[next], stroke, Vector2.zero); vh.AddVert(inner[i], stroke, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
        }
        private static Vector2[] Outline(Rect r, float c) => new[] {
            new Vector2(r.xMin+c,r.yMax), new Vector2(r.xMax-c,r.yMax), new Vector2(r.xMax,r.yMax-c), new Vector2(r.xMax,r.yMin+c),
            new Vector2(r.xMax-c,r.yMin), new Vector2(r.xMin+c,r.yMin), new Vector2(r.xMin,r.yMin+c), new Vector2(r.xMin,r.yMax-c) };
    }
}
