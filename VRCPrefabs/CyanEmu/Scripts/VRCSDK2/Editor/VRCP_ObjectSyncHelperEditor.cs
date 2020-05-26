// VRCP_ObjectSyncHelperEditor
// Created by CyanLaser

#if VRC_SDK_VRCSDK2

using UnityEditor;

namespace VRCPrefabs.CyanEmu
{
    [CustomEditor(typeof(VRCP_ObjectSyncHelper))]
    public class VRCP_ObjectSyncHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            VRCP_SyncableEditorHelper.DisplaySyncOptions(target as VRCP_ObjectSyncHelper);
        }
    }
}

#endif