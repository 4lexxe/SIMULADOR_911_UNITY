using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace EmergencyDispatch.Editor
{
    [InitializeOnLoad]
    public static class SceneArchitectureValidation
    {
        const string Running="UltimaLlamada.ArchitectureValidation";
        static double readyAt;
        static int checks;
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../scene-architecture-validation.txt"));
        static SceneArchitectureValidation(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Pump;}
        static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
        public static void Run()
        {
            EditorSceneManager.OpenScene(EmergencyDispatcherBuilderEditor.ScenePath);
            try
            {
                var root=GameObject.Find("UltimaLlamada").transform;
                Check(root.childCount==7,"Exactly seven functional root groups exist before Play.");
                foreach(var name in new[]{"Environment","Operator","EmergencySystem","Units","UI","Audio","Systems"})Check(root.Find(name)!=null,"Saved group: "+name);
                var objects=root.GetComponentsInChildren<Transform>(true);
                Check(objects.Length>500,"The saved scene contains its equipment and interface, not an empty bootstrap.");
                foreach(var t in objects)Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing script: "+t.name);
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var mat in renderer.sharedMaterials)
                    Check(mat!=null&&AssetDatabase.Contains(mat),"Renderer material is a persistent asset: "+renderer.name);
                foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.sprite!=null)Check(AssetDatabase.Contains(image.sprite),"UI sprite survives serialization: "+image.name);
                Check(root.GetComponentsInChildren<Camera>(true).Length==1&&root.GetComponentsInChildren<AudioListener>(true).Length==1,"One camera and listener.");
                Check(root.GetComponentsInChildren<Canvas>(true).Length==4,"Three world canvases and one operator canvas are saved.");
                Check(root.GetComponentsInChildren<UnitDefinition>(true).Length==5,"Five editable permanent unit definitions.");
                foreach(var path in new[]{"Environment/MonitorMap","Environment/MonitorCall","Environment/MonitorResources","Environment/Microphone","Units/AMB-04","Units/AMB-05","Units/BOM-02","Units/PAT-03","Units/PAT-07"})
                    Check(PrefabUtility.IsPartOfPrefabInstance(root.Find(path)),"Reusable object is a prefab instance: "+path);
                Check(root.GetComponentsInChildren<MonoBehaviour>(true).All(c=>c.GetType().Name!="DispatchBootstrap"),"No DispatchBootstrap component remains.");
                var voice=root.GetComponentInChildren<VoiceManager>(true);var audio=root.GetComponentInChildren<EmergencyAudioManager>(true);
                Check(voice.gameObject!=audio.gameObject,"Voice filters cannot affect feedback audio.");
                Check(voice.GetComponent<AudioHighPassFilter>()!=null&&voice.GetComponent<AudioLowPassFilter>()!=null&&voice.GetComponent<AudioDistortionFilter>()!=null,"Telephone filters exist before Play.");
                // Deliberate unsaved Inspector edits verify that initialization respects scene state.
                root.Find("Environment/MonitorMap").localPosition+=new Vector3(.031f,.02f,0);
                root.Find("Environment/Microphone").localScale*=1.1f;
                var fleet=root.GetComponentInChildren<UnitFleetManager>();fleet.roster[0].initial.baseEtaMinutes=7;
                SessionState.SetFloat("Architecture.MonitorX",root.Find("Environment/MonitorMap").localPosition.x);
                SessionState.SetFloat("Architecture.MicScale",root.Find("Environment/Microphone").localScale.x);
                SessionState.SetInt("Architecture.EnvironmentCount",root.Find("Environment").GetComponentsInChildren<Transform>(true).Length);
                SessionState.SetInt("Architecture.EditChecks",checks);
                SessionState.SetBool(Running,true);EditorApplication.update-=Pump;EditorApplication.update+=Pump;
                EditorApplication.isPlaying=true;readyAt=EditorApplication.timeSinceStartup+2;
            }
            catch(Exception ex){Fail(ex);}
        }
        static void Pump()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<readyAt)return;
            var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();if(game?.View==null)return;
            try
            {
                checks=SessionState.GetInt("Architecture.EditChecks",0);
                var root=game.transform.root;
                Check(root.Find("Environment").GetComponentsInChildren<Transform>(true).Length==SessionState.GetInt("Architecture.EnvironmentCount",-1),"Play does not construct environment objects.");
                Check(Mathf.Approximately(root.Find("Environment/MonitorMap").localPosition.x,SessionState.GetFloat("Architecture.MonitorX",0)),"Inspector monitor movement survives Play.");
                Check(Mathf.Approximately(root.Find("Environment/Microphone").localScale.x,SessionState.GetFloat("Architecture.MicScale",0)),"Inspector microphone scale survives Play.");
                Check(game.Fleet.Units[0].baseEtaMinutes==7,"Editable unit ETA is used by the actual gameplay state.");
                game.Fleet.Units[0].baseEtaMinutes=8;Check(game.SceneFleet.roster[0].initial.baseEtaMinutes==7,"Runtime state does not mutate unit definitions.");
                if(game.LiveMode)game.ToggleMode();game.StartShift();game.SelectIncomingCall("CASE_001");game.Answer();
                var savedPanels=root.GetComponentsInChildren<Canvas>(true).Select(c=>c.GetEntityId()).ToArray();
                var rows=root.GetComponentsInChildren<FleetRowView>(true).Select(r=>r.GetEntityId()).ToArray();
                for(int i=0;i<4;i++){game.Fleet.SetFilter(i==0?(UnitType?)null:(UnitType)(i-1));game.View.RefreshFleet();}
                game.Fleet.SetFilter(null);Check(rows.SequenceEqual(root.GetComponentsInChildren<FleetRowView>(true).Select(r=>r.GetEntityId())),"Filtering reuses all saved fleet rows.");
                var protocolIds=root.GetComponentsInChildren<ScrollRect>(true).Select(r=>r.GetEntityId()).ToArray();
                game.View.SetCenterTab(2);for(int i=0;i<4;i++)game.View.Execute(DispatchAction.Protocol,i);
                Check(protocolIds.SequenceEqual(root.GetComponentsInChildren<ScrollRect>(true).Select(r=>r.GetEntityId())),"Switching protocols reuses authored content.");
                game.View.SetCenterTab(0);game.SubmitText("¿En qué dirección están, en San Martín 240?");
                Check(root.GetComponentInChildren<IncidentMapManager>(true).Pin.gameObject.activeSelf,"Saved emergency pin activates when the location is known.");
                var dispatchRow=root.GetComponentsInChildren<FleetRowView>(true).First(r=>r.definition.initial.id==game.Fleet.Units[0].id);
                Check(dispatchRow.assign.interactable,"The saved fleet button becomes available after locating the incident.");
                dispatchRow.assign.onClick.Invoke();Check(game.Phase==CallPhase.Decision,"The prefab button selects the correct dispatch resource.");
                UnityEngine.Object.FindObjectsByType<Button>().First(b=>b.name=="CONFIRMAR DESPACHO").onClick.Invoke();
                Check(game.Session.DispatchedUnit==game.Fleet.Units[0],"Saved confirmation dispatches the same resource.");
                game.FinishCall();var resultObject=root.GetComponentsInChildren<CanvasGroup>(true).First(c=>c.name=="Auditoría de despacho");var resultId=resultObject.GetEntityId();
                game.View.Execute(DispatchAction.ResultTab,1);game.View.Execute(DispatchAction.ResultTab,0);
                Check(resultObject.GetEntityId()==resultId&&savedPanels.SequenceEqual(root.GetComponentsInChildren<Canvas>(true).Select(c=>c.GetEntityId())),"Score/report tabs reuse the same saved panels and canvases.");
                game.NextCase();game.View.IncomingBoard.Hide();game.View.SetCenterTab(1);
                var history=UnityEngine.Object.FindObjectsByType<HistoryRowView>();
                Check(history.Length==1,"History creates one complete prefab for one completed call.");
                history[0].open.onClick.Invoke();Check(game.View.ResultVisible,"Dynamic history prefab opens its saved audit.");
                game.View.Execute(DispatchAction.BackHistory,0);Check(!game.View.ResultVisible,"Returning from history reuses and hides the audit panel.");
                File.WriteAllText(Output,checks+" checks passed. Saved hierarchy, materials, sprites, prefab instances, Inspector edits, separate audio sources, authored controls, reusable panels and dynamic history verified.");
                Finish(0);
            }
            catch(Exception ex){Fail(ex);}
        }
        static void Fail(Exception ex){File.WriteAllText(Output,ex.ToString());Debug.LogException(ex);Finish(1);}
        static void Finish(int code){SessionState.SetBool(Running,false);EditorApplication.update-=Pump;EditorApplication.Exit(code);}
    }
}
