using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EmergencyDispatch
{
    public class EmergencySceneSetup : MonoBehaviour
    {
        [ContextMenu("Construir Escenario Completo")]
        public void BuildSceneFromInspector()
        {
#if UNITY_EDITOR
            EditorApplication.ExecuteMenuItem("Tools/Emergency Dispatch/Build Complete Scene");
#endif
        }
    }
}
