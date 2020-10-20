using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VRC.SDKBase;


namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuMain : MonoBehaviour
    {
        private const string CYAN_EMU_GAMEOBJECT_NAME_ = "__CyanEmu";

        private static CyanEmuMain instance_;

        private ICyanEmuSDKManager sdkManager_;

        private CyanEmuSettings settings_;
        private CyanEmuPlayerController playerController_;
        private VRC_SceneDescriptor descriptor_;
        private HashSet<CyanEmuSyncedObjectHelper> allSyncedObjects_ = new HashSet<CyanEmuSyncedObjectHelper>();

        private int spawnedObjectCount_;
        private bool networkReady_;

        // TODO save syncables
        //private CyanEmuBufferManager bufferManager_;


        // Dummy method to get the static initializer to be called early on.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod() { }


        static CyanEmuMain()
        {
            if (!CyanEmuSettings.Instance.enableCyanEmu || FindObjectOfType<PipelineSaver>() != null || !Application.isPlaying)
            {
                return;
            }
            
            VRCStation.Initialize += CyanEmuStationHelper.InitializeStations;
            VRCStation.useStationDelegate = CyanEmuStationHelper.UseStation;
            VRCStation.exitStationDelegate = CyanEmuStationHelper.ExitStation;

            VRC_UiShape.GetEventCamera = CyanEmuPlayerController.GetPlayerCamera;
            VRC_Pickup.OnAwake = CyanEmuPickupHelper.InitializePickup;
            VRC_Pickup.ForceDrop = CyanEmuPickupHelper.ForceDrop;
            VRC_Pickup._GetCurrentPlayer = CyanEmuPickupHelper.GetCurrentPlayer;
            VRC_Pickup._GetPickupHand = CyanEmuPickupHelper.GetPickupHand;
            VRC_ObjectSpawn.Initialize = CyanEmuObjectSpawnHelper.InitializeSpawner;

#if UDON
            VRC.Udon.UdonBehaviour.OnInit = CyanEmuUdonHelper.OnInit;
            VRC.Udon.UdonBehaviour.SendCustomNetworkEventHook = CyanEmuUdonHelper.SendCustomNetworkEventHook;
#endif

#if VRC_SDK_VRCSDK2
            VRC_Trigger.InitializeTrigger = new Action<VRC_Trigger>(CyanEmuTriggerHelper.InitializeTrigger);
            VRCSDK2.VRC_ObjectSync.Initialize += CyanEmuObjectSyncHelper.InitializeObjectSync;
            VRCSDK2.VRC_ObjectSync.TeleportHandler += CyanEmuObjectSyncHelper.TeleportTo;
            VRCSDK2.VRC_ObjectSync.RespawnHandler += CyanEmuObjectSyncHelper.RespawnObject;
            VRCSDK2.VRC_PlayerMods.Initialize = CyanEmuPlayerModsHelper.InitializePlayerMods;
            VRCSDK2.VRC_SyncAnimation.Initialize = CyanEmuSyncAnimationHelper.InitializationDelegate;
#endif

            Networking._IsMaster = CyanEmuPlayerManager.IsLocalPlayerMaster;
            Networking._LocalPlayer = CyanEmuPlayerManager.LocalPlayer;
            Networking._GetOwner = CyanEmuPlayerManager.GetOwner;
            Networking._IsOwner = CyanEmuPlayerManager.IsOwner;
            Networking._SetOwner = CyanEmuPlayerManager.TakeOwnership;
            Networking._GetUniqueName = VRC.Tools.GetGameObjectPath;
            
            VRCPlayerApi._GetPlayerId = CyanEmuPlayerManager.GetPlayerID;
            VRCPlayerApi._GetPlayerById = CyanEmuPlayerManager.GetPlayerByID;
            VRCPlayerApi._isMasterDelegate = CyanEmuPlayerManager.IsMaster;
            VRCPlayerApi.ClaimNetworkControl = CyanEmuPlayerManager.ClaimNetworkControl;
            VRCPlayerApi._EnablePickups = CyanEmuPlayerManager.EnablePickups;
            VRCPlayerApi._Immobilize = CyanEmuPlayerManager.Immobilize;
            VRCPlayerApi._TeleportTo = CyanEmuPlayerManager.TeleportTo;
            VRCPlayerApi._TeleportToOrientation = CyanEmuPlayerManager.TeleportToOrientation;
            VRCPlayerApi._TeleportToOrientationLerp = CyanEmuPlayerManager.TeleportToOrientationLerp;
            VRCPlayerApi._PlayHapticEventInHand = CyanEmuPlayerManager.PlayHapticEventInHand;
            VRCPlayerApi._GetPlayerByGameObject = CyanEmuPlayerManager.GetPlayerByGameObject;
            VRCPlayerApi._GetPickupInHand = CyanEmuPlayerManager.GetPickupInHand;
            VRCPlayerApi._GetTrackingData = CyanEmuPlayerManager.GetTrackingData;
            VRCPlayerApi._GetBoneTransform = CyanEmuPlayerManager.GetBoneTransform;
            VRCPlayerApi._GetBonePosition = CyanEmuPlayerManager.GetBonePosition;
            VRCPlayerApi._GetBoneRotation = CyanEmuPlayerManager.GetBoneRotation;
            VRCPlayerApi._TakeOwnership = CyanEmuPlayerManager.TakeOwnership;
            VRCPlayerApi._IsOwner = CyanEmuPlayerManager.IsOwner;

            VRCPlayerApi._ClearPlayerTags = CyanEmuPlayerManager.ClearPlayerTag;
            VRCPlayerApi._SetPlayerTag = CyanEmuPlayerManager.SetPlayerTag;
            VRCPlayerApi._GetPlayerTag = CyanEmuPlayerManager.GetPlayerTag;
            VRCPlayerApi._GetPlayersWithTag = CyanEmuPlayerManager.GetPlayersWithTag;
            VRCPlayerApi._SetSilencedToTagged = CyanEmuPlayerManager.SetSilencedToTagged;
            VRCPlayerApi._SetSilencedToUntagged = CyanEmuPlayerManager.SetSilencedToUntagged;
            VRCPlayerApi._ClearSilence = CyanEmuPlayerManager.ClearSilence;
            VRCPlayerApi._SetInvisibleToTagged = CyanEmuPlayerManager.SetInvisibleToTagged;
            VRCPlayerApi._SetInvisibleToUntagged = CyanEmuPlayerManager.SetInvisibleToUntagged;
            VRCPlayerApi._ClearInvisible = CyanEmuPlayerManager.ClearInvisible;

            VRCPlayerApi._IsUserInVR = (VRCPlayerApi _) => false; // TODO one day...
            VRCPlayerApi._GetRunSpeed = CyanEmuPlayerManager.GetRunSpeed;
            VRCPlayerApi._SetRunSpeed = CyanEmuPlayerManager.SetRunSpeed;
            VRCPlayerApi._GetWalkSpeed = CyanEmuPlayerManager.GetWalkSpeed;
            VRCPlayerApi._SetWalkSpeed = CyanEmuPlayerManager.SetWalkSpeed;
            VRCPlayerApi._GetJumpImpulse = CyanEmuPlayerManager.GetJumpImpulse;
            VRCPlayerApi._SetJumpImpulse = CyanEmuPlayerManager.SetJumpImpulse;
            VRCPlayerApi._GetVelocity = CyanEmuPlayerManager.GetVelocity;
            VRCPlayerApi._SetVelocity = CyanEmuPlayerManager.SetVelocity;
            VRCPlayerApi._GetPosition = CyanEmuPlayerManager.GetPosition;
            VRCPlayerApi._GetRotation = CyanEmuPlayerManager.GetRotation;
            VRCPlayerApi._GetGravityStrength = CyanEmuPlayerManager.GetGravityStrength;
            VRCPlayerApi._SetGravityStrength = CyanEmuPlayerManager.SetGravityStrength;
            VRCPlayerApi.IsGrounded = CyanEmuPlayerManager.IsGrounded;
            VRCPlayerApi._UseAttachedStation = CyanEmuPlayerManager.UseAttachedStation;
            VRCPlayerApi._UseLegacyLocomotion = CyanEmuPlayerManager.UseLegacyLocomotion;
            
            VRCPlayerApi._CombatSetup = CyanEmuCombatSystemHelper.CombatSetup;
            VRCPlayerApi._CombatSetMaxHitpoints = CyanEmuCombatSystemHelper.CombatSetMaxHitpoints;
            VRCPlayerApi._CombatGetCurrentHitpoints = CyanEmuCombatSystemHelper.CombatGetCurrentHitpoints;
            VRCPlayerApi._CombatSetRespawn = CyanEmuCombatSystemHelper.CombatSetRespawn;
            VRCPlayerApi._CombatSetDamageGraphic = CyanEmuCombatSystemHelper.CombatSetDamageGraphic;
            VRCPlayerApi._CombatGetDestructible = CyanEmuCombatSystemHelper.CombatGetDestructible;
            VRCPlayerApi._CombatSetCurrentHitpoints = CyanEmuCombatSystemHelper.CombatSetCurrentHitpoints;

            GameObject executor = new GameObject(CYAN_EMU_GAMEOBJECT_NAME_);
            executor.tag = "EditorOnly";
            instance_ = executor.AddComponent<CyanEmuMain>();
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
            
            settings_ = CyanEmuSettings.Instance;

            instance_ = this;
            DontDestroyOnLoad(this);


            CyanEmuInputModule.DisableOtherInputModules();
            gameObject.AddComponent<CyanEmuBaseInput>();
            gameObject.AddComponent<CyanEmuInputModule>();

            descriptor_ = FindObjectOfType<VRC_SceneDescriptor>();
            if (descriptor_ == null)
            {
                Debug.LogWarning("There is no VRC_SceneDescriptor in the scene.");
            }

            // SDK manager class
#if VRC_SDK_VRCSDK2
            sdkManager_ = gameObject.AddComponent<CyanEmuTriggerExecutor>();
#endif
#if UDON
            sdkManager_ = gameObject.AddComponent<CyanEmuUdonManager>();
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
            CyanEmuPlayerManager.OnNetworkReady();
        }


        public static void SpawnPlayer(bool local, string name = null)
        {
            if (local)
            {
                instance_?.SpawnLocalPlayer();
            }
            else
            {
                instance_?.SpawnRemotePlayer(name);
            }
        }

        private void SpawnLocalPlayer()
        {
            if (descriptor_ == null)
            {
                Debug.LogError("Cannot spawn player if there is no world descriptor!");
                return;
            }

            GameObject player = new GameObject("Local Player");
            player.transform.parent = transform;

            // Force move the player initially to the spawn point to prevent enter triggers at the origin
            player.transform.position = descriptor_.spawns[0].position;
            player.transform.rotation = descriptor_.spawns[0].rotation;

            playerController_ = player.AddComponent<CyanEmuPlayerController>();
            playerController_.Teleport(descriptor_.spawns[0], false);

            CyanEmuPlayer playerObj = player.AddComponent<CyanEmuPlayer>();
            VRCPlayerApi playerAPI = CyanEmuPlayerManager.CreateNewPlayer(true, player, settings_.customLocalPlayerName);
            playerObj.SetPlayer(playerAPI);
            player.name = $"[{playerAPI.playerId}] {player.name}";
        }

        private void SpawnRemotePlayer(string name = null)
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

            CyanEmuPlayer playerObj = player.AddComponent<CyanEmuPlayer>();
            VRCPlayerApi playerAPI = CyanEmuPlayerManager.CreateNewPlayer(false, player, name);
            playerObj.SetPlayer(playerAPI);
            player.name = $"[{playerAPI.playerId}] {player.name}";
        }

        public static void RemovePlayer(VRCPlayerApi player)
        {
            CyanEmuPlayerManager.RemovePlayer(player);
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
            int masterID = CyanEmuPlayerManager.GetMasterID();
            VRCPlayerApi masterPlayer = VRCPlayerApi.GetPlayerById(masterID);

            foreach (CyanEmuSyncedObjectHelper sync in allSyncedObjects_)
            {
                GameObject syncObj = sync.gameObject;
                if (Networking.GetOwner(syncObj)?.playerId == player.playerId)
                {
                    Networking.SetOwner(masterPlayer, syncObj);
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
            spawnedObject.AddComponent<CyanEmuSpawnHelper>();
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

            foreach (CyanEmuSyncedObjectHelper sync in allSyncedObjects_)
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

        public static void AddSyncedObject(CyanEmuSyncedObjectHelper sync)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.allSyncedObjects_.Add(sync);
        }

        public static void RemoveSyncedObject(CyanEmuSyncedObjectHelper sync)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.allSyncedObjects_.Remove(sync);
        }
    }
}
