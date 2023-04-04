#nullable enable

using MobileEditor.SceneManagement;
using MobileEditor.UI;
using UnityEngine;

namespace MobileEditor.Services.Selection
{
    /// <summary>
    /// Service responsible for keeping track of the current selection.
    /// </summary>
    public class SelectionService : MonoBehaviour
    {
        private UIManager? _uiManager;
        private Camera _camera = null!;
        private RectTransform _canvasRect = null!;

        /// <summary>
        /// The currently selection object.
        /// </summary>
        public SelectableObject? CurrentSelection { get; private set; }

        /// <summary>
        /// Initialize this <see cref="SelectionService"/>.
        /// </summary>
        /// <param name="uiManager">The <see cref="UIManager"/> used in the scene.</param>
        /// <param name="cameraController">The <see cref="CameraController"/> used in the scene.</param>
        public void Initialize(UIManager uiManager, CameraController cameraController)
        {
            _uiManager = uiManager;
            _canvasRect = uiManager.GetComponent<RectTransform>();
            _camera = cameraController.Camera;
        }

        private void LateUpdate()
        {
            if (CurrentSelection != null)
            {
                // Instead of converting the position to screenspace, it needs to be converted to canvas space.
                // The seleciton indicator is anchored in the bottom-left, so no offset needs to be applied to the canvas point.
                Vector3 viewportPoint = _camera.WorldToViewportPoint(CurrentSelection.Center);
                Vector3 canvasPoint = new Vector2(
                    viewportPoint.x * _canvasRect.sizeDelta.x,
                    viewportPoint.y * _canvasRect.sizeDelta.y);

                _uiManager!.MoveSelectionIndicator(canvasPoint);
            }
        }

        /// <summary>
        /// Change the current selection to the specified <paramref name="selectable"/>.
        /// </summary>
        /// <param name="selectable">The object to select, or <see langword="null"/> to clear the selection.</param>
        public void ChangeSelection(SelectableObject? selectable)
        {
            CurrentSelection = selectable;

            if (selectable != null)
            {
                _uiManager!.ShowSelectionIndicator();
            }
            else
            {
                _uiManager!.HideSelectionIndicator();
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public void ClearSelection()
        {
            _uiManager!.HideSelectionIndicator();
            CurrentSelection = null;
        }
    }
}
