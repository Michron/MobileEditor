#nullable enable

using System;
using System.IO;
using MobileEditor.Assets.Scripts.SceneManagement;
using MobileEditor.InputHandling;
using MobileEditor.InputHandling.InputStates;
using MobileEditor.SceneManagement.Serialization;
using MobileEditor.Services.Selection;
using MobileEditor.Services.Undo;
using MobileEditor.Services.Undo.Actions;
using MobileEditor.UI;
using UnityEngine;

namespace MobileEditor.SceneManagement
{
    [RequireComponent(typeof(InputHandler), typeof(ObjectController), typeof(SelectionService))]
    public class SceneManager : MonoBehaviour
    {
        private const string SceneDataFile = "SceneData.json";

        [SerializeField]
        private UIManager? _uiManager;
        [SerializeField]
        private CameraController? _cameraController;
        [SerializeField]
        private Transform? _sceneRoot;
        [SerializeField]
        private AssetDescriptor[]? _assetDescriptors;

        private readonly ObjectRegistry _objectRegistry = new();
        private readonly UndoService _undoService = new();

        private SelectionService _selectionService = null!;
        private ObjectController _objectController = null!;
        private InputHandler _inputHandler = null!;

        private bool _isNewObject;
        private int _instanceIndex;

        private void Awake()
        {
            _selectionService = GetComponent<SelectionService>();
            _inputHandler = GetComponent<InputHandler>();
            _objectController = GetComponent<ObjectController>();

            Debug.Assert(_inputHandler != null);
            Debug.Assert(_objectController != null);
            Debug.Assert(_sceneRoot != null);
            Debug.Assert(_uiManager != null);

            if (_sceneRoot == null)
            {
                Debug.LogWarning("Missing sceneroot object. A placeholder object will be used instead.");
                GameObject root = new("SceneRoot");
                _sceneRoot = root.transform;
            }

            for (int i = 0; i < _assetDescriptors!.Length; i++)
            {
                _assetDescriptors[i].Id = i;
            }
        }

        private void Start()
        {
            _inputHandler.Initialize(_uiManager!, _cameraController!, _objectController, _selectionService);
            _selectionService.Initialize(_uiManager!, _cameraController!);

            _uiManager!.SetAssetImages(_assetDescriptors);
            _uiManager.AssetDragged += SpawnAsset;
            _uiManager.DeleteObject += DeleteSelection;
            _uiManager.UndoInvoked += UndoInvoked;
            _uiManager.RedoInvoked += RedoInvoked;

            _objectController.MovedObject += MovedObject;

            _undoService.UndoHeadChanged += UndoHeadChanged;

            LoadScene();
        }

        public void RestoreAssetInstance(SelectableObject selectable)
        {
            _objectRegistry.AddObjectChecked(selectable);
        }

        public SelectableObject GetAssetInstance(int instanceId)
        {
            return _objectRegistry.GetObjectChecked(instanceId);
        }

        public void RemoveAssetInstance(int instanceId)
        {
            _objectRegistry.RemoveObjectChecked(instanceId);
        }

        private void SpawnAsset(int index)
        {
            SelectableObject selectable = SpawnAssetCore(index);

            // Enable the new object flag, so we know how to treat the next deleted or moved event.
            _isNewObject = true;

            _selectionService.ChangeSelection(selectable);

            // Switch to the DragObjectState to immediately start dragging the object.
            _inputHandler.ChangeState<DragObjectState>();

            // Don't save the scene here yet. Instead once the DragObjectState finishes, it will save the scene instead.
        }

        private SelectableObject SpawnAssetCore(int index)
        {
            AssetDescriptor descriptor = _assetDescriptors![index];
            GameObject instance = Instantiate(descriptor.Asset, _sceneRoot);
            
            if (!instance.TryGetComponent(out SelectableObject selectable))
            {
                throw new ArgumentException($"Unable to get {nameof(SelectableObject)} component from asset.");
            }

            // Assign the index to the AssetId, which can be used later to lookup which asset belongs to this selectable.
            selectable.AssetId = index;

            // The instance ID is required for undo and redo actions, to be able to look up previously deleted objects again.
            selectable.InstanceId = _instanceIndex++;

            _objectRegistry.AddObjectChecked(selectable);

            return selectable;
        }

