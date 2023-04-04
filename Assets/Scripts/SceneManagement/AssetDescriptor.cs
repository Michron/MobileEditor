using UnityEngine;

namespace MobileEditor.SceneManagement
{
    /// <summary>
    /// Describes an asset that can be spawned during runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetDescriptor", menuName = "Scriptable Objects/Asset Descriptor")]
    public class AssetDescriptor : ScriptableObject
    {
        /// <summary>
        /// The sprite that will be used as the icon for this asset.
        /// </summary>
        public Sprite Icon;

        /// <summary>
        /// The asset that this descriptor references.
        /// </summary>
        public GameObject Asset;

        /// <summary>
        /// The ID of the asset, generated at startup.
        /// </summary>
        public int Id { get; set; }
    }
}
