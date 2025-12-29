using UnityEngine;
using UnityEngine.InputSystem;
using Architecture;
using GamePlay.CharacterController.Events;
using VContainer;

namespace GamePlay.CharacterController
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MainCharacterController : MonoBehaviour
    {
        [Inject] private EventBus _eventBus;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float _moveSpeed = 5f;
        [SerializeField, Min(1f)] private float _sprintMultiplier = 1.5f;
        [SerializeField, Min(0.01f)] private float _acceleration = 20f;
        [SerializeField, Min(0.01f)] private float _deceleration = 25f;

        private InputSystem_Actions _inputActions;
        private InputSystem_Actions.PlayerActions _playerActions;
        private Rigidbody2D _rigidbody;

        private Vector2 _moveInput;
        private bool _isSprinting;

        private Vector2 _lastPublishedMoveInput;
        private bool _lastPublishedSprint;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _playerActions = _inputActions.Player;
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _playerActions.Move.performed += OnMovePerformed;
            _playerActions.Move.canceled += OnMoveCanceled;
            _playerActions.Sprint.performed += OnSprintPerformed;
            _playerActions.Sprint.canceled += OnSprintCanceled;
            _playerActions.Interact.performed += OnInteractPerformed;
            _playerActions.Backpack.performed += OnBackpackPerformed;
            _playerActions.Enable();
        }

        private void OnDisable()
        {
            _playerActions.Move.performed -= OnMovePerformed;
            _playerActions.Move.canceled -= OnMoveCanceled;
            _playerActions.Sprint.performed -= OnSprintPerformed;
            _playerActions.Sprint.canceled -= OnSprintCanceled;
            _playerActions.Interact.performed -= OnInteractPerformed;
            _playerActions.Backpack.performed -= OnBackpackPerformed;
            _playerActions.Disable();
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
        }

        private void FixedUpdate()
        {
            Vector2 clampedInput = Vector2.ClampMagnitude(_moveInput, 1f);
            float speed = clampedInput.sqrMagnitude > 0.0001f ? _moveSpeed : 0f;
            if (_isSprinting)
            {
                speed *= _sprintMultiplier;
            }

            Vector2 targetVelocity = clampedInput.normalized * speed;
            float accel = targetVelocity.sqrMagnitude > 0.0001f ? _acceleration : _deceleration;
            _rigidbody.linearVelocity = Vector2.MoveTowards(_rigidbody.linearVelocity, targetVelocity, accel * Time.fixedDeltaTime);
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
            TryPublishMoveEvent();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
            TryPublishMoveEvent();
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            _isSprinting = true;
            PublishSprintState();
            TryPublishMoveEvent();
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            _isSprinting = false;
            PublishSprintState();
            TryPublishMoveEvent();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            _eventBus?.Publish(new InteractEvent());
        }

        private void OnBackpackPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            _eventBus?.Publish(new BackpackToggleEvent());
        }

        private void TryPublishMoveEvent()
        {
            Vector2 clampedInput = Vector2.ClampMagnitude(_moveInput, 1f);
            bool inputChanged = (clampedInput - _lastPublishedMoveInput).sqrMagnitude > 0.0001f;
            bool sprintChanged = _isSprinting != _lastPublishedSprint;

            if (!inputChanged && !sprintChanged)
            {
                return;
            }

            _lastPublishedMoveInput = clampedInput;
            _lastPublishedSprint = _isSprinting;
            _eventBus?.Publish(new MoveInputEvent(clampedInput, _isSprinting));
        }

        private void PublishSprintState()
        {
            _eventBus?.Publish(new SprintStateChangedEvent(_isSprinting));
        }
    }
}