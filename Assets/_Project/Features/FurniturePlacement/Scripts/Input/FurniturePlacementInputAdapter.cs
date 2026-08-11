using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace SenCity.Features.FurniturePlacement.Input
{
    public class FurniturePlacementInputAdapter : MonoBehaviour
    {
        [Header("Placement")]
        [SerializeField] private FurniturePlacementRuntime runtime;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform interactionRect;
        [SerializeField] private LayerMask placedFurnitureMask = ~0;
        [SerializeField] private float placementPlaneY;
        [SerializeField] private bool confirmOnMouseRelease = true;
        [SerializeField, Min(1f)] private float tapMoveThresholdPixels = 18f;

        [Header("Room camera")]
        [SerializeField] private Vector2 cameraPanLimits = new Vector2(1.2f, 1.2f);
        [SerializeField, Range(15f, 80f)] private float minimumFieldOfView = 28f;
        [SerializeField, Range(15f, 90f)] private float maximumFieldOfView = 58f;
        [SerializeField, Min(0.001f)] private float mouseWheelZoomSpeed = 0.015f;
        [SerializeField, Min(0.001f)] private float pinchZoomSpeed = 0.025f;

        private readonly RaycastHit[] furnitureHitBuffer = new RaycastHit[32];
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(16);

        private PlacementSession observedSession;
        private PlacedFurnitureObject pressedObject;
        private Vector2 pointerDownPosition;
        private Vector2Int previewGrabOffset;
        private Vector3 cameraPanAnchor;
        private Vector3 cameraHomePosition;
        private bool pointerOwnedByWorld;
        private bool draggingPreview;
        private bool panningCamera;
        private bool pointerMoved;
        private bool pinchActive;
        private bool pinchHasAnchor;
        private float previousPinchDistance;
        private PointerEventData uiPointerEventData;
        private EventSystem cachedEventSystem;

        private void Awake()
        {
            if (runtime == null)
                runtime = FindAnyObjectByType<FurniturePlacementRuntime>();

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera != null)
                cameraHomePosition = worldCamera.transform.position;
        }

        private void OnEnable()
        {
            if (runtime != null)
            {
                runtime.SessionChanged -= HandleSessionChanged;
                runtime.SessionChanged += HandleSessionChanged;
                observedSession = runtime.ActiveSession;
            }
        }

        private void OnDisable()
        {
            if (runtime != null)
                runtime.SessionChanged -= HandleSessionChanged;

            ResetSinglePointerGesture();
            pinchActive = false;
        }

        private void Update()
        {
            if (runtime == null || worldCamera == null)
                return;

            HandleKeyboard();
            HandleMouseWheel();
            if (HandlePinchGesture())
                return;

            HandlePrimaryPointer();
        }

        private void HandleKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

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

        private void HandlePrimaryPointer()
        {
            if (!TryGetPrimaryPointer(out PointerSnapshot pointer))
            {
                runtime.HoverObject(null);
                return;
            }

            if (!pointer.IsTouch && !runtime.HasActiveSession && !pointer.Pressed)
            {
                PlacedFurnitureObject hovered = IsWorldSurfaceTopHit(pointer.Position)
                    ? RaycastPlacedFurniture(pointer.Position)
                    : null;
                runtime.HoverObject(hovered);
            }
            else
            {
                runtime.HoverObject(null);
            }

            if (pointer.PressedThisFrame)
                BeginSinglePointerGesture(pointer.Position);

            if (pointer.Pressed && pointerOwnedByWorld)
                ContinueSinglePointerGesture(pointer.Position);

            if (pointer.ReleasedThisFrame && pointerOwnedByWorld)
                EndSinglePointerGesture(pointer.Position);
        }

        private void BeginSinglePointerGesture(Vector2 screenPosition)
        {
            ResetSinglePointerGesture();
            if (!IsWorldSurfaceTopHit(screenPosition))
                return;

            pointerOwnedByWorld = true;
            pointerDownPosition = screenPosition;

            if (runtime.HasActiveSession)
            {
                if (!TryGetPointerPreviewOrigin(screenPosition, out Vector2Int pointerOrigin))
                    return;

                PlacementSession session = runtime.ActiveSession;
                if (session != null && !session.HasPreviewPosition)
                {
                    runtime.MovePreview(pointerOrigin);
                    previewGrabOffset = Vector2Int.zero;
                }
                else if (runtime.ActiveSession != null)
                {
                    previewGrabOffset = runtime.ActiveSession.OriginCell - pointerOrigin;
                }

                draggingPreview = true;
                return;
            }

            pressedObject = RaycastPlacedFurniture(screenPosition);
            if (pressedObject == null && TryGetPointerWorldPosition(screenPosition, out cameraPanAnchor))
                panningCamera = true;
        }

        private void ContinueSinglePointerGesture(Vector2 screenPosition)
        {
            if (!pointerMoved && Vector2.Distance(pointerDownPosition, screenPosition) >= tapMoveThresholdPixels)
                pointerMoved = true;

            if (draggingPreview)
            {
                if (TryGetPointerPreviewOrigin(screenPosition, out Vector2Int pointerOrigin))
                    runtime.MovePreview(pointerOrigin + previewGrabOffset);
                return;
            }

            if (!panningCamera || !pointerMoved || !TryGetPointerWorldPosition(screenPosition, out Vector3 worldPoint))
                return;

            PanCamera(cameraPanAnchor - worldPoint);
        }

        private void EndSinglePointerGesture(Vector2 screenPosition)
        {
            if (!runtime.HasActiveSession && !pointerMoved)
            {
                PlacedFurnitureObject releasedObject = RaycastPlacedFurniture(screenPosition);
                runtime.SelectObject(releasedObject == pressedObject ? releasedObject : null);
            }
            else if (draggingPreview && confirmOnMouseRelease && runtime.HasActiveSession)
            {
                runtime.Confirm();
            }

            ResetSinglePointerGesture();
        }

        private bool HandlePinchGesture()
        {
            if (!TryGetTwoActiveTouches(out Vector2 first, out Vector2 second))
            {
                pinchActive = false;
                pinchHasAnchor = false;
                return false;
            }

            Vector2 center = (first + second) * 0.5f;
            float distance = Vector2.Distance(first, second);
            if (!pinchActive)
            {
                if (!IsWorldSurfaceTopHit(center))
                    return false;

                pinchActive = true;
                previousPinchDistance = distance;
                pinchHasAnchor = TryGetPointerWorldPosition(center, out cameraPanAnchor);
                ResetSinglePointerGesture();
                return true;
            }

            float distanceDelta = distance - previousPinchDistance;
            previousPinchDistance = distance;
            ZoomCamera(distanceDelta * pinchZoomSpeed);

            if (pinchHasAnchor && TryGetPointerWorldPosition(center, out Vector3 worldPoint))
                PanCamera(cameraPanAnchor - worldPoint);
            return true;
        }

        private void HandleMouseWheel()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(wheel, 0f) || !IsWorldSurfaceTopHit(mouse.position.ReadValue()))
                return;

            ZoomCamera(wheel * mouseWheelZoomSpeed);
        }

        private void PanCamera(Vector3 worldDelta)
        {
            worldDelta.y = 0f;
            Vector3 target = worldCamera.transform.position + worldDelta;
            target.x = Mathf.Clamp(
                target.x,
                cameraHomePosition.x - Mathf.Abs(cameraPanLimits.x),
                cameraHomePosition.x + Mathf.Abs(cameraPanLimits.x));
            target.z = Mathf.Clamp(
                target.z,
                cameraHomePosition.z - Mathf.Abs(cameraPanLimits.y),
                cameraHomePosition.z + Mathf.Abs(cameraPanLimits.y));
            worldCamera.transform.position = target;
        }

        private void ZoomCamera(float amount)
        {
            float minimum = Mathf.Min(minimumFieldOfView, maximumFieldOfView);
            float maximum = Mathf.Max(minimumFieldOfView, maximumFieldOfView);
            worldCamera.fieldOfView = Mathf.Clamp(worldCamera.fieldOfView - amount, minimum, maximum);
        }

        public bool TryGetPointerCell(out Vector2Int cell)
        {
            cell = default;
            return TryGetPrimaryPointer(out PointerSnapshot pointer) &&
                   TryGetPointerWorldPosition(pointer.Position, out Vector3 worldPosition) &&
                   runtime.TryWorldToCell(worldPosition, out cell);
        }

        private bool TryGetPointerPreviewOrigin(Vector2 screenPosition, out Vector2Int originCell)
        {
            originCell = default;
            return TryGetPointerWorldPosition(screenPosition, out Vector3 worldPosition) &&
                   runtime.TryWorldToPreviewOrigin(worldPosition, out originCell);
        }

        private PlacedFurnitureObject RaycastPlacedFurniture(Vector2 screenPosition)
        {
            if (!TryCreateWorldRay(screenPosition, out Ray ray))
                return null;

            int hitCount = Physics.RaycastNonAlloc(
                ray,
                furnitureHitBuffer,
                Mathf.Infinity,
                placedFurnitureMask,
                QueryTriggerInteraction.Ignore);
            PlacedFurnitureObject closestObject = null;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = furnitureHitBuffer[i];
                PlacedFurnitureObject candidate = hit.collider != null
                    ? hit.collider.GetComponentInParent<PlacedFurnitureObject>()
                    : null;
                if (candidate == null || hit.distance >= closestDistance)
                    continue;

                closestObject = candidate;
                closestDistance = hit.distance;
            }

            return closestObject;
        }

        private bool TryGetPointerWorldPosition(Vector2 screenPosition, out Vector3 worldPosition)
        {
            worldPosition = default;
            if (!TryCreateWorldRay(screenPosition, out Ray ray))
                return false;

            var plane = new Plane(Vector3.up, new Vector3(0f, placementPlaneY, 0f));
            if (!plane.Raycast(ray, out float enter))
                return false;

            worldPosition = ray.GetPoint(enter);
            return true;
        }

        private bool TryCreateWorldRay(Vector2 screenPosition, out Ray ray)
        {
            ray = default;
            if (worldCamera == null)
                return false;

            if (interactionRect == null)
            {
                ray = worldCamera.ScreenPointToRay(screenPosition);
                return true;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(interactionRect, screenPosition, null) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    interactionRect,
                    screenPosition,
                    null,
                    out Vector2 localPoint))
                return false;

            Rect rect = interactionRect.rect;
            float viewportX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            float viewportY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
            ray = worldCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
            return true;
        }

        private bool IsWorldSurfaceTopHit(Vector2 screenPosition)
        {
            if (interactionRect != null &&
                !RectTransformUtility.RectangleContainsScreenPoint(interactionRect, screenPosition, null))
                return false;

            EventSystem currentEventSystem = EventSystem.current;
            if (currentEventSystem == null || interactionRect == null)
                return true;

            if (cachedEventSystem != currentEventSystem || uiPointerEventData == null)
            {
                cachedEventSystem = currentEventSystem;
                uiPointerEventData = new PointerEventData(currentEventSystem);
            }

            uiPointerEventData.position = screenPosition;
            uiRaycastResults.Clear();
            currentEventSystem.RaycastAll(uiPointerEventData, uiRaycastResults);
            if (uiRaycastResults.Count == 0)
                return true;

            Transform topHit = uiRaycastResults[0].gameObject.transform;
            return topHit == interactionRect || topHit.IsChildOf(interactionRect);
        }

        private void HandleSessionChanged(PlacementSession session)
        {
            if (ReferenceEquals(observedSession, session))
                return;

            observedSession = session;
            ResetSinglePointerGesture();
        }

        private void ResetSinglePointerGesture()
        {
            pointerOwnedByWorld = false;
            draggingPreview = false;
            panningCamera = false;
            pointerMoved = false;
            pressedObject = null;
        }

        private static bool TryGetPrimaryPointer(out PointerSnapshot snapshot)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                TouchControl touch = touchscreen.primaryTouch;
                if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
                {
                    snapshot = new PointerSnapshot(
                        touch.position.ReadValue(),
                        touch.press.isPressed,
                        touch.press.wasPressedThisFrame,
                        touch.press.wasReleasedThisFrame,
                        true);
                    return true;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                snapshot = new PointerSnapshot(
                    mouse.position.ReadValue(),
                    mouse.leftButton.isPressed,
                    mouse.leftButton.wasPressedThisFrame,
                    mouse.leftButton.wasReleasedThisFrame,
                    false);
                return true;
            }

            snapshot = default;
            return false;
        }

        private static bool TryGetTwoActiveTouches(out Vector2 first, out Vector2 second)
        {
            first = default;
            second = default;
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return false;

            int activeCount = 0;
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                    continue;

                if (activeCount == 0)
                    first = touch.position.ReadValue();
                else
                {
                    second = touch.position.ReadValue();
                    return true;
                }

                activeCount++;
            }

            return false;
        }

        private readonly struct PointerSnapshot
        {
            public PointerSnapshot(
                Vector2 position,
                bool pressed,
                bool pressedThisFrame,
                bool releasedThisFrame,
                bool isTouch)
            {
                Position = position;
                Pressed = pressed;
                PressedThisFrame = pressedThisFrame;
                ReleasedThisFrame = releasedThisFrame;
                IsTouch = isTouch;
            }

            public Vector2 Position { get; }
            public bool Pressed { get; }
            public bool PressedThisFrame { get; }
            public bool ReleasedThisFrame { get; }
            public bool IsTouch { get; }
        }
    }
}
