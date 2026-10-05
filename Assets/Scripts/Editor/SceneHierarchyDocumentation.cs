using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EmergencyDispatch.Editor
{
    public static class SceneHierarchyDocumentation
    {
        public static void ExportSavedScene()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(EmergencyDispatcherBuilderEditor.ScenePath);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Export();
        }
        [MenuItem("Tools/Última Llamada/Exportar jerarquía y referencias")]
        public static void Export()
        {
            var text=new StringBuilder("# Jerarquía exacta de Última Llamada\n\nEscena: `Assets/Scenes/UltimaLlamada.unity`. Exportada desde la escena guardada, fuera de Play.\n\nTodos los objetos de esta sección existen permanentemente. Algunos paneles permanecen desactivados hasta usarse. Los números entre corchetes desambiguan hermanos del mismo nombre.\n\n```text\n");
            var roots=SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(var root in roots)Tree(root.transform,0,text);
            text.AppendLine("```\n\n## Componentes, prefabs y referencias del Inspector\n");
            foreach(var root in roots)foreach(var t in root.GetComponentsInChildren<Transform>(true))Describe(t.gameObject,text);
            text.AppendLine("\n## Contenido dinámico\n\nÚnicamente las filas del historial varían en cantidad: DispatchView instancia `Assets/Prefabs/UI/CallHistoryRow.prefab` bajo HistoryContent al mostrar registros. El prefab contiene HistoryRowView, textos, imágenes y botón; Bind recibe el CallResult y la acción para abrir la auditoría. Los 16 marcadores de llamadas (8 en el tablero provincial y 8 en el monitor) y las 8 filas de bandeja son instancias permanentes de prefabs. Se activan al llegar cada señal; el pin de dirección confirmada sigue siendo independiente. AudioClip PCM y tonos son datos de audio generados durante la ejecución, no GameObjects.\n");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs"));Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"SceneHierarchy.md"),text.ToString());
        }
        private static void Tree(Transform t,int depth,StringBuilder text)
        {
            text.Append(' ',depth*2).AppendLine(t.name+(t.gameObject.activeSelf?"":" [inactivo]"));
            foreach(Transform child in t)Tree(child,depth+1,text);
        }
        public static string PathOf(Transform t)
        {
            string name=t.name;
            if(t.parent!=null&&t.parent.Cast<Transform>().Count(x=>x.name==t.name)>1)name+=" ["+t.GetSiblingIndex()+"]";
            return t.parent==null?name:PathOf(t.parent)+"/"+name;
        }
        private static void Describe(GameObject go,StringBuilder text)
        {
            text.AppendLine("### "+PathOf(go.transform)+"\n");
            string prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            text.AppendLine("Permanente · "+(go.activeSelf?"activo":"inactivo")+" · "+(string.IsNullOrEmpty(prefab)?"objeto de escena":"prefab `"+prefab+"`")+".\n");
            foreach(var c in go.GetComponents<Component>())
            {
                if(c==null)throw new InvalidOperationException("Missing script: "+PathOf(go.transform));
                text.Append("- **").Append(c.GetType().Name).Append("**");
                if(c is MonoBehaviour mb)text.Append(" · `").Append(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(mb))).Append('`');
                text.AppendLine();
                var so=new SerializedObject(c);var field=so.GetIterator();
                while(field.NextVisible(true))
                {
                    if(field.propertyType!=SerializedPropertyType.ObjectReference||field.name=="m_Script"||field.objectReferenceValue==null)continue;
                    var obj=field.objectReferenceValue;string target=AssetDatabase.GetAssetPath(obj);
                    if(string.IsNullOrEmpty(target))target=obj is Component comp?PathOf(comp.transform)+" ("+comp.GetType().Name+")":obj is GameObject g?PathOf(g.transform):obj.name;
                    text.AppendLine("  - `"+field.propertyPath+"` → `"+target+"`");
                }
                if(c is DispatchButton binding)text.AppendLine("  - Acción: `"+binding.action+"` · argumento: `"+binding.argument+"`");
            }
            text.AppendLine();
        }
    }
}
