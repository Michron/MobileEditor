#nullable enable

using System;
using MobileEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobileEditor.UI
{
    public class UIManager : MonoBehaviour
    {
        public event Action<int>? AssetDragged;
        public event Action? DeleteObject;
        public event Action? UndoInvoked;
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

        public void SetAssetImages(ReadOnlySpan<AssetDescriptor> descriptors)
        {
            // For now the amount of spawnable assets will be limited to the amount of slots that are in the UI.
            if(_assetImages!.Length < descriptors.Length)
            {
                throw new ArgumentException(
                    $"Unable to assign sprites to asset images, because the size of {nameof(descriptors)} is larger than the size of {nameof(_assetImages)}.");
            }

            for(int i = 0; i < _assetImages.Length; i++)
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

        public void SetUndoButtonState(bool enabled)
        {
            _undoButton!.interactable = enabled;
        }

        public void SetRedoButtonState(bool enabled)
        {
            _redoButton!.interactable = enabled;
        }

        public void DisableUndo()
        {
            _undoButton!.interactable = false;
        }

        public void DisableRedo()
        {
            _redoButton!.interactable = false;
        }

        public void ShowSelectionIndicator()
        {
            _showSelectionIndicator = true;
            _selectionGroup!.SetActive(true);
        }

        public void HideSelectionIndicator()
        {
            _showSelectionIndicator = false;
            _selectionGroup!.SetActive(false);
        }

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
            // Also use this opportunity to store the current color of the image.
            _initialDeleteColor = _deleteImage!.color;

            EventTrigger? trigger = _deleteImage.GetComponent<EventTrigger>();
            Debug.Assert(trigger != null);

            // PointerUp
            {
                EventTrigger.Entry entry = new()
                {
                    eventID = EventTriggerType.PointerUp
                };

                entry.callback.AddListener(OnDeleteObject);
                trigger!.triggers.Add(entry);
            }

            // PointerEnter
            {
                EventTrigger.Entry entry = new()
                {
                    eventID = EventTriggerType.PointerEnter
                };

                entry.callback.AddListener(EnterDeleteImage);
                trigger.triggers.Add(entry);
            }

            // PointerExit
            {
                EventTrigger.Entry entry = new()
                {
                    eventID = EventTriggerType.PointerExit
                };

                entry.callback.AddListener(ExitDeleteImage);
                trigger.triggers.Add(entry);
            }
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
            if(eventData is not PointerEventData pointerEventData)
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
