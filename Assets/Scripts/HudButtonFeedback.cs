using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    [RequireComponent(typeof(Button))]
    public sealed class HudButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Button button;
        private Vector3 original;
        private bool over, pressed;
        private void Awake() { button=GetComponent<Button>();original=transform.localScale; }
        public void OnPointerEnter(PointerEventData e) { over=true;if(button.interactable)EmergencyAudioManager.Instance?.PlayHover(); }
        public void OnPointerExit(PointerEventData e) { over=false;pressed=false; }
        public void OnPointerDown(PointerEventData e) => pressed=true;
        public void OnPointerUp(PointerEventData e) => pressed=false;
        public void OnSelect(BaseEventData e) => over=true;
        public void OnDeselect(BaseEventData e) { over=false;pressed=false; }
        private void Update() { float target=!button.interactable?1:pressed?.975f:over?1.015f:1;transform.localScale=Vector3.Lerp(transform.localScale,original*target,1-Mathf.Exp(-Time.unscaledDeltaTime*22)); }
        private void OnDisable() { transform.localScale=original;over=false;pressed=false; }
    }
}
