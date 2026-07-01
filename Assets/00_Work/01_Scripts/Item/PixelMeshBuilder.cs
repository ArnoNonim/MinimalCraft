using System.Collections.Generic;
using UnityEngine;

namespace _00_Work._01_Scripts.Item
{
    public static class PixelMeshBuilder
    {
        public static Mesh Build(Texture2D texture, float thickness = 0.1f)
        {
            int width  = texture.width;
            int height = texture.height;

            var verts  = new List<Vector3>();
            var tris   = new List<int>();
            var uvs    = new List<Vector2>();

            float pixelSize = 1f / Mathf.Max(width, height);
            float offsetX   = -width  * pixelSize * 0.5f;
            float offsetY   = -height * pixelSize * 0.5f;

            Color[] pixels = texture.GetPixels();

            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Color pixel = pixels[x + y * width];
                if (pixel.a < 0.1f) continue;

                float px = offsetX + x * pixelSize;
                float py = offsetY + y * pixelSize;
                float pz = thickness * 0.5f;

                float pixelU = (x + 0.5f) / width;
                float pixelV = (y + 0.5f) / height;
                float uSize  = 1f / width;
                float vSize  = 1f / height;

                // 앞면
                AddFace(verts, tris, uvs,
                    new Vector3(px,             py,             pz),
                    new Vector3(px + pixelSize, py,             pz),
                    new Vector3(px + pixelSize, py + pixelSize, pz),
                    new Vector3(px,             py + pixelSize, pz),
                    pixelU - uSize * 0.5f, pixelV - vSize * 0.5f,
                    uSize, vSize);

                // 뒷면
                AddFace(verts, tris, uvs,
                    new Vector3(px + pixelSize, py,             -pz),
                    new Vector3(px,             py,             -pz),
                    new Vector3(px,             py + pixelSize, -pz),
                    new Vector3(px + pixelSize, py + pixelSize, -pz),
                    pixelU - uSize * 0.5f, pixelV - vSize * 0.5f,
                    uSize, vSize);

                // 옆면 — 이웃 픽셀 없을 때만
                if (!HasPixel(pixels, x, y + 1, width, height))
                    AddFace(verts, tris, uvs,
                        new Vector3(px,             py + pixelSize, -pz),
                        new Vector3(px,             py + pixelSize,  pz),
                        new Vector3(px + pixelSize, py + pixelSize,  pz),
                        new Vector3(px + pixelSize, py + pixelSize, -pz),
                        pixelU, pixelV, 0f, 0f);

                if (!HasPixel(pixels, x, y - 1, width, height))
                    AddFace(verts, tris, uvs,
                        new Vector3(px + pixelSize, py, -pz),
                        new Vector3(px + pixelSize, py,  pz),
                        new Vector3(px,             py,  pz),
                        new Vector3(px,             py, -pz),
                        pixelU, pixelV, 0f, 0f);

                if (!HasPixel(pixels, x + 1, y, width, height))
                    AddFace(verts, tris, uvs,
                        new Vector3(px + pixelSize, py,              pz),
                        new Vector3(px + pixelSize, py,             -pz),
                        new Vector3(px + pixelSize, py + pixelSize, -pz),
                        new Vector3(px + pixelSize, py + pixelSize,  pz),
                        pixelU, pixelV, 0f, 0f);

                if (!HasPixel(pixels, x - 1, y, width, height))
                    AddFace(verts, tris, uvs,
                        new Vector3(px, py,             -pz),
                        new Vector3(px, py,              pz),
                        new Vector3(px, py + pixelSize,  pz),
                        new Vector3(px, py + pixelSize, -pz),
                        pixelU, pixelV, 0f, 0f);
            }

            var mesh = new Mesh();
            mesh.vertices  = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.uv        = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static bool HasPixel(Color[] pixels, int x, int y, int w, int h)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return false;
            return pixels[x + y * w].a >= 0.1f;
        }

        static void AddFace(
            List<Vector3> verts,
            List<int>     tris,
            List<Vector2> uvs,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
            float u, float v, float uSize, float vSize)
        {
            int start = verts.Count;
            verts.Add(v0); verts.Add(v1);
            verts.Add(v2); verts.Add(v3);

            tris.Add(start);     tris.Add(start + 2); tris.Add(start + 1);
            tris.Add(start);     tris.Add(start + 3); tris.Add(start + 2);

            uvs.Add(new Vector2(u,          v         ));
            uvs.Add(new Vector2(u + uSize,  v         ));
            uvs.Add(new Vector2(u + uSize,  v + vSize ));
            uvs.Add(new Vector2(u,          v + vSize ));
        }
    }
}