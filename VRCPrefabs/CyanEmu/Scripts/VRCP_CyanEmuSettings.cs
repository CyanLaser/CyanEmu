#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Serialization;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuSettings
    {
        private static CyanEmuSettings instance_;
        public static CyanEmuSettings Instance
        {
            get
            {
                if (instance_ == null)
                {
#if UNITY_EDITOR
                    instance_ = LoadSettings();
#else
                    instance_ = new VRCP_CyanEmuSettings();
#endif
                }
                return instance_;
            }
        }

        // Left for legacy reasons /shrug
        const string CYAN_EMU_SETTINGS_PREFS_STRING = "VRCP_TriggerExecutorSettings";

        [FormerlySerializedAs("enableTriggerExecution")]
        [SerializeField] public bool enableCyanEmu = true;
        [SerializeField] public bool displayLogs = true;
        [SerializeField] public bool spawnPlayer = true;
        [SerializeField] public bool replayBufferedTriggers = false;

        [SerializeField] public KeyCode crouchKey = KeyCode.C;
        [SerializeField] public KeyCode proneKey = KeyCode.Z;
        [SerializeField] public KeyCode runKey = KeyCode.LeftShift;

#if UNITY_EDITOR
        private static CyanEmuSettings LoadSettings()
        {
            CyanEmuSettings settings = new CyanEmuSettings();

            string data = EditorPrefs.GetString(CYAN_EMU_SETTINGS_PREFS_STRING, JsonUtility.ToJson(settings, false));
            JsonUtility.FromJsonOverwrite(data, settings);
            return settings;
        }

        public static void SaveSettings(CyanEmuSettings settings)
        {
            string data = JsonUtility.ToJson(settings, false);
            EditorPrefs.SetString(CYAN_EMU_SETTINGS_PREFS_STRING, data);
        }
#endif

    }
}