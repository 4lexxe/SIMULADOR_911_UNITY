using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace EmergencyDispatch.Editor
{
    public static class SceneBuildValidation
    {
        public static void Run()
        {
            SceneHierarchyDocumentation.ExportSavedScene();
            string artifacts=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            string target=Path.Combine(artifacts,"SceneValidationBuild","UltimaLlamada.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=new[]{EmergencyDispatcherBuilderEditor.ScenePath},
                locationPathName=target,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development
            });
            File.WriteAllText(Path.Combine(artifacts,"scene-build-validation.txt"),report.summary.result+" · errors: "+report.summary.totalErrors+" · output: "+target);
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
    }
}
