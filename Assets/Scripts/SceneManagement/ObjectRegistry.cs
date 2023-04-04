#nullable enable

using System;
using System.Collections.Generic;
using MobileEditor.Services.Selection;

namespace MobileEditor.Assets.Scripts.SceneManagement
{
    internal class ObjectRegistry
    {
        private readonly Dictionary<int, SelectableObject> _objectLookup = new();
        private readonly HashSet<SelectableObject> _objects = new();

        public IReadOnlyCollection<SelectableObject> Objects => _objects;

        public bool AddObject(SelectableObject selectableObject)
        {
            if (!_objects.Add(selectableObject))
            {
                return false;
            }

            if (!_objectLookup.TryAdd(selectableObject.InstanceId, selectableObject))
            {
                return false;
            }

            return true;
        }

        public void AddObjectChecked(SelectableObject selectableObject)
        {
            if (!AddObject(selectableObject))
            {
                throw new ArgumentException($"Unable to add an object to the registry with instance ID {selectableObject.InstanceId}.");
            }
        }

        public SelectableObject? GetObject(int instanceId)
        {
            if (_objectLookup.TryGetValue(instanceId, out SelectableObject selectable))
            {
                return selectable;
            }

            return null;

        }

        public SelectableObject GetObjectChecked(int instanceId)
        {
            SelectableObject? selectableObject = GetObject(instanceId);

            if (selectableObject == null)
            {
                throw new ArgumentException($"Unable to get an object from the registry with instance ID {instanceId}.");
            }

            return selectableObject;
        }

        public bool RemoveObject(int instanceId)
        {
            if (!_objectLookup.TryGetValue(instanceId, out SelectableObject? selectableObject))
            {
                return false;
            }

            return _objects.Remove(selectableObject) && _objectLookup.Remove(selectableObject.InstanceId);

        }

        public void RemoveObjectChecked(int instanceId)
        {
            if (!RemoveObject(instanceId))
            {
                throw new ArgumentException($"Unable to remove an object from the registry with instance ID {instanceId}.");
            }
        }
    }
}
