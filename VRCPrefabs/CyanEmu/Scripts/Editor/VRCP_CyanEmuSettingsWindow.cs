#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using System.Collections.Generic;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuSettingsWindow : EditorWindow
    {
        private const string VERSION_FILE_PATH = "Assets/VRCPrefabs/CyanEmu/version.txt";

        // General content
        private readonly GUIContent enableToggleGuiContent = new GUIContent("Enable CyanEmu", "If enabled, all triggers will function simlarly to VRChat. Note that behavior may be different than the actual game!");
        private readonly GUIContent displayLogsToggleGuiContent = new GUIContent("Enable Console Logging", "Enabling logging will print messages to the console when certain events happen. Examples include trigger execution, pickup grabbed, station entered, etc.");

        // Player Controller content
        private readonly GUIContent playerControllerFoldoutGuiContent = new GUIContent("Player Controller Settings", "");
        private readonly GUIContent playerControllerToggleGuiContent = new GUIContent("Spawn Player Controller", "If enabled, a player controller will spawn and allow you to move around your world as if in desktop mode. Supports interacts and pickups.");
        private readonly GUIContent playerControllerRunKeyGuiContent = new GUIContent("Run Key", "The button used to run. Running will make the player move faster.");
        private readonly GUIContent playerControllerCrouchKeyGuiContent = new GUIContent("Crouch Key", "The button used to crouch. Crouching will lower the camera midway to the floor and slow down the player.");
        private readonly GUIContent playerControllerProneKeyGuiContent = new GUIContent("Prone Key", "The button used to go prone. Going prone will lower the camera closer to the floor and slow down the player.");

        // Buffered Trigger content
        private readonly GUIContent bufferedTriggerFoldoutGuiContent = new GUIContent("Buffered Trigger Settings", "");
        private readonly GUIContent replayBufferedTriggerToggleGuiContent = new GUIContent("Replay Buffered Triggers", "If enabled, buffered triggers for this scene will be replayed at the start before all other triggers.");
        
        //
        private readonly GUIContent playerButtonsFoldoutGuiContent = new GUIContent("Add or Remove Players", "");



        private CyanEmuSettings settings_;
        private Vector2 scrollPosition_;
        private bool showPlayerControllerSettings_;
        private bool showBufferedTriggerSettings_;
        private bool showTriggerEventButtons_;
        private bool showPlayerButtons_;

        private string version_;

        [MenuItem("VRC Prefabs/CyanEmu/CyanEmu Settings")]
        static void Init()
        {
            CyanEmuSettingsWindow window = (CyanEmuSettingsWindow)EditorWindow.GetWindow(typeof(CyanEmuSettingsWindow), false, "CyanEmu Settings");
            window.Show();
        }

        private void OnEnable()
        {
            settings_ = CyanEmuSettings.Instance;
            version_ = System.IO.File.ReadAllText(VERSION_FILE_PATH).Trim();
        }

        void OnGUI()
        {
            scrollPosition_ = EditorGUILayout.BeginScrollView(scrollPosition_);

            GUILayout.Label("CyanEmu Settings", EditorStyles.boldLabel);
            AddIndent();
            GUILayout.Label("Version: " + version_);
            RemoveIndent();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            
            if (settings_.enableCyanEmu && Application.isPlaying && !CyanEmuMain.HasInstance())
            {
                EditorGUILayout.HelpBox("Please exit and reenter play mode to enable CyanEmu!", MessageType.Warning);
            }

            settings_.enableCyanEmu = EditorGUILayout.Toggle(enableToggleGuiContent, settings_.enableCyanEmu);

            EditorGUI.BeginDisabledGroup(!settings_.enableCyanEmu);
            

            settings_.displayLogs = EditorGUILayout.Toggle(displayLogsToggleGuiContent, settings_.displayLogs);

            DrawPlayerControllerSettings();
            
            DrawBufferedTriggerSettings();

            DrawPlayerButtons();

            EditorGUI.EndDisabledGroup();

            // TODO
            UpdateInput();

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                CyanEmuSettings.SaveSettings(settings_);
            }
        }

        private void DrawPlayerControllerSettings()
        {
            showPlayerControllerSettings_ = EditorGUILayout.Foldout(showPlayerControllerSettings_, playerControllerFoldoutGuiContent);
            if (showPlayerControllerSettings_)
            {
                AddIndent();

                settings_.spawnPlayer = EditorGUILayout.Toggle(playerControllerToggleGuiContent, settings_.spawnPlayer);

                EditorGUI.BeginDisabledGroup(!settings_.spawnPlayer);

                // key bindings
                settings_.runKey = (KeyCode)EditorGUILayout.EnumPopup(playerControllerRunKeyGuiContent, settings_.runKey);
                settings_.crouchKey = (KeyCode)EditorGUILayout.EnumPopup(playerControllerCrouchKeyGuiContent, settings_.crouchKey);
                settings_.proneKey = (KeyCode)EditorGUILayout.EnumPopup(playerControllerProneKeyGuiContent, settings_.proneKey);

                EditorGUI.EndDisabledGroup();

                RemoveIndent();
            }
        }

        private void DrawBufferedTriggerSettings()
        {
// TODO convert to enable for Udon and general syncable objects
#if VRC_SDK_VRCSDK2
            showBufferedTriggerSettings_ = EditorGUILayout.Foldout(showBufferedTriggerSettings_, bufferedTriggerFoldoutGuiContent);
            if (showBufferedTriggerSettings_)
            {
                AddIndent();

                settings_.replayBufferedTriggers = EditorGUILayout.Toggle(replayBufferedTriggerToggleGuiContent, settings_.replayBufferedTriggers);

                EditorGUI.BeginDisabledGroup(CyanEmuBufferManager.instance == null || !Application.isPlaying);

                if (GUILayout.Button("Save Current Buffered Triggers"))
                {
                    CyanEmuBufferManager.SaveBufferedTriggersToFile();
                }

                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!CyanEmuBufferManager.SceneContainsBufferedTriggers());

                if (GUILayout.Button("Clear Buffered Triggers"))
                {
                    CyanEmuBufferManager.DeleteBufferedTriggerFile();
                }

                EditorGUI.EndDisabledGroup();

                RemoveIndent();
            }
