using UnityEngine;
using Sirenix.OdinInspector;
using VContainer;
using Architecture;
using Architecture.Language;
using Architecture.GameSound;
using UI;
using GamePlay;

namespace Test
{
    /// <summary>
    /// VContainer 运行时依赖调试器
    /// 挂载在场景中，由 LifetimeScope 自动注入
    /// </summary>
    public class VContainerDebugger : SerializedMonoBehaviour
    {
        [TabGroup("Debugger", "UI System")]
        [Inject, ShowInInspector]
        public UIManager UIManager { get; private set; }

        [TabGroup("Debugger", "UI System")]
        [Inject, ShowInInspector]
        public UIRoot UIRoot { get; private set; }

        [TabGroup("Debugger", "Core Systems")]
        [Inject, ShowInInspector]
        public EventBus EventBus { get; private set; }

        [TabGroup("Debugger", "Core Systems")]
        [Inject, ShowInInspector]
        public GameFlowController GameFlowController { get; private set; }

        [TabGroup("Debugger", "Core Systems")]
        [Inject, ShowInInspector]
        public SaveManager SaveManager { get; private set; }

        [TabGroup("Debugger", "Audio & Language")]
        [Inject, ShowInInspector]
        public IAudioService AudioService { get; private set; }

        [TabGroup("Debugger", "Audio & Language")]
        [Inject, ShowInInspector]
        public AudioCatalog AudioCatalog { get; private set; }

        [TabGroup("Debugger", "Audio & Language")]
        [Inject, ShowInInspector]
        public LanguageManager LanguageManager { get; private set; }

        [TabGroup("Debugger", "GamePlay")]
        [Inject, ShowInInspector]
        public GamePlayManager GamePlayManager { get; private set; }

        [TabGroup("Debugger", "GamePlay")]
        [Inject, ShowInInspector]
        public GamePlayRoot GamePlayRoot { get; private set; }

        [Title("Debug Actions")]
        [Button(ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        private void ForceReinject()
        {
            // 如果你在运行时动态创建了该组件，可以手动触发重新注入
            if (ScopeRef.LifetimeScope != null)
            {
                ScopeRef.LifetimeScope.Container.Inject(this);
                Debug.Log("[VContainerDebugger] 手动触发依赖注入完成");
            }
        }
    }
}

