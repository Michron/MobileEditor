#nullable enable

using MobileEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace MobileEditor.InputHandling.InputStates
{
    /// <summary>
    /// Reads input with two fingers to determine if the camera should be rotated, and/or zoomed in and out.
    /// </summary>
    internal sealed class RotateAndZoomState : IInputState
    {
        private readonly InputHandler _inputHandler;
        private readonly CameraController _cameraController;

        private Vector2 _previousDirection;
        private float _previousDistance;

        public RotateAndZoomState(InputHandler inputHandler, CameraController cameraController)
        {
            _inputHandler = inputHandler;
            _cameraController = cameraController;
        }

        public void OnEnterState()
        {
            ReadOnlyArray<Touch> touches = Touch.activeTouches;

            (float distance, Vector2 direction) = GetTouchData(touches);
            _previousDistance = distance;
            _previousDirection = direction;
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

            // Get the pinch-distance, and the direction in world space of the input.
            (float distance, Vector2 direction) = GetTouchData(touches);

            UpdateZoomLevel(distance);
            UpdateRotation(direction);
        }

        private bool VerifyState(ReadOnlyArray<Touch> touches)
        {
            int touchCount = touches.Count;

            if (touchCount == 2)
            {
                return true;
            }

            if (touchCount == 1)
            {
                Touch touch = touches[0];

                // The touch.startScreenPosition is not valid for the DragCameraState,
                // so we have to manually set the initial point.
                _inputHandler.ChangeState<DragCameraState, Vector2>(
                    touch.screenPosition,
                    static (state, position) =>
                    {
                        state.InitialScreenPoint = position;
                    });

                return false;
            }

            _inputHandler.ChangeState<IdleState>();

            return false;
        }

        private void UpdateRotation(Vector2 direction)
        {
            // Get the angle between the starting position, and the current position of the touches.
            float angle = Vector2.SignedAngle(_previousDirection, direction);
            _previousDirection = direction;

            _cameraController.RotateCamera(angle);
        }

        private void UpdateZoomLevel(float distance)
        {
            float delta = _previousDistance - distance;
            _previousDistance = distance;

            _cameraController.ZoomCamera(delta);
        }

        private (float distance, Vector2 direction) GetTouchData(ReadOnlyArray<Touch> touches)
        {
            Touch touch1 = touches[0];
            Touch touch2 = touches[1];

            Vector2 delta = touch1.screenPosition - touch2.screenPosition;

            // Adjust the distance of the pinch gesture based on the DPI of the screen.
            float distance = delta.magnitude / Screen.dpi;
            Vector2 direction = delta.normalized;

            return (distance, direction);
        }
    }
}
