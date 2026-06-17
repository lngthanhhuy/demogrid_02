using NUnit.Framework;
using SenCity.Core.Grid;
using SenCity.Features.FurniturePlacement;
using UnityEngine;

namespace SenCity.Tests.FurniturePlacement
{
    public sealed class FurniturePlacementControllerTests
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
        public void ConfirmNewPlacementRaisesPlacedEventAndOccupiesCells()
        {
            SenCityGridProfile profile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition item = factory.CreateItem("chair", width: 2, depth: 1);
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            FurnitureInstanceData placed = null;
            controller.Configure(profile);
            controller.FurniturePlaced += instance => placed = instance;

            Assert.That(controller.TryBeginPlaceNew(item, Vector2Int.zero), Is.True);
            Assert.That(controller.ActiveSession.LastValidation.IsValid, Is.True);
            Assert.That(controller.ConfirmActiveSession(), Is.True);

            Assert.That(placed, Is.Not.Null);
            Assert.That(placed.ItemId, Is.EqualTo("chair"));
            Assert.That(placed.OriginCell, Is.EqualTo(Vector2Int.zero));
            Assert.That(controller.State, Is.EqualTo(PlacementSessionState.Idle));
        }

        [Test]
        public void OverlappingNewPlacementFailsAfterFirstItemIsConfirmed()
        {
            SenCityGridProfile profile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition item = factory.CreateItem("chair", width: 2, depth: 1);
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            string failureMessage = null;
            controller.Configure(profile);
            controller.PlacementFailed += message => failureMessage = message;

            controller.TryBeginPlaceNew(item, Vector2Int.zero);
            controller.ConfirmActiveSession();

            Assert.That(controller.TryBeginPlaceNew(item, Vector2Int.zero), Is.True);
            Assert.That(controller.ActiveSession.LastValidation.Failure, Is.EqualTo(PlacementValidationFailure.Overlap));
            Assert.That(controller.ConfirmActiveSession(), Is.False);
            Assert.That(failureMessage, Is.EqualTo("Cell already has furniture."));
        }

        [Test]
        public void MoveExistingIgnoresOwnCellsAndUpdatesInstance()
        {
            SenCityGridProfile profile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition item = factory.CreateItem("chair", width: 2, depth: 1);
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            FurnitureInstanceData placed = null;
            controller.Configure(profile);
            controller.FurniturePlaced += instance => placed = instance;

            controller.TryBeginPlaceNew(item, Vector2Int.zero);
            controller.ConfirmActiveSession();

            Assert.That(controller.TryBeginMoveExisting(placed, item), Is.True);
            Assert.That(controller.ActiveSession.LastValidation.IsValid, Is.True);
            controller.MovePreview(new Vector2Int(1, 0));
            Assert.That(controller.ConfirmActiveSession(), Is.True);

            Assert.That(placed.OriginCell, Is.EqualTo(new Vector2Int(1, 0)));
        }

        [Test]
        public void ClearRegisteredFurnitureAllowsRestoringIntoPreviouslyOccupiedCells()
        {
            SenCityGridProfile profile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition item = factory.CreateItem("chair", width: 2, depth: 1);
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            controller.Configure(profile);

            var instance = new FurnitureInstanceData("chair-1", item.ItemId, Vector2Int.zero, item.Footprint);
            Assert.That(controller.RegisterPlacedFurniture(instance), Is.True);
            controller.ClearRegisteredFurniture();

            Assert.That(controller.TryBeginPlaceNew(item, Vector2Int.zero), Is.True);
            Assert.That(controller.ActiveSession.LastValidation.IsValid, Is.True);
        }

        [Test]
        public void StoreRequestEntersRemoveConfirmWithoutChangingFurniture()
        {
            SenCityGridProfile profile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition item = factory.CreateItem("chair", width: 2, depth: 1);
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            controller.Configure(profile);

            var instance = new FurnitureInstanceData("chair-1", item.ItemId, Vector2Int.zero, item.Footprint);
            Assert.That(controller.RegisterPlacedFurniture(instance), Is.True);

            Assert.That(controller.TryBeginStore(instance, item), Is.True);

            Assert.That(controller.State, Is.EqualTo(PlacementSessionState.RemoveConfirm));
            Assert.That(instance.State, Is.EqualTo(FurniturePlacementState.Placed));
        }

        [Test]
        public void CancelStoreConfirmationKeepsFurnitureReserved()
        {
            SenCityGridProfile profile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition item = factory.CreateItem("chair", width: 2, depth: 1);
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            controller.Configure(profile);

            var instance = new FurnitureInstanceData("chair-1", item.ItemId, Vector2Int.zero, item.Footprint);
            controller.RegisterPlacedFurniture(instance);
            controller.TryBeginStore(instance, item);

            controller.CancelActiveSession();

            Assert.That(instance.State, Is.EqualTo(FurniturePlacementState.Placed));
            Assert.That(controller.TryBeginPlaceNew(item, Vector2Int.zero), Is.True);
            Assert.That(controller.ActiveSession.LastValidation.Failure, Is.EqualTo(PlacementValidationFailure.Overlap));
        }

        [Test]
        public void ConfirmStoreReleasesCellsAndSetsStoredState()
        {
            SenCityGridProfile profile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition item = factory.CreateItem("chair", width: 2, depth: 1);
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            FurnitureInstanceData stored = null;
            controller.Configure(profile);
            controller.FurnitureStored += instance => stored = instance;

            var instance = new FurnitureInstanceData("chair-1", item.ItemId, Vector2Int.zero, item.Footprint);
            controller.RegisterPlacedFurniture(instance);
            controller.TryBeginStore(instance, item);

            Assert.That(controller.ConfirmStoreActiveSession(), Is.True);

            Assert.That(stored, Is.SameAs(instance));
            Assert.That(instance.State, Is.EqualTo(FurniturePlacementState.Stored));
            Assert.That(controller.State, Is.EqualTo(PlacementSessionState.Idle));
            Assert.That(controller.TryBeginPlaceNew(item, Vector2Int.zero), Is.True);
            Assert.That(controller.ActiveSession.LastValidation.IsValid, Is.True);
        }

        [Test]
        public void LockedFurnitureCannotEnterStoreConfirmation()
        {
            SenCityGridProfile profile = factory.CreateGridProfile(columns: 4, rows: 4);
            FurnitureItemDefinition item = factory.CreateItem("chair", width: 2, depth: 1);
            FurniturePlacementController controller = factory.AddComponent<FurniturePlacementController>();
            string failureMessage = null;
            controller.Configure(profile);
            controller.PlacementFailed += message => failureMessage = message;

            var instance = new FurnitureInstanceData("chair-1", item.ItemId, Vector2Int.zero, item.Footprint);
            instance.SetState(FurniturePlacementState.Locked);
            controller.RegisterPlacedFurniture(instance);

            Assert.That(controller.TryBeginStore(instance, item), Is.False);

            Assert.That(controller.State, Is.EqualTo(PlacementSessionState.Idle));
            Assert.That(failureMessage, Is.EqualTo("Cannot store this item."));
        }
    }
}
