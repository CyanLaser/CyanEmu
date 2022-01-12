using UnityEditor;

namespace VRCPrefabs.CyanEmu
{
    [CustomEditor(typeof(CyanEmuObjectSyncHelper))]
    public class CyanEmuObjectSyncHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            CyanEmuSyncableEditorHelper.DisplaySyncOptions(target as CyanEmuObjectSyncHelper);
        }
    }
}