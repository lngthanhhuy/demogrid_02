using SenCity.Core.Grid;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public sealed class PlacementValidator
    {
        private readonly SenCityGridProfile gridProfile;
        private readonly GridOccupancyMap occupancyMap;

        public PlacementValidator(SenCityGridProfile gridProfile, GridOccupancyMap occupancyMap)
        {
            this.gridProfile = gridProfile;
            this.occupancyMap = occupancyMap;
        }

        public PlacementValidationResult Validate(
            FurnitureItemDefinition item,
            Vector2Int originCell,
            int rotationDegrees,
            string ignoredInstanceId = null)
        {
            if (gridProfile == null || occupancyMap == null)
                return PlacementValidationResult.Invalid(PlacementValidationFailure.MissingGrid, "Missing placement grid.");

            if (item == null)
                return PlacementValidationResult.Invalid(PlacementValidationFailure.MissingItem, "Missing furniture item.");

            if (item.LockedBySystem)
                return PlacementValidationResult.Invalid(PlacementValidationFailure.Locked, "Item is locked.");

            GridFootprint rotated = item.Footprint.Rotated(rotationDegrees);
            for (int x = 0; x < rotated.Width; x++)
            {
                for (int y = 0; y < rotated.Depth; y++)
                {
                    Vector2Int cell = new Vector2Int(originCell.x + x, originCell.y + y);
                    if (!gridProfile.IsValidCell(cell))
                        return PlacementValidationResult.Invalid(PlacementValidationFailure.OutOfBounds, "Outside placement area.");

                    string occupant = occupancyMap.GetOccupant(cell);
                    if (!string.IsNullOrEmpty(occupant) && occupant != ignoredInstanceId)
                        return PlacementValidationResult.Invalid(PlacementValidationFailure.Overlap, "Cell already has furniture.");
                }
            }

            return PlacementValidationResult.Valid();
        }
    }
}
