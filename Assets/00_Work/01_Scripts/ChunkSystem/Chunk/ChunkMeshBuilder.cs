using System.Collections.Generic;
using UnityEngine;
using _00_Work._01_Scripts.ChunkSystem.Block;

namespace _00_Work._01_Scripts.ChunkSystem.Chunk
{
    public class ChunkMeshBuilder
    {
        static readonly Vector3Int[] directions = {
            Vector3Int.up, Vector3Int.down,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.forward, Vector3Int.back
        };

        static readonly Vector3[][] faceVertices = {
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

        // 월드 y 범위: -YOffset ~ Height-YOffset-1
        private static readonly int WorldYMin = -ChunkData.YOffset;
        private static readonly int WorldYMax =  ChunkData.Height - ChunkData.YOffset - 1;

        public static void Build(
            ChunkData chunk,
            Vector2Int chunkPos,
            Dictionary<Vector2Int, ChunkData> allChunks,
            BlockDataSO blockData,
            MeshFilter mf,
            MeshRenderer mr)
        {
            var verts      = new List<Vector3>();
            var uvs        = new List<Vector2>();
            var opaqueTris = new List<int>();

            var leavesVerts = new List<Vector3>();
            var leavesUVs   = new List<Vector2>();
            var leavesTris  = new List<int>();

            var waterVerts = new List<Vector3>();
            var waterUVs   = new List<Vector2>();
            var waterTris  = new List<int>();

            // 월드 y 기준으로 순회 (-YOffset ~ Height-YOffset-1)
            for (int x = 0; x < ChunkData.Width;  x++)
            for (int y = WorldYMin; y <= WorldYMax; y++)
            for (int z = 0; z < ChunkData.Width;  z++)
            {
                byte block = chunk.GetBlock(x, y, z);
                if (block == (byte)BlockType.Air) continue;

                bool isLeaves = block == (byte)BlockType.Leaves;
                bool isWater  = block == (byte)BlockType.Water;

                for (int d = 0; d < 6; d++)
                {
                    var  dir      = directions[d];
                    byte neighbor = GetBlock(
                        x + dir.x, y + dir.y, z + dir.z,
                        chunkPos, chunk, allChunks);

                    if (isWater)
                    {
                        if (neighbor == (byte)BlockType.Water) continue;
                        if (neighbor != (byte)BlockType.Air)   continue;
                    }
                    else if (isLeaves)
                    {
                        if (neighbor != (byte)BlockType.Air    &&
                            neighbor != (byte)BlockType.Water  &&
                            neighbor != (byte)BlockType.Leaves) continue;
                    }
                    else
                    {
                        if (neighbor != (byte)BlockType.Air    &&
                            neighbor != (byte)BlockType.Water  &&
                            neighbor != (byte)BlockType.Leaves) continue;
                    }

                    var     faceUVs = blockData.GetUVs((BlockType)block, d);
                    // 메시 버텍스 오프셋은 월드 y 그대로 사용
                    Vector3 offset  = new Vector3(x, y, z);

                    if (isWater)
                    {
                        int vertStart = waterVerts.Count;
                        var vFace     = faceVertices[d];

                        if (d == 0)
                        {
                            foreach (var v in vFace)
                                waterVerts.Add(v + offset + Vector3.down * 0.1f);
                        }
                        else
                        {
                            foreach (var v in vFace)
                                waterVerts.Add(v + offset);
                        }

                        waterTris.Add(vertStart);     waterTris.Add(vertStart + 1);
                        waterTris.Add(vertStart + 2); waterTris.Add(vertStart);
                        waterTris.Add(vertStart + 2); waterTris.Add(vertStart + 3);
                        waterUVs.AddRange(faceUVs);
                    }
                    else if (isLeaves)
                    {
                        if (neighbor == (byte)BlockType.Leaves) continue;

                        int vertStart = leavesVerts.Count;

                        foreach (var v in faceVertices[d])
                            leavesVerts.Add(v + offset);

                        leavesTris.Add(vertStart);     leavesTris.Add(vertStart + 1);
                        leavesTris.Add(vertStart + 2); leavesTris.Add(vertStart);
                        leavesTris.Add(vertStart + 2); leavesTris.Add(vertStart + 3);
                        leavesUVs.AddRange(faceUVs);
                    }
                    else
                    {
                        if (neighbor != (byte)BlockType.Air &&
                            neighbor != (byte)BlockType.Water &&
                            neighbor != (byte)BlockType.Leaves) continue;

                        int vertStart = verts.Count;
                        foreach (var v in faceVertices[d])
                            verts.Add(v + offset);

                        opaqueTris.Add(vertStart);     opaqueTris.Add(vertStart + 1);
                        opaqueTris.Add(vertStart + 2); opaqueTris.Add(vertStart);
                        opaqueTris.Add(vertStart + 2); opaqueTris.Add(vertStart + 3);
                        uvs.AddRange(faceUVs);
                    }
                }
            }

            var opaqueMesh = new Mesh();
            opaqueMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            opaqueMesh.vertices    = verts.ToArray();
            opaqueMesh.triangles   = opaqueTris.ToArray();
            opaqueMesh.uv          = uvs.ToArray();
            opaqueMesh.RecalculateNormals();

            mf.mesh     = opaqueMesh;
            mr.material = blockData.atlasMaterial;

            MeshCollider col = mf.gameObject.GetComponent<MeshCollider>();
            if (col == null) col = mf.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = opaqueMesh;

            BuildSubMesh(mf.transform, "Leaves",
                leavesVerts, leavesTris, leavesUVs,
                blockData.leavesMaterial,
                mf.gameObject.layer,
                isWater:     false,
                addCollider: true);

            BuildSubMesh(mf.transform, "Water",
                waterVerts, waterTris, waterUVs,
                blockData.waterMaterial,
                mf.gameObject.layer,
                isWater:     true,
                addCollider: false);
        }

        static void BuildSubMesh(
            Transform     parent,
            string        name,
            List<Vector3> v,
            List<int>     t,
            List<Vector2> u,
            Material      mat,
            int           layer,
            bool          isWater     = false,
            bool          addCollider = false)
        {
            GameObject obj   = null;
            Transform  found = parent.Find(name);
            if (found != null) obj = found.gameObject;

            if (t.Count == 0)
            {
                if (obj != null) Object.DestroyImmediate(obj);
                return;
            }

            if (obj == null)
            {
                obj = new GameObject(name);
                obj.transform.parent        = parent;
                obj.transform.localPosition = Vector3.zero;
                obj.layer                   = layer;
            }

            MeshFilter subMF = obj.GetComponent<MeshFilter>();
            if (subMF == null) subMF = obj.AddComponent<MeshFilter>();

            MeshRenderer subMR = obj.GetComponent<MeshRenderer>();
            if (subMR == null) subMR = obj.AddComponent<MeshRenderer>();

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices    = v.ToArray();
            mesh.triangles   = t.ToArray();
            mesh.uv          = u.ToArray();
            mesh.RecalculateNormals();

            subMF.mesh     = mesh;
            subMR.material = mat;

            if (addCollider)
            {
                MeshCollider c = obj.GetComponent<MeshCollider>();
                if (c == null) c = obj.AddComponent<MeshCollider>();
                c.sharedMesh = mesh;
            }

            if (isWater)
            {
                foreach (var c in obj.GetComponents<Collider>())
                    Object.DestroyImmediate(c);

                int waterLayer = LayerMask.NameToLayer("Water");
                if (waterLayer >= 0)
                    obj.layer = waterLayer;
            }
        }

        static byte GetBlock(
            int x, int y, int z,
            Vector2Int chunkPos,
            ChunkData chunk,
            Dictionary<Vector2Int, ChunkData> allChunks)
        {
            // 월드 y 범위 벗어나면 Air 반환
            if (y < WorldYMin || y > WorldYMax)
                return (byte)BlockType.Air;

            if (x >= 0 && x < ChunkData.Width &&
                z >= 0 && z < ChunkData.Width)
                return chunk.GetBlock(x, y, z);

            int neighborCX = chunkPos.x + FloorDiv(x, ChunkData.Width);
            int neighborCZ = chunkPos.y + FloorDiv(z, ChunkData.Width);
            int localX     = Mod(x, ChunkData.Width);
            int localZ     = Mod(z, ChunkData.Width);

            var neighborPos = new Vector2Int(neighborCX, neighborCZ);
            if (allChunks.TryGetValue(neighborPos, out var neighborChunk))
                return neighborChunk.GetBlock(localX, y, localZ);

            return (byte)BlockType.Air;
        }

        static int FloorDiv(int a, int b)
            => a >= 0 ? a / b : (a - b + 1) / b;

        static int Mod(int a, int b)
            => ((a % b) + b) % b;
    }
}