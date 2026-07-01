using _00_Work._01_Scripts.ChunkSystem.Chunk;
using _00_Work._01_Scripts.Save;
using UnityEngine;

namespace _00_Work._01_Scripts.UI.UIs
{
    public class UIEscMenu : UIPopup
    {
        [SerializeField] private UISettings settings;
        
        protected override void OnOpen()
        {
            Time.timeScale = 0;
        }

        protected override void OnClose()
        {
            Time.timeScale = 1f;
        }

        public void OnSettings()
        {
            settings.Open();
        }
        
        public void QuitGame()
        {
            ChunkManager.Instance.SaveChunks();
            SaveManager.Instance.Save();
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
            Application.Quit();
#endif
        }
    }
}