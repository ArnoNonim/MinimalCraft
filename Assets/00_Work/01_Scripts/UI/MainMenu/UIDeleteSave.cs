using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.MainMenu
{
    /// <summary>
    /// 세이브 파일이 하나라도 존재하면 버튼 표시.
    /// 클릭 시 UIAreYouSure 팝업 열고, 확인 시 전체 삭제.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIDeleteSave : MonoBehaviour
    {
        [SerializeField] private Button       deleteButton;
        [SerializeField] private UIAreYouSure areYouSure;

        private CanvasGroup _group;

        private static string PlayerSavePath   =>
            Path.Combine(Application.persistentDataPath, "PlayerSave.json");
        private static string WorldSavePath    =>
            Path.Combine(Application.persistentDataPath, "WorldSave.json");
        private static string ArtifactSavePath =>
            Path.Combine(Application.persistentDataPath, "ArtifactSave.json");
        private static string ChunkSaveDir     =>
            Path.Combine(Application.persistentDataPath, "Chunks");

        // ──────────────────────────────────────────────

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        void Start()
        {
            deleteButton?.onClick.AddListener(OnDeleteClicked);
            Refresh();
        }

        void OnDeleteClicked()
        {
            if (areYouSure == null) return;
            areYouSure.OpenWithCallback(OnConfirmed);
        }

        void OnConfirmed()
        {
            DeleteAll();
            Refresh();
        }

        void Refresh()
        {
            bool hasSave = HasAnySave();
            _group.alpha          = hasSave ? 1f : 0f;
            _group.interactable   = hasSave;
            _group.blocksRaycasts = hasSave;
        }

        // ──────────────────────────────────────────────

        static bool HasAnySave()
        {
            if (File.Exists(PlayerSavePath))   return true;
            if (File.Exists(WorldSavePath))    return true;
            if (File.Exists(ArtifactSavePath)) return true;
            if (Directory.Exists(ChunkSaveDir) &&
                Directory.GetFiles(ChunkSaveDir).Length > 0) return true;
            return false;
        }

        static void DeleteAll()
        {
            if (File.Exists(PlayerSavePath))   File.Delete(PlayerSavePath);
            if (File.Exists(WorldSavePath))    File.Delete(WorldSavePath);
            if (File.Exists(ArtifactSavePath)) File.Delete(ArtifactSavePath);

            if (Directory.Exists(ChunkSaveDir))
                Directory.Delete(ChunkSaveDir, recursive: true);

            Debug.Log("[UIDeleteSave] 모든 세이브 삭제 완료");
        }
    }
}