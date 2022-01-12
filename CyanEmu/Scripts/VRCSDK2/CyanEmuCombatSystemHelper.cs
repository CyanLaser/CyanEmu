using System;
using System.Collections;
using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu { 

    [AddComponentMenu("")]
    public class CyanEmuCombatSystemHelper : MonoBehaviour, IVRC_Destructible
    {
        public static CyanEmuCombatSystemHelper instance;

        private bool respawnOnDeath_;
        private float respawnTime_;
        private Transform respawnPoint_;
        private float maxPlayerHealth_;
        private bool resetHealthOnRespawn_ = true;
        private GameObject visualDamagePrefab_;

        private Action onPlayerDamaged_;
        private Action onPlayerKilled_;
        private Action onPlayerHealed_;

        private float currentHealth_;
        
        private VRC_VisualDamage visualDamage_;

        private CyanEmuPlayerController playerController_;
        private CyanEmuPlayerController playerController {
            get
            {
                if (playerController_ == null)
                {
                    playerController_ = CyanEmuPlayerController.instance;
                }
                return playerController_;
            }
        }
        
        public static void CombatSetup(VRCPlayerApi player)
        {
            if (instance == null)
            {
                player.gameObject.AddComponent<CyanEmuCombatSystemHelper>();
            }
        }

        public static void CombatSetMaxHitpoints(VRCPlayerApi player, float maxHealth)
        {
            instance.maxPlayerHealth_ = maxHealth;
        }

        public static float CombatGetCurrentHitpoints(VRCPlayerApi player)
        {
            return instance.currentHealth_;
        }

        public static void CombatSetRespawn(VRCPlayerApi player, bool respawnOnDeath, float respawnTime, Transform spawnPoint)
        {
            instance.respawnOnDeath_ = respawnOnDeath;
            instance.respawnTime_ = respawnTime;
            instance.respawnPoint_ = spawnPoint;
        }

        public static void CombatSetDamageGraphic(VRCPlayerApi player, GameObject visualDamage)
        {
            instance.visualDamagePrefab_ = visualDamage;
            instance.CreateVisualDamage();
        }


        public static void CombatSetActions(Action onPlayerDamaged, Action onPlayerKilled, Action onPlayerHealed)
        {
            instance.onPlayerDamaged_ = onPlayerDamaged;
            instance.onPlayerKilled_ = onPlayerKilled;
            instance.onPlayerHealed_ = onPlayerHealed;
        }

        public static IVRC_Destructible CombatGetDestructible(VRCPlayerApi player)
        {
            return instance;
        }

        public static void CombatSetCurrentHitpoints(VRCPlayerApi player, float health)
        {
            float delta = health - instance.currentHealth_;
            if (delta <= 0)
            {
                instance.ApplyDamage(-delta);
            }
            else
            {
                instance.ApplyHealing(delta);
            }
            instance.currentHealth_ = health;
        }

        private void Awake()
        {
            if (instance != null)
            {
                this.LogWarning("Duplicate combat system helper! " + VRC.Tools.GetGameObjectPath(gameObject));
                DestroyImmediate(this);
                return;
            }

            instance = this;
        }

        private void Start()
        {
            currentHealth_ = GetMaxHealth();
        }

        public void CreateVisualDamage()
        {
            if (visualDamage_ != null)
            {
                Destroy(visualDamage_.gameObject);
            }

            GameObject damage = visualDamagePrefab_;
            if (damage == null)
            {
                damage = Resources.Load<GameObject>("VRC_PlayerVisualDamage");
            }
			if (damage != null) {				
				GameObject visualDamageObject = Instantiate(damage, playerController.GetCameraProxyTransform());
				float offset = 0;
				visualDamage_ = visualDamageObject.GetComponent<VRC_VisualDamage>();
				if (visualDamage_ != null) {
					visualDamage_.SetDamagePercent(0);
					offset = visualDamage_.offset;
				}
				visualDamageObject.transform.localScale = new Vector3(40, 40, 40);
				visualDamageObject.transform.localPosition = new Vector3(0, 0, offset);
			}
        }

        public float GetMaxHealth()
        {
            return maxPlayerHealth_;
        }

        public float GetCurrentHealth()
        {
            return currentHealth_;
        }

        private void ApplyVisualDamage()
        {
            if (visualDamage_ != null)
            {
                try
                {
                    visualDamage_.SetDamagePercent(1 - (currentHealth_ / maxPlayerHealth_));
                }
                catch (Exception e)
                {
                    this.LogWarning("Error applying damage: "+ e);
                }
            }
        }

        public void ApplyDamage(float damage)
        {
            if (currentHealth_ <= 0)
            {
                return;
            }
            this.Log("ApplyDamage: " + damage + " currentHealth: " + currentHealth_);

            currentHealth_ = Mathf.Clamp(currentHealth_ - damage, 0, maxPlayerHealth_);
            ApplyVisualDamage();

            onPlayerDamaged_?.Invoke();

            if (currentHealth_ <= 0)
            {
                this.Log("Player Died");
                if (playerController)
                {
                    playerController.PlayerDied();
                }

                onPlayerKilled_?.Invoke();
                
                StartCoroutine(PlayerDied());
            }
        }

        private IEnumerator PlayerDied()
        {
            yield return new WaitForSeconds(respawnTime_);

            if (respawnPoint_)
            {
                if (playerController)
                {
                    playerController.Teleport(respawnPoint_, false);
                }
            }
            
            if (resetHealthOnRespawn_)
            {
                ApplyHealing(maxPlayerHealth_);
            }
            if (playerController)
            {
                this.Log("Player Revivied");
                playerController.PlayerRevived();
            }
        }

        public void ApplyHealing(float healing)
        {
            currentHealth_ = Mathf.Clamp(currentHealth_ + healing, 0, maxPlayerHealth_);
            ApplyVisualDamage();

            this.Log("ApplyHealing: " + healing + " currentHealth: "+currentHealth_);

            onPlayerHealed_?.Invoke();

            if (currentHealth_ > 0)
            {
                if (playerController)
                {
                    playerController.PlayerRevived();
                }
            }
        }


        // When is this used?
        public object[] GetState()
        {
            throw new System.NotImplementedException();
        }

        // When is this used?
        public void SetState(object[] state)
        {
            throw new System.NotImplementedException();
        }
    }
}