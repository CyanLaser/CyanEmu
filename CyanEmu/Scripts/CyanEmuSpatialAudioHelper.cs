using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuSpatialAudioHelper : ONSPAudioSource
    {
        private const float EPS_ = 1e-3f;

        private VRC_SpatialAudioSource spatialAudioSource_;
        private AudioSource audioSource_;
        private bool useAudioSourceCurve;

        public static void InitializeAudio(VRC_SpatialAudioSource obj)
        {
            // Why?
            if (!Application.isPlaying)
            {
                return;
            }

            obj.gameObject.AddComponent<CyanEmuSpatialAudioHelper>().SetSpatializer(obj);
        }

        public void SetSpatializer(VRC_SpatialAudioSource obj)
        {
            spatialAudioSource_ = obj;
            audioSource_ = GetComponent<AudioSource>();

            UpdateSettings();
        }
        
        // Late update to help with testing
        private void LateUpdate()
        {
            UpdateSettings();
        }

        private void UpdateSettings()
        {
            if (audioSource_ == null)
            {
                Destroy(this);
                return;
            }

            // Check if we need to make changes.
            if (
                EnableSpatialization == spatialAudioSource_.EnableSpatialization &&
                Gain == spatialAudioSource_.Gain &&
                Near == spatialAudioSource_.Near &&
                Far == spatialAudioSource_.Far &&
                useAudioSourceCurve == spatialAudioSource_.UseAudioSourceVolumeCurve
            )
            {
                return;
            }

            EnableSpatialization = spatialAudioSource_.EnableSpatialization;
            Gain = spatialAudioSource_.Gain;
            useAudioSourceCurve = spatialAudioSource_.UseAudioSourceVolumeCurve;

            if (!EnableSpatialization)
            {
                Reset();
                return;
            }

            if (spatialAudioSource_.UseAudioSourceVolumeCurve)
            {
                // Sure why not. This looks good enough.
                Near = audioSource_.minDistance;
                Far = audioSource_.maxDistance;
            }
            else 
            {
                VolumetricRadius = spatialAudioSource_.VolumetricRadius;
                Near = spatialAudioSource_.Near;
                Far = spatialAudioSource_.Far;

                float near = VolumetricRadius + Near;
                float far = VolumetricRadius + Mathf.Max(near, Far + EPS_);

                audioSource_.maxDistance = far;
                
                CreateRolloffCurve(near, far);
                CreateSpatialCurve(near, far);
            }

            Reset();
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
            spatialCurve.AddKey(VolumetricRadius, audioSource_.spatialBlend);

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