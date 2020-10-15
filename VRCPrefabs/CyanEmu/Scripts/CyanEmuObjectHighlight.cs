using UnityEngine;

namespace VRCPrefabs.CyanEmu
{
    [AddComponentMenu("")]
    public class CyanEmuObjectHighlight : MonoBehaviour
    {
        private static Material highlightMaterial_;

        private Renderer interactHighlight_;
        private Collider target_;

        private static Material GetHighlightMaterial()
        {
            if (highlightMaterial_ == null)
            {
                highlightMaterial_ = new Material(Shader.Find("UI/Default"));
                highlightMaterial_.SetColor("_Color", new Color32(0, 255, 255, 50));
            }
            return highlightMaterial_;
        }

        public static CyanEmuObjectHighlight CreateInteractHelper()
        {
            GameObject interact = GameObject.CreatePrimitive(PrimitiveType.Cube);
            interact.name = "Highlight";
            DestroyImmediate(interact.GetComponent<BoxCollider>());
            CyanEmuObjectHighlight highlight = interact.AddComponent<CyanEmuObjectHighlight>();
            highlight.interactHighlight_ = interact.GetComponent<Renderer>();
            highlight.interactHighlight_.sharedMaterial = GetHighlightMaterial();
            return highlight;
        }

        public void HighlightCollider(Collider collider)
        {
            target_ = collider;
            UpdateInteractLocation(target_);
        }

        public void UpdateInteractLocation(Collider collider)
        {
            transform.transform.position = collider.bounds.center;
            transform.rotation = collider.transform.rotation;
            transform.localScale = GetColliderSize(collider) * 1.01f;
        }

        private Vector3 GetColliderSize(Collider collider)
        {
            Vector3 scale = collider.transform.lossyScale;
            if (collider.GetType() == typeof(BoxCollider))
            {
                Vector3 size = (collider as BoxCollider).size;
                return new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
            }
            else if (collider.GetType() == typeof(SphereCollider))
            {
                float max = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                return Vector3.one * max * (collider as SphereCollider).radius * 2;
            }
            else if (collider.GetType() == typeof(CapsuleCollider))
            {
                CapsuleCollider capsule = (collider as CapsuleCollider);
                float max = Mathf.Max(scale[(capsule.direction + 1) % 3], scale[(capsule.direction + 2) % 3]);
                Vector3 size = Vector3.one * capsule.radius * 2 * max;
                size[capsule.direction] = Mathf.Max(capsule.height * scale[capsule.direction], size[capsule.direction]);
                return size;
            }
            else if (collider.GetType() == typeof(MeshCollider))
            {
                MeshCollider meshCollider = (collider as MeshCollider);
                Vector3 size = meshCollider.sharedMesh.bounds.size;
                return new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
            }

            return collider.bounds.size;
        }

        private void OnWillRenderObject()
        {
            if (interactHighlight_.enabled && target_ != null)
            {
                UpdateInteractLocation(target_);
            }
        }
    }
}
