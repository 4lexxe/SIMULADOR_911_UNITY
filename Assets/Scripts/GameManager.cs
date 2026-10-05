using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EmergencyDispatch
{
    public partial class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public CallSession Session { get; private set; }
        public CallPhase Phase => shiftComplete ? CallPhase.ShiftComplete : Session?.Phase ?? CallPhase.Idle;
        public bool LiveMode { get; private set; } = true;
        public bool Busy { get; private set; }
        public int CaseNumber => index + 1;
        public int CaseCount => cases.Count;
        public string ConnectionStatus { get; private set; } = "VOZ · GEMINI LIVE";
        public string CallerText { get; private set; } = "Tu turno comienza acá.";
        public string OperatorText { get; private set; } = "";
        public string Notice { get; private set; } = "Escuchá. Preguntá. Confirmá.";
        public readonly List<CallResult> Results = new List<CallResult>();
        public UnitFleetManager Fleet { get; private set; }
        public VoiceManager Voice { get; private set; }
        public DispatchView View { get; private set; }
        private GeminiEmergencyService relay;
        private List<EmergencyCase> cases;
        private int index = -1;
        private bool shiftComplete, callerTurnStarted, operatorTurnRecorded;
        private float waitUntil, nextRing, busySince;
        private string inputTranscript = "";
        [Header("Scene systems — assign in the Inspector")]
        [SerializeField] private DispatchView sceneView;
        [SerializeField] private UnitFleetManager sceneFleet;
        [SerializeField] private GeminiEmergencyService sceneRelay;
        [SerializeField] private VoiceManager sceneVoice;
        public UnitFleetManager SceneFleet => sceneFleet;
        private void Awake() { Instance = this; }
        private void Start()
        {
            if(sceneView==null||sceneFleet==null||sceneRelay==null||sceneVoice==null)
            { Debug.LogError("Missing saved scene references on GameManager.",this);enabled=false;return; }
            Initialize(sceneView,sceneFleet,sceneRelay,sceneVoice);
        }
        private void Initialize(DispatchView view, UnitFleetManager fleet, GeminiEmergencyService service, VoiceManager voice)
        {
            View = view; Fleet = fleet; relay = service; Voice = voice;
            cases = ScenarioDatabase.GetDefaultCases();
            if (!Application.isBatchMode) LiveMode = PlayerPrefs.GetInt("ULTIMA_LIVE_MODE", 1) == 1;
            ConnectionStatus = LiveMode ? "VOZ · GEMINI LIVE" : "ENSAYO LOCAL · TEXTO";
            AudioListener.volume = DispatchPreferences.Volume;
            Fleet.ResetUnits();
            View.Bind(this);
            relay.Message += OnRelayMessage;
            Voice.Initialize(relay);
            Voice.CanTalk = () => LiveMode && Phase == CallPhase.Active && relay.Ready && !Busy && !View.LobbyVisible && !View.MapBoardVisible;
            Voice.BeginTurn = () =>
            {
                if (Voice.Playing) { Session.RegisterTag(Session.Turns, "interruption"); }
                OperatorText = ""; inputTranscript = ""; callerTurnStarted = false; operatorTurnRecorded = false;
                return Session.BeginTurn();
            };
            Voice.TurnEnded = turn => { if (Phase == CallPhase.Active) { Busy = true; busySince = Time.unscaledTime; } };
            Voice.Problem += SetNotice;
            View.Refresh();
            View.ShowLobby();
        }
        public void SetNotice(string text) { Notice = text; View?.Refresh(); }
        public void ToggleMode()
        {
            if (Phase != CallPhase.Idle && Phase != CallPhase.Incoming && Phase != CallPhase.ShiftComplete) return;
            LiveMode = !LiveMode;
            ConnectionStatus = LiveMode ? "VOZ · GEMINI LIVE" : "ENSAYO LOCAL · TEXTO";
            View.Refresh();
        }
        public void StartShift() => StartAtCase(-1);
        public void NextCase()
        {
            if (Phase == CallPhase.Active || Phase == CallPhase.Decision || Busy) return;
            if (ExamMode && ExamFinished) { View.ShowLobby(); return; }
            if (Queue == null) { StartShift(); return; }
            View.HideLobby(); relay.Disconnect(); Voice.ClearPlayback(); Busy = false;
            Session = null; index = -1;
            shiftComplete = RemainingCalls == 0;
            CallerText = "Elegí una llamada en el mapa de Jujuy.";
            OperatorText = ""; Notice = "Los puntos rojos son llamadas en espera.";
            waitUntil = Time.unscaledTime + .35f; nextRing = 0;
            SaveResults(); View.Refresh();
            if (!shiftComplete) View.IncomingBoard.Show();
        }
        public void RetryCase()
        {
            if (ExamMode) { StartExam(); return; }
            relay.Disconnect(); Voice.StopTalking(); Voice.ClearPlayback(); Busy = false;
            if (index >= 0 && index < cases.Count)
            {
                if (Session?.DispatchedUnit != null)
                {
                    var unit = Session.DispatchedUnit;
                    unit.status = UnitStatus.Available; unit.remainingEtaSeconds = 0; unit.assignedCaseId = "";
                }
                if (Results.Count > 0 && Results[Results.Count - 1].caseId == cases[index].id)
                    Results.RemoveAt(Results.Count - 1);
                Session = new CallSession(cases[index]);
                View.IncomingBoard.Hide();
                SaveResults();
                CallerText = "Una persona necesita ser escuchada.";
                OperatorText = ""; Notice = "Contestá cuando estés listo.";
                waitUntil = Time.unscaledTime + .35f; nextRing = 0;
                View.Refresh();
            }
            else
            {
                StartShift();
            }
        }
        public void Answer()
        {
            if (Session?.Answer() != true) return;
            View.IncomingBoard.Hide();
            DeskCameraController.Instance?.SetView(DeskCameraController.CameraViewMode.CenterMonitor);
            EmergencyAudioManager.Instance?.PlayCallSelect();
            callTranscript.Clear();
            inputTranscript = ""; callerTurnStarted = false; operatorTurnRecorded = false;
            if (!LiveMode) callTranscript.Add(new DialogueTurn { role = "caller", text = Session.Definition.opening });
            CallerText = Session.Definition.opening;
            Notice = LiveMode ? "Conectando la conversación de voz…" : "Ensayo de texto con respuestas limitadas. Escribí con tus palabras.";
            if (LiveMode) { Busy = true; busySince = Time.unscaledTime; relay.Connect(Session.Definition.id); ConnectionStatus = "CONECTANDO…"; }
            View.Refresh();
        }
        public void SubmitText(string text)
        {
            if (Phase != CallPhase.Active || Busy || Voice.Recording || string.IsNullOrWhiteSpace(text)) return;
            text = text.Trim();
            if (text.Length > 1200) { SetNotice("Acortá la intervención a 1200 caracteres."); return; }
            if (LiveMode && !relay.Ready) { SetNotice("La voz está desconectada. Usá «Continuar en ensayo» para practicar localmente."); return; }
            if (TryNavigationCommand(text)) return;
            int turn = Session.BeginTurn();
            OperatorText = text; callerTurnStarted = false; inputTranscript = ""; operatorTurnRecorded = true;
            callTranscript.Add(new DialogueTurn { role = "operator", text = text });
            if (LiveMode)
            {
                Busy = true; busySince = Time.unscaledTime;
                relay.Send(new RelayMessage { type = "text", text = text, turnId = turn });
            }
            else
            {
                CallerText = OfflineConversation.Respond(Session, text, turn);
                callTranscript.Add(new DialogueTurn { role = "caller", text = CallerText });
            }
            Notice = "Obtené los cinco datos y confirmá lo que sea dudoso.";
            View.Refresh();
        }
        public void ContinueOffline()
        {
            if (Phase != CallPhase.Active) return;
            Voice.StopTalking(); relay.Disconnect(); Voice.ClearPlayback();
            Busy = false; LiveMode = false; ConnectionStatus = "ENSAYO LOCAL · TEXTO";
            SetNotice("Continuás en ensayo local: las respuestas son limitadas y no hay audio.");
        }
        private void OnRelayMessage(RelayMessage msg)
        {
            if (Session == null || msg.caseId != Session.Definition.id || Phase != CallPhase.Active) return;
            switch (msg.type)
            {
                case "ready":
                    ConnectionStatus = "GEMINI LIVE · CONECTADO";
                    // The opening response still needs to finish before the player speaks.
                    Notice = "Escuchá la llamada. Luego mantené ESPACIO para hablar.";
                    break;
                case "caller":
                    if (!callerTurnStarted) { CallerText = ""; callerTurnStarted = true; }
                    CallerText += msg.text;
                    SyncFactsFromSpeech();
                    break;
                case "operator":
                    inputTranscript += msg.text; OperatorText = inputTranscript; break;
                case "audio": Voice.ReceiveAudio(msg.data, msg.sampleRate); return;
                case "interrupted": Voice.ClearPlayback(); break;
                case "fact":
                    if (Session.RegisterFact(msg.turnId, msg.eventId, msg.factId, msg.value, msg.verified))
                        EmergencyAudioManager.Instance?.PlayRadioBeep();
                    break;
                case "tag": Session.RegisterTag(msg.turnId, msg.tag); break;
                case "emotion": Session.AdjustEmotion(msg.turnId, msg.delta); break;
                case "turn_complete":
                    SyncFactsFromSpeech();
                    string completedInput = inputTranscript;
                    if (!operatorTurnRecorded && !string.IsNullOrEmpty(inputTranscript)) callTranscript.Add(new DialogueTurn { role = "operator", text = inputTranscript });
                    if (!string.IsNullOrEmpty(CallerText) && callerTurnStarted) callTranscript.Add(new DialogueTurn { role = "caller", text = CallerText });
                    Busy = false; inputTranscript = ""; callerTurnStarted = false;
                    Notice = "Mantené ESPACIO para hablar. Soltalo para escuchar.";
                    if (TryNavigationCommand(completedInput)) Session.ExcludeNavigationTurn(Session.Turns); break;
                case "error":
                    Voice.StopTalking(); Voice.ClearPlayback(); relay.Disconnect();
                    Busy = false; ConnectionStatus = "VOZ DESCONECTADA"; Notice = msg.text;
                    break;
            }
            View.Refresh();
        }
        private void SyncFactsFromSpeech()
        {
            if (Session == null || (Phase != CallPhase.Active && Phase != CallPhase.Decision)) return;
            string callerNorm = OfflineConversation.Normalize(CallerText);
            if (string.IsNullOrEmpty(callerNorm)) return;
            int turn = Math.Max(1, Session.Turns);

            foreach (var fact in Session.Definition.facts)
            {
                if (Session.Facts.ContainsKey(fact.id) && Session.Facts[fact.id].verified) continue;

                string factValNorm = OfflineConversation.Normalize(fact.value);
                string factRespNorm = OfflineConversation.Normalize(fact.response);
                string verValNorm = fact.uncertain ? OfflineConversation.Normalize(fact.verifiedValue) : "";
                string verRespNorm = fact.uncertain ? OfflineConversation.Normalize(fact.verificationResponse) : "";

                bool callerMatches = CallerSpokeFact(Session.Definition.id, fact.id, factValNorm, factRespNorm, verValNorm, verRespNorm, callerNorm);

                if (callerMatches)
                {
                    bool isVerified = !fact.uncertain || (!string.IsNullOrEmpty(verValNorm) && callerNorm.Contains(verValNorm)) || (!string.IsNullOrEmpty(verRespNorm) && callerNorm.Contains(verRespNorm));
                    string val = isVerified && fact.uncertain ? fact.verifiedValue : fact.value;
                    if (Session.RegisterFact(turn, "sync-" + turn + "-" + fact.id, fact.id, val, isVerified))
                    {
                        EmergencyAudioManager.Instance?.PlayRadioBeep();
                    }
                }
            }
        }
        private static bool CallerSpokeFact(string caseId, string factId, string valNorm, string respNorm, string verValNorm, string verRespNorm, string callerNorm)
        {
            if (!string.IsNullOrEmpty(valNorm) && callerNorm.Contains(valNorm)) return true;
            if (!string.IsNullOrEmpty(verValNorm) && callerNorm.Contains(verValNorm)) return true;
            if (!string.IsNullOrEmpty(respNorm) && callerNorm.Contains(respNorm)) return true;
            if (!string.IsNullOrEmpty(verRespNorm) && callerNorm.Contains(verRespNorm)) return true;

            if (caseId == "CASE_001")
            {
                if (factId == "location") return callerNorm.Contains("san martin 240") && callerNorm.Contains("jujuy");
                if (factId == "incident") return ContainsAny(callerNorm, "cayo", "caida", "jugando", "golpeo", "se golpeo");
                if (factId == "people") return ContainsAny(callerNorm, "solo mi hermano", "1 persona", "una persona", "no hay otra persona", "yo estoy bien", "nadie mas", "solo martin");
                if (factId == "condition") return ContainsAny(callerNorm, "despierto", "responde", "consciente", "hablo");
                if (factId == "risks") return ContainsAny(callerNorm, "no veo fuego", "sin peligro", "cables", "no hay peligro", "ningun peligro", "otro peligro", "no hay cables");
            }
            else if (caseId == "CASE_002")
            {
                if (factId == "location") return callerNorm.Contains("belgrano 815") && callerNorm.Contains("san pedro");
                if (factId == "incident") return ContainsAny(callerNorm, "llama", "fuego", "mesa de trabajo", "empezo a salir humo");
                if (factId == "people") return ContainsAny(callerNorm, "tres personas", "3 personas", "eramos tres", "los tres", "todos afuera", "en la vereda");
                if (factId == "condition") return ContainsAny(callerNorm, "podemos hablar", "lastimado", "lesiones", "ninguno lastimado", "ninguno dice");
                if (factId == "risks") return ContainsAny(callerNorm, "recipientes", "pintura", "recipientes de pintura", "combustible");
            }
            else if (caseId == "CASE_003")
            {
                if (factId == "location") return callerNorm.Contains("mitre") && callerNorm.Contains("rivadavia") && callerNorm.Contains("tilcara");
                if (factId == "incident") return ContainsAny(callerNorm, "chocaron", "choque", "dos autos", "ruido fuerte", "auto detenido", "un solo auto", "suposicion");
                if (factId == "people") return ContainsAny(callerNorm, "una persona visible", "1 persona visible", "persona junto al auto", "una persona junto", "1 persona junto");
                if (factId == "condition") return ContainsAny(callerNorm, "de pie", "lesiones desconocidas", "no puedo saber si tiene lesiones");
                if (factId == "risks") return ContainsAny(callerNorm, "obstruye", "vehiculos lo esquivan", "esquivan", "transito", "calzada");
            }
            return false;
        }
        private static bool ContainsAny(string text, params string[] terms) => Array.Exists(terms, text.Contains);
        public void SelectUnit(EmergencyUnit unit)
        {
            if (Busy || Voice.Recording) { SetNotice("Esperá a que termine la intervención antes de despachar."); return; }
            if (Session?.SelectUnit(unit) != true) { SetNotice("Necesitás una ubicación y una unidad disponible. Se asigna un recurso por caso."); return; }
            View.ShowDispatchConfirmation(unit);
        }
        public void ConfirmDispatch()
        {
            if (Session?.ConfirmDispatch() != true) return;
            EmergencyAudioManager.Instance?.PlayDispatchSuccess();
            View.CloseModal();
            SetNotice(Session.DispatchedUnit.displayName + " en camino. Comunicá la decisión y cerrá la llamada.");
        }
        public void CancelDispatch() { Session?.CancelDecision(); View.CloseModal(); View.Refresh(); }
        public void RequestFinish()
        {
            if (Phase != CallPhase.Active || Busy || Voice.Recording) return;
            View.ShowEndConfirmation(Session.DispatchedUnit == null);
        }
        public void FinishCall()
        {
            if (Phase != CallPhase.Active || Busy || Voice.Recording) return;
            Voice.StopTalking(); relay.Disconnect(); Voice.ClearPlayback();
            if (!operatorTurnRecorded && !string.IsNullOrEmpty(inputTranscript)) callTranscript.Add(new DialogueTurn { role = "operator", text = inputTranscript });
            if (!string.IsNullOrEmpty(CallerText) && callerTurnStarted) callTranscript.Add(new DialogueTurn { role = "caller", text = CallerText });
            CompleteExam();
            var result = Session.Finish();
            if (result == null) return;
            result.transcriptLog = new List<string>(callTranscript.ConvertAll(t => (t.role == "operator" ? "OPERADOR: " : "LLAMANTE: ") + t.text));
            foreach (var t in callTranscript) if (t.role == "operator") result.operatorCharacters += t.text.Length;
            result.evaluationPending = LiveMode && result.operatorCharacters > 0;
            Results.Add(result);
            EmergencyAudioManager.Instance?.PlayCallEnd();
            View.CloseModal(); View.Refresh();
            SaveResults();
            if (result.evaluationPending) StartCoroutine(RequestAIEvaluation(result, new List<DialogueTurn>(callTranscript)));
        }
        [Serializable] public class DialogueTurn { public string role; public string text; }
        [Serializable] private class EvalPayload { public string caseId; public DialogueTurn[] transcript; public string dispatchedUnit; public float seconds; public int turns; }
        [Serializable] private class EvalResponse { public bool available; public CommunicationCriterion[] criteria; }
        private readonly List<DialogueTurn> callTranscript = new List<DialogueTurn>();
        private System.Collections.IEnumerator RequestAIEvaluation(CallResult result, List<DialogueTurn> transcript)
        {
            string baseUrl = GeminiConfig.BackendUrl.Replace("ws://", "http://").Replace("wss://", "https://").TrimEnd('/');
            string url = baseUrl + "/evaluate";
            var payload = new EvalPayload
            {
                caseId = result.caseId,
                transcript = transcript != null ? transcript.ToArray() : new DialogueTurn[0],
                dispatchedUnit = result != null ? result.resource : "Sin despacho",
                seconds = result != null ? result.seconds : 0,
                turns = result.turns
            };
            string json = JsonUtility.ToJson(payload);
            using (var req = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 14;
                yield return req.SendWebRequest();

                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var resp = JsonUtility.FromJson<EvalResponse>(req.downloadHandler.text);
                        if (resp != null && resp.available && DispatchAudit.ApplyCommunication(result, resp.criteria))
                        {
                            View?.Refresh();
                            SaveResults();
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[CAD] Error al deserializar dictamen de IA: " + ex.Message);
                    }
                }
                else
                {
                    Debug.LogWarning("[CAD] Error en solicitud de dictamen de IA: " + req.error);
                }

                // Fallback a dictamen local si la IA no respondió a tiempo
                result.evaluationPending = false;
                result.evaluationSource = "Local · auditoría IA no disponible";
                View?.Refresh();
                SaveResults();
            }
        }
        private void SaveResults()
        {
            if (Application.isBatchMode || Queue == null || ExamMode) return; // Exams and automated checks do not overwrite the player's saved guard.
            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "ultimo-turno.json"),
                    JsonUtility.ToJson(CreateGuardSnapshot(), true));
            }
            catch (IOException) { Notice = "No se pudo guardar el informe local. El resultado sigue visible."; }
            catch (UnauthorizedAccessException) { Notice = "No hay permiso para guardar el informe local."; }
        }
        private void Update()
        {
            if (View == null) return;
            TickExam();
            if (View.LobbyVisible) { View.TickLobby(); return; }
            if (Queue != null && !ExamMode && Phase != CallPhase.Result && Phase != CallPhase.ShiftComplete && Queue.Advance(Time.deltaTime))
            {
                EmergencyAudioManager.Instance?.PlayNewSignal();
                View.Refresh(); SaveResults();
            }
            if (Phase == CallPhase.Active || Phase == CallPhase.Decision) Session.Seconds += Time.deltaTime;
            if (CanChooseCall && WaitingCallCount > 0 && Time.unscaledTime > waitUntil && Time.unscaledTime > nextRing)
            {
                EmergencyAudioManager.Instance?.PlayIncomingRing(); nextRing = Time.unscaledTime + 7f;
            }
            if (Busy && Time.unscaledTime - busySince > 45)
            {
                Voice.StopTalking(); relay.Disconnect(); Voice.ClearPlayback(); Busy = false;
                ConnectionStatus = "VOZ SIN RESPUESTA";
                SetNotice("La respuesta tardó demasiado. Podés continuar este caso en ensayo local.");
            }
            View.Tick();
        }
        private void OnApplicationQuit() { if(View != null && Queue != null)SaveResults(); }
        private void OnDestroy()
        {
            if (relay != null) relay.Message -= OnRelayMessage;
            if (Voice != null)
            {
                Voice.Problem -= SetNotice;
                Voice.CanTalk = null; Voice.BeginTurn = null; Voice.TurnEnded = null;
            }
            if (Instance == this) Instance = null;
        }
    }
}
