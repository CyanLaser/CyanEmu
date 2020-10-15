using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    public static class CyanEmuPlayerExtensions
    {
        public static CyanEmuPlayerController GetPlayerController(this VRCPlayerApi player)
        {
            return player.gameObject.GetComponent<CyanEmuPlayerController>();
        }
    }

    [AddComponentMenu("")]
    public class CyanEmuPlayer : MonoBehaviour
    {
        public VRCPlayerApi player;

        public void SetPlayer(VRCPlayerApi player)
        {
            this.player = player;

            // TODO handle this better
            CyanEmuPlayerController playerController = GetComponent<CyanEmuPlayerController>();
            if (playerController != null)
            {
                playerController.SetPlayer(this);
            }
        }
    }
}