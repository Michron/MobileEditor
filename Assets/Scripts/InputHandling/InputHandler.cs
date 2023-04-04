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
    /// <summary>
    /// Handles input and converts it to camera movement, or object editing actions.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        private IReadOnlyDictionary<Type, IInputState> _states = null!;
        private IInputState _currentState = null!;

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
        }

        private void Update()
        {
            _currentState.Update();
        }

        /// <summary>
        /// Initializes this <see cref="InputHandler"/> by preparing all input states.
        /// </summary>
        /// <param name="uiManager">The <see cref="UIManager"/> of the scene.</param>
        /// <param name="cameraController">The <see cref="CameraController"/> that receives input from this handler.</param>
        /// <param name="objectController">The <see cref="ObjectController"/> responsible for smoothly placing moved objects.</param>
        /// <param name="selectionService">The <see cref="SelectionService"/> of the scene.</param>
        internal void Initialize(
            UIManager uiManager,
            CameraController cameraController,
            ObjectController objectController,
            SelectionService selectionService)
        {
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

        /// <summary>
        /// Change the current state to a state of type <typeparamref name="TState"/>.
        /// </summary>
        /// <typeparam name="TState">The type of state to switch to.</typeparam>
        internal void ChangeState<TState>() where TState : class, IInputState
        {
            TState state = GetStateChecked<TState>();

            _currentState.OnExitState();

            _currentState = state;

            _currentState.OnEnterState();
        }

        /// <summary>
        /// Change the current state to a state of type <typeparamref name="TState"/>, and initialize the state with an action.
        /// </summary>
        /// <typeparam name="TState">The type of state to switch to.</typeparam>
        /// <typeparam name="TContext">The type of the context used in the initialization method.</typeparam>
        /// <param name="context">
        /// Contains data that is used when invoking <paramref name="initializeAction"/> to avoid values from being captured in the closure.
        /// </param>
        /// <param name="initializeAction">
        /// An initialization method that's invoked before the <see cref="IInputState.OnEnterState"/> method of the state is invoked.
        /// </param>
        internal void ChangeState<TState, TContext>(TContext context, Action<TState, TContext> initializeAction) where TState : class, IInputState
        {
            TState state = GetStateChecked<TState>();

            _currentState.OnExitState();

            _currentState = state;

            initializeAction(state, context);

            _currentState.OnEnterState();
        }

        private TState GetStateChecked<TState>() where TState : class, IInputState
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
