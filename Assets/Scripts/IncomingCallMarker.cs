using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    [RequireComponent(typeof(Button))]
    public sealed class IncomingCallMarker : MonoBehaviour
    {
        public string caseId;
        public Image dot, halo;
        public Text number, locality;
        public RectTransform pulse;
        public bool openBoardOnSelect;
        private Button button;
        private GameManager game;
        private bool waiting, selected;
        private void Awake() { button = GetComponent<Button>(); button.onClick.AddListener(Select); }
        public void Bind(GameManager manager) { game = manager; Refresh(); }
        private void Select()
        {
            if (game == null || !game.SelectIncomingCall(caseId)) return;
            if (openBoardOnSelect) game.View.IncomingBoard.Show();
        }
        public void Refresh()
        {
            if (game?.Queue == null) { gameObject.SetActive(false); return; }
            EmergencyCase definition = null;
            foreach (var c in game.Cases) if (c.id == caseId) { definition = c; break; }
            if (definition == null) { gameObject.SetActive(false); return; }
            bool arrived = game.IsCaseInScope(caseId) && game.Queue.HasArrived(definition);
            gameObject.SetActive(arrived);
            waiting = game.IsWaiting(definition);
            selected = game.Session?.Definition.id == caseId;
            bool complete = game.IsCompleted(caseId);
            Color color = complete ? DispatchView.Green : selected ? DispatchView.CyanAccent : DispatchView.Red;
            dot.color = color; halo.color = new Color(color.r, color.g, color.b, .4f);
            if (locality != null) locality.color = complete ? DispatchView.Muted : selected ? Color.white : new Color(.82f,.91f,.95f);
            if (button == null) button = GetComponent<Button>();
            button.interactable = arrived && waiting && game.CanChooseCall;
        }
        private void Update()
        {
            if (pulse != null) pulse.localScale = Vector3.one * (waiting ? 1 + Mathf.PingPong(Time.unscaledTime * .48f, .42f) : 1);
            if (halo != null) { var c = halo.color; c.a = waiting ? .18f + .28f * (1 - Mathf.PingPong(Time.unscaledTime * .8f, 1)) : selected ? .5f : .12f; halo.color = c; }
        }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Select); }
    }
}
