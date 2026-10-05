using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public sealed class IncomingCallBoard : MonoBehaviour
    {
        [Header("Authored provincial board")]
        public RectTransform panel;
        public IncomingCallMarker[] markers;
        public IncomingCallRow[] rows;
        public Text heading, count, selectedTown, selectedRegion, selectedCopy, selectedWait, answerLabel, footer;
        public Button answer, close;
        public JujuyMapNavigation navigation;
        public RectTransform frame;
        private GameManager game;
        private float nextRefresh;
        public bool Visible => panel != null && panel.gameObject.activeSelf;
        public static string Duration(float seconds) { int s = Mathf.Max(0, (int)seconds); return (s / 60).ToString("00") + ":" + (s % 60).ToString("00"); }
        public void Bind(GameManager manager)
        {
            game = manager; answer.onClick.AddListener(Answer); close.onClick.AddListener(Hide);
            foreach (var marker in markers) marker.Bind(manager);
            foreach (var row in rows) row.Bind(manager);
            Hide();
        }
        public void Show()
        {
            if (game == null || game.View.ResultVisible || game.View.LobbyVisible || game.Voice.Recording) return;
            panel.gameObject.SetActive(true); panel.SetAsLastSibling(); Refresh();
            EventSystem.current?.SetSelectedGameObject(close.gameObject);
        }
        public void Hide() { if(panel != null) panel.gameObject.SetActive(false); EventSystem.current?.SetSelectedGameObject(null); }
        private void Answer() { if(answer.interactable) game.Answer(); }
        public void Refresh()
        {
            if (game == null) return;
            foreach (var marker in markers) marker.Refresh();
            foreach (var row in rows) row.Refresh();
            count.text = game.WaitingCallCount.ToString("00") + " EN ESPERA  /  " + game.Results.Count.ToString("00") + " ATENDIDAS";
            heading.text = game.ExamMode ? "JUJUY / EXAMEN · UNA CAÍDA EN EL CLUB" : "JUJUY / CENTRAL DE LLAMADAS";
            var selected = game.Session?.Definition;
            answer.interactable = game.CanChooseCall && game.Phase == CallPhase.Incoming;
            selectedTown.text = selected == null ? "ELEGÍ UN PUNTO ROJO" : selected.locality.ToUpperInvariant();
            selectedRegion.text = selected == null ? "VOS DECIDÍS A QUIÉN ATENDER" : selected.region.ToUpperInvariant() + "  /  SEÑAL APROXIMADA";
            selectedCopy.text = selected == null ? "Revisá las llamadas en el mapa o en la lista. Seleccionar una señal abre su ficha; atenderla inicia la conversación." : selected.incomingSummary + ".\nConfirmá dirección, situación y personas al atender.";
            selectedWait.text = selected == null ? "SIN LLAMADA SELECCIONADA" : game.Phase == CallPhase.Incoming ? "EN ESPERA  " + Duration(game.Queue.WaitingSeconds(selected)) : "LLAMADA EN CURSO · " + Duration(game.Session.Seconds);
            answerLabel.text = answer.interactable ? "ATENDER LLAMADA" : !game.CanChooseCall ? "ATENDIENDO OTRA LLAMADA" : "SELECCIONÁ UNA LLAMADA";
            answerLabel.color = answer.interactable ? DispatchView.Green : DispatchView.Muted;
            footer.text = !game.CanChooseCall ? "La conversación sigue activa. Volvé al escritorio para terminarla antes de atender otra llamada." : game.WaitingCallCount == 0 && game.Queue != null && !float.IsInfinity(game.Queue.SecondsUntilNextArrival) ? "CANAL ABIERTO · Próxima señal en " + Mathf.CeilToInt(game.Queue.SecondsUntilNextArrival) + " s" : "ROJO: EN ESPERA    CIAN: SELECCIONADA / EN LÍNEA    VERDE: ATENDIDA    ·    Arrastrá para mover el mapa; + / − para acercar.";
        }
        private void Update()
        {
            if(!Visible)return;
            if(frame != null)frame.localScale=Vector3.one*Mathf.Min(1,Mathf.Min(panel.rect.width/1800f,panel.rect.height/990f));
            if(Time.unscaledTime >= nextRefresh) { Refresh(); nextRefresh = Time.unscaledTime + .2f; }
        }
        private void OnDestroy() { if(answer != null) answer.onClick.RemoveListener(Answer); if(close != null) close.onClick.RemoveListener(Hide); }
    }
}
