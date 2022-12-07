using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.Sdl2;

namespace CBRE.Graphics {
    public static partial class GlobalGraphics {
        internal static Framebuffer ActiveFramebuffer { get; set; }

        public static void SaveAsPng(Veldrid.Texture texture, Stream stream, uint width, uint height) {
            MappedResourceView<Rgba32> map = GraphicsDevice.Map<Rgba32>(texture, MapMode.Read);

            Rgba32[] pixels = new Rgba32[texture.Width * texture.Height];
            for (int y = 0; y < texture.Height; y++) {
                for (int x = 0; x < texture.Width; x++) {
                    int index = (int)(y * texture.Width + x);
                    pixels[index] = map[x, y];
                }
            }
            GraphicsDevice.Unmap(texture);
            using var img = Image.LoadPixelData(pixels, (int)texture.Width, (int)texture.Height);
            img.SaveAsPng(stream);
        }

        public static void SetVertexBuffer(VertexBuffer buffer) {
            CommandList.SetVertexBuffer(0, buffer._buffer);
        }

        public static void SetIndexBuffer(IndexBuffer buffer) {
            CommandList.SetIndexBuffer(buffer._buffer, buffer._indexFormat);
        }

        public static void DrawPrimitives(PrimitiveType type, int offset, int count) {
            CommandList.Draw((uint)count);
        }

        public static void DrawIndexedPrimitives(PrimitiveType type, int offset, int stride, int indices) {
            CommandList.DrawIndexed((uint)indices);
        }

        public static void Clear(ClearOptions options, System.Numerics.Vector4 color, float depth, byte stencil) {
            if (options.HasFlag(ClearOptions.Target))
                CommandList.ClearColorTarget(0, new RgbaFloat(color.X, color.Y, color.Z, color.W));
            if (options.HasFlag(ClearOptions.DepthBuffer | ClearOptions.Stencil))
                CommandList.ClearDepthStencil(depth, stencil);
            else if (options.HasFlag(ClearOptions.DepthBuffer))
                CommandList.ClearDepthStencil(depth);
        }

        public static void Clear(System.Drawing.Color color) {
            Clear(ClearOptions.Target, new System.Numerics.Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f), 0, 0);
        }

        public static void SetRenderTarget(RenderTarget2D target) {
            if (target == null) {
                CommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
                ActiveFramebuffer = GraphicsDevice.SwapchainFramebuffer;
                return;
            }
            CommandList.SetFramebuffer(target._framebuffer);
            ActiveFramebuffer = target._framebuffer;
        }

        public static Effect LoadEffect(string path) {
            Effect eff = new Effect(path);
            return eff;
        }

        public static Effect LoadBasicEffect() {
            Effect eff = new Effect("Shaders/basic.mgfx");
            return eff;
        }
    }

    public sealed class DepthStencilState {
        public static readonly DepthStencilState None = new DepthStencilState() {
            DepthBufferEnable = false,
            DepthBufferWriteEnable = false,
        };
        public static readonly DepthStencilState Default = new DepthStencilState() {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,
        };
        public bool DepthBufferEnable { get; set; }
        public bool DepthBufferWriteEnable { get; set; }
    }

    [Flags]
    public enum ClearOptions : byte {
        Target = 1,
        DepthBuffer = 2,
        Stencil = 4,
    }

    public sealed class RasterizerState {
        public static readonly RasterizerState CullCounterClockwise = new RasterizerState() {
            CullMode = FaceCullMode.Back,
            FillMode = PolygonFillMode.Solid,
            Front = FrontFace.CounterClockwise,
            DepthClipEnabled = true,
            ScissorTestEnabled = false,
        };
        public static readonly RasterizerState CullNone = new RasterizerState() {
            CullMode = FaceCullMode.None,
            FillMode = PolygonFillMode.Solid,
            Front = FrontFace.Clockwise,
            DepthClipEnabled = true,
            ScissorTestEnabled = false,
        };
        public FaceCullMode CullMode { get; set; }
        public PolygonFillMode FillMode { get; set; }
        public FrontFace Front { get; set; }
        public bool DepthClipEnabled { get; set; }
        public bool ScissorTestEnabled { get; set; }
    }
}
