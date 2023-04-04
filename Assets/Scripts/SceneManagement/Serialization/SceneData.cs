using System;
using System.Collections.Generic;
using MobileEditor.Services.Selection;
using UnityEngine;

namespace MobileEditor.SceneManagement.Serialization
{
    /// <summary>
    /// Descriptor for a scene that can be used for serialization and deserialization.
    /// </summary>
    [Serializable]
    public struct SceneData
    {
        /// <summary>
        /// A descriptor for a scene object.
        /// </summary>
        [Serializable]
        public struct SceneObjectData
        {
            /// <summary>
            /// The ID of the asset that belongs to this object.
            /// </summary>
            public int AssetId;

            /// <summary>
            /// The position in world space of the object.
            /// </summary>
            public Vector3 Position;
        }

        /// <summary>
        /// A collection of <see cref="SceneObjectData"/> describing the objects in the current scene.
        /// </summary>
        public SceneObjectData[] SceneObjects;

        private SceneData(SceneObjectData[] sceneObjects)
        {
            SceneObjects = sceneObjects;
        }

        /// <summary>
        /// Creates a new <see cref="SceneData"/> instance containing the specified <paramref name="objects"/>.
        /// </summary>
        /// <param name="objects">The objects to add to the <see cref="SceneData"/> instance.</param>
        /// <returns>A new <see cref="SceneData"/> instance which contains the data from <paramref name="objects"/>.</returns>
        public static SceneData Create(IReadOnlyCollection<SelectableObject> objects)
        {
            SceneObjectData[] sceneObjects = new SceneObjectData[objects.Count];
            int i = 0;

            foreach (SelectableObject selectableObject in objects)
            {
                sceneObjects[i] = new SceneObjectData()
                {
                    AssetId = selectableObject.AssetId,
                    Position = selectableObject.Transform.position
                };

                ++i;
            }

            return new SceneData(sceneObjects);
        }

        /// <summary>
        /// Serialize the <paramref name="sceneData"/> to a JSON string.
        /// </summary>
        /// <param name="sceneData">The data to serialize.</param>
        /// <returns>A string containg the scene data in JSON format.</returns>
        public static string Serialize(SceneData sceneData)
        {
            return JsonUtility.ToJson(sceneData);
        }

        /// <summary>
        /// Deserializes the specified JSON data to a new <see cref="SceneData"/> instance.
        /// </summary>
        /// <param name="data">The JSON data to deserialize.</param>
        /// <returns>A new <see cref="SceneData"/> instance containing the deserialized scene data.</returns>
        public static SceneData Deserialize(string data)
        {
            return JsonUtility.FromJson<SceneData>(data);
        }
    }
}
