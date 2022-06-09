using Unity.Entities;
using Unity.NetCode;

namespace Alter.Game.Server
{
    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public partial class PlayerDisconnectionSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            Entities
                .WithAll<NetworkStreamDisconnected>()
                .ForEach((in CommandTargetComponent commandTarget) =>
                {
                    ecb.DestroyEntity(commandTarget.targetEntity);
                })
                .Run();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
