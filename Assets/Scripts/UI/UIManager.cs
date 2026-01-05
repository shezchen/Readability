using System;
using System.Collections.Generic;
using System.Linq;
using Architecture;
using Architecture.Language;
using Cysharp.Threading.Tasks;
using UI.Page;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;
using Yarn.Unity;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace UI
{
    /// <summary>
    /// 先初始化，之后根据流程控制器来加载UI
    /// </summary>
    public class UIManager : IDisposable
    {
        private UIRoot _uiRoot;
        private EventBus _eventBus;

        [Title("Resource Management")]
        [ShowInInspector, ReadOnly]
        private Dictionary<string, AsyncOperationHandle<GameObject>> _uiHandles = new();
        
        public UIManager(UIRoot root, EventBus eventBus)
        {
            _uiRoot = root;
            _eventBus = eventBus;
        }

        public async UniTask Init()
        {
            // 初始化为空，后续根据流程控制器调用 RefreshPreloadedUI
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 动态刷新预加载状态：加载列表中的资源，释放不在列表中的资源
        /// </summary>
        /// <param name="targetKeys">当前阶段需要的 UI 资源 Key 列表</param>
        public void RefreshPreloadedUI(List<string> targetKeys)
        {
            if (targetKeys == null) return;

            // 释放不再需要的资源
            var keysToRemove = _uiHandles.Keys
                .Where(key => !targetKeys.Contains(key))
                .ToList();

            foreach (var key in keysToRemove)
            {
                if (_uiHandles[key].IsValid())
                {
                    Addressables.Release(_uiHandles[key]);
                }
                _uiHandles.Remove(key);
                Debug.Log($"[UIManager] 已释放不需要的 UI 资源: {key}");
            }

            // 加载新增的资源
            foreach (var key in targetKeys)
            {
                if (!_uiHandles.ContainsKey(key))
                {
                    var handle = Addressables.LoadAssetAsync<GameObject>(key);
                    _uiHandles.Add(key, handle);
                    Debug.Log($"[UIManager] 开始预加载新增 UI 资源: {key}");
                }
            }
        }

        /// <summary>
        /// 获取已预加载的 Prefab，若不存在则进行强制加载
        /// </summary>
        private async UniTask<GameObject> GetPreloadedPrefab(string key)
        {
            if (_uiHandles.TryGetValue(key, out var handle))
            {
                return await handle;
            }
            
            var newHandle = Addressables.LoadAssetAsync<GameObject>(key);
            _uiHandles.Add(key, newHandle);
            return await newHandle;
        }
        
        public async UniTask ShowLanguagePage()
        {
            var go = await GetPreloadedPrefab(AddressableKeys.Assets.LanguagePagePrefab);
            var instance = UnityEngine.Object.Instantiate(go, _uiRoot.transform);
            ScopeRef.LifetimeScope.Container.InjectGameObject(instance);
            var page = instance.GetComponent<LanguagePage>();
            await page.Display();
        }

        public async UniTask ShowMainScenePage()
        {
            var go = await GetPreloadedPrefab(AddressableKeys.Assets.MainScenePrefab);
            var instance = UnityEngine.Object.Instantiate(go, _uiRoot.transform);
            ScopeRef.LifetimeScope.Container.InjectGameObject(instance);
            var page = instance.GetComponent<MainScenePage>();
            await page.Display();
        }
        
        public async UniTask ShowStartGamePage()
        {
            var go = await GetPreloadedPrefab(AddressableKeys.Assets.StartGamePagePrefab);
            var instance = UnityEngine.Object.Instantiate(go, _uiRoot.transform);
            ScopeRef.LifetimeScope.Container.InjectGameObject(instance);
            var page = instance.GetComponent<StartGamePage>();
            await page.Display();
        }

        public async UniTask ShowSettingsPage()
        {
            var go = await GetPreloadedPrefab(AddressableKeys.Assets.SettingsPagePrefab);
            var instance = UnityEngine.Object.Instantiate(go, _uiRoot.transform);
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
        public async UniTask DisplayYarnStory(string nodeName, YarnProject yarnProject)
        {
            var go = await GetPreloadedPrefab(AddressableKeys.Assets.StoryDialogueSystemPrefab);
            var instance = Object.Instantiate(go, _uiRoot.transform);
            var dialogueRunner = instance.GetComponent<DialogueRunner>();
            
            dialogueRunner.SetProject(yarnProject);

            var lineProvider = instance.GetComponent<BuiltinLocalisedLineProvider>();
            lineProvider.LocaleCode = LanguageManager.CurrentLanguage switch
            {
                GameLanguageType.Chinese => "zh-Hans",
                GameLanguageType.English => "en",
                GameLanguageType.Japanese => "ja",
                _ => "en"
            };
            lineProvider.AssetLocaleCode = lineProvider.LocaleCode;
            
            await dialogueRunner.StartDialogue(nodeName);
        }

        public void Dispose()
        {
            foreach (var handle in _uiHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _uiHandles.Clear();
            Debug.Log("[UIManager] 资源句柄已全部释放");
        }
    }
}