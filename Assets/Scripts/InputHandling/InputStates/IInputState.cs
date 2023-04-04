#nullable enable

namespace MobileEditor.InputHandling.InputStates
{
    /// <summary>
    /// Interface for input states, which are responsible for handling various input.
    /// </summary>
    internal interface IInputState
    {
        /// <summary>
        /// Invoked when the state is made active.
        /// </summary>
        void OnEnterState();

        /// <summary>
        /// Invoked when the state is deactivated.
        /// </summary>
        void OnExitState();

        /// <summary>
        /// Invoked once per frame if the state is currently active.
        /// </summary>
        void Update();
    }
}
