using System;
using MobileEditor.SceneManagement;
using MobileEditor.Services.Selection;
using UnityEngine;

namespace MobileEditor.Services.Undo.Actions
{
    internal sealed class UndoDeleteAction : IUndoAction
    {
        private readonly SceneManager _sceneManager;
        private readonly AssetDescriptor _assetDescriptor;
        private readonly Transform _parent;
        private readonly Vector3 _position;
        private readonly int _assetId;
        private readonly int _instanceId;

        public UndoDeleteAction(SceneManager sceneManager, AssetDescriptor assetDescriptor, Transform parent, Vector3 position, int assetId, int instanceId)
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
            SelectableObject selectable = _sceneManager.GetAssetInstance(_instanceId);
            _sceneManager.RemoveAssetInstance(selectable.InstanceId);

            GameObject.Destroy(selectable.gameObject);
        }

        public void Undo()
        {
            GameObject gameObject = GameObject.Instantiate<GameObject>(_assetDescriptor.Asset, _position, Quaternion.identity, _parent);

            if (!gameObject.TryGetComponent(out SelectableObject selectable))
            {
                throw new ArgumentException($"The {nameof(gameObject)} does not have a {nameof(SelectableObject)} component.");
            }

            selectable.AssetId = _assetId;
            selectable.InstanceId = _instanceId;

            _sceneManager.RestoreAssetInstance(selectable);
        }
    }
}
