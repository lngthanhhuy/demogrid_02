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
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Text selectedItemNameText;

        private void Awake()
        {
            if (runtime == null)
                runtime = FindAnyObjectByType<FurniturePlacementRuntime>();

            if (rotateButton != null)
                rotateButton.onClick.AddListener(() => runtime?.RotatePreview());

            if (confirmButton != null)
                confirmButton.onClick.AddListener(() => runtime?.Confirm());

            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => runtime?.Cancel());

            if (storeButton != null)
                storeButton.onClick.AddListener(RequestStoreSelected);
        }

        private void OnEnable()
        {
            if (runtime == null)
                return;

            runtime.SelectedObjectChanged += HandleSelectedObjectChanged;
            runtime.ToastRequested += HandleToastRequested;
        }

        private void OnDisable()
        {
            if (runtime == null)
                return;

            runtime.SelectedObjectChanged -= HandleSelectedObjectChanged;
            runtime.ToastRequested -= HandleToastRequested;
        }

        public void BeginPlaceNew(FurnitureItemDefinition item)
        {
            runtime?.BeginPlaceNew(item, Vector2Int.zero);
        }

        private void RequestStoreSelected()
        {
            PlacedFurnitureObject selected = runtime != null ? runtime.SelectedObject : null;
            if (selected == null)
                return;

            string itemName = selected.Item != null ? selected.Item.DisplayName : "Vật phẩm";
            if (confirmDialog != null)
            {
                confirmDialog.Show($"Cất {itemName} vào Kho?", () => runtime.StoreSelected());
                return;
            }

            runtime.StoreSelected();
        }

        private void HandleSelectedObjectChanged(PlacedFurnitureObject placedObject)
        {
            if (selectedItemNameText != null)
                selectedItemNameText.text = placedObject != null && placedObject.Item != null ? placedObject.Item.DisplayName : string.Empty;
        }

        private void HandleToastRequested(string message)
        {
            if (toastPresenter != null)
                toastPresenter.Show(message);
        }
    }
}
