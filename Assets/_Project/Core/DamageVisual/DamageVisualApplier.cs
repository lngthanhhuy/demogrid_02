using System.Collections;
using UnityEngine;

namespace SenCity.Core.DamageVisual
{
    [DisallowMultipleComponent]
    public class DamageVisualApplier : MonoBehaviour
    {
        [SerializeField] private DamageVisualProfile profile;
        [SerializeField] private Renderer[] targetRenderers;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock propertyBlock;
        private Color[] baselineColors;
        private Texture[] baselineTextures;
        private Coroutine transitionRoutine;
        private DamageVisualState appliedState = DamageVisualState.Normal;

        public DamageVisualProfile Profile => profile;
        public DamageVisualState AppliedState => appliedState;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            CacheRenderers();
            CaptureBaselineVisuals();
            ApplyState(DamageVisualState.Normal, immediate: true);
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }
        }

        public void Configure(DamageVisualProfile visualProfile, Renderer[] renderers = null)
        {
            profile = visualProfile;
            if (renderers != null && renderers.Length > 0)
                targetRenderers = renderers;

            CacheRenderers();
            CaptureBaselineVisuals();
            ApplyState(appliedState, immediate: true);
        }

        public void ApplyState(DamageVisualState state)
        {
            ApplyState(state, immediate: profile == null || profile.TransitionDuration <= 0f);
        }

        public void ApplyState(DamageVisualState state, bool immediate)
        {
            if (profile == null)
                return;

            if (!immediate && profile.TransitionDuration > 0f && isActiveAndEnabled)
            {
                if (transitionRoutine != null)
                    StopCoroutine(transitionRoutine);

                DamageVisualState fromState = appliedState;
                transitionRoutine = StartCoroutine(TransitionToState(fromState, state));
                return;
            }

            ApplyStateImmediate(state);
        }

        public void ClearOverrides()
        {
            if (targetRenderers == null)
                return;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                    continue;

                renderer.SetPropertyBlock(null);
            }

            appliedState = DamageVisualState.Normal;
        }

        private IEnumerator TransitionToState(DamageVisualState fromState, DamageVisualState toState)
        {
            DamageVisualStateSettings fromSettings = profile.GetSettings(fromState);
            DamageVisualStateSettings toSettings = profile.GetSettings(toState);
            float duration = profile.TransitionDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ApplyBlendedSettings(fromSettings, toSettings, t);
                yield return null;
            }

            ApplyStateImmediate(toState);
            transitionRoutine = null;
        }

        private void ApplyStateImmediate(DamageVisualState state)
        {
            DamageVisualStateSettings settings = profile.GetSettings(state);
            ApplyBlendedSettings(settings, settings, 1f);
            appliedState = state;
        }

        private void ApplyBlendedSettings(
            DamageVisualStateSettings fromSettings,
            DamageVisualStateSettings toSettings,
            float blend)
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                return;

            int baseMapId = ResolvePropertyId(profile.BaseMapProperty, BaseMapId);
            int legacyBaseMapId = ResolvePropertyId(profile.LegacyBaseMapProperty, MainTexId);
            int baseColorId = ResolvePropertyId(profile.BaseColorProperty, BaseColorId);
            int legacyColorId = ResolvePropertyId(profile.LegacyColorProperty, ColorId);
            int damageMaskId = ResolvePropertyId(profile.DamageMaskProperty, -1);

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                    continue;

                Material sharedMaterial = renderer.sharedMaterial;
                renderer.GetPropertyBlock(propertyBlock);

                Texture targetTexture = ResolveTexture(toSettings, baselineTextures, i);
                if (targetTexture != null)
                {
                    propertyBlock.SetTexture(baseMapId, targetTexture);
                    propertyBlock.SetTexture(legacyBaseMapId, targetTexture);
                }

                Texture targetMask = toSettings.DamageMask;
                if (targetMask != null && damageMaskId >= 0 && MaterialHasProperty(sharedMaterial, damageMaskId))
                    propertyBlock.SetTexture(damageMaskId, targetMask);

                if (ShouldApplyTint(toSettings, targetTexture))
                {
                    Color fromColor = ResolveTint(fromSettings, baselineColors, i);
                    Color toColor = ResolveTint(toSettings, baselineColors, i);
                    Color blended = Color.Lerp(fromColor, toColor, blend);
                    propertyBlock.SetColor(baseColorId, blended);
                    propertyBlock.SetColor(legacyColorId, blended);
                }

                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void CacheRenderers()
        {
            if (targetRenderers != null && targetRenderers.Length > 0)
                return;

            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void CaptureBaselineVisuals()
        {
            if (targetRenderers == null)
                return;

            baselineColors = new Color[targetRenderers.Length];
            baselineTextures = new Texture[targetRenderers.Length];

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                Material material = renderer != null ? renderer.sharedMaterial : null;
                baselineColors[i] = ReadBaselineColor(material);
                baselineTextures[i] = ReadBaselineTexture(material);
            }
        }

        private static Color ReadBaselineColor(Material material)
        {
            if (material == null)
                return Color.white;

            if (material.HasProperty(BaseColorId))
                return material.GetColor(BaseColorId);

            if (material.HasProperty(ColorId))
                return material.GetColor(ColorId);

            return Color.white;
        }

        private static Texture ReadBaselineTexture(Material material)
        {
            if (material == null)
                return null;

            if (material.HasProperty(BaseMapId))
                return material.GetTexture(BaseMapId);

            if (material.HasProperty(MainTexId))
                return material.GetTexture(MainTexId);

            return null;
        }

        private static Texture ResolveTexture(
            DamageVisualStateSettings settings,
            Texture[] baselineTextures,
            int rendererIndex)
        {
            if (settings.TextureOverride != null)
                return settings.TextureOverride;

            if (baselineTextures != null && rendererIndex >= 0 && rendererIndex < baselineTextures.Length)
                return baselineTextures[rendererIndex];

            return null;
        }

        private static bool ShouldApplyTint(DamageVisualStateSettings settings, Texture targetTexture)
        {
            return settings.UseTintFallback || (settings.TextureOverride == null && targetTexture == null);
        }

        private static Color ResolveTint(
            DamageVisualStateSettings settings,
            Color[] baselineColors,
            int rendererIndex)
        {
            if (settings.UseTintFallback)
                return settings.TintColor;

            if (baselineColors != null && rendererIndex >= 0 && rendererIndex < baselineColors.Length)
                return baselineColors[rendererIndex];

            return Color.white;
        }

        private static int ResolvePropertyId(string propertyName, int fallbackId)
        {
            return string.IsNullOrWhiteSpace(propertyName)
                ? fallbackId
                : Shader.PropertyToID(propertyName);
        }

        private static bool MaterialHasProperty(Material material, int propertyId)
        {
            return material != null && propertyId >= 0 && material.HasProperty(propertyId);
        }
    }
}
