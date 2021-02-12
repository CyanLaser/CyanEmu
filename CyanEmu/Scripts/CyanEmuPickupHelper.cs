using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuPickupHelper : MonoBehaviour, ICyanEmuInteractable
    {
        private const float MAX_PICKUP_DISTANCE_ = 0.25f;
        private static Quaternion GRIP_OFFSET_ROTATION_ = Quaternion.Euler(0, 0, -90);
        private static Quaternion GUN_OFFSET_ROTATION_ = Quaternion.Euler(-90, 0, -90);

        private VRC_Pickup pickup_;
        private Rigidbody rigidbody_;

        private bool wasKinematic_;
        private bool isHeld_;

        private Vector3 positionOffset_;
        private Quaternion rotationOffset_ = Quaternion.identity;

        public static void InitializePickup(VRC_Pickup pickup)
        {
            if (pickup.gameObject.GetComponent<CyanEmuPickupHelper>() != null)
            {
                pickup.LogWarning("Multiple VRC_Pickup components on the same gameobject! " + VRC.Tools.GetGameObjectPath(pickup.gameObject));
                return;
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

        public void UpdatePosition(Transform root)
        {
            transform.position = root.transform.position + root.TransformDirection(positionOffset_);
            transform.rotation = root.transform.rotation * rotationOffset_;
        }

        public void UpdateUse()
        {
            if (pickup_.AutoHold == VRC_Pickup.AutoHoldMode.Yes)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    Drop();
                    return;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    this.Log("Pickup Use Down");
                    gameObject.OnPickupUseDown();
                }
                if (Input.GetMouseButtonUp(0))
                {
                    this.Log("Pickup Use Up");
                    gameObject.OnPickupUseUp();
                }
            }
            else
            {
                if (Input.GetMouseButtonUp(0))
                {
                    Drop();
                }
            }
        }

        public void Pickup()
        {
            if (isHeld_)
            {
                return;
            }
            
            isHeld_ = true;

            gameObject.OnPickup();

            CyanEmuPlayerController player = CyanEmuPlayerController.instance;
            if (player == null)
            {
                this.LogWarning("Unable to pickup object when there is no player!");
                return;
            }

            player.PickupObject(this);

            this.Log("Picking up object " + name);

            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            wasKinematic_ = rigidbody_.isKinematic;
            rigidbody_.isKinematic = true;

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
        }

        public void Drop()
        {
            if (!isHeld_)
            {
                return;
            }
            
            this.Log("Dropping object " + name);
            isHeld_ = false;

            gameObject.OnDrop();

            if (CyanEmuPlayerController.instance == null)
            {
                return;
            }

            CyanEmuPlayerController.instance.DropObject(this);
            rigidbody_.isKinematic = wasKinematic_;
        }

        public void SetKinematic(bool isKinematic)
        {
            if (isHeld_)
            {
                wasKinematic_ = isKinematic;
            }
            else
            {
                rigidbody_.isKinematic = isKinematic;
            }
        }

        public void SetGravity(bool hasGravity)
        {
            rigidbody_.useGravity = hasGravity;
        }

        public string PickupText()
        {
            // TODO
            return "";
        }
    }
}
