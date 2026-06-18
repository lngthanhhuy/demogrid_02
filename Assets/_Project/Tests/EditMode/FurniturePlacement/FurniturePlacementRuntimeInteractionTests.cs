using System.Collections.Generic;
using NUnit.Framework;
using SenCity.Features.FurniturePlacement;
using UnityEditor;
using UnityEngine;

namespace SenCity.Tests.FurniturePlacement
{
    public sealed class FurniturePlacementRuntimeInteractionTests
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
        public void BeginPlaceNewWithZeroQuantityFailsWithoutStartingSession()
        {
            FurnitureItemDefinition chair = factory.CreateItem("chair", quantity: 0);
            FurnitureInventoryRuntime inventory = factory.AddComponent<FurnitureInventoryRuntime>();
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            FurniturePlacementRuntime runtime = factory.AddComponent<FurniturePlacementRuntime>();
            var toasts = new List<string>();

            ConfigureInventory(inventory, chair);
            ConfigureRuntime(runtime, inventory, controller);
            runtime.ToastRequested += toasts.Add;

            Assert.That(runtime.CanBeginPlaceNew(chair), Is.False);
            Assert.That(runtime.BeginPlaceNew(chair, Vector2Int.zero), Is.False);

            Assert.That(runtime.HasActiveSession, Is.False);
            Assert.That(toasts, Has.Count.EqualTo(1));
            Assert.That(toasts[0], Is.Not.Empty);
        }

        private static void ConfigureInventory(FurnitureInventoryRuntime inventory, FurnitureItemDefinition item)
        {
            SerializedObject serialized = new SerializedObject(inventory);
            SerializedProperty catalog = serialized.FindProperty("catalog");
            catalog.arraySize = 1;
            catalog.GetArrayElementAtIndex(0).objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            inventory.Rebuild();
        }

        private static void ConfigureRuntime(
            FurniturePlacementRuntime runtime,
            FurnitureInventoryRuntime inventory,
            FurniturePlacementController controller)
        {
            SerializedObject serialized = new SerializedObject(runtime);
            serialized.FindProperty("controller").objectReferenceValue = controller;
            serialized.FindProperty("inventory").objectReferenceValue = inventory;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
