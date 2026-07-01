using UnityEngine;

namespace _00_Work._01_Scripts
{
    public class Managers : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
