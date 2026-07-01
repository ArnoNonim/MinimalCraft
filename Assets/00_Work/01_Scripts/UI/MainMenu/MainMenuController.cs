using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _00_Work._01_Scripts.UI.MainMenu
{
    /// <summary>
    /// 메인메뉴 씬 전체 흐름 관리.
    /// 게임 시작 → 저장 파일 유무 분기 → 없으면 UIWorldCreation 열기.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("씬")]
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("UI")]
        [SerializeField] private Button           startButton;
        [SerializeField] private UIWorldCreation  uiWorldCreation;

        private void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            uiWorldCreation.OnConfirm += OnWorldCreationConfirmed;
        }

        private void OnDestroy()
        {
            uiWorldCreation.OnConfirm -= OnWorldCreationConfirmed;
        }

        // ──────────────────────────────────────────────────────────────
        private void OnStartClicked()
        {
            if (HasSaveFile())
            {
                LoadGameScene();
            }
            else
            {
                uiWorldCreation.Open();
            }
        }

        private void OnWorldCreationConfirmed(int seed, float noiseScale)
        {
            // WorldCreationData.Set()은 UIWorldCreation 내부에서 이미 호출됨
            LoadGameScene();
        }

        private void LoadGameScene()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        // ── 저장 파일 체크 ─────────────────────────────────────────────
        private static bool HasSaveFile()
        {
            string path = System.IO.Path.Combine(
                Application.persistentDataPath, "WorldSave.json");
            return System.IO.File.Exists(path);
        }
    }
}