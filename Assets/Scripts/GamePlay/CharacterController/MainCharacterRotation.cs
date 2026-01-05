using DG.Tweening;
using Sirenix.OdinInspector;
using Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePlay.CharacterController
{
    /// <summary>
    /// 角色旋转组件，根据鼠标位置或手柄右摇杆方向平滑旋转Z轴
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MainCharacterRotation : MonoBehaviour
    {
        [Title("Input Settings")]
        [SerializeField, Required, Tooltip("用于鼠标屏幕坐标转世界坐标的相机")]
        private Camera _camera;

        [SerializeField, Range(0f, 1f), Tooltip("手柄右摇杆死区阈值")]
        private float _gamepadDeadZone = 0.1f;

        [SerializeField, Min(0f), Tooltip("最小输入阈值，低于此值不旋转（避免抖动）")]
        private float _minInputThreshold = 0.1f;

        [Title("Rotation Settings")]
        [SerializeField, Min(0.01f), Tooltip("旋转动画时长（秒）")]
        private float _rotationDuration = 0.2f;

        [SerializeField, Tooltip("旋转缓动类型")]
        private Ease _rotationEase = Ease.OutQuad;

        #region Runtime State (Read Only)

        [FoldoutGroup("Runtime State (Read Only)", Expanded = false), PropertyOrder(1000)]
        [ShowInInspector, ReadOnly, PropertyTooltip("当前使用的输入设备")]
        private string CurrentInputDevice => _isUsingGamepad ? "Gamepad" : "Mouse";

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1001)]
        [ShowInInspector, ReadOnly, PropertyTooltip("当前输入方向向量")]
        private Vector2 CurrentInputDirection => _currentInputDirection;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1002)]
        [ShowInInspector, ReadOnly, PropertyTooltip("手柄右摇杆原始值（用于调试死区）")]
        private Vector2 GamepadRightStickRaw => Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1003)]
        [ShowInInspector, ReadOnly, PropertyTooltip("当前旋转角度（度）")]
        private float CurrentRotationAngle => transform.eulerAngles.z;

        #endregion

        private Tweener _currentRotationTween;
        private bool _isUsingGamepad;
        private Vector2 _currentInputDirection;

        private void Awake()
        {
            // 验证相机引用
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            
            // 初始化旋转为(0,0,0)
            transform.rotation = Quaternion.identity;
        }

        private void FixedUpdate()
        {
            if (_camera == null) return;

            Vector2 lookDirection = GetLookDirection();
            _currentInputDirection = lookDirection;

            // 如果输入小于阈值，保持当前角度
            if (lookDirection.magnitude < _minInputThreshold)
            {
                return;
            }

            // 计算目标角度（Atan2返回弧度，转换为度）
            float targetAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            // 如果目标角度与当前角度相同，跳过旋转
            float currentAngle = transform.eulerAngles.z;
            if (Mathf.Approximately(Mathf.DeltaAngle(currentAngle, targetAngle), 0f))
            {
                return;
            }

            // 停止当前旋转动画
            if (_currentRotationTween != null && _currentRotationTween.IsActive())
            {
                _currentRotationTween.Kill();
            }

            // 使用DOTween平滑旋转到目标角度
            _currentRotationTween = transform.RotateZ(targetAngle, _rotationDuration, RotateMode.Fast, _rotationEase);
        }

        /// <summary>
        /// 获取朝向方向（手柄优先，然后是鼠标）
        /// </summary>
        private Vector2 GetLookDirection()
        {
            // 优先检查手柄输入
            Vector2 gamepadDirection = GetGamepadDirection();
            
            if (gamepadDirection.magnitude > _gamepadDeadZone)
            {
                _isUsingGamepad = true;
                return gamepadDirection.normalized;
            }
            
            // 使用鼠标输入
            _isUsingGamepad = false;
            return GetMouseDirection();
        }

        /// <summary>
        /// 获取手柄右摇杆方向
        /// </summary>
        private Vector2 GetGamepadDirection()
        {
            if (Gamepad.current == null)
                return Vector2.zero;
            
            return Gamepad.current.rightStick.ReadValue();
        }

        /// <summary>
        /// 获取鼠标指向角色的方向
        /// </summary>
        private Vector2 GetMouseDirection()
        {
            if (Mouse.current == null)
                return Vector2.zero;
            
            // 获取鼠标屏幕坐标
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            
            // 转换为世界坐标（2D正交相机）
            Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(
                new Vector3(mouseScreenPos.x, mouseScreenPos.y, _camera.nearClipPlane)
            );
            
            // 计算从角色位置指向鼠标位置的方向
            Vector2 direction = (mouseWorldPos - transform.position);
            
            return direction.normalized;
        }

        private void OnDisable()
        {
            StopRotationAnimation();
        }

        private void OnDestroy()
        {
            StopRotationAnimation();
        }

        /// <summary>
        /// 停止当前旋转动画
        /// </summary>
        private void StopRotationAnimation()
        {
            if (_currentRotationTween != null && _currentRotationTween.IsActive())
            {
                _currentRotationTween.Kill();
                _currentRotationTween = null;
            }
        }
    }
}