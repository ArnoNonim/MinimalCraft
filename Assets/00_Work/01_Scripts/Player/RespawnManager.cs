using System.Collections;
using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.Save;
using _00_Work._01_Scripts.Sound;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.Player
{
    /// <summary>
    /// 리스폰 로직 전담
    /// UIDeath에서 Show() 호출 → 페이드 완료 후 RespawnManager.Respawn() 호출
    /// </summary>
    public class RespawnManager : MonoBehaviour
    {
        public static RespawnManager Instance { get; private set; }

        [SerializeField] private PlayerRagdoll ragdoll;
        [SerializeField] private PlayerStats   playerStats;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Respawn()
        {
            StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            // 1. 플레이어 임시 위치 이동
            if (ragdoll != null)
                ragdoll.transform.position = new Vector3(0, 1000, 0);

            // 2. 타임스케일 0 + 로딩화면
            Time.timeScale = 0f;
            LoadingScreen.Instance?.Show();
            LoadingScreen.Instance?.SetProgress(0f);

            // 3. 래그돌/스탯 리셋
            ragdoll?.ResetRagdoll();
            yield return null;
            playerStats?.Respawn();

            // 4. 스폰 위치 결정
            Vector3 pos = WorldSaveManager.Instance != null && WorldSaveManager.Instance.HasSpawnPoint
                ? WorldSaveManager.Instance.SpawnPoint
                : ChunkManager.Instance?.FindSafeSpawnPosition() ?? Vector3.zero;

            // 5. 스폰 위치로 이동 후 청크 강제 로딩
            if (ragdoll != null)
                ragdoll.transform.position = pos;

            ChunkManager.Instance?.ForceUpdateChunks(); // ← 여기

            // 6. 청크 로딩 완료 대기
            if (ChunkManager.Instance != null)
            {
                bool loaded = false;
                System.Action onLoaded = () => loaded = true;
                ChunkManager.Instance.OnChunksLoaded += onLoaded;

                while (!loaded)
                {
                    LoadingScreen.Instance?.SetProgress(0.5f);
                    yield return null;
                }

                ChunkManager.Instance.OnChunksLoaded -= onLoaded;
            }

            // 7. 완료
            Time.timeScale = 1f;
            LoadingScreen.Instance?.Hide();
            SoundManager.Instance.PlaySFXAt("Teleport_End", ragdoll.transform, 0.3f);
        }
    }
}