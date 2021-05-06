using CBRE.Common;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools;
using CBRE.Graphics;
using CBRE.Providers.Map;
using CBRE.Providers.Texture;
using CBRE.Settings;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class PopupGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private string _title;

        public PopupGame(string title)
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
            _graphics.PreferMultiSampling = false;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.ApplyChanges();
            Window.AllowUserResizing = true;
            IsMouseVisible = true;
            _title = title;
        }

        protected override void Initialize() {
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();
            base.Initialize();
        }

        protected override void Draw(GameTime gameTime) {
            _imGuiRenderer.BeforeLayout(gameTime);
            if (ImGui.Begin(_title, ImGuiWindowFlags.NoCollapse)) {
                bool shouldBeOpen = ImGuiLayout();
                if (!shouldBeOpen) {

                }
            }
            ImGui.End();
            _imGuiRenderer.AfterLayout();
        }

        protected virtual bool ImGuiLayout() {
            if (ImGui.Button("OK")) {
                return false;
            }
            return true;
        }
	}
}
