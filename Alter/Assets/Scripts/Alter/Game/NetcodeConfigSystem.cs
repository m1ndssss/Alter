using Unity.Entities;
using Unity.NetCode;

namespace Alter.Game
{
    [UpdateInGroup(typeof(ClientAndServerInitializationSystemGroup))]
    public partial class NetcodeConfigSystem : SystemBase
    {
        protected override void OnStartRunning()
        {
            var netcodeConfig = EntityManager.CreateEntity();
            EntityManager.AddComponentData(netcodeConfig, new PredictedPhysicsConfig
            {
                PhysicsTicksPerSimTick = 1,
                DisableWhenNoConnections = true,
            });
        }

        protected override void OnUpdate()
        {
        }
    }
}
