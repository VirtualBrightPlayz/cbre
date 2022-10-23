using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace CBRE.Graphics {
    public class Texture2D : GraphicsResource {
        internal Texture _texture;
        public string Name { get => _texture.Name; set => _texture.Name = value; }
        public int Width => (int)_texture.Width;
        public int Height => (int)_texture.Height;

        public Texture2D(GraphicsDevice gd, int w, int h) : this((uint)w, (uint)h) {
        }

        public Texture2D(int w, int h) : this((uint)w, (uint)h) {
        }

        public Texture2D(uint w, uint h) {
            TextureDescription texDesc = TextureDescription.Texture2D(w, h, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled | TextureUsage.Staging);
            _texture = GlobalGraphics.GraphicsDevice.ResourceFactory.CreateTexture(texDesc);
        }

        public override void Dispose() {
            _texture?.Dispose();
        }

        public void SetData(byte[] data) {
            /*Rgba32[] pixels = new Rgba32[_texture.Width * _texture.Height * 4];
            for (int i = 0; i < data.Length - 4; i+=4) {
                pixels[i + 0] = new Rgba32(data[i + 0] / 255f, data[i + 1] / 255f, data[i + 2] / 255f, data[i + 3] / 255f);
            }
            using Image img = Image.LoadPixelData(pixels, (int)_texture.Width, (int)_texture.Height);*/
            GlobalGraphics.GraphicsDevice.UpdateTexture(_texture, data, 0, 0, 0, _texture.Width, _texture.Height, 1, 0, 0);
        }

        public void SaveAsPng(Stream stream, int w, int h) {
            SaveAsPng(stream);
        }

        public void SaveAsPng(Stream stream) {
            GlobalGraphics.SaveAsPng(_texture, stream, _texture.Width, _texture.Height);
        }
    }
}