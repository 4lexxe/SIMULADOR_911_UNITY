using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public sealed class JujuyMapNavigation : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        public RectTransform content, viewport;
        public Button zoomIn, zoomOut, reset;
        public Text zoomLabel;
        public float ZoomLevel { get; private set; } = 1;
        private Vector2 pointerStart, positionStart;
        private void Awake() { zoomIn.onClick.AddListener(In); zoomOut.onClick.AddListener(Out); reset.onClick.AddListener(ResetMap); }
        public void In() => Zoom(.25f);
        public void Out() => Zoom(-.25f);
        public void Zoom(float amount)
        { ZoomLevel = Mathf.Clamp(ZoomLevel + amount, 1, 2.5f); content.localScale = Vector3.one * ZoomLevel; Clamp(); UpdateLabel(); }
        public void ResetMap() { ZoomLevel = 1; content.localScale = Vector3.one; content.anchoredPosition = Vector2.zero; UpdateLabel(); }
        private void UpdateLabel() { if(zoomLabel != null) zoomLabel.text = ZoomLevel.ToString("0.00") + "×"; }
        public void OnBeginDrag(PointerEventData e)
        { RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,e.position,e.pressEventCamera,out pointerStart);positionStart=content.anchoredPosition; }
        public void OnDrag(PointerEventData e)
        { RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,e.position,e.pressEventCamera,out var point);content.anchoredPosition=positionStart+point-pointerStart;Clamp(); }
        public void OnScroll(PointerEventData e) => Zoom(e.scrollDelta.y > 0 ? .15f : -.15f);
        private void Clamp()
        {
            var limit = Vector2.Max(Vector2.zero,(content.rect.size * ZoomLevel - viewport.rect.size) / 2);
            var p=content.anchoredPosition;content.anchoredPosition=new Vector2(Mathf.Clamp(p.x,-limit.x,limit.x),Mathf.Clamp(p.y,-limit.y,limit.y));
        }
        private void OnDestroy() { if(zoomIn!=null)zoomIn.onClick.RemoveListener(In);if(zoomOut!=null)zoomOut.onClick.RemoveListener(Out);if(reset!=null)reset.onClick.RemoveListener(ResetMap); }
    }
}
