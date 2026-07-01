using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace _00_Work._01_Scripts.Player.SO
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/PlayerInput")]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        private bool _isInputBlocked;
        public bool IsInputBlocked
        {
            get => _isInputBlocked;
            set
            {
                if (_isInputBlocked == value) return;
                _isInputBlocked = value;

                if (_isInputBlocked)
                {
                    // 블락 시 눌린 상태 강제 해제
                    OnSprintKeyUp?.Invoke();
                    OnAttackKeyUp?.Invoke();
                    OnCrouchKeyUp?.Invoke();
                }
            }
        }
        
        public event UnityAction<Vector2> OnMovement;
        public event UnityAction<Vector2> OnLookAction;
        public event UnityAction OnJumpKeyPressed;
        public event UnityAction OnSprintKeyDown;
        public event UnityAction OnSprintKeyUp;
        public event UnityAction OnAttackKeyDown;
        public event UnityAction OnAttackKeyUp;
        public event UnityAction OnInteractKeyDown;
        public event UnityAction OnTryOpenInventory;
        public event UnityAction OnTryOpenESCMenu;
        public event UnityAction OnTryOpenRecipeBook;
        public event UnityAction OnDrop;
        public event UnityAction OnPOVChange;
        public event UnityAction OnCrouchKeyDown;
        public event UnityAction OnCrouchKeyUp;

        public event UnityAction<int> OnHotbarSelect;

        private Controls _inputActions;

        public InputActionAsset InputAsset => _inputActions?.asset;
        
        void OnEnable()
        {
            if (_inputActions == null)
            {
                _inputActions = new Controls();
                _inputActions.Player.SetCallbacks(this);
            }
            _inputActions.Player.Enable();
        }

        void OnDisable()
        {
            _inputActions.Player.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            OnMovement?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            OnLookAction?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnAttackKeyDown?.Invoke();
            if(context.canceled) OnAttackKeyUp?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if (context.started)
            {
                OnInteractKeyDown?.Invoke();
            }
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if (context.started)  OnCrouchKeyDown?.Invoke();
            if (context.canceled) OnCrouchKeyUp?.Invoke();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if (context.performed) OnJumpKeyPressed?.Invoke();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if (context.performed)  OnSprintKeyDown?.Invoke();
            if (context.canceled)   OnSprintKeyUp?.Invoke();
        }

        public void OnOpenInventory(InputAction.CallbackContext context)
        {
            if(context.performed) OnTryOpenInventory?.Invoke();
        }

        public void OnOpenESCMenu(InputAction.CallbackContext context)
        {
            if(context.performed) OnTryOpenESCMenu?.Invoke();
        }

        public void OnOpenRecipeBook(InputAction.CallbackContext context)
        {
            if(context.performed) OnTryOpenRecipeBook?.Invoke();
        }

        public void OnDropItem(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnDrop?.Invoke();
        }

        public void OnChangePOV(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if (context.performed) OnPOVChange?.Invoke();
        }

        public void OnHotbar1(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnHotbarSelect?.Invoke(0);
        }

        public void OnHotbar2(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnHotbarSelect?.Invoke(1);
        }

        public void OnHotbar3(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnHotbarSelect?.Invoke(2);
        }

        public void OnHotbar4(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnHotbarSelect?.Invoke(3);
        }

        public void OnHotbar5(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnHotbarSelect?.Invoke(4);
        }

        public void OnHotbar6(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnHotbarSelect?.Invoke(5);
        }

        public void OnHotbar7(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnHotbarSelect?.Invoke(6);
        }

        public void OnHotbar8(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnHotbarSelect?.Invoke(7);
        }

        public void OnHotbar9(InputAction.CallbackContext context)
        {
            if (IsInputBlocked) return;
            if(context.performed) OnHotbarSelect?.Invoke(8);
        }
    }
}