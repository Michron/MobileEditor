#nullable enable

using MobileEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace MobileEditor.InputHandling.InputStates
{
    /// <summary>
    /// Handles moving the camera when the user is swiping the touchscreen.
    /// </summary>
    internal sealed class DragCameraState : IInputState
    {
        private readonly InputHandler _inputHandler;
        private readonly CameraController _cameraController;
        private Vector3 _initialPosition;

        public DragCameraState(InputHandler inputHandler, CameraController cameraController)
        {
            _inputHandler = inputHandler;
            _cameraController = cameraController;
        }

        /// <summary>
        /// The screenspace starting position of the swipe action. Required to be set by the previous state.
        /// </summary>
        public Vector2 InitialScreenPoint { get; set; }

        public void OnEnterState()
        {
            _initialPosition = _cameraController.Pivot.position;
        }

        public void OnExitState()
        {
        }

        public void Update()
        {
            ReadOnlyArray<Touch> touches = Touch.activeTouches;

            if (!VerifyState(touches))
            {
                return;
            }

            // Instead of directly using the delta of the touch, the touch is converted to a point in the world,
            // which is then subtracted from the world point of the initial touch position. This way the resulting
            // delta more accurately defines the distance the finger traveled in the world. This automatically
            // makes swiping when zoomed out faster as well.

            Touch touch = touches[0];
            Vector3 currentPoint = GetWorldPoint(touch.screenPosition);
            Vector3 initialPoint = GetWorldPoint(InitialScreenPoint);

            Vector3 delta = initialPoint - currentPoint;

            delta = ConvertDragDirectionToWorld(delta);
            delta = ConvertWorldToPivotDirection(delta);

            // Filter out any Y-axis movement so the camera stays grounded.
            Vector3 flatDelta = new Vector3(delta.x, 0, delta.z);

            _cameraController.MoveCamera(_initialPosition + flatDelta);
        }

        private bool VerifyState(ReadOnlyArray<Touch> touches)
        {
            int touchCount = touches.Count;

            if (touchCount == 2)
            {
                _inputHandler.ChangeState<RotateAndZoomState>();

                return false;
            }

            if (touchCount != 1)
            {
                _inputHandler.ChangeState<IdleState>();

                return false;
            }

            return true;
        }

        private Vector3 ConvertDragDirectionToWorld(Vector3 direction)
        {
            Quaternion cameraRotation = _cameraController.CameraTransform.rotation;

            // Create a new rotation, with up being the normal of drag plane, and forward the up direction of the camera.
            Vector3 cameraUp = cameraRotation * new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 cameraBack = cameraRotation * new Vector3(0.0f, 0.0f, -1.0f);
            Quaternion rotation = Quaternion.LookRotation(cameraUp, cameraBack);

            // Invert the rotation so it transforms the direction to world space, instead of to the drag plane's space.
            rotation = Quaternion.Inverse(rotation);

            return rotation * direction;
        }

        private Vector3 ConvertWorldToPivotDirection(Vector3 direction)
        {
            Quaternion rotation = _cameraController.Pivot.rotation;

            return rotation * direction;
        }

        private Vector3 GetWorldPoint(Vector2 screenPoint)
        {
            Camera camera = _cameraController.Camera;
            Transform cameraTransform = _cameraController.CameraTransform;
            Transform pivot = _cameraController.Pivot;

            Ray ray = camera.ScreenPointToRay(screenPoint);
            Plane plane = new Plane(-cameraTransform.forward, pivot.position);

            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            // Technically the raycast can miss the plane if the FoV of the camera is exceptionally high.
            // In that case, fall back to getting a point based on the distance to the pivot, and log a warning.
            Debug.LogWarning("Camera ray failed to intersect with drag plane.");

            return ray.GetPoint(cameraTransform.localPosition.magnitude);
        }
    }
}
