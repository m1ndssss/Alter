using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Alter.Characters.Client
{
    [UpdateInGroup(typeof(ClientSimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateBefore(typeof(GhostSimulationSystemGroup))]
    public partial class OwnedPlayerInputCollectionSystem : SystemBase
    {
        private Entity _ownedPlayer;

        private ClientSimulationSystemGroup _clientSimulationSystemGroup;

        protected override void OnCreate()
        {
            _ownedPlayer = Entity.Null;

            _clientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();

            RequireSingletonForUpdate<NetworkIdComponent>();
        }

        private void SetupOwnedPlayer()
        {
            var ownNetworkId = GetSingleton<NetworkIdComponent>().Value;
            var commandTargetEntity = GetSingletonEntity<CommandTargetComponent>();
            var camera = Camera.main;

            Entities
                .WithStructuralChanges()
                .WithAll<PlayerTag>()
                .WithNone<PlayerInput>()
                .ForEach((Entity player, in GhostOwnerComponent ghostOwner) =>
                {
                    if (ghostOwner.NetworkId != ownNetworkId)
                    {
                        return;
                    }

                    EntityManager.AddComponent<PlayerController>(player);
                    EntityManager.AddBuffer<PlayerInput>(player);
                    EntityManager.AddComponentObject(player, camera);

                    EntityManager.SetComponentData(
                        commandTargetEntity,
                        new CommandTargetComponent { targetEntity = player }
                    );
                })
                .Run();
        }

        protected override void OnUpdate()
        {
            if (_ownedPlayer == Entity.Null)
            {
                _ownedPlayer = GetSingleton<CommandTargetComponent>().targetEntity;

                if (_ownedPlayer == Entity.Null)
                {
                    SetupOwnedPlayer();
                    return;
                }
            }

            var inputs = EntityManager.GetBuffer<PlayerInput>(_ownedPlayer);
            var tick = _clientSimulationSystemGroup.ServerTick;
            inputs.GetDataAtTick(tick - 1, out var prevInput);
            inputs.AddCommandData(new PlayerInput
            {
                Tick = _clientSimulationSystemGroup.ServerTick,
                
                Translation = new float3(
                    Input.GetAxis("Horizontal"),
                    Input.GetAxis("Jump"),
                    Input.GetAxis("Vertical")
                ),

                Rotation = new float2(
                    (prevInput.Rotation.x + 3f * Input.GetAxis("Mouse X")) % 360f,
                    math.clamp(prevInput.Rotation.y + 3f * Input.GetAxis("Mouse Y"), 0f, 180f)
                ),

                firingInput = Input.GetButtonDown("Fire1"),
            });
        }
    }
}
