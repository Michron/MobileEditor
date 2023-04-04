#nullable enable

using MobileEditor.SceneManagement;
using MobileEditor.Services.Selection;
using MobileEditor.UI;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace MobileEditor.InputHandling.InputStates
{
    /// <summary>
    /// Drags the currently selected <see cref="SelectableObject"/> in the scene.
    /// </summary>
    internal sealed class DragObjectState : IInputState
    {
        private readonly InputHandler _inputHandler;
        private readonly UIManager _uiManager;
        private readonly CameraController _cameraController;
        private readonly ObjectController _placementController;
        private readonly SelectionService _selectionService;

        public DragObjectState(
            InputHandler inputHandler,
            UIManager uiManager,
            CameraController cameraController,
            ObjectController objectController,
            SelectionService selectionService)
        {
            _inputHandler = inputHandler;
            _uiManager = uiManager;
            _cameraController = cameraController;
            _placementController = objectController;
            _selectionService = selectionService;
        }

        public void OnEnterState()
        {
            ReadOnlyArray<Touch> touches = Touch.activeTouches;

            if (touches.Count == 0)
            {
                Debug.LogError("At least one touch should be active.");
            }

            SelectableObject? selectable = _selectionService.CurrentSelection;

            Debug.Assert(selectable != null);

            Transform? transform = selectable!.Transform;

            // The screenpoint will still be on the object, so getting the world point would raycast on the object itself.
            // Instead get the fallback world point, which doesn't use a physics raycast.
            Vector3 worldPoint = GetFallbackWorldPoint(touches[0].screenPosition);

            // Start the move action, which disables colliders on the target so future frames can use a physics raycast.
            _placementController.StartMoveAction(transform!, worldPoint);

            _uiManager.ChangeUIState(UIState.AssetEditing);
        }

        public void OnExitState()
        {
            // Snap the object in place by ending the move action.
            _placementController.EndMoveAction();
            _uiManager.ChangeUIState(UIState.Default);
        }

        public void Update()
        {
            ReadOnlyArray<Touch> touches = Touch.activeTouches;

            if (!VerifyState(touches))
            {
                return;
            }

            Vector3 worldPoint = GetWorldPoint(touches[0].screenPosition);

            _placementController.SetMoveTarget(worldPoint);
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

        private Vector3 GetWorldPoint(Vector2 screenPoint)
        {
            Camera camera = _cameraController.Camera;
            Ray ray = camera.ScreenPointToRay(screenPoint);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, camera.farClipPlane))
            {
                return hitInfo.point;
            }

            return GetFallbackWorldPoint(ray);
        }

        private Vector3 GetFallbackWorldPoint(Vector2 screenPoint)
        {
            Camera camera = _cameraController.Camera;
            Ray ray = camera.ScreenPointToRay(screenPoint);

            return GetFallbackWorldPoint(ray);
        }

        private Vector3 GetFallbackWorldPoint(Ray ray)
        {
            // Use the camera's height as a reference when creating a fallback value.
            // This should prevent the position from being too low, while still being visible in the viewport.
            float fallbackDistance = _cameraController.CameraTransform.position.y;

            return ray.GetPoint(fallbackDistance);
        }
    }
}
