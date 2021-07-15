using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuPickupHelper : MonoBehaviour, ICyanEmuInteractable
    {
        // If the user releases the mouse button before this time, it will not fire on use up. 
        private const float INITIAL_PICKUP_DURATION_ = 0.5f;
        private const float MAX_PICKUP_DISTANCE_ = 0.25f;
        private static Quaternion GRIP_OFFSET_ROTATION_ = Quaternion.Euler(0, 45, 0);
        private static Quaternion GUN_OFFSET_ROTATION_ = Quaternion.Euler(0, -45, 0);
        
        private Rigidbody rigidbody_;
        private VRC_Pickup pickup_;

        private bool isHeld_;

        private Vector3 positionOffset_;
        private Quaternion rotationOffset_ = Quaternion.identity;

        private bool initialGrab_;
        private float grabActionStartTime_;
        private float dropActionStartTime_;

        public static void InitializePickup(VRC_Pickup pickup)
        {
            CyanEmuPickupHelper previousHelper = pickup.gameObject.GetComponent<CyanEmuPickupHelper>();
            if (previousHelper != null)
            {
                DestroyImmediate(previousHelper);
                pickup.LogWarning("Destroying old pickup helper on object: " + VRC.Tools.GetGameObjectPath(pickup.gameObject));
            }

            CyanEmuPickupHelper helper = pickup.gameObject.AddComponent<CyanEmuPickupHelper>();
            helper.SetPickup(pickup);
        }

        public static void ForceDrop(VRC_Pickup pickup)
        {
            CyanEmuPickupHelper helper = pickup.GetComponent<CyanEmuPickupHelper>();
            helper.Drop();
        }

        public static VRCPlayerApi GetCurrentPlayer(VRC_Pickup pickup)
        {
            CyanEmuPickupHelper helper = pickup.GetComponent<CyanEmuPickupHelper>();
            if (helper.isHeld_)
            {
                return Networking.LocalPlayer;
            }
            return null;
        }

        public static VRC_Pickup.PickupHand GetPickupHand(VRC_Pickup pickup)
        {
            CyanEmuPickupHelper helper = pickup.GetComponent<CyanEmuPickupHelper>();
            if (helper.isHeld_)
            {
                return VRC_Pickup.PickupHand.Right;
            }
            return VRC_Pickup.PickupHand.None;
        }

        private void SetPickup(VRC_Pickup pickup)
        {
            pickup_ = pickup;
            rigidbody_ = GetComponent<Rigidbody>();
        }

        public Rigidbody GetRigidbody()
        {
            return rigidbody_;
        } 

        public float GetProximity()
        {
            return pickup_.proximity;
        }

        public bool CanInteract()
        {
            return pickup_.pickupable;
        }

        public string GetInteractText()
        {
            if (!string.IsNullOrEmpty(pickup_.InteractionText))
            {
                return pickup_.InteractionText;
            }

            return "Hold to Grab";
        }

        public void Interact()
        {
            Pickup();
        }

        public void UpdatePosition(Transform root, bool force = false)
        {
            if (rigidbody_.isKinematic || force)
            {
                transform.position = root.transform.position + root.TransformDirection(positionOffset_);
                transform.rotation = root.transform.rotation * rotationOffset_;
            }
        }

        public void UpdateUse()
        {
            int dropIndex = 0;
            if (pickup_.AutoHold == VRC_Pickup.AutoHoldMode.Yes)
            {
                dropIndex = 1;
                if (Input.GetMouseButtonDown(dropIndex))
                {
                    dropActionStartTime_ = Time.time;
                }

                float grabDuration = Time.time - grabActionStartTime_;
                if (grabDuration > INITIAL_PICKUP_DURATION_)
                {
                    if (Input.GetMouseButtonDown(0) || (initialGrab_ && Input.GetMouseButton(0)))
                    {
                        this.Log("Pickup Use Down");
                        gameObject.OnPickupUseDown();
                        initialGrab_ = false;
                    }
                    if (Input.GetMouseButtonUp(0))
                    {
                        this.Log("Pickup Use Up");
                        gameObject.OnPickupUseUp();
                    }
                }
            }
            
            if (Input.GetMouseButtonUp(dropIndex))
            {
                Drop();
            }
        }

        public void Pickup()
        {
            if (isHeld_)
            {
                return;
            }
            
            isHeld_ = true;
            grabActionStartTime_ = Time.time;
            initialGrab_ = true;

            gameObject.OnPickup();

            CyanEmuPlayerController player = CyanEmuPlayerController.instance;
            if (player == null)
            {
                this.LogWarning("Unable to pickup object when there is no player!");
                return;
            }

            this.Log("Picking up object " + name);

            Networking.SetOwner(Networking.LocalPlayer, gameObject);


            // Calculate offest
            Transform pickupHoldPoint = null;

            Quaternion offsetRotation = Quaternion.identity;
            if (pickup_.orientation == VRC_Pickup.PickupOrientation.Grip && pickup_.ExactGrip != null)
            {
                pickupHoldPoint = pickup_.ExactGrip;
                offsetRotation = GRIP_OFFSET_ROTATION_;
            }
            else if (pickup_.orientation == VRC_Pickup.PickupOrientation.Gun && pickup_.ExactGun != null)
            {
                pickupHoldPoint = pickup_.ExactGun;
                offsetRotation = GUN_OFFSET_ROTATION_;
            }
            
            Transform arm = player.GetArmTransform();

            // Grab as if no pickup point
            if (pickupHoldPoint == null)
            {
                rotationOffset_ = Quaternion.Inverse(arm.rotation) * transform.rotation;
                positionOffset_ = arm.InverseTransformDirection(transform.position - arm.position);

                float mag = positionOffset_.magnitude;
                if (mag > MAX_PICKUP_DISTANCE_ && pickup_.orientation == VRC_Pickup.PickupOrientation.Any)
                {
                    positionOffset_ = positionOffset_.normalized * MAX_PICKUP_DISTANCE_;
                }
            }
            else
            {
                rotationOffset_ = offsetRotation * Quaternion.Inverse(Quaternion.Inverse(transform.rotation) * pickupHoldPoint.rotation);
                positionOffset_ = rotationOffset_ * transform.InverseTransformDirection(transform.position - pickupHoldPoint.position);
            }
            
            player.PickupObject(this);
        }

        public void Drop()
        {
            if (!isHeld_)
            {
                return;
            }
            
            this.Log("Dropping object " + name);
            isHeld_ = false;
            initialGrab_ = false;

            gameObject.OnDrop();

            CyanEmuPlayerController player = CyanEmuPlayerController.instance;
            if (player == null)
            {
                return;
            }

            player.DropObject(this);
            
            // Calculate throw velocity
            if (!rigidbody_.isKinematic && pickup_.AutoHold == VRC_Pickup.AutoHoldMode.Yes)
            {
                float holdDuration = Mathf.Clamp(Time.time - dropActionStartTime_, 0, 3);
                if (holdDuration > 0.2f)
                {
                    Transform rightArm = player.GetArmTransform();
                    Vector3 throwForce = rightArm.forward * (holdDuration * 500 * pickup_.ThrowVelocityBoostScale);
                    rigidbody_.AddForce(throwForce);
                    this.Log("Adding throw force: "+ throwForce);
                }
            }
        }

        public string PickupText()
        {
            // TODO
            return "";
        }
    }
}
