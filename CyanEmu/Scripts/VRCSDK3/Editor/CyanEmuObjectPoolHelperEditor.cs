#if UDON

using UnityEditor;

namespace VRCPrefabs.CyanEmu
{
    [CustomEditor(typeof(CyanEmuObjectPoolHelper))]
    public class CyanEmuObjectPoolHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            CyanEmuSyncableEditorHelper.DisplaySyncOptions(target as CyanEmuObjectPoolHelper);
        }
    }
}
#endif
