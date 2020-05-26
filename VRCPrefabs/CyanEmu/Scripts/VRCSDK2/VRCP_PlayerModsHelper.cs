// VRCP_PlayerModsHelper
// Created by CyanLaser

#if VRC_SDK_VRCSDK2

using UnityEngine;
using VRCSDK2;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_PlayerModsHelper : MonoBehaviour
    {
        public static VRCP_PlayerModsHelper roomMods;

        private VRC_PlayerMods mods_;

        private static float defaultWalkSpeed_ = VRCP_PlayerController.DEFAULT_WALK_SPEED_;
        private static float defaultRunSpeed_ = VRCP_PlayerController.DEFAULT_RUN_SPEED_;
        private static float defaultStrafeSpeed_ = VRCP_PlayerController.DEFAULT_WALK_SPEED_;
        private static float defaultJumpSpeed_ = 0;

        public static void InitializePlayerMods(VRC_PlayerMods mods)
        {
            VRCP_PlayerModsHelper helper = mods.gameObject.AddComponent<VRCP_PlayerModsHelper>();
            helper.mods_ = mods;
            if (mods.isRoomPlayerMods)
            {
                if (roomMods != null)
                {
                    helper.LogWarning("Multiple room player mods!");
                }

                roomMods = helper;
            }
        }

        public static void ApplyRoomMods(VRCP_PlayerController player)
        {
            if (roomMods != null)
            {
                roomMods.AddPlayerMods();
            }
        }
        
        public void AddPlayerMods()
        {
            this.Log("Adding player mods");
            AddPlayerMods(mods_);
        }

        public void RemovePlayerMods()
        {
            this.Log("Removing player mods");
            RemoveMods();
        }

        public void AddPlayerMods(VRC_PlayerMods mods)
        {
            VRCP_PlayerController player = VRCP_PlayerController.instance;
            if (player == null)
            {
                return;
            }

            foreach (VRCPlayerMod mod in mods.playerMods)
            {
                if (mod.name == "jump")
                {
                    player.SetJump(mod.properties[0].floatValue);

                    if (mods.isRoomPlayerMods)
                    {
                        defaultJumpSpeed_ = mod.properties[0].floatValue;
                    }
                }
                else if (mod.name == "speed")
                {
                    player.SetRunSpeed(mod.properties[0].floatValue);
                    player.SetWalkSpeed(mod.properties[1].floatValue);
                    player.SetStrafeSpeed(mod.properties[2].floatValue);

                    if (mods.isRoomPlayerMods)
                    {
                        defaultRunSpeed_ = mod.properties[0].floatValue;
                        defaultWalkSpeed_ = mod.properties[1].floatValue;
                        defaultStrafeSpeed_ = mod.properties[2].floatValue;
                    }
                }
            }
        }

        public void RemoveMods()
        {
            VRCP_PlayerController player = VRCP_PlayerController.instance;
            if (player == null)
            {
                return;
            }

            player.SetRunSpeed(defaultRunSpeed_);
            player.SetWalkSpeed(defaultWalkSpeed_);
            player.SetStrafeSpeed(defaultStrafeSpeed_);
            player.SetJump(defaultJumpSpeed_);
        }
    }
}

#endif