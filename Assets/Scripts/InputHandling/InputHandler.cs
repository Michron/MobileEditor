#nullable enable

using System;
using System.Collections.Generic;
using MobileEditor.InputHandling.InputStates;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine;
using MobileEditor.SceneManagement;
using MobileEditor.UI;
using MobileEditor.Services.Selection;

namespace MobileEditor.InputHandling
{
    public class InputHandler : MonoBehaviour
    {
        private IReadOnlyDictionary<Type, IInputState> _states = null!;
        private IInputState _currentState = null!;

        public CameraController CameraController { get; private set; } = null!;

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
        }

        private void Update()
        {
            _currentState.Update();
        }

        public void Initialize(
            UIManager uiManager,
            CameraController cameraController,
            ObjectController objectController,
            SelectionService selectionService)
        {
            CameraController = cameraController;
            _currentState = new IdleState(this);

            _states = new Dictionary<Type, IInputState>()
            {
                { typeof(IdleState), _currentState },
                { typeof(SelectObjectState), new SelectObjectState(this, cameraController, selectionService) },
                { typeof(DragCameraState), new DragCameraState(this, cameraController) },
                { typeof(DragObjectState), new DragObjectState(this, uiManager, cameraController, objectController, selectionService) },
                { typeof(RotateAndZoomState), new RotateAndZoomState(this, cameraController) },
            };

            _currentState.OnEnterState();
        }

        internal void ChangeState<TState>() where TState : class, IInputState
        {
            TState state = GetStateChecked<TState>();

            _currentState.OnExitState();

            _currentState = state;

            _currentState.OnEnterState();
        }

        internal void ChangeState<TState, TContext>(TContext context, Action<TState, TContext> initializeAction) where TState : class, IInputState
        {
            TState state = GetStateChecked<TState>();

            _currentState.OnExitState();

            _currentState = state;

            initializeAction(state, context);

            _currentState.OnEnterState();
        }

        internal TState? GetState<TState>() where TState : class, IInputState
        {
            _states.TryGetValue(typeof(TState), out IInputState? state);

            return state as TState;
        }

        internal TState GetStateChecked<TState>() where TState : class, IInputState
        {
            _states.TryGetValue(typeof(TState), out IInputState? state);

            if (state is not TState castType)
            {
                throw new KeyNotFoundException($"Unable to find state of type {nameof(TState)}.");
            }

            return castType;
        }
    }
}
