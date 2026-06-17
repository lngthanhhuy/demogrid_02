using System.Linq;
using NUnit.Framework;
using SenCity.Core.Grid;
using UnityEngine;

namespace SenCity.Tests.FurniturePlacement
{
    public sealed class GridOccupancyMapTests
    {
        [Test]
        public void ReserveRejectsOverlapsUnlessInstanceIsIgnored()
        {
            var map = new GridOccupancyMap(4, 4);
            var footprint = new GridFootprint(2, 1);

            map.Reserve("chair-a", Vector2Int.zero, footprint, 0);

            Assert.That(map.CanReserve(Vector2Int.zero, footprint, 0), Is.False);
            Assert.That(map.CanReserve(Vector2Int.zero, footprint, 0, "chair-a"), Is.True);
        }

        [Test]
        public void ReleaseFreesEveryReservedCellForInstance()
        {
            var map = new GridOccupancyMap(4, 4);
            var footprint = new GridFootprint(2, 2);
            map.Reserve("table-a", new Vector2Int(1, 1), footprint, 0);

            Assert.That(map.GetReservedCells("table-a").Count, Is.EqualTo(4));

            map.Release("table-a");

            Assert.That(map.GetReservedCells("table-a"), Is.Empty);
            Assert.That(footprint.GetCells(new Vector2Int(1, 1)).All(map.IsInside), Is.True);
        }
    }
}
