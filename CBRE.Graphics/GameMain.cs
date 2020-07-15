using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Num = System.Numerics;

namespace CBRE.Graphics {
    public class GameMain : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;

        private Texture2D _xnaTexture;
        private IntPtr _imGuiTexture;

        public GameMain()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.PreferMultiSampling = true;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.ApplyChanges();
            Window.AllowUserResizing = true;

            MenuBarItems.Add(new MenuBarItem { Title = "File" });
            MenuBarItems.Add(new MenuBarItem { Title = "Edit" });
            MenuBarItems.Add(new MenuBarItem { Title = "Map" });
            MenuBarItems.Add(new MenuBarItem { Title = "View" });
            MenuBarItems.Add(new MenuBarItem { Title = "Tools" });
            MenuBarItems.Add(new MenuBarItem { Title = "Layout" });
            MenuBarItems.Add(new MenuBarItem { Title = "Help" });

            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();

            var style = ImGui.GetStyle();
            style.ChildRounding = 0;
            style.FrameRounding = 0;
            style.GrabRounding = 0;
            style.PopupRounding = 0;
            style.ScrollbarRounding = 0;
            style.TabRounding = 0;
            style.WindowRounding = 0;

            base.Initialize();
        }

        protected override void LoadContent()
        {
			_imGuiTexture = _imGuiRenderer.BindTexture(_xnaTexture);

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

        public class MenuBarItem {
            public string Title;
        }
        public List<MenuBarItem> MenuBarItems = new List<MenuBarItem>();

        protected virtual void ImGuiLayout()
        {
            if (ImGui.Begin("main", ImGuiWindowFlags.NoBackground |
                                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                                    ImGuiWindowFlags.NoMove |
                                    ImGuiWindowFlags.NoDecoration |
                                    ImGuiWindowFlags.MenuBar)) {
                ImGui.SetWindowPos(Num.Vector2.Zero);
                ImGui.SetWindowSize(new Num.Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height));
                if (ImGui.BeginMenuBar()) {
                    for (int i = 0; i < MenuBarItems.Count; i++) {
                        if (ImGui.BeginMenu(MenuBarItems[i].Title)) {
                            if (ImGui.MenuItem("test")) { }
                            ImGui.EndMenu();
                        }
                    }

                    ImGui.EndMenuBar();
                }
                ImGui.End();
            }

        }
	}
}
