using System;
using System.Collections.Generic;
using SenCity.Core.Grid;
using SenCity.Core.Save;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    [RequireComponent(typeof(FurniturePlacementController))]
    public class FurniturePlacementRuntime : MonoBehaviour
    {
        [SerializeField] private FurniturePlacementController controller;
        [SerializeField] private FurnitureInventoryRuntime inventory;
        [SerializeField] private SenCityGridProfile gridProfile;
        [SerializeField] private Transform placedRoot;
        [SerializeField] private Transform previewRoot;

        private readonly Dictionary<string, PlacedFurnitureObject> placedObjectsById = new Dictionary<string, PlacedFurnitureObject>();
        private FurnitureGhostPreview activeGhost;
        private PlacedFurnitureObject selectedObject;

        public event Action<PlacedFurnitureObject> SelectedObjectChanged;
        public event Action<string> ToastRequested;

        public bool HasActiveSession => controller != null && controller.ActiveSession != null;
        public PlacementSession ActiveSession => controller != null ? controller.ActiveSession : null;
        public PlacedFurnitureObject SelectedObject => selectedObject;

        private void Awake()
        {
            if (controller == null)
                controller = GetComponent<FurniturePlacementController>();

            if (inventory == null)
                inventory = FindAnyObjectByType<FurnitureInventoryRuntime>();

            controller.Configure(gridProfile);
            controller.SessionChanged += HandleSessionChanged;
            controller.FurniturePlaced += HandleFurniturePlaced;
            controller.FurnitureMoved += HandleFurnitureMoved;
            controller.FurnitureStored += HandleFurnitureStored;
            controller.PlacementFailed += HandlePlacementFailed;
        }

        private void OnDestroy()
        {
            if (controller == null)
                return;

            controller.SessionChanged -= HandleSessionChanged;
            controller.FurniturePlaced -= HandleFurniturePlaced;
            controller.FurnitureMoved -= HandleFurnitureMoved;
            controller.FurnitureStored -= HandleFurnitureStored;
            controller.PlacementFailed -= HandlePlacementFailed;
        }

        public bool BeginPlaceNew(FurnitureItemDefinition item, Vector2Int originCell)
        {
            if (inventory != null && inventory.GetQuantity(item) <= 0)
            {
                RequestToast("Vật phẩm đã hết trong Kho.");
                return false;
            }

            return controller.TryBeginPlaceNew(item, originCell);
        }

        public bool BeginMoveSelected()
        {
            if (selectedObject == null)
                return false;

            return controller.TryBeginMoveExisting(selectedObject.Data, selectedObject.Item);
        }

        public void SelectObject(PlacedFurnitureObject placedObject)
        {
            selectedObject = placedObject;
            SelectedObjectChanged?.Invoke(selectedObject);
        }

        public void MovePreview(Vector2Int originCell)
        {
            controller.MovePreview(originCell);
        }

        public void RotatePreview()
        {
            controller.RotatePreviewClockwise();
        }

        public bool Confirm()
        {
            return controller.ConfirmActiveSession();
        }

        public void Cancel()
        {
            controller.CancelActiveSession();
        }

        public bool StoreSelected()
        {
            if (selectedObject == null)
                return false;

            return controller.StorePlacedFurniture(selectedObject.Data, selectedObject.Item);
        }

        public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
        {
            cell = default;
            return gridProfile != null && gridProfile.WorldToCell(worldPosition, out cell);
        }

        public bool SaveTo(FurniturePlacementSaveService saveService)
        {
            if (saveService == null)
                return false;

            FurnitureInventorySnapshot inventorySnapshot = inventory != null
                ? inventory.CaptureSnapshot()
                : new FurnitureInventorySnapshot();
            return saveService.Save(CaptureRoomSnapshot(), inventorySnapshot);
        }

        public bool LoadFrom(FurniturePlacementSaveService saveService)
        {
            if (saveService == null || !saveService.TryLoad(out FurniturePlacementSavePayload payload))
                return false;

            if (inventory != null)
                inventory.ApplySnapshot(payload.inventory);

            RestoreRoomSnapshot(payload.roomLayout);
            return true;
        }

        public FurnitureRoomLayoutSnapshot CaptureRoomSnapshot()
        {
            var instances = new List<FurnitureInstanceSaveData>();
            foreach (PlacedFurnitureObject placedObject in placedObjectsById.Values)
            {
                FurnitureInstanceData data = placedObject.Data;
                if (data == null || data.State == FurniturePlacementState.Stored)
                    continue;

                instances.Add(FurnitureInstanceSaveData.FromInstance(data));
            }

            return new FurnitureRoomLayoutSnapshot(instances);
        }

        public void RestoreRoomSnapshot(FurnitureRoomLayoutSnapshot snapshot)
        {
            ClearPlacedObjects();
            if (snapshot == null || inventory == null)
                return;

            foreach (FurnitureInstanceSaveData saved in snapshot.instances)
            {
                FurnitureItemDefinition item = inventory.GetItem(saved.itemId);
                if (item == null)
                    continue;

                var data = saved.ToInstance(item.Footprint);
                SpawnPlacedObject(item, data);
                controller.RegisterPlacedFurniture(data);
            }
        }

        private void HandleSessionChanged(PlacementSession session)
        {
            if (session == null)
            {
                DestroyGhost();
                return;
            }

            EnsureGhost(session.Item);
            activeGhost.SetPose(gridProfile, session.OriginCell, session.Item.Footprint, session.RotationDegrees);
            activeGhost.SetValidity(session.LastValidation.IsValid);
        }

        private void HandleFurniturePlaced(FurnitureInstanceData instance)
        {
            if (activeGhost != null && activeGhost.TryGetComponent(out PlacedFurnitureObject _))
            {
                // Ghosts should never be promoted directly; keep preview and real object lifecycles separate.
            }

            FurnitureItemDefinition item = ActiveSession?.Item ?? inventory?.GetItem(instance.ItemId);
            if (item == null)
                return;

            if (inventory != null)
                inventory.TryConsume(item);

            SpawnPlacedObject(item, instance);
            DestroyGhost();
            RequestToast($"Đã đặt {item.DisplayName}.");
        }

        private void HandleFurnitureMoved(FurnitureInstanceData instance)
        {
            if (instance != null && placedObjectsById.TryGetValue(instance.InstanceId, out PlacedFurnitureObject placedObject))
                placedObject.ApplyPose(gridProfile);

            DestroyGhost();
            RequestToast("Đã cập nhật vị trí vật phẩm.");
        }

        private void HandleFurnitureStored(FurnitureInstanceData instance)
        {
            if (instance == null)
                return;

            if (placedObjectsById.TryGetValue(instance.InstanceId, out PlacedFurnitureObject placedObject))
            {
                if (inventory != null)
                    inventory.ReturnItem(placedObject.Item);

                if (selectedObject == placedObject)
                    SelectObject(null);

                placedObjectsById.Remove(instance.InstanceId);
                Destroy(placedObject.gameObject);
                RequestToast("Vật phẩm đã được đưa về Kho đồ.");
            }
        }

        private void HandlePlacementFailed(string message)
        {
            RequestToast(string.IsNullOrWhiteSpace(message) ? "Vị trí này không hợp lệ." : message);
        }

        private void EnsureGhost(FurnitureItemDefinition item)
        {
            if (activeGhost != null)
                return;

            GameObject ghostObject = item != null && item.Prefab != null
                ? Instantiate(item.Prefab, previewRoot != null ? previewRoot : transform)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            if (item == null || item.Prefab == null)
                ghostObject.transform.SetParent(previewRoot != null ? previewRoot : transform, false);

            ghostObject.name = item != null ? $"Ghost_{item.DisplayName}" : "Ghost_Furniture";
            SetCollidersEnabled(ghostObject, false);
            activeGhost = ghostObject.GetComponent<FurnitureGhostPreview>();
            if (activeGhost == null)
                activeGhost = ghostObject.AddComponent<FurnitureGhostPreview>();
        }

        private PlacedFurnitureObject SpawnPlacedObject(FurnitureItemDefinition item, FurnitureInstanceData instance)
        {
            GameObject prefab = item != null ? item.Prefab : null;
            GameObject placedObject = prefab != null
                ? Instantiate(prefab, placedRoot != null ? placedRoot : transform)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            if (prefab == null)
                placedObject.transform.SetParent(placedRoot != null ? placedRoot : transform, false);

            placedObject.name = $"{item.DisplayName}_{instance.InstanceId}";
            var runtimeObject = placedObject.GetComponent<PlacedFurnitureObject>();
            if (runtimeObject == null)
                runtimeObject = placedObject.AddComponent<PlacedFurnitureObject>();

            runtimeObject.Initialize(item, instance, gridProfile);
            placedObjectsById[instance.InstanceId] = runtimeObject;
            return runtimeObject;
        }

        private void DestroyGhost()
        {
            if (activeGhost == null)
                return;

            Destroy(activeGhost.gameObject);
            activeGhost = null;
        }

        private void ClearPlacedObjects()
        {
            foreach (PlacedFurnitureObject placedObject in placedObjectsById.Values)
            {
                if (placedObject != null)
                    Destroy(placedObject.gameObject);
            }

            placedObjectsById.Clear();
            SelectObject(null);
        }

        private static void SetCollidersEnabled(GameObject root, bool enabled)
        {
            foreach (Collider objectCollider in root.GetComponentsInChildren<Collider>(true))
                objectCollider.enabled = enabled;
        }

        private void RequestToast(string message)
        {
            ToastRequested?.Invoke(message);
        }
    }
}
