using SenCity.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SenCity.Features.FurniturePlacement
{
    public class FurniturePlacementHud : MonoBehaviour
    {
        [SerializeField] private FurniturePlacementRuntime runtime;
        [SerializeField] private SenCityToastPresenter toastPresenter;
        [SerializeField] private SenCityConfirmDialog confirmDialog;
        [SerializeField] private Button moveButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Text selectedItemNameText;

        private void Awake()
        {
            if (runtime == null)
                runtime = FindAnyObjectByType<FurniturePlacementRuntime>();

            if (moveButton != null)
                moveButton.onClick.AddListener(() => runtime?.BeginMoveSelected());

            if (rotateButton != null)
                rotateButton.onClick.AddListener(() => runtime?.RotatePreview());

            if (confirmButton != null)
                confirmButton.onClick.AddListener(() => runtime?.Confirm());

            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => runtime?.Cancel());

            if (storeButton != null)
                storeButton.onClick.AddListener(() => runtime?.RequestStoreSelected());

            if (saveButton != null)
                saveButton.onClick.AddListener(() => runtime?.SaveCurrentLayout());

            if (loadButton != null)
                loadButton.onClick.AddListener(() => runtime?.LoadSavedLayout());

            RefreshButtonStates();
        }

        private void OnEnable()
        {
            if (runtime == null)
                return;

            runtime.SelectedObjectChanged += HandleSelectedObjectChanged;
            runtime.SessionChanged += HandleSessionChanged;
            runtime.StoreConfirmationRequested += HandleStoreConfirmationRequested;
            runtime.ToastRequested += HandleToastRequested;
            RefreshButtonStates();
        }

        private void OnDisable()
        {
            if (runtime == null)
                return;

            runtime.SelectedObjectChanged -= HandleSelectedObjectChanged;
            runtime.SessionChanged -= HandleSessionChanged;
            runtime.StoreConfirmationRequested -= HandleStoreConfirmationRequested;
            runtime.ToastRequested -= HandleToastRequested;
        }

        public void BeginPlaceNew(FurnitureItemDefinition item)
        {
            runtime?.BeginPlaceNew(item, Vector2Int.zero);
        }

        private void HandleStoreConfirmationRequested(PlacedFurnitureObject placedObject)
        {
            if (placedObject == null)
                return;

            string itemName = placedObject.Item != null ? placedObject.Item.DisplayName : "Vật phẩm";
            if (confirmDialog != null)
            {
                confirmDialog.Show($"Cất {itemName} vào Kho?", () => runtime.ConfirmStoreSelected(), () => runtime.Cancel());
                return;
            }

            runtime.ConfirmStoreSelected();
        }

        private void HandleSelectedObjectChanged(PlacedFurnitureObject placedObject)
        {
            if (selectedItemNameText != null)
                selectedItemNameText.text = placedObject != null && placedObject.Item != null ? placedObject.Item.DisplayName : string.Empty;

            RefreshButtonStates();
        }

        private void HandleSessionChanged(PlacementSession session)
        {
            RefreshButtonStates();
        }

        private void HandleToastRequested(string message)
        {
            if (toastPresenter != null)
                toastPresenter.Show(message);
        }

        private void RefreshButtonStates()
        {
            bool hasRuntime = runtime != null;
            bool hasSession = hasRuntime && runtime.HasActiveSession;
            bool hasPreviewSession = hasRuntime &&
                                     runtime.ActiveSession != null &&
                                     runtime.ActiveSession.State != PlacementSessionState.RemoveConfirm;
            bool hasSelection = hasRuntime && runtime.SelectedObject != null;

            if (moveButton != null)
                moveButton.interactable = hasSelection && !hasSession;

            if (rotateButton != null)
                rotateButton.interactable = hasPreviewSession;

            if (confirmButton != null)
                confirmButton.interactable = hasSession;

            if (cancelButton != null)
                cancelButton.interactable = hasSession;

            if (storeButton != null)
                storeButton.interactable = hasSelection && hasRuntime && runtime.CanStoreSelected();

            if (saveButton != null)
                saveButton.interactable = hasRuntime && !hasSession;

            if (loadButton != null)
                loadButton.interactable = hasRuntime && !hasSession;
        }
    }
}
