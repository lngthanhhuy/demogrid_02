using System;
using UnityEngine;
using UnityEngine.UI;

namespace SenCity.Core.UI
{
    public class SenCityConfirmDialog : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action onConfirm;
        private Action onCancel;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(Confirm);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);

            Hide();
        }

        public void Show(string message, Action confirm, Action cancel = null)
        {
            onConfirm = confirm;
            onCancel = cancel;

            if (messageText != null)
                messageText.text = message;

            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void Confirm()
        {
            Action callback = onConfirm;
            ClearCallbacks();
            Hide();
            callback?.Invoke();
        }

        private void Cancel()
        {
            Action callback = onCancel;
            ClearCallbacks();
            Hide();
            callback?.Invoke();
        }

        private void ClearCallbacks()
        {
            onConfirm = null;
            onCancel = null;
        }
    }
}
