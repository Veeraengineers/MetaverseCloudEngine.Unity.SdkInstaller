using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;

namespace MetaverseCloudEngine.Unity.Installer.Editor
{
    public class MetaverseSdkInstaller : AssetPostprocessor
    {
        private const string BasePath = "Assets/MetaverseCloudEngine";
        private const string SdkPath = BasePath + "/SDK";
        private const string VersionFilePath = BasePath + "/MVCE_Version.txt";
        private const string PackagePath = "Packages/com.reachcloud.metaverse-cloud-sdk";
        private const string DialogTitle = "Update Metaverse SDK";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var inPackages = 
                importedAssets.Any(path => path.StartsWith("Packages/")) ||
                deletedAssets.Any(path => path.StartsWith("Packages/")) ||
                movedAssets.Any(path => path.StartsWith("Packages/")) ||
                movedFromAssetPaths.Any(path => path.StartsWith("Packages/"));
 
            if (inPackages)
            {
                CheckPackages();
            }
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (SessionState.GetBool("MVCE_Restart", false))
            {
                EditorApplication.delayCall += () => 
                    EditorApplication.delayCall += () => 
                        EditorApplication.delayCall += () => 
                            EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                return;
            }

            CheckPackages();
        }

        private static void CheckPackages()
        {
            #if METAVERSE_CLOUD_ENGINE_INTERNAL
            return;
            #endif
            
            var sdkPackageGuid = AssetDatabase.FindAssets("MVCESDK_", new[] {PackagePath}).FirstOrDefault();
            var asset = AssetDatabase.GUIDToAssetPath(sdkPackageGuid);
            if (string.IsNullOrEmpty(asset))
                return;

            var name = Path.GetFileNameWithoutExtension(asset);
            if (string.IsNullOrEmpty(name))
                return;

            var version = name.Split("_")[1];
            var packageVer = ReadVersion();
            if (version != packageVer)
            {
                var installed = false;
                if (Uninstall())
                {
                    installed = true;
                    Install(asset);
                }

                SetVersion(version);

                if (installed)
                    TryRestart();
            }
        }

        private static void Install(string package)
        {
            MetaverseTmpInstaller.InstallTmpEssentials();
            AssetDatabase.ImportPackage(package, false);
        }

        public static void TryRestart()
        {
            EditorUtility.DisplayDialog(DialogTitle, "The Metaverse Cloud SDK is going to restart Unity to finish updating.", "Ok");
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            SessionState.SetBool("MVCE_Restart", true);
            CompilationPipeline.RequestScriptCompilation();
        }

        private static string ReadVersion()
        {
            var packageVer = File.Exists(VersionFilePath) ? File.ReadAllText(VersionFilePath) : null;
            return packageVer;
        }

        private static void SetVersion(string version)
        {
            var versionDir = Path.GetDirectoryName(VersionFilePath);
            if (!Directory.Exists(versionDir))
                Directory.CreateDirectory(versionDir);
            File.WriteAllText(VersionFilePath, version);
        }

        private static bool Uninstall()
        {
            if (AssetDatabase.IsValidFolder(SdkPath))
            {
                if (!EditorUtility.DisplayDialog(
                    DialogTitle,
                    "A Metaverse Cloud Engine SDK update is available. Would you like to update now?",
                    "Yes",
                    "No"))
                    return false;

                var dir = new DirectoryInfo(SdkPath);
                if (dir.Exists)
                {
                    dir.Delete(true);
                }
            }

            ScriptingDefines.Remove(new[] {ScriptingDefines.DefaultSymbols});
            return true;
        }

    }
}