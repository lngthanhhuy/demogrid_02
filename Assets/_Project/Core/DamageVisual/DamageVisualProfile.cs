using UnityEngine;

namespace SenCity.Core.DamageVisual
{
    [CreateAssetMenu(menuName = "Sen City/Damage Visual Profile")]
    public class DamageVisualProfile : ScriptableObject
    {
        [SerializeField] private DamageVisualStateSettings normal = new DamageVisualStateSettings();
        [SerializeField] private DamageVisualStateSettings damaged = new DamageVisualStateSettings();
        [SerializeField] private DamageVisualStateSettings destroyed = new DamageVisualStateSettings();
        [SerializeField] private string baseMapProperty = "_BaseMap";
        [SerializeField] private string legacyBaseMapProperty = "_MainTex";
        [SerializeField] private string baseColorProperty = "_BaseColor";
        [SerializeField] private string legacyColorProperty = "_Color";
        [SerializeField] private string damageMaskProperty = "_DamageMask";
        [SerializeField, Min(0f)] private float transitionDuration;

        public DamageVisualStateSettings Normal => normal;
        public DamageVisualStateSettings Damaged => damaged;
        public DamageVisualStateSettings Destroyed => destroyed;
        public string BaseMapProperty => baseMapProperty;
        public string LegacyBaseMapProperty => legacyBaseMapProperty;
        public string BaseColorProperty => baseColorProperty;
        public string LegacyColorProperty => legacyColorProperty;
        public string DamageMaskProperty => damageMaskProperty;
        public float TransitionDuration => transitionDuration;

        public DamageVisualStateSettings GetSettings(DamageVisualState state)
        {
            return state switch
            {
                DamageVisualState.Damaged => damaged,
                DamageVisualState.Destroyed => destroyed,
                _ => normal
            };
        }
    }
}
