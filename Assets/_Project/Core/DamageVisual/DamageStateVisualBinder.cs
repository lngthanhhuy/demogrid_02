using UnityEngine;

namespace SenCity.Core.DamageVisual
{
    [DisallowMultipleComponent]
    public class DamageStateVisualBinder : MonoBehaviour
    {
        [SerializeField] private DamageState damageState;
        [SerializeField] private DamageVisualApplier visualApplier;

        private void Reset()
        {
            damageState = GetComponent<DamageState>();
            visualApplier = GetComponent<DamageVisualApplier>();
        }

        private void Awake()
        {
            if (damageState == null)
                damageState = GetComponent<DamageState>();

            if (visualApplier == null)
                visualApplier = GetComponent<DamageVisualApplier>();
        }

        private void OnEnable()
        {
            if (damageState == null || visualApplier == null)
                return;

            damageState.StateChanged += HandleStateChanged;
            visualApplier.ApplyState(damageState.CurrentState);
        }

        private void OnDisable()
        {
            if (damageState == null)
                return;

            damageState.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(DamageVisualState state)
        {
            if (visualApplier != null)
                visualApplier.ApplyState(state);
        }
    }
}
