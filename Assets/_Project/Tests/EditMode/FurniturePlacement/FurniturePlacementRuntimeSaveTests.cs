using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SenCity.Core.Grid;
using SenCity.Core.Save;
using SenCity.Features.FurniturePlacement;
using UnityEditor;
using UnityEngine;

namespace SenCity.Tests.FurniturePlacement
{
    public sealed class FurniturePlacementRuntimeSaveTests
    {
        private FurniturePlacementTestFactory factory;
        private string savePath;

        [SetUp]
        public void SetUp()
        {
            factory = new FurniturePlacementTestFactory();
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(savePath) && File.Exists(savePath))
                File.Delete(savePath);

            factory.DestroyCreatedObjects();
        }

        [Test]
        public void PlacementCommitAutoSavesRoomLayoutAndInventory()
        {
            FurnitureItemDefinition chair = factory.CreateItem("chair", quantity: 2);
            FurnitureInventoryRuntime inventory = factory.AddComponent<FurnitureInventoryRuntime>();
            FurniturePlacementSaveService saveService = factory.AddComponent<FurniturePlacementSaveService>();
            FurniturePlacementRuntime runtime = factory.AddComponent<FurniturePlacementRuntime>();

            ConfigureInventory(inventory, chair);
            ConfigureSaveService(saveService, "sen_city_runtime_auto_save_test.json");
            ConfigureRuntime(runtime, inventory, saveService);
            savePath = saveService.SavePath;

            var instance = new FurnitureInstanceData("chair-1", chair.ItemId, Vector2Int.zero, chair.Footprint);
            InvokePrivate(runtime, "HandleFurniturePlaced", instance);

            Assert.That(File.Exists(savePath), Is.True);
            Assert.That(saveService.TryLoad(out FurniturePlacementSavePayload payload), Is.True);
            Assert.That(payload.roomLayout.instances, Has.Count.EqualTo(1));
            Assert.That(payload.roomLayout.instances[0].itemId, Is.EqualTo("chair"));
            Assert.That(payload.inventory.items, Has.Count.EqualTo(1));
            Assert.That(payload.inventory.items[0].quantity, Is.EqualTo(1));
        }

        [Test]
        public void AutoSaveFailureShowsFailureToastWithoutSuccessToast()
        {
            FurnitureItemDefinition chair = factory.CreateItem("chair", quantity: 2);
            FurnitureInventoryRuntime inventory = factory.AddComponent<FurnitureInventoryRuntime>();
            FailingSaveService saveService = factory.AddComponent<FailingSaveService>();
            FurniturePlacementRuntime runtime = factory.AddComponent<FurniturePlacementRuntime>();
            var toasts = new List<string>();

            ConfigureInventory(inventory, chair);
            ConfigureRuntime(runtime, inventory, saveService);
            runtime.ToastRequested += toasts.Add;

            var instance = new FurnitureInstanceData("chair-1", chair.ItemId, Vector2Int.zero, chair.Footprint);
            InvokePrivate(runtime, "HandleFurniturePlaced", instance);

            Assert.That(toasts, Is.EqualTo(new[] { "Unable to save room layout." }));
            Assert.That(runtime.CaptureRoomSnapshot().instances, Is.Empty);
            Assert.That(inventory.GetQuantity(chair), Is.EqualTo(2));
        }

        [Test]
        public void StoreAutoSaveFailureRestoresRoomLayoutAndInventory()
        {
            SenCityGridProfile gridProfile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition chair = factory.CreateItem("chair", quantity: 0);
            FurnitureInventoryRuntime inventory = factory.AddComponent<FurnitureInventoryRuntime>();
            FailingSaveService saveService = factory.AddComponent<FailingSaveService>();
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            FurniturePlacementRuntime runtime = factory.AddComponent<FurniturePlacementRuntime>();
            var toasts = new List<string>();

            ConfigureInventory(inventory, chair);
            controller.Configure(gridProfile);
            ConfigureRuntime(runtime, inventory, saveService, controller, gridProfile);
            InvokePrivate(runtime, "Awake");
            runtime.RestoreRoomSnapshot(new FurnitureRoomLayoutSnapshot(new List<FurnitureInstanceSaveData>
            {
                new FurnitureInstanceSaveData
                {
                    instanceId = "chair-1",
                    itemId = chair.ItemId,
                    cellX = 0,
                    cellY = 0,
                    rotationDegrees = 0,
                    footprintWidth = chair.Footprint.Width,
                    footprintDepth = chair.Footprint.Depth,
                    state = FurniturePlacementState.Placed
                }
            }));
            PlacedFurnitureObject placedObject = Object.FindAnyObjectByType<PlacedFurnitureObject>();
            Assert.That(placedObject, Is.Not.Null);
            runtime.SelectObject(placedObject);
            runtime.ToastRequested += toasts.Add;

            Assert.That(runtime.RequestStoreSelected(), Is.True);
            Assert.That(runtime.ConfirmStoreSelected(), Is.True);

            FurnitureRoomLayoutSnapshot rollbackSnapshot = runtime.CaptureRoomSnapshot();
            Assert.That(toasts, Is.EqualTo(new[] { "Unable to save room layout." }));
            Assert.That(rollbackSnapshot.instances, Has.Count.EqualTo(1));
            Assert.That(rollbackSnapshot.instances[0].instanceId, Is.EqualTo("chair-1"));
            Assert.That(rollbackSnapshot.instances[0].state, Is.EqualTo(FurniturePlacementState.Placed));
            Assert.That(inventory.GetQuantity(chair), Is.Zero);
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

        private static void ConfigureSaveService(FurniturePlacementSaveService saveService, string fileName)
        {
            SerializedObject serialized = new SerializedObject(saveService);
            serialized.FindProperty("fileName").stringValue = fileName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRuntime(
            FurniturePlacementRuntime runtime,
            FurnitureInventoryRuntime inventory,
            FurniturePlacementSaveService saveService,
            FurniturePlacementController controller = null,
            SenCityGridProfile gridProfile = null)
        {
            SerializedObject serialized = new SerializedObject(runtime);
            serialized.FindProperty("controller").objectReferenceValue = controller;
            serialized.FindProperty("inventory").objectReferenceValue = inventory;
            serialized.FindProperty("saveService").objectReferenceValue = saveService;
            serialized.FindProperty("gridProfile").objectReferenceValue = gridProfile;
            serialized.FindProperty("autoSaveAfterCommit").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokePrivate(FurniturePlacementRuntime runtime, string methodName, FurnitureInstanceData instance)
        {
            MethodInfo method = typeof(FurniturePlacementRuntime).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(runtime, new object[] { instance });
        }

        private static void InvokePrivate(FurniturePlacementRuntime runtime, string methodName)
        {
            MethodInfo method = typeof(FurniturePlacementRuntime).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(runtime, null);
        }

        private sealed class FailingSaveService : FurniturePlacementSaveService
        {
            public override bool Save(FurnitureRoomLayoutSnapshot roomLayout, FurnitureInventorySnapshot inventory)
            {
                return false;
            }
        }
    }
}
