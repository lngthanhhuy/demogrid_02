using SenCity.Core.Grid;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    [CreateAssetMenu(menuName = "Sen City/Furniture Item")]
    public class FurnitureItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private FurnitureCategory category;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite icon;
        [SerializeField] private GridFootprint footprint = new GridFootprint(1, 1);
        [SerializeField, Min(0)] private int startingQuantity = 1;
        [SerializeField] private bool canStore = true;
        [SerializeField] private bool lockedBySystem;

        public string ItemId => itemId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public FurnitureCategory Category => category;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;
        public GridFootprint Footprint => footprint;
        public int StartingQuantity => startingQuantity;
        public bool CanStore => canStore && !lockedBySystem;
        public bool LockedBySystem => lockedBySystem;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = name;

            footprint = new GridFootprint(footprint.Width, footprint.Depth);
        }
    }
}
