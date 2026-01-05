using System.Collections.Generic;
using Architecture;
using DG.Tweening;
using GamePlay.Events;
using R3;
using Sirenix.OdinInspector;
using TMPro;
using Tools;
using UI.Page;
using UnityEngine;
using VContainer;

namespace GamePlay
{
    /// <summary>
    /// 文档阅读器页面组件，支持翻页和打字机效果显示
    /// </summary>
    [RequireComponent(typeof(UIBinder))]
    public class DocumentReaderPage : MonoBehaviour
    {
        [Title("Document Configuration")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true), Tooltip("页面列表")]
        private List<DocumentPageData> _pageList = new List<DocumentPageData>();

        [Title("UI References")]
        [SerializeField, Required, Tooltip("文本内容显示组件")]
        private TextMeshProUGUI _textContent;

        [SerializeField, Tooltip("页码显示组件（可选）")]
        private TextMeshProUGUI _pageIndicator;

        [Title("Page Indicator Settings")]
        [SerializeField, Tooltip("是否显示页码")]
        private bool _enablePageIndicator = true;

        [SerializeField, Tooltip("页码显示格式（{0}=当前页, {1}=总页数）")]
        private string _pageIndicatorFormat = "{0}/{1}";

        [Title("Typewriter Settings")]
        [SerializeField, Min(0.01f), Tooltip("打字机效果时长（秒）")]
        private float _typewriterDuration = 1f;

        [SerializeField, Tooltip("打字机效果缓动类型")]
        private Ease _typewriterEase = Ease.Linear;

        #region Runtime State (Read Only)

        [FoldoutGroup("Runtime State (Read Only)", Expanded = false), PropertyOrder(1000)]
        [ShowInInspector, ReadOnly, PropertyTooltip("当前页面索引（从0开始）")]
        private int CurrentPageIndex => _currentPageIndex;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1001)]
        [ShowInInspector, ReadOnly, PropertyTooltip("文档总页数")]
        private int TotalPages => _pageList?.Count ?? 0;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1002)]
        [ShowInInspector, ReadOnly, PropertyTooltip("是否可以翻到上一页")]
        private bool CanGoToPreviousPage => _currentPageIndex > 0;

        [FoldoutGroup("Runtime State (Read Only)"), PropertyOrder(1003)]
        [ShowInInspector, ReadOnly, PropertyTooltip("是否可以翻到下一页")]
        private bool CanGoToNextPage => _currentPageIndex < (TotalPages - 1);

        #endregion

        [Inject] private EventBus _eventBus;

        private int _currentPageIndex = 0;
        private UIBinder _uiBinder;
        private Tweener _currentTypewriterTween;
        private DisposableBag _disposableBag;

        private void Awake()
        {
            ScopeRef.LifetimeScope.Container.Inject(this);
            _uiBinder = GetComponent<UIBinder>();
        }

        private void OnEnable()
        {
            _disposableBag = new DisposableBag();
            
            _eventBus.Receive<LeftPageEvent>()
                .Subscribe(OnLeftPageEvent)
                .AddTo(ref _disposableBag);

            _eventBus.Receive<RightPageEvent>()
                .Subscribe(OnRightPageEvent)
                .AddTo(ref _disposableBag);

            _eventBus.Receive<SetPageListEvent>()
                .Subscribe(OnSetPageListEvent)
                .AddTo(ref _disposableBag);
        }

        private void OnDisable()
        {
            _disposableBag.Dispose();
            StopTypewriterAnimation();
        }

        private void OnDestroy()
        {
            StopTypewriterAnimation();
        }

        /// <summary>
        /// 设置页面列表并刷新显示
        /// </summary>
        public void SetPageList(List<DocumentPageData> pageList)
        {
            if (pageList == null)
            {
                _pageList = new List<DocumentPageData>();
            }
            else
            {
                _pageList = new List<DocumentPageData>(pageList);
            }
            
            _currentPageIndex = 0;
            GoToPage(0);
        }

        /// <summary>
        /// 在尾部添加一个页面并刷新显示
        /// </summary>
        public void AddPage(DocumentPageData pageData)
        {
            if (pageData == null)
            {
                Debug.LogWarning("[DocumentReaderPage] 尝试添加空的页面数据");
                return;
            }

            _pageList.Add(pageData);
            
            // 如果当前在最后一页（或列表为空时添加第一个页面），刷新显示新添加的页面
            if (_pageList.Count == 1 || _currentPageIndex == _pageList.Count - 2)
            {
                GoToPage(_pageList.Count - 1);
            }
            else
            {
                // 否则只更新页码显示
                UpdatePageIndicator();
            }
        }

        /// <summary>
        /// 切换到指定页面
        /// </summary>
        private void GoToPage(int pageIndex)
        {
            if (_pageList == null || _pageList.Count == 0)
            {
                Debug.LogWarning("[DocumentReaderPage] 页面列表为空或没有页面");
                return;
            }

            if (pageIndex < 0 || pageIndex >= _pageList.Count)
            {
                Debug.LogWarning($"[DocumentReaderPage] 页面索引 {pageIndex} 超出范围 (0-{_pageList.Count - 1})");
                return;
            }

            _currentPageIndex = pageIndex;
            UpdateDisplayWithTypewriter();
        }

        /// <summary>
        /// 使用打字机效果更新UI显示
        /// </summary>
        private void UpdateDisplayWithTypewriter()
        {
            if (_pageList == null || _currentPageIndex < 0 || _currentPageIndex >= _pageList.Count)
            {
                return;
            }

            var pageData = _pageList[_currentPageIndex];
            if (pageData == null)
            {
                Debug.LogWarning($"[DocumentReaderPage] 页面 {_currentPageIndex} 数据为空");
                return;
            }

            // 停止当前打字机动画
            StopTypewriterAnimation();

            // 清空文本
            if (_textContent != null)
            {
                _textContent.text = "";
            }

            // 使用打字机效果显示新页面内容
            if (_textContent != null && !string.IsNullOrEmpty(pageData.Text))
            {
                _currentTypewriterTween = _textContent.TypeText(pageData.Text, _typewriterDuration, _typewriterEase);
            }

            // 更新页码显示（立即更新，不使用打字机效果）
            UpdatePageIndicator();
        }

        /// <summary>
        /// 更新页码显示
        /// </summary>
        private void UpdatePageIndicator()
        {
            if (_enablePageIndicator && _pageIndicator != null)
            {
                _pageIndicator.text = string.Format(_pageIndicatorFormat, _currentPageIndex + 1, _pageList.Count);
            }
        }

        /// <summary>
        /// 停止当前打字机动画
        /// </summary>
        private void StopTypewriterAnimation()
        {
            if (_currentTypewriterTween != null && _currentTypewriterTween.IsActive())
            {
                _currentTypewriterTween.Kill();
                _currentTypewriterTween = null;
            }
        }

        /// <summary>
        /// 处理左翻页事件（上一页）
        /// </summary>
        private void OnLeftPageEvent(LeftPageEvent evt)
        {
            if (CanGoToPreviousPage)
            {
                GoToPage(_currentPageIndex - 1);
            }
        }

        /// <summary>
        /// 处理右翻页事件（下一页）
        /// </summary>
        private void OnRightPageEvent(RightPageEvent evt)
        {
            if (CanGoToNextPage)
            {
                GoToPage(_currentPageIndex + 1);
            }
        }

        /// <summary>
        /// 处理重置页面列表事件
        /// </summary>
        private void OnSetPageListEvent(SetPageListEvent evt)
        {
            SetPageList(evt.PageDataList);
        }
    }
}

