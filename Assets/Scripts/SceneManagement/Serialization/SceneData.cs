using System;
using System.Collections.Generic;
using MobileEditor.Services.Selection;
using UnityEngine;

namespace MobileEditor.SceneManagement.Serialization
{
    [Serializable]
    public class SceneData
    {
        [Serializable]
        public struct SceneObjectData
        {
            public int AssetId;
            public Vector3 Position;
        }

        public SceneObjectData[] SceneObjects;

        private SceneData(SceneObjectData[] sceneObjects)
        {
            SceneObjects = sceneObjects;
        }

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

        public static string Serialize(SceneData sceneData)
        {
            return JsonUtility.ToJson(sceneData);
        }

        public static SceneData Deserialize(string data)
        {
            return JsonUtility.FromJson<SceneData>(data);
        }
    }
}
