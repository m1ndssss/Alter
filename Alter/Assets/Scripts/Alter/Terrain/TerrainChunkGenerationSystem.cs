using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Alter.Terrain
{
    [UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
    public partial class TerrainChunkGenerationSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
                .WithAll<TerrainChunkToGenerateTag>()
                .ForEach((
                    Entity entity,
                    int entityInQueryIndex,
                    ref DynamicBuffer<TerrainBlock> blocks,
                    in Translation translation
                ) =>
                {
                    ecb.RemoveComponent<TerrainChunkToGenerateTag>(entityInQueryIndex, entity);
                    ecb.AddComponent<TerrainChunkToGenerateMeshTag>(entityInQueryIndex, entity);
                    var origin = new int3(translation.Value);

                    for (var x = origin.x; x < origin.x + TerrainChunk.Size.x; x++)
                    {
                        for (var z = origin.z; z < origin.z + TerrainChunk.Size.z; z++)
                        {
                            var h = 0.5f * noise.snoise(0.01f * new float2(x, z));
                            h = math.remap(-1f, 1f, 0f, 1f, h);
                            h = math.pow(h, 2f);
                            var height = (int)math.remap(0f, 1f, 0f, TerrainChunk.Size.y, h);
                            var heightInChunk = math.clamp(
                                height,
                                origin.y,
                                origin.y + TerrainChunk.Size.y
                            ) - origin.y;
                            var height1InChunk = math.clamp(
                                height - 1,
                                origin.y,
                                origin.y + TerrainChunk.Size.y
                            ) - origin.y;
                            var height2InChunk = math.clamp(
                                height - 2,
                                origin.y,
                                origin.y + TerrainChunk.Size.y
                            ) - origin.y;

                            for (var y = 0; y < height2InChunk; y++)
                            {
                                blocks.Add(new TerrainBlock(TerrainBlockType.Stone));
                            }

                            if (height2InChunk < height1InChunk)
                            {
                                blocks.Add(new TerrainBlock(TerrainBlockType.Dirt));
                            }

                            if (height1InChunk < heightInChunk)
                            {
                                blocks.Add(new TerrainBlock(TerrainBlockType.Grass));
                            }

                            for (var y = heightInChunk; y < TerrainChunk.Size.y; y++)
                            {
                                blocks.Add(new TerrainBlock(TerrainBlockType.None));
                            }
                        }
                    }
                })
                .ScheduleParallel();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
