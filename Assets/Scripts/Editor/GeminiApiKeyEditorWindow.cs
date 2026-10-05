using UnityEditor;
using UnityEngine;
namespace EmergencyDispatch.Editor
{
    public class GeminiApiKeyEditorWindow : EditorWindow
    {
        private string backend;
        [MenuItem("Tools/Última Llamada/Configurar conexión de voz")]
        public static void ShowWindow() { GetWindow<GeminiApiKeyEditorWindow>("Conexión de voz"); }
        private void OnEnable() { backend = GeminiConfig.BackendUrl; GeminiConfig.RemoveLegacyCredential(); }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unity → Backend → Gemini Live", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configurá GEMINI_API_KEY en Backend/.env. Unity solo guarda la dirección del relay. El puntaje se calcula en Unity.", MessageType.Info);
            backend = EditorGUILayout.TextField("WebSocket del backend", backend);
            if (GUILayout.Button("Guardar dirección")) GeminiConfig.BackendUrl = backend.Trim();
        }
    }
}
