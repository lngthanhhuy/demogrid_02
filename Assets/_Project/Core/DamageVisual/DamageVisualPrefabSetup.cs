using UnityEngine;

namespace SenCity.Core.DamageVisual
{
    [DisallowMultipleComponent]
    public class DamageVisualPrefabSetup : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Material bodyMaterial;
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private Transform damageEffectAnchor;

        public Transform ModelRoot => modelRoot;
        public Material BodyMaterial => bodyMaterial;
        public Renderer[] TargetRenderers => targetRenderers;
        public Collider InteractionCollider => interactionCollider;
        public Transform DamageEffectAnchor => damageEffectAnchor;

        public void Configure(
            Transform model,
            Material material,
            Renderer[] renderers,
            Collider collider,
            Transform effectAnchor)
        {
            modelRoot = model;
            bodyMaterial = material;
            targetRenderers = renderers;
            interactionCollider = collider;
            damageEffectAnchor = effectAnchor;
        }
    }
}
