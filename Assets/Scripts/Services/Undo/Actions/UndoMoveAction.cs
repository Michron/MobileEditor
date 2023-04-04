using MobileEditor.SceneManagement;
using MobileEditor.Services.Selection;
using UnityEngine;

namespace MobileEditor.Services.Undo.Actions
{
    /// <summary>
    /// Undo action for move operations.
    /// </summary>
    internal sealed class UndoMoveAction : IUndoAction
    {
        private readonly SceneManager _sceneManager;
        private readonly Vector3 _originalPosition;
        private readonly Vector3 _targetPosition;
        private readonly int _instanceId;

        public UndoMoveAction(SceneManager sceneManager, Vector3 originalPosition, Vector3 targetPosition, int instanceId)
        {
            _sceneManager = sceneManager;
            _originalPosition = originalPosition;
            _targetPosition = targetPosition;
            _instanceId = instanceId;
        }

        public void Redo()
        {
            SelectableObject selectable = _sceneManager.GetAssetInstance(_instanceId);
            selectable.Transform.position = _targetPosition;
        }

        public void Undo()
        {
            SelectableObject selectable = _sceneManager.GetAssetInstance(_instanceId);
            selectable.Transform.position = _originalPosition;
        }
    }
}
