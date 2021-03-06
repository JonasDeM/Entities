using Unity.Entities;
using UnityEngine;

namespace Unity.Scenes
{
    /// <summary>
    /// The group of systems responsible for loading and unloading scenes.
    /// </summary>
    [ExecuteAlways]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class SceneSystemGroup : ComponentSystemGroup
    {
    }
}
