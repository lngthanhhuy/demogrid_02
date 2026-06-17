using System;
using SenCity.Core.Grid;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    [Serializable]
    public class FurnitureInstanceData
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string itemId;
        [SerializeField] private Vector2Int originCell;
        [SerializeField] private int rotationDegrees;
        [SerializeField] private GridFootprint footprint;
        [SerializeField] private FurniturePlacementState state;

        public string InstanceId => instanceId;
        public string ItemId => itemId;
        public Vector2Int OriginCell => originCell;
        public int RotationDegrees => rotationDegrees;
        public GridFootprint Footprint => footprint;
        public FurniturePlacementState State => state;

        public FurnitureInstanceData(
            string instanceId,
            string itemId,
            Vector2Int originCell,
            GridFootprint footprint,
            int rotationDegrees = 0)
        {
            this.instanceId = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
            this.itemId = itemId;
            this.originCell = originCell;
            this.footprint = footprint;
            this.rotationDegrees = GridFootprint.NormalizeRotation(rotationDegrees);
            state = FurniturePlacementState.Placed;
        }

        public void MoveTo(Vector2Int nextOriginCell, int nextRotationDegrees)
        {
            originCell = nextOriginCell;
            rotationDegrees = GridFootprint.NormalizeRotation(nextRotationDegrees);
            state = FurniturePlacementState.Placed;
        }

        public void SetState(FurniturePlacementState nextState)
        {
            state = nextState;
        }
    }

    public enum FurniturePlacementState
    {
        Placed,
        Moving,
        Stored,
        Locked
    }
}
