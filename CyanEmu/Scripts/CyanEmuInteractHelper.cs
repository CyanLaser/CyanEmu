using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuInteractHelper : MonoBehaviour
    {
        private const int MAX_INTERACT_RAYCASTS_ = 100;
        private static readonly LayerMask INTERACT_LAYERS_ = ~(1 << 18); // All layers but MirrorReflection

        private static readonly RaycastHit[] raycastHitBuffer = new RaycastHit[MAX_INTERACT_RAYCASTS_];
        private static readonly RaycastHitComparer raycastHitComparer = new RaycastHitComparer();

        //public CyanEmuHighlightManager highlightManager;

        private CyanEmuObjectHighlight highlight_;
        private GameObject toolTip_;
        private TextMesh toolTipText_;
        private Transform cameraTransform_;
        private Transform raycastTransform_;
        private Func<bool> shouldCheckForInteracts_;

        private void Start()
        {
            highlight_ = CyanEmuObjectHighlight.CreateInteractHelper();
            highlight_.transform.parent = transform;

            // Tool tip text
            toolTip_ = new GameObject("ToolTip");
            toolTip_.transform.parent = transform;
            GameObject child = new GameObject("ToolTipText");
            child.transform.parent = toolTip_.transform;
            child.transform.localRotation = Quaternion.Euler(0, 180, 0);
            child.transform.localPosition = new Vector3(0, .05f, 0);

            toolTipText_ = child.AddComponent<TextMesh>();
            toolTipText_.anchor = TextAnchor.LowerCenter;
            toolTipText_.characterSize = 0.01f;
            toolTipText_.fontSize = 100;
            toolTipText_.text = "";

            SetEnabled(false);
        }

        public void Initialize(Transform cameraTransform, Transform raycastTransform, Func<bool> shouldCheckForInteracts)
        {
            cameraTransform_ = cameraTransform;
            raycastTransform_ = raycastTransform;
            shouldCheckForInteracts_ = shouldCheckForInteracts;
        }

        private void LateUpdate()
        {
            CheckForInteracts();
        }

        private void CheckForInteracts()
        {
            // Disable interact check if holding pickup
            if (!shouldCheckForInteracts_())
            {
                SetEnabled(false);
                return;
            }

            //highlightManager.ClearInteracts();

            bool display = false;
            Ray ray = new Ray(raycastTransform_.position, raycastTransform_.forward);
            int hitCount = Physics.RaycastNonAlloc(ray, raycastHitBuffer, float.MaxValue, INTERACT_LAYERS_);

            Array.Sort(raycastHitBuffer, 0, hitCount, raycastHitComparer);

            // Go through all colliders in order of distance and stop after find something
            // interactable, or a physical collider blocking everything else. 
            for (int curHit = 0; curHit < hitCount && !display; ++curHit)
            {
                RaycastHit hit = raycastHitBuffer[curHit];
                GameObject hitObject = hit.collider.gameObject;
                ICyanEmuInteractable interactable = hitObject.GetClosestInteractable(hit.distance);

                if (interactable != null)
                {
                    //highlightManager.AddInteractable(foundInteractable);

                    HighlightCollider(hit.collider, interactable.GetInteractText());
                    display = true;

                    // TODO get input from input manager
                    if (Input.GetMouseButtonDown(0))
                    {
                        hitObject.Interact(hit.distance);
                    }
                }

                if (!hit.collider.isTrigger)
                {
                    break;
                }
            }

            SetEnabled(display);
        }

        public void SetEnabled(bool enabled)
        {
            if (highlight_.gameObject.activeInHierarchy != enabled)
            {
                highlight_.gameObject.SetActive(enabled);
                toolTip_.SetActive(enabled);
            }
        }

        public void HighlightCollider(Collider collider, string toolTip)
        {
            highlight_.HighlightCollider(collider);
            toolTipText_.text = toolTip;

            toolTip_.transform.position = collider.transform.position;
            toolTip_.transform.LookAt(cameraTransform_);
        }

        class RaycastHitComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit x, RaycastHit y)
            {
                return x.distance.CompareTo(y.distance);
            }
        }
    }
}