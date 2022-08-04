using System;

namespace CBRE.Common {
    public interface ITexture : IDisposable {
        TextureFlags Flags { get; }
        string Name { get; }
        uint UWidth { get; }
        uint UHeight { get; }
    }

    public static class TextureExtensions {
        public static bool HasTransparency(this ITexture texture) {
            return texture.Flags.HasFlag(TextureFlags.Transparent);
        }
    }
}