#endif
        }

        private void DrawPlayerButtons()
        {
            showPlayerButtons_ = EditorGUILayout.Foldout(showPlayerButtons_, playerButtonsFoldoutGuiContent);
            if (showPlayerButtons_)
            {
                AddIndent();

                EditorGUI.BeginDisabledGroup(!CyanEmuMain.HasInstance() || !Application.isPlaying);

                /*
                EditorGUI.BeginDisabledGroup(CyanEmuPlayerController.instance != null);

                if (GUILayout.Button("Spawn Local Player"))
                {
                    CyanEmuMain.SpawnPlayer(true);
                }

                EditorGUI.EndDisabledGroup();
                */

                if (GUILayout.Button("Spawn Remote Player"))
                {
                    CyanEmuMain.SpawnPlayer(false);
                }

                List<VRCPlayerApi> playersToRemove = new List<VRCPlayerApi>();
                foreach (var player in VRCPlayerApi.AllPlayers)
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(player.displayName);
                    GUILayout.Space(5);

                    EditorGUI.BeginDisabledGroup(VRCPlayerApi.AllPlayers.Count == 1 || player.isLocal);

                    if (GUILayout.Button("Remove Player"))
                    {
                        playersToRemove.Add(player);
                    }

                    EditorGUI.EndDisabledGroup();

                    GUILayout.EndHorizontal();
                }

                for (int i = playersToRemove.Count -1; i >= 0; --i)
                {
                    CyanEmuMain.RemovePlayer(playersToRemove[i]);
                }
                playersToRemove.Clear();

                EditorGUI.EndDisabledGroup();

                RemoveIndent();
            }
        }

        // TODO
        private void UpdateInput()
        {
            /*
            if (GUILayout.Button("Read Input"))
            {
                CyanEmuInputManager.SetupInputMap();
            }
            */
        }

        private void AddIndent()
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15 + 4);
            EditorGUILayout.BeginVertical();
        }

        private void RemoveIndent()
        {
            --EditorGUI.indentLevel;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }
}


#endif