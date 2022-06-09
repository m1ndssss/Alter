using Unity.Entities;
using UnityEngine;

namespace Alter.Characters
{
    [GenerateAuthoringComponent]
    // ReSharper disable once Unity.RedundantAttributeOnTarget
    [AddComponentMenu("Alter/Characters/Player Tag")]
    // ReSharper disable once RequiredBaseTypesIsNotInherited
    public struct PlayerTag : IComponentData
    {
    }
}
