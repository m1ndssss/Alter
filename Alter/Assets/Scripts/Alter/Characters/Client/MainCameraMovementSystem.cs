using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Alter.Characters.Client
{
    [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
    public partial class MainCameraMovementSystem : SystemBase
    {
        private static readonly float3 CameraHeight = new float3(0f, 1.7f / 2f, 0f);

        private ClientSimulationSystemGroup _clientSimulationSystemGroup;

        protected override void OnCreate()
        {
            _clientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
        }

        protected override void OnUpdate()
        {
            var tick = _clientSimulationSystemGroup.ServerTick;

            Entities
                .WithoutBurst()
                .ForEach((
                    Camera camera,
                    in LocalToWorld localToWorld,
                    in DynamicBuffer<PlayerInput> inputs
                ) =>
                {
                    inputs.GetDataAtTick(tick, out var input);
                    var transform = camera.transform;
                    transform.position = localToWorld.Position + CameraHeight;
                    transform.rotation = quaternion.Euler(
                        math.radians(90f - input.Rotation.y),
                        math.radians(input.Rotation.x),
                        0f
                    );

                    if (Input.GetMouseButton(0))
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                    else if (Input.GetKey(KeyCode.Escape))
                    {
                        Cursor.lockState = CursorLockMode.None;
                    }
                })
                .Run();
        }
    }
}
