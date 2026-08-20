using UnityEngine;
using UnityEngine.UI;

namespace MeatPlant
{
    public class MeatPlantUiStageDebugger : MonoBehaviour
    {
        [Header("Plant Reference")]
        [SerializeField]
        private MeatPlantVisualController plant;

        [Header("Demo Health Buttons")]
        [SerializeField]
        private Button healthyButton;

        [SerializeField]
        private Button damagedButton;

        [SerializeField]
        private Button deadButton;

        private void Reset()
        {
            plant = GetComponentInParent<MeatPlantVisualController>();

            if (plant == null)
            {
                plant = FindAnyObjectByType<MeatPlantVisualController>();
            }
        }

        private void Awake()
        {
            if (plant == null)
            {
                plant = GetComponentInParent<MeatPlantVisualController>();
            }

            if (plant == null)
            {
                plant = FindAnyObjectByType<MeatPlantVisualController>();
            }

            BindButtons();
        }

        private void BindButtons()
        {
            if (healthyButton != null)
            {
                healthyButton.onClick.RemoveAllListeners();
                healthyButton.onClick.AddListener(SetHealthy);
            }

            if (damagedButton != null)
            {
                damagedButton.onClick.RemoveAllListeners();
                damagedButton.onClick.AddListener(SetDamaged);
            }

            if (deadButton != null)
            {
                deadButton.onClick.RemoveAllListeners();
                deadButton.onClick.AddListener(SetDead);
            }
        }

        public void SetHealthy()
        {
            if (plant == null)
            {
                Debug.LogError(
                    "[MeatPlantUiDebugger] Plant reference is missing.",
                    this
                );

                return;
            }

            plant.SetHealthState(HealthState.Healthy);
        }

        public void SetDamaged()
        {
            if (plant == null)
            {
                Debug.LogError(
                    "[MeatPlantUiDebugger] Plant reference is missing.",
                    this
                );

                return;
            }

            plant.SetHealthState(HealthState.Damaged);
        }

        public void SetDead()
        {
            if (plant == null)
            {
                Debug.LogError(
                    "[MeatPlantUiDebugger] Plant reference is missing.",
                    this
                );

                return;
            }

            plant.SetHealthState(HealthState.Dead);
        }

        public void SetSprout()
        {
            if (plant == null)
                return;

            plant.SetGrowthStage(GrowthStage.Sprout);
        }

        public void SetMature()
        {
            if (plant == null)
                return;

            plant.SetGrowthStage(GrowthStage.Mature);
        }

        public void SetPostHarvest()
        {
            if (plant == null)
                return;

            plant.SetGrowthStage(GrowthStage.PostHarvest);
        }
    }
}
