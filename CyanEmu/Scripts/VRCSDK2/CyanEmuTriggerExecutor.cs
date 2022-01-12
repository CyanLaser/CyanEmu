#if VRC_SDK_VRCSDK2

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;
using System;
using System.Reflection;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuTriggerExecutor : MonoBehaviour, ICyanEmuSDKManager
    {
        private const int MAX_EXECUTION_DEPTH_ = 100;

        private static CyanEmuTriggerExecutor instance_;

        // Used for over broadcast detection
        private static bool isTriggerGlobalBroadcast_ = false;
        private static int executionDepth_ = 0;

        private CyanEmuSettings settings_;
        private CyanEmuPlayerController playerController_;
        private CyanEmuBufferManager bufferManager_;

        private bool networkReady_;

        private HashSet<VRC_Trigger> allTriggers_ = new HashSet<VRC_Trigger>();
        private HashSet<CyanEmuTriggerHelper> timerTriggers_ = new HashSet<CyanEmuTriggerHelper>();
        private HashSet<CyanEmuTriggerHelper> keyTriggers_ = new HashSet<CyanEmuTriggerHelper>();
        private List<VRC_Trigger.TriggerEvent> deferredTriggers_ = new List<VRC_Trigger.TriggerEvent>();

        private Dictionary<VRC_Trigger.TriggerEvent, VRC_Trigger> triggerEventToTrigger_ = new Dictionary<VRC_Trigger.TriggerEvent, VRC_Trigger>();
        private List<VRC_Trigger.TriggerEvent> eventsToFireOnUpdate = new List<VRC_Trigger.TriggerEvent>();

        // TODO precache all types on trigger load. Get all types into list and cache all at once rather than per type when needed
        // Component Cache
        private readonly Assembly[] assemblies_ = AppDomain.CurrentDomain.GetAssemblies();
        private Dictionary<string, Type> typeCache_ = new Dictionary<string, Type>();


        private void Awake()
        {
            if (instance_ != null)
            {
                this.LogError("Already have an instance of Trigger executor!");
                DestroyImmediate(this);
                return;
            }

            instance_ = this;

            settings_ = CyanEmuSettings.Instance;

            bufferManager_ = new CyanEmuBufferManager();

            if (settings_.replayBufferedTriggers)
            {
                CyanEmuBufferManager.LoadBufferedTriggersFromFile();
            }
        }

        public static void SetupCombat()
        {
            VRCSDK2.VRC_CombatSystem combatSystem = FindObjectOfType<VRCSDK2.VRC_CombatSystem>();
            if (combatSystem != null)
            {
                combatSystem.gameObject.AddComponent<CyanEmuCombatSystemHelper>();

                CyanEmuCombatSystemHelper.CombatSetMaxHitpoints(null, combatSystem.maxPlayerHealth);
                CyanEmuCombatSystemHelper.CombatSetRespawn(null, combatSystem.respawnOnDeath, combatSystem.respawnTime, combatSystem.respawnPoint);
                CyanEmuCombatSystemHelper.CombatSetDamageGraphic(null, combatSystem.visualDamagePrefab);

                CyanEmuCombatSystemHelper.CombatSetActions(
                    () => VRC_Trigger.TriggerCustom(combatSystem.onPlayerDamagedTrigger),
                    () => VRC_Trigger.TriggerCustom(combatSystem.onPlayerKilledTrigger),
                    () => VRC_Trigger.TriggerCustom(combatSystem.onPlayerHealedTrigger)
                );
            }
        }

        private void Update()
        {
            foreach (CyanEmuTriggerHelper trigger in timerTriggers_)
            {
                trigger.UpdateTimers(eventsToFireOnUpdate);
            }

            // TODO update so that triggers are mapped based on key
            foreach (CyanEmuTriggerHelper trigger in keyTriggers_)
            {
                trigger.UpdateOnKeyTriggers(eventsToFireOnUpdate);
            }

            foreach (VRC_Trigger.TriggerEvent triggerEvent in eventsToFireOnUpdate)
            {
                ExecuteTrigger(triggerEvent);
            }
            eventsToFireOnUpdate.Clear();
        }

        #region ICyanEmuSDKManager

        public void OnNetworkReady()
        {
            // Go through all buffered triggers first
            if (settings_.replayBufferedTriggers)
            {
                CyanEmuMain.SpawnPlayer(false);

                this.Log("Executing Buffered Triggers");

                // This is hacky <_<
                networkReady_ = true;
                bufferManager_.ReplayTriggers();
                networkReady_ = false;
            }

            FireTriggerTypeInternal(VRC_Trigger.TriggerType.OnNetworkReady);

            networkReady_ = true;
            this.Log("Executing Deferred Triggers");
            
            foreach (VRC_Trigger.TriggerEvent evt in deferredTriggers_)
            {
                // Prevent deleted triggers from throwing errors.
                if (GetTriggerForEvent(evt) == null)
                {
                    continue;
                }

                ExecuteTrigger(evt);
            }
            deferredTriggers_.Clear();
        }
        
        public void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                SetupCombat();
            }

            FireTriggerTypeInternal(VRC_Trigger.TriggerType.OnPlayerJoined);
        }

        public void OnPlayerLeft(VRCPlayerApi player)
        {
            FireTriggerTypeInternal(VRC_Trigger.TriggerType.OnPlayerLeft);
        }

        public void OnPlayerRespawn(VRCPlayerApi player) { } // SDK2 does not support this method

        public void OnSpawnedObject(GameObject spawnedObject)
        {
            VRC_Trigger[] triggers = spawnedObject.GetComponentsInChildren<VRC_Trigger>();
            for (int trig = 0; trig < triggers.Length; ++trig)
            {
                triggers[trig].ExecuteTriggerType(VRC_Trigger.TriggerType.OnSpawn);
            }
        }

        #endregion

        private void FireTriggerTypeInternal(VRC_Trigger.TriggerType triggerType)
        {
            foreach (VRC_Trigger trigger in allTriggers_)
            {
                if (trigger == null || trigger.gameObject == null || !trigger.gameObject.activeInHierarchy || !trigger.enabled)
                {
                    continue;
                }

                trigger.ExecuteTriggerType(triggerType);
            }
        }

        public static void FireTriggerType(VRC_Trigger.TriggerType triggerType)
        {
            if (instance_ == null) { return; }
            instance_.FireTriggerTypeInternal(triggerType);
        }



        public static VRC_Trigger GetTriggerForEvent(VRC_Trigger.TriggerEvent trigEvent)
        {
            if (instance_ == null)
            {
                return null;
            }

            return instance_.triggerEventToTrigger_[trigEvent];
        }

        public static void AddTrigger(VRC_Trigger trigger)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.allTriggers_.Add(trigger);
            foreach (VRC_Trigger.TriggerEvent trigEvent in trigger.Triggers)
            {
                instance_.triggerEventToTrigger_.Add(trigEvent, trigger);
            }
        }

        public static void RemoveTrigger(VRC_Trigger trigger)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.allTriggers_.Remove(trigger);
        }

        public static void AddTimerTrigger(CyanEmuTriggerHelper trigger)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.timerTriggers_.Add(trigger);
        }

        public static void RemoveTimerTrigger(CyanEmuTriggerHelper trigger)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.timerTriggers_.Remove(trigger);
        }

        public static void AddKeyTrigger(CyanEmuTriggerHelper trigger)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.keyTriggers_.Add(trigger);
        }

        public static void RemoveKeyTrigger(CyanEmuTriggerHelper trigger)
        {
            if (instance_ == null)
            {
                return;
            }

            instance_.keyTriggers_.Remove(trigger);
        }

        public static void ExecuteTrigger(VRC_Trigger.TriggerEvent trigger)
        {
            if (instance_ == null)
            {
                return;
            }
            
            if (!instance_.networkReady_)
            {
                instance_.deferredTriggers_.Add(trigger);
                instance_.Log("Deferring Trigger: " + trigger.GetTriggerEventAsString());
                return;
            }

            if (trigger.AfterSeconds != 0)
            {
                instance_.Log("Delaying Execution of trigger name: " + trigger.GetTriggerEventAsString());
                instance_.StartCoroutine(ExecuteTriggerDelay(trigger));
            }
            else
            {
                ExecuteTriggerNow(trigger, isTriggerGlobalBroadcast_);
            }
        }

        private static IEnumerator ExecuteTriggerDelay(VRC_Trigger.TriggerEvent trigger)
        {
            bool isGlobalBroadcast = isTriggerGlobalBroadcast_;
            yield return new WaitForSeconds(trigger.AfterSeconds);
            ExecuteTriggerNow(trigger, isGlobalBroadcast);
        }

        public static void ExecuteTriggerNow(VRC_Trigger.TriggerEvent trigger, bool prevGlobal)
        {
            if (executionDepth_ > MAX_EXECUTION_DEPTH_)
            {
                instance_.LogError("Reached maximum execution depth of "+ MAX_EXECUTION_DEPTH_ +"! Failed to execute trigger!");
                return;
            }
            ++executionDepth_;
            
            instance_.Log("Executing Trigger: " + trigger.GetTriggerEventAsString());


            bool globalBroadcast = trigger.BroadcastType != VRC_EventHandler.VrcBroadcastType.Local;
            bool setGlobal = false;
            if ((!prevGlobal && globalBroadcast) || (prevGlobal && !isTriggerGlobalBroadcast_))
            {
                isTriggerGlobalBroadcast_ = true;
                setGlobal = true;
            }

            if (prevGlobal && trigger.BroadcastType.IsEveryoneBroadcastType())
            {
                // Custom trigger oversync
                instance_.LogWarning("Potential OverSync! "+ trigger.GetTriggerEventAsString());
            }
            
            // Random - No error checking. Assumes sum of all values is >= 1.
            if (trigger.Probabilities.Length != 0)
            {
                float value = UnityEngine.Random.value;
                int ind = 0;
                float sum = 0;
                while (sum < value)
                {
                    sum += trigger.Probabilities[ind++];
                    if (sum >= value)
                    {
                        VRC_EventHandler.VrcEvent triggerEvent = trigger.Events[ind - 1];

                        VRC_EventHandler.EventInfo eventInfo = new VRC_EventHandler.EventInfo()
                        {
                            broadcast = trigger.BroadcastType,
                            evt = triggerEvent,
                            instagator = instance_.gameObject // todo? set player?
                        };
                        ExecuteEvent(eventInfo, trigger);
                        break;
                    }
                }
            }
            else
            {
                foreach (VRC_EventHandler.VrcEvent triggerEvent in trigger.Events)
                {
                    VRC_EventHandler.EventInfo eventInfo = new VRC_EventHandler.EventInfo()
                    {
                        broadcast = trigger.BroadcastType,
                        evt = triggerEvent,
                        instagator = instance_.gameObject // todo? set player?
                    };
                    ExecuteEvent(eventInfo, trigger);
                }
            }

            if (setGlobal)
            {
                isTriggerGlobalBroadcast_ = false;
            }

            --executionDepth_;
        }

        public static void ExecuteEvent(VRC_EventHandler.EventInfo eventInfo, VRC_Trigger.TriggerEvent originalTrigger)
        {
            // Only save when we don't have an instigator. 
            // Todo, check for this properly. 
            if (eventInfo.broadcast.IsBufferedBroadcastType() && eventInfo.instagator != null)
            {
                instance_.Log("Adding event to the buffer queue");
                instance_.bufferManager_.SaveBufferedEvent(eventInfo);
            }

            if (eventInfo.evt.ParameterObjects.Length > 0)
            {
                foreach (GameObject obj in eventInfo.evt.ParameterObjects)
                {
                    if (obj == null)
                    {
                        instance_.LogError("Object is null! It was probably deleted and but a trigger is still trying to modify it!");
                        continue;
                    }

                    try
                    {
                        ExecuteEventForObject(eventInfo.evt, obj, eventInfo, originalTrigger);
                    }
                    catch (Exception e)
                    {
                        instance_.LogError(e.ToString());
                    }


                    if (eventInfo.evt.EventType == VRC_EventHandler.VrcEventType.SpawnObject && eventInfo.evt.ParameterObjects.Length > 1)
                    {
                        instance_.LogWarning("Only spawning one object since VRChat limits spawning.");
                        break;
                    }
                }
            }
            else
            {
                instance_.LogWarning("No Object specified for trigger. This is unsupported");
                instance_.LogError("Trigger with no receiver means you probably deleted the object but are still trying to perform actions on it.");
            }
        }

        private static void ExecuteEventForObject(VRC_EventHandler.VrcEvent triggerEvent, GameObject obj, VRC_EventHandler.EventInfo eventInfo, VRC_Trigger.TriggerEvent originalTrigger)
        {
            instance_.Log("Executing Trigger Event: " + triggerEvent.GetEventAsString(obj) + "\n_On Trigger: " + originalTrigger?.GetTriggerEventAsString());

            bool isBufferedExecution = eventInfo.broadcast.IsBufferedBroadcastType() && eventInfo.instagator == null;
            if (eventInfo.broadcast.IsOwnerBroadcastType())
            {
                if (!Networking.IsOwner(obj) || isBufferedExecution)
                {
                    instance_.LogWarning("Not executing as user is not the owner");
                    return;
                }
            }

            if (!isBufferedExecution && eventInfo.broadcast.IsMasterBroadcastType())
            {
                if (!Networking.IsMaster || isBufferedExecution)
                {
                    instance_.LogWarning("Not executing as user is not the master");
                    return;
                }
            }

            if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.ActivateCustomTrigger)
            {
                // Only activates first one
                VRC_Trigger trigger = obj.GetComponent<VRC_Trigger>();
                if (obj.activeInHierarchy && trigger.enabled)
                {
                    trigger.ExecuteCustomTrigger(triggerEvent.ParameterString);
                }
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AnimationBool)
            {
                Animator animator = obj.GetComponent<Animator>();
                bool value = animator.GetBool(triggerEvent.ParameterString);
                bool newValue = VRC_EventHandler.BooleanOp(triggerEvent.ParameterBoolOp, value);
                animator.SetBool(triggerEvent.ParameterString, newValue);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AnimationInt)
            {
                Animator animator = obj.GetComponent<Animator>();
                animator.SetInteger(triggerEvent.ParameterString, triggerEvent.ParameterInt);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AnimationFloat)
            {
                Animator animator = obj.GetComponent<Animator>();
                animator.SetFloat(triggerEvent.ParameterString, triggerEvent.ParameterFloat);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AnimationIntAdd)
            {
                Animator animator = obj.GetComponent<Animator>();
                int value = animator.GetInteger(triggerEvent.ParameterString);
                animator.SetInteger(triggerEvent.ParameterString, value + triggerEvent.ParameterInt);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AnimationIntDivide)
            {
                Animator animator = obj.GetComponent<Animator>();
                int value = animator.GetInteger(triggerEvent.ParameterString);
                animator.SetInteger(triggerEvent.ParameterString, value / triggerEvent.ParameterInt);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AnimationIntMultiply)
            {
                Animator animator = obj.GetComponent<Animator>();
                int value = animator.GetInteger(triggerEvent.ParameterString);
                animator.SetInteger(triggerEvent.ParameterString, value * triggerEvent.ParameterInt);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AnimationIntSubtract)
            {
                Animator animator = obj.GetComponent<Animator>();
                int value = animator.GetInteger(triggerEvent.ParameterString);
                animator.SetInteger(triggerEvent.ParameterString, value - triggerEvent.ParameterInt);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AnimationTrigger)
            {
                obj.GetComponent<Animator>().SetTrigger(triggerEvent.ParameterString);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.PlayAnimation)
            {
                obj.GetComponent<Animation>().Play(triggerEvent.ParameterString);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AudioTrigger)
            {
                AudioSource[] audioSources = obj.GetComponents<AudioSource>();
                foreach (var source in audioSources)
                {
                    if (string.IsNullOrEmpty(triggerEvent.ParameterString) || source.clip.name == triggerEvent.ParameterString)
                    {
                        source.Play();
                    }
                }
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.DestroyObject)
            {
                Destroy(obj);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SendRPC)
            {
                object[] parameters = VRC_Serialization.ParameterDecoder(triggerEvent.ParameterBytes);
                Type[] parameterTypes = new Type[parameters.Length];
                for (int paramIndex = 0; paramIndex < parameters.Length; ++paramIndex)
                {
                    parameterTypes[paramIndex] = parameters[paramIndex].GetType();
                }

                foreach (MonoBehaviour mono in obj.GetComponents<MonoBehaviour>())
                {
                    MethodInfo methodInfo = mono.GetType().GetMethod(triggerEvent.ParameterString, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, parameterTypes, null);

                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(mono, parameters);
                    }
                }
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetComponentActive)
            {
                Type type = instance_.GetTypeForComponent(triggerEvent.ParameterString);
                if (type != null)
                {
                    PropertyInfo property = type.GetProperty("enabled");
                    if (property != null)
                    {
                        foreach (Component component in obj.GetComponents(type))
                        {
                            bool value = (bool)property.GetValue(component, null);
                            bool newValue = VRC_EventHandler.BooleanOp(triggerEvent.ParameterBoolOp, value);
                            property.SetValue(component, newValue, null);
                        }
                    }
                }
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetGameObjectActive)
            {
                bool newValue = VRC_EventHandler.BooleanOp(triggerEvent.ParameterBoolOp, obj.activeSelf);
                obj.SetActive(newValue);

                CyanEmuTriggerHelper triggerHelper = obj.GetComponent<CyanEmuTriggerHelper>();
                if (triggerHelper != null && isTriggerGlobalBroadcast_)
                {
                    if (newValue && triggerHelper.HasGlobalOnDisable)
                    {
                        instance_.LogWarning("Posisble OnEnable or OnTimer oversync!");
                    }
                    else if (!newValue && triggerHelper.HasGlobalOnDisable)
                    {
                        instance_.LogWarning("Posisble OnDisable oversync!");
                    }
                }
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetLayer)
            {
                obj.layer = triggerEvent.ParameterInt;
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetMaterial)
            {
                Material mat = VRC_SceneDescriptor.GetMaterial(triggerEvent.ParameterString);
                obj.GetComponent<Renderer>().material = mat;
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetParticlePlaying)
            {
                ParticleSystem system = obj.GetComponent<ParticleSystem>();
                bool newValue = VRC_EventHandler.BooleanOp(triggerEvent.ParameterBoolOp, system.isPlaying);
                if (newValue)
                {
                    system.Play();
                }
                else
                {
                    system.Stop();
                }
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetUIText)
            {
                obj.GetComponent<UnityEngine.UI.Text>().text = triggerEvent.ParameterString;
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SpawnObject)
            {
                GameObject prefab = VRC_SceneDescriptor.GetPrefab(triggerEvent.ParameterString);
                CyanEmuMain.SpawnObject(prefab, obj.transform.position, obj.transform.rotation);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.TeleportPlayer)
            {
                if (isBufferedExecution)
                {
                    instance_.LogWarning("Teleport player actions should not be buffered. Ignoring");
                }
                else
                {
                    if (CyanEmuPlayerController.instance != null)
                    {
                        CyanEmuPlayerController.instance.Teleport(triggerEvent.ParameterObjects[0].transform, triggerEvent.ParameterBoolOp == VRC_EventHandler.VrcBooleanOp.True);
                    }
                    else
                    {
                        instance_.LogWarning("No player container to teleport!");
                    }

                    if (eventInfo.broadcast != VRC_EventHandler.VrcBroadcastType.Local)
                    {
                        instance_.LogWarning("TeleportPlayer action should be set to local!");
                    }
                }

            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddForce)
            {
                HandleTriggerPhysicsEvent(triggerEvent, obj);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddVelocity)
            {
                HandleTriggerPhysicsEvent(triggerEvent, obj);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetVelocity)
            {
                HandleTriggerPhysicsEvent(triggerEvent, obj);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddAngularVelocity)
            {
                HandleTriggerPhysicsEvent(triggerEvent, obj);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetAngularVelocity)
            {
                HandleTriggerPhysicsEvent(triggerEvent, obj);
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddDamage)
            {
                HandleTriggerDamageEvent(triggerEvent, obj);

                if (isBufferedExecution)
                {
                    instance_.LogWarning("AddDamage action should not be buffered!");
                }
                else if (eventInfo.broadcast != VRC_EventHandler.VrcBroadcastType.Local)
                {
                    instance_.LogWarning("AddDamage action should be set to local!");
                }
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddHealth)
            {
                HandleTriggerDamageEvent(triggerEvent, obj);

                if (isBufferedExecution)
                {
                    instance_.LogWarning("AddHealth action should not be buffered!");
                }
                else if (eventInfo.broadcast != VRC_EventHandler.VrcBroadcastType.Local)
                {
                    instance_.LogWarning("AddHealth action should be set to local!");
                }
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetWebPanelURI) { }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetWebPanelVolume) { }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.MeshVisibility) { }
        }

        private static void HandleTriggerPhysicsEvent(VRC_EventHandler.VrcEvent triggerEvent, GameObject obj)
        {
            object[] parameters = VRC_Serialization.ParameterDecoder(triggerEvent.ParameterBytes);
            if (parameters.Length == 0)
            {
                parameters = new object[1] { new Vector4() };
            }

            Vector4 vec = (Vector4)parameters[0];
            Vector3 vel = vec;
            if (vec.w < .5f)
            {
                vel = obj.transform.TransformVector(vel);
            }
            Rigidbody rigidbody = obj.GetComponent<Rigidbody>();

            if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetAngularVelocity)
            {
                rigidbody.angularVelocity = vel;
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddAngularVelocity)
            {
                rigidbody.angularVelocity += vel;
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.SetVelocity)
            {
                rigidbody.velocity = vel;
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddVelocity)
            {
                rigidbody.velocity += vel;
            }
            else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddForce)
            {
                rigidbody.AddForce(vel);
            }
        }

        private static void HandleTriggerDamageEvent(VRC_EventHandler.VrcEvent triggerEvent, GameObject obj)
        {
            if (obj == null)
            {
                VRCSDK2.VRC_CombatSystem combatSystem = VRCSDK2.VRC_CombatSystem.GetInstance();
                if (combatSystem != null)
                {
                    obj = combatSystem.gameObject;
                }
            }

            if (obj != null)
            {
                IVRC_Destructible destructable = obj.GetComponent<IVRC_Destructible>();
                if (destructable != null)
                {
                    if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddDamage)
                    {
                        destructable.ApplyDamage(triggerEvent.ParameterFloat);
                    }
                    else if (triggerEvent.EventType == VRC_EventHandler.VrcEventType.AddHealth)
                    {
                        destructable.ApplyHealing(triggerEvent.ParameterFloat);
                    }
                }
            }
        }

        public Type GetTypeForComponent(string component)
        {
            if (!typeCache_.ContainsKey(component))
            {
                Type type = null;
                foreach (Assembly assembly in assemblies_)
                {
                    type = assembly.GetType(component, false, true);
                    if (type != null)
                        break;
                }

                if (type == null)
                {
                    instance_.LogError("Cannot find component of type " + component + "!");
                }

                typeCache_.Add(component, type);
            }

            return typeCache_[component];
        }
    }
}

#endif
