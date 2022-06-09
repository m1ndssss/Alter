using Unity.Entities;

namespace Alter.Characters
{
    [GenerateAuthoringComponent]
    public struct PlayerData : IComponentData
    {
        public float Health;
        public float Armor;
    }
}
