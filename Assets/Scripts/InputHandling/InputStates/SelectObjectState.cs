#nullable enable

using MobileEditor.SceneManagement;
using MobileEditor.Services.Selection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Utilities;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace MobileEditor.InputHandling.InputStates
{
    internal sealed class SelectObjectState : IInputState
    {
        private readonly InputHandler _inputHandler;
        private readonly CameraController _cameraController;
        private readonly SelectionService _selectionService;
        private readonly EventSystem _eventSystem;
        private readonly int _selectionMask;

        private Vector2 _initialScreenPoint;
        private bool _isOnCurrentSelection;

        public SelectObjectState(
            InputHandler inputHandler,
            CameraController cameraController,
            SelectionService selectionService)
        {
            _inputHandler = inputHandler;
            _cameraController = cameraController;
            _selectionService = selectionService;
            _eventSystem = EventSystem.current;

            // In this case we want to filter everything except Selection, so invert the mask.
            _selectionMask = ~LayerMask.NameToLayer("Selection");
        }

        public void OnEnterState()
        {
            ReadOnlyArray<Touch> touches = Touch.activeTouches;
            Touch touch = touches[0];

            _initialScreenPoint = touch.screenPosition;
            SelectableObject? selectable = GetSelectableAtScreenPosition(touch.screenPosition);

            _isOnCurrentSelection = selectable != null && selectable == _selectionService.CurrentSelection;
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

            Touch touch = touches[0];
            float dragDistance = (_initialScreenPoint - touch.screenPosition).sqrMagnitude;

            if (touch.phase == TouchPhase.Moved && dragDistance > _eventSystem.pixelDragThreshold)
            {
                if (_isOnCurrentSelection)
                {
                    _inputHandler.ChangeState<DragObjectState>();
                }
                else
                {
                    // Set the InitialScreenPoint of the state to avoid an offset because of the pixel drag threshold.
                    _inputHandler.ChangeState<DragCameraState, Vector2>(
                        _initialScreenPoint,
                        static (state, point) =>
                        {
                            state.InitialScreenPoint = point;
                        });
                }

                return;
            }

            if (touch.phase == TouchPhase.Ended)
            {
                SelectableObject? selectable = GetSelectableAtScreenPosition(touch.screenPosition);

                _selectionService.ChangeSelection(selectable);

                _inputHandler.ChangeState<IdleState>();
            }
        }

        private SelectableObject? GetSelectableAtScreenPosition(Vector2 position)
        {
            Camera camera = _cameraController.Camera;
            Ray ray = camera.ScreenPointToRay(position);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, camera.farClipPlane, _selectionMask))
            {
                GameObject gameObject = hitInfo.collider.gameObject;

                return gameObject.GetComponentInParent<SelectableObject>();
            }

            return null;
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
    }
}
