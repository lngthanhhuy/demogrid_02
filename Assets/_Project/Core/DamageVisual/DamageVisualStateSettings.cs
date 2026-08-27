using System;
using UnityEngine;

namespace SenCity.Core.DamageVisual
{
    [Serializable]
    public class DamageVisualStateSettings
    {
        [SerializeField] private Texture textureOverride;
        [SerializeField] private Texture damageMask;
        [SerializeField] private Color tintColor = Color.white;
        [SerializeField] private bool useTintFallback;

        public Texture TextureOverride => textureOverride;
        public Texture DamageMask => damageMask;
        public Color TintColor => tintColor;
        public bool UseTintFallback => useTintFallback;
    }
}
