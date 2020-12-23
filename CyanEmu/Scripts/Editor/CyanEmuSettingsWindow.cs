#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using System.Collections.Generic;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuSettingsWindow : EditorWindow
    {
        private const string VERSION_FILE_PATH = "Assets/CyanEmu/version.txt";
        private const string GITHUB_URL = "https://github.com/CyanLaser/CyanEmu";
        private const string WIKI_URL = "https://github.com/CyanLaser/CyanEmu/wiki";

        // General content
        private readonly GUIContent generalFoldoutGuiContent = new GUIContent("General Settings", "");
        private readonly GUIContent enableToggleGuiContent = new GUIContent("Enable CyanEmu", "If enabled, all triggers will function simlarly to VRChat. Note that behavior may be different than the actual game!");
        private readonly GUIContent displayLogsToggleGuiContent = new GUIContent("Enable Console Logging", "Enabling logging will print messages to the console when certain events happen. Examples include trigger execution, pickup grabbed, station entered, etc.");
        private readonly GUIContent deleteEditorOnlyToggleGuiContent = new GUIContent("Remove \"EditorOnly\"", "Enabling this setting will ensure that all objects with the tag \"EditorOnly\" are deleted when in playmode. This can be helpful in finding objects that will not be uploaded with your world. Enable console logging to see which objects are deleted.");

        // Player Controller content
        private readonly GUIContent playerControllerFoldoutGuiContent = new GUIContent("Player Controller Settings", "");
        private readonly GUIContent playerControllerToggleGuiContent = new GUIContent("Spawn Player Controller", "If enabled, a player controller will spawn and allow you to move around your world as if in desktop mode. Supports interacts and pickups.");
        private readonly GUIContent playerControllerRunKeyGuiContent = new GUIContent("Run Key", "The button used to run. Running will make the player move faster.");
        private readonly GUIContent playerControllerCrouchKeyGuiContent = new GUIContent("Crouch Key", "The button used to crouch. Crouching will lower the camera midway to the floor and slow down the player.");
        private readonly GUIContent playerControllerProneKeyGuiContent = new GUIContent("Prone Key", "The button used to go prone. Going prone will lower the camera closer to the floor and slow down the player.");
        private readonly GUIContent playerControllerCustomName = new GUIContent("Player Name", "Set a custom name for testing udon script name detection");

        // Buffered Trigger content
        private readonly GUIContent bufferedTriggerFoldoutGuiContent = new GUIContent("Buffered Trigger Settings", "");
        private readonly GUIContent replayBufferedTriggerToggleGuiContent = new GUIContent("Replay Buffered Triggers", "If enabled, buffered triggers for this scene will be replayed at the start before all other triggers.");
        
        //
        private readonly GUIContent playerButtonsFoldoutGuiContent = new GUIContent("Add or Remove Players", "");



        private static CyanEmuSettings settings_;
        private Vector2 scrollPosition_;
        private GUIStyle boxStyle_;
        private bool showGeneralSettings_ = true;
        private bool showPlayerControllerSettings_;
        private bool showBufferedTriggerSettings_;
        private bool showTriggerEventButtons_;
        private bool showPlayerButtons_;

        private string version_;
        private string remotePlayerCustomName = "";

        public static void TryInitOnLoad()
        {
            settings_ = CyanEmuSettings.Instance;
            if (settings_.displaySettingsWindowAtLaunch)
            {
                Init();
            }
        }

        [MenuItem("Window/CyanEmu/CyanEmu Settings", priority = 1010)]
        static void Init()
        {
            CyanEmuSettingsWindow window = (CyanEmuSettingsWindow)EditorWindow.GetWindow(typeof(CyanEmuSettingsWindow), false, "CyanEmu Settings");
            window.Show();
        }

        private void OnEnable()
        {
            if (settings_ == null)
            {
                settings_ = CyanEmuSettings.Instance;
            }

            version_ = System.IO.File.ReadAllText(VERSION_FILE_PATH).Trim();

            if (settings_.displaySettingsWindowAtLaunch)
            {
                settings_.displaySettingsWindowAtLaunch = false;
                CyanEmuSettings.SaveSettings(settings_);
            }
        }

        void OnGUI()
        {
            boxStyle_ = new GUIStyle(EditorStyles.helpBox);
            scrollPosition_ = EditorGUILayout.BeginScrollView(scrollPosition_);

            DrawHeader();

            EditorGUI.BeginChangeCheck();

            // Disables UI if CyanEmu is disabled
            DrawGeneralSettings();

            DrawPlayerControllerSettings();
            
            DrawBufferedTriggerSettings();

            DrawPlayerButtons();

            // Disable group from General settings
            EditorGUI.EndDisabledGroup();
            

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                CyanEmuSettings.SaveSettings(settings_);
            }
        }

        private void DrawHeader()
        {
            DrawTitle();
            DrawLinks();
            GUILayout.Space(5);
        }

        private void DrawTitle()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("CyanEmu Settings", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: " + version_);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Created by CyanLaser");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLinks()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Space(10);
            if (GUILayout.Button("Documentation", GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL(WIKI_URL);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Github Repo", GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL(GITHUB_URL);
            }
            GUILayout.Space(10);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGeneralSettings()
        {
            EditorGUILayout.BeginVertical(boxStyle_);
            showGeneralSettings_ = EditorGUILayout.Foldout(showGeneralSettings_, generalFoldoutGuiContent, true);

            if (showGeneralSettings_)
            {
                if (settings_.enableCyanEmu && FindObjectOfType<VRC_SceneDescriptor>() == null)
                {
                    EditorGUILayout.HelpBox("No VRC_SceneDescriptor in scene. Please add one to enable CyanEmu.", MessageType.Warning);
                }
                if (settings_.enableCyanEmu && Application.isPlaying && !CyanEmuMain.HasInstance())
                {
                    EditorGUILayout.HelpBox("Please exit and reenter play mode to enable CyanEmu!", MessageType.Warning);
                }

                settings_.enableCyanEmu = EditorGUILayout.Toggle(enableToggleGuiContent, settings_.enableCyanEmu);

                EditorGUI.BeginDisabledGroup(!settings_.enableCyanEmu);

                settings_.displayLogs = EditorGUILayout.Toggle(displayLogsToggleGuiContent, settings_.displayLogs);

                settings_.deleteEditorOnly = EditorGUILayout.Toggle(deleteEditorOnlyToggleGuiContent, settings_.deleteEditorOnly);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPlayerControllerSettings()
        {
            EditorGUILayout.BeginVertical(boxStyle_);

            showPlayerControllerSettings_ = EditorGUILayout.Foldout(showPlayerControllerSettings_, playerControllerFoldoutGuiContent, true);
            if (showPlayerControllerSettings_)
            {
                AddIndent();

                settings_.spawnPlayer = EditorGUILayout.Toggle(playerControllerToggleGuiContent, settings_.spawnPlayer);

                EditorGUI.BeginDisabledGroup(!settings_.spawnPlayer);

                // key bindings
                settings_.runKey = (KeyCode)EditorGUILayout.EnumPopup(playerControllerRunKeyGuiContent, settings_.runKey);
                settings_.crouchKey = (KeyCode)EditorGUILayout.EnumPopup(playerControllerCrouchKeyGuiContent, settings_.crouchKey);
                settings_.proneKey = (KeyCode)EditorGUILayout.EnumPopup(playerControllerProneKeyGuiContent, settings_.proneKey);
                
                // custom name
                settings_.customLocalPlayerName = EditorGUILayout.TextField(playerControllerCustomName, settings_.customLocalPlayerName);

                EditorGUI.EndDisabledGroup();

                RemoveIndent();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBufferedTriggerSettings()
        {
            // TODO convert to enable for Udon and general syncable objects
#if VRC_SDK_VRCSDK2
            EditorGUILayout.BeginVertical(boxStyle_);
            showBufferedTriggerSettings_ = EditorGUILayout.Foldout(showBufferedTriggerSettings_, bufferedTriggerFoldoutGuiContent, true);
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
            EditorGUILayout.EndVertical();
#endif
        }

        private void DrawPlayerButtons()
        {
            EditorGUILayout.BeginVertical(boxStyle_);
            showPlayerButtons_ = EditorGUILayout.Foldout(showPlayerButtons_, playerButtonsFoldoutGuiContent, true);
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

                remotePlayerCustomName = EditorGUILayout.TextField(playerControllerCustomName, remotePlayerCustomName);

                if (GUILayout.Button("Spawn Remote Player"))
                {
                    CyanEmuMain.SpawnPlayer(false, remotePlayerCustomName);
                }

                List<VRCPlayerApi> playersToRemove = new List<VRCPlayerApi>();
                if (Application.isPlaying)
                {
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

                    for (int i = playersToRemove.Count - 1; i >= 0; --i)
                    {
                        CyanEmuMain.RemovePlayer(playersToRemove[i]);
                    }
                    playersToRemove.Clear();
                }

                EditorGUI.EndDisabledGroup();

                RemoveIndent();
            }
            EditorGUILayout.EndVertical();
        }

        private void AddIndent()
        {
            ++EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 7 + 4);
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