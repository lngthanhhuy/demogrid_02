using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SenCity.Core.Grid;
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

        [Test]
        public void BeginPlaceNewWhileSessionActiveFailsWithoutReplacingSession()
        {
            SenCityGridProfile gridProfile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition chair = factory.CreateItem("chair", quantity: 1);
            FurnitureItemDefinition table = factory.CreateItem("table", quantity: 1);
            FurnitureInventoryRuntime inventory = factory.AddComponent<FurnitureInventoryRuntime>();
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            FurniturePlacementRuntime runtime = factory.AddComponent<FurniturePlacementRuntime>();
            var toasts = new List<string>();

            ConfigureInventory(inventory, chair, table);
            ConfigureRuntime(runtime, inventory, controller, gridProfile);
            InvokePrivate(runtime, "Awake");
            runtime.ToastRequested += toasts.Add;

            Assert.That(runtime.BeginPlaceNew(chair, Vector2Int.zero), Is.True);
            Assert.That(runtime.CanBeginPlaceNew(table), Is.False);
            Assert.That(runtime.BeginPlaceNew(table, new Vector2Int(1, 0)), Is.False);

            Assert.That(runtime.HasActiveSession, Is.True);
            Assert.That(runtime.ActiveSession.Item, Is.SameAs(chair));
            Assert.That(toasts, Has.Count.EqualTo(1));
            Assert.That(toasts[0], Is.Not.Empty);
        }

        private static void ConfigureInventory(FurnitureInventoryRuntime inventory, params FurnitureItemDefinition[] items)
        {
            SerializedObject serialized = new SerializedObject(inventory);
            SerializedProperty catalog = serialized.FindProperty("catalog");
            catalog.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                catalog.GetArrayElementAtIndex(i).objectReferenceValue = items[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            inventory.Rebuild();
        }

        private static void ConfigureRuntime(
            FurniturePlacementRuntime runtime,
            FurnitureInventoryRuntime inventory,
            FurniturePlacementController controller,
            SenCityGridProfile gridProfile = null)
        {
            SerializedObject serialized = new SerializedObject(runtime);
            serialized.FindProperty("controller").objectReferenceValue = controller;
            serialized.FindProperty("inventory").objectReferenceValue = inventory;
            serialized.FindProperty("gridProfile").objectReferenceValue = gridProfile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokePrivate(FurniturePlacementRuntime runtime, string methodName)
        {
            MethodInfo method = typeof(FurniturePlacementRuntime).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(runtime, null);
        }
    }
}
