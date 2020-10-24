﻿using System;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using System.Reflection;

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuPlayerController : MonoBehaviour
    {
        public static CyanEmuPlayerController instance;

        private enum Stance {
            STANDING,
            CROUCHING,
            PRONE
        }

        public const float DEFAULT_RUN_SPEED_ = 4;
        public const float DEFAULT_WALK_SPEED_ = 2;

        private const float CROUCH_SPEED_MULTIPLYER_ = 0.35f;
        private const float PRONE_SPEED_MULTIPLYER_ = 0.15f;

        private const float STANDING_HEIGHT_ = 1.6f;
        private const float CROUCHING_HEIGHT_ = 1.0f;
        private const float PRONE_HEIGHT_ = 0.5f;

        private const float STICK_TO_GROUND_FORCE_ = 2f;
        private const float RATE_OF_AIR_ACCELERATION_ = 5f;

        private readonly KeyCode MenuKey = KeyCode.Escape;

        private Vector3 capsuleSize_ = new Vector3(0.4f, 1, 0.4f);
        private float currentScale_ = 1f;

        private GameObject playspace_;
        private GameObject playerCamera_;
        private GameObject capsule_;
        private GameObject rightArmPosition_;
        private GameObject leftArmPosition_;
        private GameObject menu_;
        private Transform cameraProxyObject_;
        private Rigidbody rigidbody_;
        private VRC_SceneDescriptor descriptor_;
        private CharacterController characterController_;
        private Camera camera_;
        private MouseLook mouseLook_;
        private CyanEmuStationHelper currentStation_;
        private CyanEmuPickupHelper currentPickup_;
        private CyanEmuBaseInput baseInput_;
        private CyanEmuInteractHelper interactHelper_;
        private CyanEmuPlayer player_;

        private Stance stance_;
        private bool isDead_;
        private bool isWalking_;

        private float walkSpeed_ = DEFAULT_WALK_SPEED_;
        private float strafeSpeed_ = DEFAULT_WALK_SPEED_;
        private float runSpeed_ = DEFAULT_RUN_SPEED_;
        private float jumpSpeed_;
        private bool jump_;
        private float gravityStrength_ = 1f;

        //Only used to prevent sliding without changing the input manager.
        private Vector2 prevInput_; 
        private Vector2 prevInputResult_;

        private bool velSet;
        private Vector3 playerRetainedVelocity_;

        private CollisionFlags collisionFlags_;
        private bool peviouslyGrounded_;
        private bool legacyLocomotion_;

        private Texture2D reticleTexture_;

        private static CyanEmuSettings settings_;

        public static Camera GetPlayerCamera()
        {
            if (instance != null)
            {
                return instance.GetCamera();
            }
            return Camera.main;
        }
        
        private void Awake()
        {
            if (instance != null)
            {
                this.LogError("Player controller instance already exists!");
                DestroyImmediate(this);
                return;
            }

#if VRC_SDK_VRCSDK2
            legacyLocomotion_ = true;
#endif

            instance = this;
            descriptor_ = FindObjectOfType<VRC_SceneDescriptor>();
            gameObject.layer = LayerMask.NameToLayer("PlayerLocal");
            gameObject.tag = "Player";

            rigidbody_ = gameObject.AddComponent<Rigidbody>();
            rigidbody_.isKinematic = true;

            characterController_ = gameObject.AddComponent<CharacterController>();
            characterController_.slopeLimit = 50;
            characterController_.stepOffset = .5f;
            characterController_.skinWidth = 0.005f;
            characterController_.minMoveDistance = 0;
            characterController_.center = new Vector3(0, 0.8f, 0);
            characterController_.radius = 0.2f;
            characterController_.height = 1.6f;


            capsule_ = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule_.transform.localScale = capsuleSize_;
            capsule_.transform.parent = transform;
            capsule_.transform.localPosition = new Vector3(0, 1, 0);
            DestroyImmediate(capsule_.GetComponent<Collider>());


            playerCamera_ = new GameObject("Player Camera");
            camera_ = playerCamera_.AddComponent<Camera>();
            camera_.cullingMask &= ~(1 << 18); // remove mirror reflection

            playerCamera_.AddComponent<AudioListener>();
            playerCamera_.transform.parent = transform;
            playerCamera_.transform.localPosition = new Vector3(0, STANDING_HEIGHT_, .1f);
            playerCamera_.transform.localRotation = Quaternion.identity;
            // TODO, make based on avatar armspan/settings
            playerCamera_.transform.localScale = Vector3.one * 1.13f;

            playspace_ = new GameObject("Playspace Center");
            playspace_.transform.parent = transform;
            playspace_.transform.localPosition = new Vector3(-1, 0, -1);
            playspace_.transform.localRotation = Quaternion.Euler(0, 45, 0);

            rightArmPosition_ = new GameObject("Right Arm Position");
            rightArmPosition_.transform.parent = playerCamera_.transform;
            rightArmPosition_.transform.localPosition = new Vector3(0.2f, -0.2f, 0.75f);
            
            leftArmPosition_ = new GameObject("Left Arm Position");
            leftArmPosition_.transform.parent = playerCamera_.transform;
            leftArmPosition_.transform.localPosition = new Vector3(-0.2f, -0.2f, 0.75f);
            
            mouseLook_ = new MouseLook();
            mouseLook_.Init(transform, camera_.transform);

            stance_ = Stance.STANDING;

            baseInput_ = transform.parent.gameObject.GetComponent<CyanEmuBaseInput>();
            CreateMenu();

            GameObject interactHelper = new GameObject("InteractHelper");
            interactHelper.transform.parent = transform.parent;
            interactHelper_ = interactHelper.AddComponent<CyanEmuInteractHelper>();
            Func<bool> shouldCheckForInteracts = () => { return currentPickup_ == null && !menu_.activeInHierarchy && !isDead_; };
            interactHelper_.Initialize(playerCamera_.transform, playerCamera_.transform, shouldCheckForInteracts);

            reticleTexture_ = Resources.Load<Texture2D>("Images/Reticle");
            
            cameraProxyObject_ = new GameObject("CameraDamageProxy").transform;
            cameraProxyObject_.parent = CyanEmuMain.GetProxyObjectTransform();
            UpdateCameraProxyPosition();

            // experimental!
            //interactHelper_.highlightManager = playerCamera_.AddComponent<VRCP_HighlightManager>();
        }

        private void Start()
        {
            settings_ = CyanEmuSettings.Instance;
            Camera refCamera = null;
            if (descriptor_.ReferenceCamera != null)
            {
                refCamera = descriptor_.ReferenceCamera.GetComponent<Camera>();
            }
            if (refCamera == null)
            {
                refCamera = Camera.main;
            }
            if (refCamera == null)
            {
                GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCamera != null)
                {
                    refCamera = mainCamera.GetComponent<Camera>();
                }
            }

            CopyCameraValues(refCamera, camera_);
            
            if (CyanEmuCombatSystemHelper.instance != null)
            {
                CyanEmuCombatSystemHelper.instance.CreateVisualDamage();
            }

            // Go through all ui shapes to update canvas cameras?
            foreach (var ui in FindObjectsOfType<VRC_UiShape>())
            {
                Canvas canvas = ui.GetComponent<Canvas>();
                if (canvas == null) continue;
                canvas.worldCamera = camera_;
            }

#if VRC_SDK_VRCSDK2
            CyanEmuPlayerModsHelper.ApplyRoomMods(this);
#endif
        }

        public static void CopyCameraValues(Camera refCamera, Camera camera)
        {
            if (refCamera != null)
            {
                camera.farClipPlane = refCamera.farClipPlane;
                camera.nearClipPlane = Mathf.Clamp(refCamera.nearClipPlane, 0.01f, 0.02f);
                camera.clearFlags = refCamera.clearFlags;
                camera.backgroundColor = refCamera.backgroundColor;
                camera.tag = "MainCamera";

#if UNITY_POST_PROCESSING_STACK_V2
                PostProcessLayer refPostProcessLayer = refCamera.GetComponent<PostProcessLayer>();
                if (refPostProcessLayer != null)
                {
                    PostProcessLayer postProcessLayer = camera.gameObject.AddComponent<PostProcessLayer>();
                    postProcessLayer.volumeLayer = refPostProcessLayer.volumeLayer;

                    postProcessLayer.volumeTrigger = refPostProcessLayer.volumeTrigger != refPostProcessLayer.transform
                        ? refPostProcessLayer.volumeTrigger
                        : postProcessLayer.volumeTrigger = camera.transform;
                    
                    // post processing should always be enabled.
                    // postProcessLayer.enabled = refPostProcessLayer.enabled;

                    // Use reflection to copy over resources : https://github.com/Unity-Technologies/PostProcessing/issues/467
                    FieldInfo resourcesInfo = typeof(PostProcessLayer).GetField("m_Resources", BindingFlags.NonPublic | BindingFlags.Instance);
                    PostProcessResources postProcessResources = resourcesInfo.GetValue(refPostProcessLayer) as PostProcessResources;
                    postProcessLayer.Init(postProcessResources);
                }
#endif
                refCamera.gameObject.SetActive(false);
            }
        }

        private void CreateMenu()
        {
            Font font = Font.CreateDynamicFontFromOSFont("Ariel", 20);

            // Create Menu
            menu_ = new GameObject("Menu");
            menu_.transform.parent = transform.parent;
            Canvas canvas = menu_.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera_;
            canvas.planeDistance = camera_.nearClipPlane + 0.1f;
            canvas.sortingOrder = 1000;
            menu_.AddComponent<GraphicRaycaster>();

            GameObject respawnButton = new GameObject("RespawnButton");
            respawnButton.transform.SetParent(menu_.transform, false);
            respawnButton.transform.localPosition = new Vector3(60, 0, 0);
            respawnButton.AddComponent<Image>();
            Button button = respawnButton.AddComponent<Button>();
            button.onClick.AddListener(Respawn);

            GameObject respawnText = new GameObject("RespawnText");
            respawnText.transform.SetParent(respawnButton.transform, false);
            Text text = respawnText.AddComponent<Text>();
            respawnText.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            text.text = "Respawn";
            text.fontSize = 20;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.font = font;


            GameObject exitMenuButton = new GameObject("ExitMenuButton");
            exitMenuButton.transform.SetParent(menu_.transform, false);
            exitMenuButton.transform.localPosition = new Vector3(-60, 0, 0);
            exitMenuButton.AddComponent<Image>();
            button = exitMenuButton.AddComponent<Button>();
            button.onClick.AddListener(CloseMenu);

            GameObject exitMenuText = new GameObject("ExitMenuText");
            exitMenuText.transform.SetParent(exitMenuButton.transform, false);
            text = exitMenuText.AddComponent<Text>();
            exitMenuText.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            text.text = "Close\nMenu";
            text.fontSize = 20;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.font = font;

            ToggleMenu(true);
        }


        public float GetJump()
        {
            return jumpSpeed_;
        }

        public void SetJump(float jump)
        {
            jumpSpeed_ = jump;
        }

        public float GetRunSpeed()
        {
            return runSpeed_;
        }

        public void SetRunSpeed(float runSpeed)
        {
            runSpeed_ = runSpeed;
        }

        public float GetWalkSpeed()
        {
            return walkSpeed_;
        }

        public void SetWalkSpeed(float walkSpeed)
        {
            walkSpeed_ = walkSpeed;
        }

        public float GetStrafeSpeed()
        {
            return strafeSpeed_;
        }

        public void SetStrafeSpeed(float strafeSpeed)
        {
            strafeSpeed_ = strafeSpeed;
        }
        
        public float GetGravityStrength()
        {
            return gravityStrength_;
        }

        public void SetGravityStrength(float gravity)
        {
            gravityStrength_ = gravity;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public Quaternion GetRotation()
        {
            return transform.rotation;
        }

        public Vector3 GetVelocity()
        {
            return characterController_.velocity;
        }
        
        public void SetVelocity(Vector3 velocity)
        {
            playerRetainedVelocity_ = velocity;
            velSet = true;
        }

        public bool IsGrounded()
        {
            return characterController_.isGrounded;
        }

        public void UseLegacyLocomotion()
        {
            legacyLocomotion_ = true;
        }

        public VRCPlayerApi.TrackingData GetTrackingData(VRCPlayerApi.TrackingDataType trackingDataType)
        {
            VRCPlayerApi.TrackingData data = new VRCPlayerApi.TrackingData();

            if (trackingDataType == VRCPlayerApi.TrackingDataType.Head)
            {
                data.position = playerCamera_.transform.position;
                data.rotation = playerCamera_.transform.rotation;
            }
            else if (trackingDataType == VRCPlayerApi.TrackingDataType.LeftHand)
            {
                data.position = leftArmPosition_.transform.position;
                data.rotation = leftArmPosition_.transform.rotation;
            }
            else if (trackingDataType == VRCPlayerApi.TrackingDataType.RightHand)
            {
                data.position = rightArmPosition_.transform.position;
                data.rotation = rightArmPosition_.transform.rotation;
            }

            return data;
        }

        public void SetPlayer(CyanEmuPlayer player)
        {
            player_ = player;
        }


        public void Respawn()
        {
            if (currentStation_ != null)
            {
                currentStation_.ExitStation();
            }
            CloseMenu();
            Teleport(descriptor_.spawns[0], false);
        }

        public void Teleport(Transform point, bool fromPlaySpace)
        {
            Teleport(point.position, Quaternion.Euler(0, point.rotation.eulerAngles.y, 0), fromPlaySpace);
        }

        public void Teleport(Vector3 position, Quaternion floorRotation, bool fromPlaySpace)
        {
            floorRotation = Quaternion.Euler(0, floorRotation.eulerAngles.y, 0);
            if (fromPlaySpace)
            {
                floorRotation = Quaternion.Inverse(playspace_.transform.localRotation) * floorRotation;
                position = position + floorRotation * -playspace_.transform.localPosition;
            }

            this.Log("Moving player to " + position.ToString("F3") + " and rotation " + floorRotation.eulerAngles.ToString("F3"));

            transform.rotation = floorRotation;
            transform.position = position;
            mouseLook_.SetRotation(floorRotation);
            UpdateCameraProxyPosition();
            Physics.SyncTransforms();
        }

        public void EnterStation(CyanEmuStationHelper station)
        {
            if (currentStation_ != null)
            {
                currentStation_.ExitStation();
            }

            currentStation_ = station;

            if (!station.IsMobile)
            {
                characterController_.enabled = false;
                Teleport(station.EnterLocation, false);
                mouseLook_.SetBaseRotation(station.EnterLocation);
                mouseLook_.SetRotation(Quaternion.identity);
            }
        }

        public void ExitStation(CyanEmuStationHelper station)
        {
            currentStation_ = null;
            characterController_.enabled = true;

            if (!station.IsMobile)
            {
                Teleport(station.ExitLocation, false);
            }
            mouseLook_.SetBaseRotation(null);
            jump_ = false;
        }

        public void PickupObject(CyanEmuPickupHelper pickup)
        {
            if (currentPickup_ != null)
            {
                currentPickup_.Drop();
            }
            currentPickup_ = pickup;
        }

        public void DropObject(CyanEmuPickupHelper pickup)
        {
            if (currentPickup_ == pickup)
            {
                currentPickup_ = null;
            }
        }

        public void SitPosition(Transform seat)
        {
            transform.position = seat.position;
            UpdateCameraProxyPosition();
        }

        public Camera GetCamera()
        {
            return camera_;
        }

        public float GetCameraScale()
        {
            return camera_.transform.lossyScale.x;
        }

        public Transform GetCameraProxyTransform()
        {
            return cameraProxyObject_;
        }

        public Transform GetArmTransform()
        {
            return rightArmPosition_.transform;
        }

        public VRC_Pickup GetHeldPickup(VRC_Pickup.PickupHand hand)
        {
            if (hand == VRC_Pickup.PickupHand.Right)
            {
                return currentPickup_?.GetComponent<VRC_Pickup>();
            }
            return null;
        }

        public void PlayerDied()
        {
            if (currentPickup_ != null)
            {
                currentPickup_.Drop();
            }
            if (currentStation_ != null)
            {
                currentStation_.ExitStation();
            }
            isDead_ = true;
        }

        public void PlayerRevived()
        {
            isDead_ = false;
        }

        private void Update()
        {
            RotateView();
            if (!jump_ && characterController_.isGrounded && jumpSpeed_ > 0)
            {
                jump_ = Input.GetButton("Jump");
            }

            UpdateStance();
            UpdateMenu();

            if (currentPickup_ != null)
            {
                currentPickup_.UpdatePosition(rightArmPosition_.transform);
                currentPickup_.UpdateUse();
            }

            UpdateCameraProxyPosition();
        }

        private void FixedUpdate()
        {
            Physics.SyncTransforms();
            Vector2 speed;
            Vector2 input;
            Vector2 prevInput = prevInputResult_;
            GetInput(out speed, out input);

            if (currentStation_ != null && !currentStation_.CanPlayerMoveWhileSeated(input.magnitude))
            {
                return;
            }

            if (menu_.activeInHierarchy || isDead_)
            {
                input = Vector2.zero;
                jump_ = false;
            }

            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * input.y * speed.x + transform.right * input.x * speed.y;
            desiredMove.y = 0;

            float gravityContribution = (gravityStrength_ * Physics.gravity * Time.fixedDeltaTime).y;

            if (!velSet)
            {
                if (characterController_.isGrounded)
                {
                    playerRetainedVelocity_ = Vector3.zero;
                    playerRetainedVelocity_.y = -STICK_TO_GROUND_FORCE_;
                    if (jump_)
                    {
                        if (!legacyLocomotion_)
                        {
                            playerRetainedVelocity_ = desiredMove;
                        }
                        playerRetainedVelocity_.y = jumpSpeed_;
                        desiredMove = Vector3.zero;
                        jump_ = false;
                    }
                }
                else
                {
                    // Slowly add velocity from movement inputs
                    if (!legacyLocomotion_)
                    {
                        Vector3 localVelocity = transform.InverseTransformVector(characterController_.velocity);
                        localVelocity.x = Mathf.Clamp(localVelocity.x, -speed.y, speed.y);
                        localVelocity.z = Mathf.Clamp(localVelocity.z, -speed.x, speed.x);

                        Vector3 maxAc = new Vector3(speed.y - localVelocity.x, 0, speed.x - localVelocity.z);
                        Vector3 minAc = new Vector3(-speed.y - localVelocity.x, 0, -speed.x - localVelocity.z);

                        Vector3 inputAcceleration = new Vector3(input.x * speed.y, 0, input.y * speed.x) * Time.fixedDeltaTime * RATE_OF_AIR_ACCELERATION_;
                        inputAcceleration.x = Mathf.Clamp(inputAcceleration.x, minAc.x, maxAc.x);
                        inputAcceleration.z = Mathf.Clamp(inputAcceleration.z, minAc.z, maxAc.z);

                        inputAcceleration = transform.TransformVector(inputAcceleration);
                        playerRetainedVelocity_ += inputAcceleration;
                        desiredMove = Vector3.zero;
                    }
                    // Legacy stutter stepping
                    else if ((input.sqrMagnitude < 1e-3 ^ prevInput.sqrMagnitude < 1e-3))
                    {
                        playerRetainedVelocity_ = Vector3.zero;
                    }
                    playerRetainedVelocity_.y += gravityContribution;
                }
            }
            else // Dumb behavior that hopefully needs to be removed
            {
                characterController_.Move(new Vector3(desiredMove.x * 0.05f, desiredMove.y * 0.05f + gravityContribution, desiredMove.z * 0.05f) * Time.fixedDeltaTime);
                desiredMove = Vector3.zero;
            }

            desiredMove += playerRetainedVelocity_;

            peviouslyGrounded_ = characterController_.isGrounded;
            collisionFlags_ = characterController_.Move(desiredMove * Time.fixedDeltaTime);

            if (!menu_.activeInHierarchy)
            {
                mouseLook_.UpdateCursorLock();
            }

            velSet = false;

            UpdateCameraProxyPosition();
        }

        private void LateUpdate()
        {
            if (currentStation_ != null)
            {
                SitPosition(currentStation_.EnterLocation);
            }
        }

        private void UpdateCameraProxyPosition()
        {
            cameraProxyObject_.position = camera_.transform.position;
            cameraProxyObject_.rotation = camera_.transform.rotation;
            cameraProxyObject_.localScale = camera_.transform.lossyScale;
        }

        private void UpdateMenu()
        {
            if (Input.GetKeyDown(MenuKey))
            {
                ToggleMenu(!menu_.activeInHierarchy);
            }
        }

        private void CloseMenu()
        {
            ToggleMenu(false);
        }

        private void ToggleMenu(bool isOpen)
        {
            mouseLook_.SetCursorLocked(!isOpen);
            menu_.SetActive(isOpen);
            baseInput_.isMenuOpen = isOpen;
        }

        private void UpdateStance()
        {
            bool updatePosition = false;

            if (Input.GetKeyDown(CyanEmuSettings.Instance.crouchKey))
            {
                updatePosition = true;
                if (stance_ == Stance.CROUCHING)
                {
                    stance_ = Stance.STANDING;
                }
                else
                {
                    stance_ = Stance.CROUCHING;
                }
            }
            if (Input.GetKeyDown(CyanEmuSettings.Instance.proneKey))
            {
                updatePosition = true;
                if (stance_ == Stance.PRONE)
                {
                    stance_ = Stance.STANDING;
                }
                else
                {
                    stance_ = Stance.PRONE;
                }
            }
            

            if (updatePosition || (settings_.localPlayerScale != currentScale_))
            {
                currentScale_ = settings_.localPlayerScale;
                Vector3 cameraPosition = playerCamera_.transform.localPosition;
                cameraPosition.y = (stance_ == Stance.STANDING ? (STANDING_HEIGHT_ * settings_.localPlayerScale) : stance_ == Stance.CROUCHING ? (CROUCHING_HEIGHT_ * settings_.localPlayerScale) : (PRONE_HEIGHT_ * settings_.localPlayerScale));
                playerCamera_.transform.localPosition = cameraPosition;
                capsule_.transform.localScale = capsuleSize_ * settings_.localPlayerScale;
                capsule_.transform.localPosition = new Vector3(0, settings_.localPlayerScale, 0);
            }
        }

        private void GetInput(out Vector2 speed, out Vector2 input)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector2 curInput = new Vector2(horizontal, vertical);

            // Prevent sliding without changing the input manager >_>
            horizontal = GetExpectedMovement(horizontal, prevInput_.x, prevInputResult_.x);
            vertical = GetExpectedMovement(vertical, prevInput_.y, prevInputResult_.y);

            isWalking_ = !Input.GetKey(CyanEmuSettings.Instance.runKey);
            
            speed = new Vector2(isWalking_? walkSpeed_ : runSpeed_, strafeSpeed_);
            input = new Vector2(horizontal, vertical);
            prevInput_ = curInput;
            prevInputResult_ = input;

            if (stance_ == Stance.CROUCHING)
            {
                speed *= CROUCH_SPEED_MULTIPLYER_;
            }
            else if (stance_ == Stance.PRONE)
            {
                speed *= PRONE_SPEED_MULTIPLYER_;
            }
            
            if (input.sqrMagnitude > 1)
            {
                input.Normalize();
            }
        }

        // Get the direction of change. If moving towards 1 or -1, return 1 or -1. If moving towards 0, return 0.
        private float GetExpectedMovement(float input, float previous, float previousDecision)
        {
            if (input != 0 && input == previous)
            {
                return previousDecision;
            }
            
            if (input < 0 && input < previous)
            {
                return -1;
            }

            if (input > 0 && input > previous)
            {
                return 1;
            }

            return 0;
        }


        private void RotateView()
        {
            mouseLook_.LookRotation(transform, camera_.transform, currentStation_ != null && !currentStation_.IsMobile);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {

#if VRC_SDK_VRCSDK2

            VRC_Trigger trig = hit.collider.GetComponent<VRC_Trigger>();
            if (trig != null)
            {
                trig.ExecuteTriggerType(VRC_Trigger.TriggerType.OnAvatarHit);
            }
#endif
#if UDON
            // TODO fake implementing OnPlayerColliderEnter/Stay/Exit for Udon with this method.
#endif 

            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (collisionFlags_ == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(characterController_.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }

        private void OnGUI()
        {
            Vector2 center = CyanEmuBaseInput.GetScreenCenter();
            Vector2 size = new Vector2(reticleTexture_.width, reticleTexture_.height);
            Rect position = new Rect(center - size * 0.5f, size);
            GUI.DrawTexture(position, reticleTexture_);
        }
    }

    // Modified from standard assets
    public class MouseLook
    {
        public float XSensitivity = 2f;
        public float YSensitivity = 2f;
        public float MinimumAngle = -90F;
        public float MaximumAngle = 90F;
        public bool lockCursor = true;

        private Quaternion m_CharacterTargetRot;
        private Quaternion m_CameraTargetRot;
        private bool m_cursorIsLocked = false;
        private Transform m_BaseRotation;

        public void Init(Transform character, Transform camera)
        {
            SetBaseRotation(null);
            m_CharacterTargetRot = character.localRotation;
            m_CameraTargetRot = camera.localRotation;
        }

        public void SetRotation(Quaternion newRotation)
        {
            m_CharacterTargetRot = newRotation;
        }

        public void SetBaseRotation(Transform baseRotation)
        {
            m_BaseRotation = baseRotation;
        }

        public void LookRotation(Transform character, Transform camera, bool inStation)
        {
            float yRot = Input.GetAxis("Mouse X") * XSensitivity;
            float xRot = Input.GetAxis("Mouse Y") * YSensitivity;

            if (!m_cursorIsLocked)
            {
                yRot = 0;
                xRot = 0;
            }

            m_CharacterTargetRot *= Quaternion.Euler(0f, yRot, 0f);
            m_CameraTargetRot *= Quaternion.Euler(-xRot, 0f, 0f);
            
            m_CameraTargetRot = ClampRotationAroundAxis(m_CameraTargetRot, 0);

            if (inStation)
            {
                m_CharacterTargetRot = ClampRotationAroundAxis(m_CharacterTargetRot, 1);
                character.localRotation = m_BaseRotation.rotation * m_CharacterTargetRot;
            }
            else
            {
                character.localRotation = m_CharacterTargetRot;
            }
            
            camera.localRotation = m_CameraTargetRot;
        }

        public void UpdateCursorLock()
        {
            if (lockCursor)
            {
                InternalLockUpdate();
            }
        }

        public void SetCursorLocked(bool isLocked)
        {
            m_cursorIsLocked = isLocked;
            InternalLockUpdate();
        }

        private void InternalLockUpdate()
        {
            if (m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        Quaternion ClampRotationAroundAxis(Quaternion q, int index)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angle = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q[index]);

            angle = Mathf.Clamp(angle, MinimumAngle, MaximumAngle);

            q[index] = Mathf.Tan(0.5f * Mathf.Deg2Rad * angle);

            return q;
        }
    }
}
