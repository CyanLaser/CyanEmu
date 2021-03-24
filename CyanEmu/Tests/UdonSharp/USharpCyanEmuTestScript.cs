
#if UDON

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace CyanEmu.UdonSharp
{
    [AddComponentMenu("")]
    public class USharpCyanEmuTestScript : UdonSharpBehaviour
    {
        private VRCPlayerApi _localPlayer;

        void Start()
        {
            _localPlayer = Networking.LocalPlayer;

            TestResults(TestGettingPlayerInfo());

            // TestResults(TestPlayerTags());
            // TestResults(TestPlayerAudioSettings(_localPlayer));
            // TestResults(TestPlayerLocomotionSettings(_localPlayer));
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            Debug.Log("Testing OnPlayerJoined. Local: " + player.isLocal);
            
            TestResults(TestPlayerTags(player));
            TestResults(TestPlayerAudioSettings(player));
            TestResults(TestOtherPlayerSettings(player)); 
            TestResults(TestPlayerLocomotionSettings(player));
        }

        private void TestResults(bool value)
        {
            if (value)
            {
                Debug.Log("Pass");
            }
            else
            {
                Debug.LogError("Fail");
            }
        }

        private bool TestGettingPlayerInfo()
        {
            Debug.Log("TestGettingPlayerInfo");

            Debug.Log("GetPlayerById(-1)");
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(-1);
            if (player != null)
            {
                Debug.LogError("Player -1 is not null?");
                return false;
            }
            
            Debug.Log("GetPlayerById(1)");
            player = VRCPlayerApi.GetPlayerById(1);
            if (player == null)
            {
                Debug.LogError("Player 1 is null");
                return false;
            }

            Debug.Log("GetPlayerId(_localPlayer)");
            int id = VRCPlayerApi.GetPlayerId(_localPlayer);
            if (id != _localPlayer.playerId)
            {
                Debug.LogError("GetPlayerId returned the wrong value for the local player");
                return false; 
            }
            
            Debug.Log("GetPlayerId(null)");
            id = VRCPlayerApi.GetPlayerId(null);
            if (id != -1)
            {
                Debug.LogError("GetPlayerId returned the wrong value for the local player");
                return false; 
            }
            
            return true;
        }

        private bool TestPlayerTags(VRCPlayerApi player)
        {
            Debug.Log("TestPlayerTags");

            string playerTag1 = "tag1";
            string tagValue1 = "value1";
            string tagValue2 = "value2";

            string cur = player.GetPlayerTag(playerTag1);
            if (!string.IsNullOrEmpty(cur))
            {
                Debug.LogError("Empty tag isn't empty!");
                return false;
            }

            player.SetPlayerTag(playerTag1, tagValue1);
            cur = player.GetPlayerTag(playerTag1);
            if (!cur.Equals(tagValue1))
            {
                Debug.LogError("Set tag does not equal expected value1!");
                return false;
            }
            
            player.SetPlayerTag(playerTag1, tagValue2);
            cur = player.GetPlayerTag(playerTag1);
            if (!cur.Equals(tagValue2))
            {
                Debug.LogError("Set tag does not equal expected value2!");
                return false;
            }

            // TODO
            //localPlayer_.GetPlayersWithTag(playerTag);

            player.ClearPlayerTags();
            cur = player.GetPlayerTag(playerTag1);
            if (!string.IsNullOrEmpty(cur))
            {
                Debug.LogError("Tags were cleared but player still has tag!");
                return false;
            }

            return true;
        }

        private bool TestPlayerAudioSettings(VRCPlayerApi player)
        {
            Debug.Log("TestPlayerAudioSettings");

            player.SetAvatarAudioVolumetricRadius(5);
            player.SetAvatarAudioNearRadius(5);
            player.SetAvatarAudioFarRadius(5);
            player.SetAvatarAudioGain(5);
            player.SetVoiceLowpass(false);
            player.SetVoiceVolumetricRadius(5);
            player.SetVoiceDistanceFar(5);
            player.SetVoiceDistanceNear(5);
            player.SetVoiceGain(5);

            // Always return true since program would crash if not implemented.
            return true;
        }

        private bool TestPlayerLocomotionSettings(VRCPlayerApi player)
        {
            Debug.Log("TestPlayerLocomotionSettings");

            // Teleports
            Debug.Log("Player TeleportTo");
            player.TeleportTo(transform.position, Quaternion.identity);
            
            float expected = 200;
            float val = 0;

            if (player.isLocal)
            {
                // Strafe
                Debug.Log("Player Strafe: " + player.GetStrafeSpeed());
                player.SetStrafeSpeed(expected);
                val = player.GetStrafeSpeed();
                if (val != expected)
                {
                    Debug.LogError("Strafe speed does not equal expected value!");
                    return false;
                }
            }

            if (player.isLocal)
            {
                // Walk
                Debug.Log("Player Walk");
                player.SetWalkSpeed(expected);
                val = player.GetWalkSpeed();
                if (val != expected)
                {
                    Debug.LogError("Walk speed does not equal expected value!");
                    return false;
                }
            }

            if (player.isLocal)
            {
                // Run
                Debug.Log("Player Run");
                player.SetRunSpeed(expected);
                val = player.GetRunSpeed();
                if (val != expected)
                {
                    Debug.LogError("Run speed does not equal expected value!");
                    return false;
                }
            }

            if (player.isLocal)
            {
                // Jump
                Debug.Log("Player Jump");
                player.SetJumpImpulse(expected);
                val = player.GetJumpImpulse();
                if (val != expected)
                {
                    Debug.LogError("Jump speed does not equal expected value!");
                    return false;
                }
            }

            if (player.isLocal)
            {
                Debug.Log("Player Gravity");
                player.SetGravityStrength(expected);
                val = player.GetGravityStrength();
                if (val != expected)
                {
                    Debug.LogError("Gravity strength does not equal expected value!");
                    return false;
                }
            }

            {
                Debug.Log("Player Velocity: " + player.GetVelocity());
                Vector3 velocityExpected = Vector3.up * 50;
                player.SetVelocity(velocityExpected);
                // TODO fix
                // Vector3 velocityActual = player.GetVelocity();
                // if (velocityActual != velocityExpected)
                // {
                //     Debug.LogError("Player velocity does not equal expected value! " +velocityActual +" " + velocityExpected);
                //     return false;
                // }
            }
            
            {
                Debug.Log("Player IsPlayerGrounded: " + player.IsPlayerGrounded());
            }

            return true;
        }

        private bool TestOtherPlayerSettings(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                Debug.Log("Player Immobilize");
                player.Immobilize(true);
                player.Immobilize(false);
            }

            Debug.Log("Player EnablePickups");
            player.EnablePickups(true);
            player.EnablePickups(false);
        
            Debug.Log("PlayHapticEventInHand");
            player.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 1, .5f, .5f);

            return true;
        }
    }
}
#endif