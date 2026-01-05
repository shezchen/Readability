using System.Collections.Generic;

namespace GamePlay.Events
{
    /// <summary>
    /// 重置页面列表事件
    /// </summary>
    public record SetPageListEvent(List<DocumentPageData> PageDataList);
}

