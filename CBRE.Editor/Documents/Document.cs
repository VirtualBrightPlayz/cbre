using CBRE.Common;
using CBRE.Common.Mediator;
using CBRE.DataStructures.GameData;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions;
using CBRE.Editor.Editing;
using CBRE.Editor.History;
using CBRE.Editor.Rendering;
using CBRE.Editor.Settings;
using CBRE.Editor.Tools;
using CBRE.Graphics;
using CBRE.Providers.Map;
using CBRE.Providers.Texture;
using CBRE.Settings;
using CBRE.Settings.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Path = System.IO.Path;

namespace CBRE.Editor.Documents {
    public class Document {
        public string MapFile { get; set; }
        public string MapFileName { get; set; }
        public Map Map { get; set; }

        public GameData GameData { get; set; }

        public SelectionManager Selection { get; private set; }
        public HistoryManager History { get; private set; }

        public ObjectRenderer ObjectRenderer { get; private set; }

        private readonly DocumentSubscriptions _subscriptions;
        private readonly DocumentMemory _memory;

        private Document() {
            Map = new Map();
            Selection = new SelectionManager(this);
            History = new HistoryManager(this);
        }

        public Document(string mapFile, Map map) {
            MapFile = mapFile;
            Map = map;
            MapFileName = mapFile == null
                              ? DocumentManager.GetUntitledDocumentName()
                              : Path.GetFileName(mapFile);

            ObjectRenderer = new ObjectRenderer(this);

            SelectListTransform = Matrix.Identity;

            _subscriptions = new DocumentSubscriptions(this);

            _memory = new DocumentMemory();

            var cam = Map.GetActiveCamera();
            if (cam != null) _memory.SetCamera(cam.EyePosition, cam.LookPosition);

            Selection = new SelectionManager(this);
            History = new HistoryManager(this);
            if (Map.GridSpacing <= 0) {
                Map.GridSpacing = Grid.DefaultSize;
            }

            GameData = new GameData();

            var texList = Map.GetAllTextures();

            Map.PostLoadProcess(GameData, GetTexture, SettingsManager.GetSpecialTextureOpacity);

            if (MapFile != null) Mediator.Publish(EditorMediator.FileOpened, MapFile);

            //TODO: reimplement autosaving
        }

        public void SetMemory<T>(string name, T obj) {
            _memory.Set(name, obj);
        }

        public T GetMemory<T>(string name, T def = default(T)) {
            return _memory.Get(name, def);
        }

        public void SetActive() {
            if (!CBRE.Settings.View.KeepSelectedTool) ToolManager.Activate(_memory.SelectedTool);
            /*if (!CBRE.Settings.View.KeepCameraPositions) _memory.RestoreViewports(ViewportManager.Viewports);

            ViewportManager.AddContext3D(new WidgetLinesRenderable());
            Renderer.Register(ViewportManager.Viewports);
            ViewportManager.AddContextAll(new ToolRenderable());
            ViewportManager.AddContextAll(new HelperRenderable(this));*/

            _subscriptions.Subscribe();

            /*RenderAll();*/
        }

        public void SetInactive() {
            if (!CBRE.Settings.View.KeepSelectedTool && ToolManager.ActiveTool != null) _memory.SelectedTool = ToolManager.ActiveTool.GetType();
            /*if (!CBRE.Settings.View.KeepCameraPositions) _memory.RememberViewports(ViewportManager.Viewports);

            ViewportManager.ClearContexts();
            HelperManager.ClearCache();*/

            _subscriptions.Unsubscribe();
        }

        public void Close() {
            Scheduler.Clear(this);
        }

        public bool SaveFile(string path = null, bool forceOverride = false, bool switchPath = true) {
            path = forceOverride ? path : path ?? MapFile;

            if (path != null) {
                IEnumerable<string> noSaveExtensions = FileTypeRegistration.GetSupportedExtensions().Where(x => !x.CanSave).Select(x => x.Extension);
                foreach (string ext in noSaveExtensions) {
                    if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) {
                        path = null;
                        break;
                    }
                }
            }

