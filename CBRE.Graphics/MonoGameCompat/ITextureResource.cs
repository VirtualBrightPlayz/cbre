using System;
using System.IO;

namespace CBRE.Graphics {
    public interface ITextureResource : IDisposable {
        int Width { get; }
        int Height { get; }
        void SaveAsPng(Stream stream);
        Veldrid.Texture GetInternalTexture();
    }
}