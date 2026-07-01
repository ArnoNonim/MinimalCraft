using _00_Work._01_Scripts.Player.SO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Work._01_Scripts.Player.PlayerCamera
{
    public class FirstThirdTest : MonoBehaviour
    {
        [field:SerializeField] public PlayerInputSO PlayerInput { get; set; }
        
        
        private CameraLook _camLook;

        private void OnEnable()
        {
            PlayerInput.OnPOVChange += ChangePOV;
        }
        
        private void OnDisable()
        {
            PlayerInput.OnPOVChange -= ChangePOV;
        }
        
        private void Start()
        {
            _camLook = GetComponent<CameraLook>();
        }

        private void ChangePOV()
        {
            if (_camLook.isThirdPerson)
            {
                _camLook.SwitchToFirstPerson();
                _camLook.firstPersonCameraTransform.gameObject.SetActive(true);
                _camLook.thirdPersonCameraTransform.gameObject.SetActive(false);
            }
            else
            {
                _camLook.SwitchToThirdPerson();
                _camLook.firstPersonCameraTransform.gameObject.SetActive(false);
                _camLook.thirdPersonCameraTransform.gameObject.SetActive(true);
            }
        }
    }
}
