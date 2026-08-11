using System.Collections;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public sealed class SenCityRoomCameraFocus : MonoBehaviour
    {
        [SerializeField] private Camera roomCamera;
        [SerializeField, Range(0.35f, 1f)] private float focusZoom = 0.7f;
        [SerializeField, Min(0.05f)] private float transitionDuration = 0.28f;
        [SerializeField, Min(0f)] private float lookHeight = 0.3f;

        private Vector3 restorePosition;
        private Quaternion restoreRotation;
        private Vector3 restoreLookPoint;
        private Vector3 transitionTargetPosition;
        private Quaternion transitionTargetRotation;
        private Coroutine transition;
        private bool initialized;
        private bool isFocused;

        private void Awake()
        {
            Initialize();
        }

        public void Focus(Transform target)
        {
            if (target == null || !Initialize())
                return;

            if (!isFocused)
                CaptureRestorePose();

            isFocused = true;
            Vector3 targetLookPoint = target.position + Vector3.up * lookHeight;
            Vector3 restoreOffset = restorePosition - restoreLookPoint;
            Vector3 targetPosition = targetLookPoint + restoreOffset * focusZoom;
            Quaternion targetRotation = Quaternion.LookRotation(targetLookPoint - targetPosition, Vector3.up);
            StartTransition(targetPosition, targetRotation);
        }

        public void Restore()
        {
            if (!Initialize() || !isFocused)
                return;

            isFocused = false;
            StartTransition(restorePosition, restoreRotation);
        }

        public void RestoreImmediate()
        {
            if (!Initialize() || (!isFocused && transition == null))
                return;

            isFocused = false;
            if (transition != null)
                StopCoroutine(transition);
            transition = null;
            roomCamera.transform.SetPositionAndRotation(restorePosition, restoreRotation);
        }

        private bool Initialize()
        {
            if (initialized)
                return roomCamera != null;

            if (roomCamera == null)
                roomCamera = Camera.main;
            if (roomCamera == null)
                return false;

            CaptureRestorePose();
            initialized = true;
            return true;
        }

        private void CaptureRestorePose()
        {
            restorePosition = roomCamera.transform.position;
            restoreRotation = roomCamera.transform.rotation;
            Ray centerRay = new Ray(restorePosition, roomCamera.transform.forward);
            Plane floor = new Plane(Vector3.up, Vector3.zero);
            restoreLookPoint = floor.Raycast(centerRay, out float distance)
                ? centerRay.GetPoint(distance)
                : restorePosition + roomCamera.transform.forward * 5f;
        }

        private void StartTransition(Vector3 targetPosition, Quaternion targetRotation)
        {
            if (transition != null &&
                Vector3.SqrMagnitude(transitionTargetPosition - targetPosition) < 0.000001f &&
                Quaternion.Angle(transitionTargetRotation, targetRotation) < 0.01f)
                return;

            if (transition != null)
                StopCoroutine(transition);

            if (Vector3.SqrMagnitude(roomCamera.transform.position - targetPosition) < 0.000001f &&
                Quaternion.Angle(roomCamera.transform.rotation, targetRotation) < 0.01f)
            {
                roomCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
                transition = null;
                return;
            }

            transitionTargetPosition = targetPosition;
            transitionTargetRotation = targetRotation;
            transition = StartCoroutine(AnimateCamera(targetPosition, targetRotation));
        }

        private IEnumerator AnimateCamera(Vector3 targetPosition, Quaternion targetRotation)
        {
            Vector3 startPosition = roomCamera.transform.position;
            Quaternion startRotation = roomCamera.transform.rotation;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                roomCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, t),
                    Quaternion.Slerp(startRotation, targetRotation, t));
                yield return null;
            }

            roomCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
            transition = null;
        }
    }
}
