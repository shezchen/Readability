using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePlay
{
    /// <summary>
    /// 页面阅读器开关组件，使用平滑透明度动画切换显示状态
    /// </summary>
    public class PageReaderToggle : MonoBehaviour
    {
        [Title("References")]
        [SerializeField, Required, Tooltip("用于控制透明度的 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [SerializeField, Required, Tooltip("页面阅读器根对象")]
        private GameObject _pageReader;

        [Title("Animation Settings")]
        [SerializeField, Min(0.01f), Tooltip("渐变动画时长（秒）")]
        private float _fadeDuration = 0.3f;

        [SerializeField, Tooltip("渐变动画缓动类型")]
        private Ease _fadeEase = Ease.OutQuad;

        [SerializeField, Tooltip("动画完成后是否设置交互状态")]
        private bool _setInteractable = true;

        [SerializeField, Tooltip("动画完成后是否设置射线阻挡")]
        private bool _setBlocksRaycasts = true;

        #region Runtime State (Read Only)

        [FoldoutGroup("Runtime State (Read Only)", Expanded = false), PropertyOrder(1000)]
        [ShowInInspector, ReadOnly, PropertyTooltip("阅读器当前是否打开")]
        private bool IsOpen => _isOpen;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1001)]
        [ShowInInspector, ReadOnly, PropertyTooltip("是否正在执行动画")]
        private bool IsAnimating => _currentTween != null && _currentTween.IsActive();

        #endregion

        private bool _isOpen;
        private InputSystem_Actions _inputActions;
        private InputSystem_Actions.PlayerActions _playerActions;
        private Tweener _currentTween;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();
            _playerActions = _inputActions.Player;

            // 初始化为打开状态
            _isOpen = true;
            _pageReader.SetActive(true);
            _canvasGroup.alpha = 1f;
            
            if (_setInteractable)
                _canvasGroup.interactable = true;
            if (_setBlocksRaycasts)
                _canvasGroup.blocksRaycasts = true;
        }

        private void OnEnable()
        {
            _playerActions.ToggleReader.performed += ToggleReader;
            _playerActions.Enable();
        }

        private void OnDisable()
        {
            _playerActions.ToggleReader.performed -= ToggleReader;
            _playerActions.Disable();

            // 取消正在进行的动画
            CancelCurrentTween();
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
            CancelCurrentTween();
        }

        /// <summary>
        /// 输入回调：切换阅读器显示状态
        /// </summary>
        private void ToggleReader(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            // 如果正在执行动画，忽略输入
            if (IsAnimating) return;

            if (_isOpen)
            {
                CloseReaderAsync().Forget();
            }
            else
            {
                OpenReaderAsync().Forget();
            }
        }

        /// <summary>
        /// 打开阅读器（异步）
        /// </summary>
        private async UniTask OpenReaderAsync()
        {
            // 取消之前的动画
            CancelCurrentTween();

            // 先激活对象
            _pageReader.SetActive(true);

            // 从当前透明度渐变到1
            _currentTween = _canvasGroup.FadeTo(
                1f,
                _fadeDuration,
                _setInteractable,
                _setBlocksRaycasts,
                _fadeEase
            );

            // 等待动画完成
            if (_currentTween != null)
            {
                await _currentTween.AsyncWaitForCompletion();
            }

            _isOpen = true;
            _currentTween = null;
        }

        /// <summary>
        /// 关闭阅读器（异步）
        /// </summary>
        private async UniTask CloseReaderAsync()
        {
            // 取消之前的动画
            CancelCurrentTween();

            // 从当前透明度渐变到0
            _currentTween = _canvasGroup.FadeTo(
                0f,
                _fadeDuration,
                _setInteractable,
                _setBlocksRaycasts,
                _fadeEase
            );

            // 等待动画完成
            if (_currentTween != null)
            {
                await _currentTween.AsyncWaitForCompletion();
            }

            // 动画完成后禁用对象
            _pageReader.SetActive(false);

            _isOpen = false;
            _currentTween = null;
        }

        /// <summary>
        /// 取消当前正在进行的动画
        /// </summary>
        private void CancelCurrentTween()
        {
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
                _currentTween = null;
            }
        }

        #region Editor Utilities

        [FoldoutGroup("Debug Tools"), PropertyOrder(2000)]
        [Button("打开阅读器"), DisableInEditorMode, DisableIf(nameof(_isOpen))]
        private void TestOpen()
        {
            OpenReaderAsync().Forget();
        }

        [FoldoutGroup("Debug Tools"), PropertyOrder(2001)]
        [Button("关闭阅读器"), DisableInEditorMode, EnableIf(nameof(_isOpen))]
        private void TestClose()
        {
            CloseReaderAsync().Forget();
        }

        #endregion
    }
}