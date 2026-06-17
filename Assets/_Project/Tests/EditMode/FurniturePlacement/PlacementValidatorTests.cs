using NUnit.Framework;
using SenCity.Core.Grid;
using SenCity.Features.FurniturePlacement;
using UnityEngine;

namespace SenCity.Tests.FurniturePlacement
{
    public sealed class PlacementValidatorTests
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
        public void ValidateRejectsOutOfBoundsFootprint()
        {
            SenCityGridProfile profile = factory.CreateGridProfile(columns: 3, rows: 3);
            FurnitureItemDefinition item = factory.CreateItem("wide-table", width: 2, depth: 2);
            var validator = new PlacementValidator(profile, new GridOccupancyMap(3, 3));

            PlacementValidationResult result = validator.Validate(item, new Vector2Int(2, 2), 0);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Failure, Is.EqualTo(PlacementValidationFailure.OutOfBounds));
        }

        [Test]
        public void ValidateRejectsLockedItems()
        {
            SenCityGridProfile profile = factory.CreateGridProfile();
            FurnitureItemDefinition item = factory.CreateItem("locked-item", lockedBySystem: true);
            var validator = new PlacementValidator(profile, new GridOccupancyMap(profile.columns, profile.rows));

            PlacementValidationResult result = validator.Validate(item, Vector2Int.zero, 0);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Failure, Is.EqualTo(PlacementValidationFailure.Locked));
        }

        [Test]
        public void ValidateAllowsMovingAcrossOwnCells()
        {
            SenCityGridProfile profile = factory.CreateGridProfile();
            FurnitureItemDefinition item = factory.CreateItem("chair", width: 2, depth: 1);
            var map = new GridOccupancyMap(profile.columns, profile.rows);
            map.Reserve("chair-instance", Vector2Int.zero, item.Footprint, 0);
            var validator = new PlacementValidator(profile, map);

            PlacementValidationResult result = validator.Validate(item, Vector2Int.zero, 0, "chair-instance");

            Assert.That(result.IsValid, Is.True);
        }
    }
}