            throw new NotImplementedException();
            /*if (path == null) {
                using (var sfd = new SaveFileDialog()) {
                    var filter = String.Join("|", FileTypeRegistration.GetSupportedExtensions()
                        .Where(x => x.CanSave).Select(x => x.Description + " (*" + x.Extension + ")|*" + x.Extension));
                    var all = FileTypeRegistration.GetSupportedExtensions().Where(x => x.CanSave).Select(x => "*" + x.Extension).ToArray();
                    sfd.Filter = "All supported formats (" + String.Join(", ", all) + ")|" + String.Join(";", all) + "|" + filter;
                    if (sfd.ShowDialog() == DialogResult.OK) {
                        path = sfd.FileName;
                    }
                }
            }
            if (path == null) return false;

            // Save the 3D camera position
            var cam = ViewportManager.Viewports.OfType<Viewport3D>().Select(x => x.Camera).FirstOrDefault();
            if (cam != null) {
                if (Map.ActiveCamera == null) {
                    Map.ActiveCamera = !Map.Cameras.Any() ? new Camera { LookPosition = Coordinate.UnitX * Map.GridSpacing * 1.5m } : Map.Cameras.First();
                    if (!Map.Cameras.Contains(Map.ActiveCamera)) Map.Cameras.Add(Map.ActiveCamera);
                }
                var dist = (Map.ActiveCamera.LookPosition - Map.ActiveCamera.EyePosition).VectorMagnitude();
                var loc = cam.Location;
                var look = cam.LookAt - cam.Location;
                look.Normalize();
                look = loc + look * (float)dist;
                Map.ActiveCamera.EyePosition = new Coordinate((decimal)loc.X, (decimal)loc.Y, (decimal)loc.Z);
                Map.ActiveCamera.LookPosition = new Coordinate((decimal)look.X, (decimal)look.Y, (decimal)look.Z);
            }
            Map.WorldSpawn.EntityData.SetPropertyValue("wad", string.Join(";", GetUsedTexturePackages().Select(x => x.PackageRoot).Where(x => x.EndsWith(".wad"))));
            MapProvider.SaveMapToFile(path, Map);
            if (switchPath) {
                MapFile = path;
                MapFileName = Path.GetFileName(MapFile);
                History.TotalActionsSinceLastSave = 0;
                Mediator.Publish(EditorMediator.DocumentSaved, this);
            }*/
            return true;
        }

        private string GetAutosaveFormatString() {
            if (MapFile == null || Path.GetFileNameWithoutExtension(MapFile) == null) return null;
            var we = Path.GetFileNameWithoutExtension(MapFile);
            var ex = Path.GetExtension(MapFile);
            return we + ".auto.{0}" + ex;
        }

        private string GetAutosaveFolder() {
            //if (Game.UseCustomAutosaveDir && System.IO.Directory.Exists(Game.AutosaveDir)) return Game.AutosaveDir;
            if (MapFile == null || Path.GetDirectoryName(MapFile) == null) return null;
            return Path.GetDirectoryName(MapFile);
        }

        public void Autosave() {
            throw new NotImplementedException();
            /*
            //if (!Game.Autosave) return;
            var dir = GetAutosaveFolder();
            var fmt = GetAutosaveFormatString();

            // Only save on change if the game is configured to do so
            if (dir != null && fmt != null && (History.TotalActionsSinceLastAutoSave != 0 || !Game.AutosaveOnlyOnChanged)) {
                var date = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd-hh-mm-ss");
                var filename = String.Format(fmt, date);
                if (System.IO.File.Exists(filename)) System.IO.File.Delete(filename);

                // Save the file
                MapProvider.SaveMapToFile(Path.Combine(dir, filename), Map);

                // Delete extra autosaves if there is a limit
                if (Game.AutosaveLimit > 0) {
                    var asFiles = GetAutosaveFiles(dir);
                    foreach (var file in asFiles.OrderByDescending(x => x.Value).Skip(Game.AutosaveLimit)) {
                        if (System.IO.File.Exists(file.Key)) System.IO.File.Delete(file.Key);
                    }
                }

                // Publish event
                Mediator.Publish(EditorMediator.FileAutosaved, this);
                History.TotalActionsSinceLastAutoSave = 0;

                if (Game.AutosaveTriggerFileSave && MapFile != null) {
                    SaveFile();
                }
            }

            // Reschedule autosave
            var at = Math.Max(1, Game.AutosaveTime);
            Scheduler.Schedule(this, Autosave, TimeSpan.FromMinutes(at));*/
        }

        public Dictionary<string, DateTime> GetAutosaveFiles(string dir) {
            var ret = new Dictionary<string, DateTime>();
            var fs = GetAutosaveFormatString();
            if (fs == null || dir == null) return ret;
            // Search for matching files
            var files = System.IO.Directory.GetFiles(dir, String.Format(fs, "*"));
            foreach (var file in files) {
                // Match the date portion with a regex
                var re = Regex.Escape(fs.Replace("{0}", ":")).Replace(":", "{0}");
                var regex = String.Format(re, "(\\d{4})-(\\d{2})-(\\d{2})-(\\d{2})-(\\d{2})-(\\d{2})");
                var match = Regex.Match(Path.GetFileName(file), regex, RegexOptions.IgnoreCase);
                if (!match.Success) continue;

                // Parse the date and add it if it is valid
                DateTime date;
                var result = DateTime.TryParse(String.Format("{0}-{1}-{2}T{3}:{4}:{5}Z",
                                                             match.Groups[1].Value, match.Groups[2].Value,
                                                             match.Groups[3].Value, match.Groups[4].Value,
                                                             match.Groups[5].Value, match.Groups[6].Value),
                                                             CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                                                             out date);
                if (result) {
                    ret.Add(file, date);
                }
            }
            return ret;
        }

