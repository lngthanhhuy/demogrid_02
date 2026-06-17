using NUnit.Framework;
using SenCity.Features.FurniturePlacement;
using UnityEditor;

namespace SenCity.Tests.FurniturePlacement
{
    public sealed class FurnitureInventoryRuntimeTests
    {
        private FurniturePlacementTestFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new FurniturePlacementTestFactory();
        }

        [TearDown]
        public void TearDown()
        {
            factory.DestroyCreatedObjects();
        }

        [Test]
        public void RebuildLoadsQuantitiesFromCatalogAsset()
        {
            FurnitureItemDefinition chair = factory.CreateItem("chair", quantity: 2);
            FurnitureItemDefinition table = factory.CreateItem("table", quantity: 1);
            FurnitureCatalogDefinition catalog = factory.CreateCatalog(chair, table);
            FurnitureInventoryRuntime inventory = factory.AddComponent<FurnitureInventoryRuntime>();

            SerializedObject serialized = new SerializedObject(inventory);
            serialized.FindProperty("catalogAsset").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            inventory.Rebuild();

            Assert.That(inventory.GetItem("chair"), Is.SameAs(chair));
            Assert.That(inventory.GetQuantity(chair), Is.EqualTo(2));
            Assert.That(inventory.GetQuantity(table), Is.EqualTo(1));
        }

        [Test]
        public void ConsumeAndReturnUpdateInventoryQuantity()
        {
            FurnitureItemDefinition chair = factory.CreateItem("chair", quantity: 1);
            FurnitureInventoryRuntime inventory = factory.AddComponent<FurnitureInventoryRuntime>();

            SerializedObject serialized = new SerializedObject(inventory);
            SerializedProperty catalog = serialized.FindProperty("catalog");
            catalog.arraySize = 1;
            catalog.GetArrayElementAtIndex(0).objectReferenceValue = chair;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            inventory.Rebuild();

            Assert.That(inventory.TryConsume(chair), Is.True);
            Assert.That(inventory.GetQuantity(chair), Is.Zero);
            Assert.That(inventory.TryConsume(chair), Is.False);

            inventory.ReturnItem(chair);

            Assert.That(inventory.GetQuantity(chair), Is.EqualTo(1));
        }

        [Test]
        public void SnapshotRoundTripPreservesKnownQuantities()
        {
            FurnitureItemDefinition chair = factory.CreateItem("chair", quantity: 3);
            FurnitureInventoryRuntime inventory = factory.AddComponent<FurnitureInventoryRuntime>();
            SerializedObject serialized = new SerializedObject(inventory);
            SerializedProperty catalog = serialized.FindProperty("catalog");
            catalog.arraySize = 1;
            catalog.GetArrayElementAtIndex(0).objectReferenceValue = chair;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            inventory.Rebuild();
            inventory.TryConsume(chair);

            FurnitureInventorySnapshot snapshot = inventory.CaptureSnapshot();
            inventory.TryConsume(chair);
            inventory.ApplySnapshot(snapshot);

            Assert.That(inventory.GetQuantity(chair), Is.EqualTo(2));
        }
    }
}
