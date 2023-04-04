#nullable enable

using System;
/// <summary>
/// Interface that defines an undoable and redoable action.
/// </summary>
public interface IUndoAction
{
    public void Undo();
    public void Redo();
}