        public Vector3 Snap(Vector3 c, decimal spacing = 0) {
            if (!Map.SnapToGrid) return c;

            bool snap = true;
            /*var snap = (Select.SnapStyle == SnapStyle.SnapOnAlt && ViewportManager.Alt) ||
                       (Select.SnapStyle == SnapStyle.SnapOffAlt && !ViewportManager.Alt);*/

            return snap ? c.Snap(spacing == 0 ? Map.GridSpacing : spacing) : c;
        }

        /// <summary>
        /// Performs the action, adds it to the history stack, and optionally updates the display lists
        /// </summary>
        /// <param name="name">The name of the action, for history purposes</param>
        /// <param name="action">The action to perform</param>
        public void PerformAction(string name, IAction action) {
            try {
                action.Perform(this);
            } catch (Exception ex) {
                var st = new StackTrace();
                var frames = st.GetFrames() ?? new StackFrame[0];
                var msg = "Action exception: " + name + " (" + action + ")";
                foreach (var frame in frames) {
                    var method = frame.GetMethod();
                    msg += "\r\n    " + method.ReflectedType.FullName + "." + method.Name;
                }
                Logging.Logger.ShowException(new Exception(msg, ex), "Error performing action");
            }

            var history = new HistoryAction(name, action);
            History.AddHistoryItem(history);
        }

        public Matrix SelectListTransform {
            get { return ObjectRenderer.TexturedShaded.Parameters["Selection"].GetValueMatrix().ToCbre(); }
            set {
                ObjectRenderer.TexturedShaded.Parameters["Selection"].SetValue(value.ToXna());
                ObjectRenderer.SolidShaded.Parameters["Selection"].SetValue(value.ToXna());
            }
        }

        public void SetSelectListTransform(Matrix matrix) {
            SelectListTransform = matrix;
        }

        public void EndSelectionTransform() {
            SelectListTransform = Matrix.Identity;
        }

        public ITexture GetTexture(string name) {
            TextureItem item = TextureProvider.GetItem(name);
            return item?.Texture;
        }

        public void RenderAll() {
            ObjectRenderer.MarkDirty();
            ViewportManager.MarkForRerender();
        }

        public void RenderSelection(IEnumerable<MapObject> objects) {
            RenderObjects(objects);
        }

        public void RenderObjects(IEnumerable<MapObject> objects) {
            var faces = objects.Where(obj => obj is Solid).SelectMany(obj => ((Solid)obj).Faces);
            RenderFaces(faces);
        }

        public void RenderFaces(IEnumerable<Face> faces) {
            var texNames = faces.Select(f => f.Texture.Name).Distinct();
            foreach (var tex in texNames) {
                ObjectRenderer.MarkDirty(tex);
            }
            ViewportManager.MarkForRerender();
        }

        public void Make3D(ViewportBase viewport, Viewport3D.ViewType type) {
            throw new NotImplementedException();
            /*var vp = ViewportManager.Make3D(viewport, type);
            vp.RenderContext.Add(new WidgetLinesRenderable());
            Renderer.Register(new[] { vp });
            vp.RenderContext.Add(new ToolRenderable());
            vp.RenderContext.Add(new HelperRenderable(this));
            Renderer.UpdateGrid(Map.GridSpacing, Map.Show2DGrid, Map.Show3DGrid, false);*/
        }

        public void Make2D(ViewportBase viewport, Viewport2D.ViewDirection direction) {
            throw new NotImplementedException();
            /*var vp = ViewportManager.Make2D(viewport, direction);
            Renderer.Register(new[] { vp });
            vp.RenderContext.Add(new ToolRenderable());
            vp.RenderContext.Add(new HelperRenderable(this));
            Renderer.UpdateGrid(Map.GridSpacing, Map.Show2DGrid, Map.Show3DGrid, false);*/
        }

        public IEnumerable<string> GetUsedTextures() {
            return Map.WorldSpawn.Find(x => x is Solid).OfType<Solid>().SelectMany(x => x.Faces).Select(x => x.Texture.Name).Distinct();
        }
    }
}
