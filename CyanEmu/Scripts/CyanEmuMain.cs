using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;


namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuMain : MonoBehaviour
    {
        private const string CYAN_EMU_GAMEOBJECT_NAME_ = "__CyanEmu";
        private const string EDITOR_ONLY_TAG_ = "EditorOnly";

        private static CyanEmuMain instance_;

        private ICyanEmuSDKManager sdkManager_;

        private CyanEmuSettings settings_;
        private CyanEmuPlayerController playerController_;
        private VRC_SceneDescriptor descriptor_;
        private Transform proxyObjectParents_;

        private bool shouldVerifySyncedObjectList_;
        private readonly Queue<CyanEmuSyncedObjectHelper> toBeAddedSync_ = new Queue<CyanEmuSyncedObjectHelper>();
        private readonly Queue<CyanEmuSyncedObjectHelper> toBeRemovedSync_ = new Queue<CyanEmuSyncedObjectHelper>();
        private HashSet<CyanEmuSyncedObjectHelper> allSyncedObjects_ = new HashSet<CyanEmuSyncedObjectHelper>();

        private int spawnedObjectCount_;
        private bool networkReady_;

        // TODO save syncables
        //private CyanEmuBufferManager bufferManager_;


        // Dummy method to get the static initializer to be called early on.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            if (!CyanEmuEnabled())
            {
                return;
            }

            DestroyEditorOnly();
        }

        static CyanEmuMain()
        {
            if (!CyanEmuEnabled())
            {
                return;
            }

            LinkAPI();
            CreateInstance();
        }

        private static bool CyanEmuEnabled()
        {
            return 
                CyanEmuSettings.Instance.enableCyanEmu &&
                FindObjectOfType<PipelineSaver>() == null && 
                Application.isPlaying;
        }

        private static void LinkAPI()
        {
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
            
            // This is no longer used as of SDK 2021.03.22.18.27
            VRC.Udon.UdonBehaviour.SendCustomNetworkEventHook = CyanEmuUdonHelper.SendCustomNetworkEventHook;
#endif

#if VRC_SDK_VRCSDK2
            VRC_Trigger.InitializeTrigger = CyanEmuTriggerHelper.InitializeTrigger;
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

            VRC_SpatialAudioSource.Initialize = CyanEmuSpatialAudioHelper.InitializeAudio;

            // New methods added. Try not to break older sdks
            // TODO figure out a better way...

            // 2020-10-27 
            var _SetAvatarAudioVolumetricRadius = typeof(VRCPlayerApi).GetField("_SetAvatarAudioVolumetricRadius", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetAvatarAudioVolumetricRadius != null) _SetAvatarAudioVolumetricRadius.SetValue(null, (Action<VRCPlayerApi, float>)CyanEmuPlayerManager.SetAvatarAudioVolumetricRadius);

            var _SetAvatarAudioNearRadius = typeof(VRCPlayerApi).GetField("_SetAvatarAudioNearRadius", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetAvatarAudioNearRadius != null) _SetAvatarAudioNearRadius.SetValue(null, (Action<VRCPlayerApi, float>)CyanEmuPlayerManager.SetAvatarAudioNearRadius);

            var _SetAvatarAudioFarRadius = typeof(VRCPlayerApi).GetField("_SetAvatarAudioFarRadius", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetAvatarAudioFarRadius != null) _SetAvatarAudioFarRadius.SetValue(null, (Action<VRCPlayerApi, float>)CyanEmuPlayerManager.SetAvatarAudioFarRadius);

            var _SetAvatarAudioGain = typeof(VRCPlayerApi).GetField("_SetAvatarAudioGain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetAvatarAudioGain != null) _SetAvatarAudioGain.SetValue(null, (Action<VRCPlayerApi, float>)CyanEmuPlayerManager.SetAvatarAudioGain);

            var _SetVoiceLowpass = typeof(VRCPlayerApi).GetField("_SetVoiceLowpass", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetVoiceLowpass != null) _SetVoiceLowpass.SetValue(null, (Action<VRCPlayerApi, bool>)CyanEmuPlayerManager.SetVoiceLowpass);

            var _SetVoiceVolumetricRadius = typeof(VRCPlayerApi).GetField("_SetVoiceVolumetricRadius", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetVoiceVolumetricRadius != null) _SetVoiceVolumetricRadius.SetValue(null, (Action<VRCPlayerApi, float>)CyanEmuPlayerManager.SetVoiceVolumetricRadius);

            var _SetVoiceDistanceFar = typeof(VRCPlayerApi).GetField("_SetVoiceDistanceFar", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetVoiceDistanceFar != null) _SetVoiceDistanceFar.SetValue(null, (Action<VRCPlayerApi, float>)CyanEmuPlayerManager.SetVoiceDistanceFar);

            var _SetVoiceDistanceNear = typeof(VRCPlayerApi).GetField("_SetVoiceDistanceNear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetVoiceDistanceNear != null) _SetVoiceDistanceNear.SetValue(null, (Action<VRCPlayerApi, float>)CyanEmuPlayerManager.SetVoiceDistanceNear);

            var _SetVoiceGain = typeof(VRCPlayerApi).GetField("_SetVoiceGain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetVoiceGain != null) _SetVoiceGain.SetValue(null, (Action<VRCPlayerApi, float>)CyanEmuPlayerManager.SetVoiceGain);

            
            // 2020-11-16
            var _GetStrafeSpeed = typeof(VRCPlayerApi).GetField("_GetStrafeSpeed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_GetStrafeSpeed != null) _GetStrafeSpeed.SetValue(null, (Func<VRCPlayerApi, float>)CyanEmuPlayerManager.GetStrafeSpeed);

            var _SetStrafeSpeed = typeof(VRCPlayerApi).GetField("_SetStrafeSpeed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (_SetStrafeSpeed != null) _SetStrafeSpeed.SetValue(null, (Action<VRCPlayerApi, float>)CyanEmuPlayerManager.SetStrafeSpeed);
        }

        private static void CreateInstance()
        {
            GameObject executor = new GameObject(CYAN_EMU_GAMEOBJECT_NAME_);
            executor.tag = EDITOR_ONLY_TAG_;
            instance_ = executor.AddComponent<CyanEmuMain>();
        }

        private static void DestroyEditorOnly()
        {
            if (!CyanEmuSettings.Instance.deleteEditorOnly)
            {
                return;
            }

            List<GameObject> rootObjects = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            Queue<GameObject> queue = new Queue<GameObject>(rootObjects);
            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                if (obj.tag == EDITOR_ONLY_TAG_)
                {
                    obj.Log("Deleting editor only object: " + VRC.Tools.GetGameObjectPath(obj));
                    DestroyImmediate(obj);
                }
                else
                {
                    for (int child = 0; child < obj.transform.childCount; ++child)
                    {
                        queue.Enqueue(obj.transform.GetChild(child).gameObject);
                    }
                }
            }
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
                this.LogError("Already have an instance of CyanEmu!");
                DestroyImmediate(gameObject);
                return;
            }
            
            settings_ = CyanEmuSettings.Instance;

            instance_ = this;
            DontDestroyOnLoad(this);

            proxyObjectParents_ = new GameObject(CYAN_EMU_GAMEOBJECT_NAME_ + "ProxyObjects").transform;
            DontDestroyOnLoad(proxyObjectParents_);

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
            networkReady_ = true;
            
            yield return new WaitForSeconds(0.1f);
            CyanEmuPlayerManager.OnNetworkReady();
        }

        public static Transform GetProxyObjectTransform()
        {
            return instance_.proxyObjectParents_;
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
            Transform spawn = GetSpawnPoint();
            player.transform.position = spawn.position;
            player.transform.rotation = Quaternion.Euler(0, spawn.rotation.eulerAngles.y, 0); 

            playerController_ = player.AddComponent<CyanEmuPlayerController>();
            playerController_.Teleport(spawn, false);

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
            Transform spawn = GetSpawnPoint();
            player.transform.position = spawn.position;
            player.transform.rotation = Quaternion.Euler(0, spawn.rotation.eulerAngles.y, 0);

            GameObject playerVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playerVis.layer = player.layer;
            playerVis.transform.SetParent(player.transform, false);

            CyanEmuPlayer playerObj = player.AddComponent<CyanEmuPlayer>();
            VRCPlayerApi playerAPI = CyanEmuPlayerManager.CreateNewPlayer(false, player, name);
            playerObj.SetPlayer(playerAPI);
            player.name = $"[{playerAPI.playerId}] {player.name}";
        }

        private Transform GetSpawnPoint()
        {
            if (descriptor_.spawns.Length == 0 || descriptor_.spawns[0] == null)
            {
                throw new Exception("[CyanEmuMain] Cannot spawn player when descriptor does not have a spawn set!");
            }

            return descriptor_.spawns[0];
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
                if (sync == null)
                {
                    continue;
                }
                
                GameObject syncObj = sync.gameObject;
                if (Networking.GetOwner(syncObj)?.playerId == player.playerId)
                {
                    Networking.SetOwner(masterPlayer, syncObj);
                }
            }

            sdkManager_.OnPlayerLeft(player);
        }

        public static void PlayerRespawned(VRCPlayerApi player)
        {
            instance_?.OnPlayerRespawn(player);
        }

        private void OnPlayerRespawn(VRCPlayerApi player)
        {
            sdkManager_.OnPlayerRespawn(player);
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
            
            ProcessAddedAndRemovedSyncedObjects();
            ProcessSyncedObjectsBelowRespawn();
        }

        private void ProcessSyncedObjectsBelowRespawn()
        {
            if (playerController_ != null && playerController_.transform.position.y < descriptor_.RespawnHeightY)
            {
                playerController_.Teleport(descriptor_.spawns[0], false);
            }
            
            // TODO space this out so that there are only x number per frame instead of all every time? 
            List<GameObject> objsToDestroy = new List<GameObject>();
            foreach (CyanEmuSyncedObjectHelper sync in allSyncedObjects_)
            {
                if (sync == null)
                {
                    shouldVerifySyncedObjectList_ = true;
                    Debug.LogWarning("Null Synced Object!");
                    continue;
                }
                
                if (!sync.SyncPosition)
                {
                    continue;
                }
                
                if (sync.transform.position.y < descriptor_.RespawnHeightY)
                {
                    if (descriptor_.ObjectBehaviourAtRespawnHeight == VRC_SceneDescriptor.RespawnHeightBehaviour.Respawn)
                    {
                        sync.Respawn();
                    }
                    else
                    {
                        objsToDestroy.Add(sync.gameObject);
                    }
                }
            }

            foreach (var obj in objsToDestroy)
            {
                Destroy(obj);
            }
        }
        
        public static void AddSyncedObject(CyanEmuSyncedObjectHelper sync)
        {
            if (instance_ == null || sync == null)
            {
                return;
            }

            instance_.QueueAddSyncedObject(sync);
        }

        public static void RemoveSyncedObject(CyanEmuSyncedObjectHelper sync)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.QueueRemoveSyncedObject(sync);
        }
        
        private void QueueAddSyncedObject(CyanEmuSyncedObjectHelper syncedObject)
        {
            if (syncedObject == null)
            {
                return;
            }
            toBeAddedSync_.Enqueue(syncedObject);
        }
        
        private void QueueRemoveSyncedObject(CyanEmuSyncedObjectHelper syncedObject)
        {
            shouldVerifySyncedObjectList_ = true;
            toBeRemovedSync_.Enqueue(syncedObject);
        }
        
        private void ProcessAddedAndRemovedSyncedObjects()
        {
            if (toBeAddedSync_.Count > 0)
            {
                foreach (var sync in toBeAddedSync_)
                {
                    if (sync == null)
                    {
                        shouldVerifySyncedObjectList_ = true;
                        continue;
                    }
                    allSyncedObjects_.Add(sync);
                }
                toBeAddedSync_.Clear();
            }
            if (toBeRemovedSync_.Count > 0)
            {
                foreach (var udon in toBeRemovedSync_)
                {
                    if (udon == null)
                    {
                        shouldVerifySyncedObjectList_ = true;
                        continue;
                    }
                    allSyncedObjects_.Remove(udon);
                }
                toBeRemovedSync_.Clear();
            }

            if (shouldVerifySyncedObjectList_)
            {
                HashSet<CyanEmuSyncedObjectHelper> allSyncs = new HashSet<CyanEmuSyncedObjectHelper>();
                foreach (var sync in allSyncedObjects_)
                {
                    if (sync == null)
                    {
                        continue;
                    }
                    allSyncs.Add(sync);
                }

                allSyncedObjects_ = allSyncs;
            }
        }
    }
}
