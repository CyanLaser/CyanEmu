using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuInputAxesSetup : MonoBehaviour
    {
        public static void SetupInputMap()
        {
            HashSet<string> inputAxisNames = new HashSet<string>();
            CyanEmuInputAxesSettings inputAxes = CyanEmuInputAxesSettings.TryLoadInputAxesSettings();

            if (inputAxes == null)
            {
                return;
            }
            
            foreach (var inputAxis in inputAxes.inputAxes)
            {
                inputAxisNames.Add(inputAxis.name);
            }

            SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            SerializedProperty axesProp = serializedObject.FindProperty("m_Axes");

            for (int currentAxis = axesProp.arraySize - 1; currentAxis > 0; --currentAxis)
            {
                SerializedProperty inputAxisProperty = axesProp.GetArrayElementAtIndex(currentAxis);
                SerializedProperty axisName = inputAxisProperty.FindPropertyRelative("m_Name");

                if (inputAxisNames.Contains(axisName.stringValue))
                {
                    axesProp.DeleteArrayElementAtIndex(currentAxis);
                }
            }

            foreach (var inputAxis in inputAxes.inputAxes)
            {
                ++axesProp.arraySize;
                SerializedProperty inputAxisProperty = axesProp.GetArrayElementAtIndex(axesProp.arraySize - 1);

                SerializedProperty axisName = inputAxisProperty.FindPropertyRelative("m_Name");
                SerializedProperty descriptiveName = inputAxisProperty.FindPropertyRelative("descriptiveName");
                SerializedProperty descriptiveNegativeName = inputAxisProperty.FindPropertyRelative("descriptiveNegativeName");
                SerializedProperty negativeButton = inputAxisProperty.FindPropertyRelative("negativeButton");
                SerializedProperty positiveButton = inputAxisProperty.FindPropertyRelative("positiveButton");
                SerializedProperty altNegativeButton = inputAxisProperty.FindPropertyRelative("altNegativeButton");
                SerializedProperty altPositiveButton = inputAxisProperty.FindPropertyRelative("altPositiveButton");
                SerializedProperty gravity = inputAxisProperty.FindPropertyRelative("gravity");
                SerializedProperty dead = inputAxisProperty.FindPropertyRelative("dead");
                SerializedProperty sensitivity = inputAxisProperty.FindPropertyRelative("sensitivity");
                SerializedProperty snap = inputAxisProperty.FindPropertyRelative("snap");
                SerializedProperty invert = inputAxisProperty.FindPropertyRelative("invert");
                SerializedProperty type = inputAxisProperty.FindPropertyRelative("type");
                SerializedProperty axis = inputAxisProperty.FindPropertyRelative("axis");
                SerializedProperty joyNum = inputAxisProperty.FindPropertyRelative("joyNum");

                axisName.stringValue = inputAxis.name;
                descriptiveName.stringValue = inputAxis.descriptiveName;
                descriptiveNegativeName.stringValue = inputAxis.descriptiveNegativeName;
                negativeButton.stringValue = inputAxis.negativeButton;
                positiveButton.stringValue = inputAxis.positiveButton;
                altNegativeButton.stringValue = inputAxis.altNegativeButton;
                altPositiveButton.stringValue = inputAxis.altPositiveButton;
                gravity.floatValue = inputAxis.gravity;
                dead.floatValue = inputAxis.dead;
                sensitivity.floatValue = inputAxis.sensitivity;
                snap.boolValue = inputAxis.snap;
                invert.boolValue = inputAxis.invert;
                type.enumValueIndex = inputAxis.type;
                axis.enumValueIndex = inputAxis.axis;
                joyNum.enumValueIndex = inputAxis.joyNum;
            }

            serializedObject.ApplyModifiedProperties();
        }

        //[MenuItem("Window/CyanEmu/Export InputMap", priority = 1010)]
        public static void ExportInputMap()
        {
            SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            SerializedProperty axesProp = serializedObject.FindProperty("m_Axes");

            CyanEmuInputAxesSettings inputAxes = ScriptableObject.CreateInstance<CyanEmuInputAxesSettings>();

            for (int currentAxis = 0; currentAxis < axesProp.arraySize; ++currentAxis)
            {
                SerializedProperty inputAxisProperty = axesProp.GetArrayElementAtIndex(currentAxis);
                SerializedProperty axisName = inputAxisProperty.FindPropertyRelative("m_Name");

                string axisNameString = axisName.stringValue;
                if (!axisNameString.StartsWith("Oculus_CrossPlatform") && 
                    !axisNameString.StartsWith("Joy"))
                {
                    continue;
                }

                SerializedProperty descriptiveName = inputAxisProperty.FindPropertyRelative("descriptiveName");
                SerializedProperty descriptiveNegativeName = inputAxisProperty.FindPropertyRelative("descriptiveNegativeName");
                SerializedProperty negativeButton = inputAxisProperty.FindPropertyRelative("negativeButton");
                SerializedProperty positiveButton = inputAxisProperty.FindPropertyRelative("positiveButton");
                SerializedProperty altNegativeButton = inputAxisProperty.FindPropertyRelative("altNegativeButton");
                SerializedProperty altPositiveButton = inputAxisProperty.FindPropertyRelative("altPositiveButton");
                SerializedProperty gravity = inputAxisProperty.FindPropertyRelative("gravity");
                SerializedProperty dead = inputAxisProperty.FindPropertyRelative("dead");
                SerializedProperty sensitivity = inputAxisProperty.FindPropertyRelative("sensitivity");
                SerializedProperty snap = inputAxisProperty.FindPropertyRelative("snap");
                SerializedProperty invert = inputAxisProperty.FindPropertyRelative("invert");
                SerializedProperty type = inputAxisProperty.FindPropertyRelative("type");
                SerializedProperty axis = inputAxisProperty.FindPropertyRelative("axis");
                SerializedProperty joyNum = inputAxisProperty.FindPropertyRelative("joyNum");

                CyanEmuInputAxesSettings.InputAxis inputAxis = new CyanEmuInputAxesSettings.InputAxis
                {
                    name = axisName.stringValue,
                    descriptiveName = descriptiveName.stringValue,
                    descriptiveNegativeName = descriptiveNegativeName.stringValue,
                    negativeButton = negativeButton.stringValue,
                    positiveButton = positiveButton.stringValue,
                    altNegativeButton = altNegativeButton.stringValue,
                    altPositiveButton = altPositiveButton.stringValue,
                    gravity = gravity.floatValue,
                    dead = dead.floatValue,
                    sensitivity = sensitivity.floatValue,
                    snap = snap.boolValue,
                    invert = invert.boolValue,
                    type = type.enumValueIndex,
                    axis = axis.enumValueIndex,
                    joyNum = joyNum.enumValueIndex,
                };

                inputAxes.inputAxes.Add(inputAxis);
            }

            CyanEmuInputAxesSettings.SaveInputAxesSettings(inputAxes);
        }
    }
}