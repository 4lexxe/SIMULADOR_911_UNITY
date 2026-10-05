using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EmergencyDispatch.Editor
{
    public static class EmergencyDispatcherBuilderEditor
    {
        public const string ScenePath="Assets/Scenes/UltimaLlamada.unity";
        [MenuItem("Tools/Última Llamada/Abrir escena editable")]
        [MenuItem("Tools/Emergency Dispatch/Build Complete Scene")]
        public static void BuildCompleteScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            BuildScene();
        }
        // Kept for compatibility with existing editor commands. Only opens the saved scene.
        public static void BuildScene()
        {
            if(!File.Exists(ScenePath))throw new FileNotFoundException("Restaurá la escena guardada desde la copia del proyecto.",ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
