// VRCP_UdonHelperEditor
// Created by CyanLaser

#if UDON

using UnityEngine;
using UnityEditor;
using VRC.Udon;

namespace VRCPrefabs.CyanEmu
{
    [CustomEditor(typeof(VRCP_UdonHelper))]
    public class VRCP_UdonHelperEditor : Editor
    {
        private bool expand_ = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            VRCP_UdonHelper udonHelper = target as VRCP_UdonHelper;

            VRCP_SyncableEditorHelper.DisplaySyncOptions(udonHelper);

            UdonBehaviour udonBehaviour = udonHelper.GetUdonBehaviour();

            // TODO set public variables

            expand_ = EditorGUILayout.Foldout(expand_, "Run Custom Event");

            if (expand_)
            {
                foreach (string eventName in udonBehaviour.GetPrograms())
                {
                    if (GUILayout.Button(eventName))
                    {
                        udonBehaviour.SendCustomEvent(eventName);
                    }
                }
            }
        }
    }
}

#endif