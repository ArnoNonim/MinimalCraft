using System.IO;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Chunk
{
    public static class ChunkSerializer
    {
        public static string SaveDirectory { get; set; }

        static string GetPath(Vector2Int chunkPos)
        {
            if (string.IsNullOrEmpty(SaveDirectory))
                throw new System.Exception(
                    "ChunkSerializer.SaveDirectory 미설정 — " +
                    "ChunkManager.Awake에서 설정해줘.");

            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);

            return Path.Combine(
                SaveDirectory, $"{chunkPos.x}_{chunkPos.y}.chunk");
        }

        public static void Save(Vector2Int pos, ChunkData data)
        {
            try   { File.WriteAllBytes(GetPath(pos), data.blocks); }
            catch (System.Exception e)
            { Debug.LogError($"청크 저장 실패 {pos}: {e.Message}"); }
        }

        public static bool TryLoad(Vector2Int pos, out ChunkData data)
        {
            data = null;
            string path = GetPath(pos);
            if (!File.Exists(path)) return false;

            try
            {
                byte[] bytes = File.ReadAllBytes(path);

                int expected = ChunkData.Width *
                               ChunkData.Width *
                               ChunkData.Height;

                if (bytes.Length != expected)
                {
                    return false;
                }

                data        = new ChunkData();
                data.blocks = bytes;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"청크 로드 실패 {pos}: {e.Message}");
                return false;
            }
        }

        public static bool Exists(Vector2Int pos)
        {
            try   { return File.Exists(GetPath(pos)); }
            catch { return false; }
        }

        public static void Delete(Vector2Int pos)
        {
            string path = GetPath(pos);
            if (File.Exists(path)) File.Delete(path);
        }

        public static void DeleteAll()
        {
            if (Directory.Exists(SaveDirectory))
                Directory.Delete(SaveDirectory, true);
            Debug.Log("전체 청크 삭제 완료");
        }

        public static int GetSavedCount()
        {
            if (!Directory.Exists(SaveDirectory)) return 0;
            return Directory.GetFiles(SaveDirectory, "*.chunk").Length;
        }
    }
}