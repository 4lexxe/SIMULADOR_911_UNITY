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
    public static class JujuyGameplayValidation
    {
        const string Running="UltimaLlamada.JujuyValidation";
        static int stage,checks;
        static double readyAt;
        static GuardSave checkpoint;
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        static JujuyGameplayValidation() { if(SessionState.GetBool(Running,false))EditorApplication.update+=Pump; }
        public static void AuthorAndRun() { JujuySceneAuthoring.Run(); Run(); }
        public static void Run()
        {
            EditorSceneManager.OpenScene(EmergencyDispatcherBuilderEditor.ScenePath);
            SessionState.SetBool(Running,true);EditorApplication.update-=Pump;EditorApplication.update+=Pump;
            EditorApplication.isPlaying=true;readyAt=EditorApplication.timeSinceStartup+2;
        }
        static void Check(bool condition,string description) { if(!condition)throw new Exception(description);checks++; }
        static void Pump()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<readyAt)return;
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();if(game?.View==null)return;
            try
            {
                var board=game.View.IncomingBoard;
                if(stage==0)
                {
                    Check(game.View.LobbyVisible&&game.Session==null,"Start remains in the lobby.");
                    Check(game.Cases.Count==8&&game.Cases.Select(c=>c.locality).Distinct().Count()==8,"Eight different georeferenced locations.");
                    Check(game.Cases.All(c=>c.latitude< -21&&c.latitude> -25&&c.longitude< -64&&c.longitude> -68),"All cases are in Jujuy's geographic extent.");
                    Check(board.markers.Length==8&&board.rows.Length==8,"Every signal and queue row is authored before Play.");
                    var objects=game.transform.root.GetComponentsInChildren<Transform>(true);
                    Check(objects.All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No missing scripts in the authored scene.");
                    if(game.LiveMode)game.ToggleMode();game.StartShift();
                    Check(game.Session==null&&game.Phase==CallPhase.Idle&&board.Visible,"Starting opens the provincial map without auto-answer or auto-selection.");
                    Check(game.WaitingCallCount==3&&!board.answer.interactable,"Three initial signals; answering requires a selection.");
                    Check(!game.SelectIncomingCall("CASE_008"),"Future signals cannot be answered early.");
                    board.markers[2].GetComponent<Button>().onClick.Invoke();
                    Check(game.Session.Definition.id=="CASE_003"&&game.Phase==CallPhase.Incoming,"The actual geographic marker selects its own case.");
                    board.rows[3].GetComponent<Button>().onClick.Invoke();
                    Check(game.Session.Definition.id=="CASE_004"&&game.Session.Facts.Count==0,"Changing selection preserves every other signal and reveals no canonical facts.");
                    board.Refresh();Check(board.answer.interactable,"Selected incoming call can be answered.");
                    board.navigation.Zoom(.75f);Check(board.navigation.ZoomLevel==1.75f,"Map zoom is functional.");board.navigation.ResetMap();
                    stage=1;readyAt=EditorApplication.timeSinceStartup+.4;return;
                }
                if(stage==1)
                {
                    Capture("jujuy-incoming-map.png");Capture("jujuy-map-720p.png",1280,720);Capture("jujuy-map-4x3.png",1440,1080);
                    board.answer.onClick.Invoke();
                    Check(game.Phase==CallPhase.Active&&!board.Visible,"Answer starts the selected conversation and returns to the desk.");
                    var session=game.Session;
                    game.NextCase();Check(ReferenceEquals(session,game.Session),"Next cannot discard a live call.");
                    Check(!game.SelectIncomingCall("CASE_001"),"Another pin cannot replace a live call.");
                    game.Queue.Advance(25);game.View.Refresh();
                    Check(game.IsWaiting(game.Cases[1])&&ReferenceEquals(session,game.Session),"A later signal arrives during the active call without interrupting it.");
                    board.Show();Check(!board.answer.interactable,"Active-call map is read-only for answering.");board.Hide();
                    foreach(var q in new[]{"Entiendo, estoy con vos. ¿En qué dirección están?","¿Qué ocurrió?","¿Cuántas personas están afectadas?","¿Cuál es el estado de tu papá?","¿Hay algún peligro?","Entiendo. Entonces, confirmo lo que me contaste, ¿es correcto?"})game.SubmitText(q);
                    Check(session.Facts.Count==5,"The chosen new case supplies all five facts through its own dialogue.");
                    var map=UnityEngine.Object.FindAnyObjectByType<IncidentMapManager>();Check(map.Pin.gameObject.activeSelf,"The authored confirmed-location pin still works.");
                    DeskCameraController.Instance.SetView(DeskCameraController.CameraViewMode.LeftMonitor);DeskCameraController.Instance.Snap();Capture("jujuy-monitor.png");
                    var unit=game.Fleet.Units.First(u=>u.type==UnitType.Ambulance);game.SelectUnit(unit);game.ConfirmDispatch();game.SubmitText("La ayuda ya está en camino.");game.FinishCall();
                    Check(game.Results.Count==1&&game.Results[0].caseId=="CASE_004","Results use chosen case identity, independent of catalogue order.");
                    Check(game.Results[0].Total==100,"Content-based scoring remains intact for a new location: "+game.Results[0].Total+" / "+string.Join(",",DispatchAudit.Values(game.Results[0])));
                    stage=2;readyAt=EditorApplication.timeSinceStartup+2.3;return;
                }
                if(stage==2)
                {
                    Capture("jujuy-score.png");
                    var viewData=new SerializedObject(game.View);var score=(Text)viewData.FindProperty("resultScore").objectReferenceValue;
                    Check(score.text=="100"&&score.rectTransform.localScale==Vector3.one,"Animated score settles exactly, including its impact scale.");
                    var unit=game.Session.DispatchedUnit;game.RetryCase();Check(unit.status==UnitStatus.Available&&game.Results.Count==0,"Retry releases only the selected case resource and clears its score.");
                    game.Answer();game.FinishCall();game.NextCase();
                    Check(board.Visible&&game.Session==null&&!game.SelectIncomingCall("CASE_004"),"Finishing returns to the map; completed calls cannot be re-answered.");
                    game.Queue.Advance(100);game.View.Refresh();
                    Capture("jujuy-all-signals.png");
                    board.markers[7].GetComponent<Button>().onClick.Invoke();game.Answer();game.FinishCall();game.NextCase();
                    game.SelectIncomingCall("CASE_001");
                    string json=JsonUtility.ToJson(game.CreateGuardSnapshot());
                    Check(GuardSave.TryParse(json,game.Cases.ToList(),out checkpoint),"Save supports non-sequential results and a selected pending call.");
                    Check(checkpoint.calls[0].caseId=="CASE_004"&&checkpoint.calls[1].caseId=="CASE_008"&&checkpoint.selectedCaseId=="CASE_001","Checkpoint preserves actual selection order.");
                    var invalid=JsonUtility.FromJson<GuardSave>(json);invalid.calls.Add(invalid.calls[0]);Check(!GuardSave.TryParse(JsonUtility.ToJson(invalid),game.Cases.ToList(),out _),"Duplicate completed calls are rejected.");
                    invalid=JsonUtility.FromJson<GuardSave>(json);invalid.selectedCaseId="CASE_008";Check(!GuardSave.TryParse(JsonUtility.ToJson(invalid),game.Cases.ToList(),out _),"Completed calls cannot be restored as incoming.");
                    string legacy="{\"date\":\"2026-09-28\",\"calls\":[]}";
                    Check(GuardSave.TryParse(legacy,game.Cases.ToList(),out var old)&&old.version==3,"Existing guards migrate to the queue save format.");
                    foreach(int n in new[]{0,4,1,6,5,2})
                    { Check(game.SelectIncomingCall(game.Cases[n].id),"Remaining signal selectable: "+n);game.Answer();game.FinishCall();game.NextCase(); }
                    Check(game.Phase==CallPhase.ShiftComplete&&game.Results.Count==8,"All eight cases can finish in a player-selected order.");
                    stage=3;readyAt=EditorApplication.timeSinceStartup+2.3;return;
                }
                if(stage==3)
                {
                    Capture("jujuy-guard.png");
                    var score=(Text)new SerializedObject(game.View).FindProperty("resultScore").objectReferenceValue;
                    string finalScore=score.text;
                    game.View.Execute(DispatchAction.ResultTab,1);game.View.Execute(DispatchAction.ResultTab,0);
                    Check(score.text==finalScore,"Switching guard report tabs does not restart its tally or reward.");
                    typeof(GameManager).GetProperty("SavedGuard").SetValue(game,checkpoint);game.ContinueGuard();
                    Check(game.Results.Count==2&&game.Session.Definition.id=="CASE_001"&&game.Phase==CallPhase.Incoming&&board.Visible,"Continue restores the non-sequential guard and pending selection.");
                    Check(game.Queue.Elapsed>=checkpoint.queueElapsed,"Continue preserves queue timing.");
                    game.NextCase();board.Hide();game.View.SetCenterTab(1);
                    UnityEngine.Object.FindObjectsByType<HistoryRowView>().First().open.onClick.Invoke();
                    Check(game.View.ResultVisible,"An archived result can be opened while waiting for calls.");
                    game.View.ShowLobby();game.StartShift();
                    Check(board.Visible&&!game.View.ResultVisible&&game.Session==null,"Starting a guard from an archived audit clears the old overlay before opening the map.");
                    game.View.ShowLobby();game.View.Execute(DispatchAction.LobbyPage,1);
                    Check(game.View.LobbyVisible,"The menu can interrupt the map without losing the guard.");
                    stage=4;readyAt=EditorApplication.timeSinceStartup+.3;return;
                }
                Capture("jujuy-cases.png");
                File.WriteAllText(Path.Combine(Output,"jujuy-validation.txt"),checks+" checks passed: authored scene, geographic selection, dynamic arrivals, active-call isolation, new-case dialogue, exact scoring animation, retry, out-of-order saves, migration, full eight-call guard and continuation. No microphone, remote model, player save or preference writes.");
                Finish(0);
            }
            catch(Exception ex) { File.WriteAllText(Path.Combine(Output,"jujuy-validation.txt"),ex.ToString());Debug.LogException(ex);Finish(1); }
        }
        static void Capture(string name,int width=1920,int height=1080)
        {
            Canvas.ForceUpdateCanvases();var camera=Camera.main;var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.aspect=(float)width/height;
            var hud=UnityEngine.Object.FindObjectsByType<Canvas>().First(c=>c.renderMode==RenderMode.ScreenSpaceCamera);float old=hud.scaleFactor;hud.scaleFactor=Mathf.Min(width/1920f,height/1080f);
            foreach(var text in hud.GetComponentsInChildren<Text>())text.SetAllDirty();Canvas.ForceUpdateCanvases();camera.Render();
            RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;hud.scaleFactor=old;UnityEngine.Object.Destroy(image);target.Release();UnityEngine.Object.Destroy(target);
        }
        static void Finish(int code) { SessionState.SetBool(Running,false);EditorApplication.update-=Pump;EditorApplication.Exit(code); }
    }
}
