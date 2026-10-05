from pathlib import Path
import re

root = Path(r'H:\UNITY\SERIUS\Artifacts\ValidationProject')
scripts = root / 'Assets/Scripts'
backup = Path(r'H:\UNITY\SERIUS\Artifacts\SceneMigrationBackup\Scripts')
def read(name): return (backup/name).read_text(encoding='utf-8-sig')
def write(name, text):
    p=scripts/name; p.parent.mkdir(parents=True, exist_ok=True); p.write_text(text, encoding='utf-8')
def method(text, name):
    m=re.search(r'^        (?:private|public|internal)[^\n]*\b'+re.escape(name)+r'\(', text, re.M)
    if not m: raise ValueError(name)
    start=m.start()
    if '{' in m.group() and '}' in m.group(): return m.group()
    end=text.index('\n        }', m.end())+len('\n        }')
    return text[start:end]
def replace(text, name, value): return text.replace(method(text,name),value)

view=read('DispatchView.cs'); lobby=read('DispatchLobbyView.cs'); result=read('DispatchResultView.cs'); overlay=read('DispatchOverlayView.cs')
# Scene construction is editor-only. Runtime code holds serialized references and changes data/state.
author_methods=[]
for name in ['Rect','Box','GlassCard','Label','EnsureSprites','CreateRingSprite','CreateCircleSprite','CreateRoundedBoxSprite','RoundedBox','RoundedCard','RoundedButton','Button','Icon','Badge','Header','BuildMap','AddMapPoi','BuildCall','BuildHistoryTab','BuildProtocolsTab','RenderProtocolBadgeHeader','RenderProtocolSection','BuildFleet']:
    src=method(view,name);author_methods.append(src);view=view.replace(src,'')
for name in ['Angular','Glyph','Stripes','ScrollCopy']:
    author_methods.append(method(result,name))
author_methods.append(method(overlay,'BuildOverlay'))
for name in ['BuildLobby','DrawMenuFrame','BuildOperatorPanel','BuildLobbyCases','BuildLobbySettings','BuildLobbyCredits']:
    src=method(lobby,name);author_methods.append(src);lobby=lobby.replace(src,'')
protocol=method(view,'RefreshProtocolsTab')
protocol=protocol[protocol.index('            if (protocolCategory == 0)'):]
author_methods.append('        private void AuthorProtocolPage()\n        {\n'+protocol)

build=method(view,'Build').replace('public void Build(DispatchBootstrap source, GameManager manager, Camera camera, RectTransform[] screens)', 'public void AuthorInterface(DispatchAuthoringAssets source, GameManager manager, Camera camera, RectTransform[] screens)')
build=build.replace('            BuildLobby();','            BuildLobby();\n            AuthorExtraPanels();\n            AuthorButtonBindings();')
author_methods.insert(0,build)
view=replace(view,'Build','''        public void Bind(GameManager manager)
        {
            Instance = this; game = manager;
            input.onSubmit.AddListener(OnTextSubmit);
            settingsName.onValueChanged.AddListener(OnNameChanged);
            settingsVolume.onValueChanged.AddListener(OnVolumeChanged);
            settingsVolume.SetValueWithoutNotify(AudioListener.volume);
            CloseResult(); CloseModal();
            foreach (var row in fleetRows) row.Bind(manager);
        }
        private void OnTextSubmit(string _) => SendInput();
        private void OnNameChanged(string value) => operatorDraft = value;
        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value; volumeValue.text = Mathf.RoundToInt(value * 100) + "%";
        }''')
