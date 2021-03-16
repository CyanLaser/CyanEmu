using System.Collections.Generic;
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

        public static CyanEmuPlayer GetCyanEmuPlayer(this VRCPlayerApi player)
        {
            return player.gameObject.GetComponent<CyanEmuPlayer>();
        }
    }

    [AddComponentMenu("")]
    [SelectionBase]
    public class CyanEmuPlayer : MonoBehaviour
    {
        public VRCPlayerApi player;
        private Dictionary<string, string> tags = new Dictionary<string, string>();

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

        public void ClearTags()
        {
            tags.Clear();
        }

        public void SetTag(string tagName, string tagValue)
        {
            tags[tagName] = tagValue;
        }

        public string GetTag(string tagName)
        {
            if (tags.TryGetValue(tagName, out string tagValue))
            {
                return tagValue;
            }
            return "";
        }

        public bool HasTag(string tagName, string tagValue)
        {
            return GetTag(tagName) == tagValue;
        }
    }
}