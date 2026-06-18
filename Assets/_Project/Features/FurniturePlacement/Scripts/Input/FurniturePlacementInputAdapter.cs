using SenCity.Features.FurniturePlacement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SenCity.Features.FurniturePlacement.Input
{
    public class FurniturePlacementInputAdapter : MonoBehaviour
    {
        [SerializeField] private FurniturePlacementRuntime runtime;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask placedFurnitureMask = ~0;
        [SerializeField] private float placementPlaneY;
        [SerializeField] private float holdToMoveSeconds = 0.25f;
        [SerializeField] private bool confirmOnMouseRelease = true;

        private float pointerDownTime;
        private PlacedFurnitureObject pressedObject;
        private bool moveStartedFromHold;

        private void Awake()
        {
            if (runtime == null)
                runtime = FindAnyObjectByType<FurniturePlacementRuntime>();

            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        private void Update()
        {
            if (runtime == null || worldCamera == null)
                return;

            HandleKeyboard();
            HandlePointer();
        }

        private void HandleKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                runtime.Cancel();

            if (keyboard.rKey.wasPressedThisFrame)
                runtime.RotatePreview();

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                runtime.Confirm();

            if (keyboard.mKey.wasPressedThisFrame)
                runtime.BeginMoveSelected();

            if (keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
                runtime.RequestStoreSelected();

            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            {
                if (keyboard.sKey.wasPressedThisFrame)
                    runtime.SaveCurrentLayout();

                if (keyboard.lKey.wasPressedThisFrame)
                    runtime.LoadSavedLayout();
            }
        }

        private void HandlePointer()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                runtime.HoverObject(null);
                return;
            }

            if (TryGetPointerCell(out Vector2Int cell) && runtime.HasActiveSession)
                runtime.MovePreview(cell);

            PlacedFurnitureObject pointerObject = !runtime.HasActiveSession ? RaycastPlacedFurniture() : null;
            runtime.HoverObject(pointerObject);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                pointerDownTime = Time.time;
                pressedObject = pointerObject;
                moveStartedFromHold = false;

                if (!runtime.HasActiveSession && pressedObject != null)
                    runtime.SelectObject(pressedObject);
                else if (!runtime.HasActiveSession)
                    runtime.SelectObject(null);
            }

            if (mouse.leftButton.isPressed && pressedObject != null && !runtime.HasActiveSession)
            {
                if (!moveStartedFromHold && Time.time - pointerDownTime >= holdToMoveSeconds)
                {
                    runtime.SelectObject(pressedObject);
                    moveStartedFromHold = runtime.BeginMoveSelected();
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (runtime.HasActiveSession && confirmOnMouseRelease)
                    runtime.Confirm();

                pressedObject = null;
                moveStartedFromHold = false;
            }
        }

        public bool TryGetPointerCell(out Vector2Int cell)
        {
            cell = default;
            if (!TryGetPointerPosition(out Vector2 pointerPosition))
                return false;

            Ray ray = worldCamera.ScreenPointToRay(pointerPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, placementPlaneY, 0f));
            if (!plane.Raycast(ray, out float enter))
                return false;

            Vector3 worldPosition = ray.GetPoint(enter);
            return runtime.TryWorldToCell(worldPosition, out cell);
        }

        private PlacedFurnitureObject RaycastPlacedFurniture()
        {
            if (!TryGetPointerPosition(out Vector2 pointerPosition))
                return null;

            Ray ray = worldCamera.ScreenPointToRay(pointerPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placedFurnitureMask))
                return null;

            return hit.collider.GetComponentInParent<PlacedFurnitureObject>();
        }

        private static bool TryGetPointerPosition(out Vector2 pointerPosition)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                pointerPosition = default;
                return false;
            }

            pointerPosition = mouse.position.ReadValue();
            return true;
        }
    }
}
