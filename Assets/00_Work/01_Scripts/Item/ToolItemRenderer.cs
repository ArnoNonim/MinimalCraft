using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    public class ToolItemRenderer : MonoBehaviour
    {
        [Header("설정")]
        public Texture2D toolTexture;
        public float     thickness  = 0.08f;
        public Material  toolMaterial;

        void Awake()
        {
            GenerateMesh();
        }

        public void GenerateMesh()
        {
            if (toolTexture == null) return;

            var mf = GetComponent<MeshFilter>()
                     ?? gameObject.AddComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>()
                     ?? gameObject.AddComponent<MeshRenderer>();

            mf.mesh     = PixelMeshBuilder.Build(toolTexture, thickness);
            mr.material = toolMaterial;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            GenerateMesh();
        }
#endif
    }
}