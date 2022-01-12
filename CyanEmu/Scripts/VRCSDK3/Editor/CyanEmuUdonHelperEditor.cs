#if UDON

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using VRC.Udon;
using VRC.Udon.Editor.ProgramSources;

namespace VRCPrefabs.CyanEmu
{
    [CustomEditor(typeof(CyanEmuUdonHelper))]
    public class CyanEmuUdonHelperEditor : Editor
    {
        private static readonly MethodInfo DrawPropertyMethod;
        
        private bool expandVariableEditor_ = false;
        private bool expandEventSelector_ = false;

        static CyanEmuUdonHelperEditor()
        {
            DrawPropertyMethod = typeof(UdonProgramAsset).GetMethod("DrawPublicVariableField", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            CyanEmuUdonHelper udonHelper = target as CyanEmuUdonHelper;

            CyanEmuSyncableEditorHelper.DisplaySyncOptions(udonHelper);

            UdonBehaviour udonBehaviour = udonHelper.GetUdonBehaviour();

            ShowVariableEditor(udonBehaviour);
            
            ShowExportedEvents(udonBehaviour);
        }

        private void ShowVariableEditor(UdonBehaviour udonBehaviour)
        {
            expandVariableEditor_ = EditorGUILayout.Foldout(expandVariableEditor_, "Edit Public Variables", true);

            if (!expandVariableEditor_)
            {
                return;
            }

            var program = udonBehaviour.programSource;

            if (!(program is UdonProgramAsset programAsset))
            {
                return;
            }
            
            var publicVariables = udonBehaviour.publicVariables;

            foreach (var varName in publicVariables.VariableSymbols)
            {
                publicVariables.TryGetVariableType(varName, out Type varType);
                object value = udonBehaviour.GetProgramVariable(varName);
                object[] parameters = {varName, value, varType, false, true};
                var res = DrawPropertyMethod.Invoke(programAsset, parameters);
                
                if ((bool)parameters[3])
                {
                    udonBehaviour.SetProgramVariable(varName, res);
                }
            }
        }
        
        private void ShowExportedEvents(UdonBehaviour udonBehaviour)
        {
            expandEventSelector_ = EditorGUILayout.Foldout(expandEventSelector_, "Run Custom Event", true);

            if (!expandEventSelector_)
            {
                return;
            }

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

#endif