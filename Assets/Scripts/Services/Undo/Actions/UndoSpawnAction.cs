using System;
using MobileEditor.SceneManagement;
using MobileEditor.Services.Selection;
using UnityEngine;

namespace MobileEditor.Services.Undo.Actions
{
    /// <summary>
    /// Undo action for spawn operations.
    /// </summary>
    internal sealed class UndoSpawnAction : IUndoAction
    {
        private readonly SceneManager _sceneManager;
        private readonly AssetDescriptor _assetDescriptor;
        private readonly Transform _parent;
        private readonly Vector3 _position;
        private readonly int _assetId;
        private readonly int _instanceId;

        public UndoSpawnAction(SceneManager sceneManager, AssetDescriptor assetDescriptor, Transform parent, Vector3 position, int assetId, int instanceId)
        {
            _sceneManager = sceneManager;
            _assetDescriptor = assetDescriptor;
            _parent = parent;
            _position = position;
            _assetId = assetId;
            _instanceId = instanceId;
        }

        public void Redo()
        {
            GameObject gameObject = GameObject.Instantiate<GameObject>(_assetDescriptor.Asset, _position, Quaternion.identity, _parent);

            if (!gameObject.TryGetComponent(out SelectableObject selectable))
            {
                throw new ArgumentException($"The {nameof(gameObject)} does not have a {nameof(SelectableObject)} component.");
            }

            // The IDs on the selectable were lost after respawning, so assign them again.
            selectable.AssetId = _assetId;
            selectable.InstanceId = _instanceId;

            // Register the selectable in the registry again.
            _sceneManager.RestoreAssetInstance(selectable);
        }

        public void Undo()
        {
            // Get the selectable from the registry, because references to the original selectable might be gone already.
            SelectableObject selectable = _sceneManager.GetAssetInstance(_instanceId);

            // Remove the object from the registry so it's not still saved with the scene.
            _sceneManager.RemoveAssetInstance(selectable.InstanceId);

            GameObject.Destroy(selectable.gameObject);
        }
    }
}
