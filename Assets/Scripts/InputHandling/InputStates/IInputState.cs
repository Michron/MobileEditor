#nullable enable

namespace MobileEditor.InputHandling.InputStates
{
    internal interface IInputState
    {
        void OnEnterState();
        void OnExitState();
        void Update();
    }
}
