using System;
using System.Linq;
using Architecture.GameSound;
using Architecture.Language;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;
using VContainer;
using Yarn.Unity;
using Object = UnityEngine.Object;

namespace Architecture
{
    public class GameFlowController : MonoBehaviour
    {
        [LabelText("跳过启动流程"), SerializeField] private bool _skipStartupFlow = false;
        
        [FoldoutGroup("Yarn Projects"), SerializeField] private YarnProject mainStoryYarnProject;
        [FoldoutGroup("Yarn Projects"), SerializeField] private YarnProject gamePlayYarnProject;
        
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
            //清空UI Root
            _uiManager.ClearUIRoot();
            _gamePlayRoot.ClearGamePlayRoot();
            _gamePlayManager.Reset();
            await StartGameDay(_saveManager.CurrentGameSave.GameDay);
        }
        
        public async UniTask ExitGamePlay()
        {
            // TODO
        }

        public async UniTask StartGameDay(int day)
        {
            switch (day)
            {
                case 0:
                    await _uiManager.DisplayYarnStory(StoryName.Prologue, mainStoryYarnProject);
                    break;
                case 1:
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
            _uiManager.ClearUIRoot();
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