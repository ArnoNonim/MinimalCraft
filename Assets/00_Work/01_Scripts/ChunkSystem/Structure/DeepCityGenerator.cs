using System.Collections.Generic;
using _00_Work._01_Scripts.ChunkSystem.Block;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using UnityEngine;

namespace _00_Work._01_Scripts.ChunkSystem.Structure
{
    public static class DeepCityGenerator
    {
        private const float CityThreshold  = 0.40f;
        private const float CityNoiseScale = 0.0008f;

        public const int CitySize  = 256;
        public const int HalfCity  = CitySize / 2;

        private const int CityFloorY   = -38;
        private const int CityCeilingY = -20;
        private const int CityHeight   = CityCeilingY - CityFloorY;

        private const int MainRoadWidth = 4;
        private const int SubRoadWidth  = 2;

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        public static void Generate(
            ChunkData chunk,
            Vector2Int chunkPos,
            int seed,
            Dictionary<Vector2Int, ChunkData> allChunks)
        {
            var originOpt = GetCityOrigin(chunkPos, seed);
            if (!originOpt.HasValue) return;

            Vector2Int cityOrigin = originOpt.Value;
            int offsetX = chunkPos.x * ChunkData.Width - cityOrigin.x * ChunkData.Width;
            int offsetZ = chunkPos.y * ChunkData.Width - cityOrigin.y * ChunkData.Width;

            if (offsetX < 0 || offsetX >= CitySize || offsetZ < 0 || offsetZ >= CitySize) return;

            GenerateCityChunk(chunk, chunkPos, offsetX, offsetZ, seed);
        }

        // ──────────────────────────────────────────────
        // 도시 원점 탐색
        // ──────────────────────────────────────────────

        static Vector2Int? GetCityOrigin(Vector2Int chunkPos, int seed)
        {
            int gridSize = CitySize / ChunkData.Width;
            int cellX    = Mathf.FloorToInt((float)chunkPos.x / gridSize);
            int cellZ    = Mathf.FloorToInt((float)chunkPos.y / gridSize);

            for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                int cx = cellX + dx;
                int cz = cellZ + dz;

                float noise = Mathf.PerlinNoise(
                    cx * 1.3f + seed * 0.00371f + 17.3f,
                    cz * 1.3f + seed * 0.00371f + 31.7f);

                if (noise < CityThreshold) continue;

                var origin = new Vector2Int(cx * gridSize, cz * gridSize);
                
                if (chunkPos.x >= origin.x && chunkPos.x < origin.x + gridSize &&
                    chunkPos.y >= origin.y && chunkPos.y < origin.y + gridSize)
                    return origin;
            }
            return null;
        }

        // ──────────────────────────────────────────────
        // 청크별 도시 생성
        // ──────────────────────────────────────────────

        static void GenerateCityChunk(
            ChunkData chunk, Vector2Int chunkPos,
            int cityX, int cityZ, int seed)
        {
            for (int lx = 0; lx < ChunkData.Width; lx++)
            for (int lz = 0; lz < ChunkData.Width; lz++)
            {
                int wx = cityX + lx;
                int wz = cityZ + lz;

                bool inCity = wx >= 2 && wx < CitySize - 2 &&
                              wz >= 2 && wz < CitySize - 2;
                if (!inCity) continue;

                // 공동 굴착
                for (int y = CityFloorY + 1; y <= CityCeilingY; y++)
                    chunk.SetBlock(lx, y, lz, (byte)BlockType.Air);

                // 바닥
                chunk.SetBlock(lx, CityFloorY, lz, (byte)BlockType.Deepslate);

                // 불규칙 천장
                float ceilNoise = Mathf.PerlinNoise(
                    wx * 0.04f + seed * 0.0031f + 200f,
                    wz * 0.04f + seed * 0.0031f + 200f);
                int ceilY = CityCeilingY + Mathf.FloorToInt(ceilNoise * 5f) - 2;
                for (int y = CityCeilingY; y <= ceilY + 2; y++)
                    chunk.SetBlock(lx, y, lz, (byte)BlockType.Deepslate);

                // 구역 판정 후 구조물 배치
                var zone = GetZoneType(wx, wz, seed);
                PlaceZone(chunk, lx, lz, wx, wz, zone, seed);
            }
        }

