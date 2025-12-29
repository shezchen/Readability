using System;
using Architecture;
using DG.Tweening;
using R3;
using Tools;
using UnityEngine;
using VContainer;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class BlackScreenRoot : MonoBehaviour
    {
        [SerializeField] private float _fadeDuration = 0.5f;
        [Inject] private EventBus _eventBus; 
        private CanvasGroup _canvasGroup;
        private Tween _currentTween;

        private void Awake()
        {
            ScopeRef.LifetimeScope.Container.Inject(this);
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _eventBus.Receive<BlackScreenEvent>().Subscribe(OnBlackScreenEvent).AddTo(this);
            foreach (Transform t in transform)
            {
                t.gameObject.SetActive(true);
            }
        }
        
        private void OnBlackScreenEvent(BlackScreenEvent evt)
        {
            if (evt.IsBlack)
            {
                _currentTween?.Kill();
                _currentTween = _canvasGroup.FadeIn(_fadeDuration, true, true);
            }
            else
            {
                _currentTween?.Kill();
                _currentTween = _canvasGroup.FadeOut(_fadeDuration, false, false);
            }
        }
    }
}