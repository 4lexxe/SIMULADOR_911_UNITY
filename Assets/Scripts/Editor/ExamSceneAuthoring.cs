using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EmergencyDispatch.Editor
{
    public static class ExamSceneAuthoring
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(EmergencyDispatcherBuilderEditor.ScenePath);
            var view = Object.FindAnyObjectByType<DispatchView>();
            var data = new SerializedObject(view);
            var font = (Font)data.FindProperty("font").objectReferenceValue;
            var canvas = view.transform.Find("OperatorCanvas");
            var cases = canvas.GetComponentsInChildren<Transform>(true).First(t => t.name == "CasesPanel");
            if (cases.Find("Examen de 180 segundos") == null)
            {
                var rect = Rect(cases, "Examen de 180 segundos", 496, 18, 240, 44);
                var shape = rect.gameObject.AddComponent<HudShape>();
                shape.color = new Color(.02f,.15f,.11f); shape.edge = DispatchView.Green;
                var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = shape;
                var caption = Label(rect, font, "EXAMEN / 180 S", 20); caption.color = DispatchView.Green;
                var binding = rect.gameObject.AddComponent<DispatchButton>(); binding.receiver = view; binding.action = DispatchAction.StartExam;
                rect.gameObject.AddComponent<HudButtonFeedback>();
            }
            if (canvas.GetComponent<ExamHud>() == null)
            {
                var panel = Rect(canvas, "Reloj del examen", 0, 12, 440, 46);
                panel.anchorMin = panel.anchorMax = new Vector2(.5f,1); panel.pivot = new Vector2(.5f,1);
                panel.anchoredPosition = new Vector2(0,-12);
                var shape = panel.gameObject.AddComponent<HudShape>(); shape.color = DispatchView.Navy; shape.edge = DispatchView.CyanAccent; shape.raycastTarget = false;
                var hud = canvas.gameObject.AddComponent<ExamHud>(); hud.game = Object.FindAnyObjectByType<GameManager>(); hud.panel = panel;
                hud.label = Label(panel,font,"EXAMEN  03:00 / 03:00",25);
                panel.gameObject.SetActive(false);
                var menu = (Button)data.FindProperty("lobbyMenuButton").objectReferenceValue;
                var menuRect = (RectTransform)menu.transform;
                menuRect.anchoredPosition = new Vector2(350,menuRect.anchoredPosition.y);
            }
            var menuControl = (Button)data.FindProperty("lobbyMenuButton").objectReferenceValue;
            var menuPosition = (RectTransform)menuControl.transform;
            menuPosition.anchoredPosition = new Vector2(350,menuPosition.anchoredPosition.y);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        static Text Label(RectTransform parent,Font font,string copy,int size)
        {
            var rect=Rect(parent,"Texto",8,0,parent.sizeDelta.x-16,parent.sizeDelta.y);
            var text=rect.gameObject.AddComponent<Text>();text.font=font;text.text=copy;text.fontSize=size;
            text.alignment=TextAnchor.MiddleCenter;text.color=DispatchView.CyanAccent;text.raycastTarget=false;return text;
        }
    }
}
