#nullable enable

using System;
using UnityEngine;

namespace MobileEditor.SceneManagement
{
    /// <summary>
    /// Controls the movement of a single camera. Allows the camera to move, rotate, and zoom in and out smoothly.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        [Range(0.01f, 5.0f)]
        [Tooltip("The smallest distance to which the camera is allowed to zoom in.")]
        private float _minZoomLevel = 1.0f;
        [SerializeField]
        [Range(10.0f, 250.0f)]
        [Tooltip("The largest distance to which the camera is allowed to zoom out.")]
        private float _maxZoomLevel = 25.0f;
        [SerializeField]
        [Tooltip("The sensitivity of the zoom in and out action when pinching on the screen.")]
        private float _zoomSensitivity = 1.0f;
        [SerializeField]
        [Tooltip("The lowest speed at which a zoom action will move to it's target location.")]
        private float _minZoomSpeed = 0.1f;
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("The maximum time a zoom action will use to move to the target position.")]
        private float _zoomInertiaTime = 0.2f;
        [SerializeField]
        [Tooltip("The sensitivity of a rotate actions. A value of 1.0 will match the angle of the input, while lower values slow down the rotation, and higher values increase the speed of the rotation.")]
        private float _rotateSensitivity = 1.0f;
        [SerializeField]
        [Tooltip("The minimal rotation speed in degrees per second at which a rotation will smoothly rotate to it's target rotation.")]
        private float _minRotateSpeed = 5.0f;
        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("The maximum time a rotate action will take to complete.")]
        private float _rotationInertiaTime = 0.2f;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _targetZoomDistance;
        private Vector3 _zoomDirection;

        /// <summary>
        /// The <see cref="Transform"/> component of the pivot of the camera.
        /// </summary>
        public Transform Pivot { get; private set; } = null!;

        /// <summary>
        /// The camera used by this <see cref="CameraController"/>.
        /// </summary>
        public Camera Camera { get; private set; } = null!;

        /// <summary>
        /// The <see cref="Transform"/> component of the camera itself.
        /// </summary>
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
            Vector3 cameraPosition = new Vector3(0.0f, 12.0f, -10f);
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

        /// <summary>
        /// Set the target position where the camera should move to.
        /// </summary>
        /// <param name="position">The target position.</param>
        public void MoveCamera(Vector3 position)
        {
            _targetPosition = position;
        }

        /// <summary>
        /// Rotate the camera on the Y-axis by the specified angle in degrees.
        /// The actual rotation will happen smoothly over several frames.
        /// </summary>
        /// <param name="angle">The angle in degrees to rotate.</param>
        public void RotateCamera(float angle)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle * _rotateSensitivity, new Vector3(0.0f, 1.0f, 0.0f));

            _targetRotation *= rotation;
        }

        /// <summary>
        /// Zooms the camera in or out by the specified <paramref name="delta"/> value.
        /// The zooming action will be applied smoothly over several frames.
        /// </summary>
        /// <param name="delta">The amount the camera should zoom in or out.</param>
        public void ZoomCamera(float delta)
        {
            // Apply a sensitivity to the zoom value.
            delta *= _zoomSensitivity;

            // Convert the delta to a value that's > 1.0 (zoom out) or < 1 (zoom in).
            delta += 1.0f;

            // Clamp the zoom level so the camera stays within an acceptable range.
            _targetZoomDistance = Mathf.Clamp(_targetZoomDistance * delta, _minZoomLevel, _maxZoomLevel);
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
    }
}
