using System;
using System.Collections.Generic;
using UnityEngine;

namespace SenCity.Core.Grid
{
    [Serializable]
    public struct GridFootprint
    {
        [Min(1)] public int width;
        [Min(1)] public int depth;

        public GridFootprint(int width, int depth)
        {
            this.width = Mathf.Max(1, width);
            this.depth = Mathf.Max(1, depth);
        }

        public int Width => Mathf.Max(1, width);
        public int Depth => Mathf.Max(1, depth);

        public GridFootprint Rotated(int rotationDegrees)
        {
            int normalized = NormalizeRotation(rotationDegrees);
            return normalized == 90 || normalized == 270
                ? new GridFootprint(Depth, Width)
                : new GridFootprint(Width, Depth);
        }

        public IEnumerable<Vector2Int> GetCells(Vector2Int originCell, int rotationDegrees = 0)
        {
            GridFootprint rotated = Rotated(rotationDegrees);
            for (int x = 0; x < rotated.Width; x++)
            {
                for (int y = 0; y < rotated.Depth; y++)
                    yield return new Vector2Int(originCell.x + x, originCell.y + y);
            }
        }

        public static int NormalizeRotation(int rotationDegrees)
        {
            int normalized = rotationDegrees % 360;
            if (normalized < 0)
                normalized += 360;

            return (normalized / 90) * 90;
        }
    }
}
