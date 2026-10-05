using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch.Editor
{
    // Explicit one-time authoring command. No runtime construction or automatic rebuild.
    public static class JujuySceneAuthoring
    {
        static Font font;
        static Sprite disc, ring;
        static readonly Color cyan = DispatchView.CyanAccent;
        static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;
        }
        static Text Text(Transform parent,string name,string value,float x,float y,float width,float height,int size,Color color)
        {
            var r=Rect(parent,name,x,y,width,height);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;
            t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        static HudShape Shape(Transform parent,string name,float x,float y,float w,float h,Color fill,Color stroke)
        { var r=Rect(parent,name,x,y,w,h);var s=r.gameObject.AddComponent<HudShape>();s.color=fill;s.edge=stroke;s.cut=12;return s; }
        static Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,Color tone)
        {
            var s=Shape(parent,name,x,y,w,h,new Color(.025f,.12f,.18f),tone);var b=s.gameObject.AddComponent<Button>();b.targetGraphic=s;
            var colors=b.colors;colors.highlightedColor=new Color(.68f,.92f,1);colors.pressedColor=new Color(.45f,.75f,.9f);colors.disabledColor=new Color(.45f,.5f,.55f,.7f);b.colors=colors;
            Text(b.transform,"Rótulo",caption,12,0,w-24,h,21,tone).alignment=TextAnchor.MiddleCenter;
            b.gameObject.AddComponent<HudButtonFeedback>();return b;
        }
        static void Wire(Button button,DispatchView view,DispatchAction action,int argument=0)
        { var binding=button.gameObject.AddComponent<DispatchButton>();binding.receiver=view;binding.action=action;binding.argument=argument; }
        static IncomingCallMarker MarkerPrefab()
        {
            var root=Rect(null,"Señal de llamada",0,0,48,48);root.pivot=new Vector2(.5f,.5f);
            var marker=root.gameObject.AddComponent<IncomingCallMarker>();
            var target=root.gameObject.AddComponent<Image>();target.color=new Color(1,1,1,0);var button=root.GetComponent<Button>();button.targetGraphic=target;button.transition=Selectable.Transition.None;
            var pulse=Rect(root,"Pulso de señal",0,0,48,48);pulse.anchorMin=pulse.anchorMax=pulse.pivot=new Vector2(.5f,.5f);pulse.anchoredPosition=Vector2.zero;marker.pulse=pulse;
            marker.halo=pulse.gameObject.AddComponent<Image>();marker.halo.sprite=ring;marker.halo.color=DispatchView.Red;marker.halo.raycastTarget=false;
            var dot=Rect(root,"Punto",9,9,30,30);marker.dot=dot.gameObject.AddComponent<Image>();marker.dot.sprite=disc;marker.dot.color=DispatchView.Red;marker.dot.raycastTarget=false;
            marker.number=Text(root,"Número","01",0,0,48,48,17,Color.white);marker.number.alignment=TextAnchor.MiddleCenter;marker.number.fontStyle=FontStyle.Bold;
            marker.locality=Text(root,"Localidad","Localidad",52,11,235,30,18,Color.white);
            var shadow=marker.locality.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.9f);shadow.effectDistance=new Vector2(1,-2);
            var saved=PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/IncomingCallMarker.prefab").GetComponent<IncomingCallMarker>();UnityEngine.Object.DestroyImmediate(root.gameObject);return saved;
        }
        static IncomingCallRow RowPrefab()
        {
            var bg=Shape(null,"Llamada en espera",0,0,516,40,new Color(.022f,.055f,.085f),cyan);var b=bg.gameObject.AddComponent<Button>();b.targetGraphic=bg;b.transition=Selectable.Transition.None;
            var row=bg.gameObject.AddComponent<IncomingCallRow>();row.background=bg;
            row.title=Text(bg.transform,"Localidad","Localidad",14,0,360,40,19,Color.white);
            row.status=Text(bg.transform,"Espera","00:00",382,0,118,40,16,DispatchView.Red);row.status.alignment=TextAnchor.MiddleRight;
            bg.gameObject.AddComponent<HudButtonFeedback>();
            var saved=PrefabUtility.SaveAsPrefabAsset(bg.gameObject,"Assets/Prefabs/IncomingCallRow.prefab").GetComponent<IncomingCallRow>();UnityEngine.Object.DestroyImmediate(bg.gameObject);return saved;
        }
        static IncomingCallMarker PlaceMarker(IncomingCallMarker prefab,RectTransform parent,EmergencyCase c,int number,bool monitor)
        {
            var marker=(IncomingCallMarker)PrefabUtility.InstantiatePrefab(prefab,parent);marker.name="Llamada "+number.ToString("00")+" · "+c.locality;marker.caseId=c.id;marker.openBoardOnSelect=monitor;
            var r=(RectTransform)marker.transform;r.anchorMin=r.anchorMax=new Vector2(c.mapNormalizedCoord.x,c.mapNormalizedCoord.y);r.anchoredPosition=Vector2.zero;
            marker.number.text=number.ToString("00");marker.locality.text=c.locality;
            // Offset labels in the Quebrada and southeastern cluster, leaving the true geographic dots fixed.
            if(number==1||number==7)
            {
                marker.locality.rectTransform.anchoredPosition=new Vector2(-236,-12);marker.locality.alignment=TextAnchor.MiddleRight;
            }
            if(number==2)marker.locality.rectTransform.anchoredPosition=new Vector2(46,-29);
            if(number==8) { marker.locality.text="Libertador Gral. San Martín";marker.locality.rectTransform.sizeDelta=new Vector2(270,30); }
            if(monitor) { r.localScale=Vector3.one*.7f;marker.locality.fontSize=18; }
            PrefabUtility.RecordPrefabInstancePropertyModifications(marker);PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            PrefabUtility.RecordPrefabInstancePropertyModifications(marker.number);PrefabUtility.RecordPrefabInstancePropertyModifications(marker.locality);PrefabUtility.RecordPrefabInstancePropertyModifications(marker.locality.rectTransform);
            return marker;
        }
        public static void Run()
        {
            AssetDatabase.Refresh();EditorSceneManager.OpenScene(EmergencyDispatcherBuilderEditor.ScenePath);
            var view=UnityEngine.Object.FindAnyObjectByType<DispatchView>();var serialized=new SerializedObject(view);
            font=(Font)serialized.FindProperty("font").objectReferenceValue;
            disc=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");ring=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/RadarRing.png");
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Sprites/JujuyMap.png");
            var cases=ScenarioDatabase.GetDefaultCases();
            var markerPrefab=MarkerPrefab();var rowPrefab=RowPrefab();
            var canvas=view.transform.Find("OperatorCanvas");
            var old=canvas.Find("Mapa provincial de Jujuy");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var backdrop=Rect(canvas,"Mapa provincial de Jujuy",0,0,0,0);backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color=new Color(.002f,.012f,.025f,.98f);
            var board=backdrop.gameObject.AddComponent<IncomingCallBoard>();board.panel=backdrop;
            var frame=Shape(backdrop,"Tablero editable",0,0,1760,960,new Color(.014f,.032f,.052f),new Color(.1f,.48f,.64f));
            var fr=(RectTransform)frame.transform;fr.anchorMin=fr.anchorMax=fr.pivot=new Vector2(.5f,.5f);fr.anchoredPosition=Vector2.zero;
            board.frame=fr;
            board.heading=Text(fr,"Título","JUJUY / CENTRAL DE LLAMADAS",34,22,1370,52,38,Color.white);board.heading.fontStyle=FontStyle.Bold;
            Text(fr,"Instrucción","SELECCIONÁ UNA SEÑAL. ESCUCHÁ. DECIDÍ.",36,80,1100,30,20,cyan);
            board.close=Button(fr,"Volver al escritorio","VOLVER AL ESCRITORIO",1440,30,284,50,cyan);
            board.count=Text(fr,"Contador de guardia","00 EN ESPERA / 00 ATENDIDAS",1170,94,554,30,19,DispatchView.Red);
            var viewport=Rect(fr,"Mapa / viewport",30,140,1120,760);viewport.gameObject.AddComponent<Image>().color=new Color(.02f,.06f,.09f);viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect(viewport,"Geografía de Jujuy",0,0,1040,760);content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,.5f);content.anchoredPosition=Vector2.zero;
            var mapImage=content.gameObject.AddComponent<RawImage>();mapImage.texture=texture;mapImage.color=Color.white;
            board.markers=new IncomingCallMarker[cases.Count];board.rows=new IncomingCallRow[cases.Count];
            var queue=Shape(fr,"Bandeja y ficha",1170,140,560,760,new Color(.022f,.055f,.085f),new Color(.08f,.25f,.36f));
            Text(queue.transform,"Título de bandeja","LLAMADAS / TIEMPO EN ESPERA",22,14,516,34,20,cyan);
            for(int i=0;i<cases.Count;i++)
            {
                board.markers[i]=PlaceMarker(markerPrefab,content,cases[i],i+1,false);
                var row=(IncomingCallRow)PrefabUtility.InstantiatePrefab(rowPrefab,queue.transform);row.name="Bandeja "+(i+1).ToString("00")+" · "+cases[i].locality;
                row.caseId=cases[i].id;((RectTransform)row.transform).anchoredPosition=new Vector2(22,-62-i*44);row.title.text=cases[i].locality;board.rows[i]=row;
                PrefabUtility.RecordPrefabInstancePropertyModifications(row);PrefabUtility.RecordPrefabInstancePropertyModifications(row.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(row.title);
            }
            var detail=Shape(queue.transform,"Ficha seleccionada",22,433,516,229,new Color(.026f,.09f,.13f),new Color(.08f,.36f,.45f));
            board.selectedRegion=Text(detail.transform,"Región","VOS DECIDÍS A QUIÉN ATENDER",18,12,480,28,16,cyan);
            board.selectedTown=Text(detail.transform,"Localidad","ELEGÍ UN PUNTO ROJO",18,44,480,65,26,Color.white);board.selectedTown.fontStyle=FontStyle.Bold;
            board.selectedCopy=Text(detail.transform,"Datos previos","La dirección se confirma durante la llamada.",18,111,480,76,19,new Color(.76f,.85f,.91f));
            board.selectedWait=Text(detail.transform,"Estado de señal","SIN LLAMADA SELECCIONADA",18,194,480,24,17,DispatchView.Amber);
            board.answer=Button(queue.transform,"Atender llamada seleccionada","ATENDER LLAMADA",22,684,516,54,DispatchView.Green);board.answerLabel=board.answer.GetComponentInChildren<Text>();
            board.footer=Text(fr,"Leyenda dinámica","ROJO: EN ESPERA    CIAN: SELECCIONADA    VERDE: ATENDIDA",34,907,1680,24,17,cyan);
            Text(fr,"Atribución","Geografía: GeoRef / IGN Argentina · Casos y direcciones de simulación · La señal representa una localidad, no una posición GPS exacta.",34,934,1680,20,14,DispatchView.Muted);
            var navigation=viewport.gameObject.AddComponent<JujuyMapNavigation>();navigation.content=content;navigation.viewport=viewport;
            navigation.zoomIn=Button(viewport,"Acercar","+",16,16,52,52,cyan);navigation.zoomOut=Button(viewport,"Alejar","−",78,16,52,52,cyan);
            navigation.reset=Button(viewport,"Vista provincial","CENTRAR",140,16,146,52,cyan);navigation.zoomLabel=Text(viewport,"Escala","1.00×",300,26,90,30,17,cyan);board.navigation=navigation;
            view.IncomingBoard=board;
            // Reuse the authored monitor and confirmed-location prefabs.
            var map=UnityEngine.Object.FindAnyObjectByType<IncidentMapManager>();
            foreach(Transform child in map.Content.Cast<Transform>().ToArray())if(child!=map.Pin&&child!=map.Rings)UnityEngine.Object.DestroyImmediate(child.gameObject);
            map.Content.sizeDelta=new Vector2(624,456);map.Content.anchorMin=map.Content.anchorMax=map.Content.pivot=new Vector2(.5f,.5f);map.Content.anchoredPosition=Vector2.zero;
            map.Content.GetComponent<RawImage>().texture=texture;map.Content.GetComponent<RawImage>().color=Color.white;
            map.incomingMarkers=new IncomingCallMarker[cases.Count];for(int i=0;i<cases.Count;i++)map.incomingMarkers[i]=PlaceMarker(markerPrefab,map.Content,cases[i],i+1,true);
            map.Pin.SetAsLastSibling();map.Rings.SetAsFirstSibling();map.Pin.gameObject.SetActive(false);map.Rings.gameObject.SetActive(false);
            map.ScaleLabel.text="JUJUY · 1.00×";map.ScaleLabel.fontSize=14;
            map.Caption.text="JUJUY · LLAMADAS EN ESPERA\nAbrí el mapa provincial y elegí una señal.";map.Caption.fontSize=19;
            var monitor=map.Content.parent.parent;
            foreach(var t in monitor.GetComponentsInChildren<Text>(true))if(t.text.Contains("Buscar direcci"))t.text="PROVINCIA DE JUJUY";
            var oldOpen=monitor.Find("Abrir mapa provincial");if(oldOpen!=null)UnityEngine.Object.DestroyImmediate(oldOpen.gameObject);
            var open=Button(monitor,"Abrir mapa provincial","ABRIR MAPA  [M]",710,553,262,62,cyan);Wire(open,view,DispatchAction.OpenMap);
            map.Caption.rectTransform.sizeDelta=new Vector2(654,map.Caption.rectTransform.sizeDelta.y);
            // Main menu opens the queue; authored practice cards cover all eight locations.
            foreach(var binding in view.GetComponentsInChildren<DispatchButton>(true))if(binding.action==DispatchAction.StartGuard&&binding.name.StartsWith("Menú")) {binding.argument=-1;EditorUtility.SetDirty(binding);}
            var casesPanel=canvas.GetComponentsInChildren<Transform>(true).First(t=>t.name=="CasesPanel");
            foreach(Transform child in casesPanel.Cast<Transform>().ToArray())if(child.name.StartsWith("Caso ")||child.GetComponent<Text>()!=null&&child.name=="Texto 02")UnityEngine.Object.DestroyImmediate(child.gameObject);
            for(int i=0;i<cases.Count;i++)
            {
                float x=16+(i%2)*416,y=102+(i/2)*96;
                var card=Shape(casesPanel,"Caso "+(i+1),x,y,402,88,new Color(.023f,.065f,.10f),new Color(.06f,.3f,.4f));
                Text(card.transform,"Localidad",(i+1).ToString("00")+" / "+cases[i].locality,12,3,376,36,17,cyan);
                Text(card.transform,"Situación",cases[i].title,12,41,254,40,15,Color.white);
                var play=Button(card.transform,"Jugar caso "+(i+1),"ELEGIR",280,44,110,34,cyan);Wire(play,view,DispatchAction.StartGuard,i);
            }
            foreach(var b in view.transform.root.GetComponentsInChildren<Button>(true))if(b.GetComponent<HudButtonFeedback>()==null&&b.GetComponent<IncomingCallMarker>()==null)b.gameObject.AddComponent<HudButtonFeedback>();
            backdrop.gameObject.SetActive(false);
            EditorUtility.SetDirty(view);EditorUtility.SetDirty(map);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../jujuy-authoring.txt")),"Authored Jujuy board, map, 16 geographic markers, 8 queue rows, 8 practice cards and button feedback. No runtime UI construction.");
        }
    }
}
