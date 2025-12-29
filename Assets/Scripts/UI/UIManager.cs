using Architecture;
using Architecture.Language;
using Cysharp.Threading.Tasks;
using UI.Page;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;
using Yarn.Unity;

namespace UI
{
    /// <summary>
    /// 先初始化，之后根据流程控制器来加载UI
    /// </summary>
    public class UIManager:ManagerNeedInitializeBase
    {
        private UIRoot _uiRoot;
        private EventBus _eventBus;

        
        private AsyncOperationHandle<GameObject> _handleMainScenePrefab;
        private AsyncOperationHandle<GameObject> _handleStartGamePagePrefab;
        private AsyncOperationHandle<GameObject> _handleLanguagePagePrefab;
        private AsyncOperationHandle<GameObject> _handleSettingsPagePrefab;
        private AsyncOperationHandle<GameObject> _handleYarnDialoguePrefab;
        
        
        public UIManager(UIRoot root, EventBus eventBus)
        {
            _uiRoot = root;
            _eventBus = eventBus;
        }

        public override async UniTask Init()
        {
            await base.Init();

            //启动预加载
            _handleMainScenePrefab = Addressables.LoadAssetAsync<GameObject>(AddressableKeys.Assets.MainScenePrefab);
            _handleLanguagePagePrefab =
                Addressables.LoadAssetAsync<GameObject>(AddressableKeys.Assets.LanguagePagePrefab);
            _handleStartGamePagePrefab =
                Addressables.LoadAssetAsync<GameObject>(AddressableKeys.Assets.StartGamePagePrefab);
            _handleSettingsPagePrefab =
                Addressables.LoadAssetAsync<GameObject>(AddressableKeys.Assets.SettingsPagePrefab);
            _handleYarnDialoguePrefab =
                Addressables.LoadAssetAsync<GameObject>(AddressableKeys.Assets.StoryDialogueSystemPrefab);
        }
        
        public async UniTask ShowLanguagePage()
        {
            var go = await _handleLanguagePagePrefab;
            var instance = Object.Instantiate(go, _uiRoot.transform);
            ScopeRef.LifetimeScope.Container.InjectGameObject(instance);
            var page = instance.GetComponent<LanguagePage>();
            await page.Display();
        }

        public async UniTask ShowMainScenePage()
        {
            var go = await _handleMainScenePrefab;
            var instance = Object.Instantiate(go, _uiRoot.transform);
            ScopeRef.LifetimeScope.Container.InjectGameObject(instance);
            var page = instance.GetComponent<MainScenePage>();
            await page.Display();
        }
        
        public async UniTask ShowStartGamePage()
        {
            var go = await _handleStartGamePagePrefab;
            var instance = Object.Instantiate(go, _uiRoot.transform);
            ScopeRef.LifetimeScope.Container.InjectGameObject(instance);
            var page = instance.GetComponent<StartGamePage>();
            await page.Display();
        }

        public async UniTask ShowSettingsPage()
        {
            var go = await _handleSettingsPagePrefab;
            var instance = Object.Instantiate(go, _uiRoot.transform);
            ScopeRef.LifetimeScope.Container.InjectGameObject(instance);
            var page = instance.GetComponent<SettingsPage>();
            await page.Display();
        }
        
        public async UniTask StartBlackScreen()
        {
            _eventBus.Publish(new BlackScreenEvent(true));
            await UniTask.Delay(500);
        }

        public async UniTask EndBlackScreen()
        {
            _eventBus.Publish(new BlackScreenEvent(false));
            await UniTask.Delay(500);
        }
        
        /// <summary>
        /// 销毁UI Root下的所有子物体
        /// </summary>
        public void ClearUIRoot()
        {
            for (int i = _uiRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_uiRoot.transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 实例化对话系统Prefab，设置dialogue runner的yarn Project，设置line provider的本地化标志，播放Yarn脚本
        /// </summary>
        /// <param name="nodeName">播放节点名</param>
        /// <param name="yarnProject">需要传入Yarn Project</param>
        public async UniTask DisplayYarnStory(string nodeName,YarnProject yarnProject)
        {
            var go = await _handleYarnDialoguePrefab;
            var instance = Object.Instantiate(go, _uiRoot.transform);
            var dialogueRunner = instance.GetComponent<DialogueRunner>();
            
            dialogueRunner.SetProject(yarnProject);

            var lineProvider = instance.GetComponent<BuiltinLocalisedLineProvider>();
            lineProvider.LocaleCode  = LanguageManager.CurrentLanguage switch
            {
                GameLanguageType.Chinese => "zh-Hans",
                GameLanguageType.English => "en",
                GameLanguageType.Japanese => "ja",
                _ => "en"
            };
            lineProvider.AssetLocaleCode = lineProvider.LocaleCode;
            
            await dialogueRunner.StartDialogue(nodeName);
        }
    }
}