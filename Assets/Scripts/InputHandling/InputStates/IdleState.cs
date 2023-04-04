#nullable enable

using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;

namespace MobileEditor.InputHandling.InputStates
{
    internal sealed class IdleState : IInputState
    {
        private readonly InputHandler _handler;
        private readonly EventSystem _eventSystem;

        public IdleState(InputHandler inputHandler)
        {
            _handler = inputHandler;
            _eventSystem = EventSystem.current;
        }

        public void OnEnterState()
        {
        }

        public void OnExitState()
        {
        }

        public void Update()
        {
            ReadOnlyArray<Touch> touches = Touch.activeTouches;
            int touchCount = touches.Count;

            if (touchCount == 0 || touchCount > 2)
            {
                return;
            }

            if (touchCount == 1)
            {
                Touch touch = touches[0];

                // Verify if the touch started this frame, and it's not on a UI element.
                if (touch.phase == TouchPhase.Began && !_eventSystem.IsPointerOverGameObject(touch.touchId))
                {
                    _handler.ChangeState<SelectObjectState>();
                }
            }
            else if (touchCount == 2)
            {
                _handler.ChangeState<RotateAndZoomState>();
            }
        }
    }
}
