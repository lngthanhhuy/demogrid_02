using NUnit.Framework;
using SenCity.Core.Grid;
using SenCity.Features.FurniturePlacement.Save;
using SenCity.Features.FurniturePlacement;
using UnityEngine;

namespace SenCity.Tests.FurniturePlacement
{
    public sealed class FurnitureSnapshotTests
    {
        [Test]
        public void InstanceSaveDataRoundTripsPlacementFields()
        {
            var footprint = new GridFootprint(2, 3);
            var instance = new FurnitureInstanceData("instance-1", "planter", new Vector2Int(2, 1), footprint, 270);

            FurnitureInstanceSaveData saveData = FurnitureInstanceSaveData.FromInstance(instance);
            FurnitureInstanceData restored = saveData.ToInstance(new GridFootprint(1, 1));

            Assert.That(restored.InstanceId, Is.EqualTo("instance-1"));
            Assert.That(restored.ItemId, Is.EqualTo("planter"));
            Assert.That(restored.OriginCell, Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(restored.RotationDegrees, Is.EqualTo(270));
            Assert.That(restored.Footprint.Width, Is.EqualTo(2));
            Assert.That(restored.Footprint.Depth, Is.EqualTo(3));
            Assert.That(restored.State, Is.EqualTo(FurniturePlacementState.Placed));
        }

        [Test]
        public void ToInstanceUsesFallbackFootprintWhenSavedFootprintIsMissing()
        {
            var saveData = new FurnitureInstanceSaveData
            {
                instanceId = "instance-2",
                itemId = "table",
                cellX = 1,
                cellY = 2,
                rotationDegrees = 90,
                footprintWidth = 0,
                footprintDepth = 0,
                state = FurniturePlacementState.Placed
            };

            FurnitureInstanceData restored = saveData.ToInstance(new GridFootprint(4, 2));

            Assert.That(restored.Footprint.Width, Is.EqualTo(4));
            Assert.That(restored.Footprint.Depth, Is.EqualTo(2));
        }
    }
}
