// VRCP_CyanEmuMain
// Created by CyanLaser

// Check settings under VRC Prefabs/CyanEmu/CyanEmu Settings

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VRC.SDKBase;


namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_CyanEmuMain : MonoBehaviour
    {
        private const string CYAN_EMU_GAMEOBJECT_NAME_ = "__VRCPCyanEmu";

        private static VRCP_CyanEmuMain instance_;

        private VRCP_SDKManager sdkManager_;

        private VRCP_CyanEmuSettings settings_;
        private VRCP_PlayerController playerController_;
        private VRC_SceneDescriptor descriptor_;
        private HashSet<VRCP_SyncedObjectHelper> allSyncedObjects_ = new HashSet<VRCP_SyncedObjectHelper>();

        private int spawnedObjectCount_;
        private bool networkReady_;

        // TODO save syncables
        //private VRCP_BufferManager bufferManager_;


        // Dummy method to get the static initializer to be called early on.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod() { }


        static VRCP_CyanEmuMain()
        {
            if (!VRCP_CyanEmuSettings.Instance.enableCyanEmu || FindObjectOfType<PipelineSaver>() != null || !Application.isPlaying)
            {
                return;
            }
            
            VRCStation.Initialize += VRCP_StationHelper.InitializeStations;
            VRCStation.useStationDelegate = VRCP_StationHelper.UseStation;
            VRCStation.exitStationDelegate = VRCP_StationHelper.ExitStation;

            VRC_UiShape.GetEventCamera = VRCP_PlayerController.GetPlayerCamera;
            VRC_Pickup.OnAwake = VRCP_PickupHelper.InitializePickup;
            VRC_Pickup.ForceDrop = VRCP_PickupHelper.ForceDrop;
            VRC_Pickup._GetCurrentPlayer = VRCP_PickupHelper.GetCurrentPlayer;
            VRC_Pickup._GetPickupHand = VRCP_PickupHelper.GetPickupHand;
            VRC_ObjectSpawn.Initialize = VRCP_ObjectSpawnHelper.InitializeSpawner;

#if UDON
            VRC.Udon.UdonBehaviour.OnInit = VRCP_UdonHelper.OnInit;
            VRC.Udon.UdonBehaviour.RunProgramAsRPCHook = VRCP_UdonHelper.RunProgramAsRPCHook;
#endif

#if VRC_SDK_VRCSDK2
            VRC_Trigger.InitializeTrigger = new Action<VRC_Trigger>(VRCP_TriggerHelper.InitializeTrigger);
            VRCSDK2.VRC_ObjectSync.Initialize += VRCP_ObjectSyncHelper.InitializeObjectSync;
            VRCSDK2.VRC_ObjectSync.TeleportHandler += VRCP_ObjectSyncHelper.TeleportTo;
            VRCSDK2.VRC_ObjectSync.RespawnHandler += VRCP_ObjectSyncHelper.RespawnObject;
            VRCSDK2.VRC_PlayerMods.Initialize = VRCP_PlayerModsHelper.InitializePlayerMods;
            VRCSDK2.VRC_SyncAnimation.Initialize = VRCP_SyncAnimationHelper.InitializationDelegate;
#endif

            Networking._IsMaster = VRCP_PlayerManager.IsLocalPlayerMaster;
            Networking._LocalPlayer = VRCP_PlayerManager.LocalPlayer;
            Networking._GetOwner = VRCP_PlayerManager.GetPlayerByGameObject;
            Networking._IsOwner = VRCP_PlayerManager.IsOwner;
            Networking._SetOwner = VRCP_PlayerManager.TakeOwnership;
            Networking._GetUniqueName = VRC.Tools.GetGameObjectPath;

            VRCPlayerApi._GetPlayerId = VRCP_PlayerManager.GetPlayerID;
            VRCPlayerApi._GetPlayerById = VRCP_PlayerManager.GetPlayerByID;
            VRCPlayerApi._isMasterDelegate = VRCP_PlayerManager.IsMaster;
            VRCPlayerApi.ClaimNetworkControl = VRCP_PlayerManager.ClaimNetworkControl;
            VRCPlayerApi._EnablePickups = VRCP_PlayerManager.EnablePickups;
            VRCPlayerApi._Immobilize = VRCP_PlayerManager.Immobilize;
            VRCPlayerApi._TeleportToOrientation = VRCP_PlayerManager.TeleportToOrientation;
            VRCPlayerApi._TeleportTo = VRCP_PlayerManager.TeleportTo;
            VRCPlayerApi._PlayHapticEventInHand = VRCP_PlayerManager.PlayHapticEventInHand;
            VRCPlayerApi._GetPlayerByGameObject = VRCP_PlayerManager.GetPlayerByGameObject;
            VRCPlayerApi._GetPickupInHand = VRCP_PlayerManager.GetPickupInHand;
            VRCPlayerApi._GetTrackingData = VRCP_PlayerManager.GetTrackingData;
            VRCPlayerApi._GetBoneTransform = VRCP_PlayerManager.GetBoneTransform;
            VRCPlayerApi._GetBonePosition = VRCP_PlayerManager.GetBonePosition;
            VRCPlayerApi._GetBoneRotation = VRCP_PlayerManager.GetBoneRotation;
            VRCPlayerApi._TakeOwnership = VRCP_PlayerManager.TakeOwnership;
            VRCPlayerApi._IsOwner = VRCP_PlayerManager.IsOwner;

            VRCPlayerApi._IsUserInVR = (VRCPlayerApi _) => false; // TODO one day...
            VRCPlayerApi._GetRunSpeed = VRCP_PlayerManager.GetRunSpeed;
            VRCPlayerApi._SetRunSpeed = VRCP_PlayerManager.SetRunSpeed;
            VRCPlayerApi._GetWalkSpeed = VRCP_PlayerManager.GetWalkSpeed;
            VRCPlayerApi._SetWalkSpeed = VRCP_PlayerManager.SetWalkSpeed;
            VRCPlayerApi._GetJumpImpulse = VRCP_PlayerManager.GetJumpImpulse;
            VRCPlayerApi._SetJumpImpulse = VRCP_PlayerManager.SetJumpImpulse;
            VRCPlayerApi._GetVelocity = VRCP_PlayerManager.GetVelocity;
            VRCPlayerApi._SetVelocity = VRCP_PlayerManager.SetVelocity;
            VRCPlayerApi._GetPosition = VRCP_PlayerManager.GetPosition;
            VRCPlayerApi._GetRotation = VRCP_PlayerManager.GetRotation;
            VRCPlayerApi._GetGravityStrength = VRCP_PlayerManager.GetGravityStrength;
            VRCPlayerApi._SetGravityStrength = VRCP_PlayerManager.SetGravityStrength;
            VRCPlayerApi.IsGrounded = VRCP_PlayerManager.IsGrounded;
            VRCPlayerApi._UseAttachedStation = VRCP_PlayerManager.UseAttachedStation;
            VRCPlayerApi._UseLegacyLocomotion = VRCP_PlayerManager.UseLegacyLocomotion;
            
            VRCPlayerApi._CombatSetup = VRCP_CombatSystemHelper.CombatSetup;
            VRCPlayerApi._CombatSetMaxHitpoints = VRCP_CombatSystemHelper.CombatSetMaxHitpoints;
            VRCPlayerApi._CombatGetCurrentHitpoints = VRCP_CombatSystemHelper.CombatGetCurrentHitpoints;
            VRCPlayerApi._CombatSetRespawn = VRCP_CombatSystemHelper.CombatSetRespawn;
            VRCPlayerApi._CombatSetDamageGraphic = VRCP_CombatSystemHelper.CombatSetDamageGraphic;
            VRCPlayerApi._CombatGetDestructible = VRCP_CombatSystemHelper.CombatGetDestructible;
            VRCPlayerApi._CombatSetCurrentHitpoints = VRCP_CombatSystemHelper.CombatSetCurrentHitpoints;

            GameObject executor = new GameObject(CYAN_EMU_GAMEOBJECT_NAME_);
            executor.tag = "EditorOnly";
            instance_ = executor.AddComponent<VRCP_CyanEmuMain>();
        }

        public static bool HasInstance()
        {
            return instance_ != null;
        }

        public static bool IsNetworkReady()
        {
            return instance_.networkReady_;
        }

        private void Awake()
        {
            if (instance_ != null)
            {
                this.LogError("Already have an instance of Trigger executor!");
                DestroyImmediate(this);
                return;
            }
            
            settings_ = VRCP_CyanEmuSettings.Instance;

            instance_ = this;
            DontDestroyOnLoad(this);


            VRCP_InputModule.DisableOtherInputModules();
            gameObject.AddComponent<VRCP_BaseInput>();
            gameObject.AddComponent<VRCP_InputModule>();

            descriptor_ = FindObjectOfType<VRC_SceneDescriptor>();
            if (descriptor_ == null)
            {
                Debug.LogWarning("There is no VRC_SceneDescriptor in the scene.");
            }

            // SDK manager class
#if VRC_SDK_VRCSDK2
            sdkManager_ = gameObject.AddComponent<VRCP_TriggerExecutor>();
#endif
#if UDON
            sdkManager_ = gameObject.AddComponent<VRCP_UdonManager>();
#endif

            StartCoroutine(OnNetworkReady());
        }

        private IEnumerator OnNetworkReady()
        {
            yield return new WaitForSeconds(0.5f);

            this.Log("Sending OnNetworkReady");
            sdkManager_.OnNetworkReady();

            if (settings_.spawnPlayer)
            {
                // TODO add option to allow for spawning remote players first to have data on not being master
                /*
                for (int i = 0; i < x; ++i)
                {
                    SpawnRemotePlayer();
                }
                */

                SpawnLocalPlayer();
            }

            yield return new WaitForSeconds(0.1f);

            networkReady_ = true;
            VRCP_PlayerManager.OnNetworkReady();
        }


        public static void SpawnPlayer(bool local)
        {
            if (local)
            {
                instance_?.SpawnLocalPlayer();
            }
            else
            {
                instance_?.SpawnRemotePlayer();
            }
        }

        private void SpawnLocalPlayer()
        {
            if (descriptor_ == null)
            {
                Debug.LogError("Cannot spawn player if there is no world descriptor!");
                return;
            }

            GameObject player = new GameObject("Player");
            player.transform.parent = transform;
            playerController_ = player.AddComponent<VRCP_PlayerController>();
            playerController_.Teleport(descriptor_.spawns[0], false);

            VRCP_Player playerObj = player.AddComponent<VRCP_Player>();
            playerObj.SetPlayer(VRCP_PlayerManager.CreateNewPlayer(true, player));
        }

        private void SpawnRemotePlayer()
        {
            if (descriptor_ == null)
            {
                Debug.LogError("Cannot spawn player if there is no world descriptor!");
                return;
            }

            GameObject player = new GameObject("Remote Player");
            player.transform.parent = transform;
            player.layer = LayerMask.NameToLayer("Player");
            // TODO do this better
            player.transform.position = descriptor_.spawns[0].position;
            player.transform.rotation = descriptor_.spawns[0].rotation;
            GameObject playerVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerVis.layer = player.layer;
            playerVis.transform.SetParent(player.transform, false);

            VRCP_Player playerObj = player.AddComponent<VRCP_Player>();
            playerObj.SetPlayer(VRCP_PlayerManager.CreateNewPlayer(false, player));
        }

        public static void RemovePlayer(VRCPlayerApi player)
        {
            VRCP_PlayerManager.RemovePlayer(player);
            Destroy(player.gameObject);
        }

        public static void PlayerJoined(VRCPlayerApi player)
        {
            instance_?.OnPlayerJoined(player);
        }

        private void OnPlayerJoined(VRCPlayerApi player)
        {
            sdkManager_.OnPlayerJoined(player);
        }

        public static void PlayerLeft(VRCPlayerApi player)
        {
            instance_?.OnPlayerLeft(player);
        }

        private void OnPlayerLeft(VRCPlayerApi player)
        {
            int masterID = VRCP_PlayerManager.GetMasterID();

            foreach (VRCP_SyncedObjectHelper sync in allSyncedObjects_)
            {
                VRCP_Syncable syncable = sync.GetComponent<VRCP_Syncable>();
                Debug.Assert(syncable != null, "VRCP_Main:OnPlayerLeft expected syncable component.");
                if (syncable.GetOwner() == player.playerId)
                {
                    syncable.SetOwner(masterID);
                }
            }

            sdkManager_.OnPlayerLeft(player);
        }

        public static GameObject SpawnObject(GameObject prefab)
        {
            return SpawnObject(prefab, Vector3.zero, Quaternion.identity);
        }

        public static GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject spawnedObject = Instantiate(prefab, position, rotation) as GameObject;
            spawnedObject.name = prefab.name + " (Dynamic Clone " + instance_.spawnedObjectCount_ + ")";
            spawnedObject.SetActive(true);
            spawnedObject.AddComponent<VRCP_SpawnHelper>();
            ++instance_.spawnedObjectCount_;

            instance_.OnSpawnedObject(spawnedObject);

            return spawnedObject;
        }

        private void OnSpawnedObject(GameObject spawnedObject)
        {
            sdkManager_.OnSpawnedObject(spawnedObject);
        }

        private void LateUpdate()
        {
            if (descriptor_ == null)
            {
                return;
            }

            if (playerController_ != null && playerController_.transform.position.y < descriptor_.RespawnHeightY)
            {
                playerController_.Teleport(descriptor_.spawns[0], false);
            }

            foreach (VRCP_SyncedObjectHelper sync in allSyncedObjects_)
            {
                if (sync.transform.position.y < descriptor_.RespawnHeightY)
                {
                    if (descriptor_.ObjectBehaviourAtRespawnHeight == VRC_SceneDescriptor.RespawnHeightBehaviour.Respawn)
                    {
                        sync.Respawn();
                    }
                    else
                    {
                        Destroy(sync.gameObject);
                    }
                }
            }
        }

        public static void AddSyncedObject(VRCP_SyncedObjectHelper sync)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.allSyncedObjects_.Add(sync);
        }

        public static void RemoveSyncedObject(VRCP_SyncedObjectHelper sync)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.allSyncedObjects_.Remove(sync);
        }
    }
}
