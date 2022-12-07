using System;
using System.IO;
using Veldrid;

namespace CBRE.Graphics {
    public class RenderTarget2D : GraphicsResource, ITextureResource {
        internal Texture _texture;
        internal Texture _depth;
        internal Framebuffer _framebuffer;
        public string Name { get => _texture.Name; set => _texture.Name = value; }
        public int Width => (int)_texture.Width;
        public int Height => (int)_texture.Height;

        public RenderTarget2D(GraphicsDevice gd, int w, int h, bool mipMap, SurfaceFormat preferredFormat = SurfaceFormat.Color, DepthFormat preferredDepthFormat = DepthFormat.None) : this((uint)w, (uint)h, preferredDepthFormat) {
        }

        public RenderTarget2D(GraphicsDevice gd, int w, int h) : this((uint)w, (uint)h, DepthFormat.None) {
        }

        public RenderTarget2D(uint w, uint h, DepthFormat preferredDepthFormat) {
            TextureDescription texDesc = TextureDescription.Texture2D(w, h, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled | TextureUsage.RenderTarget | TextureUsage.Staging);
            _texture = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateTexture(texDesc);
            if (preferredDepthFormat == DepthFormat.Depth24Stencil8) {
                TextureDescription depthDesc = TextureDescription.Texture2D(w, h, 1, 1, PixelFormat.D24_UNorm_S8_UInt, TextureUsage.Sampled | TextureUsage.RenderTarget | TextureUsage.Staging | TextureUsage.DepthStencil);
                _depth = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateTexture(depthDesc);
            }
            FramebufferDescription fbDesc = new FramebufferDescription(_depth, _texture);
            _framebuffer = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateFramebuffer(fbDesc);
        }

        public override void Dispose() {
            _texture?.Dispose();
            _depth?.Dispose();
            _framebuffer?.Dispose();
        }

        public void SaveAsPng(Stream stream) {
            GlobalGraphics.SaveAsPng(_texture, stream, _texture.Width, _texture.Height);
        }

        public Texture GetInternalTexture() {
            return _texture;
        }
    }

    public enum SurfaceFormat : byte {
        Single,
        Vector4,
        Color,
    }

    public enum DepthFormat : byte {
        None,
        Depth24Stencil8,
    }
}