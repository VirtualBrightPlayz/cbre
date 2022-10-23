using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.Sdl2;

namespace CBRE.Graphics {
    public static partial class GlobalGraphics {
        public static GraphicsDevice GraphicsDevice { get; private set; }
        public static CommandList CommandList { get; private set; }
        public static Sdl2Window Window { get; private set; }
        public static ImGuiRenderer ImGuiRenderer { get; private set; }
        public static Texture BlankWhiteTexture { get; private set; }
        public static Sampler PointSampler { get; private set; }

        public static void Set(GraphicsDevice gfxDev, Sdl2Window window, ImGuiRenderer imGuiRenderer) {
            GraphicsDevice = gfxDev;
            CommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
            Window = window;
            ImGuiRenderer = imGuiRenderer;
            // Image<Rgba32> img = Image.LoadPixelData<Rgba32>(Enumerable.Repeat(new Rgba32(119, 119, 119, 255), 64 * 64).ToArray(), 64, 64);
            TextureDescription desc = TextureDescription.Texture2D(64, 64, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled);
            BlankWhiteTexture = GraphicsDevice.ResourceFactory.CreateTexture(desc);
            BlankWhiteTexture.Name = nameof(BlankWhiteTexture);
            GraphicsDevice.UpdateTexture(BlankWhiteTexture, Enumerable.Repeat(new Rgba32(119, 119, 119, 255), 64 * 64).ToArray(), 0, 0, 0, 64, 64, 1, 0, 0);
            SamplerDescription desc2 = SamplerDescription.Point;
            PointSampler = GraphicsDevice.ResourceFactory.CreateSampler(desc2);
            PointSampler.Name = nameof(PointSampler);
            ImGuiRenderer.GetOrCreateImGuiBinding(GraphicsDevice.ResourceFactory, BlankWhiteTexture);
        }

        public static void BeginMainDraw() {
            CommandList.Begin();
            CommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
            CommandList.ClearColorTarget(0, RgbaFloat.Black);
        }

        public static void EndMainDraw() {
            CommandList.End();
            GraphicsDevice.SubmitCommands(CommandList);
            GraphicsDevice.SwapBuffers();
        }

        public static void BeginPass() {
            return;
            CommandList.Begin();
            CommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
        }

        public static void EndPass() {
            return;
            CommandList.End();
            GraphicsDevice.SubmitCommands(CommandList);
            GraphicsDevice.WaitForIdle();
        }
    }
}
