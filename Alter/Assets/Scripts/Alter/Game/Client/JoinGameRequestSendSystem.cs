using Alter.Characters;
using Unity.Entities;
using Unity.NetCode;

namespace Alter.Game.Client
{
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public partial class JoinGameRequestSendSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            RequireSingletonForUpdate<PlayerSpawner>();
            RequireForUpdate(GetEntityQuery(
                ComponentType.ReadOnly<NetworkIdComponent>(),
                ComponentType.Exclude<NetworkStreamInGame>()
            ));
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            Entities
                .WithAll<NetworkIdComponent>()
                .WithNone<NetworkStreamInGame>()
                .ForEach((Entity connection) =>
                {
                    ecb.AddComponent<NetworkStreamInGame>(connection);

                    var request = ecb.CreateEntity();
                    ecb.AddComponent<JoinGameRequest>(request);
                    ecb.AddComponent(request, new SendRpcCommandRequestComponent
                    {
                        TargetConnection = connection,
                    });
                })
                .Run();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
