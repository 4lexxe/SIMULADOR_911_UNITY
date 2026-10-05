using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EmergencyDispatch.Editor
{
    [InitializeOnLoad]
    public static class DispatchValidation
    {
        private const string Running = "UltimaLlamada.Validation";
        private static int frames, stage, checks;
        private static string output;
        private static string originalBackend;
        static DispatchValidation()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Pump;
        }
        public static void Run()
        {
            
            EditorSceneManager.OpenScene(EmergencyDispatcherBuilderEditor.ScenePath);
            SessionState.SetBool(Running, true);
            EditorApplication.update -= Pump;
            EditorApplication.update += Pump;
            EditorApplication.isPlaying = true;
        }
        private static void Pump()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            var sceneGame = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            if (sceneGame?.View == null || ++frames < 80) return;
            try
            {
                output = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
                var game = sceneGame;
                if (stage == 0)
                {
                    DomainChecks();
                    if (game.LiveMode) game.ToggleMode();
                    Capture("overview.png");
                    game.StartShift(); game.SelectIncomingCall("CASE_001");
                    Check(game.Phase == CallPhase.Incoming, "A shift begins with an incoming call.");
                    Capture("incoming.png");
                    game.Answer();
                    game.SubmitText("Entiendo, estoy con vos. ¿Dónde están, en San Martín 240?");
                    game.SubmitText("¿Qué ocurrió?");
                    game.SubmitText("¿Cuántas personas están afectadas?");
                    Capture("case-active.png");
                    DeskCameraController.Instance.SetView(DeskCameraController.CameraViewMode.CenterMonitor);
                    DeskCameraController.Instance.Snap();
                    Capture("call-closeup.png");
                    stage = 1; frames = 0; return;
                }
                if (stage == 1)
                {
                    game.SubmitText("¿Está despierto?");
                    game.SubmitText("¿Hay algún peligro?");
                    game.SubmitText("Entiendo. Entonces, confirmo los datos, ¿es correcto?");
                    game.SelectUnit(game.Fleet.Units.First(u => u.type == UnitType.Ambulance));
                    Check(game.Phase == CallPhase.Decision, "Dispatch requires confirmation.");
                    game.ConfirmDispatch();
                    Check(game.Phase == CallPhase.Active && game.Session.DispatchedUnit != null, "Dispatch keeps the conversation active.");
                    game.SubmitText("Ya asigné la unidad, está en camino.");
                    game.FinishCall();
                    Check(game.Session.Result.Total == 100, "Complete eight-turn conversation scores 100.");
                    Capture("case-result.png");
                    for (int c = 1; c < game.CaseCount; c++)
                    {
                        game.NextCase(); game.Queue.Advance(120); game.SelectIncomingCall(game.Cases[c].id); game.Answer();
                        foreach (string q in new[] { "¿Dónde ocurrió?", "¿Qué pasó?", "¿Cuántas personas hay?", "¿Cuál es su estado?", "¿Qué riesgos hay?" }) game.SubmitText(q);
                        if (c == 2)
                        {
                            Check(!game.Session.Facts["incident"].verified, "Contradictory report remains unverified.");
                            game.SubmitText("¿Lo viste o lo suponés?");
                            Check(game.Session.Facts["incident"].verified && game.Session.Facts["incident"].value.Contains("no confirmado"), "Verification corrects the assumed collision.");
                        }
                        game.SelectUnit(game.Fleet.Units.First(u => u.type == game.Session.Definition.requiredUnitType));
                        game.ConfirmDispatch(); game.FinishCall();
                    }
                    game.NextCase();
                    Check(game.Phase == CallPhase.ShiftComplete && game.Results.Count == 8, "All eight calls complete one shift.");
                    Capture("shift-result.png");
                    game.StartShift(); game.SelectIncomingCall("CASE_001"); Check(game.Phase == CallPhase.Incoming && game.Results.Count == 0 && game.Fleet.Units.All(u => u.status == UnitStatus.Available), "Replay resets cases, results and fleet.");
                    if (Array.IndexOf(Environment.GetCommandLineArgs(), "-testRelay") >= 0)
                    {
                        originalBackend = GeminiConfig.BackendUrl;
                        GeminiConfig.BackendUrl = "ws://127.0.0.1:18787";
                        game.ToggleMode(); game.Answer();
                        stage = 2; frames = 0; return;
                    }
                    Complete(false);
                }
                if (stage == 2)
                {
                    if (frames >= 900) throw new Exception("Live fixture connection timed out.");
                    if (game.Busy) return;
                    Check(game.ConnectionStatus.Contains("CONECTADO"), "Unity ClientWebSocket establishes the relay session.");
                    Check(game.Voice.ReceivedSamples > 0, "Unity decodes the 24 kHz PCM response.");
                    game.SubmitText("¿Dónde están?");
                    stage = 3; frames = 0; return;
                }
                if (stage == 3)
                {
                    if (frames >= 900) throw new Exception("Live fixture response timed out.");
                    if (game.Busy) return;
                    Check(game.Session.Facts.TryGetValue("location", out var fact) && fact.value == game.Session.Definition.facts[0].value, "Live function evidence reaches authoritative Unity case state.");
                    Check(game.CallerText.Contains("Club Deportivo Norte"), "Live transcription reaches the subtitle.");
                    game.ContinueOffline();
                    Check(!game.LiveMode && !game.Busy, "Explicit offline recovery closes the voice session.");
                    GeminiConfig.BackendUrl = originalBackend;
                    Complete(true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (originalBackend != null) GeminiConfig.BackendUrl = originalBackend;
                if (output != null) File.WriteAllText(Path.Combine(output, "unity-validation.txt"), ex.ToString());
                SessionState.SetBool(Running, false); EditorApplication.Exit(1);
            }
        }
        private static void Complete(bool relay)
        {
            File.WriteAllText(Path.Combine(output, "unity-validation.txt"), checks + " assertions passed. Eight-call play-mode flow and six renders completed.\n" +
                (relay ? "Unity WebSocket, PCM decode, subtitles, validated fact event and offline recovery passed against local synthetic Gemini fixture. No real microphone or Google API was used.\n" : ""));
            SessionState.SetBool(Running, false); EditorApplication.update -= Pump;
            EditorApplication.Exit(0);
        }
        private static void Check(bool ok, string message)
        {
            if (!ok) throw new Exception("Validation failed: " + message);
            checks++;
        }
        private static void DomainChecks()
        {
            var cases = ScenarioDatabase.GetDefaultCases();
            Check(cases.Count == 8, "Eight authored scenarios load.");
            var blank = new CallSession(cases[0]); blank.Answer();
            Check(blank.Finish().Total == 0, "Empty call scores zero.");
            var session = new CallSession(cases[0]);
            var unit = ScenarioDatabase.GetDefaultUnits()[0];
            Check(!session.SelectUnit(unit), "Cannot dispatch before answering.");
            session.Answer(); Check(!session.SelectUnit(unit), "Cannot dispatch without location.");
            int turn = session.BeginTurn();
            Check(!session.RegisterFact(turn, "bad", "location", "Invented address", true), "Fabricated values rejected.");
            Check(!session.RegisterFact(turn, "bad", "unknown", "Any", true), "Unknown field rejected.");
            Check(!session.RegisterFact(999, "bad", "location", cases[0].facts[0].value, true), "Unknown turn rejected.");
            Check(session.RegisterFact(turn, "fact-1", "location", cases[0].facts[0].value, true), "Canonical location accepted.");
            Check(!session.RegisterFact(turn, "fact-1", "location", cases[0].facts[0].value, true), "Repeated event rejected.");
            Check(session.RegisterTag(turn, "relevant") && !session.RegisterTag(turn, "relevant"), "Classification deduplicates per turn.");
            Check(!session.RegisterTag(turn, "award_100"), "Unknown classification rejected.");
            session.AdjustEmotion(turn, 1000); session.AdjustEmotion(turn, 1000);
            Check(session.Calm == 40, "Emotion is clamped and applied once.");
            Check(session.SelectUnit(unit), "Known location permits selection.");
            session.CancelDecision(); Check(unit.status == UnitStatus.Available, "Cancel leaves fleet unchanged.");
            session.SelectUnit(unit); Check(session.ConfirmDispatch(), "Valid dispatch succeeds.");
            Check(!session.SelectUnit(ScenarioDatabase.GetDefaultUnits()[1]), "Only one unit can be assigned.");
            var result = session.Finish();
            Check(ReferenceEquals(result, session.Finish()), "Finish is idempotent.");
            Check(!session.RegisterTag(turn, "clear"), "Late events cannot change results.");
            Check(result.Total >= 0 && result.Total <= 100, "Score is bounded.");
            var wrong = new CallSession(cases[0]); wrong.Answer(); int t = wrong.BeginTurn();
            wrong.RegisterFact(t, "loc", "location", cases[0].facts[0].value, true);
            wrong.SelectUnit(ScenarioDatabase.GetDefaultUnits()[2]); wrong.ConfirmDispatch();
            Check(wrong.Finish().decision == 0, "Incorrect resource gives no decision points.");
        }
        private static void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            var camera = Camera.main;
            var target = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = target; camera.aspect = 1920f / 1080;
            var hud = UnityEngine.Object.FindObjectsByType<Canvas>().First(c => c.renderMode == RenderMode.ScreenSpaceCamera);
            float oldScale = hud.scaleFactor;
            hud.scaleFactor = 1;
            foreach (var text in hud.GetComponentsInChildren<UnityEngine.UI.Text>()) text.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(output, name), image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            hud.scaleFactor = oldScale;
            UnityEngine.Object.Destroy(image); target.Release(); UnityEngine.Object.Destroy(target);
        }
    }
}
