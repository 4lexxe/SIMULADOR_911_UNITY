using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public enum DispatchAction
    {
        StartGuard, ContinueGuard, LobbyPage, Exit, OpenMenu, CheckConnection, Mode, LiveMode, LocalMode,
        SaveSettings, Camera, Type, Send, Offline, CenterTab, Protocol, Filter, ZoomIn, ZoomOut,
        CenterMap, Answer, Finish, ResultTab, Retry, Next, BackHistory, OpenMap, StartExam
    }

    [RequireComponent(typeof(Button))]
    public sealed class DispatchButton : MonoBehaviour
    {
        public DispatchView receiver;
        public DispatchAction action;
        public int argument;
        private Button button;
        private void Awake() { button=GetComponent<Button>();button.onClick.AddListener(Click); }
        private void Click() { EmergencyAudioManager.Instance?.PlayButtonClick();receiver.Execute(action,argument); }
        private void OnDestroy() { if(button!=null)button.onClick.RemoveListener(Click); }
    }
}
