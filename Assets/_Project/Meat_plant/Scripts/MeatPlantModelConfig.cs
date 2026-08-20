using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MeatPlant
{
    [CreateAssetMenu(
        fileName = "MeatPlantModelConfig",
        menuName = "Meat Plant/Model Config"
    )]
    public class MeatPlantModelConfig : ScriptableObject
    {
        [Header("Plant Information")]
        [SerializeField]
        private string plantId;

        [SerializeField]
        private string displayName;

        [Header("Growth Stage Models")]
        [SerializeField]
        private GrowthStageModel[] growthStageModels = new GrowthStageModel[5];

        [Header("Renderer Binding")]
        [SerializeField]
        private Renderer[] rendererBindings;

        [Header("Materials")]
        [SerializeField]
        private Material[] defaultMaterials;

        [Header("Health")]
        [SerializeField]
        private HealthVisualProfile healthVisualProfile;

        [Header("Transform")]
        [SerializeField]
        private Vector3 scaleOverride = Vector3.one;

        [Header("Collider Configuration")]
        [SerializeField]
        private ColliderConfiguration colliderConfiguration;

        [Header("Anchor Configuration")]
        [SerializeField]
        private Transform effectAnchor;

        [SerializeField]
        private Transform uiAnchor;

        [System.Serializable]
        public class GrowthStageModel
        {
            [Header("Growth Stage")]
            public GrowthStage stage;

            [Header("Asset Number")]
            [Range(1, 5)]
            public int assetNumber;

            [Header("Model Reference")]
            public GameObject model;
        }

        [System.Serializable]
        public class ColliderConfiguration
        {
            public bool useCollider = true;

            public Vector3 center = Vector3.zero;

            public Vector3 size = Vector3.one;
        }

        public string PlantId => plantId;

        public string DisplayName => displayName;

        public GrowthStageModel[] GrowthStageModels =>
            growthStageModels;

        public Renderer[] RendererBindings =>
            rendererBindings;

        public Material[] DefaultMaterials =>
            defaultMaterials;

        public HealthVisualProfile HealthVisualProfile =>
            healthVisualProfile;

        public Vector3 ScaleOverride =>
            scaleOverride;

        public ColliderConfiguration ColliderConfig =>
            colliderConfiguration;

        public Transform EffectAnchor =>
            effectAnchor;

        public Transform UIAnchor =>
            uiAnchor;

        public GameObject GetModel(GrowthStage stage)
        {
            if (growthStageModels == null)
                return TryResolveFallbackModel(stage);

            foreach (GrowthStageModel entry in growthStageModels)
            {
                if (entry == null)
                    continue;

                if (entry.stage == stage && entry.model != null)
                    return entry.model;
            }

            foreach (GrowthStageModel entry in growthStageModels)
            {
                if (entry != null && entry.model != null)
                    return entry.model;
            }

            return TryResolveFallbackModel(stage);
        }

        public int GetAssetNumber(GrowthStage stage)
        {
            if (growthStageModels == null)
                return -1;

            foreach (GrowthStageModel entry in growthStageModels)
            {
                if (entry == null)
                    continue;

                if (entry.stage == stage)
                    return entry.assetNumber;
            }

            return -1;
        }

        private GameObject TryResolveFallbackModel(GrowthStage stage)
        {
            string prefabName = GetStagePrefabName(stage);
            string assetPath =
                $"Assets/_Project/Meat_plant/Placeholders/{prefabName}.prefab";

#if UNITY_EDITOR
            GameObject fallbackPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (fallbackPrefab != null)
                return fallbackPrefab;
#endif

            return null;
        }

        private static string GetStagePrefabName(GrowthStage stage)
        {
            return stage switch
            {
                GrowthStage.Sprout => "Sprout",
                GrowthStage.Juvenile => "Juvenile",
                GrowthStage.Mature => "Mature",
                GrowthStage.HarvestReady => "Harvest Ready",
                GrowthStage.PostHarvest => "Post Harvest",
                _ => "Sprout"
            };
        }
    }
}