using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UI.Page
{
    public abstract class BasePage:MonoBehaviour
    {
        public abstract UniTask Display();
        public abstract UniTask Hide();
    }
}