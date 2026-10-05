from pathlib import Path
import re
base=Path(r'H:\UNITY\SERIUS\Artifacts\SceneMigrationBackup\Scripts')
out=Path(r'H:\UNITY\SERIUS\Artifacts\ValidationProject\Assets\Scripts\Editor\DispatchWorldAuthoring.cs')
s=(base/'DispatchBootstrap.cs').read_text(encoding='utf-8-sig')
def method(name):
    m=re.search(r'^        (?:private|public)[^\n]*\b'+name+r'\(',s,re.M);e=s.index('\n        }',m.end())+len('\n        }');return s[m.start():e]
mat=method('Material').replace('            var result =','''            string path="Assets/Materials/Dispatch/"+name+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;
            var result =''').replace(' return result;',' AssetDatabase.CreateAsset(result,path);return result;')
primitive=method('Primitive').replace('if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);','UnityEngine.Object.DestroyImmediate(collider);')
props=method('BuildProps').replace('Instantiate(microphoneModel, stage)','(GameObject)PrefabUtility.InstantiatePrefab(microphoneModel,stage)')
props=props.replace('mic.name = "Micrófono"','mic.name = "Microphone"')
props=props.replace('                mic.transform.rotation = Quaternion.Euler(0, 25, 0);','''                mic.transform.rotation = Quaternion.Euler(0, 25, 0);
                var collider=mic.AddComponent<BoxCollider>(); collider.size=new Vector3(.2f,.37f,.2f)/mic.transform.localScale.x;
                collider.center=new Vector3(0,.185f,0)/mic.transform.localScale.x;
                PrefabUtility.SaveAsPrefabAssetAndConnect(mic,"Assets/Prefabs/Equipment/Microphone.prefab",InteractionMode.AutomatedAction);''')
props=props.replace('            Primitive("LED · push-to-talk",','            var led=Primitive("LED · push-to-talk",')
props=props.replace('Vector3.one * .012f, micLed);','''Vector3.one * .012f, micLed);
            led.AddComponent<MicrophoneIndicator>().voice=voice;
            led.transform.SetParent(stage.Find("Microphone"),true);''')
props=props.replace('var kb = new GameObject("Teclado")','var kb = new GameObject("Keyboard")').replace('new GameObject("Teléfono")','new GameObject("Telephone")')
monitor=method('BuildMonitor').replace('new[] { "01 · Mapa", "02 · Llamada", "03 · Recursos" }','new[] { "MonitorMap", "MonitorCall", "MonitorResources" }')
monitor=monitor.replace('            var go = new GameObject(','''            group.gameObject.AddComponent<BoxCollider>().center=new Vector3(0,1.26f,0);
            group.GetComponent<BoxCollider>().size=new Vector3(1.275f,.85f,.075f);
            if(index==0)PrefabUtility.SaveAsPrefabAssetAndConnect(group.gameObject,"Assets/Prefabs/Equipment/Monitor.prefab",InteractionMode.AutomatedAction);
            var go = new GameObject(''')
