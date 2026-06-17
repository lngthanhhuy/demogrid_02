using UnityEngine;

namespace SenCity.Core.Grid
{
    [CreateAssetMenu(menuName = "Sen City/Grid Profile")]
    public class SenCityGridProfile : ScriptableObject
    {
        [Min(0.01f)] public float cellSize = 0.2f;
        [Min(1)] public int columns = 20;
        [Min(1)] public int rows = 20;
        public Vector3 origin = Vector3.zero;

        public bool IsValidCell(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
        }

        public bool WorldToCell(Vector3 worldPosition, out Vector2Int cell)
        {
            Vector3 local = worldPosition - origin;
            cell = new Vector2Int(
                Mathf.FloorToInt(local.x / cellSize),
                Mathf.FloorToInt(local.z / cellSize));

            return IsValidCell(cell);
        }

        public Vector3 CellOrigin(Vector2Int cell)
        {
            return origin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
        }

        public Vector3 CellCenter(Vector2Int cell)
        {
            return CellOrigin(cell) + new Vector3(cellSize * 0.5f, 0f, cellSize * 0.5f);
        }

        public Vector3 FootprintCenter(Vector2Int originCell, GridFootprint footprint, int rotationDegrees)
        {
            GridFootprint rotated = footprint.Rotated(rotationDegrees);
            return CellOrigin(originCell) + new Vector3(
                rotated.Width * cellSize * 0.5f,
                0f,
                rotated.Depth * cellSize * 0.5f);
        }
    }
}
