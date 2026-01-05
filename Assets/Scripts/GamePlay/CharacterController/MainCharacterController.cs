using UnityEngine;
using UnityEngine.InputSystem;
using Architecture;
using GamePlay.CharacterController;
using GamePlay.Events;
using VContainer;
using R3;
using Sirenix.OdinInspector;

namespace GamePlay.CharacterController
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MainCharacterController : MonoBehaviour
    {
        [Inject] private EventBus _eventBus;

        [Title("Movement")] [SerializeField, Min(0.01f)]
        private float _defaultMoveSpeed = 5f;

        [Title("Sprint System")] [SerializeField, Min(1f)]
        private float _sprintMultiplier = 1.5f;

        [SerializeField, Min(0.1f)] private float _sprintTransitionDuration = 1f;

        [SerializeField] private AnimationCurve _sprintCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Title("Stamina System")]
        [ShowInInspector, ReadOnly, ProgressBar(0, nameof(_maxStamina), ColorGetter = nameof(GetStaminaBarColor))]
        private float Stamine => _stamina.CurrentValue;

        public SerializableReactiveProperty<float> _stamina = new SerializableReactiveProperty<float>(100f);


        [SerializeField, Min(1f)] private float _maxStamina = 100f;

        [SerializeField, Min(0.1f)] private float _staminaDrainRate = 20f;

        [SerializeField, Min(0.1f)] private float _staminaRecoveryRate = 15f;

        [SerializeField, Min(0f)] private float _staminaRecoveryCooldown = 3f;

        private InputSystem_Actions _inputActions;
        private InputSystem_Actions.PlayerActions _playerActions;
        private Rigidbody2D _rigidbody;

        private Vector2 _moveInput;
        private bool _isSprinting;

        private Vector2 _lastPublishedMoveInput;
        private bool _lastPublishedSprint;

        // Sprint transition state
        private float _currentSprintMultiplier; // 当前实际的冲刺倍率
        private float _sprintTransitionTimer;
        private bool _isTransitioningToSprint;

        // Stamina recovery state
        private float _staminaRecoveryTimer;
        private readonly DisposableBag _disposableBag = new DisposableBag();

        #region Runtime Debug Info (Odin Inspector Only)

        [FoldoutGroup("Runtime State (Read Only)", Expanded = false), PropertyOrder(1000)]
        [ShowInInspector, ReadOnly, PropertyTooltip("当前实际的冲刺速度倍率（1.0 = 正常速度）")]
        private float CurrentSprintMultiplier => _currentSprintMultiplier;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1001)]
        [ShowInInspector, ReadOnly, PropertyTooltip("冲刺加速/减速过渡计时器")]
        private float SprintTransitionTimer => _sprintTransitionTimer;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1002)]
        [ShowInInspector, ReadOnly, PropertyTooltip("体力恢复冷却计时器")]
        private float StaminaRecoveryTimer => _staminaRecoveryTimer;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1003)]
        [ShowInInspector, ReadOnly, PropertyTooltip("是否正在冲刺")]
        private bool IsSprinting => _isSprinting;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1004)]
        [ShowInInspector, ReadOnly, PropertyTooltip("当前移动输入向量")]
        private Vector2 MoveInput => _moveInput;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1005)]
        [ShowInInspector, ReadOnly, PropertyTooltip("当前角色速度")]
        private Vector2 CurrentVelocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1006)]
        [ShowInInspector, ReadOnly, PropertyTooltip("当前实际移动速度（考虑了冲刺倍率）")]
        private float CurrentSpeed => _rigidbody != null ? _rigidbody.linearVelocity.magnitude : 0f;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1007)]
        [ShowInInspector, ReadOnly, PropertyTooltip("体力恢复是否在冷却中")]
        private bool IsStaminaRecoveryOnCooldown =>
            _staminaRecoveryTimer < _staminaRecoveryCooldown && _stamina.Value < _maxStamina;

        #endregion

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _playerActions = _inputActions.Player;
            _rigidbody = GetComponent<Rigidbody2D>();

            // 配置Rigidbody2D以减少抖动
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep;
            
            // 锁定Z轴旋转（俯视角2D不需要）
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            // 设置线性阻尼为0，因为我们直接控制速度
            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0f;
            
            // 设置重力为0（俯视角2D不需要重力）
            _rigidbody.gravityScale = 0f;

            // 初始化体力
            _stamina.Value = _maxStamina;
            _currentSprintMultiplier = 1f;
            _sprintTransitionTimer = 0;
        }

        private void OnEnable()
        {
            _playerActions.Move.performed += OnMovePerformed;
            _playerActions.Move.canceled += OnMoveCanceled;
            _playerActions.Sprint.performed += OnSprintPerformed;
            _playerActions.Sprint.canceled += OnSprintCanceled;
            _playerActions.Interact.performed += OnInteractPerformed;
            _playerActions.LeftPage.performed += OnLeftPagePerformed;
            _playerActions.RightPage.performed += OnRightPagePerformed;
            _playerActions.Enable();
        }

        private void OnDisable()
        {
            _playerActions.Move.performed -= OnMovePerformed;
            _playerActions.Move.canceled -= OnMoveCanceled;
            _playerActions.Sprint.performed -= OnSprintPerformed;
            _playerActions.Sprint.canceled -= OnSprintCanceled;
            _playerActions.Interact.performed -= OnInteractPerformed;
            _playerActions.LeftPage.performed -= OnLeftPagePerformed;
            _playerActions.RightPage.performed -= OnRightPagePerformed;
            _playerActions.Disable();
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
            _disposableBag.Dispose();
        }

        private void FixedUpdate()
        {
            UpdateSprintTransition();
            UpdateStamina();
            
            Vector2 clampedInput = Vector2.ClampMagnitude(_moveInput, 1f);

            // 使用动画曲线控制的冲刺倍率直接计算目标速度
            float currentSpeed = _defaultMoveSpeed * _currentSprintMultiplier;
            _rigidbody.linearVelocity = clampedInput * currentSpeed;
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
            if (_isSprinting == false)
            {
                _sprintTransitionTimer = 0;
            }

            // 检查是否有足够体力
            if (_stamina.Value > _maxStamina * 0.15f)
            {
                _isSprinting = true;
                PublishSprintState();
                // 重置体力恢复冷却
                _staminaRecoveryTimer = 0f;
            }

            TryPublishMoveEvent();
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            if (_isSprinting)
            {
                _sprintTransitionTimer = 0;
            }

            _isSprinting = false;
            PublishSprintState();
            TryPublishMoveEvent();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            _eventBus?.Publish(new InteractEvent());
        }

        private void OnRightPagePerformed(InputAction.CallbackContext context)
        {
            _eventBus?.Publish(new RightPageEvent());
        }

        private void OnLeftPagePerformed(InputAction.CallbackContext context)
        {
            _eventBus?.Publish(new LeftPageEvent());
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

        private void UpdateSprintTransition()
        {
            // 更新过渡计时器（使用固定时间步长）
            if (_isSprinting)
            {
                // 正在加速到冲刺状态
                _sprintTransitionTimer = Mathf.Min(_sprintTransitionTimer + Time.fixedDeltaTime, _sprintTransitionDuration);

                float t = Mathf.Clamp01(_sprintTransitionTimer / _sprintTransitionDuration);
                _currentSprintMultiplier = Mathf.Lerp(1f, _sprintMultiplier, _sprintCurve.Evaluate(t));
            }
            else if (!_isSprinting)
            {
                // 正在减速到正常状态
                _sprintTransitionTimer = Mathf.Min(_sprintTransitionTimer + Time.fixedDeltaTime, _sprintTransitionDuration);

                float t = Mathf.Clamp01(_sprintTransitionTimer / _sprintTransitionDuration);
                _currentSprintMultiplier = Mathf.Lerp(_sprintMultiplier, 1f, _sprintCurve.Evaluate(t));
            }
        }

        private void UpdateStamina()
        {
            // 体力消耗（使用固定时间步长）
            if (_isSprinting)
            {
                _stamina.Value -= _staminaDrainRate * Time.fixedDeltaTime;

                // 体力耗尽，强制停止冲刺
                if (_stamina.Value <= 0f)
                {
                    _stamina.Value = 0f;
                    _isSprinting = false;
                    PublishSprintState();
                    TryPublishMoveEvent();
                }
            }
            // 体力恢复
            else if (_stamina.Value < _maxStamina)
            {
                // 累计恢复冷却时间
                _staminaRecoveryTimer += Time.fixedDeltaTime;

                // 冷却结束后开始恢复
                if (_staminaRecoveryTimer >= _staminaRecoveryCooldown)
                {
                    _stamina.Value += _staminaRecoveryRate * Time.fixedDeltaTime;
                    _stamina.Value = Mathf.Min(_stamina.Value, _maxStamina);
                }
            }
        }

        private Color GetStaminaBarColor()
        {
            float ratio = _stamina.Value / _maxStamina;
            if (ratio > 0.5f) return Color.green;
            if (ratio > 0.25f) return Color.yellow;
            return Color.red;
        }
    }
}