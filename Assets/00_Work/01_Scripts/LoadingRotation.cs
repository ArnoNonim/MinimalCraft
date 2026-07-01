using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace _00_Work._01_Scripts
{
    public class LoadingRotation : MonoBehaviour
    {
        [SerializeField] private float duration = 2f;
        [SerializeField] private Vector3 rotationAxis = new Vector3(0, 360f, 0);
    
        private void Awake()
        {
            LMotion.Create(Vector3.zero, rotationAxis, duration)
                .WithEase(Ease.Linear)
                .WithLoops(-1, LoopType.Restart)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalEulerAngles(transform);
        }
    }
}
