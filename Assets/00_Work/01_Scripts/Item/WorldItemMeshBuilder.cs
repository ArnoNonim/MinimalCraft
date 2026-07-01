using _00_Work._01_Scripts.ChunkSystem.Block;
using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    public static class WorldItemMeshBuilder
    {
        static readonly Vector3[][] FaceVertices = {
            new[]{ new Vector3(0,1,1), new Vector3(1,1,1),
                new Vector3(1,1,0), new Vector3(0,1,0) },
            new[]{ new Vector3(0,0,0), new Vector3(1,0,0),
                new Vector3(1,0,1), new Vector3(0,0,1) },
            new[]{ new Vector3(0,0,0), new Vector3(0,0,1),
                new Vector3(0,1,1), new Vector3(0,1,0) },
            new[]{ new Vector3(1,0,1), new Vector3(1,0,0),
                new Vector3(1,1,0), new Vector3(1,1,1) },
            new[]{ new Vector3(0,0,1), new Vector3(1,0,1),
                new Vector3(1,1,1), new Vector3(0,1,1) },
            new[]{ new Vector3(1,0,0), new Vector3(0,0,0),
                new Vector3(0,1,0), new Vector3(1,1,0) },
        };

        public static Mesh Build(BlockType blockType, BlockDataSO blockData)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris  = new System.Collections.Generic.List<int>();
            var uvs   = new System.Collections.Generic.List<Vector2>();

            // 6면 전부 생성
            for (int d = 0; d < 6; d++)
            {
                int vertStart = verts.Count;

                // 중심이 (0,0,0)이 되도록 오프셋
                foreach (var v in FaceVertices[d])
                    verts.Add(v - new Vector3(0.5f, 0.5f, 0.5f));

                tris.Add(vertStart);     tris.Add(vertStart + 1);
                tris.Add(vertStart + 2); tris.Add(vertStart);
                tris.Add(vertStart + 2); tris.Add(vertStart + 3);

                var faceUVs = blockData.GetUVs(blockType, d);
                uvs.AddRange(faceUVs);
            }

            var mesh = new Mesh();
            mesh.vertices  = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.uv        = uvs.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}