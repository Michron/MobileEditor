#nullable enable

/// <summary>
/// Interface that defines an undoable and redoable action.
/// </summary>
public interface IUndoAction
{
    /// <summary>
    /// Undo this action.
    /// </summary>
    public void Undo();

    /// <summary>
    /// Redo this action.
    /// </summary>
    public void Redo();
}
