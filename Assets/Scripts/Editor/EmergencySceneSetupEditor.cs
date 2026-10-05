using UnityEngine;
using UnityEditor;

namespace EmergencyDispatch.Editor
{
    [CustomEditor(typeof(EmergencySceneSetup))]
    public class EmergencySceneSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            GUI.backgroundColor = new Color(0.2f, 0.65f, 1f);

            if (GUILayout.Button("🚀 GENERAR ESCENARIO 3D COMPLETO", GUILayout.Height(45)))
            {
                EmergencyDispatcherBuilderEditor.BuildCompleteScene();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Haz clic en el botón superior o usa el menú: Tools > Emergency Dispatch > Build Complete Scene", MessageType.Info);
        }
    }
}
