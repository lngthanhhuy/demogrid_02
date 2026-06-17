using UnityEngine;
using UnityEngine.UI;

namespace SenCity.Features.FurniturePlacement
{
    [RequireComponent(typeof(Button))]
    public class FurnitureInventoryButton : MonoBehaviour
    {
        [SerializeField] private FurniturePlacementRuntime runtime;
        [SerializeField] private FurnitureItemDefinition item;
        [SerializeField] private Text label;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(BeginPlacement);
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(FurniturePlacementRuntime runtime, FurnitureItemDefinition item, Text label)
        {
            Unsubscribe();
            this.runtime = runtime;
            this.item = item;
            this.label = label;
            Subscribe();
            Refresh();
        }

        public void BeginPlacement()
        {
            runtime?.BeginPlaceNew(item, Vector2Int.zero);
        }

        private void Refresh()
        {
            if (button == null)
                button = GetComponent<Button>();

            int quantity = runtime != null && item != null ? runtime.GetInventoryQuantity(item) : 0;
            if (label != null && item != null)
                label.text = $"{item.DisplayName} x{quantity}";

            if (button != null)
                button.interactable = runtime != null && runtime.CanBeginPlaceNew(item);
        }

        private void Subscribe()
        {
            if (runtime == null)
                return;

            runtime.InventoryChanged -= Refresh;
            runtime.InventoryChanged += Refresh;
            runtime.SessionChanged -= HandleSessionChanged;
            runtime.SessionChanged += HandleSessionChanged;
        }

        private void Unsubscribe()
        {
            if (runtime == null)
                return;

            runtime.InventoryChanged -= Refresh;
            runtime.SessionChanged -= HandleSessionChanged;
        }

        private void HandleSessionChanged(PlacementSession session)
        {
            Refresh();
        }
    }
}