        // ──────────────────────────────────────────────
        // 구역 판정
        // ──────────────────────────────────────────────

        static CityZoneType GetZoneType(int wx, int wz, int seed)
        {
            int   rx   = wx - HalfCity;
            int   rz   = wz - HalfCity;
            float dist = Mathf.Sqrt(rx * rx + rz * rz);

            if (dist < 8f)  return CityZoneType.Temple;
            if (dist < 22f) return CityZoneType.Plaza;

            // 방사형 대로 (8방향)
            float angle = Mathf.Atan2(rz, rx) * Mathf.Rad2Deg;
            float mod45  = ((angle % 45f) + 45f) % 45f;
            if (mod45 < 2.5f || mod45 > 42.5f) return CityZoneType.MainRoad;

            // 격자 대로 32블록
            if (wx % 32 < MainRoadWidth || wz % 32 < MainRoadWidth) return CityZoneType.MainRoad;

            // 소로 16블록
            if (wx % 16 < SubRoadWidth || wz % 16 < SubRoadWidth) return CityZoneType.SubRoad;

            // 탑 — 64블록 교차점
            int modX64 = wx % 64;
            int modZ64 = wz % 64;
            if (modX64 >= 29 && modX64 <= 35 && modZ64 >= 29 && modZ64 <= 35)
                return CityZoneType.Tower;

            return CityZoneType.Building;
        }

        // ──────────────────────────────────────────────
        // 구역별 배치
        // ──────────────────────────────────────────────

        static void PlaceZone(ChunkData chunk, int lx, int lz,
            int wx, int wz, CityZoneType zone, int seed)
        {
            switch (zone)
            {
                case CityZoneType.MainRoad: BuildMainRoad(chunk, lx, lz, wx, wz); break;
                case CityZoneType.SubRoad:  BuildSubRoad(chunk, lx, lz);          break;
                case CityZoneType.Plaza:    BuildPlaza(chunk, lx, lz, wx, wz);    break;
                case CityZoneType.Temple:   BuildTemple(chunk, lx, lz, wx, wz);   break;
                case CityZoneType.Tower:    BuildTower(chunk, lx, lz, wx, wz);    break;
                case CityZoneType.Building: BuildBuilding(chunk, lx, lz, wx, wz, seed); break;
            }
        }

        // ──────────────────────────────────────────────
        // 도로
        // ──────────────────────────────────────────────

        static void BuildMainRoad(ChunkData chunk, int lx, int lz, int wx, int wz)
        {
            chunk.SetBlock(lx, CityFloorY, lz, (byte)BlockType.Stone);

            // 8블록마다 가로등 기둥
            if ((wx % 8 == 0 && wz % 32 == 2) || (wz % 8 == 0 && wx % 32 == 2))
            {
                for (int h = 1; h <= 6; h++)
                    chunk.SetBlock(lx, CityFloorY + h, lz, (byte)BlockType.Stone);
                // 기둥 꼭대기 — 횃불 자리 (나중에 횃불 블록으로 교체)
                chunk.SetBlock(lx, CityFloorY + 7, lz, (byte)BlockType.Stone);
            }
        }

        static void BuildSubRoad(ChunkData chunk, int lx, int lz)
        {
            chunk.SetBlock(lx, CityFloorY, lz, (byte)BlockType.MiddleSite);
        }

        // ──────────────────────────────────────────────
        // 광장
        // ──────────────────────────────────────────────

        static void BuildPlaza(ChunkData chunk, int lx, int lz, int wx, int wz)
        {
            // 체크무늬 + 테두리 장식
            int rx = wx - HalfCity;
            int rz = wz - HalfCity;
            float dist = Mathf.Sqrt(rx * rx + rz * rz);

            // 외곽 링 — 돌 테두리
            if (dist > 19f && dist < 22f)
            {
                chunk.SetBlock(lx, CityFloorY,     lz, (byte)BlockType.Stone);
                chunk.SetBlock(lx, CityFloorY + 1, lz, (byte)BlockType.Stone);
                return;
            }

            // 안쪽 — 체크무늬
            bool check = (((wx - HalfCity) + (wz - HalfCity)) % 2 == 0);
            chunk.SetBlock(lx, CityFloorY, lz,
                check ? (byte)BlockType.Stone : (byte)BlockType.MiddleSite);
        }

