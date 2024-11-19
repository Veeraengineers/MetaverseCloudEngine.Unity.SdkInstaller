#if !CLOUD_BUILD_PLATFORM
using System.Linq;
using System.Net;
using MetaverseCloudEngine.Unity.Installer;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace MetaverseCloudEngine.Unity.Installer
{
    public class MetaverseRequiredPackageInstaller : AssetPostprocessor
    {
        private const string InitialUpdateCheckFlag = "MVCE_InitialUpdateCheck";

        private static readonly string[] PackagesToInstall =
        {
            "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
            "https://github.com/Unity-Technologies/AssetBundles-Browser.git",
            "https://github.com/ReachCloudDevelopers/GLTFUtility.git",
        };

        private static AddAndRemoveRequest _packageRequest;
        
        public static void ForceInstallPackages()
        {
            SessionState.EraseBool(InitialUpdateCheckFlag);
            InstallPackages();
        }

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        private static void InstallPackages()
        {
            if (EditorApplication.isCompiling) return;
            if (SessionState.GetBool(InitialUpdateCheckFlag, 
#if METAVERSE_CLOUD_ENGINE_INTERNAL
                true
#else
                false
#endif
                    )) return;
            try
            {
                    while (!TryUpdatePackages())
                    {
                        ShowProgressBar();
                        System.Threading.Thread.Sleep(500);
                    }

                    HideProgressBar();
            }
            finally
            {
                SessionState.SetBool(InitialUpdateCheckFlag, true);
            }
        }
        
#if !METAVERSE_CLOUD_ENGINE_INTERNAL
        private static void OnPostprocessAllAssets(
            string[] importedAssets, 
            string[] deletedAssets, 
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (deletedAssets.Length <= 0 ||
                !deletedAssets.Any(x => x.Contains("Packages/com.reachcloud.metaverse-cloud-sdk"))) 
                return;
            SessionState.EraseBool(InitialUpdateCheckFlag);
            ScriptingDefines.RemoveDefaultSymbols(
                EditorUtility.DisplayDialog("Uninstall Metaverse Cloud Engine SDK Defines", 
                    "Would you like to remove integration package scripting symbols? " +
                    "You will have to re-enable integrations if you choose to install the SDK again.", 
                    "No (Recommended)", "Yes") == false);
        }
#endif

        [UsedImplicitly]
        private static bool TryUpdatePackages()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _packageRequest = null;
                return true;
            }
            
            var httpClient = new HttpWebRequest("https://api.github.com/repos/ReachCloudDevelopers/MetaverseCloudEngine.Unity.SDK/commits?per_page=1");
            var response = httpClient.GetResponse();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Debug.LogError("Failed to check for updates: " + response.StatusDescription);
                return true;
            }
            
            var latestCommit = response.GetResponseStream().ReadToEnd();
            var regex = new System.Text.RegularExpressions.Regex("\"sha\": \"([a-zA-Z0-9]+)\"");
            var match = regex.Match(latestCommit);
            if (!match.Success)
            {
                Debug.LogError("Failed to check for updates: " + latestCommit);
                return true;
            }
            
            var latestCommitHash = match.Groups[1].Value;
            _packageRequest ??= Client.AddAndRemove(packagesToAdd: PackagesToInstall.Concat(new[] {
                $"https://github.com/ReachCloudDevelopers/MetaverseCloudEngine.Unity.SDK.git#{latestCommitHash}"
            }));
            switch (_packageRequest.Status)
            {
                case StatusCode.InProgress:
                    return false;
                case StatusCode.Success:
                    OnPackagesInstalled();
                    break;
                default:
                    OnPackagesFailed();
                    break;
            }

            _packageRequest = null;
            return true;

        }

        private static void OnPackagesInstalled()
        {
            ScriptingDefines.AddDefaultSymbols();
            Client.Resolve();
            EditorUtility.ClearProgressBar();
        }

        private static void OnPackagesFailed()
        {
            Debug.LogError("Failed to install packages: " + _packageRequest.Error.message);
            EditorUtility.ClearProgressBar();
        }

        [UsedImplicitly]
        private static void ShowProgressBar() => EditorUtility.DisplayProgressBar("Metaverse Cloud Engine Dependencies", "Ensuring Package Dependencies...", 1);

        [UsedImplicitly]
        private static void HideProgressBar() => EditorUtility.ClearProgressBar();
    }
}
#endif
