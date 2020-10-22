using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRCPrefabs.CyanEmu
{
    [CustomEditor(typeof(CyanEmuSpatialAudioHelper))]
    public class CyanEmuSpatialAudioHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Show nothing
        }
    }
}