monitor=monitor.replace('"Pantalla", typeof(RectTransform)','new[]{"MapPanel","CallPanel","ResourcesPanel"}[index], typeof(RectTransform)')
# Instantiate the same monitor prefab for the remaining permanent equipment.
start=monitor.index('            var group =');geomend=monitor.index('            var go =')
geo=monitor[start:geomend]
monitor=monitor[:start]+'''            Transform group;
            if(index==0)
            {
'''+geo.replace('            var group =','            group =')+'''
            }
            else
            {
                group=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Equipment/Monitor.prefab"),stage)).transform;
                group.name=index==1?"MonitorCall":"MonitorResources";group.localPosition=new Vector3(x,0,.43f);
            }
'''+monitor[geomend:]
header='''using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace EmergencyDispatch.Editor
{
    // Used only to migrate the old empty scene, never by the player.
    internal sealed class DispatchWorldAuthoring
    {
        private Transform stage;
        private Material black,silver,white,micLed;
        private Shader surfaceShader;
        private GameObject microphoneModel;
        private Material microphoneMaterial;
        private VoiceManager voice;
        private static Transform Group(Transform parent,string name)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        public void Author(Transform root)
        {
            Directory.CreateDirectory("Assets/Materials/Dispatch");Directory.CreateDirectory("Assets/Prefabs/Equipment");Directory.CreateDirectory("Assets/Prefabs/Units");Directory.CreateDirectory("Assets/Prefabs/Emergency");
            surfaceShader=Shader.Find("Universal Render Pipeline/Lit");
            microphoneModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Microphone/Microphone.obj");
            microphoneMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Microphone/Microphone_URP.mat");
            stage=Group(root,"Environment");
            var operatorRoot=Group(root,"Operator");var emergencyRoot=Group(root,"EmergencySystem");var unitsRoot=Group(root,"Units");var uiRoot=Group(root,"UI");var audioRoot=Group(root,"Audio");var systemsRoot=Group(root,"Systems");
            var manager=Group(systemsRoot,"GameManager").gameObject.AddComponent<GameManager>();
            var relay=Group(systemsRoot,"GeminiRelay").gameObject.AddComponent<GeminiEmergencyService>();
            var fleet=unitsRoot.gameObject.AddComponent<UnitFleetManager>();
            var view=uiRoot.gameObject.AddComponent<DispatchView>();
            var map=emergencyRoot.gameObject.AddComponent<IncidentMapManager>();view.mapController=map;
            voice=Group(operatorRoot,"VoiceController").gameObject.AddComponent<VoiceManager>();
            var speaker=voice.GetComponent<AudioSource>();speaker.playOnAwake=false;speaker.loop=true;speaker.spatialBlend=0;
            var high=voice.gameObject.AddComponent<AudioHighPassFilter>();high.cutoffFrequency=450;high.highpassResonanceQ=1.35f;
            var low=voice.gameObject.AddComponent<AudioLowPassFilter>();low.cutoffFrequency=3300;low.lowpassResonanceQ=1.55f;
            voice.gameObject.AddComponent<AudioDistortionFilter>().distortionLevel=.16f;
            var audio=Group(audioRoot,"FeedbackAudio").gameObject.AddComponent<EmergencyAudioManager>();
            audio.GetComponent<AudioSource>().playOnAwake=false;audio.GetComponent<AudioSource>().volume=.24f;
            var defaults=ScenarioDatabase.GetDefaultUnits();fleet.roster=new UnitDefinition[defaults.Count];
            string[] unitNames={"Ambulance","FireTruck","PoliceCar"};
            for(int i=0;i<defaults.Count;i++)
            {
                string path="Assets/Prefabs/Units/"+unitNames[(int)defaults[i].type]+".prefab";
                UnitDefinition definition;
                if(!File.Exists(path))
                {
                    definition=Group(unitsRoot,defaults[i].id).gameObject.AddComponent<UnitDefinition>();definition.initial=defaults[i];
                    PrefabUtility.SaveAsPrefabAssetAndConnect(definition.gameObject,path,InteractionMode.AutomatedAction);
                }
                else
                {
                    definition=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),unitsRoot)).GetComponent<UnitDefinition>();
                    definition.initial=defaults[i];definition.name=defaults[i].id;
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(definition);fleet.roster[i]=definition;
            }
            manager.Configure(view,fleet,relay,voice);
            var cameraObject=Group(operatorRoot,"Main Camera").gameObject;cameraObject.tag="MainCamera";
            var camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();
            var controller=cameraObject.AddComponent<DeskCameraController>();
            var poses=Group(operatorRoot,"Viewpoints");controller.viewpoints=new Transform[4];
            for(int i=0;i<4;i++)
            {
                var pose=Group(poses,new[]{"DeskOverview","Map","Call","Resources"}[i]);
                pose.position=i==0?new Vector3(0,1.52f,-1.90f):new Vector3(i==1?-1.28f:i==3?1.28f:0,1.27f,-.66f);
                pose.rotation=i==0?Quaternion.Euler(5,0,0):Quaternion.identity;controller.viewpoints[i]=pose;
            }
            camera.transform.SetPositionAndRotation(controller.viewpoints[0].position,controller.viewpoints[0].rotation);camera.fieldOfView=54;
            camera.nearClipPlane=.04f;camera.farClipPlane=60;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.77f,.84f,.91f);camera.allowHDR=false;camera.allowMSAA=true;
            var lighting=Group(stage,"Lighting");var sun=Group(lighting,"WindowLight");sun.rotation=Quaternion.Euler(38,-30,0);
            var light=sun.gameObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.73f,.78f,.84f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.fog=false;
            black=Material("EquipmentGraphite",new Color(.035f,.045f,.058f),.34f);silver=Material("Aluminium",new Color(.24f,.28f,.32f),.7f);white=Material("Desk",new Color(.82f,.85f,.87f),.3f);
            var backdrop=Primitive("CentralBackdrop",PrimitiveType.Quad,new Vector3(0,.45f,6),new Vector3(18.5f,10.41f,1),white);
            var backdropMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="CentralBackdrop"};
            backdropMaterial.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Sprites/CentralBackdrop.png"));AssetDatabase.CreateAsset(backdropMaterial,"Assets/Materials/Dispatch/CentralBackdrop.mat");backdrop.GetComponent<Renderer>().sharedMaterial=backdropMaterial;
            var desk=Primitive("Desk",PrimitiveType.Cube,new Vector3(0,.71f,-.04f),new Vector3(4.4f,.07f,1.9f),white);desk.AddComponent<BoxCollider>();
            BuildProps();
            var screens=new RectTransform[3];for(int i=0;i<3;i++)screens[i]=BuildMonitor(i,camera);
            var eventSystem=Group(uiRoot,"EventSystem").gameObject;eventSystem.AddComponent<EventSystem>();eventSystem.AddComponent<InputSystemUIInputModule>();
            var assets=new DispatchAuthoringAssets {cityMap=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Sprites/CityMap.png"),emergencyPin=SpriteAt("Icon_PinEmergency"),radarRing=SpriteAt("RadarRing"),avatar=SpriteAt("Icon_AvatarCaller"),ambulance=SpriteAt("Icon_Ambulance"),fire=SpriteAt("Icon_FireTruck"),police=SpriteAt("Icon_PoliceCar")};
            view.AuthorInterface(assets,manager,camera,screens);
            PrefabUtility.SaveAsPrefabAssetAndConnect(map.Pin.gameObject,"Assets/Prefabs/Emergency/EmergencyPin.prefab",InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(map.Rings.gameObject,"Assets/Prefabs/Emergency/RadarRing.prefab",InteractionMode.AutomatedAction);
            // Keep equipment groups easy to select without exposing every key at the root.
            var headset=Group(stage,"Headset");foreach(Transform child in stage.GetComponentsInChildren<Transform>().Clone() as Transform[])
                if(child.parent==stage&&(child.name=="Banda auriculares"||child.name=="Almohadilla"))child.SetParent(headset,true);
            foreach(var name in new[]{"Keyboard","Telephone","Headset"})PrefabUtility.SaveAsPrefabAssetAndConnect(stage.Find(name).gameObject,"Assets/Prefabs/Equipment/"+name+".prefab",InteractionMode.AutomatedAction);
        }
        private static Sprite SpriteAt(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/"+name+".png");
'''
out.write_text(header+'\n\n'.join([mat,primitive,monitor,props,method('FitModel')])+'\n    }\n}\n',encoding='utf-8')
