
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuInputManager : MonoBehaviour
    {
        // TODO update input manager asset to add needed axes and remove gravity from horizontal and vertical axes
		// https://docs.unity3d.com/2018.4/Documentation/Manual/xr_input.html
#if UNITY_EDITOR
        public static void SetupInputMap()
        {
            SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            SerializedProperty axesProp = serializedObject.FindProperty("m_Axes");

            for (int i = 0; i < axesProp.arraySize; ++i)
            {
                Debug.Log(axesProp.GetArrayElementAtIndex(i));
            }
        }
#endif


        private void Update()
        {
        }
    }
}