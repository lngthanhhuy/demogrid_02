using NUnit.Framework;
using SenCity.Core.DamageVisual;
using UnityEngine;

namespace SenCity.Tests.DamageVisual
{
    public sealed class DamageStateTests
    {
        private GameObject host;
        private DamageState damageState;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("DamageStateTests");
            damageState = host.AddComponent<DamageState>();
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
                Object.DestroyImmediate(host);
        }

        [Test]
        public void ZeroDamage_StaysNormal()
        {
            damageState.Reset();

            Assert.That(damageState.CurrentDamage, Is.EqualTo(0f));
            Assert.That(damageState.CurrentState, Is.EqualTo(DamageVisualState.Normal));
        }

        [Test]
        public void ApplyDamage_UsesConfigurableThresholds()
        {
            damageState.ApplyDamage(24f);
            Assert.That(damageState.CurrentState, Is.EqualTo(DamageVisualState.Normal));

            damageState.ApplyDamage(1f);
            Assert.That(damageState.CurrentState, Is.EqualTo(DamageVisualState.Damaged));

            damageState.ApplyDamage(50f);
            Assert.That(damageState.CurrentState, Is.EqualTo(DamageVisualState.Destroyed));
        }

        [Test]
        public void SetState_UpdatesDamageToThreshold()
        {
            damageState.SetState(DamageVisualState.Damaged);

            Assert.That(damageState.CurrentDamage, Is.EqualTo(damageState.Thresholds.DamagedThreshold));
            Assert.That(damageState.CurrentState, Is.EqualTo(DamageVisualState.Damaged));
        }

        [Test]
        public void Reset_ReturnsToNormal()
        {
            damageState.SetState(DamageVisualState.Destroyed);
            damageState.Reset();

            Assert.That(damageState.CurrentDamage, Is.EqualTo(0f));
            Assert.That(damageState.CurrentState, Is.EqualTo(DamageVisualState.Normal));
        }
    }
}
