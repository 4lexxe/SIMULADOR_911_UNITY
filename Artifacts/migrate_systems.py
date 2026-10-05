from pathlib import Path
import re
p=Path(r'H:\UNITY\SERIUS\Artifacts\ValidationProject\Assets\Scripts')
def edit(name,old,new):
    file=p/name;s=file.read_text(encoding='utf-8-sig');assert old in s, (name,old[:60]);file.write_text(s.replace(old,new),encoding='utf-8')
edit('GameManager.cs','        private void Awake() { Instance = this; }','''        [Header("Scene systems — assign in the Inspector")]
        [SerializeField] private DispatchView sceneView;
        [SerializeField] private UnitFleetManager sceneFleet;
        [SerializeField] private GeminiEmergencyService sceneRelay;
        [SerializeField] private VoiceManager sceneVoice;
        public UnitFleetManager SceneFleet => sceneFleet;
        public void Configure(DispatchView view, UnitFleetManager fleet, GeminiEmergencyService service, VoiceManager voice)
        { sceneView=view;sceneFleet=fleet;sceneRelay=service;sceneVoice=voice; }
        private void Awake() { Instance = this; }
        private void Start()
        {
            if(sceneView==null||sceneFleet==null||sceneRelay==null||sceneVoice==null)
            { Debug.LogError("Missing saved scene references on GameManager.",this);enabled=false;return; }
            Initialize(sceneView,sceneFleet,sceneRelay,sceneVoice);
        }''')
edit('GameManager.cs','            Fleet.InitializeUnits(ScenarioDatabase.GetDefaultUnits());','            Fleet.ResetUnits();\n            View.Bind(this);')
edit('GameMenu.cs','Fleet.InitializeUnits(ScenarioDatabase.GetDefaultUnits());','Fleet.ResetUnits();')
edit('UnitFleetManager.cs','        public UnitType? Filter { get; private set; }','''        [Header("Permanent dispatch resources")]
        public UnitDefinition[] roster;
        public UnitType? Filter { get; private set; }
        public void ResetUnits()
        {
            var states=new List<EmergencyUnit>();
            foreach(var definition in roster)states.Add(definition.CreateState());
            InitializeUnits(states);
        }''')
edit('EmergencyAudioManager.cs','    public class EmergencyAudioManager', '    [RequireComponent(typeof(AudioSource))]\n    public class EmergencyAudioManager')
edit('EmergencyAudioManager.cs','            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = .24f;','            source = GetComponent<AudioSource>();')
v=(p/'VoiceManager.cs').read_text(encoding='utf-8-sig');start=v.index('            speaker.playOnAwake =');end=v.index('            playback =',start)
v=v[:start]+v[end:];(p/'VoiceManager.cs').write_text(v,encoding='utf-8')
edit('WaveformVisualizer.cs','        private RectTransform[] bars;','        [SerializeField] private RectTransform[] bars;')
edit('WaveformVisualizer.cs','        private Image[] barImages;','        [SerializeField] private Image[] barImages;')
edit('WaveformVisualizer.cs','''        private void Awake()
        {
            CreateBars();
        }

        private void CreateBars()''','''#if UNITY_EDITOR
        public void AuthorBars()''')
edit('WaveformVisualizer.cs','        public void SetSpeaking(bool speaking)','#endif\n        public void SetSpeaking(bool speaking)')
edit('WaveformVisualizer.cs','i < barCount; i++)\n            {\n                float normalizedIndex','i < bars.Length; i++)\n            {\n                float normalizedIndex')
v=(p/'DeskCameraController.cs').read_text(encoding='utf-8-sig')
v=v.replace('        private Camera cameraComponent;','''        [Header("Editable camera poses")]
        public Transform[] viewpoints;
        public float[] fieldOfViews = {54,57,57,57};
        private Camera cameraComponent;''')
start=v.index('            if (mode == CameraViewMode.DeskOverview)');end=v.index('\n        }\n        public void Snap',start)
v=v[:start]+'''            int index=(int)mode;
            if(viewpoints==null||index>=viewpoints.Length||viewpoints[index]==null) return;
            destination=viewpoints[index].position; rotation=viewpoints[index].rotation;
            fov=fieldOfViews[index];'''+v[end:]
(p/'DeskCameraController.cs').write_text(v,encoding='utf-8')
# Old automated checks must load the saved scene, not rebuild it as a side effect.
for name in ['Editor/LobbyValidation.cs','Editor/HudAuditValidation.cs','Editor/DispatchValidation.cs']:
    file=p/name;v=file.read_text(encoding='utf-8-sig');v=v.replace('EmergencyDispatcherBuilderEditor.BuildScene();','')
    v=v.replace('var bootstrap = UnityEngine.Object.FindAnyObjectByType<DispatchBootstrap>();','var sceneGame = UnityEngine.Object.FindAnyObjectByType<GameManager>();').replace('bootstrap?.Game?.View','sceneGame?.View').replace('var game = bootstrap.Game;','var game = sceneGame;')
    file.write_text(v,encoding='utf-8')
# Retire construction on the old component after its complete audit and backup.
file=p/'DispatchBootstrap.cs';file.write_text('''using UnityEngine;
namespace EmergencyDispatch
{
    // Migration placeholder. Removed from the scene by the editor migration; never creates runtime objects.
    [System.Obsolete("Use the authored UltimaLlamada scene.")]
    public sealed class DispatchBootstrap : MonoBehaviour { }
}
''',encoding='utf-8')
