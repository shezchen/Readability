using Architecture;
using Cysharp.Threading.Tasks;
using UI.Page;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

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
        /// 第一次加载并进入GamePlay
        /// </summary>
        public async UniTask EnterGamePlay()
        {
            // 开始对话系统
            
            // TODO
        }
    }
}