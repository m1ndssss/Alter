using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Alter.Characters
{
    public class PlayerShooting : MonoBehaviour {}

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class PlayerShootingSystem : SystemBase
    {
        private EndFixedStepSimulationEntityCommandBufferSystem _ecbSystem;

        public static Entity GetRaycast(float3 rayFrom, float3 rayTo, World world)
        {
            var physicsWorldSystem = world.GetExistingSystem<BuildPhysicsWorld>();
            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

            RaycastInput input = new RaycastInput()
            {
                Start = new float3(rayFrom.x, rayFrom.y, 0f),
                End = new float3(rayTo.x, rayTo.y, 0f),

                Filter = new CollisionFilter()
                {
                    BelongsTo = ~0u,
                    CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                    GroupIndex = 0
                }
            };

            if (collisionWorld.CastRay(input, out var hit))
            {
                return physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
            }
            return Entity.Null;
        }

        private GhostPredictionSystemGroup _ghostPredictionSystemGroup;

        protected override void OnCreate()
        {
            _ghostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
            _ecbSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var tick = _ghostPredictionSystemGroup.PredictingTick;
            var world = World;
            var hittedEntity = Entity.Null;
            var ecb = _ecbSystem.CreateCommandBuffer();

            Entities
                .ForEach(
                    (
                        in DynamicBuffer<PlayerInput> inputs,
                        in Translation translation,
                        in LocalToWorld ltw,
                        in PredictedGhostComponent predicted
                    ) =>
                    {
                        if (!GhostPredictionSystemGroup.ShouldPredict(tick, predicted))
                        {
                            return;
                        }

                        inputs.GetDataAtTick(tick, out var input);

                        if (input.firingInput)
                        {
                            //TODO: Needs to make the Translation a float3 variable to be able to use Raycasts.

                            var physicsWorldSystem = world.GetExistingSystem<BuildPhysicsWorld>();
                            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

                            RaycastInput raycastInput = new RaycastInput()
                            {
                                Start = translation.Value * ltw.Forward,
                                End = translation.Value * ltw.Forward * 100,

                                Filter = new CollisionFilter()
                                {
                                    BelongsTo = ~0u,
                                    CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                                    GroupIndex = 0
                                }
                            };

                            if (collisionWorld.CastRay(raycastInput, out var hit))
                            {
                                hittedEntity = physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                            }
                            else
                            {
                                hittedEntity = Entity.Null;
                            }

                            var bulletHoleEntity = ecb.Instantiate(BulletHolePrefab.prefabEntity);

                            ecb.AddComponent(bulletHoleEntity, typeof(BulletHoleSpawner));
                            ecb.SetComponent(bulletHoleEntity, new Translation {
                                Value = translation.Value * ltw.Forward * 100,
                            });

                            Debug.DrawRay(translation.Value, ltw.Forward * 100, Color.red, 50f);
                        }

                        if (hittedEntity != Entity.Null)
                        {
                            var playerData = GetComponent<PlayerData>(hittedEntity);

                            playerData.Health -= 30;
                        }


                    })
                .WithoutBurst().Run();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}




