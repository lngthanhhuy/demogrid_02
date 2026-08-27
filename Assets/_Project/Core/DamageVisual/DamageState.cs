using System;
using UnityEngine;

namespace SenCity.Core.DamageVisual
{
    [DisallowMultipleComponent]
    public class DamageState : MonoBehaviour
    {
        [SerializeField] private DamageStateThresholds thresholds = new DamageStateThresholds();
        [SerializeField, Min(0f)] private float currentDamage;

        public float CurrentDamage => currentDamage;
        public DamageVisualState CurrentState { get; private set; } = DamageVisualState.Normal;
        public DamageStateThresholds Thresholds => thresholds;

        public event Action<DamageVisualState> StateChanged;
        public event Action<float> DamageChanged;

        private void OnValidate()
        {
            thresholds.OnValidate();
            currentDamage = Mathf.Max(0f, currentDamage);
            SyncStateFromDamage(notify: false);
        }

        private void Awake()
        {
            SyncStateFromDamage(notify: false);
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f)
                return;

            SetDamage(currentDamage + amount);
        }

        public void SetDamage(float damage)
        {
            float clampedDamage = Mathf.Max(0f, damage);
            if (Mathf.Approximately(currentDamage, clampedDamage))
                return;

            currentDamage = clampedDamage;
            DamageChanged?.Invoke(currentDamage);
            SyncStateFromDamage(notify: true);
        }

        public void SetState(DamageVisualState state)
        {
            float targetDamage = thresholds.DamageForState(state);
            CurrentState = state;
            currentDamage = targetDamage;
            DamageChanged?.Invoke(currentDamage);
            StateChanged?.Invoke(CurrentState);
        }

        public void Reset()
        {
            SetDamage(0f);
        }

        private void SyncStateFromDamage(bool notify)
        {
            DamageVisualState evaluatedState = thresholds.EvaluateState(currentDamage);
            if (!notify)
            {
                CurrentState = evaluatedState;
                return;
            }

            if (CurrentState == evaluatedState)
                return;

            CurrentState = evaluatedState;
            StateChanged?.Invoke(CurrentState);
        }
    }
}
