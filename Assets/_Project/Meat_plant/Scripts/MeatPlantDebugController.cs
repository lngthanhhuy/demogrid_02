using UnityEngine;

namespace MeatPlant
{
    public class MeatPlantDebugController : MonoBehaviour
    {
        [SerializeField]
        private MeatPlantVisualController plant;

        private void Reset()
        {
            plant = GetComponent<MeatPlantVisualController>();

            if (plant != null)
            {
                Debug.Log(
                    "[MeatPlantDebug] Plant reference auto-assigned from current GameObject.",
                    this
                );
            }
        }

        private void Awake()
        {
            if (plant == null)
            {
                plant = GetComponent<MeatPlantVisualController>();
            }

            if (plant == null)
            {
                plant = FindAnyObjectByType<MeatPlantVisualController>();
            }

            if (plant == null)
            {
                Debug.LogError(
                    "[MeatPlantDebug] MeatPlantVisualController not found on this object or in scene.",
                    this
                );
            }
        }

        private bool TryGetPlant()
        {
            if (plant != null)
                return true;

            Debug.LogWarning(
                "[MeatPlantDebug] Plant reference is missing. Please assign it in the Inspector or make sure the component exists in the scene.",
                this
            );

            return false;
        }

        public void SetSprout()
        {
            if (!TryGetPlant())
                return;

            plant.SetGrowthStage(GrowthStage.Sprout);
        }

        public void SetJuvenile()
        {
            if (!TryGetPlant())
                return;

            plant.SetGrowthStage(GrowthStage.Juvenile);
        }

        public void SetMature()
        {
            if (!TryGetPlant())
                return;

            plant.SetGrowthStage(GrowthStage.Mature);
        }

        public void SetHarvestReady()
        {
            if (!TryGetPlant())
                return;

            plant.SetGrowthStage(GrowthStage.HarvestReady);
        }

        public void SetPostHarvest()
        {
            if (!TryGetPlant())
                return;

            plant.SetGrowthStage(GrowthStage.PostHarvest);
        }

        public void SetHealthy()
        {
            if (!TryGetPlant())
                return;

            plant.SetHealthState(HealthState.Healthy);
        }

        public void SetDamaged()
        {
            if (!TryGetPlant())
                return;

            plant.SetHealthState(HealthState.Damaged);
        }

        public void SetDead()
        {
            if (!TryGetPlant())
                return;

            plant.SetHealthState(HealthState.Dead);
        }

        public void ResetPlant()
        {
            if (!TryGetPlant())
                return;

            plant.SetState(
                GrowthStage.Sprout,
                HealthState.Healthy
            );
        }
    }
}