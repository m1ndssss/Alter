using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Alter.Terrain
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Alter/Terrain/Terrain")]
    public class TerrainAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        private static readonly int3 Size = new(128, 60, 128);

        public UnityEngine.Material material;

        public void Convert(
            Entity entity,
            EntityManager dstManager,
            GameObjectConversionSystem conversionSystem
        )
        {
            for (var x = -Size.x / 2; x < Size.x / 2; x += TerrainChunk.Size.x)
            {
                for (var z = -Size.z / 2; z < Size.z / 2; z += TerrainChunk.Size.z)
                {
                    for (var y = 0; y < Size.y; y += TerrainChunk.Size.y)
                    {
                        var chunkEntity = conversionSystem.CreateAdditionalEntity(gameObject);
                        dstManager.SetName(chunkEntity, $"TerrainChunk({x}, {y}, {z})");

                        dstManager.AddComponents(chunkEntity, new ComponentTypes(
                            typeof(LocalToWorld),
                            typeof(TerrainBlock),
                            typeof(TerrainChunkToGenerateTag)
                        ));
                        dstManager.AddComponentData(chunkEntity, new Translation
                        {
                            Value = new float3(x, y, z),
                        });
                        dstManager.AddSharedComponentData(chunkEntity, new PhysicsWorldIndex());
                        dstManager.AddComponentData(chunkEntity, new PhysicsCollider
                        {
                            Value = BlobAssetReference<Unity.Physics.Collider>.Null,
                        });

                        var renderMeshDesc = new RenderMeshDescription(new Mesh(), material);
                        RenderMeshUtility.AddComponents(chunkEntity, dstManager, renderMeshDesc);

                        var chunkHalfSize = TerrainChunk.Size / 2;
                        dstManager.SetComponentData(chunkEntity, new RenderBounds
                        {
                            Value = new AABB
                            {
                                Center = chunkHalfSize,
                                Extents = chunkHalfSize,
                            },
                        });
                    }
                }
            }
        }
    }
}
