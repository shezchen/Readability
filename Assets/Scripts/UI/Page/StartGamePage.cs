using System;
using Architecture;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using Tools;
using UI.Button;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace UI.Page
{
    /// <summary>
    /// 选择存档并载入页面
    /// </summary>
    [RequireComponent(typeof(CanvasGroup),typeof(UIBinder))]
    public class StartGamePage : BasePage
    {
        [Title("渐变时长"),SerializeField] private float fadeDuration = 0.5f;
        [BoxGroup("按钮"),Header("加载存档按钮"), SerializeField] private UnityEngine.UI.Button loadButton;
        [BoxGroup("按钮"),Header("删除存档按钮"), SerializeField] private UnityEngine.UI.Button deleteButton;
        [BoxGroup("按钮"),Header("创建新存档按钮"), SerializeField] private UnityEngine.UI.Button newSaveButton;

        [Inject] private SaveManager _saveManager;
        [Inject] private EventBus _eventBus;
        [Inject] private UIManager _uiManager;
        
        private CanvasGroup _canvasGroup;
        private UIBinder _uiBinder;
        
        private Tween _currentTween;
        private bool[] _saveExists;

        private void Awake()
        {
            ScopeRef.LifetimeScope.Container.Inject(this);
            _canvasGroup = GetComponent<CanvasGroup>();
            _uiBinder = GetComponent<UIBinder>();
            
            loadButton.gameObject.SetActive(false);
            deleteButton.gameObject.SetActive(false);
            newSaveButton.gameObject.SetActive(false);
        }

        public override async UniTask Display()
        {
            _eventBus.Publish(new PageShow(typeof(StartGamePage)));
            
            _saveExists = new []
            {
                _saveManager.CheckSaveExists(0),
                _saveManager.CheckSaveExists(1),
                _saveManager.CheckSaveExists(2)
            };

            for (int i = 0; i < 3; i++)
            {
                _uiBinder.Get<SaveSlotButton>("Button_SaveSlot"+i).SetSlotLabel(!_saveExists[i]);
            }
            
            _currentTween?.Kill();
            _currentTween = _canvasGroup.FadeIn(fadeDuration, true, true);
            await _currentTween.AsyncWaitForCompletion();
            EventSystem.current.SetSelectedGameObject(_uiBinder.Get("Button_SaveSlot1"));
        }

        public override async UniTask Hide()
        {
            _eventBus.Publish(new PageHide(typeof(StartGamePage)));
            
            _currentTween?.Kill();
            _currentTween = _canvasGroup.FadeOut(fadeDuration, false, false);
            await _currentTween.AsyncWaitForCompletion();
            Destroy(gameObject);
        }

        public void OnSaveSlotSubmitted(int index)
        {
            if (_saveExists[index])
            {
                loadButton.gameObject.SetActive(true);
                deleteButton.gameObject.SetActive(true);
                newSaveButton.gameObject.SetActive(false);
                
                loadButton.onClick.RemoveAllListeners();
                loadButton.onClick.AddListener(() => LoadSaveSlot(index));
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => DeleteSaveSlot(index));
                
                EventSystem.current.SetSelectedGameObject(loadButton.gameObject);
            }
            else
            {
                loadButton.gameObject.SetActive(false);
                deleteButton.gameObject.SetActive(false);
                newSaveButton.gameObject.SetActive(true);
                
                newSaveButton.onClick.RemoveAllListeners();
                newSaveButton.onClick.AddListener(() => CreateNewSaveSlot(index));
                
                EventSystem.current.SetSelectedGameObject(newSaveButton.gameObject);
            }
        }

        /// <summary>
        /// 加载存档，调用StartBlackScreen()黑屏切换，进入游戏，将自己销毁,注意进入游戏后MainScene是非激活
        /// </summary>
        /// <param name="index">存档槽序号0~2</param>
        public async void LoadSaveSlot(int index)
        {
            try
            {
                Debug.Log($"加载存档 {index}");
                await _uiManager.StartBlackScreen();
                await _saveManager.LoadGame(index);
                await _uiManager.EnterGamePlay();
                gameObject.SetActive(false);
                await _uiManager.EndBlackScreen();
                Destroy(gameObject);
            }
            catch (Exception e)
            {
                Debug.LogError($"加载存档 {index} 失败: {e.Message}");
            }
        }
        
        public void DeleteSaveSlot(int index)
        {
            Debug.Log($"删除存档 {index}");
            _saveManager.DeleteSave(index);
            
            _saveExists = new []
            {
                _saveManager.CheckSaveExists(0),
                _saveManager.CheckSaveExists(1),
                _saveManager.CheckSaveExists(2)
            };

            for (int i = 0; i < 3; i++)
            {
                _uiBinder.Get<SaveSlotButton>("Button_SaveSlot"+i).SetSlotLabel(!_saveExists[i]);
            }
            
            EventSystem.current.SetSelectedGameObject(_uiBinder.Get("Button_SaveSlot1"));
        }

        public async void CreateNewSaveSlot(int index)
        {
            try
            {
                Debug.Log($"创建新存档 {index}");
                await _saveManager.CreateNewSave(index);
                LoadSaveSlot(index);
            }
            catch (Exception e)
            {
                Debug.LogError($"创建新存档 {index} 失败: {e.Message}");
            }
        }
    }
}