
using UnityEditor;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuBoot : AssetPostprocessor
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            CyanEmuSettingsWindow.TryInitOnLoad();
            SetAudioSettings(); 
        }

        // Used to ensure that everything has been imported before trying to load the inputmap.
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            CyanEmuInputAxesSetup.SetupInputMap();
        }

        private static void SetAudioSettings()
        {
            SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0]);
            SerializedProperty spatializerPluginProp = serializedObject.FindProperty("m_SpatializerPlugin");
            SerializedProperty ambisonicDecoderPluginProp = serializedObject.FindProperty("m_AmbisonicDecoderPlugin");

            spatializerPluginProp.stringValue = "OculusSpatializer";
            ambisonicDecoderPluginProp.stringValue = "OculusSpatializer";

            serializedObject.ApplyModifiedProperties();
        }
    }
}