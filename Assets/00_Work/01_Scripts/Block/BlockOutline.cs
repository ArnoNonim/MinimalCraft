using _00_Work._01_Scripts.ChunkSystem.Block;
using UnityEngine;

namespace _00_Work._01_Scripts.Block
{
    [RequireComponent(typeof(Camera))]
    public class BlockOutline : MonoBehaviour
    {
        [Header("참조")]
        public BlockHighlighter highlighter;

        [Header("색상")]
        public Color normalOutlineColor   = Color.black;
        public Color interactOutlineColor = Color.yellow;

        private Material _lineMat;

        void Awake()
        {
            _lineMat = new Material(
                Shader.Find("Hidden/Internal-Colored"));

            _lineMat.hideFlags =
                HideFlags.HideAndDontSave;

            _lineMat.SetInt("_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha);

            _lineMat.SetInt("_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            _lineMat.SetInt("_Cull",
                (int)UnityEngine.Rendering.CullMode.Off);

            _lineMat.SetInt("_ZWrite", 0);
        }

        void OnRenderObject()
        {
            if (highlighter == null)
                return;

            if (!highlighter.TryGetTargetBlock(
                    out Vector3 center,
                    out BlockType blockType))
                return;

            Color color =
                BlockInteractionUtility.IsInteractable(blockType)
                    ? interactOutlineColor
                    : normalOutlineColor;

            float s = 0.502f;

            Vector3[] v =
            {
                center + new Vector3(-s,-s,-s),
                center + new Vector3( s,-s,-s),
                center + new Vector3( s, s,-s),
                center + new Vector3(-s, s,-s),

                center + new Vector3(-s,-s, s),
                center + new Vector3( s,-s, s),
                center + new Vector3( s, s, s),
                center + new Vector3(-s, s, s)
            };

            GL.PushMatrix();

            _lineMat.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Color(color);

            DrawLine(v[0], v[1]);
            DrawLine(v[1], v[2]);
            DrawLine(v[2], v[3]);
            DrawLine(v[3], v[0]);

            DrawLine(v[4], v[5]);
            DrawLine(v[5], v[6]);
            DrawLine(v[6], v[7]);
            DrawLine(v[7], v[4]);

            DrawLine(v[0], v[4]);
            DrawLine(v[1], v[5]);
            DrawLine(v[2], v[6]);
            DrawLine(v[3], v[7]);

            GL.End();

            GL.PopMatrix();
        }

        void DrawLine(Vector3 a, Vector3 b)
        {
            GL.Vertex(a);
            GL.Vertex(b);
        }
    }
}