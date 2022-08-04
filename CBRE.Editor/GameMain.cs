using CBRE.Common;
using CBRE.Common.Mediator;
using CBRE.Editor.Documents;
using CBRE.Editor.Popup;
using CBRE.Editor.Problems;
using CBRE.Editor.Problems.RMesh;
using CBRE.Editor.Rendering;
using CBRE.Editor.Tools;
using CBRE.Graphics;
using CBRE.Providers.Map;
using CBRE.Providers.Model;
using CBRE.Providers.Texture;
using CBRE.Settings;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Num = System.Numerics;

namespace CBRE.Editor {
    partial class GameMain : Game
    {
        public static GameMain Instance { get; private set; }

        private DiscordManager discord;

        private GraphicsDeviceManager graphics;
        private ImGuiRenderer imGuiRenderer;

        private AsyncTexture rotateCursorTexture;
        private MouseCursor rotateCursor;
        public MouseCursor RotateCursor {
            get {
                if (rotateCursor != null) { return rotateCursor; }
                if (rotateCursorTexture?.MonoGameTexture != null) {
                    rotateCursor = MouseCursor.FromTexture2D(rotateCursorTexture.MonoGameTexture, 8, 8);
                    return rotateCursor;
                }
                return MouseCursor.Arrow;
            }
        }

        public GameTime LastTime { get; set; }

        public List<PopupUI> Popups { get; private set; } = new List<PopupUI>();
        public List<DockableWindow> Dockables { get; private set; } = new List<DockableWindow>();
        public Queue<Action> PostDrawActions { get; private set; } = new Queue<Action>();

        public GameMain()
        {
            Instance = this;

            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1600;
            graphics.PreferredBackBufferHeight = 900;
            graphics.PreferMultiSampling = false;
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();
            Window.AllowUserResizing = true;

            IsMouseVisible = true;
        }

        public static Dictionary<string, AsyncTexture> MenuTextures;

        private ImGuiStylePtr imGuiStyle;

        private DocumentTabs documentTabs;

        public void CreateImGuiRenderer(out ImGuiRenderer renderer, out ImGuiStylePtr style) {
            renderer = new ImGuiRenderer(this);
            renderer.RebuildFontAtlas();

            style = ImGui.GetStyle();
            style.TabMinWidthForCloseButton = 50f;
            style.ChildRounding = 0;
            style.FrameRounding = 0;
            style.GrabRounding = 0;
            style.PopupRounding = 0;
            style.ScrollbarRounding = 0;
            style.TabRounding = 0;
            style.WindowRounding = 0;
            style.FrameBorderSize = 0;
            style.DisplayWindowPadding = Num.Vector2.Zero;
            style.WindowPadding = Num.Vector2.Zero;
            style.IndentSpacing = 0;
            var colors = style.Colors;
            colors[(int)ImGuiCol.FrameBg] = new Num.Vector4(0.05f, 0.05f, 0.07f, 1.0f);
        }

        public void SetDiscord(bool enabled) {
            if (enabled) {
                if (discord == null) {
                    discord = new();
                }
            } else if (discord != null) {
                discord.Dispose();
                discord = null;
            }
        }

        protected override void Initialize()
        {
            SettingsManager.Read();
            ToolManager.Init();

            CreateImGuiRenderer(out imGuiRenderer, out imGuiStyle);

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            GlobalGraphics.Set(GraphicsDevice, Window, imGuiRenderer);

            MenuTextures = new Dictionary<string, AsyncTexture>();
            string[] files = Directory.GetFiles("Resources");
            foreach (string file in files) {
                if (!System.IO.Path.GetExtension(file).Equals(".png", StringComparison.OrdinalIgnoreCase)) { continue; }
                MenuTextures.Add(System.IO.Path.GetFileNameWithoutExtension(file),
                    LoadTexture(file));
            }

            Mediator.MediatorException += MediatorError;
            InitMenus();
            InitTopBar();
            InitToolBar();
            Subscribe();

            TextureProvider.CreatePackages(Directories.GetTextureCategories());

            MapProvider.Register(new VmfProvider());
            MapProvider.Register(new RmfProvider());
            MapProvider.Register(new MapFormatProvider());
            MapProvider.Register(new L3DWProvider());
            MapProvider.Register(new RMeshProvider());

            ModelProvider.Register(new AssimpProvider());

            ViewportManager.Init();
            DocumentManager.AddAndSwitch(new Document(Document.NewDocumentName, new DataStructures.MapObjects.Map()));

            SetDiscord(Misc.DiscordIntegration);

            documentTabs = new DocumentTabs();
            
            // Initial windows
            Dockables.AddRange(new DockableWindow[] {
                new ToolsWindow(),
                new ToolPropsWindow(),
                new StatsWindow(),
                new ViewportWindow(),
                new VisgroupsWindow()
            });

            base.Initialize();

            timing.StartMeasurement();
        }

