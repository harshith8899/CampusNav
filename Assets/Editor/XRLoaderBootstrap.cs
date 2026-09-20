using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

namespace CampusNav.EditorTools
{
    /// <summary>
    /// Enables the ARCore loader for Android and the ARKit loader for iOS
    /// under XR Plug-in Management, without requiring anyone to open
    /// Project Settings > XR Plug-in Management and tick the checkboxes by
    /// hand. Runs automatically the first time this project is opened in
    /// the Editor (after the packages added to manifest.json have been
    /// resolved), and is safe to re-run via the menu item below — it checks
    /// what's already assigned before doing anything.
    ///
    /// This has to run inside the Editor because XR Plug-in Management's
    /// settings are a graph of ScriptableObject assets wired together via
    /// internal GUIDs generated at runtime (see XRPackageMetadataStore) —
    /// there's no static project file to hand-edit for this part.
    /// </summary>
    internal static class XRLoaderBootstrap
    {
        private const string ARCoreLoaderTypeName = "UnityEngine.XR.ARCore.ARCoreLoader";
        private const string ARKitLoaderTypeName = "UnityEngine.XR.ARKit.ARKitLoader";
        private const string SettingsAssetDir = "Assets/XR";
        private const string SettingsAssetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

        static XRLoaderBootstrap()
        {
            // Delay past the initial domain load so the XR management/ARCore/
            // ARKit assemblies are fully available before we touch their types.
            EditorApplication.delayCall += RunOnce;
        }

        [MenuItem("CampusNav/Setup/Enable AR XR Loaders (ARCore + ARKit)")]
        private static void RunOnce()
        {
            try
            {
                var settings = GetOrCreateSettings();
                bool changedAndroid = ConfigureLoader(settings, BuildTargetGroup.Android, ARCoreLoaderTypeName);
                bool changedIOS = ConfigureLoader(settings, BuildTargetGroup.iOS, ARKitLoaderTypeName);

                if (changedAndroid || changedIOS)
                {
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log("CampusNav: XR Plug-in Management configured — ARCore (Android) / ARKit (iOS) loaders assigned.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"CampusNav: could not auto-configure XR loaders ({e.Message}). " +
                                  "Open Project Settings > XR Plug-in Management and enable ARCore/ARKit manually, " +
                                  "or re-run CampusNav > Setup > Enable AR XR Loaders from the menu.");
            }
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreateSettings()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget settings);
            if (settings != null) return settings;

            if (!AssetDatabase.IsValidFolder(SettingsAssetDir))
            {
                AssetDatabase.CreateFolder("Assets", "XR");
            }

            settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            return settings;
        }

        private static bool ConfigureLoader(XRGeneralSettingsPerBuildTarget settings, BuildTargetGroup group, string loaderTypeName)
        {
            if (XRPackageMetadataStore.IsLoaderAssigned(loaderTypeName, group))
            {
                return false;
            }

            if (!settings.HasSettingsForBuildTarget(group))
            {
                settings.CreateDefaultSettingsForBuildTarget(group);
            }

            if (!settings.HasManagerSettingsForBuildTarget(group))
            {
                settings.CreateDefaultManagerSettingsForBuildTarget(group);
            }

            XRManagerSettings manager = settings.ManagerSettingsForBuildTarget(group);
            bool assigned = XRPackageMetadataStore.AssignLoader(manager, loaderTypeName, group);
            if (assigned)
            {
                EditorUtility.SetDirty(manager);
            }
            return assigned;
        }
    }
}
