#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace MobileEditor.Services.Selection
{
    public class SelectableObject : MonoBehaviour
    {
        private static readonly List<Renderer> RendererCache = new();

        private float _size;
        private Transform _transform = null!;

        public int AssetId { get; internal set; }
        public int InstanceId { get; internal set; }

        public Vector3 Center => Transform.position + new Vector3(0.0f, _size, 0.0f);
        public Transform Transform { get => _transform; set => _transform = value; }

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

            return Mathf.Max(bounds.extents.x, bounds.extents.y);
        }
    }
}
