using UnityEngine;

namespace MeatPlant
{
    public class MeatPlantVisualController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private MeatPlantModelConfig config;

        [Header("Model")]
        [SerializeField]
        private Transform modelContainer;

        [Header("Current State")]
        [SerializeField]
        private GrowthStage currentGrowthStage = GrowthStage.Sprout;

        [SerializeField]
        private HealthState currentHealthState = HealthState.Healthy;

        private GameObject currentModel;

        public GrowthStage CurrentGrowthStage =>
            currentGrowthStage;

        public HealthState CurrentHealthState =>
            currentHealthState;

        private void Start()
        {
            RefreshVisual();
        }

        // =========================================================
        // GROWTH STAGE
        // =========================================================

        public void SetGrowthStage(GrowthStage stage)
        {
            GrowthStage previousStage = currentGrowthStage;
            HealthState previousHealth = currentHealthState;

            if (currentGrowthStage == stage &&
                currentHealthState == MapStageToHealthState(stage))
            {
                Debug.Log(
                    $"[MeatPlantVisual] Stage already set: {stage}. " +
                    $"Health: {currentHealthState}",
                    this
                );

                return;
            }

            currentGrowthStage = stage;
            currentHealthState = MapStageToHealthState(stage);

            Debug.Log(
                $"[MeatPlantVisual] GrowthStage changed: " +
                $"{previousStage} -> {currentGrowthStage}. " +
                $"Health: {previousHealth} -> {currentHealthState}",
                this
            );

            RefreshGrowthStage();
        }

        private static HealthState MapStageToHealthState(
            GrowthStage stage)
        {
            return stage switch
            {
                GrowthStage.Sprout => HealthState.Healthy,
                GrowthStage.Juvenile => HealthState.Healthy,
                GrowthStage.Mature => HealthState.Healthy,
                GrowthStage.HarvestReady => HealthState.Damaged,
                GrowthStage.PostHarvest => HealthState.Dead,
                _ => HealthState.Healthy
            };
        }

        private void RefreshGrowthStage()
        {
            if (config == null)
            {
                Debug.LogWarning(
                    $"{name}: MeatPlantModelConfig is not assigned.",
                    this
                );

                return;
            }

            if (modelContainer == null)
            {
                Debug.LogWarning(
                    $"{name}: Model Container is not assigned.",
                    this
                );

                return;
            }

            // Remove current model
            ClearModelContainer();

            // Get model from ScriptableObject config
            GameObject modelPrefab =
                config.GetModel(currentGrowthStage);

            if (modelPrefab == null)
            {
                Debug.LogWarning(
                    $"{name}: No model configured for " +
                    $"Growth Stage '{currentGrowthStage}'.",
                    this
                );

                return;
            }

            // Create new model
            currentModel = Instantiate(
                modelPrefab,
                modelContainer
            );

            currentModel.name =
                $"{currentGrowthStage}_Model";

            // Reset local transform
            Transform modelTransform =
                currentModel.transform;

            modelTransform.localPosition =
                Vector3.zero;

            modelTransform.localRotation =
                Quaternion.identity;

            // Keep the prefab's original scale
            // and apply optional config scale override.
            Vector3 prefabScale =
                modelTransform.localScale;

            Vector3 finalScale =
                Vector3.Scale(
                    prefabScale,
                    config.ScaleOverride
                );

            modelTransform.localScale =
                finalScale;

            // Do NOT override the model's default
            // material here.
            //
            // Each Growth Stage model should keep
            // its own material / texture reference.

            RefreshHealthVisual();
        }

        // =========================================================
        // HEALTH STATE
        // =========================================================

        public void SetHealthState(HealthState state)
        {
            if (currentHealthState == state)
            {
                Debug.Log(
                    $"[MeatPlantVisual] Health already set: {state}.",
                    this
                );

                return;
            }

            HealthState previousHealth = currentHealthState;
            currentHealthState = state;

            Debug.Log(
                $"[MeatPlantVisual] Health changed: " +
                $"{previousHealth} -> {currentHealthState}. " +
                $"GrowthStage: {currentGrowthStage}",
                this
            );

            RefreshHealthVisual();
        }

        private void RefreshHealthVisual()
        {
            if (currentModel == null)
                return;

            if (config == null)
                return;

            HealthVisualProfile profile =
                config.HealthVisualProfile;

            if (profile == null)
                return;

            HealthVisualProfile.HealthVisual visual =
                profile.GetVisual(currentHealthState);

            if (visual == null)
                return;

            Renderer[] renderers =
                currentModel.GetComponentsInChildren<Renderer>(
                    true
                );

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                ApplyHealthVisual(
                    renderer,
                    visual
                );
            }
        }

        private void ApplyHealthVisual(
            Renderer renderer,
            HealthVisualProfile.HealthVisual visual)
        {
            // Do not replace the prefab material/texture.
            // Keep the material assigned on the prefab itself.
            // Only tint/color the current material when needed.
            if (!visual.useTint)
                return;

            Material material =
                renderer.sharedMaterial;

            if (material == null)
                return;

            MaterialPropertyBlock block =
                new MaterialPropertyBlock();

            renderer.GetPropertyBlock(block);

            if (material.HasProperty("_BaseColor"))
            {
                block.SetColor(
                    "_BaseColor",
                    visual.tint
                );
            }
            else if (material.HasProperty("_Color"))
            {
                block.SetColor(
                    "_Color",
                    visual.tint
                );
            }

            renderer.SetPropertyBlock(block);
        }

        // =========================================================
        // STATE
        // =========================================================

        public void SetState(
            GrowthStage growthStage,
            HealthState healthState)
        {
            currentGrowthStage =
                growthStage;

            currentHealthState =
                healthState;

            RefreshVisual();
        }

        public void RefreshVisual()
        {
            RefreshGrowthStage();

            // RefreshGrowthStage()
            // already calls RefreshHealthVisual()
            // after creating the model.
        }

        // =========================================================
        // CLEAR MODEL
        // =========================================================

        private void ClearModelContainer()
        {
            if (modelContainer == null)
                return;

            for (int i = modelContainer.childCount - 1;
                 i >= 0;
                 i--)
            {
                Transform child =
                    modelContainer.GetChild(i);

                Destroy(child.gameObject);
            }

            currentModel = null;
        }
    }
}