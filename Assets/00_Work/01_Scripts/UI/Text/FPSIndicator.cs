using TMPro;
using UnityEngine;

namespace _00_Work._01_Scripts.UI
{
    public class FPSIndicator : MonoBehaviour
    {
        public TextMeshProUGUI fpsText;
        public float pollingTime = 0.5f;
        private float _timer;
        private int _frameCount;

        void Update()
        {
            _timer += Time.deltaTime;
            _frameCount++;

            if (_timer >= pollingTime)
            {
                int frameRate = Mathf.RoundToInt(_frameCount / _timer);
                if (fpsText != null)
                {
                    fpsText.text = $"{frameRate} FPS";
                }

                _timer = 0f;
                _frameCount = 0;
            }
        }
    }
}