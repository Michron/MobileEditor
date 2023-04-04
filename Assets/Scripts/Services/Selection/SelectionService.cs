#nullable enable

using MobileEditor.SceneManagement;
using MobileEditor.UI;
using UnityEngine;

namespace MobileEditor.Services.Selection
{
    public class SelectionService : MonoBehaviour
    {
        private UIManager? _uiManager;
        private Camera _camera = null!;
        private RectTransform _canvasRect = null!;

        public SelectableObject? CurrentSelection { get; private set; }

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

        public void ChangeSelection(SelectableObject? selectable)
        {
            CurrentSelection = selectable;
            
            if(selectable != null)
            {
                _uiManager!.ShowSelectionIndicator();
            }
            else
            {
                _uiManager!.HideSelectionIndicator();
            }
        }

        public void ClearSelection()
        {
            _uiManager!.HideSelectionIndicator();
            CurrentSelection = null;
        }
    }
}
