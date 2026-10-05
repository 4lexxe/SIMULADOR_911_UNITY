using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace EmergencyDispatch
{
    public partial class DispatchView
    {
        [SerializeField] private RectTransform lobby, lobbyContent, lobbyPage;
        [SerializeField] private Text lobbyOperator, lobbyConnection, lobbyState, lobbyContinueHint, operatorIdHud, continueCaption;
        private string operatorDraft;
        [SerializeField] private Button lobbyContinue, lobbyMenuButton;
        [SerializeField] private List<Button> lobbyEntries = new List<Button>();
        private int lobbyPageNumber;
        private bool checkingRelay, relayAvailable, relayConfigured;
        public bool LobbyVisible => lobby != null && lobby.gameObject.activeSelf;

        public void ShowLobby()
        {
            if(lobby==null||!game.CanOpenMenu)return;
            IncomingBoard.Hide();CloseModal();input.DeactivateInputField();operatorDraft=DispatchPreferences.OperatorName;game.RefreshSavedGuard();lobby.gameObject.SetActive(true);lobby.SetAsLastSibling();
            ShowLobbyPage(0);RefreshLobby();EventSystem.current?.SetSelectedGameObject(lobbyEntries[0].gameObject);
            StartCoroutine(CheckLobbyConnection());
        }
        public void HideLobby()
        {
            if(lobby==null)return;lobby.gameObject.SetActive(false);CloseModal();EventSystem.current?.SetSelectedGameObject(null);
        }
        private void RefreshLobby()
        {
            if(lobbyOperator==null)return;
            lobbyOperator.text=DispatchPreferences.OperatorName;lobbyContinue.interactable=game.CanContinueGuard;
            lobbyContinue.GetComponent<CanvasGroup>().alpha=game.CanContinueGuard?1:.4f;
            var enabledEntries=lobbyEntries.FindAll(b=>b.interactable);
            for(int i=0;i<enabledEntries.Count;i++)
            {
                var nav=enabledEntries[i].navigation;nav.mode=Navigation.Mode.Explicit;
                nav.selectOnUp=enabledEntries[(i+enabledEntries.Count-1)%enabledEntries.Count];nav.selectOnDown=enabledEntries[(i+1)%enabledEntries.Count];enabledEntries[i].navigation=nav;
            }
            continueCaption.text=game.HasCurrentGuard?"RETOMAR GUARDIA EN JUJUY":game.CanContinueGuard?"RETOMAR GUARDIA GUARDADA":"SIN GUARDIA GUARDADA";
            if(operatorIdHud!=null)operatorIdHud.text="CENTRAL 911  /  "+DispatchPreferences.OperatorName;
            lobbyState.text=game.HasCurrentGuard?"GUARDIA EN CURSO":"LISTO PARA COMENZAR";
            lobbyConnection.text=!game.LiveMode?"ENSAYO LOCAL · SIN CONEXIÓN":checkingRelay?"COMPROBANDO SERVICIO…":relayAvailable&&relayConfigured?"GEMINI LIVE · LISTO AL ATENDER":relayAvailable?"GEMINI LIVE · FALTA CONFIGURAR":Application.isBatchMode?"CONEXIÓN SIN COMPROBAR":"SERVICIO DE VOZ NO DISPONIBLE";
            lobbyConnection.color=!game.LiveMode||relayAvailable&&relayConfigured?Color.white:Amber;
            lobbyContinueHint.text=game.CanContinueGuard?"CONTINUAR · Conservá tus casos completados y elegí la próxima llamada.":"JUJUY · Ocho llamadas. Vos decidís a quién atender.";
        }
        [Serializable] private class RelayHealth { public string status;public bool configured; }
        private IEnumerator CheckLobbyConnection()
        {
            if(checkingRelay||Application.isBatchMode)yield break;
            checkingRelay=true;RefreshLobby();
            string endpoint=GeminiConfig.BackendUrl.Replace("ws://","http://").Replace("wss://","https://").TrimEnd('/')+"/health";
            using(var request=UnityWebRequest.Get(endpoint))
            {
                request.timeout=3;yield return request.SendWebRequest();relayAvailable=false;relayConfigured=false;
                if(request.result==UnityWebRequest.Result.Success)
                {
                    try { var health=JsonUtility.FromJson<RelayHealth>(request.downloadHandler.text);relayAvailable=health?.status=="ok";relayConfigured=health?.configured==true; }catch(ArgumentException) { }
                }
            }
            checkingRelay=false;RefreshLobby();
        }
        private void RequestLobbyStart(int caseIndex)
        {
            if(!game.CanContinueGuard) { game.StartAtCase(caseIndex); return; }
            OpenModal("INICIAR UNA NUEVA GUARDIA","Se reemplazará el progreso de la guardia actual.\n"+(caseIndex<0?"Elegí la próxima llamada en el mapa de Jujuy.":"La nueva guardia seleccionará el caso "+(caseIndex+1).ToString("00")+"."));
            SetConfirmation("VOLVER",CloseModal,"INICIAR GUARDIA",()=>game.StartAtCase(caseIndex),new Color(.025f,.28f,.15f));
        }
        private void RequestExamStart()
        {
            OpenModal("EXAMEN PRÁCTICO · 180 SEGUNDOS", "Una caída en el club · San Salvador de Jujuy.\nEl reloj comienza al iniciar e incluye mapa, conexión y menú.\nObtené cinco datos, despachá y comunicá el cierre.\nSe reinicia la sesión actual; la guardia guardada en disco se conserva.");
            SetConfirmation("VOLVER", CloseModal, "INICIAR EXAMEN", game.StartExam, new Color(.025f,.28f,.15f));
        }
        private void ShowLobbyExit()
        {
            OpenModal("SALIR DE ÚLTIMA LLAMADA","Los casos completados quedan guardados.\nSi hay una llamada en curso, se reinicia al continuar.");
            SetConfirmation("VOLVER",CloseModal,"SALIR DEL JUEGO",()=> {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            },new Color(.40f,.06f,.09f));
        }
        private void ShowLobbyPage(int page)
        {
            int previousPage=lobbyPageNumber; lobbyPageNumber=page;
            foreach(var item in lobbyPages) item.gameObject.SetActive(false);
            lobbyPage=page==0?null:lobbyPages[page-1];
            if(page==0) { if(previousPage>0) EventSystem.current?.SetSelectedGameObject(lobbyEntries[previousPage+1].gameObject); return; }
            lobbyPage.gameObject.SetActive(true);
            if(page==2) RefreshSettings();
            var controls=lobbyPage.GetComponentsInChildren<Selectable>();
            WireMenuNavigation(controls,lobbyEntries[page+1]);
            EventSystem.current?.SetSelectedGameObject(controls.Length>1?controls[1].gameObject:controls[0].gameObject);
        }
        private void RefreshSettings()
        {
            settingsName.SetTextWithoutNotify(operatorDraft??DispatchPreferences.OperatorName);
            settingsVolume.SetValueWithoutNotify(AudioListener.volume); OnVolumeChanged(AudioListener.volume);
            bool canChange=game.Phase==CallPhase.Idle||game.Phase==CallPhase.Incoming||game.Phase==CallPhase.ShiftComplete;
            liveSetting.interactable=localSetting.interactable=canChange;
            liveSetting.GetComponent<Image>().color=game.LiveMode?new Color(.025f,.23f,.32f):PanelHeader;
            localSetting.GetComponent<Image>().color=game.LiveMode?PanelHeader:new Color(.025f,.23f,.32f);
        }
        private void SaveSettings()
        {
            DispatchPreferences.Save(settingsName.text,AudioListener.volume,game.LiveMode); RefreshLobby();
            settingsSaved.text="Configuración guardada."; settingsSaved.color=Green;
        }
        private void WireMenuNavigation(Selectable[] controls,Selectable back)
        {
            var enabled=new List<Selectable>();foreach(var control in controls)if(control.interactable)enabled.Add(control);
            for(int i=0;i<enabled.Count;i++)
            {
                var nav=enabled[i].navigation;nav.mode=Navigation.Mode.Explicit;
                nav.selectOnUp=enabled[(i+enabled.Count-1)%enabled.Count];nav.selectOnDown=enabled[(i+1)%enabled.Count];
                nav.selectOnLeft=back;nav.selectOnRight=enabled[(i+1)%enabled.Count];enabled[i].navigation=nav;
            }
            if(enabled.Count>0)EventSystem.current?.SetSelectedGameObject(enabled[0].gameObject);
        }

        public void TickLobby()
        {
            if(!LobbyVisible||SafeIsTyping())return;
            if(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject==null)EventSystem.current.SetSelectedGameObject(lobbyEntries[0].gameObject);
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)
            {
                if(modal!=null)CloseModal();else if(lobbyPageNumber!=0)ShowLobbyPage(0);else if(game.HasCurrentGuard)game.ContinueGuard();
            }
        }
    }
}
