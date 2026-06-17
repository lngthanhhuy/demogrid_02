using SenCity.Core.Grid;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public class FurnitureGhostPreview : MonoBehaviour
    {
        [SerializeField] private Color validColor = new Color(0.18f, 0.85f, 0.45f, 0.45f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.24f, 0.16f, 0.45f);

        private MaterialPropertyBlock propertyBlock;
        private Renderer[] renderers;

        private void Awake()
        {
            CacheRenderers();
        }

        public void SetPose(
            SenCityGridProfile gridProfile,
            Vector2Int originCell,
            GridFootprint footprint,
            int rotationDegrees)
        {
            if (gridProfile == null)
                return;

            transform.position = gridProfile.FootprintCenter(originCell, footprint, rotationDegrees);
            transform.rotation = Quaternion.Euler(0f, GridFootprint.NormalizeRotation(rotationDegrees), 0f);
        }

        public void SetValidity(bool isValid)
        {
            CacheRenderers();
            EnsurePropertyBlock();
            Color color = isValid ? validColor : invalidColor;
            foreach (Renderer previewRenderer in renderers)
            {
                if (previewRenderer == null)
                    continue;

                previewRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_Color", color);
                previewRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void CacheRenderers()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
        }
    }
}