# Serialized objects are visible and replaceable in the Inspector.
fields=['private Font font;', 'private GameObject idleBody, activeBody, historyBody, protocolsBody;', 'private Button tabCallBtn, tabHistoryBtn, tabProtocolsBtn;', 'private Image tabCallImg, tabHistoryImg, tabProtocolsImg;', 'private Text tabCallText, historyKpiText;', 'private RectTransform historyScrollContent, protocolScrollContent;', 'private Button[] protocolCategoryBtns = new Button[4];', 'private Image[] protocolCategoryImgs = new Image[4];', 'private Text status, timer, caller, dialogue, operatorLine, micStatus, notice, factCount, idleTitle, idleCopy;', 'private Text clockText, dateText;', 'private Text operatorStatusText, voiceStatusText;', 'private Text[] factValues;', 'private Button mainButton, modeButton, endButton, textToggle, sendButton, offlineButton;', 'private Text mainButtonLabel;', 'private InputField input;', 'private IncidentMapManager map;', 'private Image callLed;', 'private WaveformVisualizer wave;', 'private RectTransform promptDock;']
for field in fields: view=view.replace(field,'[SerializeField] '+field)
view=view.replace('        private DispatchBootstrap assets;', '')
view=view.replace('private RectTransform overlay, center, fleetList, modal, resultPanel;', '[SerializeField] private RectTransform overlay, center, fleetList;\n        private RectTransform modal, resultPanel;')
view=view.replace('private readonly List<Button> filterButtons = new List<Button>();','[SerializeField] private List<Button> filterButtons = new List<Button>();')
view=view.replace('        private readonly List<Action> fleetTicks = new List<Action>();','')
view=view.replace('private static Sprite ringSprite;','') .replace('private static Sprite roundedBoxSprite;','').replace('private static Sprite circleSprite;','')
view=view.replace('if (Time.unscaledTime > nextFleetTick) { foreach (var update in fleetTicks) update(); nextFleetTick = Time.unscaledTime + .5f; }','if (Time.unscaledTime > nextFleetTick) { RefreshFleet(); nextFleetTick = Time.unscaledTime + .5f; }')
view=re.sub(r'Destroy\(resultPanel.gameObject\);\s*resultPanel = null;', 'CloseResult();', view)
view=replace(view,'RefreshFleet','''        public void RefreshFleet()
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
        }''')
view=replace(view,'RefreshProtocolsTab','''        private void RefreshProtocolsTab()
        {
            for (int i=0;i<protocolPages.Length;i++)
            {
                protocolPages[i].gameObject.SetActive(i==protocolCategory);
                protocolCategoryImgs[i].color=i==protocolCategory ? new Color(.10f,.34f,.56f) : PanelHeader;
            }
            protocolScrollContent.GetComponentInParent<ScrollRect>().content=protocolPages[protocolCategory];
        }''')
view=replace(view,'RefreshHistoryTab','''        private void RefreshHistoryTab()
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
        }''')
view=replace(view,'OpenModal','''        private RectTransform OpenModal(string title, string copy)
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
        }''')
for name in ['ShowDispatchConfirmation','ShowEndConfirmation']:
    src=method(view,name)
    lines=src.splitlines()
    # Preserve the original explanatory copy.
    lines=[l for l in lines if 'Button(panel,' not in l]
    action='SetConfirmation("CANCELAR",()=>game.CancelDispatch(),"CONFIRMAR DESPACHO",()=>game.ConfirmDispatch(),new Color(.06f,.45f,.30f));' if name=='ShowDispatchConfirmation' else 'SetConfirmation("CONTINUAR LLAMADA",CloseModal,"FINALIZAR Y EVALUAR",()=>game.FinishCall(),new Color(.55f,.12f,.16f));'
    lines.insert(-1,'            '+action)
    view=view.replace(src,'\n'.join(lines))
view=re.sub(r'        public void CloseModal\(\)[^\n]*', '        public void CloseModal() { if (confirmationRoot != null) confirmationRoot.gameObject.SetActive(false); modal = null; }',view)
view=replace(view,'OnDestroy','''        private void OnDestroy()
        {
            if(input!=null) input.onSubmit.RemoveListener(OnTextSubmit);
            if(settingsName!=null) settingsName.onValueChanged.RemoveListener(OnNameChanged);
            if(settingsVolume!=null) settingsVolume.onValueChanged.RemoveListener(OnVolumeChanged);
            if (Instance == this) Instance = null;
        }''')
write('DispatchView.cs',re.sub(r'\n\s*\n\s*\n','\n\n',view))

for field in ['private RectTransform lobby, lobbyContent, lobbyPage;','private Text lobbyOperator, lobbyConnection, lobbyState, lobbyContinueHint, operatorIdHud, continueCaption;','private Button lobbyContinue, lobbyMenuButton;']:
    lobby=lobby.replace(field,'[SerializeField] '+field)
