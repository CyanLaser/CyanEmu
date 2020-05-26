// VRCP_Player
// Created by CyanLaser

using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public static class VRCP_PlayerExtensions
    {
        public static VRCP_PlayerController GetPlayerController(this VRCPlayerApi player)
        {
            return player.gameObject.GetComponent<VRCP_PlayerController>();
        }
    }

    [AddComponentMenu("")]
    public class VRCP_Player : MonoBehaviour
    {
        public VRCPlayerApi player;

        public void SetPlayer(VRCPlayerApi player)
        {
            this.player = player;

            // TODO handle this better
            VRCP_PlayerController playerController = GetComponent<VRCP_PlayerController>();
            if (playerController != null)
            {
                playerController.SetPlayer(this);
            }
        }
    }
}