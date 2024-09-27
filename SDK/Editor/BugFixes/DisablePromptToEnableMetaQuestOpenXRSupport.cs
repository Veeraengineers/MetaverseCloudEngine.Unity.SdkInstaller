using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MetaverseCloudEngine.Unity.Editors.BugFixes
{
    public static class DisablePromptToEnableMetaQuestOpenXRSupport
    {
        [InitializeOnLoadMethod]
        private static void PatchCode()
        {
            var files = System.IO.Directory.GetFiles("Library/PackageCache", "MetaXRFeatureEnabler.cs", System.IO.SearchOption.AllDirectories);
            if (files.Length == 0) return;
            var path = files.FirstOrDefault(x => x.Replace("\\", "/").StartsWith("Library/PackageCache/com.meta.xr.sdk.core@") && x.Replace("\\", "/").EndsWith("/Editor/OpenXRFeatures/MetaXRFeatureEnabler.cs"));
            if (!System.IO.File.Exists(path)) return;
            var text = System.IO.File.ReadAllText(path);
            if (text.Contains("EditorApplication.update += EnableMetaXRFeature;"))
            {
                text = text.Replace("EditorApplication.update += EnableMetaXRFeature;", "// Removed line...");
                System.IO.File.WriteAllText(path, text);
                CompilationPipeline.RequestScriptCompilation();
            }
        }
    }
}