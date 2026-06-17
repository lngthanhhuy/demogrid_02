using System.Collections.Generic;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    [CreateAssetMenu(menuName = "Sen City/Furniture Catalog")]
    public class FurnitureCatalogDefinition : ScriptableObject
    {
        [SerializeField] private List<FurnitureItemDefinition> items = new List<FurnitureItemDefinition>();

        public IReadOnlyList<FurnitureItemDefinition> Items => items;

        public bool TryGetItem(string itemId, out FurnitureItemDefinition item)
        {
            item = null;
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            foreach (FurnitureItemDefinition candidate in items)
            {
                if (candidate != null && candidate.ItemId == itemId)
                {
                    item = candidate;
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            var seenIds = new HashSet<string>();
            for (int i = items.Count - 1; i >= 0; i--)
            {
                FurnitureItemDefinition item = items[i];
                if (item == null)
                {
                    items.RemoveAt(i);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(item.ItemId) && !seenIds.Add(item.ItemId))
                    Debug.LogWarning($"Furniture catalog '{name}' contains duplicate item id '{item.ItemId}'.", this);
            }
        }
    }
}
