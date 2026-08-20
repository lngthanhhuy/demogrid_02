using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenCity.Core.DamageVisual
{
    [DisallowMultipleComponent]
    public class DamageStateDebugController : MonoBehaviour
    {
        [Header("Damage System")]
        [SerializeField] private DamageState damageState;

        [Header("Debug Settings")]
        [SerializeField, Min(0f)] private float damagePerApply = 10f;

        [Header("UI Search")]
        [SerializeField] private Transform uiSearchRoot;

        [Header("UI (optional manual override)")]
        [SerializeField] private Button normalButton;
        [SerializeField] private Button damagedButton;
        [SerializeField] private Button destroyedButton;
        [SerializeField] private Button applyDamageButton;
        [SerializeField] private Button resetButton;

        [Header("UI Display (optional manual override)")]
        [SerializeField] private TMP_Text currentStateText;
        [SerializeField] private TMP_Text currentDamageText;
        [SerializeField] private TMP_Text thresholdText;

        [SerializeField] private Text currentStateLegacyText;
        [SerializeField] private Text currentDamageLegacyText;
        [SerializeField] private Text thresholdLegacyText;

        private bool uiResolved;

        private void Reset()
        {
            damageState = GetComponent<DamageState>();

            Debug.Log(
                damageState != null
                    ? "[DamageDebug] DamageState assigned automatically."
                    : "[DamageDebug] DamageState not found on this GameObject.",
                this
            );
        }

        private void Awake()
        {
            Debug.Log("[DamageDebug] Initializing DamageStateDebugController...", this);

            if (damageState == null)
            {
                damageState = GetComponent<DamageState>();

                if (damageState != null)
                {
                    Debug.Log(
                        "[DamageDebug] DamageState found on current GameObject.",
                        this
                    );
                }
            }

            if (damageState == null)
            {
                damageState = FindAnyObjectByType<DamageState>();

                if (damageState != null)
                {
                    Debug.Log(
                        $"[DamageDebug] DamageState found in scene: {damageState.name}",
                        damageState
                    );
                }
            }

            if (damageState == null)
            {
                Debug.LogError(
                    "[DamageDebug] DamageState could not be found.",
                    this
                );
            }

            ResolveUiReferences();
        }

        private void OnEnable()
        {
            if (damageState == null)
            {
                Debug.LogWarning(
                    "[DamageDebug] Cannot subscribe events because DamageState is missing.",
                    this
                );

                return;
            }

            damageState.StateChanged += HandleDamageChanged;
            damageState.DamageChanged += HandleDamageChanged;

            Debug.Log(
                $"[DamageDebug] Event listeners registered. " +
                $"Current State: {damageState.CurrentState} | " +
                $"Current Damage: {damageState.CurrentDamage:0.##}",
                this
            );

            RefreshUI();
        }

        private void OnDisable()
        {
            if (damageState != null)
            {
                damageState.StateChanged -= HandleDamageChanged;
                damageState.DamageChanged -= HandleDamageChanged;
            }

            UnbindUI();

            Debug.Log("[DamageDebug] Controller disabled.", this);
        }

        private void ResolveUiReferences()
        {
            if (uiResolved)
                return;

            uiResolved = DamageStateDebugUiResolver.TryResolve(
                uiSearchRoot,
                ref normalButton,
                ref damagedButton,
                ref destroyedButton,
                ref applyDamageButton,
                ref resetButton,
                ref currentStateText,
                ref currentDamageText,
                ref thresholdText,
                ref currentStateLegacyText,
                ref currentDamageLegacyText,
                ref thresholdLegacyText
            );

            if (uiResolved)
            {
                Debug.Log(
                    "[DamageDebug] UI references resolved successfully.",
                    this
                );
            }
            else
            {
                Debug.LogWarning(
                    "[DamageDebug] UI references could not be fully resolved.",
                    this
                );
            }

            BindUI();
        }

        private void BindUI()
        {
            if (normalButton != null)
            {
                normalButton.onClick.AddListener(SetNormal);
                Debug.Log("[DamageDebug] Normal button bound.", this);
            }

            if (damagedButton != null)
            {
                damagedButton.onClick.AddListener(SetDamaged);
                Debug.Log("[DamageDebug] Damaged button bound.", this);
            }

            if (destroyedButton != null)
            {
                destroyedButton.onClick.AddListener(SetDestroyed);
                Debug.Log("[DamageDebug] Destroyed button bound.", this);
            }

            if (applyDamageButton != null)
            {
                applyDamageButton.onClick.AddListener(ApplyDamage);
                Debug.Log("[DamageDebug] Apply Damage button bound.", this);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetDamage);
                Debug.Log("[DamageDebug] Reset button bound.", this);
            }
        }

        private void UnbindUI()
        {
            if (normalButton != null)
                normalButton.onClick.RemoveListener(SetNormal);

            if (damagedButton != null)
                damagedButton.onClick.RemoveListener(SetDamaged);

            if (destroyedButton != null)
                destroyedButton.onClick.RemoveListener(SetDestroyed);

            if (applyDamageButton != null)
                applyDamageButton.onClick.RemoveListener(ApplyDamage);

            if (resetButton != null)
                resetButton.onClick.RemoveListener(ResetDamage);
        }

        public void SetNormal()
        {
            if (damageState == null)
            {
                Debug.LogWarning(
                    "[DamageDebug] SetNormal failed: DamageState is missing.",
                    this
                );

                return;
            }

            damageState.SetState(DamageVisualState.Normal);

            Debug.Log(
                $"[DamageDebug] State → Normal | " +
                $"Damage: {damageState.CurrentDamage:0.##}",
                this
            );
        }

        public void SetDamaged()
        {
            if (damageState == null)
            {
                Debug.LogWarning(
                    "[DamageDebug] SetDamaged failed: DamageState is missing.",
                    this
                );

                return;
            }

            damageState.SetState(DamageVisualState.Damaged);

            Debug.Log(
                $"[DamageDebug] State → Damaged | " +
                $"Damage: {damageState.CurrentDamage:0.##}",
                this
            );
        }

        public void SetDestroyed()
        {
            if (damageState == null)
            {
                Debug.LogWarning(
                    "[DamageDebug] SetDestroyed failed: DamageState is missing.",
                    this
                );

                return;
            }

            damageState.SetState(DamageVisualState.Destroyed);

            Debug.Log(
                $"[DamageDebug] State → Destroyed | " +
                $"Damage: {damageState.CurrentDamage:0.##}",
                this
            );
        }

        public void ApplyDamage()
        {
            if (damageState == null)
            {
                Debug.LogWarning(
                    "[DamageDebug] ApplyDamage failed: DamageState is missing.",
                    this
                );

                return;
            }

            float previousDamage = damageState.CurrentDamage;

            damageState.ApplyDamage(damagePerApply);

            Debug.Log(
                $"[DamageDebug] Apply Damage: +{damagePerApply:0.##} | " +
                $"Damage: {previousDamage:0.##} → {damageState.CurrentDamage:0.##} | " +
                $"State: {damageState.CurrentState}",
                this
            );
        }

        public void ResetDamage()
        {
            if (damageState == null)
            {
                Debug.LogWarning(
                    "[DamageDebug] Reset failed: DamageState is missing.",
                    this
                );

                return;
            }

            damageState.Reset();

            Debug.Log(
                $"[DamageDebug] Reset | " +
                $"Damage: {damageState.CurrentDamage:0.##} | " +
                $"State: {damageState.CurrentState}",
                this
            );
        }

        private void HandleDamageChanged(DamageVisualState state)
        {
            Debug.Log(
                $"[DamageDebug] StateChanged event → {state}",
                this
            );

            RefreshUI();
        }

        private void HandleDamageChanged(float damage)
        {
            Debug.Log(
                $"[DamageDebug] DamageChanged event → {damage:0.##}",
                this
            );

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (damageState == null)
                return;

            string stateLabel =
                $"Current State: {damageState.CurrentState}";

            string damageLabel =
                $"Current Damage: {damageState.CurrentDamage:0.##}";

            string thresholdLabel =
                $"Threshold: Damaged >= " +
                $"{damageState.Thresholds.DamagedThreshold:0.##}, " +
                $"Destroyed >= " +
                $"{damageState.Thresholds.DestroyedThreshold:0.##}";

            SetLabel(
                currentStateText,
                currentStateLegacyText,
                stateLabel
            );

            SetLabel(
                currentDamageText,
                currentDamageLegacyText,
                damageLabel
            );

            SetLabel(
                thresholdText,
                thresholdLegacyText,
                thresholdLabel
            );

            Debug.Log(
                $"[DamageDebug] UI Refresh | " +
                $"State: {damageState.CurrentState} | " +
                $"Damage: {damageState.CurrentDamage:0.##} | " +
                $"Thresholds: Damaged >= " +
                $"{damageState.Thresholds.DamagedThreshold:0.##}, " +
                $"Destroyed >= " +
                $"{damageState.Thresholds.DestroyedThreshold:0.##}",
                this
            );
        }

        private static void SetLabel(
            TMP_Text tmpText,
            Text legacyText,
            string value)
        {
            if (tmpText != null)
                tmpText.text = value;

            if (legacyText != null)
                legacyText.text = value;
        }
    }
}