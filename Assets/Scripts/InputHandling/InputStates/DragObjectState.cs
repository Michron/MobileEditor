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

            // New objects are not guaranteed to spawn within the screen, so move them to where the current touch would approximately be.
            if (selectable.IsNewObject)
            {
                Vector3 worldPoint = GetWorldPoint(touches[0].screenPosition);
                transform.position = worldPoint;
            }

            _placementController.StartMoveAction(transform!);

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

            // Use the camera's height as a reference when creating a fallback value.
            // This should prevent the position from being too low, while still being visible in the viewport.
            float fallbackDistance = _cameraController.CameraTransform.position.y;

            return ray.GetPoint(fallbackDistance);
        }
    }
}
