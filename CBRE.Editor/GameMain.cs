using CBRE.Common;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using CBRE.Providers.Map;
using CBRE.Providers.Texture;
using CBRE.Settings;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;

        public GameMain()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.PreferMultiSampling = true;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.ApplyChanges();
            Window.AllowUserResizing = true;

            IsMouseVisible = true;
        }

        public static Dictionary<string, AsyncTexture> MenuTextures;

        ImGuiStylePtr ImGuiStyle;

        protected override void Initialize()
        {
            SettingsManager.Read();

            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();

            GlobalGraphics.Set(GraphicsDevice, _imGuiRenderer);

            ImGuiStyle = ImGui.GetStyle();
            ImGuiStyle.ChildRounding = 0;
            ImGuiStyle.FrameRounding = 0;
            ImGuiStyle.GrabRounding = 0;
            ImGuiStyle.PopupRounding = 0;
            ImGuiStyle.ScrollbarRounding = 0;
            ImGuiStyle.TabRounding = 0;
            ImGuiStyle.WindowRounding = 0;
            ImGuiStyle.FrameBorderSize = 0;
            ImGuiStyle.DisplayWindowPadding = Num.Vector2.Zero;
            ImGuiStyle.WindowPadding = Num.Vector2.Zero;
            ImGuiStyle.IndentSpacing = 0;
            var colors = ImGuiStyle.Colors;
            colors[(int)ImGuiCol.FrameBg] = new Num.Vector4(0.05f, 0.05f, 0.07f, 1.0f);

            MenuTextures = new Dictionary<string, AsyncTexture>();
            string[] files = Directory.GetFiles("Resources");
            foreach (string file in files) {
                if (!System.IO.Path.GetExtension(file).Equals(".png", StringComparison.OrdinalIgnoreCase)) { continue; }
                MenuTextures.Add(System.IO.Path.GetFileNameWithoutExtension(file),
                    LoadTexture(file));
            }

            InitMenus();
            InitTopBar();
            InitToolBar();

            TextureProvider.CreatePackages(Directories.GetTextureCategories());

            MapProvider.Register(new VmfProvider());
            MapProvider.Register(new RmfProvider());
            MapProvider.Register(new MapFormatProvider());
            MapProvider.Register(new L3DWProvider());

            Map map = MapProvider.GetMapFromFile("D:/Admin/Downloads/room2_2.3dw");
            DocumentManager.AddAndSwitch(new Document("room2_2.3dw", map));

            base.Initialize();
        }

        private AsyncTexture LoadTexture(string filename) {
            return new AsyncTexture(filename);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
            TaskPool.Update();

            GraphicsDevice.Clear(new Color(50, 50, 60));

            // Call BeforeLayout first to set things up
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
            _imGuiRenderer.AfterLayout();

            if (DocumentManager.CurrentDocument != null) {
                var prevViewport = GlobalGraphics.GraphicsDevice.Viewport;
                GlobalGraphics.GraphicsDevice.Viewport = new Viewport(46, 47, (Window.ClientBounds.Width - 46) / 2, (Window.ClientBounds.Height - 47) / 2);
                var brushRenderer = DocumentManager.CurrentDocument.BrushRenderer;
                brushRenderer.Projection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI * 0.5f, GlobalGraphics.GraphicsDevice.Viewport.AspectRatio, 1.0f, 10000.0f);
                brushRenderer.View = Matrix.CreateLookAt(new Vector3(0, 0, 100), new Vector3(0, 1, 100), new Vector3(0, 0, 1));
                brushRenderer.World = Matrix.Identity;

                GraphicsDevice.BlendFactor = Color.White;
                GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;

                brushRenderer.RenderSolidUntextured();
                GlobalGraphics.GraphicsDevice.Viewport = prevViewport;

                GraphicsDevice.DepthStencilState = DepthStencilState.None;
            }

            base.Draw(gameTime);
        }

        protected virtual void ImGuiLayout()
        {
            if (ImGui.Begin("main", ImGuiWindowFlags.NoBackground |
                                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                                    ImGuiWindowFlags.NoMove |
                                    ImGuiWindowFlags.NoDecoration |
                                    ImGuiWindowFlags.MenuBar |
                                    ImGuiWindowFlags.NoScrollbar |
                                    ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.SetWindowPos(new Num.Vector2(-1, 0));
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width + 2, Window.ClientBounds.Height));

                UpdateMenus();
                UpdateTopBar();
                UpdateToolBar();

                ImGui.End();
            }
        }
	}
}
