#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace MobileEditor.Services.Selection
{
    /// <summary>
    /// Marks a GameObject as selectable in the scene, meaning it can be moved around and deleted.
    /// </summary>
    public class SelectableObject : MonoBehaviour
    {
        private static readonly List<Renderer> RendererCache = new();

        private float _size;

        /// <summary>
        /// The ID of the asset this was spawned from.
        /// </summary>
        public int AssetId { get; internal set; }

        /// <summary>
        /// The instance ID of this object.
        /// </summary>
        public int InstanceId { get; internal set; }

        /// <summary>
        /// The approximate center of the object, based on its bounds.
        /// </summary>
        public Vector3 Center => Transform.position + new Vector3(0.0f, _size, 0.0f);

        /// <summary>
        /// Reference to the <see cref="UnityEngine.Transform"/> of this object.
        /// </summary>
        public Transform Transform { get; set; } = null!;

        private void Awake()
        {
            Transform = transform;

            InitializeSelectionCollider();
        }

        private void InitializeSelectionCollider()
        {
            _size = GetObjectSize();

            GameObject child = new GameObject("SelectionCollider");
            child.transform.parent = transform;
            child.transform.localPosition = new Vector3();
            child.layer = LayerMask.NameToLayer("Selection");

            // Use a sphere collider based on the size of the object to get an approximate selection area.
            SphereCollider collider = child.AddComponent<SphereCollider>();
            collider.radius = _size;
            collider.center = new Vector3(0.0f, _size, 0.0f);
        }

        private float GetObjectSize()
        {
            RendererCache.Clear();
            GetComponentsInChildren(RendererCache);

            // At minimum each object will have a bounds of 1 unit.
            Bounds bounds = new Bounds(transform.position + new Vector3(0.0f, 0.5f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f));

            foreach (Renderer renderer in RendererCache)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            // This will be converted to a sphere's radius, so we only need the largest extent.
            return Mathf.Max(bounds.extents.x, bounds.extents.y);
        }
    }
}
