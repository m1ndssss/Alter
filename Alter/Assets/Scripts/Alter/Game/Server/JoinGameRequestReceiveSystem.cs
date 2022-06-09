using Alter.Characters;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Alter.Game.Server
{
    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public partial class JoinGameRequestReceiveSystem : SystemBase
    {
        private Entity _playerPrefab;

        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _playerPrefab = Entity.Null;

            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            RequireSingletonForUpdate<PlayerSpawner>();
            RequireForUpdate(GetEntityQuery(
                ComponentType.ReadOnly<JoinGameRequest>(),
                ComponentType.ReadOnly<ReceiveRpcCommandRequestComponent>()
            ));
        }

        protected override void OnUpdate()
        {
            if (_playerPrefab == Entity.Null)
            {
                _playerPrefab = GetSingleton<PlayerSpawner>().PlayerPrefab;
            }

            var ecb = _ecbSystem.CreateCommandBuffer();

            var networkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>(true);
            var playerPrefab = _playerPrefab;

            Entities
                .WithAll<JoinGameRequest>()
                .ForEach((Entity request, in ReceiveRpcCommandRequestComponent receive) =>
                {
                    var player = ecb.Instantiate(playerPrefab);
                    ecb.AddComponent<PlayerController>(player);
                    ecb.AddBuffer<PlayerInput>(player);
                    ecb.SetComponent(player, new Translation
                    {
                        Value = new float3(0f, 64f, 0f),
                    });
                    ecb.SetComponent(player, new GhostOwnerComponent
                    {
                        NetworkId = networkIdFromEntity[receive.SourceConnection].Value,
                    });

                    ecb.AddComponent<NetworkStreamInGame>(receive.SourceConnection);
                    ecb.SetComponent(receive.SourceConnection, new CommandTargetComponent
                    {
                        targetEntity = player,
                    });

                    ecb.DestroyEntity(request);
                })
                .Run();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
