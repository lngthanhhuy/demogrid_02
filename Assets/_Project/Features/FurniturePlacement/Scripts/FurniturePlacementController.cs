using System;
using SenCity.Core.Grid;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public class FurniturePlacementController : MonoBehaviour
    {
        [SerializeField] private SenCityGridProfile gridProfile;

        private GridOccupancyMap occupancyMap;
        private PlacementValidator validator;
        private PlacementSession activeSession;

        public event Action<PlacementSession> SessionChanged;
        public event Action<FurnitureInstanceData> FurniturePlaced;
        public event Action<FurnitureInstanceData> FurnitureMoved;
        public event Action<FurnitureInstanceData> FurnitureStored;
        public event Action<string> PlacementFailed;

        public PlacementSessionState State => activeSession?.State ?? PlacementSessionState.Idle;
        public PlacementSession ActiveSession => activeSession;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        public void Configure(SenCityGridProfile profile)
        {
            gridProfile = profile;
            InitializeIfNeeded(force: true);
        }

        public bool TryBeginPlaceNew(FurnitureItemDefinition item, Vector2Int originCell)
        {
            InitializeIfNeeded();
            if (!HasPlacementGrid())
                return Fail("Missing placement grid.");

            if (!CanStartSession())
                return Fail("A placement session is already active.");

            if (item == null)
                return Fail("Missing furniture item.");

            activeSession = new PlacementSession(PlacementSessionState.PlacementNew, item, originCell, 0);
            ValidateActiveSession();
            NotifySessionChanged();
            return true;
        }

        public bool TryBeginMoveExisting(FurnitureInstanceData instance, FurnitureItemDefinition item)
        {
            InitializeIfNeeded();
            if (!HasPlacementGrid())
                return Fail("Missing placement grid.");

            if (!CanStartSession())
                return Fail("A placement session is already active.");

            if (instance == null || item == null)
                return Fail("Missing placed furniture data.");

            activeSession = new PlacementSession(
                PlacementSessionState.MovingExisting,
                item,
                instance.OriginCell,
                instance.RotationDegrees,
                instance);
            ValidateActiveSession();
            NotifySessionChanged();
            return true;
        }

        public void MovePreview(Vector2Int originCell)
        {
            if (activeSession == null)
                return;

            activeSession.MovePreview(originCell);
            ValidateActiveSession();
            NotifySessionChanged();
        }

        public void RotatePreviewClockwise()
        {
            if (activeSession == null)
                return;

            activeSession.RotateClockwise();
            ValidateActiveSession();
            NotifySessionChanged();
        }

        public bool ConfirmActiveSession()
        {
            if (activeSession == null)
                return Fail("No active placement session.");

            ValidateActiveSession();
            if (!activeSession.LastValidation.IsValid)
                return Fail(activeSession.LastValidation.Message);

            if (activeSession.IsMoveExisting)
                return CommitMoveExisting();

            return CommitPlaceNew();
        }

        public void CancelActiveSession()
        {
            if (activeSession == null)
                return;

            activeSession.SetState(PlacementSessionState.Idle);
            activeSession = null;
            NotifySessionChanged();
        }

        public bool StorePlacedFurniture(FurnitureInstanceData instance, FurnitureItemDefinition item)
        {
            InitializeIfNeeded();
            if (!HasPlacementGrid())
                return Fail("Missing placement grid.");

            if (instance == null || item == null)
                return Fail("Missing placed furniture data.");

            if (!item.CanStore)
                return Fail("Cannot store this item.");

            occupancyMap.Release(instance.InstanceId);
            instance.SetState(FurniturePlacementState.Stored);
            FurnitureStored?.Invoke(instance);
            return true;
        }

        public bool RegisterPlacedFurniture(FurnitureInstanceData instance)
        {
            InitializeIfNeeded();
            if (instance == null || !HasPlacementGrid())
                return false;

            occupancyMap.Reserve(instance.InstanceId, instance.OriginCell, instance.Footprint, instance.RotationDegrees);
            return true;
        }

        private bool CommitPlaceNew()
        {
            FurnitureInstanceData instance = new FurnitureInstanceData(
                Guid.NewGuid().ToString("N"),
                activeSession.Item.ItemId,
                activeSession.OriginCell,
                activeSession.Item.Footprint,
                activeSession.RotationDegrees);

            occupancyMap.Reserve(instance.InstanceId, instance.OriginCell, instance.Footprint, instance.RotationDegrees);
            activeSession.SetState(PlacementSessionState.Saving);
            FurniturePlaced?.Invoke(instance);
            activeSession = null;
            NotifySessionChanged();
            return true;
        }

        private bool CommitMoveExisting()
        {
            FurnitureInstanceData instance = activeSession.SourceInstance;
            occupancyMap.Release(instance.InstanceId);
            instance.MoveTo(activeSession.OriginCell, activeSession.RotationDegrees);
            occupancyMap.Reserve(instance.InstanceId, instance.OriginCell, instance.Footprint, instance.RotationDegrees);

            activeSession.SetState(PlacementSessionState.Saving);
            FurnitureMoved?.Invoke(instance);
            activeSession = null;
            NotifySessionChanged();
            return true;
        }

        private void ValidateActiveSession()
        {
            if (activeSession == null)
                return;

            if (!HasPlacementGrid())
            {
                activeSession.ApplyValidation(PlacementValidationResult.Invalid(
                    PlacementValidationFailure.MissingGrid,
                    "Missing placement grid."));
                return;
            }

            PlacementValidationResult result = validator.Validate(
                activeSession.Item,
                activeSession.OriginCell,
                activeSession.RotationDegrees,
                activeSession.IgnoredInstanceId);
            activeSession.ApplyValidation(result);
        }

        private void InitializeIfNeeded(bool force = false)
        {
            if (gridProfile == null)
                return;

            if (force || occupancyMap == null)
            {
                occupancyMap = new GridOccupancyMap(gridProfile.columns, gridProfile.rows);
                validator = new PlacementValidator(gridProfile, occupancyMap);
            }
        }

        private bool CanStartSession()
        {
            return activeSession == null || activeSession.State == PlacementSessionState.Idle;
        }

        private bool HasPlacementGrid()
        {
            return gridProfile != null && occupancyMap != null && validator != null;
        }

        private bool Fail(string message)
        {
            PlacementFailed?.Invoke(message);
            return false;
        }

        private void NotifySessionChanged()
        {
            SessionChanged?.Invoke(activeSession);
        }
    }
}
