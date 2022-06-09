using Unity.Entities;

namespace Alter.Terrain
{
    public static class TerrainBlockDynamicBufferExtensions
    {
        private static int IndexAt(int x, int y, int z)
        {
            return x * TerrainChunk.Size.z * TerrainChunk.Size.y + z * TerrainChunk.Size.y + y;
        }

        public static TerrainBlockType BlockTypeAt(
            this DynamicBuffer<TerrainBlock> blocks,
            int x,
            int y,
            int z
        )
        {
            return blocks[IndexAt(x, y, z)].Type;
        }

        public static bool HaveNoneAt(this DynamicBuffer<TerrainBlock> blocks, int x, int y, int z)
        {
            return blocks.BlockTypeAt(x, y, z) == TerrainBlockType.None;
        }
    }
}
