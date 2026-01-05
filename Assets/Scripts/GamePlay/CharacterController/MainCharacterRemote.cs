using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePlay.CharacterController
{
    /// <summary>
    /// 遥控器组件，允许角色从面朝方向发射射线与触发器交互
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class MainCharacterRemote : MonoBehaviour
    {
        [Title("Cooldown Settings")]
        [SerializeField, Min(0f), Tooltip("遥控器使用冷却时长（秒）")]
        private float _cooldownDuration = 2f;

        [Title("Raycast Settings")]
        [SerializeField, Min(0.1f), Tooltip("射线最大距离")]
        private float _raycastDistance = 10f;

        [SerializeField, Tooltip("可交互层级（用于过滤碰撞检测）")]
        private LayerMask _interactableLayer = ~0; // 默认所有层

        [Title("Visual Settings")]
        [SerializeField, Min(0.01f), Tooltip("射线显示时长（秒）")]
        private float _lineDisplayDuration = 0.3f;

        [SerializeField, Tooltip("射线颜色渐变")]
        private Gradient _lineColorGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.cyan, 0f),
                new GradientColorKey(Color.blue, 1f)
            },
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        };

        [SerializeField, Min(0.001f), Tooltip("射线宽度")]
        private float _lineWidth = 0.1f;

        [SerializeField, Tooltip("射线宽度曲线（从起点到终点）")]
        private AnimationCurve _lineWidthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        #region Runtime State (Read Only)

        [FoldoutGroup("Runtime State (Read Only)", Expanded = false), PropertyOrder(1000)]
        [ShowInInspector, ReadOnly, PropertyTooltip("是否在冷却中")]
        private bool IsOnCooldown => Time.time < _nextAvailableTime;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1001)]
        [ShowInInspector, ReadOnly, PropertyTooltip("冷却剩余时间（秒）")]
        [ProgressBar(0, nameof(_cooldownDuration), ColorGetter = nameof(GetCooldownBarColor))]
        private float CooldownRemaining => Mathf.Max(0f, _nextAvailableTime - Time.time);

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1002)]
        [ShowInInspector, ReadOnly, PropertyTooltip("上次击中的对象")]
        private GameObject LastHitObject => _lastHitCollider != null ? _lastHitCollider.gameObject : null;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1003)]
        [ShowInInspector, ReadOnly, PropertyTooltip("上次射线是否击中可交互触发器")]
        private bool LastHitWasControllable => _lastHitWasControllable;

        #endregion

        private InputSystem_Actions _inputActions;
        private InputSystem_Actions.PlayerActions _playerActions;
        private LineRenderer _lineRenderer;

        private float _nextAvailableTime; // 下次可用时间
        private CancellationTokenSource _hideLineCts;
        private Collider2D _lastHitCollider;
        private bool _lastHitWasControllable;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _playerActions = _inputActions.Player;

            // 初始化 LineRenderer
            _lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
            _lineRenderer.enabled = false;
        }

        private void OnEnable()
        {
            _playerActions.RemoteInteract.performed += OnRemoteInteractPerformed;
            _playerActions.Enable();
        }

        private void OnDisable()
        {
            _playerActions.RemoteInteract.performed -= OnRemoteInteractPerformed;
            _playerActions.Disable();

            // 取消异步任务
            CancelHideLineTask();
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
            CancelHideLineTask();
        }

        /// <summary>
        /// 输入回调：玩家按下遥控器按键
        /// </summary>
        private void OnRemoteInteractPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            TryUseRemote();
        }

        /// <summary>
        /// 尝试使用遥控器
        /// </summary>
        private void TryUseRemote()
        {
            // 检查冷却
            if (IsOnCooldown)
            {
                OnRemoteUsedWhileOnCooldown();
                return;
            }

            // 执行遥控器逻辑
            PerformRemoteInteraction();

            // 设置冷却
            _nextAvailableTime = Time.time + _cooldownDuration;
        }

        /// <summary>
        /// 执行遥控器交互（射线检测 + 可视化）
        /// </summary>
        private void PerformRemoteInteraction()
        {
            Vector2 origin = transform.position;
            Vector2 direction = GetFacingDirection();

            // 射线检测（检测所有碰撞）
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, _raycastDistance, _interactableLayer);

            Vector2 endPoint;

            if (hit.collider != null)
            {
                // 击中了某个碰撞体
                endPoint = hit.point;
                _lastHitCollider = hit.collider;

                // 检查是否是可交互的触发器
                if (hit.collider.isTrigger && hit.collider.CompareTag("RemoteControllable"))
                {
                    _lastHitWasControllable = true;
                    OnRemoteHitTrigger(hit.collider);
                }
                else
                {
                    _lastHitWasControllable = false;
                }
            }
            else
            {
                // 未击中任何碰撞体，显示完整距离
                endPoint = origin + direction * _raycastDistance;
                _lastHitCollider = null;
                _lastHitWasControllable = false;
            }

            // 显示射线
            ShowRaycastLine(origin, endPoint);
        }

        /// <summary>
        /// 获取角色面朝方向（基于 Transform 旋转）
        /// </summary>
        private Vector2 GetFacingDirection()
        {
            // 从 Z 轴旋转角度转换为 2D 方向向量
            float angle = transform.eulerAngles.z;
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        /// <summary>
        /// 显示射线（使用 LineRenderer）
        /// </summary>
        private void ShowRaycastLine(Vector2 start, Vector2 end)
        {
            // 取消之前的隐藏任务
            CancelHideLineTask();
            // 显示线段
            _lineRenderer.enabled = true;
            // 设置线段位置
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, new Vector3(start.x, start.y, 0f));
            _lineRenderer.SetPosition(1, new Vector3(end.x, end.y, 0f));

            // 创建新的 CancellationTokenSource 并启动异步隐藏任务
            _hideLineCts = new CancellationTokenSource();
            HideLineAfterDelayAsync(_hideLineCts.Token).Forget();
        }

        /// <summary>
        /// 延迟隐藏射线（异步）
        /// </summary>
        private async UniTask HideLineAfterDelayAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(_lineDisplayDuration), 
                    cancellationToken: ct
                );
                
                _lineRenderer.enabled = false;
            }
            catch (System.OperationCanceledException)
            {
                // 任务被取消，正常情况
            }
        }

        /// <summary>
        /// 取消隐藏射线任务
        /// </summary>
        private void CancelHideLineTask()
        {
            if (_hideLineCts != null)
            {
                _hideLineCts.Cancel();
                _hideLineCts.Dispose();
                _hideLineCts = null;
            }
        }

        /// <summary>
        /// 配置 LineRenderer 初始设置
        /// </summary>
        private void ConfigureLineRenderer()
        {
            _lineRenderer.positionCount = 2;
            _lineRenderer.colorGradient = _lineColorGradient;
            _lineRenderer.widthMultiplier = _lineWidth;
            _lineRenderer.widthCurve = _lineWidthCurve;

            // 2D 设置
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.alignment = LineAlignment.TransformZ;
        }

        #region TODO Methods

        /// <summary>
        /// 当遥控器在冷却中被使用时调用
        /// TODO: 实现冷却中使用的反馈逻辑
        /// </summary>
        private void OnRemoteUsedWhileOnCooldown()
        {
            // TODO: 添加音效、UI提示或其他反馈
            Debug.Log($"[MainCharacterRemote] Remote is on cooldown! ({CooldownRemaining:F2}s remaining)");
        }

        /// <summary>
        /// 当遥控器击中可交互触发器时调用
        /// TODO: 实现与触发器的交互逻辑
        /// </summary>
        /// <param name="target">击中的触发器 Collider2D</param>
        private void OnRemoteHitTrigger(Collider2D target)
        {
            // TODO: 实现与目标的交互逻辑
            Debug.Log($"[MainCharacterRemote] Hit controllable trigger: {target.gameObject.name}");
        }

        #endregion

        #region Editor Utilities

        [FoldoutGroup("Debug Tools"), PropertyOrder(2000)]
        [Button("测试使用遥控器"), DisableInEditorMode]
        private void TestRemote()
        {
            TryUseRemote();
        }

        [FoldoutGroup("Debug Tools"), PropertyOrder(2001)]
        [Button("重置冷却"), DisableInEditorMode]
        private void ResetCooldown()
        {
            _nextAvailableTime = 0f;
        }

        private Color GetCooldownBarColor()
        {
            if (!IsOnCooldown) return Color.green;
            float ratio = CooldownRemaining / _cooldownDuration;
            if (ratio > 0.5f) return Color.yellow;
            return Color.red;
        }

        #endregion
    }
}