        protected override void OnExiting(object sender, EventArgs args) {
            SettingsManager.Write();
            base.OnExiting(sender, args);
        }

        private AsyncTexture LoadTexture(string filename) {
            return new AsyncTexture(filename);
        }

        private readonly Timing timing = new Timing();
        private Keys[] previousKeys = Array.Empty<Keys>();

        protected override void Update(GameTime gameTime) {
            timing.EndMeasurement();
            timing.StartMeasurement();

            base.Update(gameTime);

            if (!Popups.Any()) {
                // Hotkeys
                Keys[] pressedKeys = Keyboard.GetState().GetPressedKeys().Where(k => k != Keys.None).ToArray();
                Keys[] hitKeys = pressedKeys.Where(k => !previousKeys.Contains(k)).ToArray();

                bool ctrlPressed = pressedKeys.Contains(Keys.LeftControl) || pressedKeys.Contains(Keys.RightControl);
                bool shiftPressed = pressedKeys.Contains(Keys.LeftShift) || pressedKeys.Contains(Keys.RightShift);
                bool altPressed = pressedKeys.Contains(Keys.LeftAlt) || pressedKeys.Contains(Keys.RightAlt);
                HotkeyImplementation def = Hotkeys.GetHotkeyFor(hitKeys, ctrlPressed, shiftPressed, altPressed);
                if (def != null) {
                    Mediator.Publish(def.Definition.Action, def.Definition.Parameter);
                }
                previousKeys = pressedKeys;
            }

            timing.PerformTicks(() => {
                Popups.ForEach(p => p.Update());
                Dockables.ForEach(d => d.Update());
            });

            LastTime = gameTime;
        }

        protected override void Draw(GameTime gameTime)
        {
            ViewportManager.RenderIfNecessary();

            GlobalGraphics.GraphicsDevice.Viewport = new Viewport(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height);

            TaskPool.Update();

            GraphicsDevice.Clear(new Color(50, 50, 60));

            // Call BeforeLayout first to set things up
            imGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
            imGuiRenderer.AfterLayout();

            base.Draw(gameTime);

            while (PostDrawActions.Count > 0) {
                try {
                    PostDrawActions.Dequeue()?.Invoke();
                } catch (Exception e) {
                    Logging.Logger.ShowException(e);
                }
            }
        }

        public const int MenuBarHeight = 20;
        protected virtual void ImGuiLayout() {
            uint dockId = ImGui.GetID("Dock");
            ImGuiViewportPtr viewportPtr = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewportPtr.Pos);
            ImGui.SetNextWindowSize(new Num.Vector2(viewportPtr.Size.X,MenuBarHeight + TopBarHeight + DocumentTabs.Height));
            if (ImGui.Begin("Main Window", ImGuiWindowFlags.NoMove |
                                           ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoBringToFrontOnFocus |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoScrollbar)) {
                if (ImGui.BeginMainMenuBar()) {
                    UpdateMenus();
                    ImGui.EndMainMenuBar();
                }
                UpdateTopBar();
                documentTabs.ImGuiLayout();
                
                ImGui.End();
            }
            
            ImGui.SetNextWindowPos(viewportPtr.Pos + new Num.Vector2(0, MenuBarHeight + TopBarHeight + DocumentTabs.Height));
            ImGui.SetNextWindowSize(viewportPtr.Size - new Num.Vector2(0, MenuBarHeight + TopBarHeight + DocumentTabs.Height));
            if (ImGui.Begin("Dock Space", ImGuiWindowFlags.NoMove |
                                          ImGuiWindowFlags.NoResize |
                                          ImGuiWindowFlags.NoCollapse |
                                          ImGuiWindowFlags.NoTitleBar |
                                          ImGuiWindowFlags.NoBringToFrontOnFocus)) {
                ImGui.DockSpace(dockId);
                ImGui.End();
            }

            for (int i = 0; i < Popups.Count; i++) {
                Popups[i].Draw(out bool shouldBeOpen);
                if (!shouldBeOpen) {
                    Popups[i].Dispose();
                    Popups.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < Dockables.Count; i++) {
                ImGui.SetNextWindowDockID(dockId, ImGuiCond.FirstUseEver);
                Dockables[i].Draw(out bool shouldBeOpen);
                if (!shouldBeOpen) {
                    Dockables[i].Dispose();
                    Dockables.RemoveAt(i);
                    i--;
                }
            }
            ImGui.SetNextWindowDockID(0, ImGuiCond.Always);
        }
	}
}
