using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.Save;
using _00_Work._01_Scripts.UI.UIs;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.MainMenu
{
    public class UIMainMenu : MonoBehaviour
    {
        [SerializeField] private UISettings settings;
        
        public void OnSettings()
        {
            settings.Open();
        }
        
        public void QuitGame()
        {
            if (!UIManager.Instance.isMainMenu)
            {
                ChunkManager.Instance.SaveChunks();
                SaveManager.Instance.Save();
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
            Application.Quit();
#endif
        }
    }
}