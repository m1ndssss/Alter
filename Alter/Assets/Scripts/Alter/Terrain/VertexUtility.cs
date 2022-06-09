using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Alter.Terrain
{
    public struct Byte4
    {
        // ReSharper disable NotAccessedField.Local
#pragma warning disable CS0414
        private byte _x;
        private byte _y;
        private byte _z;
        private byte _w;
#pragma warning restore CS0414
        // ReSharper restore NotAccessedField.Local

        public Byte4(byte x, byte y, byte z)
        {
            _x = x;
            _y = y;
            _z = z;
            _w = 128;
        }
    }

    public struct BlockVertex
    {
        public static readonly VertexAttributeDescriptor[] Attributes = {
            new(VertexAttribute.Position),
            // We only need 3-dimensional normals, but attribute data size must be a multiple of 4.
            new(VertexAttribute.Normal, VertexAttributeFormat.SNorm8, 4),
        };

        // ReSharper disable NotAccessedField.Local
        private readonly float3 _position;
        private Byte4 _normal;
        // ReSharper restore NotAccessedField.Local

        public BlockVertex(float3 position, Byte4 normal)
        {
            _position = position;
            _normal = normal;
        }
    }

    public enum BlockFace
    {
        Bottom,
        Top,
        South,
        North,
        West,
        East,
    }

    public static class VertexUtility
    {
        private static readonly float3[][] FaceVertexPositions =
        {
            // Too bad C# seemingly doesn't support `{[index] = value}` syntax like C does!
            // BlockFace.Bottom
            new[]
            {
                new float3(1f, 0f, 0f),
                new float3(1f, 0f, 1f),
                new float3(0f, 0f, 1f),
                new float3(0f, 0f, 0f),
            },
            // BlockFace.Top
            new[]
            {
                new float3(0f, 1f, 0f),
                new float3(0f, 1f, 1f),
                new float3(1f, 1f, 1f),
                new float3(1f, 1f, 0f),
            },
            // BlockFace.South
            new[]
            {
                new float3(0f, 0f, 0f),
                new float3(0f, 1f, 0f),
                new float3(1f, 1f, 0f),
                new float3(1f, 0f, 0f),
            },
            // BlockFace.North
            new[]
            {
                new float3(1f, 0f, 1f),
                new float3(1f, 1f, 1f),
                new float3(0f, 1f, 1f),
                new float3(0f, 0f, 1f),
            },
            // BlockFace.West
            new[]
            {
                new float3(0f, 0f, 1f),
                new float3(0f, 1f, 1f),
                new float3(0f, 1f, 0f),
                new float3(0f, 0f, 0f),
            },
            // BlockFace.East
            new[]
            {
                new float3(1f, 0f, 0f),
                new float3(1f, 1f, 0f),
                new float3(1f, 1f, 1f),
                new float3(1f, 0f, 1f),
            },
        };

        private static readonly Byte4[] FaceNormals =
        {
            // BlockFace.Bottom
            new(128, 0, 128),
            // BlockFace.Top
            new(128, 255, 128),
            // BlockFace.South
            new(128, 128, 0),
            // BlockFace.North
            new(128, 128, 255),
            // BlockFace.West
            new(0, 128, 128),
            // BlockFace.East
            new(255, 128, 128),
        };

        public static void AddBlockFace(
            NativeList<BlockVertex> renderVertices,
            NativeList<float3> colliderVertices,
            float3 position,
            BlockFace face
        )
        {
            var vertexPositions = FaceVertexPositions[(int)face];
            var normal = FaceNormals[(int)face];

            foreach (var vertexPosition in vertexPositions)
            {
                renderVertices.Add(new BlockVertex(position + vertexPosition, normal));
                colliderVertices.Add(new float3(position + vertexPosition));
            }
        }
    }
}
