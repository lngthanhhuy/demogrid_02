using UnityEngine;

namespace MeatPlant
{
    [CreateAssetMenu(
        fileName = "HealthVisualProfile",
        menuName = "Meat Plant/Health Visual Profile"
    )]
    public class HealthVisualProfile : ScriptableObject
    {
        [System.Serializable]
        public class HealthVisual
        {
            public HealthState state;

            public Material material;

            public Color tint = Color.white;

            public bool useTint = true;
        }

        [SerializeField]
        private HealthVisual[] visuals = new HealthVisual[3];

        public HealthVisual GetVisual(HealthState state)
        {
            foreach (HealthVisual visual in visuals)
            {
                if (visual.state == state)
                {
                    return visual;
                }
            }

            return null;
        }
    }
}