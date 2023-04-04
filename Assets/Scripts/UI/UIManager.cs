#nullable enable

using System;
using MobileEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobileEditor.UI
{
    /// <summary>
    /// Manages the main UI of the scene.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>
        /// Invoked when an asset icon in the UI is dragged. The index of the asset is passed on to the delegate.
        /// </summary>
        public event Action<int>? AssetDragged;

        /// <summary>
        /// Invoked when a pointer is released on the delete button.
        /// </summary>
        public event Action? DeleteObject;

        /// <summary>
        /// Invoked when the undo button is pressed.
        /// </summary>
        public event Action? UndoInvoked;

        /// <summary>
        /// Invoked when the redo button is pressed.
        /// </summary>
        public event Action? RedoInvoked;

        [SerializeField]
        private GameObject? _selectionGroup;
        [SerializeField]
        private GameObject? _undoGroup;
        [SerializeField]
        private GameObject? _assetsGroup;
        [SerializeField]
        private GameObject? _deleteGroup;
        [SerializeField]
        private Button? _undoButton;
        [SerializeField]
        private Button? _redoButton;
        [SerializeField]
        private Image[]? _assetImages;
        [SerializeField]
        private Image? _deleteImage;
        [SerializeField]
        [Tooltip("Custom highlight color for the delete button when it's being hovered over.")]
        private Color _deleteHighlightColor;

        private Color _initialDeleteColor;
        private bool _showSelectionIndicator;
        private RectTransform? _selectionTransform;

        private void Awake()
        {
            Debug.Assert(_selectionGroup != null);
            Debug.Assert(_undoGroup != null);
            Debug.Assert(_assetsGroup != null);
            Debug.Assert(_deleteGroup != null);
            Debug.Assert(_undoButton != null);
            Debug.Assert(_redoButton != null);
            Debug.Assert(_assetImages != null);
            Debug.Assert(_deleteImage != null);

            _selectionTransform = _selectionGroup!.GetComponent<RectTransform>();

            ChangeUIState(UIState.Default);
            DisableUndo();
            DisableRedo();
            HideSelectionIndicator();

            InitializeAssetTriggers();
            InitializeDeleteTriggers();

            _undoButton!.onClick.AddListener(OnUndoButtonClicked);
            _redoButton!.onClick.AddListener(OnRedoButtonClicked);
        }

        /// <summary>
        /// Change the UI state to the specified state.
        /// </summary>
        /// <param name="state">The state to switch to.</param>
        public void ChangeUIState(UIState state)
        {
            switch (state)
            {
                case UIState.Default:
                    RestoreSelectionIndicator();
                    _undoGroup!.SetActive(true);
                    _assetsGroup!.SetActive(true);
                    _deleteGroup!.SetActive(false);
                    break;
                case UIState.AssetEditing:
                    ResetDeleteImage();
                    DisableSelectionIndicator();
                    _undoGroup!.SetActive(false);
                    _assetsGroup!.SetActive(false);
                    _deleteGroup!.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// Assign values to the asset images in the UI based on their respective <see cref="AssetDescriptor"/>.
        /// </summary>
        /// <param name="descriptors">The <see cref="AssetDescriptor"/> instances to apply to the UI.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="descriptors"/> contains more instances than the UI has images available for the assets.
        /// </exception>
        public void SetAssetImages(ReadOnlySpan<AssetDescriptor> descriptors)
        {
            // For now the amount of spawnable assets will be limited to the amount of slots that are in the UI.
            if (_assetImages!.Length < descriptors.Length)
            {
                throw new ArgumentException(
                    $"Unable to assign sprites to asset images, because the size of {nameof(descriptors)} is larger than the size of {nameof(_assetImages)}.");
            }

            for (int i = 0; i < _assetImages.Length; i++)
            {
                if (i < descriptors.Length)
                {
                    _assetImages[i].sprite = descriptors[i].Icon;
                }
                else
                {
                    _assetImages[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Set the interactable state of the undo button.
        /// </summary>
        /// <param name="enabled">The interactable state to apply to the button.</param>
        public void SetUndoButtonState(bool enabled)
        {
            _undoButton!.interactable = enabled;
        }

        /// <summary>
        /// Set the state of the redo button.
        /// </summary>
        /// <param name="enabled">The interactable state to apply to the button.</param>
        public void SetRedoButtonState(bool enabled)
        {
            _redoButton!.interactable = enabled;
        }

        /// <summary>
        /// Disable interaction for the undo button.
        /// </summary>
        public void DisableUndo()
        {
            _undoButton!.interactable = false;
        }

        /// <summary>
        /// Disable interaciton for the redo button.
        /// </summary>
        public void DisableRedo()
        {
            _redoButton!.interactable = false;
        }

        /// <summary>
        /// Show the selection indicator in the UI.
        /// </summary>
        public void ShowSelectionIndicator()
        {
            _showSelectionIndicator = true;
            _selectionGroup!.SetActive(true);
        }

        /// <summary>
        /// Hide the seleciton indicator in the UI.
        /// </summary>
        public void HideSelectionIndicator()
        {
            _showSelectionIndicator = false;
            _selectionGroup!.SetActive(false);
        }

        /// <summary>
        /// Move the selection indicator to the specified screen position.
        /// </summary>
        /// <param name="screenPosition">The position in the screen to move the indicator to.</param>
        public void MoveSelectionIndicator(Vector2 screenPosition)
        {
            _selectionTransform!.localPosition = screenPosition;
        }

        private void InitializeAssetTriggers()
        {
            for (int i = 0; i < _assetImages!.Length; i++)
            {
                EventTrigger? trigger = _assetImages[i].GetComponent<EventTrigger>();
                Debug.Assert(trigger != null);

                EventTrigger.Entry entry = new()
                {
                    eventID = EventTriggerType.PointerDown
                };

                // Create a copy of i, otherwise it will be captured in the lambda and still get incremented.
                int index = i;

                entry.callback.AddListener((_) => { OnAssetDragged(index); });
                trigger!.triggers.Add(entry);
            }
        }

        private void InitializeDeleteTriggers()
        {
            static void AddTrigger(EventTrigger trigger, EventTriggerType triggerType, UnityAction<BaseEventData> action)
            {
                EventTrigger.Entry entry = new()
                {
                    eventID = triggerType
                };

                entry.callback.AddListener(action);
                trigger!.triggers.Add(entry);
            }

            // Also use this opportunity to store the current color of the image.
            _initialDeleteColor = _deleteImage!.color;

            if (!_deleteImage.TryGetComponent(out EventTrigger trigger))
            {
                throw new MissingComponentException($"{_deleteImage.gameObject.name} is missing an {nameof(EventTrigger)} component.");
            }

            AddTrigger(trigger!, EventTriggerType.PointerUp, OnDeleteObject);
            AddTrigger(trigger!, EventTriggerType.PointerEnter, EnterDeleteImage);
            AddTrigger(trigger!, EventTriggerType.PointerExit, ExitDeleteImage);
        }

        private void DisableSelectionIndicator()
        {
            _selectionGroup!.SetActive(false);
        }

        private void RestoreSelectionIndicator()
        {
            _selectionGroup!.SetActive(_showSelectionIndicator);
        }

        private void EnterDeleteImage(BaseEventData eventData)
        {
            if (eventData is not PointerEventData pointerEventData)
            {
                return;
            }

            // Set the pointerPress object to the delete image when we hover over the image.
            // Without this, the PointerUp event would not be invoked on the image.
            pointerEventData.pointerPress = _deleteImage!.gameObject;

            HighlightDeleteImage();
        }

        private void ExitDeleteImage(BaseEventData eventData)
        {
            if (eventData is not PointerEventData pointerEventData)
            {
                return;
            }

            // Reset the pointerPress, otherwise we might get an unintended PointerUp event.
            pointerEventData.pointerPress = null;

            ResetDeleteImage();
        }

        private void ResetDeleteImage()
        {
            _deleteImage!.CrossFadeColor(_initialDeleteColor, duration: 0.1f, ignoreTimeScale: true, useAlpha: false);
        }

        private void HighlightDeleteImage()
        {
            _deleteImage!.CrossFadeColor(_deleteHighlightColor, duration: 0.1f, ignoreTimeScale: true, useAlpha: false);
        }

        private void OnDeleteObject(BaseEventData _)
        {
            DeleteObject?.Invoke();
        }

        private void OnAssetDragged(int index)
        {
            AssetDragged?.Invoke(index);
        }
        private void OnRedoButtonClicked()
        {
            RedoInvoked?.Invoke();
        }

        private void OnUndoButtonClicked()
        {
            UndoInvoked?.Invoke();
        }
    }
}
