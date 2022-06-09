using Unity.Entities;
using Unity.Mathematics;

namespace Alter.Terrain
{
    public static class TerrainChunk
    {
        private const int SizeX = 16;
        private const int SizeY = 60;
        private const int SizeZ = 16;
        public const int SizeXYZ = SizeX * SizeY * SizeZ;
        public static readonly int3 Size = new(SizeX, SizeY, SizeZ);
    }

    public struct TerrainChunkToGenerateTag : IComponentData
    {
    }

    public struct TerrainChunkToGenerateMeshTag : IComponentData
    {
    }

    public enum TerrainBlockType : byte
    {
        None,
        Stone,
        Dirt,
        Grass,
    }

    [InternalBufferCapacity(TerrainChunk.SizeXYZ)]
    public struct TerrainBlock : IBufferElementData
    {
        // ReSharper disable once NotAccessedField.Local
        public readonly TerrainBlockType Type;

        public TerrainBlock(TerrainBlockType type)
        {
            Type = type;
        }
    }
}
