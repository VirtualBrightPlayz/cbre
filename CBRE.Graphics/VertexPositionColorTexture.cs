using System.Numerics;
using Veldrid;

namespace CBRE.Graphics
{
    public struct VertexPositionColorTexture
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public RgbaFloat Color;
        public const uint SizeInBytes = 36;

        public VertexPositionColorTexture(Vector3 pos, RgbaFloat rgba, Vector2 uv) {
            Position = pos;
            Color = rgba;
            TextureCoordinate = uv;
        }
    }
}