using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenCity.Core.DamageVisual
{
    internal static class DamageStateDebugUiResolver
    {
        private static readonly string[] UiRootNames =
        {
            "Damage Debug HUD",
            "Damage Debug Panel",
            "Damage Debug",
            "DamageDebug"
        };

        public static bool TryResolve(
            Transform searchRoot,
            ref Button normalButton,
            ref Button damagedButton,
            ref Button destroyedButton,
            ref Button applyDamageButton,
            ref Button resetButton,
            ref TMP_Text currentStateText,
            ref TMP_Text currentDamageText,
            ref TMP_Text thresholdText,
            ref Text currentStateLegacyText,
            ref Text currentDamageLegacyText,
            ref Text thresholdLegacyText)
        {
            Transform root = searchRoot != null ? searchRoot : FindUiRoot();

            normalButton ??= FindButton(root, "Normal", "Normal Button") ?? FindButtonInScene("Normal", "Normal Button");
            damagedButton ??= FindButton(root, "Damaged", "Damaged Button") ?? FindButtonInScene("Damaged", "Damaged Button");
            destroyedButton ??= FindButton(root, "Destroyed", "Destroyed Button") ?? FindButtonInScene("Destroyed", "Destroyed Button");
            applyDamageButton ??= FindButton(root, "Apply Damage", "Apply Damage Button") ?? FindButtonInScene("Apply Damage", "Apply Damage Button");
            resetButton ??= FindButton(root, "Reset", "Reset Button") ?? FindButtonInScene("Reset", "Reset Button");

            if (currentStateText == null && currentStateLegacyText == null)
            {
                currentStateText = FindTmpText(root, "Current State", "Current State Text") ?? FindTmpTextInScene("Current State", "Current State Text");
                currentStateLegacyText = FindLegacyText(root, "Current State", "Current State Text") ?? FindLegacyTextInScene("Current State", "Current State Text");
            }

            if (currentDamageText == null && currentDamageLegacyText == null)
            {
                currentDamageText = FindTmpText(root, "Current Damage", "Current Damage Text") ?? FindTmpTextInScene("Current Damage", "Current Damage Text");
                currentDamageLegacyText = FindLegacyText(root, "Current Damage", "Current Damage Text") ?? FindLegacyTextInScene("Current Damage", "Current Damage Text");
            }

            if (thresholdText == null && thresholdLegacyText == null)
            {
                thresholdText = FindTmpText(root, "Threshold", "Threshold Text") ?? FindTmpTextInScene("Threshold", "Threshold Text");
                thresholdLegacyText = FindLegacyText(root, "Threshold", "Threshold Text") ?? FindLegacyTextInScene("Threshold", "Threshold Text");
            }

            return normalButton != null
                   || damagedButton != null
                   || destroyedButton != null
                   || applyDamageButton != null
                   || resetButton != null
                   || currentStateText != null
                   || currentDamageText != null
                   || thresholdText != null
                   || currentStateLegacyText != null
                   || currentDamageLegacyText != null
                   || thresholdLegacyText != null;
        }

        private static Transform FindUiRoot()
        {
            foreach (string rootName in UiRootNames)
            {
                GameObject rootObject = GameObject.Find(rootName);
                if (rootObject != null)
                    return rootObject.transform;
            }

            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                foreach (string rootName in UiRootNames)
                {
                    Transform child = FindChildRecursive(canvas.transform, rootName);
                    if (child != null)
                        return child;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (string.Equals(parent.name, childName, StringComparison.OrdinalIgnoreCase))
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Button FindButton(Transform root, params string[] names)
        {
            if (root == null)
                return null;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null)
                    continue;

                if (MatchesName(button.gameObject.name, names) || MatchesLabel(button, names))
                    return button;
            }

            return null;
        }

        private static TMP_Text FindTmpText(Transform root, params string[] names)
        {
            if (root == null)
                return null;

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text != null && MatchesName(text.gameObject.name, names))
                    return text;
            }

            return null;
        }

        private static Text FindLegacyText(Transform root, params string[] names)
        {
            if (root == null)
                return null;

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (text != null && MatchesName(text.gameObject.name, names))
                    return text;
            }

            return null;
        }

        private static Button FindButtonInScene(params string[] names)
        {
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Button button in buttons)
            {
                if (button == null)
                    continue;

                if (MatchesName(button.gameObject.name, names) || MatchesLabel(button, names))
                    return button;
            }

            return null;
        }

        private static TMP_Text FindTmpTextInScene(params string[] names)
        {
            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (TMP_Text text in texts)
            {
                if (text != null && MatchesName(text.gameObject.name, names))
                    return text;
            }

            return null;
        }

        private static Text FindLegacyTextInScene(params string[] names)
        {
            Text[] texts = UnityEngine.Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Text text in texts)
            {
                if (text != null && MatchesName(text.gameObject.name, names))
                    return text;
            }

            return null;
        }

        private static bool MatchesName(string objectName, string[] names)
        {
            foreach (string name in names)
            {
                if (string.Equals(objectName, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool MatchesLabel(Button button, string[] names)
        {
            TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
            if (tmpLabel != null)
            {
                foreach (string name in names)
                {
                    if (string.Equals(tmpLabel.text, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            Text legacyLabel = button.GetComponentInChildren<Text>(true);
            if (legacyLabel != null)
            {
                foreach (string name in names)
                {
                    if (string.Equals(legacyLabel.text, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }
    }
}
