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
        private bool poseInitialized;
        private Vector2Int lastOriginCell;
        private int lastRotationDegrees;
        private bool validityInitialized;
        private bool lastValidity;

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

            int normalizedRotation = GridFootprint.NormalizeRotation(rotationDegrees);
            if (poseInitialized && lastOriginCell == originCell && lastRotationDegrees == normalizedRotation)
                return;

            transform.position = gridProfile.FootprintCenter(originCell, footprint, rotationDegrees);
            transform.rotation = Quaternion.Euler(0f, normalizedRotation, 0f);
            lastOriginCell = originCell;
            lastRotationDegrees = normalizedRotation;
            poseInitialized = true;
        }

        public void SetValidity(bool isValid)
        {
            if (validityInitialized && lastValidity == isValid)
                return;

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

            lastValidity = isValid;
            validityInitialized = true;
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
