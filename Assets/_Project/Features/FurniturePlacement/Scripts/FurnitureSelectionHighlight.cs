using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public class FurnitureSelectionHighlight : MonoBehaviour
    {
        [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.28f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.7f, 0.9f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float selectedBlend = 0.35f;
        [SerializeField, Range(0f, 1f)] private float hoverBlend = 0.22f;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock propertyBlock;
        private Renderer[] renderers;
        private Color[] originalColors;
        private bool isSelected;
        private bool isHovered;

        public bool IsSelected => isSelected;
        public bool IsHovered => isHovered;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            CacheRenderers();
            ApplySelection(false);
        }

        public void SetSelected(bool selected)
        {
            if (isSelected == selected)
                return;

            isSelected = selected;
            ApplyHighlight();
        }

        public void SetHovered(bool hovered)
        {
            if (isHovered == hovered)
                return;

            isHovered = hovered;
            ApplyHighlight();
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
            isSelected = selected;
            ApplyHighlight();
        }

        private void ApplyHighlight()
        {
            if (renderers == null)
                CacheRenderers();

            propertyBlock ??= new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                    continue;

                Color color = originalColors[i];
                if (isSelected)
                    color = Color.Lerp(color, selectedColor, selectedBlend);
                else if (isHovered)
                    color = Color.Lerp(color, hoverColor, hoverBlend);

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorProperty, color);
                propertyBlock.SetColor(BaseColorProperty, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
