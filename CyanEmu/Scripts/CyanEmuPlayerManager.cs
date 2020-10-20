﻿using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public class CyanEmuPlayerManager
    {
        // Player Manager system
        private static int masterID = -1;
        private static int localPlayerID = -1;
        private static int nextPlayerID = 1;
        private static readonly Dictionary<VRCPlayerApi, int> playerIDs = new Dictionary<VRCPlayerApi, int>();
        private static readonly Dictionary<int, VRCPlayerApi> players = new Dictionary<int, VRCPlayerApi>();

        private static readonly List<VRCPlayerApi> waitingPlayers = new List<VRCPlayerApi>();
        private static bool networkReady = false;

        public static void InitializePlayer(VRCPlayerApi player)
        {
            int id = nextPlayerID;
            player.Log("Assigning player id " + id);
            ++nextPlayerID;

            playerIDs[player] = id;
            players[id] = player;
            VRCPlayerApi.AllPlayers.Add(player);

            if (masterID == -1)
            {
                player.Log("Player is now master");
                masterID = id;
                Debug.Assert(player.isMaster, "VRCP_PlayerManager:InitializePlayer Player should be considered master!");
            }
            else
            {
                Debug.Assert(!player.isMaster, "VRCP_PlayerManager:InitializePlayer Player should not be considered master!");
            }

            Debug.Assert(player.playerId == id, "VRCP_PlayerManager:InitializePlayer Player's id does not match assigned id!");

            if (networkReady)
            {
                CyanEmuMain.PlayerJoined(player);
            }
            else
            {
                waitingPlayers.Add(player);
            }
        }

        public static void OnNetworkReady()
        {
            networkReady = true;
            foreach (var player in waitingPlayers)
            {
                CyanEmuMain.PlayerJoined(player);
            }

            waitingPlayers.Clear();
        }

        public static VRCPlayerApi CreateNewPlayer(bool local, GameObject playerObj, string name = null)
        {
            VRCPlayerApi player = new VRCPlayerApi();
            player.gameObject = playerObj;

            player.displayName = (string.IsNullOrEmpty(name) ? (local ? "Local" : "Remote") +" Player " + nextPlayerID : name);
            player.isLocal = local;
            InitializePlayer(player);

            if (local)
            {
                localPlayerID = player.playerId;
            }
            Debug.Assert(player.isLocal == local, "VRCP_PlayerManager:CreateNewPlayer New player does not match local settings!");

            return player;
        }

        public static void RemovePlayer(VRCPlayerApi player)
        {
            playerIDs.Remove(player);
            players.Remove(player.playerId);

            VRCPlayerApi.AllPlayers.Remove(player);
            
            if (masterID == player.playerId)
            {
                masterID = -1;
                if (VRCPlayerApi.AllPlayers.Count != 0)
                {
                    masterID = VRCPlayerApi.AllPlayers[0].playerId;
                }
            }

            CyanEmuMain.PlayerLeft(player);
        }

        public static int GetMasterID()
        {
            return masterID;
        }

        public static VRCPlayerApi LocalPlayer()
        {
            return players[localPlayerID];
        }

        public static VRCPlayerApi GetPlayerByID(int playerID)
        {
            return players[playerID];
        }

        public static int GetPlayerID(VRCPlayerApi player)
        {
            return playerIDs[player];
        }

        public static bool IsMaster(VRCPlayerApi player)
        {
            return GetPlayerID(player) == masterID;
        }

        public static bool IsLocalPlayerMaster()
        {
            return localPlayerID == masterID;
        }

        public static void EnablePickups(VRCPlayerApi player, bool enabled)
        {
            // TODO
        }

        public static void Immobilize(VRCPlayerApi player, bool immobilized)
        {
            // TODO
        }

        public static void TeleportToOrientationLerp(VRCPlayerApi player, Vector3 position, Quaternion rotation, VRC_SceneDescriptor.SpawnOrientation orientation, bool lerp)
        {
            // Ignore lerp since there is no networking here
            TeleportToOrientation(player, position, rotation, orientation);
        }

        public static void TeleportToOrientation(VRCPlayerApi player, Vector3 position, Quaternion rotation, VRC_SceneDescriptor.SpawnOrientation orientation)
        {
            if (!player.isLocal)
            {
                return;
            }
            player.GetPlayerController().Teleport(position, rotation, orientation == VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint);
        }

        public static void TeleportTo(VRCPlayerApi player, Vector3 position, Quaternion rotation)
        {
            if (!player.isLocal)
            {
                return;
            }
            player.GetPlayerController().Teleport(position, rotation, false);
        }

        public static void PlayHapticEventInHand(VRCPlayerApi player, VRC_Pickup.PickupHand hand, float f1, float f2, float f3)
        {
            // TODO
        }

        public static void ClaimNetworkControl(VRCPlayerApi player, VRC_ObjectApi obj)
        {
            // TODO Is this necessary? 
        }

        public static VRCPlayerApi GetPlayerByGameObject(GameObject obj)
        {
            CyanEmuPlayer player = obj.GetComponentInParent<CyanEmuPlayer>();
            if (player != null)
            {
                return player.player;
            }
            return null;
        }

        public static VRCPlayerApi GetOwner(GameObject obj)
        {
            ICyanEmuSyncable sync = obj.GetComponent<ICyanEmuSyncable>();

            int playerID = sync != null ? sync.GetOwner() : masterID;

            if (!players.TryGetValue(playerID, out VRCPlayerApi player))
            {
                return null;
            }
            return player;
        }

        public static VRC_Pickup GetPickupInHand(VRCPlayerApi player, VRC_Pickup.PickupHand hand)
        {
            return player.GetPlayerController().GetHeldPickup(hand);
        }

        public static VRCPlayerApi.TrackingData GetTrackingData(VRCPlayerApi player, VRCPlayerApi.TrackingDataType trackingDataType)
        {
            return player.GetPlayerController().GetTrackingData(trackingDataType);
        }

        public static void TakeOwnership(VRCPlayerApi player, GameObject obj)
        {
            VRCPlayerApi prevOwner = GetOwner(obj);
            if (prevOwner == player)
            {
                return;
            }

            ICyanEmuSyncable[] syncs = obj.GetComponents<ICyanEmuSyncable>();
            foreach (ICyanEmuSyncable sync in syncs)
            {
                sync.SetOwner(player.playerId);
            }

            ICyanEmuSyncableHandler[] syncHandlers = obj.GetComponents<ICyanEmuSyncableHandler>();
            foreach (ICyanEmuSyncableHandler syncHandler in syncHandlers)
            {
                syncHandler.OnOwnershipTransferred(player.playerId);
            }
        }

        public static bool IsOwner(VRCPlayerApi player, GameObject obj)
        {
            ICyanEmuSyncable sync = obj.GetComponent<ICyanEmuSyncable>();
            int owner = sync == null ? masterID : sync.GetOwner();
            return owner == player.playerId;
        }

        public static float GetRunSpeed(VRCPlayerApi player)
        {
            if (!player.isLocal)
            {
                return 0;
            }
            return player.GetPlayerController().GetRunSpeed();
        }

        public static void SetRunSpeed(VRCPlayerApi player, float speed)
        {
            if (!player.isLocal)
            {
                return;
            }
            player.GetPlayerController().SetRunSpeed(speed);
        }

        public static float GetWalkSpeed(VRCPlayerApi player)
        {
            if (!player.isLocal)
            {
                return 0;
            }
            return player.GetPlayerController().GetWalkSpeed();
        }

        public static void SetWalkSpeed(VRCPlayerApi player, float speed)
        {
            if (!player.isLocal)
            {
                return;
            }
            player.GetPlayerController().SetWalkSpeed(speed);
        }

        public static float GetJumpImpulse(VRCPlayerApi player)
        {
            if (!player.isLocal)
            {
                return 0;
            }
            return player.GetPlayerController().GetJump();
        }

        public static void SetJumpImpulse(VRCPlayerApi player, float jump)
        {
            if (!player.isLocal)
            {
                return;
            }
            player.GetPlayerController().SetJump(jump);
        }

        public static float GetGravityStrength(VRCPlayerApi player)
        {
            if (!player.isLocal)
            {
                return 0;
            }
            return player.GetPlayerController().GetGravityStrength();
        }
        
        public static void SetGravityStrength(VRCPlayerApi player, float gravity)
        {
            if (!player.isLocal)
            {
                return;
            }
            player.GetPlayerController().SetGravityStrength(gravity);
        }

        public static Vector3 GetVelocity(VRCPlayerApi player)
        {
            if (!player.isLocal)
            {
                return Vector3.zero;
            }
            return player.GetPlayerController().GetVelocity();
        }

        public static void SetVelocity(VRCPlayerApi player, Vector3 velocity)
        {
            if (!player.isLocal)
            {
                return;
            }
            player.GetPlayerController().SetVelocity(velocity);
        }
        
        public static Vector3 GetPosition(VRCPlayerApi player)
        {
            return player.gameObject.transform.position;
        }

        public static Quaternion GetRotation(VRCPlayerApi player)
        {
            return player.gameObject.transform.rotation;
        }

        public static bool IsGrounded(VRCPlayerApi player)
        {
            if (!player.isLocal)
            {
                return false;
            }
            return player.GetPlayerController().IsGrounded();
        }

        public static void UseAttachedStation(VRCPlayerApi player)
        {
            if (!player.isLocal)
            {
                return;
            }

            GameObject obj = null;
#if UDON
            // This is dumb
            obj = VRC.Udon.UdonManager.Instance.currentlyExecuting.gameObject;
#endif            
            obj?.GetComponent<CyanEmuStationHelper>()?.UseStation();
        }

        public static void UseLegacyLocomotion(VRCPlayerApi player)
        {
            if (!player.isLocal)
            {
                return;
            }
            player.GetPlayerController().UseLegacyLocomotion();
        }


        public static Quaternion GetBoneRotation(VRCPlayerApi player, HumanBodyBones bone)
        {
            return player.gameObject.transform.rotation;
        }

        public static Vector3 GetBonePosition(VRCPlayerApi player, HumanBodyBones bone)
        {
            return player.gameObject.transform.position;
        }

        public static Transform GetBoneTransform(VRCPlayerApi player, HumanBodyBones bone)
        {
            // TODO
            return null;
        }

        public static List<int> GetPlayersWithTag(string tagName, string tagValue)
        {
            List<int> players = new List<int>();
            foreach (var player in VRCPlayerApi.AllPlayers)
            {
                if (player.GetCyanEmuPlayer().HasTag(tagName, tagValue))
                {
                    players.Add(player.playerId);
                }
            }
            return players;
        }

        public static void ClearPlayerTag(VRCPlayerApi player)
        {
            player.GetCyanEmuPlayer().ClearTags();
        }

        public static void SetPlayerTag(VRCPlayerApi player, string tagName, string tagValue)
        {
            player.GetCyanEmuPlayer().SetTag(tagName, tagValue);
        }

        public static string GetPlayerTag(VRCPlayerApi player, string tagName)
        {
            return player.GetCyanEmuPlayer().GetTag(tagName);
        }


        public static void ClearSilence(VRCPlayerApi player)
        {
            // TODO?
        }

        public static void SetSilencedToUntagged(VRCPlayerApi player, int number, string tagName, string tagValue)
        {
            // TODO?
        }

        public static void SetSilencedToTagged(VRCPlayerApi player, int number, string tagName, string tagValue)
        {
            // TODO?
        }


        public static void ClearInvisible(VRCPlayerApi player)
        {
            // TODO?
        }

        public static void SetInvisibleToUntagged(VRCPlayerApi player, bool invisible, string tagName, string tagValue)
        {
            // TODO?
        }

        public static void SetInvisibleToTagged(VRCPlayerApi player, bool invisible, string tagName, string tagValue)
        {
            // TODO?
        }


        /*
        TODO all the interfaces:
        
        public static ClaimNetworkControlDelegate ClaimNetworkControl;
        public static GetLookRayDelegate GetLookRay;

        public static Action<VRCPlayerApi, RuntimeAnimatorController> _PushAnimations;
        public static Action<VRCPlayerApi> _PopAnimations;
        public static SetAnimatorBoolDelegate SetAnimatorBool;

        public static Func<VRCPlayerApi, bool> _isModeratorDelegate;
        public static Func<VRCPlayerApi, bool> _isSuperDelegate;

        public static Action<VRCPlayerApi, Color> _SetNamePlateColor;
        public static Action<VRCPlayerApi> _RestoreNamePlateColor;
        public static Action<VRCPlayerApi, bool> _SetNamePlateVisibility;
        public static Action<VRCPlayerApi> _RestoreNamePlateVisibility;
        */
    }
}
 