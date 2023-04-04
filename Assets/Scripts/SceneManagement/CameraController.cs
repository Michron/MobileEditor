#nullable enable

using System;
using UnityEngine;

namespace MobileEditor.SceneManagement
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        [Range(0.01f, 5.0f)]
        private float _minZoomLevel = 1.0f;
        [SerializeField]
        [Range(10.0f, 250.0f)]
        private float _maxZoomLevel = 25.0f;
        [SerializeField]
        private float _zoomSensitivity = 1.0f;
        [SerializeField]
        private float _minZoomSpeed = 0.1f;
        [SerializeField]
        [Range(0f, 1f)]
        private float _zoomInertiaTime = 0.2f;
        [SerializeField]
        private float _rotateSensitivity = 1.0f;
        [SerializeField]
        private float _minRotateSpeed = 5.0f;
        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float _rotationInertiaTime = 0.2f;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _targetZoomDistance;
        private Vector3 _zoomDirection;

        public Transform Pivot { get; private set; } = null!;
        public Camera Camera { get; private set; } = null!;
        public Transform CameraTransform { get; private set; } = null!;

        private void Awake()
        {
            Pivot = transform;
            Camera = GetComponentInChildren<Camera>();
            CameraTransform = Camera.transform;

            Debug.Assert(Camera != null);

            // Move the pivot of the controller to (0,0,0)
            _targetPosition = new Vector3();
            _targetRotation = Quaternion.identity;
            Pivot.SetPositionAndRotation(_targetPosition, _targetRotation);

            // Set the position and rotation of the camera to look at (0,0,0) from a small distance.
            Vector3 cameraPosition = new Vector3(0.0f, 10.0f, -7.5f);
            _targetZoomDistance = cameraPosition.magnitude;
            _zoomDirection = cameraPosition / _targetZoomDistance;
            Quaternion cameraRotation = Quaternion.LookRotation(-_zoomDirection, new Vector3(0.0f, 1.0f, 0.0f));
            CameraTransform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }

        private void LateUpdate()
        {
            UpdateCameraPosition();
            UpdateCameraRotation();
            UpdateCameraZoom();
        }

        private void UpdateCameraPosition()
        {
            // Could apply smoothing here, but that might not feel right with touch input.
            Pivot.position = _targetPosition;
        }

        private void UpdateCameraRotation()
        {
            if (_rotationInertiaTime <= 0)
            {
                Pivot.rotation = _targetRotation;

                return;
            }

            Quaternion rotation = Pivot.rotation;
            float delta = Quaternion.Angle(rotation, _targetRotation);
            float speed = 1.0f / _rotationInertiaTime;

            // Smoothly rotate towards the target rotation, while keeping a minimum speed.
            rotation = Quaternion.RotateTowards(rotation, _targetRotation, MathF.Max(_minRotateSpeed, delta) * Time.deltaTime * speed);

            Pivot.rotation = rotation;
        }

        private void UpdateCameraZoom()
        {
            if (_zoomInertiaTime <= 0)
            {
                CameraTransform.localPosition = _zoomDirection * _targetZoomDistance;
            }

            float zoomDistance = CameraTransform.localPosition.magnitude;
            float delta = _targetZoomDistance - zoomDistance;
            float speed = 1.0f / _zoomInertiaTime;

            Mathf.MoveTowards(zoomDistance, _targetZoomDistance, MathF.Max(_minZoomSpeed, delta) * Time.deltaTime * speed);
            Vector3 zoom = _zoomDirection * _targetZoomDistance;

            CameraTransform.localPosition = zoom;
        }

        public void MoveCamera(Vector3 position)
        {
            _targetPosition = position;
        }

        public void RotateCamera(float angle)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle * _rotateSensitivity, new Vector3(0.0f, 1.0f, 0.0f));

            _targetRotation *= rotation;
        }

        public void ZoomCamera(float delta)
        {
            // Apply a sensitivity to the zoom value.
            delta *= _zoomSensitivity;

            // Convert the delta to a value that's > 1.0 (zoom out) or < 1 (zoom in).
            delta += 1.0f;

            // Clamp the zoom level so the camera stays within an acceptable range.
            _targetZoomDistance = Mathf.Clamp(_targetZoomDistance * delta, _minZoomLevel, _maxZoomLevel);
        }
    }
}