lobby=lobby.replace('private readonly List<Button> lobbyEntries = new List<Button>();','[SerializeField] private List<Button> lobbyEntries = new List<Button>();')
lobby=replace(lobby,'RequestLobbyStart','''        private void RequestLobbyStart(int caseIndex)
        {
            if(!game.CanContinueGuard) { game.StartAtCase(caseIndex); return; }
            OpenModal("INICIAR UNA NUEVA GUARDIA","Se reemplazará el progreso de la guardia actual.\\nLa nueva guardia comienza en el caso "+(caseIndex+1).ToString("00")+".");
            SetConfirmation("VOLVER",CloseModal,"INICIAR GUARDIA",()=>game.StartAtCase(caseIndex),new Color(.025f,.28f,.15f));
        }''')
lobby=replace(lobby,'ShowLobbyExit','''        private void ShowLobbyExit()
        {
            OpenModal("SALIR DE ÚLTIMA LLAMADA","Los casos completados quedan guardados.\\nSi hay una llamada en curso, se reinicia al continuar.");
            SetConfirmation("VOLVER",CloseModal,"SALIR DEL JUEGO",()=> {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            },new Color(.40f,.06f,.09f));
        }''')
lobby=replace(lobby,'ShowLobbyPage','''        private void ShowLobbyPage(int page)
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
        }''')
write('DispatchLobbyView.cs', re.sub(r'\n\s*\n\s*\n','\n\n',lobby))
# Old overlay file becomes an editor-only authored layout in the partial file.
write('DispatchOverlayView.cs','// The saved HUD is bound in DispatchView. Its migration layout lives in Authoring/DispatchView.Authoring.cs.\n')

author='\n\n'.join(author_methods)
# Static lambdas are not serialized. DispatchButton stores each action in the saved scene.
author=author.replace('            button.onClick.AddListener(() => { EmergencyAudioManager.Instance?.PlayButtonClick(); action?.Invoke(); });','')
author=author.replace('            input.onSubmit.AddListener(_=>SendInput());','')
author=author.replace('            nameInput.onValueChanged.AddListener(v=>operatorDraft=v);','            settingsName=nameInput;')
author=author.replace('            slider.onValueChanged.AddListener(v=>{AudioListener.volume=v;value.text=Mathf.RoundToInt(v*100)+"%";});','            settingsVolume=slider; volumeValue=value;')
author=author.replace('            live.interactable=local.interactable=canChange;','            live.interactable=local.interactable=canChange; liveSetting=live; localSetting=local;')
author=author.replace('            var saved=Label(', '            var saved=settingsSaved=Label(')
author=author.replace('wave.SetSpeaking(false);','wave.AuthorBars();wave.SetSpeaking(false);')
author=author.replace('            map = screen.gameObject.AddComponent<IncidentMapManager>(); map.Content = content; map.Pin = pin; map.Rings = rings;', '            map = mapController; map.Content = content; map.Pin = pin; map.Rings = rings;\n            pin.name="EmergencyPin"; rings.name="RadarRing"; pin.gameObject.SetActive(false); rings.gameObject.SetActive(false);')
# Always author scrollbars so a later longer text remains scrollable.
author=author.replace('if(label.preferredHeight>h)','if(true)')
author=author.replace('                scroll.verticalScrollbar=bar;','                scroll.verticalScrollbar=bar; scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;')
header='''#if UNITY_EDITOR
// One-time migration layouts. Never compiled into the player or called by Awake/Start.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace EmergencyDispatch
{
    public sealed class DispatchAuthoringAssets
    {
        public Texture2D cityMap;
        public Sprite emergencyPin, radarRing, avatar, ambulance, fire, police;
    }
    public partial class DispatchView
    {
        private DispatchAuthoringAssets assets;
        private static Sprite ringSprite, roundedBoxSprite, circleSprite;
        public IncidentMapManager mapController;
'''
write('Authoring/DispatchView.Authoring.cs',header+author+'\n    }\n}\n#endif\n')
