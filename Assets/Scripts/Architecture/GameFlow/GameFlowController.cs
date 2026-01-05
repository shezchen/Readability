using System;
using System.Collections.Generic;
using System.Linq;
using Architecture.GameSound;
using Architecture.Language;
using Cysharp.Threading.Tasks;
using GamePlay;
using GamePlay.Events;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;
using Yarn.Unity;
using Object = UnityEngine.Object;

namespace Architecture
{
    public class GameFlowController : SerializedMonoBehaviour
    {
        [LabelText("跳过启动流程"), SerializeField] private bool _skipStartupFlow = false;
        
        [FoldoutGroup("Yarn Projects"), SerializeField] private YarnProject mainStoryYarnProject;
        [FoldoutGroup("Yarn Projects"), SerializeField] private YarnProject gamePlayYarnProject;

        [Title("Document Page Data")]
        [SerializeField, DictionaryDrawerSettings(
            DisplayMode = DictionaryDisplayOptions.Foldout,
            KeyLabel = "Page ID",
            ValueLabel = "Page Data"
        )]
        [Tooltip("文档页面数据字典，Key为页面ID，Value为页面数据")]
        private Dictionary<string, DocumentPageData> _documentPageDataDict = new Dictionary<string, DocumentPageData>();
        
        [Inject] private UIManager _uiManager;
        [Inject] private SaveManager _saveManager;
        [Inject] private LanguageManager _languageManager;
        [Inject] private IAudioService _audioService;
        [Inject] private EventBus _eventBus;
        [Inject] private GamePlay.GamePlayRoot _gamePlayRoot;
        [Inject] private GamePlay.GamePlayManager _gamePlayManager;
        private async void Start()
        {
            await _languageManager.Init();
            await _saveManager.Init();
            await _uiManager.Init();

            if (_skipStartupFlow)
            {
                return;
            }

            //此处可以加是否是首次启动的判断
            await _uiManager.ShowLanguagePage();
        }
        
        /// <summary>
        /// 进入GamePlay
        /// </summary>
        public async UniTask EnterGamePlay()
        {
            //TODO 设置UI Manager的加载
            await StartGameDay(_saveManager.CurrentGameSave.GameDay);
        }
        
        public async UniTask ExitGamePlay()
        {
            // TODO
        }

        public async UniTask StartGameDay(int day)
        {
            _uiManager.ClearUIRoot();
            _gamePlayRoot.ClearGamePlayRoot();
            _gamePlayManager.ResetGamePlay();
            
            switch (day)
            {
                case 0:
                    await _uiManager.DisplayYarnStory(StoryName.Prologue, mainStoryYarnProject);
                    break;
                case 1:
                    Debug.Log("进入第一天");
                    //第一天直接进入关卡
                    var go = await Addressables.LoadAssetAsync<GameObject>(AddressableKeys.Assets.LevelTestPrefab);
                    var instance = Instantiate(go, _gamePlayRoot.transform);
                    ScopeRef.LifetimeScope.Container.InjectGameObject(instance);
                    _eventBus.Publish(new SetPageListEvent(new List<DocumentPageData>()
                    {
                        _documentPageDataDict["1-1"],
                        _documentPageDataDict["1-2"],
                        _documentPageDataDict["1-3"]
                    }));
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
            }
        }
        
        [YarnCommand("EndDayZero")]
        public async void EndDayZero()
        {
            await EndGameDay();
        }

        public async UniTask EndGameDay()
        {
            if (_saveManager.CurrentGameSave.GameDay == 5)
            {
                // TODO 游戏结束流程
            }
            else
            {
                _saveManager.CurrentGameSave.GameDay++;
                await _saveManager.SaveGame(_saveManager.CurrentGameSaveIndex);
                
                // 继续下一天的流程
                await StartGameDay(_saveManager.CurrentGameSave.GameDay);
            }
        }
    }
}