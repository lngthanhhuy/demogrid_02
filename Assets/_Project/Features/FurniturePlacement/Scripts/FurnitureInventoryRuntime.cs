using System.Collections.Generic;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public class FurnitureInventoryRuntime : MonoBehaviour
    {
        [SerializeField] private List<FurnitureItemDefinition> catalog = new List<FurnitureItemDefinition>();

        private readonly Dictionary<string, FurnitureItemDefinition> itemsById = new Dictionary<string, FurnitureItemDefinition>();
        private readonly Dictionary<string, int> quantitiesByItemId = new Dictionary<string, int>();

        public IReadOnlyList<FurnitureItemDefinition> Catalog => catalog;

        private void Awake()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            itemsById.Clear();
            quantitiesByItemId.Clear();

            foreach (FurnitureItemDefinition item in catalog)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                    continue;

                itemsById[item.ItemId] = item;
                quantitiesByItemId[item.ItemId] = Mathf.Max(0, item.StartingQuantity);
            }
        }

        public FurnitureItemDefinition GetItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            itemsById.TryGetValue(itemId, out FurnitureItemDefinition item);
            return item;
        }

        public int GetQuantity(FurnitureItemDefinition item)
        {
            return item == null ? 0 : GetQuantity(item.ItemId);
        }

        public int GetQuantity(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && quantitiesByItemId.TryGetValue(itemId, out int quantity)
                ? quantity
                : 0;
        }

        public bool TryConsume(FurnitureItemDefinition item)
        {
            if (item == null)
                return false;

            int quantity = GetQuantity(item.ItemId);
            if (quantity <= 0)
                return false;

            quantitiesByItemId[item.ItemId] = quantity - 1;
            return true;
        }

        public void ReturnItem(FurnitureItemDefinition item)
        {
            if (item == null)
                return;

            quantitiesByItemId[item.ItemId] = GetQuantity(item.ItemId) + 1;
        }

        public FurnitureInventorySnapshot CaptureSnapshot()
        {
            var entries = new List<FurnitureInventoryEntry>();
            foreach (KeyValuePair<string, int> quantity in quantitiesByItemId)
                entries.Add(new FurnitureInventoryEntry(quantity.Key, quantity.Value));

            return new FurnitureInventorySnapshot(entries);
        }

        public void ApplySnapshot(FurnitureInventorySnapshot snapshot)
        {
            if (snapshot == null)
                return;

            foreach (FurnitureInventoryEntry entry in snapshot.items)
            {
                if (!string.IsNullOrWhiteSpace(entry.itemId))
                    quantitiesByItemId[entry.itemId] = Mathf.Max(0, entry.quantity);
            }
        }
    }

    [System.Serializable]
    public class FurnitureInventorySnapshot
    {
        public List<FurnitureInventoryEntry> items = new List<FurnitureInventoryEntry>();

        public FurnitureInventorySnapshot()
        {
        }

        public FurnitureInventorySnapshot(List<FurnitureInventoryEntry> items)
        {
            this.items = items ?? new List<FurnitureInventoryEntry>();
        }
    }

    [System.Serializable]
    public class FurnitureInventoryEntry
    {
        public string itemId;
        public int quantity;

        public FurnitureInventoryEntry()
        {
        }

        public FurnitureInventoryEntry(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }
}
