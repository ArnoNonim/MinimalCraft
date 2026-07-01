using System.Collections;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.Save;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.Item.SO
{
    [CreateAssetMenu(fileName = "ReturnStone", menuName = "Item/ReturnStone")]
    public class ReturnStoneItemSO : UsableItemSO
    {
        public override void OnUse(GameObject user)
        {
            if (WorldSaveManager.Instance == null) return;
            if (!WorldSaveManager.Instance.HasSpawnPoint)
            {
                Debug.LogWarning("[ReturnStone] 저장된 스폰 포인트 없음");
                return;
            }

            user.GetComponent<MonoBehaviour>()
                ?.StartCoroutine(TeleportRoutine(user));
        }

        private IEnumerator TeleportRoutine(GameObject user)
        {
            var spawnPos = WorldSaveManager.Instance.SpawnPoint;
            var chunk    = ChunkManager.Instance;

            // 로딩 스크린 표시
            LoadingScreen.Instance?.Show();
            Time.timeScale = 0f;

            // 플레이어를 스폰 위치로 이동 → 청크 갱신 트리거
            user.transform.position = spawnPos;
            chunk.ForceUpdateChunks();

            // OnChunksLoaded 대기
            bool loaded = false;
            void OnLoaded() => loaded = true;
            chunk.OnChunksLoaded += OnLoaded;

            while (!loaded)
                yield return null;

            chunk.OnChunksLoaded -= OnLoaded;

            Time.timeScale = 1f;
            LoadingScreen.Instance?.Hide();
        }
    }
}