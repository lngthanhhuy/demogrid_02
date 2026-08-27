using System;
using UnityEngine;

namespace SenCity.Core.DamageVisual
{
    [Serializable]
    public class DamageStateThresholds
    {
        [SerializeField, Min(0f)] private float damagedThreshold = 25f;
        [SerializeField, Min(0f)] private float destroyedThreshold = 75f;

        public float DamagedThreshold => damagedThreshold;
        public float DestroyedThreshold => destroyedThreshold;

        public DamageVisualState EvaluateState(float damage)
        {
            if (damage >= destroyedThreshold)
                return DamageVisualState.Destroyed;

            if (damage >= damagedThreshold)
                return DamageVisualState.Damaged;

            return DamageVisualState.Normal;
        }

        public float DamageForState(DamageVisualState state)
        {
            return state switch
            {
                DamageVisualState.Destroyed => destroyedThreshold,
                DamageVisualState.Damaged => damagedThreshold,
                _ => 0f
            };
        }

        public void OnValidate()
        {
            damagedThreshold = Mathf.Max(0f, damagedThreshold);
            destroyedThreshold = Mathf.Max(damagedThreshold, destroyedThreshold);
        }
    }
}
