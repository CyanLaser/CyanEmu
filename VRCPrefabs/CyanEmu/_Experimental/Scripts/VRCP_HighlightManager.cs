// VRCP_HighlightManager
// Created by CyanLaser

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class VRCP_HighlightManager : MonoBehaviour
    {
        private HashSet<VRCP_Interactable> objectsToHighlight_ = new HashSet<VRCP_Interactable>();
        private CommandBuffer glowBuffer_;
        private Camera camera_;
        private Material colorMat_;
        private Material glowMat_;
        private int tempShaderID_;

        public void AddInteractable(VRCP_Interactable interact)
        {
            objectsToHighlight_.Add(interact);
        }

        public void RemoveInteractable(VRCP_Interactable interact)
        {
            objectsToHighlight_.Remove(interact);
        }

        public void ClearInteracts()
        {
            objectsToHighlight_.Clear();
        }


        void Awake()
        {
            camera_ = GetComponent<Camera>();
            
            colorMat_ = new Material(Shader.Find("Unlit/Color"));
            colorMat_.SetColor("_Color", new Color32(0, 255, 255, 255));

            glowMat_ = new Material(Shader.Find("CyanEmu/HighlightShader"));

            glowBuffer_ = new CommandBuffer();
            glowBuffer_.name = "Glow Buffer";

            tempShaderID_ = Shader.PropertyToID("_TempShader1");

            camera_.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, glowBuffer_);
        }

        private void OnPreRender()
        {
            SetupCommandBuffer();
        }

        private void SetupCommandBuffer()
        {
            glowBuffer_.Clear();

            glowBuffer_.GetTemporaryRT(tempShaderID_, -1, -1, 24, FilterMode.Bilinear);
            glowBuffer_.SetRenderTarget(tempShaderID_);
            glowBuffer_.SetGlobalTexture("_GlowMap", tempShaderID_);

            glowBuffer_.ClearRenderTarget(true, true, Color.black);

            foreach (VRCP_Interactable interact in objectsToHighlight_)
            {
                MonoBehaviour behaviour = (MonoBehaviour)interact;

                List<Renderer> renderers = GetEnabledRenderers(behaviour);
                if (renderers.Count > 0)
                {
                    foreach (Renderer rend in renderers)
                    {
                        glowBuffer_.DrawRenderer(rend, colorMat_);
                    }
                }
                else
                {
                    // Get collider on object to render?
                }
            }

            //camera_.AddCommandBuffer(CameraEvent.AfterEverything, glowBuffer_);
        }

        private static List<Renderer> GetEnabledRenderers(MonoBehaviour behaviour)
        {
            List<Renderer> renderers = new List<Renderer>();
            foreach (Renderer rend in behaviour.GetComponentsInChildren<Renderer>())
            {
                if (rend.gameObject.activeInHierarchy && rend.enabled)
                {
                    renderers.Add(rend);
                }
            }

            return renderers;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination, glowMat_);
        }
    }
}