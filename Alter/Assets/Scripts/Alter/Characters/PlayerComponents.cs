using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Alter.Characters
{
    [GhostComponent(PrefabType = GhostPrefabType.All, OwnerPredictedSendType = GhostSendType.None)]
    public struct PlayerInput : ICommandData
    {
        [GhostField]
        public uint Tick { get; set; }

        [GhostField(Quantization = 1000)]
        public float3 Translation;
        [GhostField(Quantization = 1000)]
        public float2 Rotation;

        [GhostField] public bool firingInput;
    }

    public struct PlayerController : IComponentData
    {
        public float3 Velocity;
        public bool OnGround;
    }
}
