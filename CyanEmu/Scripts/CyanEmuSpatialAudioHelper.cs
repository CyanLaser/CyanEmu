using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class CyanEmuSpatialAudioHelper : ONSPAudioSource
    {
        private const float EPS_ = 1e-3f;

        private VRC_SpatialAudioSource spatialAudioSource_;
        private AudioSource audioSource_;
        private bool useAudioSourceCurve;
        private ONSPAudioSource onsp_;
        private bool forceUpdate_ = true;

        public static void InitializeAudio(VRC_SpatialAudioSource obj)
        {
            // Why?
            if (!Application.isPlaying || string.IsNullOrEmpty(obj.gameObject.scene.path))
            {
                return;
            }

            CyanEmuSpatialAudioHelper spatialAudio = obj.GetComponent<CyanEmuSpatialAudioHelper>();
            if (spatialAudio != null)
            {
                DestroyImmediate(spatialAudio);
            }
            
            spatialAudio = obj.gameObject.AddComponent<CyanEmuSpatialAudioHelper>();
            spatialAudio.SetSpatializer(obj);
        }

        private void SetSpatializer(VRC_SpatialAudioSource obj)
        {
            spatialAudioSource_ = obj;
            audioSource_ = GetComponent<AudioSource>();
            onsp_ = this;
            forceUpdate_ = true;

            UpdateSettings();
        }

        private void OnEnable()
        {
            // ONSP needs to reapply audio settings everytime the object is enabled.
            forceUpdate_ = true;
        }

        // Late update to help with testing
        private void LateUpdate()
        {
            UpdateSettings();
        }

        private void UpdateSettings()
        {
            if (spatialAudioSource_ == null)
            {
                spatialAudioSource_ = GetComponent<VRC_SpatialAudioSource>();
                if (spatialAudioSource_ == null)
                {
                    Destroy(this);
                }
                SetSpatializer(spatialAudioSource_);  
                return;
            }

            // Check if we need to make changes.
            if (
                onsp_.EnableSpatialization != spatialAudioSource_.EnableSpatialization ||
                onsp_.Gain != spatialAudioSource_.Gain ||
                onsp_.Near != spatialAudioSource_.Near ||
                onsp_.Far != spatialAudioSource_.Far ||
                useAudioSourceCurve != spatialAudioSource_.UseAudioSourceVolumeCurve
            ) {
                forceUpdate_ = true;
            }
            
            onsp_.EnableSpatialization = spatialAudioSource_.EnableSpatialization;
            onsp_.Gain = spatialAudioSource_.Gain;
            useAudioSourceCurve = spatialAudioSource_.UseAudioSourceVolumeCurve;
            onsp_.Near = spatialAudioSource_.Near;
            onsp_.Far = spatialAudioSource_.Far;
            onsp_.VolumetricRadius = spatialAudioSource_.VolumetricRadius;
            
            onsp_.SetParameters(ref audioSource_);
            
            if (!onsp_.EnableSpatialization)
            {
                return;
            }

            if (!forceUpdate_)
            {
                return;
            }
            
            forceUpdate_ = false;

            if (!spatialAudioSource_.UseAudioSourceVolumeCurve)
            {
                float near = onsp_.VolumetricRadius + onsp_.Near;
                float far = onsp_.VolumetricRadius + Mathf.Max(near, onsp_.Far + EPS_);

                audioSource_.maxDistance = far;
                
                CreateRolloffCurve(near, far);
                CreateSpatialCurve(near, far);
            }
        }

        // Create volume rolloff curve where Volumetric + near is volume 1, then 2^-x fall off to far.
        private void CreateRolloffCurve(float near, float far)
        {
            audioSource_.rolloffMode = AudioRolloffMode.Custom;

            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(new Keyframe(near, 1));
            int max = 8;
            for (int loc = 1; loc < max; ++loc)
            {
                float time = near + Mathf.Pow(2, loc - max) * (far - near);
                float value = Mathf.Pow(2.2f, -loc);
                curve.AddKey(new Keyframe(time, value));
            }
            curve.AddKey(new Keyframe(far, 0));

            for (int i = 0; i < curve.length; ++i)
            {
                curve.SmoothTangents(i, 0);
            }

            audioSource_.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
        }

        // Create spatial blend curve so that it goes from (Setting) to 3d from min to max
        private void CreateSpatialCurve(float near, float far)
        {
            AnimationCurve spatialCurve = new AnimationCurve();
            spatialCurve.AddKey(0, audioSource_.spatialBlend);
            spatialCurve.AddKey(onsp_.VolumetricRadius, audioSource_.spatialBlend);

            Keyframe nearFrame = new Keyframe(near + EPS_, 1);
            nearFrame.outTangent = 0;
            spatialCurve.AddKey(nearFrame);

            Keyframe farFrame = new Keyframe(far, 1);
            farFrame.inTangent = 0;
            spatialCurve.AddKey(farFrame);

            audioSource_.SetCustomCurve(AudioSourceCurveType.SpatialBlend, spatialCurve);
        }
    }
}