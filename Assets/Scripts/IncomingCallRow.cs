using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    [RequireComponent(typeof(Button))]
    public sealed class IncomingCallRow : MonoBehaviour
    {
        public string caseId;
        public Text title, status;
        public HudShape background;
        private Button button;
        private GameManager game;
        private EmergencyCase definition;
        private void Awake() { button = GetComponent<Button>(); button.onClick.AddListener(Select); }
        public void Bind(GameManager manager)
        { game = manager; foreach (var c in game.Cases) if (c.id == caseId) definition = c; Refresh(); }
        private void Select() => game?.SelectIncomingCall(caseId);
        public void Refresh()
        {
            if (game == null || definition == null) return;
            gameObject.SetActive(game.IsCaseInScope(caseId));
            bool arrived = game.Queue?.HasArrived(definition) == true, complete = game.IsCompleted(caseId);
            bool selected = game.Session?.Definition.id == caseId;
            bool waiting = game.IsWaiting(definition);
            title.text = definition.locality;
            if(button == null) button = GetComponent<Button>();
            button.interactable = waiting && game.CanChooseCall;
            Color tone = !arrived ? DispatchView.Muted : complete ? DispatchView.Green : selected ? DispatchView.CyanAccent : DispatchView.Red;
            title.color = arrived ? Color.white : DispatchView.Muted; status.color = tone;
            status.text = !arrived ? "SIN SEÑAL" : complete ? "ATENDIDA" : !waiting ? "EN LÍNEA" : IncomingCallBoard.Duration(game.Queue.WaitingSeconds(definition));
            background.edge = selected ? DispatchView.CyanAccent : new Color(tone.r,tone.g,tone.b,.3f);
            background.color = selected ? new Color(.025f,.16f,.23f) : new Color(.022f,.055f,.085f); background.SetVerticesDirty();
        }
        private void OnDestroy() { if(button != null) button.onClick.RemoveListener(Select); }
    }
}
