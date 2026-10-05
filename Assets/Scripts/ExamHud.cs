using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    // Permanent, Inspector-editable HUD. Only its visibility and text change in Play.
    public sealed class ExamHud : MonoBehaviour
    {
        public GameManager game;
        public RectTransform panel;
        public Text label;
        private bool warned;
        private void LateUpdate()
        {
            bool visible = game != null && game.ExamMode && !game.ExamFinished;
            panel.gameObject.SetActive(visible);
            if (!visible) { warned = false; return; }
            panel.SetAsLastSibling();
            int seconds = Mathf.CeilToInt(game.ExamRemaining);
            label.text = "EXAMEN  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00") + " / 03:00";
            label.color = seconds <= 30 ? DispatchView.Amber : DispatchView.CyanAccent;
            if (seconds <= 30 && !warned) { warned = true; EmergencyAudioManager.Instance?.PlayRadioBeep(); }
            if (seconds > 30) warned = false;
        }
    }
}
