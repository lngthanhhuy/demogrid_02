using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SenCity.Core.UI
{
    public class SenCityToastPresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text messageText;
        [SerializeField, Min(0.25f)] private float visibleDuration = 2f;

        private Coroutine routine;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(string message)
        {
            if (messageText != null)
                messageText.text = message;

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(ShowRoutine());
        }

        public void HideImmediate()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator ShowRoutine()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            yield return new WaitForSeconds(visibleDuration);
            HideImmediate();
            routine = null;
        }
    }
}
