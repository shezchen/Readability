using UnityEngine;

namespace GamePlay
{
    public class GamePlayRoot : MonoBehaviour
    {
        public void ClearGamePlayRoot()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
