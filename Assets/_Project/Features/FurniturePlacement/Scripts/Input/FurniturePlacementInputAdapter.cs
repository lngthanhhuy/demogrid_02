using SenCity.Features.FurniturePlacement;
using UnityEngine;

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
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                runtime.Cancel();

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                runtime.RotatePreview();

            if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
                runtime.Confirm();

            if (UnityEngine.Input.GetKeyDown(KeyCode.M))
                runtime.BeginMoveSelected();

            if (UnityEngine.Input.GetKeyDown(KeyCode.Delete) || UnityEngine.Input.GetKeyDown(KeyCode.Backspace))
                runtime.RequestStoreSelected();

            if (UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl))
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.S))
                    runtime.SaveCurrentLayout();

                if (UnityEngine.Input.GetKeyDown(KeyCode.L))
                    runtime.LoadSavedLayout();
            }
        }

        private void HandlePointer()
        {
            if (TryGetPointerCell(out Vector2Int cell) && runtime.HasActiveSession)
                runtime.MovePreview(cell);

            PlacedFurnitureObject pointerObject = !runtime.HasActiveSession ? RaycastPlacedFurniture() : null;
            runtime.HoverObject(pointerObject);

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                pointerDownTime = Time.time;
                pressedObject = pointerObject;
                moveStartedFromHold = false;

                if (!runtime.HasActiveSession && pressedObject != null)
                    runtime.SelectObject(pressedObject);
                else if (!runtime.HasActiveSession)
                    runtime.SelectObject(null);
            }

            if (UnityEngine.Input.GetMouseButton(0) && pressedObject != null && !runtime.HasActiveSession)
            {
                if (!moveStartedFromHold && Time.time - pointerDownTime >= holdToMoveSeconds)
                {
                    runtime.SelectObject(pressedObject);
                    moveStartedFromHold = runtime.BeginMoveSelected();
                }
            }

            if (UnityEngine.Input.GetMouseButtonUp(0))
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
            Ray ray = worldCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, placementPlaneY, 0f));
            if (!plane.Raycast(ray, out float enter))
                return false;

            Vector3 worldPosition = ray.GetPoint(enter);
            return runtime.TryWorldToCell(worldPosition, out cell);
        }

        private PlacedFurnitureObject RaycastPlacedFurniture()
        {
            Ray ray = worldCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placedFurnitureMask))
                return null;

            return hit.collider.GetComponentInParent<PlacedFurnitureObject>();
        }
    }
}
