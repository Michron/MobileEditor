#nullable enable

using System;
using System.Collections.Generic;
using MobileEditor.Services.Selection;

namespace MobileEditor.Assets.Scripts.SceneManagement
{
    /// <summary>
    /// Functions as a lookup for <see cref="SelectableObject"/> instances in the scene.
    /// </summary>
    internal class ObjectRegistry
    {
        private readonly Dictionary<int, SelectableObject> _objectLookup = new();
        private readonly HashSet<SelectableObject> _objects = new();

        /// <summary>
        /// A read-only collection of all <see cref="SelectableObject"/> instances in the scene.
        /// </summary>
        public IReadOnlyCollection<SelectableObject> Objects => _objects;

        /// <summary>
        /// Add the specified <paramref name="selectableObject"/> to the registry.
        /// </summary>
        /// <param name="selectableObject">The object to add.</param>
        /// <returns>
        /// <see langword="true"/> if the object was added to the registry, <see langword="false"/> otherwise.
        /// </returns>
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

        /// <summary>
        /// Add the specified <paramref name="selectableObject"/> to the registry,
        /// and throw an exception if the object couldn't be added.
        /// </summary>
        /// <param name="selectableObject">The object to add.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the object could not be added to the registry, likely because it or an object with the same ID is already registered.
        /// </exception>
        public void AddObjectChecked(SelectableObject selectableObject)
        {
            if (!AddObject(selectableObject))
            {
                throw new ArgumentException($"Unable to add an object to the registry with instance ID {selectableObject.InstanceId}.");
            }
        }

        /// <summary>
        /// Get an object from the registry.
        /// </summary>
        /// <param name="instanceId">The instance ID of the object to get.</param>
        /// <returns>
        /// The <see cref="SelectableObject"/> instance that belongs to the ID, or <see langword="null"/> if the ID was not found.
        /// </returns>
        public SelectableObject? GetObject(int instanceId)
        {
            if (_objectLookup.TryGetValue(instanceId, out SelectableObject selectable))
            {
                return selectable;
            }

            return null;

        }

        /// <summary>
        /// Get an object from the registry, and throws an exception if the object couldn't be found.
        /// </summary>
        /// <param name="instanceId">The instance ID of the object to get.</param>
        /// <returns>The <see cref="SelectableObject"/> instance that belongs to the ID.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="instanceId"/> does not match with any ID in the registry.
        /// </exception>
        public SelectableObject GetObjectChecked(int instanceId)
        {
            SelectableObject? selectableObject = GetObject(instanceId);

            if (selectableObject == null)
            {
                throw new ArgumentException($"Unable to get an object from the registry with instance ID {instanceId}.");
            }

            return selectableObject;
        }

        /// <summary>
        /// Removes an object from the registry.
        /// </summary>
        /// <param name="instanceId">The ID of the object to remove.</param>
        /// <returns>
        /// <see langword="true"/> if the object was removed from the registry, <see langword="false"/> otherwise.
        /// </returns>
        public bool RemoveObject(int instanceId)
        {
            if (!_objectLookup.TryGetValue(instanceId, out SelectableObject? selectableObject))
            {
                return false;
            }

            return _objects.Remove(selectableObject) && _objectLookup.Remove(selectableObject.InstanceId);

        }

        /// <summary>
        /// Removes an object from the registry, and throws an exception if the object could not be removed.
        /// </summary>
        /// <param name="instanceId">The ID of the object to remove.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="instanceId"/> does not match an ID in the registry.
        /// </exception>
        public void RemoveObjectChecked(int instanceId)
        {
            if (!RemoveObject(instanceId))
            {
                throw new ArgumentException($"Unable to remove an object from the registry with instance ID {instanceId}.");
            }
        }
    }
}
