using System;
using System.Collections.Generic;
using System.Text;
using CBRE.Common;
using Microsoft.Xna.Framework.Graphics;

namespace CBRE.Graphics {
    public class RenderTargetTexture : ITexture {
        public RenderTarget2D MonoGameTexture { get; private set; }

        public RenderTargetTexture(int width, int height) {
            MonoGameTexture = new RenderTarget2D(GlobalGraphics.GraphicsDevice, width, height);
        }

        public TextureFlags Flags => TextureFlags.None;

        public string Name => "RENDERTARGET";

        public int Width => MonoGameTexture?.Width ?? 0;

        public int Height => MonoGameTexture?.Height ?? 0;

        public void Dispose() {
            MonoGameTexture?.Dispose(); MonoGameTexture = null;
        }
    }
}
