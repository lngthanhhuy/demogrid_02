using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public class FurnitureSelectionHighlight : MonoBehaviour
    {
        [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.28f, 1f);
        [SerializeField, Range(0f, 1f)] private float selectedBlend = 0.35f;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private Renderer[] renderers;
        private Color[] originalColors;
        private bool isSelected;

        private void Awake()
        {
            CacheRenderers();
            ApplySelection(false);
        }

        public void SetSelected(bool selected)
        {
            if (isSelected == selected)
                return;

            isSelected = selected;
            ApplySelection(isSelected);
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].sharedMaterial;
                if (material != null && material.HasProperty(BaseColorProperty))
                    originalColors[i] = material.GetColor(BaseColorProperty);
                else if (material != null && material.HasProperty(ColorProperty))
                    originalColors[i] = material.GetColor(ColorProperty);
                else
                    originalColors[i] = Color.white;
            }
        }

        private void ApplySelection(bool selected)
        {
            if (renderers == null)
                CacheRenderers();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                    continue;

                Color color = selected
                    ? Color.Lerp(originalColors[i], selectedColor, selectedBlend)
                    : originalColors[i];

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorProperty, color);
                propertyBlock.SetColor(BaseColorProperty, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
