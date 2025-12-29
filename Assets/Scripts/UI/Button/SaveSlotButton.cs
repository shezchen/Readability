using TMPro;
using UI.Page;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace UI.Button
{
    public class SaveSlotButton : MonoBehaviour,ISubmitHandler,IPointerClickHandler
    {
        [SerializeField] private StartGamePage startGamePage;
        [SerializeField] private int slotIndex;
        
        [SerializeField] private LocalizeStringEvent slotLabel;
        [SerializeField] private LocalizedString slotEmptyLabel;
        [SerializeField] private LocalizedString slotOccupiedLabel0;
        [SerializeField] private LocalizedString slotOccupiedLabel1;
        [SerializeField] private LocalizedString slotOccupiedLabel2;
        
        public void OnSubmit(BaseEventData eventData)
        {
            startGamePage.OnSaveSlotSubmitted(slotIndex);
        }
        
        /// <summary>
        /// 设置存档槽显示
        /// </summary>
        /// <param name="isEmpty">是否为空</param>
        public void SetSlotLabel(bool isEmpty)
        {
            if (isEmpty)
            {
                slotLabel.StringReference = slotEmptyLabel;
                slotLabel.RefreshString();
            }
            else
            {
                switch (slotIndex)
                {
                    case 0:
                        slotLabel.StringReference = slotOccupiedLabel0;
                        slotLabel.RefreshString();
                        break;
                    case 1:
                        slotLabel.StringReference = slotOccupiedLabel1;
                        slotLabel.RefreshString();
                        break;
                    case 2:
                        slotLabel.StringReference = slotOccupiedLabel2;
                        slotLabel.RefreshString();
                        break;
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSubmit(eventData);
        }
    }
}