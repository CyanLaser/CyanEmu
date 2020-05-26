using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SpatialTracking;
using VRC.SDKBase;


#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace VRCPrefabs.CyanEmu
{
    public class VRCP_VRTest : MonoBehaviour
    {
        private GameObject cameraObject_;
        private Camera camera_;

        private GameObject leftController_;
        private GameObject rightController_;

        private TrackedPoseDriver left_;
        private TrackedPoseDriver right_;


        private void Awake()
        {
            cameraObject_ = new GameObject("VR Camera");
            cameraObject_.transform.parent = transform;
            camera_ = cameraObject_.AddComponent<Camera>();
            cameraObject_.AddComponent<AudioListener>();

            // TODO Get camera settings
            CopyCameraSettings();

            leftController_ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftController_.transform.parent = transform;
            leftController_.GetComponent<BoxCollider>().isTrigger = true;
            leftController_.transform.localScale = Vector3.one * 0.1f;
            left_ = leftController_.AddComponent<TrackedPoseDriver>();
            left_.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRController, TrackedPoseDriver.TrackedPose.LeftPose);

            rightController_ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightController_.transform.parent = transform;
            rightController_.GetComponent<BoxCollider>().isTrigger = true;
            rightController_.transform.localScale = Vector3.one * 0.1f;
            right_ = rightController_.AddComponent<TrackedPoseDriver>();
            right_.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRController, TrackedPoseDriver.TrackedPose.RightPose);

            InputTracking.nodeAdded += InputTracking_nodeAdded;
            InputTracking.nodeRemoved += InputTracking_nodeRemoved;

            StartCoroutine(LoadDevice("OpenVR"));
        }

        private void CopyCameraSettings()
        {
            Camera refCamera = null;
            VRC_SceneDescriptor descriptor = VRC_SceneDescriptor.Instance;
            if (descriptor != null && descriptor.ReferenceCamera != null)
            {
                refCamera = descriptor.ReferenceCamera.GetComponent<Camera>();
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

            if (refCamera != null)
            {
                camera_.farClipPlane = refCamera.farClipPlane;
                camera_.nearClipPlane = refCamera.nearClipPlane;
                camera_.clearFlags = refCamera.clearFlags;
                camera_.backgroundColor = refCamera.backgroundColor;

#if UNITY_POST_PROCESSING_STACK_V2
                PostProcessLayer refPostProcessLayer = refCamera.GetComponent<PostProcessLayer>();
                if (refPostProcessLayer != null)
                {
                    PostProcessLayer postProcessLayer = camera_.gameObject.AddComponent<PostProcessLayer>();
                    postProcessLayer.volumeLayer = refPostProcessLayer.volumeLayer;
                    postProcessLayer.volumeTrigger = refPostProcessLayer.volumeTrigger;
                    postProcessLayer.enabled = refPostProcessLayer.enabled;
                }
#endif
                refCamera.gameObject.SetActive(false);
            }
        }

        public Camera GetCamera()
        {
            return camera_;
        }

        private void Update()
        {
        }

        private void InputTracking_nodeAdded(XRNodeState obj)
        {
            Debug.Log(obj.nodeType);
            // If right or left controller, 
        }

        private void InputTracking_nodeRemoved(XRNodeState obj)
        {
            Debug.Log(obj.nodeType);
            // If right or left controller, Update position to under camera
        }

        IEnumerator LoadDevice(string newDevice)
        {
            if (string.Compare(XRSettings.loadedDeviceName, newDevice, true) != 0)
            {
                XRSettings.LoadDeviceByName(newDevice);
                yield return null;
                XRSettings.enabled = true;
            }
        }

        // TODO Update method which properly sets stance for desktop users
        // TODO Create tracking data pose providers
    }
}