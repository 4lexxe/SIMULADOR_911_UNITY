using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EmergencyDispatch
{
    public partial class DispatchView : MonoBehaviour
    {
        // Authentic CAD (Computer Aided Dispatch) 911 Terminal Palette matching Reference
        public static readonly Color Navy = new Color(.022f, .042f, .072f);
        public static readonly Color PanelColor = new Color(.038f, .072f, .118f, .94f);
        public static readonly Color PanelHeader = new Color(.055f, .105f, .175f);
        public static readonly Color BorderColor = new Color(.16f, .42f, .72f, .65f);
        public static readonly Color CardGlassBg = new Color(.025f, .052f, .095f, .88f);
        public static readonly Color Muted = new Color(.48f, .64f, .78f);
        public static readonly Color Blue = new Color(.20f, .60f, .96f);
        public static readonly Color CyanAccent = new Color(.28f, .82f, 1.0f);
        public static readonly Color Green = new Color(.10f, .86f, .52f);
        public static readonly Color Amber = new Color(1.0f, .72f, .18f);
        public static readonly Color Red = new Color(.92f, .22f, .26f);

        private GameManager game;

        [SerializeField] private Font font;
        [SerializeField] private RectTransform overlay, center, fleetList;
        private RectTransform modal, resultPanel;
        [SerializeField] private GameObject idleBody, activeBody, historyBody, protocolsBody;
        [SerializeField] private Button tabCallBtn, tabHistoryBtn, tabProtocolsBtn;
        [SerializeField] private Image tabCallImg, tabHistoryImg, tabProtocolsImg;
        [SerializeField] private Text tabCallText, historyKpiText;
        private int centerTab = 0; // 0 = Llamada actual, 1 = Historial, 2 = Protocolos
        private int protocolCategory = 0; // 0 = SAME, 1 = Bomberos, 2 = Policia, 3 = General
        [SerializeField] private RectTransform historyScrollContent, protocolScrollContent;
        [SerializeField] private Button[] protocolCategoryBtns = new Button[4];
        [SerializeField] private Image[] protocolCategoryImgs = new Image[4];
        private CallResult currentViewingResult;
        private Action currentViewingBackAction;
        private int currentViewingCaseNum = -1;
        [SerializeField] private Text status, timer, caller, dialogue, operatorLine, micStatus, notice, factCount, idleTitle, idleCopy;
        [SerializeField] private Text clockText, dateText;
        [SerializeField] private Text operatorStatusText, voiceStatusText;
        [SerializeField] private Text[] factValues;
        [SerializeField] private Button mainButton, modeButton, endButton, textToggle, sendButton, offlineButton;
        [SerializeField] private Text mainButtonLabel;
        [SerializeField] private InputField input;
        [SerializeField] private IncidentMapManager map;
        [SerializeField] private Image callLed;
        [SerializeField] private WaveformVisualizer wave;
        [SerializeField] private RectTransform promptDock;

        [SerializeField] private List<Button> filterButtons = new List<Button>();
        private bool showText = false;
        private int resultTab = 0;
        private CallPhase previousPhase = (CallPhase)(-1);
        private float nextFleetTick;
        public IncomingCallBoard IncomingBoard;
        public bool MapBoardVisible => IncomingBoard != null && IncomingBoard.Visible;
        public bool ResultVisible => resultPanel != null;

        public static DispatchView Instance { get; private set; }

        public static bool SafeIsTyping()
        {
            if (Instance != null && Instance.input != null && Instance.input.isFocused) return true;
            try
            {
                var es = EventSystem.current;
                if (es != null)
                {
                    var go = es.currentSelectedGameObject;
                    if (go != null)
                    {
                        var inf = go.GetComponent<InputField>();
                        if (inf != null && inf.isFocused) return true;
                    }
                }
            }
            catch {}
            return false;
        }

        public void Bind(GameManager manager)
        {
            if (game != null) return;
            Instance = this; game = manager; IncomingBoard.Bind(manager); map.Bind(manager);
            input.onSubmit.AddListener(OnTextSubmit);
            settingsName.onValueChanged.AddListener(OnNameChanged);
            settingsVolume.onValueChanged.AddListener(OnVolumeChanged);
            settingsVolume.SetValueWithoutNotify(AudioListener.volume);
            CloseResult(); CloseModal();
            foreach (var row in fleetRows) row.Bind(manager);
        }
        public void ResetGuardView()
        {
            IncomingBoard.Hide(); CloseResult(); CloseModal();
            currentViewingResult=null;currentViewingBackAction=null;currentViewingCaseNum=-1;
            centerTab=0;resultTab=0;animatedResult=null;animatedScore=-1;
        }
        private void OnTextSubmit(string _) => SendInput();
        private void OnNameChanged(string value) => operatorDraft = value;
        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value; volumeValue.text = Mathf.RoundToInt(value * 100) + "%";
        }

        private static Color CyanOrBlue(bool cyan) => cyan ? CyanAccent : Blue;

        public void SetCenterTab(int tab)
        {
            centerTab = tab;
            UpdateCenterTabs();
        }

        private void UpdateCenterTabs()
        {
            Color activeColor = new Color(.10f, .34f, .56f);
            Color idleColor = PanelHeader;

            if (tabCallImg != null) tabCallImg.color = centerTab == 0 ? activeColor : idleColor;
            if (tabHistoryImg != null) tabHistoryImg.color = centerTab == 1 ? activeColor : idleColor;
            if (tabProtocolsImg != null) tabProtocolsImg.color = centerTab == 2 ? activeColor : idleColor;

            bool isHist = centerTab == 1;
            bool isProt = centerTab == 2;

            if (historyBody != null) historyBody.SetActive(isHist);
            if (protocolsBody != null) protocolsBody.SetActive(isProt);

            if (isHist)
            {
                if (idleBody != null) idleBody.SetActive(false);
                if (activeBody != null) activeBody.SetActive(false);
                RefreshHistoryTab();
            }
            else if (isProt)
            {
                if (idleBody != null) idleBody.SetActive(false);
                if (activeBody != null) activeBody.SetActive(false);
                RefreshProtocolsTab();
            }
            else
            {
                CallPhase phase = game != null ? game.Phase : CallPhase.Idle;
                bool active = phase == CallPhase.Active || phase == CallPhase.Decision;
                if (idleBody != null) idleBody.SetActive(!active && phase != CallPhase.Result && phase != CallPhase.ShiftComplete);
                if (activeBody != null) activeBody.SetActive(active);
            }
        }

        private void RefreshHistoryTab()
        {
            if (game==null) return;
            // Only history entries vary in number. Each entry is a complete prefab.
            foreach (var row in historyRows) if (row!=null) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
            historyRows.Clear();
            int count=game.Results.Count;
            historyEmpty.SetActive(count==0);
            if (count==0) { historyKpiText.text="SIN REGISTROS · PUESTO EN GUARDIA ACTIVA"; historyScrollContent.sizeDelta=new Vector2(952,450); return; }
            int total=0; float seconds=0;
            foreach(var result in game.Results) { total+=result.Total; seconds+=result.seconds; }
            historyKpiText.text="INCIDENTES: "+count+"   |   PROMEDIO: "+Mathf.RoundToInt((float)total/count)+"/100   |   TIEMPO EN LÍNEA: "+(int)(seconds/60)+"m "+(int)(seconds%60)+"s";
            var cases=ScenarioDatabase.GetDefaultCases();
            for(int i=0;i<count;i++)
            {
                var row=Instantiate(historyRowPrefab,historyScrollContent);
                row.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-i*100);
                int caseIndex=cases.FindIndex(c=>c.id==game.Results[i].caseId);
                row.Bind(game.Results[i],caseIndex+1,caseIndex>=0?cases[caseIndex].title:"Incidente reportado",ShowArchivedResult);
                historyRows.Add(row);
            }
            historyScrollContent.sizeDelta=new Vector2(952,count*100+20);
        }

        private void ShowArchivedResult(CallResult r, int caseIndex)
        {
            if (resultPanel != null) { CloseResult(); }
            BuildResult(false, r, () =>
            {
                if (resultPanel != null) { CloseResult(); }
                SetCenterTab(1);
            }, caseIndex);
        }

        private void RefreshProtocolsTab()
        {
            for (int i=0;i<protocolPages.Length;i++)
            {
                protocolPages[i].gameObject.SetActive(i==protocolCategory);
                protocolCategoryImgs[i].color=i==protocolCategory ? new Color(.10f,.34f,.56f) : PanelHeader;
            }
            protocolScrollContent.GetComponentInParent<ScrollRect>().content=protocolPages[protocolCategory];
        }

        private void SendInput()
        {
            string text = input.text; game.SubmitText(text);
            if (!game.Busy || game.LiveMode) input.text = "";
            input.DeactivateInputField(); EventSystem.current?.SetSelectedGameObject(null);
        }

        private void RefreshInput()
        {
            if (promptDock != null) promptDock.gameObject.SetActive(showText && game.Phase == CallPhase.Active);
            bool canType = game.Phase == CallPhase.Active && !game.Busy && !game.Voice.Recording && !ResultVisible;
            input.interactable = sendButton.interactable = canType;
            if (showText && canType) input.ActivateInputField();
        }

        public void Tick()
        {
            if (lobbyMenuButton != null) lobbyMenuButton.interactable = game.CanOpenMenu;
            if (!LobbyVisible && (!ResultVisible || currentViewingBackAction == null) && !SafeIsTyping() && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (modal != null) { if (game.Phase == CallPhase.Decision) game.CancelDispatch(); else CloseModal(); }
                else if(MapBoardVisible) IncomingBoard.Hide();
                else ShowLobby();
            }
            if (LobbyVisible) return;
            if(!ResultVisible && modal==null && !SafeIsTyping() && Keyboard.current?.mKey.wasPressedThisFrame==true)
            { if(MapBoardVisible)IncomingBoard.Hide();else game.OpenIncomingMap(); }
            if (game.Session != null)
            {
                int seconds = (int)game.Session.Seconds;
                timer.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            }
            if (game.Phase == CallPhase.Incoming) callLed.enabled = Mathf.Sin(Time.unscaledTime * 5) > -.2f; else callLed.enabled = true;
            micStatus.text = game.Voice.Recording ? "HABLANDO · Soltá ESPACIO para escuchar" : game.Busy ? "ESCUCHANDO · Esperá tu turno" : game.LiveMode ? "ESPACIO · Mantené para hablar" : "ENSAYO · Escribí tu intervención";
            
            // Dynamic Clock and Date updates in real time
            if (clockText != null) clockText.text = DateTime.Now.ToString("HH:mm");
            if (dateText != null) dateText.text = DateTime.Now.ToString("ddd, dd MMM", new System.Globalization.CultureInfo("es-ES")).ToUpperInvariant();

            // Status updates for HUD top-right
            if (operatorStatusText != null)
            {
                operatorStatusText.text = (game.Phase == CallPhase.Active || game.Phase == CallPhase.Decision) ?
                    "EN ATENCIÓN · CASO #" + game.CaseNumber.ToString("00") :
                    game.Phase == CallPhase.ShiftComplete ? "GUARDIA FINALIZADA" : "GUARDIA DISPONIBLE";
            }
            if (voiceStatusText != null)
            {
                voiceStatusText.text = game.LiveMode ? (game.ConnectionStatus.Contains("CONECTADO") ? "● CONECTADA" : "● " + game.ConnectionStatus) : "● ENSAYO LOCAL";
                voiceStatusText.color = game.LiveMode ? (game.ConnectionStatus.Contains("CONECTADO") ? Green : Amber) : Muted;
            }

            if (wave != null) wave.SetSpeaking(game.Voice.Playing || game.Voice.Recording);
            modeButton.interactable=game.Phase==CallPhase.Idle||game.Phase==CallPhase.Incoming||game.Phase==CallPhase.ShiftComplete;
            if (Time.unscaledTime > nextFleetTick) { RefreshFleet(); nextFleetTick = Time.unscaledTime + .5f; }

            if (resultPanel != null)
            {
                var kb = Keyboard.current;
                if (kb != null && !SafeIsTyping())
                {
                    if (kb.f1Key.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame)
                    {
                        if (resultTab != 0)
                        {
                            resultTab = 0;
                            CloseResult();
                            BuildResult(game.Phase == CallPhase.ShiftComplete, currentViewingResult, currentViewingBackAction, currentViewingCaseNum);
                        }
                    }
                    else if (kb.f2Key.wasPressedThisFrame || kb.digit2Key.wasPressedThisFrame)
                    {
                        if (resultTab != 1)
                        {
                            resultTab = 1;
                            CloseResult();
                            BuildResult(game.Phase == CallPhase.ShiftComplete, currentViewingResult, currentViewingBackAction, currentViewingCaseNum);
                        }
                    }
                    else if (kb.tabKey.wasPressedThisFrame)
                    {
                        resultTab = (resultTab == 0 ? 1 : 0);
                        CloseResult();
                        BuildResult(game.Phase == CallPhase.ShiftComplete, currentViewingResult, currentViewingBackAction, currentViewingCaseNum);
                    }
                    else if (kb.escapeKey.wasPressedThisFrame && currentViewingBackAction != null)
                    {
                        resultTab = 0;
                        CloseResult();
                        currentViewingBackAction.Invoke();
                    }
                }
            }
        }

        public void RefreshFleet()
        {
            if (game?.Fleet == null) return;
            for (int i=0;i<filterButtons.Count;i++)
                filterButtons[i].GetComponent<Image>().color = (i==0 ? !game.Fleet.Filter.HasValue : game.Fleet.Filter==(UnitType)(i-1)) ? new Color(.12f,.36f,.56f) : PanelHeader;
            int index=0;
            foreach (var row in fleetRows)
            {
                bool visible=!game.Fleet.Filter.HasValue || row.definition.initial.type==game.Fleet.Filter.Value;
                row.gameObject.SetActive(visible);
                if (visible) { row.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-index++*76); row.Refresh(); }
            }
        }

        private RectTransform OpenModal(string title, string copy)
        {
            CloseModal(); modal=confirmationRoot; modal.gameObject.SetActive(true); modal.SetAsLastSibling();
            confirmationTitle.text=title; confirmationCopy.text=copy;
            return confirmationPanel;
        }
        private void SetConfirmation(string cancel, Action cancelAction, string confirm, Action confirmAction, Color tone)
        {
            confirmationCancel.name=cancel; confirmationCancel.GetComponentInChildren<Text>().text=cancel;
            confirmationAccept.name=confirm; confirmationAccept.GetComponentInChildren<Text>().text=confirm;
            confirmationAccept.GetComponent<Image>().color=tone;
            confirmationCancel.onClick.RemoveAllListeners(); confirmationAccept.onClick.RemoveAllListeners();
            confirmationCancel.onClick.AddListener(()=>{ EmergencyAudioManager.Instance?.PlayButtonClick(); cancelAction(); });
            confirmationAccept.onClick.AddListener(()=>{ EmergencyAudioManager.Instance?.PlayButtonClick(); confirmAction(); });
            WireMenuNavigation(new Selectable[]{confirmationCancel,confirmationAccept},confirmationCancel);
        }

        public void ShowDispatchConfirmation(EmergencyUnit unit)
        {
            var panel = OpenModal("ORDEN DE DESPACHO CAD · RECURSO #" + unit.id, "UNIDAD: " + unit.displayName + " · BASE: " + unit.baseLocation + "\nDESTINO: " + game.Session.Facts["location"].value + "\nTIEMPO ESTIMADO DE ARRIBO (ETA): " + unit.baseEtaMinutes + " min.");
            SetConfirmation("CANCELAR",()=>game.CancelDispatch(),"CONFIRMAR DESPACHO",()=>game.ConfirmDispatch(),new Color(.06f,.45f,.30f));
        }

        public void ShowEndConfirmation(bool noResource)
        {
            var panel = OpenModal("FINALIZAR LLAMADA Y AUDITAR CASO", noResource ? "ADVERTENCIA CAD: No se asignó ninguna unidad de auxilio para este incidente.\nSi finalizás ahora, el caso quedará registrado sin despacho." : "¿Confirmaste los datos del incidente y comunicaste el cierre al denunciante?\nAl cerrar se generará la auditoría de supervisión de la llamada.");
            SetConfirmation("CONTINUAR LLAMADA",CloseModal,"FINALIZAR Y EVALUAR",()=>game.FinishCall(),new Color(.55f,.12f,.16f));
        }

        public void CloseModal() { if (confirmationRoot != null) confirmationRoot.gameObject.SetActive(false); modal = null; }

        private System.Collections.IEnumerator AnimateEntrance(CanvasGroup cg, RectTransform panel, int targetScore, Image ringFill, Text scoreLbl, RectTransform[] barFills, float[] targetWidths)
        {
            float duration = 1.65f;
            float elapsed = 0f;
            float nextTick = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Panel alpha and subtle scale bounce
                float panelT = Mathf.Clamp01(elapsed / 0.3f);
                if (cg != null) cg.alpha = panelT;
                if (panel != null) panel.localScale = Vector3.Lerp(new Vector3(0.96f, 0.96f, 1f), Vector3.one, 1f - Mathf.Pow(1f - panelT, 2f));

                // EaseOutCubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                int currentScore = Mathf.RoundToInt(targetScore * ease);
                if (scoreLbl != null) scoreLbl.text = currentScore.ToString();
                if (ringFill != null) ringFill.fillAmount = (targetScore / 100f) * ease;

                if (elapsed >= nextTick && elapsed < duration * .82f && targetScore > 0)
                {
                    nextTick = elapsed + .075f;
                    EmergencyAudioManager.Instance?.PlayScoreTick();
                }

                // Staggered bars
                for (int i = 0; i < barFills.Length; i++)
                {
                    if (barFills[i] != null)
                    {
                        float barDelay = i * 0.12f;
                        float barT = Mathf.Clamp01((elapsed - barDelay) / (duration - 0.55f));
                        float barEase = 1f - Mathf.Pow(1f - barT, 3f);
                        barFills[i].sizeDelta = new Vector2(targetWidths[i] * barEase, barFills[i].sizeDelta.y);
                    }
                }

                resultGrade.color = new Color(resultGrade.color.r,resultGrade.color.g,resultGrade.color.b,Mathf.Clamp01((elapsed-.9f)/.4f));
                yield return null;
            }

            if (cg != null) cg.alpha = 1f;
            if (panel != null) panel.localScale = Vector3.one;
            if (scoreLbl != null) scoreLbl.text = targetScore.ToString();
            if (ringFill != null) ringFill.fillAmount = targetScore / 100f;
            for (int i = 0; i < barFills.Length; i++)
            {
                if (barFills[i] != null) barFills[i].sizeDelta = new Vector2(targetWidths[i], barFills[i].sizeDelta.y);
            }
            EmergencyAudioManager.Instance?.PlayScoreReveal(targetScore);
            // A short score impact after the exact final value settles.
            float impact=0;
            while(impact<.32f)
            {
                impact+=Time.unscaledDeltaTime;float pulse=Mathf.Sin(Mathf.Clamp01(impact/.32f)*Mathf.PI)*.1f;
                scoreLbl.rectTransform.localScale=Vector3.one*(1+pulse);yield return null;
            }
            scoreLbl.rectTransform.localScale=Vector3.one;
        }

        public void Refresh()
        {
            if (game.View == null) return;
            IncomingBoard.Refresh();
            if (LobbyVisible) { RefreshLobby(); return; }
            CallPhase phase = game.Phase;
            bool active = phase == CallPhase.Active || phase == CallPhase.Decision;

            if (phase == CallPhase.Incoming && previousPhase != CallPhase.Incoming)
            {
                centerTab = 0;
            }
            else if (active && (previousPhase == CallPhase.Incoming || previousPhase == CallPhase.Idle))
            {
                centerTab = 0;
            }

            UpdateCenterTabs();

            if (tabCallText != null)
            {
                tabCallText.text = (phase == CallPhase.Incoming && centerTab != 0) ? "  LLAMADA ENTRANTE ⚠" : "  LLAMADA ACTUAL";
            }

            status.text = phase == CallPhase.Incoming ? "LLAMADA ENTRANTE" : active ? "LLAMADA EN CURSO" : phase == CallPhase.Result ? "CASO FINALIZADO · RESULTADO" : phase == CallPhase.ShiftComplete ? "TURNO COMPLETADO · INFORME DE GUARDIA" : "CENTRAL DE EMERGENCIAS";
            callLed.color = active || phase == CallPhase.Incoming ? Red : Green;
            idleTitle.text = phase == CallPhase.Incoming ? game.Session.Definition.locality : game.HasCurrentGuard ? "JUJUY · LLAMADAS EN ESPERA" : "Tu guardia empieza acá.";
            idleCopy.text = phase == CallPhase.Incoming ? game.Session.Definition.incomingSummary + "\nLa señal indica la localidad. Confirmá la dirección al atender." : "Abrí el mapa, elegí un punto rojo y decidí qué llamada atender.";
            mainButtonLabel.text = phase == CallPhase.Incoming ? "  CONTESTAR LLAMADA ENTRANTE" : game.HasCurrentGuard ? "  ELEGIR LLAMADA EN EL MAPA" : "  INICIAR TURNO DE GUARDIA";

            if (active)
            {
                caller.text = "Denunciante en escena (" + game.Session.Definition.callerName + ")";
                dialogue.text = string.IsNullOrEmpty(game.CallerText) ? "..." : "\"" + game.CallerText + "\"";
                operatorLine.text = string.IsNullOrEmpty(game.OperatorText) ? "> Canal abierto para transmisión." : "> " + DispatchPreferences.OperatorName + ": \"" + game.OperatorText + "\"";
                factCount.text = "CONFIRMADOS: " + game.Session.Facts.Count + " / 5";
                for (int i = 0; i < 5; i++)
                {
                    var fact = game.Session.Definition.facts[i];
                    bool known = game.Session.Facts.TryGetValue(fact.id, out var record);
                    factValues[i].text = known ? (record.verified ? "[✓]  " : "[?]  ") + record.value : "[—]  Pendiente de verificación";
                    factValues[i].color = !known ? Muted : record.verified ? Green : Amber;
                }
            }

            endButton.interactable = active && !game.Busy && !game.Voice.Recording;
            map.SetCase(game.Session);
            RefreshFleet(); RefreshInput();

            if (phase == CallPhase.Result || phase == CallPhase.ShiftComplete)
            {
                if (resultPanel != null) { resultPanel.gameObject.SetActive(false); CloseResult(); }
                if (phase == CallPhase.Result) BuildResult(false);
                if (phase == CallPhase.ShiftComplete) BuildResult(true);
                previousPhase = phase;
            }
            else if (phase != previousPhase)
            {
                if (resultPanel != null && currentViewingBackAction == null) { resultPanel.gameObject.SetActive(false); CloseResult(); }
                previousPhase = phase;
            }

            offlineButton.gameObject.SetActive(active && game.LiveMode && !game.ConnectionStatus.Contains("CONECTADO") && !game.Busy);
            Tick();
        }

        private void OnDestroy()
        {
            if(input!=null) input.onSubmit.RemoveListener(OnTextSubmit);
            if(settingsName!=null) settingsName.onValueChanged.RemoveListener(OnNameChanged);
            if(settingsVolume!=null) settingsVolume.onValueChanged.RemoveListener(OnVolumeChanged);
            if (Instance == this) Instance = null;
        }
    }
}

