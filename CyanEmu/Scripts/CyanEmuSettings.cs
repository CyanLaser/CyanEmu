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
                    instance_ = new CyanEmuSettings();
#endif
                }
                return instance_;
            }
        }

        const string CYAN_EMU_SETTINGS_PREFS_STRING = "CyanEmuSettings";

        [SerializeField] public bool displaySettingsWindowAtLaunch = true;

        [SerializeField] public KeyCode crouchKey = KeyCode.C;
        [SerializeField] public KeyCode proneKey = KeyCode.Z;
        [SerializeField] public KeyCode runKey = KeyCode.LeftShift;

        [SerializeField] public string customLocalPlayerName = "";

        // TODO move settings to be per project instead of global to all
        [SerializeField] public bool enableCyanEmu = true;
        [SerializeField] public bool displayLogs = true;
        [SerializeField] public bool deleteEditorOnly = true;
        [SerializeField] public bool spawnPlayer = true;
        [SerializeField] public bool replayBufferedTriggers = false;

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