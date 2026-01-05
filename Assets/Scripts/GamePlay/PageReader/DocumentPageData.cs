using Sirenix.OdinInspector;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// 文档单页数据
    /// </summary>
    [CreateAssetMenu(menuName = "GamePlay/PageReader/Document Page Data", fileName = "DocumentPageData")]
    public class DocumentPageData : ScriptableObject
    {
        [Title("Page Content")]
        [SerializeField, MultiLineProperty(10)]
        private string _text = "";

        /// <summary>
        /// 页面文本内容
        /// </summary>
        public string Text => _text;
    }
}

