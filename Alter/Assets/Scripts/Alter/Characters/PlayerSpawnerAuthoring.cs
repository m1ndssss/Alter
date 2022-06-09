using Unity.Entities;
using UnityEngine;

namespace Alter.Characters
{
    [GenerateAuthoringComponent]
    // ReSharper disable once Unity.RedundantAttributeOnTarget
    [AddComponentMenu("Alter/Characters/Player Spawner")]
    // ReSharper disable once RequiredBaseTypesIsNotInherited
    public struct PlayerSpawner : IComponentData
    {
        // ReSharper disable once UnassignedField.Global
        public Entity PlayerPrefab;
    }
}
