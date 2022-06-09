using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alter.Terrain
{
    [BurstCompile]
    public partial struct TerrainChunkMeshGenerationJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter EntityCommandWriter;
        public Mesh.MeshDataArray MeshDataArray;
        public NativeArray<VertexAttributeDescriptor> VertexLayout;

        // ReSharper disable once UnusedMember.Global
        public void Execute(
            Entity entity,
            [EntityInQueryIndex] int entityIndex,
            ref DynamicBuffer<TerrainBlock> blocks
        )
        {
            EntityCommandWriter.RemoveComponent<TerrainChunkToGenerateMeshTag>(entityIndex, entity);

            using var vertices = new NativeList<BlockVertex>(Allocator.Temp);
            using var colliderVertices = new NativeList<float3>(Allocator.Temp);
            using var triangles = new NativeList<ushort>(Allocator.Temp);
            using var colliderTriangles = new NativeList<int3>(Allocator.Temp);

            // TODO: `MeshCollider.Create` copies the vertex and triangle `NativeArray`s it
            // takes[1], but we make a second set of lists just for creating the collider, as
            // `MeshData` methods and `MeshCollider.Create` require different, incompatible types of
            // `NativeArray`s. Maybe we should make our own `MeshCollider` creation function to
            // avoid this unnecessary memory copying.
            //
            // [1]: See
            // Library/PackageCache/com.unity.physics@0.50.0-preview.43/Unity.Physics/Collision/Colliders/Physics_MeshCollider.cs
            // from line 33.

            for (var x = 0; x < TerrainChunk.Size.x; x++)
            {
                for (var z = 0; z < TerrainChunk.Size.z; z++)
                {
                    for (var y = 0; y < TerrainChunk.Size.y; y++)
                    {
                        var blockType = blocks.BlockTypeAt(x, y, z);

                        if (blockType is TerrainBlockType.None)
                        {
                            continue;
                        }

                        var blockPos = new float3(x, y, z);
                        var prevNumFaces = (ushort)(vertices.Length / 4);
                        ushort numFaces = 0;

                        if (y == 0 || blocks.HaveNoneAt(x, y - 1, z))
                        {
                            numFaces++;
                            VertexUtility.AddBlockFace(vertices, colliderVertices, blockPos, BlockFace.Bottom);
                        }

                        if (y == TerrainChunk.Size.y - 1 || blocks.HaveNoneAt(x, y + 1, z))
                        {
                            numFaces++;
                            VertexUtility.AddBlockFace(vertices, colliderVertices, blockPos, BlockFace.Top);
                        }

                        if (z == 0 || blocks.HaveNoneAt(x, y, z - 1))
                        {
                            numFaces++;
                            VertexUtility.AddBlockFace(vertices, colliderVertices, blockPos, BlockFace.South);
                        }

                        if (z == TerrainChunk.Size.z - 1 || blocks.HaveNoneAt(x, y, z + 1))
                        {
                            numFaces++;
                            VertexUtility.AddBlockFace(vertices, colliderVertices, blockPos, BlockFace.North);
                        }

                        if (x == 0 || blocks.HaveNoneAt(x - 1, y, z))
                        {
                            numFaces++;
                            VertexUtility.AddBlockFace(vertices, colliderVertices, blockPos, BlockFace.West);
                        }

                        if (x == TerrainChunk.Size.x - 1 || blocks.HaveNoneAt(x + 1, y, z))
                        {
                            numFaces++;
                            VertexUtility.AddBlockFace(vertices, colliderVertices, blockPos, BlockFace.East);
                        }

                        for (ushort i = 0; i < numFaces; i++)
                        {
                            triangles.Add((ushort)(4 * prevNumFaces + 4 * i));
                            triangles.Add((ushort)(4 * prevNumFaces + 4 * i + 1));
                            triangles.Add((ushort)(4 * prevNumFaces + 4 * i + 2));
                            triangles.Add((ushort)(4 * prevNumFaces + 4 * i));
                            triangles.Add((ushort)(4 * prevNumFaces + 4 * i + 2));
                            triangles.Add((ushort)(4 * prevNumFaces + 4 * i + 3));
                            colliderTriangles.Add(new int3(
                                4 * prevNumFaces + 4 * i,
                                4 * prevNumFaces + 4 * i + 1,
                                4 * prevNumFaces + 4 * i + 2
                            ));
                            colliderTriangles.Add(new int3(
                                4 * prevNumFaces + 4 * i,
                                4 * prevNumFaces + 4 * i + 2,
                                4 * prevNumFaces + 4 * i + 3
                            ));
                        }
                    }
                }
            }

            var meshData = MeshDataArray[entityIndex];

            meshData.SetVertexBufferParams(vertices.Length, VertexLayout);
            vertices.AsArray().CopyTo(meshData.GetVertexData<BlockVertex>());

            meshData.SetIndexBufferParams(triangles.Length, IndexFormat.UInt16);
            triangles.AsArray().CopyTo(meshData.GetIndexData<ushort>());

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));

            EntityCommandWriter.SetComponent(entityIndex, entity, new PhysicsCollider
            {
                Value = Unity.Physics.MeshCollider.Create(
                    colliderVertices,
                    colliderTriangles,
                    new CollisionFilter
                    {
                        BelongsTo = 1 << 0,
                        CollidesWith = ~0u - (1 << 0),
                    }
                ),
            });
        }
    }

    [UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
    public partial class TerrainChunkMeshGenerationSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        private EntityQuery _chunksToGenerateMeshQuery;

        private NativeArray<VertexAttributeDescriptor> _vertexLayout;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            _chunksToGenerateMeshQuery = GetEntityQuery(
                ComponentType.ReadWrite<TerrainBlock>(),
                ComponentType.ReadOnly<TerrainChunkToGenerateMeshTag>()
            );

            _vertexLayout = new NativeArray<VertexAttributeDescriptor>(
                BlockVertex.Attributes,
                Allocator.Persistent
            );
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

            var numChunksToGenerateMesh = _chunksToGenerateMeshQuery.CalculateEntityCount();
            var meshDataArray = Mesh.AllocateWritableMeshData(numChunksToGenerateMesh);

            new TerrainChunkMeshGenerationJob
            {
                EntityCommandWriter = ecb,
                MeshDataArray = meshDataArray,
                VertexLayout = _vertexLayout,
            }.ScheduleParallel(_chunksToGenerateMeshQuery);

            var meshes = new Mesh[numChunksToGenerateMesh];

            // This relies on the assumption that the entities queried are the same as for
            // _chunksToGenerateMeshQuery.
            Entities
                .WithoutBurst()
                .WithAll<TerrainBlock, TerrainChunkToGenerateMeshTag>()
                .ForEach((int entityInQueryIndex, in RenderMesh renderMesh) =>
                {
                    meshes[entityInQueryIndex] = renderMesh.mesh;
                })
                .Run();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, meshes);

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }

        protected override void OnDestroy()
        {
            _vertexLayout.Dispose();
        }
    }
}
