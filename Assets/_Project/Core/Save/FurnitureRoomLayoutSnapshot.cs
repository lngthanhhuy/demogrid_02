using System;
using System.Collections.Generic;
using SenCity.Core.Grid;
using SenCity.Features.FurniturePlacement;
using UnityEngine;

namespace SenCity.Core.Save
{
    [Serializable]
    public class FurnitureRoomLayoutSnapshot
    {
        public List<FurnitureInstanceSaveData> instances = new List<FurnitureInstanceSaveData>();

        public FurnitureRoomLayoutSnapshot()
        {
        }

        public FurnitureRoomLayoutSnapshot(List<FurnitureInstanceSaveData> instances)
        {
            this.instances = instances ?? new List<FurnitureInstanceSaveData>();
        }
    }

    [Serializable]
    public class FurnitureInstanceSaveData
    {
        public string instanceId;
        public string itemId;
        public int cellX;
        public int cellY;
        public int rotationDegrees;
        public int footprintWidth;
        public int footprintDepth;
        public FurniturePlacementState state;

        public static FurnitureInstanceSaveData FromInstance(FurnitureInstanceData instance)
        {
            return new FurnitureInstanceSaveData
            {
                instanceId = instance.InstanceId,
                itemId = instance.ItemId,
                cellX = instance.OriginCell.x,
                cellY = instance.OriginCell.y,
                rotationDegrees = instance.RotationDegrees,
                footprintWidth = instance.Footprint.Width,
                footprintDepth = instance.Footprint.Depth,
                state = instance.State
            };
        }

        public FurnitureInstanceData ToInstance(GridFootprint fallbackFootprint)
        {
            GridFootprint savedFootprint = new GridFootprint(
                footprintWidth > 0 ? footprintWidth : fallbackFootprint.Width,
                footprintDepth > 0 ? footprintDepth : fallbackFootprint.Depth);
            var instance = new FurnitureInstanceData(
                instanceId,
                itemId,
                new Vector2Int(cellX, cellY),
                savedFootprint,
                rotationDegrees);
            instance.SetState(state);
            return instance;
        }
    }
}
