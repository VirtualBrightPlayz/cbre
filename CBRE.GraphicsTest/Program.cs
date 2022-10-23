using System;
using System.Drawing;
using CBRE.DataStructures.Geometric;
using CBRE.Graphics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

public class Program {
    public static void Main(string[] args) {
        WindowCreateInfo windowCI = new WindowCreateInfo() {
            X = 100,
            Y = 100,
            WindowWidth = 960,
            WindowHeight = 540,
            WindowTitle = "CBRE Graphics Test",
        };
        GraphicsDeviceOptions options = new GraphicsDeviceOptions {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
        };
        Console.WriteLine("h");
        if (!RenderDoc.Load(out RenderDoc doc)) {
            Console.WriteLine("Failed to load RenderDoc");
            return;
        }
        VeldridStartup.CreateWindowAndGraphicsDevice(windowCI, options, GraphicsBackend.Vulkan, out Sdl2Window window, out GraphicsDevice gd);
        window.Resizable = false;
        ImGuiRenderer renderer = new ImGuiRenderer(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);
        GlobalGraphics.Set(gd, window, renderer);
        while (window.Exists) {
            window.PumpEvents();
            if (doc.IsFrameCapturing()) {
                doc.LaunchReplayUI();
            }
            GlobalGraphics.BeginMainDraw();
            PrimitiveDrawing.Begin(PrimitiveType.LineLoop);
            PrimitiveDrawing.SetColor(Color.Red);
            PrimitiveDrawing.Circle(new Vector3(0, 0, 0), 1f);
            PrimitiveDrawing.End();
            PrimitiveDrawing.Begin(PrimitiveType.LineLoop);
            PrimitiveDrawing.SetColor(Color.Blue);
            PrimitiveDrawing.Square(new Vector3(0, 0, 0), 1f);
            PrimitiveDrawing.End();
            GlobalGraphics.EndMainDraw();
        }
    }
}