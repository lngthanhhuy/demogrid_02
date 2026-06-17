using System.Collections.Generic;
using UnityEngine;

namespace SenCity.Core.Grid
{
    public sealed class GridOccupancyMap
    {
        private readonly int columns;
        private readonly int rows;
        private readonly string[,] occupiedByInstanceId;

        public GridOccupancyMap(int columns, int rows)
        {
            this.columns = Mathf.Max(1, columns);
            this.rows = Mathf.Max(1, rows);
            occupiedByInstanceId = new string[this.columns, this.rows];
        }

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
        }

        public bool CanReserve(
            Vector2Int originCell,
            GridFootprint footprint,
            int rotationDegrees,
            string ignoredInstanceId = null)
        {
            foreach (Vector2Int cell in footprint.GetCells(originCell, rotationDegrees))
            {
                if (!IsInside(cell))
                    return false;

                string occupant = occupiedByInstanceId[cell.x, cell.y];
                if (!string.IsNullOrEmpty(occupant) && occupant != ignoredInstanceId)
                    return false;
            }

            return true;
        }

        public void Reserve(
            string instanceId,
            Vector2Int originCell,
            GridFootprint footprint,
            int rotationDegrees)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                Debug.LogWarning("[GridOccupancyMap] Reserve called without an instance id.");
                return;
            }

            if (!CanReserve(originCell, footprint, rotationDegrees, instanceId))
            {
                Debug.LogWarning($"[GridOccupancyMap] Cannot reserve cells for {instanceId} at {originCell}.");
                return;
            }

            foreach (Vector2Int cell in footprint.GetCells(originCell, rotationDegrees))
                occupiedByInstanceId[cell.x, cell.y] = instanceId;
        }

        public void Release(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (occupiedByInstanceId[x, y] == instanceId)
                        occupiedByInstanceId[x, y] = null;
                }
            }
        }

        public string GetOccupant(Vector2Int cell)
        {
            return IsInside(cell) ? occupiedByInstanceId[cell.x, cell.y] : null;
        }

        public IReadOnlyList<Vector2Int> GetReservedCells(string instanceId)
        {
            var cells = new List<Vector2Int>();
            if (string.IsNullOrEmpty(instanceId))
                return cells;

            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (occupiedByInstanceId[x, y] == instanceId)
                        cells.Add(new Vector2Int(x, y));
                }
            }

            return cells;
        }
    }
}
