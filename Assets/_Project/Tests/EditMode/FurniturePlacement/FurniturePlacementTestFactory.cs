using System.Collections.Generic;
using SenCity.Core.Grid;
using SenCity.Features.FurniturePlacement;
using UnityEditor;
using UnityEngine;

namespace SenCity.Tests.FurniturePlacement
{
    internal sealed class FurniturePlacementTestFactory
    {
        private readonly List<Object> createdObjects = new List<Object>();

        public SenCityGridProfile CreateGridProfile(int columns = 6, int rows = 6, float cellSize = 0.5f)
        {
            var profile = ScriptableObject.CreateInstance<SenCityGridProfile>();
            createdObjects.Add(profile);
            profile.columns = columns;
            profile.rows = rows;
            profile.cellSize = cellSize;
            profile.origin = Vector3.zero;
            return profile;
        }

        public FurnitureItemDefinition CreateItem(
            string itemId,
            int width = 1,
            int depth = 1,
            int quantity = 1,
            bool canStore = true,
            bool lockedBySystem = false)
        {
            var item = ScriptableObject.CreateInstance<FurnitureItemDefinition>();
            createdObjects.Add(item);
            item.name = itemId;

            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("itemId").stringValue = itemId;
            serialized.FindProperty("displayName").stringValue = itemId;
            serialized.FindProperty("category").enumValueIndex = (int)FurnitureCategory.Decoration;
            SerializedProperty footprint = serialized.FindProperty("footprint");
            footprint.FindPropertyRelative("width").intValue = width;
            footprint.FindPropertyRelative("depth").intValue = depth;
            serialized.FindProperty("startingQuantity").intValue = quantity;
            serialized.FindProperty("canStore").boolValue = canStore;
            serialized.FindProperty("lockedBySystem").boolValue = lockedBySystem;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        public FurnitureCatalogDefinition CreateCatalog(params FurnitureItemDefinition[] items)
        {
            var catalog = ScriptableObject.CreateInstance<FurnitureCatalogDefinition>();
            createdObjects.Add(catalog);

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty itemList = serialized.FindProperty("items");
            itemList.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                itemList.GetArrayElementAtIndex(i).objectReferenceValue = items[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        public T AddComponent<T>() where T : Component
        {
            var gameObject = new GameObject(typeof(T).Name);
            createdObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        public void DestroyCreatedObjects()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                    Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }
    }
}
