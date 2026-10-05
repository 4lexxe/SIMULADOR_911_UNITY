using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch.Editor
{
    [InitializeOnLoad]
    public static class LobbyValidation
    {
        const string Running="UltimaLlamada.LobbyValidation";
        static double readyAt;
        static int checks;
        static string output;
        static LobbyValidation() { if(SessionState.GetBool(Running,false))EditorApplication.update+=Pump; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(EmergencyDispatcherBuilderEditor.ScenePath);
            SessionState.SetBool(Running,true);EditorApplication.update-=Pump;EditorApplication.update+=Pump;EditorApplication.isPlaying=true;readyAt=EditorApplication.timeSinceStartup+2;
        }
        static void Check(bool ok,string message) { if(!ok)throw new Exception(message);checks++; }
        static Button Find(string name) => UnityEngine.Object.FindObjectsByType<Button>().First(b=>b.name==name&&b.gameObject.activeInHierarchy);
        static void Click(string name) { var button=Find(name);Check(button.interactable,"Button is usable: "+name);button.onClick.Invoke();Canvas.ForceUpdateCanvases(); }
        static void Pump()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<readyAt)return;
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();if(game?.View==null)return;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            try
            {
                Check(game.View.LobbyVisible&&game.Phase==CallPhase.Idle,"Play starts in the lobby without an active call.");
                Check(Resources.Load<Texture2D>("UI/LobbyBackground")!=null,"The supplied background is packaged with the game.");
                Check(UnityEngine.Object.FindObjectsByType<Button>().Count(b=>b.name.StartsWith("Menú · "))==6,"The six requested menu actions exist.");
                Check(!Find("Menú · CONTINUAR").interactable,"Continue is disabled without an existing guard.");
                Check(Find("Menú · INICIAR TURNO").navigation.selectOnDown==Find("Menú · CASOS"),"Keyboard navigation skips unavailable Continue.");
                if(game.LiveMode)game.ToggleMode();
                Capture("lobby-main.png");Capture("lobby-720p.png",1280,720);Capture("lobby-4x3.png",1440,1080);
                Click("Menú · CASOS");Capture("lobby-cases.png");
                Check(UnityEngine.Object.FindObjectsByType<Button>().Count(b=>b.name.StartsWith("Jugar caso "))==8,"All authored cases can be selected.");
                Click("Menú · CONFIGURACIÓN");
                var volume=UnityEngine.Object.FindObjectsByType<Slider>().First(s=>s.name=="Volumen");volume.value=.35f;
                Check(Mathf.Abs(AudioListener.volume-.35f)<.001f,"Volume slider controls the actual audio listener.");
                Canvas.ForceUpdateCanvases();Check(Mathf.Abs(volume.fillRect.rect.width-778*.35f)<2,"The volume fill stays within its track.");
                var profile=UnityEngine.Object.FindObjectsByType<InputField>().First(f=>f.name=="Nombre del operador");profile.text="OPERADORA 01";
                Click("GUARDAR CAMBIOS");Check(DispatchPreferences.OperatorName!="OPERADORA 01","Batch validation preserves the player's preferences.");Capture("lobby-settings.png");
                Click("Menú · CRÉDITOS");Capture("lobby-credits.png");
                Click("Menú · SALIR");Check(Find("SALIR DEL JUEGO").interactable,"Quitting has an explicit confirmation.");Click("VOLVER");
                Click("Menú · INICIAR TURNO");Check(!game.View.LobbyVisible&&game.Phase==CallPhase.Idle&&game.View.MapBoardVisible,"Starting opens incoming-call selection.");
                var session=game.Session;game.View.ShowLobby();Check(Find("Menú · CONTINUAR").interactable,"Continue enables for the in-memory guard.");
                Click("Menú · CONTINUAR");Check(ReferenceEquals(session,game.Session)&&!game.View.LobbyVisible,"Continuing preserves the same call and its state.");
                game.View.ShowLobby();Click("Menú · CASOS");Click("Jugar caso 2");
                Check(ReferenceEquals(session,game.Session),"Selecting another case waits for replacement confirmation.");
                Click("INICIAR GUARDIA");Check(game.CaseNumber==2&&game.Phase==CallPhase.Incoming&&!game.View.LobbyVisible,"Case selection starts the chosen authored scenario.");
                game.Answer();game.View.ShowLobby();Check(game.View.LobbyVisible,"A quiet active call can return to the menu.");
                Click("Menú · CONFIGURACIÓN");Check(!Find("VOZ · GEMINI LIVE").interactable&&!Find("ENSAYO · TEXTO").interactable,"The conversation mode cannot change midway through a call.");
                Click("Menú · CONTINUAR");game.FinishCall();
                Check(game.View.ResultVisible,"The result screen still works after returning from the menu.");
                Click("MENÚ PRINCIPAL");Check(game.View.LobbyVisible,"Results link back to the lobby.");
                CheckSaves(game);
                File.WriteAllText(Path.Combine(output,"lobby-validation.txt"),checks+" assertions passed. Main menu, six actions, eight cases, settings, continue, confirmation, result navigation and save parsing verified. Six screenshots at 1080p, 720p and 4:3. No live API, microphone or player-save writes.");
                Finish(0);
            }
            catch(Exception ex) { File.WriteAllText(Path.Combine(output,"lobby-validation.txt"),ex.ToString());Debug.LogException(ex);Finish(1); }
        }
        static void CheckSaves(GameManager game)
        {
            var cases=ScenarioDatabase.GetDefaultCases();var session=new CallSession(cases[0]);session.Answer();var result=session.Finish();
            var save=new GuardSave{version=2,date=DateTime.UtcNow.ToString("o"),nextCaseIndex=1,liveMode=false,calls=new List<CallResult>{result},units=ScenarioDatabase.GetDefaultUnits()};
            Check(GuardSave.TryParse(JsonUtility.ToJson(save),cases,out var loaded)&&loaded.nextCaseIndex==1&&loaded.calls[0].Total==0,"A checkpoint preserves the next case and completed scores.");
            game.NextCase();game.Queue.Advance(120);
            foreach(var c in game.Cases.Where(c=>!game.IsCompleted(c.id)).ToArray()){game.SelectIncomingCall(c.id);game.Answer();game.FinishCall();game.NextCase();}
            loaded.units[0].status=UnitStatus.EnRoute;loaded.units[0].remainingEtaSeconds=90;loaded.units[0].assignedCaseId=cases[0].id;
            typeof(GameManager).GetProperty("SavedGuard").SetValue(game,loaded);
            game.ContinueGuard();Check(game.CaseNumber==2&&game.Results.Count==1&&game.Phase==CallPhase.Incoming,"Continue restores completed scores and enters the saved next case.");
            Check(game.Fleet.Units[0].status==UnitStatus.EnRoute&&game.Fleet.Units[0].remainingEtaSeconds==90,"Continue restores the fleet from the same checkpoint.");
            string legacy="{\"date\":\"2026-09-28\",\"calls\":["+JsonUtility.ToJson(result)+"]}";
            Check(GuardSave.TryParse(legacy,cases,out var migrated)&&migrated.nextCaseIndex==1,"Existing reports migrate to the next completed-case checkpoint.");
            Check(!GuardSave.TryParse("{}",cases,out _)&&!GuardSave.TryParse("invalid",cases,out _),"Incomplete and damaged saves do not enable Continue.");
            save.nextCaseIndex=cases.Count+1;Check(!GuardSave.TryParse(JsonUtility.ToJson(save),cases,out _),"Unknown checkpoint indices are rejected.");
            save.nextCaseIndex=1;save.calls[0].communication=26;Check(!GuardSave.TryParse(JsonUtility.ToJson(save),cases,out _),"Out-of-range saved scores are rejected.");
        }
        static void Finish(int code) { SessionState.SetBool(Running,false);EditorApplication.update-=Pump;EditorApplication.Exit(code); }
        static void Capture(string name,int width=1920,int height=1080)
        {
            Canvas.ForceUpdateCanvases();var camera=Camera.main;var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.aspect=(float)width/height;
            var hud=UnityEngine.Object.FindObjectsByType<Canvas>().First(c=>c.renderMode==RenderMode.ScreenSpaceCamera);float old=hud.scaleFactor;hud.scaleFactor=Mathf.Min(width/1920f,height/1080f);
            foreach(var text in hud.GetComponentsInChildren<Text>())text.SetAllDirty();Canvas.ForceUpdateCanvases();camera.Render();
            RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;hud.scaleFactor=old;UnityEngine.Object.Destroy(image);target.Release();UnityEngine.Object.Destroy(target);
        }
    }
}
