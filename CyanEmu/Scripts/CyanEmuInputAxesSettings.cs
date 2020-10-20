using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuInputAxesSettings : ScriptableObject
    {
        public static string INPUT_MAP_FILE_NAME = "Assets/CyanEmu/Resources/CyanEmuOculusInputMap.asset";
        
        public List<InputAxis> inputAxes = new List<InputAxis>();

        [Serializable]
        public class InputAxis
        {
            public string name;
            public string descriptiveName;
            public string descriptiveNegativeName;
            public string negativeButton;
            public string positiveButton;
            public string altNegativeButton;
            public string altPositiveButton;
            public float gravity;
            public float dead;
            public float sensitivity;
            public bool snap;
            public bool invert;
            public int type;
            public int axis;
            public int joyNum;
        }

#if UNITY_EDITOR
        public static CyanEmuInputAxesSettings TryLoadInputAxesSettings()
        {
            CyanEmuInputAxesSettings settings = LoadInputAxesSettings();

            if (settings == null)
            {
                AssetDatabase.ImportAsset(INPUT_MAP_FILE_NAME);
                settings = LoadInputAxesSettings();
            }

            return settings;
        }

        public static CyanEmuInputAxesSettings LoadInputAxesSettings()
        {
            return (CyanEmuInputAxesSettings)AssetDatabase.LoadAssetAtPath(INPUT_MAP_FILE_NAME, typeof(CyanEmuInputAxesSettings));
        }

        public static void SaveInputAxesSettings(CyanEmuInputAxesSettings settings)
        {
            AssetDatabase.CreateAsset(settings, INPUT_MAP_FILE_NAME);
        }
#endif
    }
}
