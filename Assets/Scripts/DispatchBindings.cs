using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public partial class DispatchView
    {
        [Header("Saved panels and reusable content")]
        [SerializeField] private RectTransform[] lobbyPages, protocolPages;
        [SerializeField] private GameObject historyEmpty;
        [SerializeField] private HistoryRowView historyRowPrefab;
        [SerializeField] private FleetRowView[] fleetRows;
        [SerializeField] private RectTransform confirmationRoot, confirmationPanel;
        [SerializeField] private Text confirmationTitle, confirmationCopy;
        [SerializeField] private Button confirmationCancel, confirmationAccept;
        [Header("Settings controls")]
        [SerializeField] private InputField settingsName;
        [SerializeField] private Slider settingsVolume;
        [SerializeField] private Text volumeValue, settingsSaved;
        [SerializeField] private Button liveSetting, localSetting;
        private readonly List<HistoryRowView> historyRows = new List<HistoryRowView>();

        public void Execute(DispatchAction action, int argument)
        {
            switch(action)
            {
                case DispatchAction.StartGuard: RequestLobbyStart(argument); break;
                case DispatchAction.StartExam: RequestExamStart(); break;
                case DispatchAction.ContinueGuard: game.ContinueGuard(); break;
                case DispatchAction.LobbyPage: ShowLobbyPage(argument); break;
                case DispatchAction.Exit: ShowLobbyExit(); break;
                case DispatchAction.OpenMenu: ShowLobby(); break;
                case DispatchAction.CheckConnection: StartCoroutine(CheckLobbyConnection()); break;
                case DispatchAction.Mode: game.ToggleMode(); break;
                case DispatchAction.LiveMode: if(!game.LiveMode)game.ToggleMode(); RefreshSettings(); break;
                case DispatchAction.LocalMode: if(game.LiveMode)game.ToggleMode(); RefreshSettings(); break;
                case DispatchAction.SaveSettings: SaveSettings(); break;
                case DispatchAction.Camera: DeskCameraController.Instance.SetView((DeskCameraController.CameraViewMode)argument); break;
                case DispatchAction.Type: showText=!showText; RefreshInput(); if(showText)input.ActivateInputField(); break;
                case DispatchAction.Send: SendInput(); break;
                case DispatchAction.Offline: game.ContinueOffline(); break;
                case DispatchAction.CenterTab: SetCenterTab(argument); break;
                case DispatchAction.Protocol: protocolCategory=argument; RefreshProtocolsTab(); break;
                case DispatchAction.Filter: game.Fleet.SetFilter(argument==0?(UnitType?)null:(UnitType)(argument-1)); break;
                case DispatchAction.ZoomIn: map.Zoom(.25f); break;
                case DispatchAction.ZoomOut: map.Zoom(-.25f); break;
                case DispatchAction.CenterMap: map.Center(); break;
                case DispatchAction.Answer: if(game.Phase==CallPhase.Incoming)game.Answer();else game.OpenIncomingMap(); break;
                case DispatchAction.OpenMap: game.OpenIncomingMap(); break;
                case DispatchAction.Finish: game.RequestFinish(); break;
                case DispatchAction.ResultTab: SetResultTab(showingGuard,argument); break;
                case DispatchAction.Retry: if(showingGuard)SetResultTab(true,1);else {resultTab=0;game.RetryCase();} break;
                case DispatchAction.Next: resultTab=0;if(showingGuard)game.StartShift();else game.NextCase();break;
                case DispatchAction.BackHistory: var back=currentViewingBackAction;currentViewingBackAction=null;resultTab=0;CloseResult();back?.Invoke();break;
            }
        }
    }
}
