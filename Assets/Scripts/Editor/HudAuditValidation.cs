using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch.Editor
{
    [InitializeOnLoad]
    public static class HudAuditValidation
    {
        const string Running="UltimaLlamada.HudAuditValidation";
        static int stage,checks;
        static double readyAt;
        static string output;
        static HudAuditValidation() { if(SessionState.GetBool(Running,false))EditorApplication.update+=Pump; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(EmergencyDispatcherBuilderEditor.ScenePath);
            SessionState.SetBool(Running,true);EditorApplication.update-=Pump;EditorApplication.update+=Pump;EditorApplication.isPlaying=true;
            readyAt=EditorApplication.timeSinceStartup+2;
        }
        static void Check(bool ok,string text) { if(!ok)throw new Exception(text);checks++; }
        static void Pump()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<readyAt)return;
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();if(game?.View==null)return;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            try
            {
                if(stage==0)
                {
                    if(game.LiveMode)game.ToggleMode();
                    Check(!game.LiveMode,"No external model or microphone during validation.");
                    var empty=new CallSession(ScenarioDatabase.GetDefaultCases()[0]);empty.Answer();var zero=empty.Finish();
                    Check(zero.Total==0&&zero.positiveNotes.Count==0,"Empty call has no points or invented praise.");
                    Capture("hud-desk.png");
                    game.StartShift();game.SelectIncomingCall("CASE_001");game.Answer();
                    foreach(var q in new[]{"Entiendo, estoy con vos. ¿En qué dirección están, en San Martín 240?","¿Qué ocurrió?","¿Cuántas personas están afectadas?","¿Está despierto?","¿Hay algún peligro?","Entiendo. Entonces, confirmo los datos, ¿es correcto?"})game.SubmitText(q);
                    Check(game.Session.Facts.Count==5,"Five canonical case facts discovered.");
                    DeskCameraController.Instance.SetView(DeskCameraController.CameraViewMode.CenterMonitor);DeskCameraController.Instance.Snap();
                    Capture("hud-call.png");
                    var unit=game.Fleet.Units.First(u=>u.type==UnitType.Ambulance);game.SelectUnit(unit);game.ConfirmDispatch();
                    game.SubmitText("Ya asigné la unidad, está en camino.");game.FinishCall();
                    Check(game.Session.Result.Total==100,"Complete grounded local performance gives 100.");
                    Check(!game.Session.Result.evaluationPending,"Offline play never waits for a remote evaluation.");
                    Check(game.Session.Result.transcriptLog.Count==15,"Each operator and caller turn is recorded once.");
                    stage=1;readyAt=EditorApplication.timeSinceStartup+2.3;return;
                }
                if(stage==1)
                {
                    Capture("hud-score-perfect.png");
                    var r=game.Session.Result;
                    var bad=DispatchAudit.CriterionIds.Select(id=>new CommunicationCriterion{id=id,score=3,reason="Ejemplo",evidence="Una cita que nadie dijo"}).ToArray();
                    Check(!DispatchAudit.ApplyCommunication(r,bad)&&r.Total==100,"Fabricated quotes cannot change the score.");
                    // Deliberately authored low-communication fixture for visual regression (40 + 9 + 20 + 10 + 5).
                    int[] scores={3,2,0,0,4};
                    string[] reasons={"Las preguntas reúnen datos útiles; faltó conectar las respuestas.","Las preguntas son breves, pero faltó ordenar la conversación.","No se registró una intervención de contención en esta muestra.","No se registró una verificación de lo entendido en esta muestra.","El trato fue correcto; faltó una presentación inicial."};
                    r.transcriptLog=new System.Collections.Generic.List<string>{"LLAMANTE: Ayuda, mi hermano se cayó.","OPERADOR: ¿Dónde están?","LLAMANTE: En San Martín 240.","OPERADOR: ¿Qué pasó?","LLAMANTE: Se cayó mientras jugaba."};
                    r.turns=2;r.seconds=132;r.operatorCharacters="¿Dónde están?¿Qué pasó?".Length;
                    var criteria=DispatchAudit.CriterionIds.Select((id,i)=>new CommunicationCriterion{id=id,score=scores[i],reason=reasons[i],evidence=scores[i]==0?"":"¿Dónde están?"}).ToArray();
                    Check(DispatchAudit.ApplyCommunication(r,criteria),"Grounded criteria are accepted.");
                    Check(r.Total==84&&r.communication==9&&r.areas[1].score==9,"All displayed scores share the same 84/100 result.");
                    Check(r.grade=="Caso resuelto"&&r.improvementNotes.Any(n=>n.Contains("0/5")),"Grade and feedback update with the communication assessment.");
                    var aggregate=DispatchAudit.Aggregate(new[]{r,zeroResult()});
                    Check(aggregate.Total==aggregate.areas.Sum(a=>a.score),"Guard total equals its five average bars.");
                    game.View.Refresh();stage=2;readyAt=EditorApplication.timeSinceStartup+2.3;return;
                }
                if(stage==2)
                {
                    Capture("hud-score-reference.png");
                    var report=UnityEngine.Object.FindObjectsByType<Button>().First(b=>b.name.StartsWith("VER INFORME COMPLETO"));report.onClick.Invoke();
                    Check(game.View.ResultVisible,"Report remains inside the result overlay.");
                    Canvas.ForceUpdateCanvases();
                    var scroll=UnityEngine.Object.FindObjectsByType<ScrollRect>().First(s=>s.name=="Informe desplazable");
                    Check(scroll.content.rect.height>scroll.viewport.rect.height,"Long report grows and is scrollable without losing the transcript.");
                    Capture("hud-report.png");
                    stage=3;readyAt=EditorApplication.timeSinceStartup+.2;return;
                }
                if(stage==3)
                {
                    var unit=game.Session.DispatchedUnit;game.RetryCase();
                    Check(unit.status==UnitStatus.Available&&string.IsNullOrEmpty(unit.assignedCaseId),"Retry releases the resource used by this case.");
                    Check(game.Results.Count==0&&game.Phase==CallPhase.Incoming&&!game.View.ResultVisible,"Retry clears the result and opens a fresh call.");
                    game.Answer();game.FinishCall();
                    Check(game.Session.Result.Total==0&&game.Session.Result.operatorCharacters==0,"Retry cannot reuse the previous transcript or score.");
                    game.NextCase();game.Queue.Advance(120);
                    foreach(var c in game.Cases.Skip(1)){game.SelectIncomingCall(c.id);game.Answer();game.FinishCall();game.NextCase();}
                    Check(game.Phase==CallPhase.ShiftComplete&&game.Results.Count==8,"The eight-call guard can still complete.");
                    stage=4;readyAt=EditorApplication.timeSinceStartup+2.3;return;
                }
                Capture("hud-guard.png");
                File.WriteAllText(Path.Combine(output,"hud-validation.txt"),checks+" assertions passed. Unity compiled, local play, evidence rejection, 84-point scoring fixture, full report, retry, guard averages and six rendered screenshots verified. No real model or microphone used.");
                Finish(0);
            }
            catch(Exception ex) { File.WriteAllText(Path.Combine(output,"hud-validation.txt"),ex.ToString());Debug.LogException(ex);Finish(1); }
        }
        static CallResult zeroResult() { var s=new CallSession(ScenarioDatabase.GetDefaultCases()[0]);s.Answer();return s.Finish(); }
        static void Finish(int code) { SessionState.SetBool(Running,false);EditorApplication.update-=Pump;EditorApplication.Exit(code); }
        static void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();var camera=Camera.main;var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;camera.aspect=1920f/1080;
            var hud=UnityEngine.Object.FindObjectsByType<Canvas>().First(c=>c.renderMode==RenderMode.ScreenSpaceCamera);float old=hud.scaleFactor;hud.scaleFactor=1;
            foreach(var text in hud.GetComponentsInChildren<Text>())text.SetAllDirty();Canvas.ForceUpdateCanvases();camera.Render();
            RenderTexture.active=target;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;hud.scaleFactor=old;UnityEngine.Object.Destroy(image);target.Release();UnityEngine.Object.Destroy(target);
        }
    }
}
