using System.Collections.Generic;
using Architecture;
using GamePlay.Events;

namespace GamePlay
{
    /// <summary>
    /// 全局单例，管理游戏玩法相关的数据逻辑,进入游戏之前必须初始化
    /// </summary>
    public class GamePlayManager
    {
        private readonly EventBus _eventBus;

        public GamePlayManager(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// 重置游戏玩法数据
        /// </summary>
        public void ResetGamePlay()
        {
            // 发布重置页面列表事件
            _eventBus.Publish(new SetPageListEvent(new List<DocumentPageData>()));
        }
    }
}