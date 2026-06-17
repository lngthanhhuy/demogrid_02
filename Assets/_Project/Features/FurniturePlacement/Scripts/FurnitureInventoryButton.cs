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
            RefreshLabel();
        }

        public void Configure(FurniturePlacementRuntime runtime, FurnitureItemDefinition item, Text label)
        {
            this.runtime = runtime;
            this.item = item;
            this.label = label;
            RefreshLabel();
        }

        public void BeginPlacement()
        {
            runtime?.BeginPlaceNew(item, Vector2Int.zero);
        }

        private void RefreshLabel()
        {
            if (label != null && item != null)
                label.text = item.DisplayName;
        }
    }
}
