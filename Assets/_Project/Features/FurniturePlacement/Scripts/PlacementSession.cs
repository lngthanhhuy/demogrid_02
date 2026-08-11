using SenCity.Core.Grid;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public sealed class PlacementSession
    {
        private Vector2Int anchorCenterHalfCells;

        public PlacementSession(
            PlacementSessionState state,
            FurnitureItemDefinition item,
            Vector2Int originCell,
            int rotationDegrees,
            FurnitureInstanceData sourceInstance = null,
            bool hasPreviewPosition = true)
        {
            State = state;
            Item = item;
            OriginCell = originCell;
            RotationDegrees = GridFootprint.NormalizeRotation(rotationDegrees);
            SourceInstance = sourceInstance;
            HasPreviewPosition = sourceInstance != null || hasPreviewPosition;
            UpdateAnchorFromOrigin();
            LastValidation = PlacementValidationResult.Invalid(
                PlacementValidationFailure.NoActiveSession,
                "Preview has not been validated.");
        }

        public PlacementSessionState State { get; private set; }
        public FurnitureItemDefinition Item { get; }
        public FurnitureInstanceData SourceInstance { get; }
        public Vector2Int OriginCell { get; private set; }
        public int RotationDegrees { get; private set; }
        public PlacementValidationResult LastValidation { get; private set; }
        public bool HasPreviewPosition { get; private set; }

        public bool IsMoveExisting => SourceInstance != null;
        public string IgnoredInstanceId => SourceInstance?.InstanceId;

        public bool MovePreview(Vector2Int originCell)
        {
            bool changed = !HasPreviewPosition || OriginCell != originCell;
            if (!changed)
                return false;

            OriginCell = originCell;
            HasPreviewPosition = true;
            UpdateAnchorFromOrigin();
            return true;
        }

        public bool RotateClockwise()
        {
            int nextRotation = GridFootprint.NormalizeRotation(RotationDegrees + 90);
            if (nextRotation == RotationDegrees || Item == null)
                return false;

            GridFootprint nextFootprint = Item.Footprint.Rotated(nextRotation);
            OriginCell = new Vector2Int(
                Mathf.FloorToInt((anchorCenterHalfCells.x - nextFootprint.Width) * 0.5f),
                Mathf.FloorToInt((anchorCenterHalfCells.y - nextFootprint.Depth) * 0.5f));
            RotationDegrees = nextRotation;
            return true;
        }

        public void ApplyValidation(PlacementValidationResult validation)
        {
            LastValidation = validation;
            if (State == PlacementSessionState.PlacementNew || State == PlacementSessionState.MovingExisting ||
                State == PlacementSessionState.ValidPreview || State == PlacementSessionState.InvalidPreview)
            {
                State = validation.IsValid ? PlacementSessionState.ValidPreview : PlacementSessionState.InvalidPreview;
            }
        }

        public void SetState(PlacementSessionState nextState)
        {
            State = nextState;
        }

        private void UpdateAnchorFromOrigin()
        {
            GridFootprint rotated = Item != null
                ? Item.Footprint.Rotated(RotationDegrees)
                : new GridFootprint(1, 1);
            anchorCenterHalfCells = new Vector2Int(
                OriginCell.x * 2 + rotated.Width,
                OriginCell.y * 2 + rotated.Depth);
        }
    }
}
