
using UnityEditor;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuBoot : AssetPostprocessor
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            CyanEmuSettingsWindow.TryInitOnLoad();
        }

        // Used to ensure that everything has been imported before trying to load the inputmap.
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            CyanEmuInputAxesSetup.SetupInputMap();
        }
    }
}