        private void DeleteSelection()
        {
            SelectableObject? selectable = _selectionService.CurrentSelection;

            if (selectable == null)
            {
                Debug.LogError("Attempting to delete current selection, but nothing is selected.");

                return;
            }

            _selectionService.ClearSelection();

            GameObject gameObject = selectable.gameObject;
            RemoveAssetInstance(selectable.InstanceId);

            if (_isNewObject)
            {
                _isNewObject = false;

                // No need to create an undo event if the object was still new.
                Destroy(gameObject);

                return;
            }

            AssetDescriptor assetDescriptor = _assetDescriptors![selectable.AssetId];

            // The current position of the object is likely on the delete button, but we need the original position of the object.
            // The ObjectController still has this information stored for us.
            Vector3 position = _objectController.InitialPosition;

            UndoDeleteAction action = new UndoDeleteAction(this, assetDescriptor, _sceneRoot, position, selectable.AssetId, selectable.InstanceId);
            _undoService.RegisterUndo(action);

            Destroy(gameObject);

            SaveScene();
        }

        private void MovedObject(Transform transform, Vector3 initialPosition, Vector3 finalPosition)
        {
            if (_isNewObject)
            {
                _isNewObject = false;
                PlaceObject(transform, finalPosition);

                return;
            }

            if (!transform.TryGetComponent(out SelectableObject selectable))
            {
                throw new ArgumentException($"Moved object does not have a {nameof(SelectableObject)} component.");
            }

            UndoMoveAction action = new UndoMoveAction(this, initialPosition, finalPosition, selectable.InstanceId);
            _undoService.RegisterUndo(action);

            SaveScene();
        }

        private void PlaceObject(Transform transform, Vector3 position)
        {
            if (!transform.TryGetComponent(out SelectableObject selectable))
            {
                throw new ArgumentException($"Moved object does not have a {nameof(SelectableObject)} component.");
            }

            AssetDescriptor assetDescriptor = _assetDescriptors![selectable.AssetId];

            UndoSpawnAction action = new UndoSpawnAction(this, assetDescriptor, _sceneRoot, position, selectable.AssetId, selectable.InstanceId);
            _undoService.RegisterUndo(action);

            SaveScene();
        }

        private void UndoHeadChanged(int _)
        {
            _uiManager!.SetUndoButtonState(_undoService.CanUndo);
            _uiManager.SetRedoButtonState(_undoService.CanRedo);

            SaveScene();
        }

        private void RedoInvoked()
        {
            if (!_undoService.CanRedo)
            {
                Debug.LogWarning("Trying to invoke an redo, but there are no redo actions.");

                return;
            }

            _undoService.Redo();
            _selectionService.ClearSelection();
        }

        private void UndoInvoked()
        {
            if (!_undoService.CanUndo)
            {
                Debug.LogWarning("Trying to invoke an undo, but there are no undo actions.");

                return;
            }

            _undoService.Undo();
            _selectionService.ClearSelection();
        }

        private void SaveScene()
        {
            var sceneData = SceneData.Create(_objectRegistry.Objects);
            string jsonData = SceneData.Serialize(sceneData);

            string filePath = Path.Combine(Application.persistentDataPath, SceneDataFile);

            try
            {
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        private void LoadScene()
        {
            string filePath = Path.Combine(Application.persistentDataPath, SceneDataFile);

            if (!File.Exists(filePath))
            {
                return;
            }

            string jsonData = File.ReadAllText(filePath);
            SceneData sceneData = SceneData.Deserialize(jsonData);

            for (int i = 0; i < sceneData.SceneObjects.Length; ++i)
            {
                SceneData.SceneObjectData objectData = sceneData.SceneObjects[i];

                SelectableObject selectable = SpawnAssetCore(objectData.AssetId);

                selectable.Transform.position = objectData.Position;
            }
        }
    }
}
