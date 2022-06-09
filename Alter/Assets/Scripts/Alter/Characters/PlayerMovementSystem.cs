using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Alter.Characters
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ExportPhysicsWorld))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    public partial class PlayerMovementSystem : SystemBase
    {
        private const float Gravity = 2.25f * 9.81f;

        // X axis is sideways and backwards, Y axis is jump, and Z axis is forward.
        private static readonly float3 PlayerSpeed = new float3(4f, 8f, 7f);

        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;

        private BuildPhysicsWorld _buildPhysicsWorldSystem;

        protected override void OnCreate()
        {
            _ghostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();

            _buildPhysicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
        }

        protected override void OnStartRunning()
        {
            this.RegisterPhysicsRuntimeSystemReadWrite();
        }

        private static bool IsNegligibleVelocity(float3 velocity)
        {
            const float epsilon = 0.0001f;
            return math.abs(velocity.x) < epsilon && math.abs(velocity.y) < epsilon &&
                   math.abs(velocity.z) < epsilon;
        }

        protected override void OnUpdate()
        {
            var tick = _ghostPredictionSystemGroup.PredictingTick;
            var deltaTime = Time.DeltaTime;

            var collisionWorld = _buildPhysicsWorldSystem.PhysicsWorld.CollisionWorld;

            Entities
                .WithReadOnly(collisionWorld)
                .ForEach((
                    ref PlayerController controller,
                    ref Translation translation,
                    ref Rotation rotation,
                    in PredictedGhostComponent predicted,
                    in DynamicBuffer<PlayerInput> inputs,
                    in LocalToWorld localToWorld,
                    in PhysicsCollider collider
                ) =>
                {
                    if (!GhostPredictionSystemGroup.ShouldPredict(tick, predicted))
                    {
                        return;
                    }

                    inputs.GetDataAtTick(tick, out var input);

                    var walk = PlayerSpeed.x * input.Translation.x * localToWorld.Right;
                    walk += (input.Translation.z > 0f ? PlayerSpeed.z : PlayerSpeed.x) *
                        input.Translation.z * localToWorld.Forward;

                    controller.Velocity += walk;

                    if (controller.OnGround && input.Translation.y > 0f)
                    {
                        controller.Velocity.y += PlayerSpeed.y;
                    }

                    if (!controller.OnGround || !IsNegligibleVelocity(controller.Velocity))
                    {
                        controller.OnGround = false;
                        controller.Velocity.y -= Gravity * deltaTime;
                    }

                    if (IsNegligibleVelocity(controller.Velocity))
                    {
                        return;
                    }

                    var velocity = controller.Velocity * deltaTime;

                    for (var i = 0; i < 3; i++)
                    {
                        var castInput = new ColliderCastInput(
                            collider.Value,
                            translation.Value,
                            translation.Value + velocity
                        );

                        if (!collisionWorld.CastCollider(castInput, out var hit))
                        {
                            break;
                        }

                        for (var c = 0; c < 3; c++)
                        {
                            if (hit.SurfaceNormal[c] == 0f)
                            {
                                continue;
                            }

                            velocity[c] *= hit.Fraction;
                            controller.Velocity[c] = walk[c];
                        }

                        if (hit.SurfaceNormal.y > 0f)
                        {
                            controller.OnGround = true;
                        }
                    }

                    controller.Velocity -= walk;
                    translation.Value += velocity;
                    rotation.Value = quaternion.Euler(0f, math.radians(input.Rotation.x), 0f);
                })
                .ScheduleParallel();
        }
    }
}
