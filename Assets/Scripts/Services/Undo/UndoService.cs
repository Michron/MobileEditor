#nullable enable

using System;
using System.Collections.Generic;

namespace MobileEditor.Services.Undo
{
    /// <summary>
    /// Service responsible for managing undo actions in the scene.
    /// </summary>
    public class UndoService
    {
        /// <summary>
        /// Invoked whenever the current head of the undo stack has changed.
        /// </summary>
        public event Action<int>? UndoHeadChanged;

        private readonly List<IUndoAction> _undoActions = new();

        private int _head = -1;

        /// <summary>
        /// Whether an undo action can be performed.
        /// </summary>
        public bool CanUndo => _head >= 0;

        /// <summary>
        /// Whether a redo action can be performed.
        /// </summary>
        public bool CanRedo => _head < _undoActions.Count - 1;

        /// <summary>
        /// Register an <see cref="IUndoAction"/> instance.
        /// </summary>
        /// <param name="action">The action to register.</param>
        public void RegisterUndo(IUndoAction action)
        {
            TrimRedoActions();

            _undoActions.Add(action);
            ++_head;

            OnUndoHeadChanged();
        }

        /// <summary>
        /// Undo the last registered action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if there's no action to undo.</exception>
        public void Undo()
        {
            if(_head < 0)
            {
                throw new InvalidOperationException("Unable to perform an undo action, because there's no actions left to undo.");
            }

            IUndoAction action = _undoActions[_head];

            action.Undo();

            // Set the head to the previous action.
            --_head;

            OnUndoHeadChanged();
        }

        /// <summary>
        /// Redo the last undone action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if there's no action to redo.</exception>
        public void Redo()
        {
            if (_head + 1 >= _undoActions.Count)
            {
                throw new InvalidOperationException("Unable to perform a redo action, because there's no actions left redo.");
            }

            // Set the head to the next action.
            ++_head;

            IUndoAction action = _undoActions[_head];

            action.Redo();

            OnUndoHeadChanged();
        }

        private void TrimRedoActions()
        {
            // If there's nothing to redo we don't need to trim.
            if (!CanRedo)
            {
                return;
            }

            // If the head is already all the way at the start, just clear all actions.
            if(_head < 0)
            {
                _undoActions.Clear();
            }

            // Otherwise remove the range that's past the head's index.
            _undoActions.RemoveRange(_head + 1, _undoActions.Count - _head - 1);
        }

        private void OnUndoHeadChanged()
        {
            UndoHeadChanged?.Invoke(_head);
        }
    }
}
