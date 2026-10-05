using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LobbyShade : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            var dark=new Color(.003f,.018f,.035f,.88f);var clear=new Color(.002f,.016f,.032f,.2f);
            vh.AddVert(new Vector3(r.xMin,r.yMin),dark,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),dark,Vector2.zero);
            vh.AddVert(new Vector3(r.xMax,r.yMax),clear,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),clear,Vector2.zero);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);
        }
    }
}
