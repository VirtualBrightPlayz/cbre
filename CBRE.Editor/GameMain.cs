using CBRE.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
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

        Dictionary<string, AsyncTexture> menuTextures;

        ImGuiStylePtr ImGuiStyle;

        protected override void Initialize()
        {
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

            menuTextures = new Dictionary<string, AsyncTexture>();
            string[] files = Directory.GetFiles("Resources");
            foreach (string file in files) {
                if (!Path.GetExtension(file).Equals(".png", StringComparison.OrdinalIgnoreCase)) { continue; }
                menuTextures.Add(Path.GetFileNameWithoutExtension(file),
                    LoadTexture(file));
            }

            InitMenus();
            InitTopBar();
            InitToolBar();

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
            GraphicsDevice.Clear(Color.DarkSlateGray);

            // Call BeforeLayout first to set things up
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
            _imGuiRenderer.AfterLayout();

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
