using System;

namespace CBRE.Common {
    public interface ITexture : IDisposable {
        TextureFlags Flags { get; }
        string Name { get; }
        int Width { get; }
        int Height { get; }
    }

    public static class TextureExtensions {
        public static bool HasTransparency(this ITexture texture) {
            return texture.Flags.HasFlag(TextureFlags.Transparent);
        }
    }
}
