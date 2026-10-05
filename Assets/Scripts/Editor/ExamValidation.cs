using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch.Editor
{
    [InitializeOnLoad]
    public static class ExamValidation
    {
        const string Running="UltimaLlamada.ExamValidation";
        static int stage;
        static double readyAt;
        static readonly System.Collections.Generic.List<string> checks=new System.Collections.Generic.List<string>();
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        static ExamValidation(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Pump;}
        public static void AuthorAndRun(){ExamSceneAuthoring.Run();Run();}
        public static void Run()
        {
            EditorSceneManager.OpenScene(EmergencyDispatcherBuilderEditor.ScenePath);
            SessionState.SetBool(Running,true);EditorApplication.update-=Pump;EditorApplication.update+=Pump;
            EditorApplication.isPlaying=true;readyAt=EditorApplication.timeSinceStartup+2;
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add("PASS · "+message);}
        static void Expire(GameManager game)
        {
            typeof(GameManager).GetField("examStartedAt",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,Time.realtimeSinceStartupAsDouble-181);
            typeof(GameManager).GetMethod("TickExam",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,null);
        }
        static void Pump()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<readyAt)return;
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();if(game?.View==null)return;
            try
            {
                if(stage==0)
                {
                    Check(game.GetComponentInParent<Transform>().root.GetComponentInChildren<ExamHud>(true)?.label!=null,"Exam clock and references authored before Play.");
                    game.View.Execute(DispatchAction.LobbyPage,1);
                    Check(game.View.GetComponentsInChildren<DispatchButton>().Any(b=>b.action==DispatchAction.StartExam),"Exam button reachable from Cases.");
                    stage++;readyAt=EditorApplication.timeSinceStartup+.3;return;
                }
                if(stage==1)
                {
                    Capture("exam-menu.png");
                    game.View.Execute(DispatchAction.StartExam,0);
                    Check(!game.ExamMode,"Briefing requires intentional start before the clock runs.");
                    game.View.CloseModal();if(game.LiveMode)game.ToggleMode();game.StartExam();
                    Check(game.ExamMode&&!game.ExamFinished&&game.ExamRemaining>178&&game.ExamRemaining<=180,"Exam starts with a 180-second budget.");
                    Check(game.Session.Definition.id==GameManager.ExamCaseId&&game.View.MapBoardVisible,"Exam selects the documented case on Jujuy map.");
                    Check(game.WaitingCallCount==1&&game.RemainingCalls==1,"Exam has exactly one playable case.");
                    Check(!game.SelectIncomingCall("CASE_003"),"Other guard calls cannot replace exam case.");
                    Check(game.View.IncomingBoard.markers.Count(m=>m.gameObject.activeSelf)==1,"Only exam signal visible.");
                    stage++;readyAt=EditorApplication.timeSinceStartup+.3;return;
                }
                if(stage==2)
                {
                    Capture("exam-map.png");
                    game.View.ShowLobby();Expire(game);
                    Check(game.ExamFinished&&game.ExamTimedOut&&game.ExamElapsed==180,"Deadline also expires from menu before answering.");
                    Check(!game.View.LobbyVisible&&game.Phase==CallPhase.Result&&game.Results.Count==1,"Expiry closes menu and shows one final result.");
                    Check(game.Results[0].Total==0&&game.Results[0].transcriptLog.Count==0,"Unanswered exam has no invented evidence or prior transcript.");
                    Check(!game.HasCurrentGuard,"A finished exam is not offered as an unfinished guard.");
                    game.RetryCase();Check(!game.ExamFinished&&game.ExamRemaining>179&&game.Results.Count==0,"Retry creates a fresh exam and resets its budget.");
                    game.Answer();game.SubmitText("¿En qué dirección están?");game.SelectUnit(game.Fleet.Units[0]);
                    Check(game.Phase==CallPhase.Decision,"Pending dispatch confirmation reached.");Expire(game);
                    Check(game.ExamFinished&&game.Session.DispatchedUnit==null&&game.Results[0].decision==0,"Deadline never auto-confirms a pending dispatch.");
                    game.StartExam();game.Answer();
                    typeof(GameManager).GetProperty("Busy").SetValue(game,true);Expire(game);
                    Check(game.ExamFinished&&!game.Busy,"A stalled service cannot extend the exam.");
                    game.StartExam();game.Answer();typeof(VoiceManager).GetProperty("Recording").SetValue(game.Voice,true);Expire(game);
                    Check(game.ExamFinished&&!game.Voice.Recording,"Recording state is stopped at expiry (synthetic state, no physical capture).");
                    var commandSession=new CallSession(game.Cases[0]);int commandTurn=commandSession.Answer()?commandSession.BeginTurn():0;
                    commandSession.RegisterTag(commandTurn,"relevant");commandSession.ExcludeNavigationTurn(commandTurn);
                    Check(commandSession.Turns==1&&commandSession.ScoredTurns==0&&commandSession.Count("relevant")==0,"Navigation preserves relay IDs but is excluded from scored turns and local tags.");
                    game.StartExam();game.Answer();
                    foreach(var phrase in new[]{"central mostrar mapa","Central mostrar llamada.","central mostrar unidades","central mostrar escritorio"})
                        Check(VoiceNavigation.TryParse(phrase,out _),"Exact navigation command accepted: "+phrase);
                    Check(!VoiceNavigation.TryParse("No quiero central mostrar mapa",out _)&&!VoiceNavigation.TryParse("mapa",out _),"Conversation mentions and partial commands do not trigger navigation.");
                    Check(VoiceNavigation.TryParse("Central, mostrar unidades.",out _)&&VoiceNavigation.TryParse("  central   mostrar mapa! ",out _),"Recognizer punctuation and repeated whitespace preserve exact commands.");
                    game.SubmitText("central mostrar unidades");
                    Check(DeskCameraController.Instance.Mode==DeskCameraController.CameraViewMode.RightMonitor&&game.Session.Turns==0,"Text command moves camera without awarding dialogue points.");
                    var relayMessage=typeof(GameManager).GetMethod("OnRelayMessage",BindingFlags.NonPublic|BindingFlags.Instance);
                    relayMessage.Invoke(game,new object[]{new RelayMessage{type="operator",caseId=GameManager.ExamCaseId,text="Central mostrar mapa."}});
                    relayMessage.Invoke(game,new object[]{new RelayMessage{type="turn_complete",caseId=GameManager.ExamCaseId}});
                    Check(DeskCameraController.Instance.Mode==DeskCameraController.CameraViewMode.LeftMonitor,"Recognized voice transcript executes navigation through relay path (synthetic input).");
                    game.StartExam();game.Answer();
                    foreach(var q in new[]{"Entiendo, estoy con vos. ¿En qué dirección están?","¿Qué ocurrió?","¿Cuántas personas están afectadas?","¿Cuál es el estado de tu hermano?","¿Hay algún peligro?","Entiendo. Entonces, confirmo lo que me contaste, ¿es correcto?"})game.SubmitText(q);
                    game.SelectUnit(game.Fleet.Units[0]);game.ConfirmDispatch();game.SubmitText("La ayuda ya está en camino.");
                    DeskCameraController.Instance.SetView(DeskCameraController.CameraViewMode.CenterMonitor);DeskCameraController.Instance.Snap();
                    stage++;readyAt=EditorApplication.timeSinceStartup+.4;return;
                }
                if(stage==3)
                {
                    Capture("exam-call.png");game.FinishCall();
                    Check(game.ExamFinished&&!game.ExamTimedOut&&game.ExamElapsed<180,"Normal completion ends the timer early.");
                    Check(game.Results[0].Total==100&&game.Results[0].decision==20,"Complete local dialogue earns the existing content-based 100/100.");
                    stage++;readyAt=EditorApplication.timeSinceStartup+2;return;
                }
                Capture("exam-score.png");game.NextCase();
                Check(game.View.LobbyVisible,"After exam, Next returns to menu rather than a second case.");
                game.StartShift();Check(!game.ExamMode&&game.WaitingCallCount==3&&game.RemainingCalls==8,"Normal guard retains eight cases and its initial three signals.");
                File.WriteAllText(Path.Combine(Output,"exam-validation.txt"),checks.Count+" checks passed. Synthetic/local validation; no physical microphone or real Gemini call.\n"+string.Join("\n",checks));Finish(0);
            }
            catch(Exception ex){File.WriteAllText(Path.Combine(Output,"exam-validation.txt"),string.Join("\n",checks)+"\nFAIL · "+ex);Debug.LogException(ex);Finish(1);}
        }
        static void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();var camera=Camera.main;var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;camera.aspect=1920f/1080;
            var hud=UnityEngine.Object.FindObjectsByType<Canvas>().First(c=>c.renderMode==RenderMode.ScreenSpaceCamera);float old=hud.scaleFactor;hud.scaleFactor=1;
            foreach(var label in hud.GetComponentsInChildren<Text>())label.SetAllDirty();
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name),texture.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;hud.scaleFactor=old;UnityEngine.Object.Destroy(texture);target.Release();UnityEngine.Object.Destroy(target);
        }
        static void Finish(int code){SessionState.SetBool(Running,false);EditorApplication.update-=Pump;EditorApplication.Exit(code);}
    }
}
