#if UDON

using UnityEngine;
using UnityEditor;
using VRC.Udon;

namespace VRCPrefabs.CyanEmu
{
    [CustomEditor(typeof(CyanEmuUdonHelper))]
    public class CyanEmuUdonHelperEditor : Editor
    {
        private bool expand_ = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            CyanEmuUdonHelper udonHelper = target as CyanEmuUdonHelper;

            CyanEmuSyncableEditorHelper.DisplaySyncOptions(udonHelper);

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