using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTestTorch : MonoBehaviour
{
    [SerializeField] private GameObject torch;
    
    private Animator _animator;

    private bool _isTorchActived;
    
    private static readonly int IsHoldingTorchHash =
        Animator.StringToHash("IsHoldingTorch");

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _animator.SetBool(IsHoldingTorchHash, _isTorchActived);
        torch.SetActive(_isTorchActived);
    }
    
    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            UpdateActive();
        }
    }

    private void UpdateActive()
    {
        torch.SetActive(_isTorchActived);
        _animator.SetBool(IsHoldingTorchHash, _isTorchActived);
        _isTorchActived = !_isTorchActived;
    }
}