        // ──────────────────────────────────────────────
        // 신전
        // ──────────────────────────────────────────────

        static void BuildTemple(ChunkData chunk, int lx, int lz, int wx, int wz)
        {
            int rx   = wx - HalfCity;
            int rz   = wz - HalfCity;
            float dist = Mathf.Sqrt(rx * rx + rz * rz);

            // 계단식 피라미드
            int steps = Mathf.Max(0, 8 - Mathf.FloorToInt(dist));

            for (int s = 0; s <= steps; s++)
            {
                byte mat = s == steps ? (byte)BlockType.Stone : (byte)BlockType.Deepslate;
                chunk.SetBlock(lx, CityFloorY + s, lz, mat);
            }

            // 중앙 제단 기둥
            if (Mathf.Abs(rx) <= 1 && Mathf.Abs(rz) <= 1)
            {
                for (int h = steps + 1; h <= steps + 5; h++)
                    chunk.SetBlock(lx, CityFloorY + h, lz, (byte)BlockType.Stone);
            }

            // 신전 4기둥
            if ((Mathf.Abs(rx) == 5 && Mathf.Abs(rz) == 5))
            {
                for (int h = 0; h <= 6; h++)
                    chunk.SetBlock(lx, CityFloorY + h, lz, (byte)BlockType.Deepslate);
            }
        }

        // ──────────────────────────────────────────────
        // 탑
        // ──────────────────────────────────────────────

        static void BuildTower(ChunkData chunk, int lx, int lz, int wx, int wz)
        {
            int tx = (wx % 64) - 32;
            int tz = (wz % 64) - 32;

            float dist = Mathf.Sqrt(tx * tx + tz * tz);

            if (dist > 4f) return;

            bool isOuter = dist > 2.8f;
            bool isInner = dist <= 1.5f;

            for (int h = 0; h <= CityHeight - 1; h++)
            {
                int y = CityFloorY + h;

                if (isOuter)
                {
                    // 외벽 — 전체
                    chunk.SetBlock(lx, y, lz, (byte)BlockType.Deepslate);
                }
                else if (isInner)
                {
                    // 내부 중심 기둥
                    if (h == 0 || h == CityHeight - 1)
                        chunk.SetBlock(lx, y, lz, (byte)BlockType.Stone);
                    // 나머지 내부는 비움
                }
                else
                {
                    // 외벽 안쪽 — 창문 뚫기 (4블록마다)
                    bool isWindow = h % 4 == 2;
                    if (!isWindow)
                        chunk.SetBlock(lx, y, lz, (byte)BlockType.Deepslate);
                }
            }

            // 탑 꼭대기 — 흉벽 패턴
            if (dist <= 4f)
            {
                bool merlon = (Mathf.RoundToInt(Mathf.Atan2(tz, tx) * 4f / Mathf.PI) % 2 == 0);
                if (merlon)
                    chunk.SetBlock(lx, CityCeilingY, lz, (byte)BlockType.Deepslate);
            }
        }

        // ──────────────────────────────────────────────
        // 건물 — 개선된 자연스러운 버전
        // ──────────────────────────────────────────────

