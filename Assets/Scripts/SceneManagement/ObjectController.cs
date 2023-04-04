#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobileEditor.SceneManagement
{
    /// <summary>
    /// Manages editing selectable objects in the scene by smoothly moving them around.
    /// </summary>
    public class ObjectController : MonoBehaviour
    {
        /// <summary>
        /// Invoked when an object has finished moving.
        /// </summary>
        public event Action<Transform, Vector3, Vector3>? MovedObject;

        [SerializeField]
        [Range(0.0f, 5.0f)]
        [Tooltip("The minimal speed per second that an object that's being dragged will move at.")]
        private float _minMoveSpeed = 0.1f;
        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("If larger than 0, a smooth delay is applied to dragged objects.")]
        private float _moveInertiaTime = 0.2f;

        private readonly List<(Collider, bool)> _colliderStateCache = new();
        private readonly List<Collider> _colliderCache = new();

        private Transform? _transform;
        private Vector3 _initialPosition;
        private Vector3 _targetPosition;

        /// <summary>
        /// The initial position of the last moved object before it was moved.
        /// </summary>
        public Vector3 InitialPosition => _initialPosition;

        private void Update()
        {
            UpdateMoveAction();
        }

        /// <summary>
        /// Initiates a new move action for an object. This will temporarily disable colliders on the object to avoid collision with other object.
        /// </summary>
        /// <param name="transform">The object to move.</param>
        public void StartMoveAction(Transform transform)
        {
            _transform = transform;
            _initialPosition = _transform.position;

            // Disable colliders on the selected object to prevent side effects when moving it around.
            DisableColliders(_transform);
        }

        /// <summary>
        /// Updates the target position of the object that's currently being moved.
        /// </summary>
        /// <param name="targetPosition">The new target position.</param>
        public void SetMoveTarget(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
        }

        /// <summary>
        /// Finish moving the current object, and place it back in the scene.
        /// </summary>
        public void EndMoveAction()
        {
            if(_transform == null)
            {
                // This can happen if the object was deleted by the user.
                return;
            }

            // Snap the object to the target location when moving is ending.
            _transform!.position = _targetPosition;

            RestoreColliders();

            // Signal that a move action has been completed.
            MovedObject?.Invoke(_transform, _initialPosition, _targetPosition);

            _transform = null;
        }

        private void UpdateMoveAction()
        {
            if (_transform == null)
            {
                return;
            }

            if (_moveInertiaTime <= 0)
            {
                _transform.position = _targetPosition;

                return;
            }

            Vector3 position = _transform.position;
            Vector3 delta = _targetPosition - position;
            float speed = 1.0f / _moveInertiaTime;
            float distance = Mathf.Max(_minMoveSpeed, delta.magnitude);

            _transform.position = Vector3.MoveTowards(position, _targetPosition, distance * speed * Time.deltaTime);
        }

        private void DisableColliders(Transform transform)
        {
            // Empty the caches to make sure we start from 0.
            _colliderStateCache.Clear();
            _colliderCache.Clear();

            transform.GetComponentsInChildren(_colliderCache);

            for (int i = 0; i < _colliderCache.Count; ++i)
            {
                Collider collider = _colliderCache[i];

                _colliderStateCache.Add((collider, collider.enabled));

                collider.enabled = false;
            }

            // The collider cache is not needed anymore, so clear it to remove any lingering references.
            _colliderCache.Clear();
        }

        private void RestoreColliders()
        {
            for (int i = 0; i < _colliderStateCache.Count; ++i)
            {
                (Collider collider, bool state) = _colliderStateCache[i];

                collider.enabled = state;
            }

            // It's safe to assume that colliders will only need to be restored once.
            _colliderStateCache.Clear();
        }
    }
}
