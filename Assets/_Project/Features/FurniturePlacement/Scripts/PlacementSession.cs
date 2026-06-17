using SenCity.Core.Grid;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public sealed class PlacementSession
    {
        public PlacementSession(
            PlacementSessionState state,
            FurnitureItemDefinition item,
            Vector2Int originCell,
            int rotationDegrees,
            FurnitureInstanceData sourceInstance = null)
        {
            State = state;
            Item = item;
            OriginCell = originCell;
            RotationDegrees = GridFootprint.NormalizeRotation(rotationDegrees);
            SourceInstance = sourceInstance;
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

        public bool IsMoveExisting => SourceInstance != null;
        public string IgnoredInstanceId => SourceInstance?.InstanceId;

        public void MovePreview(Vector2Int originCell)
        {
            OriginCell = originCell;
        }

        public void RotateClockwise()
        {
            RotationDegrees = GridFootprint.NormalizeRotation(RotationDegrees + 90);
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
    }
}