        static void BuildBuilding(ChunkData chunk, int lx, int lz,
            int wx, int wz, int seed)
        {
            // 16블록 격자 내 블록 단위 지역 시드
            int blockSeed = ((wx / 16) * 7919) ^ ((wz / 16) * 1299827) ^ seed;
            var rng       = new System.Random(blockSeed);

            // ── 건물 크기 및 위치 ─────────────────────────────────────────
            int bWidth  = rng.Next(5, 12);
            int bDepth  = rng.Next(5, 12);
            int bHeight = rng.Next(4, CityHeight - 3);

            int cellX = wx % 16;
            int cellZ = wz % 16;

            int marginX = rng.Next(1, Mathf.Max(2, 16 - bWidth - 1));
            int marginZ = rng.Next(1, Mathf.Max(2, 16 - bDepth - 1));

            bool inBX = cellX >= marginX && cellX < marginX + bWidth;
            bool inBZ = cellZ >= marginZ && cellZ < marginZ + bDepth;
            if (!inBX || !inBZ) return;

            int localX = cellX - marginX;
            int localZ = cellZ - marginZ;

            bool isEdgeX = localX == 0 || localX == bWidth - 1;
            bool isEdgeZ = localZ == 0 || localZ == bDepth - 1;
            bool isWall  = isEdgeX || isEdgeZ;
            bool isCorner = isEdgeX && isEdgeZ;

            // ── 건물 재료 — 3가지 조합 중 랜덤 ─────────────────────────
            // 0: 나무 기둥 + 판자 벽  1: 판자 기둥 + 심층석 벽  2: 심층석 기둥 + 나무 벽
            int matChoice  = blockSeed % 3;
            byte wallMat   = matChoice == 0 ? (byte)BlockType.WoodPlanks
                           : matChoice == 1 ? (byte)BlockType.Deepslate
                                            : (byte)BlockType.WoodPlanks;
            byte accentMat = matChoice == 0 ? (byte)BlockType.Log
                           : matChoice == 1 ? (byte)BlockType.WoodPlanks
                                            : (byte)BlockType.Deepslate;

            // ── 각 높이별 배치 ────────────────────────────────────────────
            for (int h = 0; h <= bHeight; h++)
            {
                int y = CityFloorY + h;

                // 지붕
                if (h == bHeight)
                {
                    // 지붕 처마 — 외벽보다 1블록 넓게
                    chunk.SetBlock(lx, y, lz, wallMat);
                    // 박공 지붕: 중앙으로 올수록 높아짐
                    int midX = bWidth  / 2;
                    int midZ = bDepth  / 2;
                    int rise = Mathf.Min(localX, bWidth  - 1 - localX);
                    int riseZ = Mathf.Min(localZ, bDepth - 1 - localZ);
                    int roofPeak = Mathf.Min(rise, riseZ);
                    for (int rh = 1; rh <= roofPeak; rh++)
                        chunk.SetBlock(lx, y + rh, lz, (byte)BlockType.Log);
                    continue;
                }

                if (!isWall) continue; // 내부 비움

                // 모서리 기둥 — 더 두껍게
                if (isCorner)
                {
                    chunk.SetBlock(lx, y, lz, accentMat);
                    continue;
                }

                // 창문 뚫기
                bool isWindow = false;
                if (h >= 2 && h <= bHeight - 2)
                {
                    // X방향 벽 창문
                    if (isEdgeZ && !isEdgeX)
                        isWindow = localX % 3 == 1 && h % 3 == 2;
                    // Z방향 벽 창문
                    if (isEdgeX && !isEdgeZ)
                        isWindow = localZ % 3 == 1 && h % 3 == 2;
                }

                if (!isWindow)
                    chunk.SetBlock(lx, y, lz, wallMat);

                // 창문 아래 받침대
                if (isWindow && h >= 3)
                    chunk.SetBlock(lx, y - 1, lz, accentMat);

                // 1층 바닥 테두리 장식
                if (h == 1)
                    chunk.SetBlock(lx, y, lz, accentMat);

                // 처마 라인 (지붕 바로 아래)
                if (h == bHeight - 1)
                    chunk.SetBlock(lx, y, lz, accentMat);
            }

            // ── 입구 — 남쪽 벽 중앙에 문 뚫기 ──────────────────────────
            if (localZ == 0 && localX == bWidth / 2)
            {
                chunk.SetBlock(lx, CityFloorY + 1, lz, (byte)BlockType.Air);
                chunk.SetBlock(lx, CityFloorY + 2, lz, (byte)BlockType.Air);
            }
            if (localZ == 0 && localX == bWidth / 2 - 1)
            {
                chunk.SetBlock(lx, CityFloorY + 1, lz, (byte)BlockType.Air);
                chunk.SetBlock(lx, CityFloorY + 2, lz, (byte)BlockType.Air);
            }
        }

        // ──────────────────────────────────────────────

        private enum CityZoneType
        {
            Building, MainRoad, SubRoad, Plaza, Temple, Tower
        }
    }
}