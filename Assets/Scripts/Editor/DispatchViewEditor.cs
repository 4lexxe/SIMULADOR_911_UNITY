using UnityEditor;
using UnityEngine;
namespace EmergencyDispatch.Editor
{
    [CustomEditor(typeof(DispatchView))]
    public sealed class DispatchViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("La interfaz existe en la escena. Elegí una vista para editarla sin entrar en Play. Los estados iniciales se restablecen al jugar.",MessageType.Info);
            using(new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("Escritorio"))Preview(false,false);
                if(GUILayout.Button("Menú"))Preview(true,false);
                if(GUILayout.Button("Resultados"))Preview(false,true);
                if(GUILayout.Button("Mapa Jujuy")) { Preview(false,false);((DispatchView)target).IncomingBoard.panel.gameObject.SetActive(true); }
                EditorGUILayout.EndHorizontal();
            }
            DrawDefaultInspector();
        }
        private void Preview(bool menu,bool result)
        {
            if(((DispatchView)target).IncomingBoard!=null)((DispatchView)target).IncomingBoard.panel.gameObject.SetActive(false);
            foreach(var key in new[]{"lobby","resultRoot","confirmationRoot"})
            {
                var value=serializedObject.FindProperty(key).objectReferenceValue as Transform;if(value==null)continue;
                Undo.RecordObject(value.gameObject,"Vista de edición");
                value.gameObject.SetActive(key=="lobby"?menu:key=="resultRoot"&&result);
            }
            SceneView.RepaintAll();
        }
    }
}
