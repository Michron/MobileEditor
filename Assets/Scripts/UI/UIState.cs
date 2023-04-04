namespace MobileEditor.UI
{
    /// <summary>
    /// Defines a state the UI can be in.
    /// </summary>
    public enum UIState
    {
        /// <summary>
        /// The default state of the UI, showing the assets hotbar, and undo redo buttons.
        /// </summary>
        Default,
        /// <summary>
        /// A minimal UI which only shows an icon for deleting the current asset.
        /// </summary>
        AssetEditing
    }
}
