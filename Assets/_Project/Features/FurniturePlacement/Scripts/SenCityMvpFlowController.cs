using System;
using System.Collections;
using SenCity.Core.Pet;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SenCity.Features.FurniturePlacement
{
    public sealed class SenCityMvpFlowController : MonoBehaviour
    {
        private const string OnboardingCompletedKey = "SenCity.Onboarding.Completed";

        [Header("Screens")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject onboardingPanel;
        [SerializeField] private GameObject mainStudioPanel;
        [SerializeField] private GameObject buildDrawerPanel;
        [SerializeField] private GameObject petCardPanel;

        [Header("Popups")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private GameObject deleteConfirmationPanel;
        [SerializeField] private GameObject exitConfirmationPanel;
        [SerializeField] private Text selectedItemNameText;
        [SerializeField] private Text selectedItemDetailsText;

        [Header("Navigation")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button focusButton;
        [SerializeField] private Button petButton;
        [SerializeField] private Button buildBackButton;
        [SerializeField] private Button petBackButton;
        [SerializeField] private Button startButton;

        [Header("Placement")]
        [SerializeField] private Button placeButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button moveSelectedButton;
        [SerializeField] private Button rotateSelectedButton;
        [SerializeField] private Button deleteSelectedButton;
        [SerializeField] private Button closeSelectionButton;
        [SerializeField] private Button confirmDeleteButton;
        [SerializeField] private Button cancelDeleteButton;
        [SerializeField] private Text placementStatusText;

        [Header("Pet")]
        [SerializeField] private Button feedButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button sleepButton;
        [SerializeField] private Text petStateText;
        [SerializeField] private MonoBehaviour petControllerSource;

        [Header("Exit")]
        [SerializeField] private Button confirmExitButton;
        [SerializeField] private Button cancelExitButton;

        [Header("Runtime")]
        [SerializeField] private Text toastText;
        [SerializeField] private FurniturePlacementRuntime furnitureRuntime;
        [SerializeField] private SenCityRoomCameraFocus roomCameraFocus;
        [SerializeField, Min(0f)] private float loadingDuration = 0.7f;
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;

        private ISenCityPetCommands petCommands;
        private bool bootCompleted;
        private PlacementSession trackedPlacementSession;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            petCommands = petControllerSource as ISenCityPetCommands;

            Wire(buildButton, ShowBuild);
            Wire(focusButton, StartFocus);
            Wire(petButton, ShowPet);
            Wire(buildBackButton, HandleBack);
            Wire(petBackButton, HandleBack);
            Wire(startButton, FinishOnboarding);

            Wire(placeButton, ConfirmPlacement);
            Wire(rotateButton, () => furnitureRuntime?.RotatePreview());
            Wire(cancelButton, CancelPlacement);
            Wire(moveSelectedButton, MoveSelected);
            Wire(rotateSelectedButton, RotateSelected);
            Wire(deleteSelectedButton, RequestDeleteSelected);
            Wire(closeSelectionButton, CloseSelection);
            Wire(confirmDeleteButton, ConfirmDeleteSelected);
            Wire(cancelDeleteButton, CancelDeleteSelected);

            Wire(feedButton, () => RunPetCommand(commands => commands.Feed()));
            Wire(playButton, () => RunPetCommand(commands => commands.Play()));
            Wire(sleepButton, () => RunPetCommand(commands => commands.Sleep()));

            Wire(confirmExitButton, QuitApplication);
            Wire(cancelExitButton, () => SetPanel(exitConfirmationPanel, false));

            SetPanel(loadingPanel, true);
            SetPanel(onboardingPanel, false);
            SetPanel(mainStudioPanel, false);
            SetPanel(buildDrawerPanel, false);
            SetPanel(petCardPanel, false);
            CloseAllPopups(false);
            SetPanel(toastText != null ? toastText.gameObject : null, false);
            RefreshPlacementControls(null);
        }

        private IEnumerator Start()
        {
            yield return null;
            if (loadingDuration > 0f)
                yield return new WaitForSecondsRealtime(loadingDuration);

            bootCompleted = true;
            SetPanel(loadingPanel, false);
            if (PlayerPrefs.GetInt(OnboardingCompletedKey, 0) == 0)
                BeginOnboarding();
            else
                ShowMain();
        }

        private void OnEnable()
        {
            if (furnitureRuntime != null)
            {
                furnitureRuntime.SessionChanged += HandlePlacementSessionChanged;
                furnitureRuntime.SelectedObjectChanged += HandleSelectedObjectChanged;
                furnitureRuntime.StoreConfirmationRequested += HandleStoreConfirmationRequested;
                furnitureRuntime.ToastRequested += ShowToast;
            }

            if (petCommands != null)
                petCommands.StateChanged += HandlePetStateChanged;
        }

        private void OnDisable()
        {
            if (furnitureRuntime != null)
            {
                furnitureRuntime.SessionChanged -= HandlePlacementSessionChanged;
                furnitureRuntime.SelectedObjectChanged -= HandleSelectedObjectChanged;
                furnitureRuntime.StoreConfirmationRequested -= HandleStoreConfirmationRequested;
                furnitureRuntime.ToastRequested -= ShowToast;
            }

            if (petCommands != null)
                petCommands.StateChanged -= HandlePetStateChanged;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (bootCompleted && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                HandleBack();
        }

        public void ShowMain()
        {
            CancelActivePlacementIfNeeded();
            CloseAllPopups(true);
            SetPanel(loadingPanel, false);
            SetPanel(onboardingPanel, false);
            SetPanel(mainStudioPanel, true);
            SetPanel(buildDrawerPanel, false);
            SetPanel(petCardPanel, false);
        }

        public void ShowBuild()
        {
            CloseAllPopups(true);
            SetPanel(mainStudioPanel, false);
            SetPanel(buildDrawerPanel, true);
            SetPanel(petCardPanel, false);
            SetPlacementStatus("Choose an item, then tap the room to position it.", false);
        }

        public void ShowPet()
        {
            CancelActivePlacementIfNeeded();
            CloseAllPopups(true);
            SetPanel(mainStudioPanel, false);
            SetPanel(buildDrawerPanel, false);
            SetPanel(petCardPanel, true);
            if (petCommands != null)
                HandlePetStateChanged(petCommands.CurrentStateLabel);
        }

        public void BeginOnboarding()
        {
            SetPanel(loadingPanel, false);
            SetPanel(onboardingPanel, true);
            SetPanel(mainStudioPanel, false);
            SetPanel(buildDrawerPanel, false);
            SetPanel(petCardPanel, false);
        }

        public void HandleBack()
        {
            if (IsOpen(exitConfirmationPanel))
            {
                SetPanel(exitConfirmationPanel, false);
                return;
            }

            if (IsOpen(deleteConfirmationPanel))
            {
                CancelDeleteSelected();
                return;
            }

            if (IsOpen(selectionPanel))
            {
                CloseSelection();
                return;
            }

            if (furnitureRuntime != null && furnitureRuntime.HasActiveSession)
            {
                CancelPlacement();
                return;
            }

            if (IsOpen(buildDrawerPanel) || IsOpen(petCardPanel))
            {
                ShowMain();
                return;
            }

            if (IsOpen(onboardingPanel))
                return;

            SetPanel(exitConfirmationPanel, true);
        }

        public void ShowToast(string message)
        {
            if (toastText == null || string.IsNullOrWhiteSpace(message))
                return;

            toastText.text = message;
            toastText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideToast));
            Invoke(nameof(HideToast), 2.5f);
        }

        private void FinishOnboarding()
        {
            PlayerPrefs.SetInt(OnboardingCompletedKey, 1);
            PlayerPrefs.Save();
            ShowMain();
        }

        private void StartFocus()
        {
            ShowToast("A calm focus session is ready.");
        }

        private void ConfirmPlacement()
        {
            if (furnitureRuntime == null || !furnitureRuntime.HasActiveSession)
            {
                ShowToast("Select an item before confirming.");
                return;
            }

            if (!furnitureRuntime.Confirm())
                ShowToast("Move the item to a valid green position first.");
        }

        private void CancelPlacement()
        {
            furnitureRuntime?.Cancel();
            roomCameraFocus?.Restore();
            SetPanel(deleteConfirmationPanel, false);
        }

        private void MoveSelected()
        {
            if (furnitureRuntime == null || !furnitureRuntime.BeginMoveSelected())
            {
                ShowToast("Select furniture to move first.");
                return;
            }

            SetPanel(selectionPanel, false);
            roomCameraFocus?.Restore();
            ShowToast("Drag the preview, then tap Confirm.");
        }

        private void RotateSelected()
        {
            if (furnitureRuntime == null)
                return;

            if (!furnitureRuntime.HasActiveSession && !furnitureRuntime.BeginMoveSelected())
            {
                ShowToast("Select furniture to rotate first.");
                return;
            }

            SetPanel(selectionPanel, false);
            roomCameraFocus?.Restore();
            furnitureRuntime.RotatePreview();
            ShowToast("Rotated 90 degrees. Tap Confirm to save.");
        }

        private void RequestDeleteSelected()
        {
            if (furnitureRuntime == null || !furnitureRuntime.RequestStoreSelected())
                return;

            SetPanel(selectionPanel, false);
            SetPanel(deleteConfirmationPanel, true);
        }

        private void ConfirmDeleteSelected()
        {
            if (furnitureRuntime == null)
                return;

            if (furnitureRuntime.ConfirmStoreSelected())
            {
                SetPanel(deleteConfirmationPanel, false);
                roomCameraFocus?.Restore();
            }
        }

        private void CancelDeleteSelected()
        {
            furnitureRuntime?.Cancel();
            SetPanel(deleteConfirmationPanel, false);
            PlacedFurnitureObject selected = furnitureRuntime != null ? furnitureRuntime.SelectedObject : null;
            if (selected != null && IsOpen(buildDrawerPanel))
                ShowSelectionFor(selected);
        }

        private void CloseSelection()
        {
            SetPanel(selectionPanel, false);
            roomCameraFocus?.Restore();
            furnitureRuntime?.DeselectSelected();
        }

        private void HandleSelectedObjectChanged(PlacedFurnitureObject placedObject)
        {
            if (placedObject == null)
            {
                SetPanel(selectionPanel, false);
                roomCameraFocus?.Restore();
                return;
            }

            string displayName = placedObject.Item != null ? placedObject.Item.DisplayName : "Furniture";
            petCommands?.GoToTarget(placedObject.transform, displayName);

            if (IsOpen(buildDrawerPanel) && (furnitureRuntime == null || !furnitureRuntime.HasActiveSession))
                ShowSelectionFor(placedObject);
        }

        private void ShowSelectionFor(PlacedFurnitureObject placedObject)
        {
            if (placedObject == null)
                return;

            if (selectedItemNameText != null)
                selectedItemNameText.text = placedObject.Item != null ? placedObject.Item.DisplayName : "Furniture";

            if (selectedItemDetailsText != null && placedObject.Data != null)
            {
                Vector2Int cell = placedObject.Data.OriginCell;
                selectedItemDetailsText.text = $"Grid {cell.x + 1}, {cell.y + 1}  •  Rotation {placedObject.Data.RotationDegrees}°";
            }

            SetPanel(selectionPanel, true);
            roomCameraFocus?.Focus(placedObject.transform);
        }

        private void HandleStoreConfirmationRequested(PlacedFurnitureObject placedObject)
        {
            SetPanel(selectionPanel, false);
            SetPanel(deleteConfirmationPanel, placedObject != null);
        }

        private void HandlePlacementSessionChanged(PlacementSession session)
        {
            RefreshPlacementControls(session);

            if (session == null)
            {
                trackedPlacementSession = null;
                PlacedFurnitureObject selected = furnitureRuntime != null ? furnitureRuntime.SelectedObject : null;
                if (selected != null && IsOpen(buildDrawerPanel) && !IsOpen(deleteConfirmationPanel))
                    ShowSelectionFor(selected);
                return;
            }

            if (session.State == PlacementSessionState.RemoveConfirm)
            {
                trackedPlacementSession = session;
                return;
            }

            if (!ReferenceEquals(trackedPlacementSession, session))
            {
                SetPanel(selectionPanel, false);
                SetPanel(deleteConfirmationPanel, false);
                roomCameraFocus?.RestoreImmediate();
            }

            trackedPlacementSession = session;
        }

        private void RefreshPlacementControls(PlacementSession session)
        {
            bool active = session != null && session.State != PlacementSessionState.RemoveConfirm;
            bool valid = active && furnitureRuntime != null && furnitureRuntime.CanConfirmActiveSession();
            if (placeButton != null && placeButton.interactable != valid)
                placeButton.interactable = valid;
            if (rotateButton != null && rotateButton.interactable != active)
                rotateButton.interactable = active;
            if (cancelButton != null && cancelButton.interactable != active)
                cancelButton.interactable = active;

            if (!active)
            {
                SetPlacementStatus("Tap a card or select furniture in the room.", false);
                return;
            }

            string message = valid
                ? "Valid position • Tap Confirm to apply"
                : session.LastValidation.Message;
            SetPlacementStatus(message, valid);
        }

        private void SetPlacementStatus(string message, bool valid)
        {
            if (placementStatusText == null)
                return;

            Color color = valid
                ? new Color(0.25f, 0.42f, 0.16f)
                : new Color(0.52f, 0.25f, 0.34f);
            if (placementStatusText.text != message)
                placementStatusText.text = message;
            if (placementStatusText.color != color)
                placementStatusText.color = color;
        }

        private void RunPetCommand(Action<ISenCityPetCommands> command)
        {
            if (petCommands == null)
            {
                ShowToast("Mochi AI is not available yet.");
                return;
            }

            command?.Invoke(petCommands);
            HandlePetStateChanged(petCommands.CurrentStateLabel);
        }

        private void HandlePetStateChanged(string state)
        {
            if (petStateText != null)
                petStateText.text = state;
            if (IsOpen(petCardPanel))
                ShowToast(state);
        }

        private void CloseAllPopups(bool clearSelection)
        {
            SetPanel(selectionPanel, false);
            SetPanel(deleteConfirmationPanel, false);
            SetPanel(exitConfirmationPanel, false);
            roomCameraFocus?.Restore();
            if (clearSelection && furnitureRuntime != null && !furnitureRuntime.HasActiveSession)
                furnitureRuntime.DeselectSelected();
        }

        private void CancelActivePlacementIfNeeded()
        {
            if (furnitureRuntime != null && furnitureRuntime.HasActiveSession)
                furnitureRuntime.Cancel();
        }

        private void HideToast()
        {
            if (toastText != null)
                toastText.gameObject.SetActive(false);
        }

        private static void QuitApplication()
        {
            Application.Quit();
        }

        private static bool IsOpen(GameObject panel)
        {
            return panel != null && panel.activeInHierarchy;
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        private static void SetPanel(GameObject panel, bool value)
        {
            if (panel != null && panel.activeSelf != value)
                panel.SetActive(value);
        }
    }
}
