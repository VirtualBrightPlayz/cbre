using CBRE.DataStructures.Geometric;
using CBRE.Editor.Tools.SelectTool;
using CBRE.Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace CBRE.Editor.Documents {
    internal class DocumentMemory {
        private readonly Dictionary<Viewport2D.ViewDirection, Tuple<Vector3, decimal>> _positions;
        private Vector3 _cameraLookat;
        private Vector3 _cameraLocation;
        public Type SelectedTool { get; set; }
        public PointF SplitterPosition { get; set; }

        private Dictionary<string, object> _store;

        public DocumentMemory() {
            _positions = new Dictionary<Viewport2D.ViewDirection, Tuple<Vector3, decimal>>();
            _cameraLocation = new Vector3(0, 0, 0);
            _cameraLookat = new Vector3(1, 0, 0);
            SelectedTool = typeof(SelectTool);
            _store = new Dictionary<string, object>();
        }

        public void SetCamera(Vector3 position, Vector3 look) {
            _cameraLocation = new Vector3(position.X, position.Y, position.Z);
            _cameraLookat = new Vector3(look.X, look.Y, look.Z);
        }

        public void RememberViewports(IEnumerable<ViewportBase> viewports) {
            // Todo viewport: remember types and positions
            _positions.Clear();
            foreach (var vp in viewports) {
                var vp3 = vp as Viewport3D;
                var vp2 = vp as Viewport2D;
                if (vp2 != null) {
                    if (!_positions.ContainsKey(vp2.Direction)) {
                        _positions.Add(vp2.Direction, Tuple.Create(vp2.Position, vp2.Zoom));
                    }
                }
                if (vp3 != null) {
                    var cam = vp3.Camera;
                    _cameraLookat = cam.LookPosition;
                    _cameraLocation = cam.EyePosition;
                }
            }
        }

        public void RestoreViewports(IEnumerable<ViewportBase> viewports) {
            foreach (var vp in viewports) {
                var vp3 = vp as Viewport3D;
                var vp2 = vp as Viewport2D;
                if (vp2 != null) {
                    if (_positions.ContainsKey(vp2.Direction)) {
                        vp2.Position = _positions[vp2.Direction].Item1;
                        vp2.Zoom = _positions[vp2.Direction].Item2;
                    }
                }
                if (vp3 != null) {
                    vp3.Camera.EyePosition = _cameraLocation;
                    vp3.Camera.LookPosition = _cameraLookat;
                }
            }
        }

        public void Set<T>(string name, T state) {
            if (_store.ContainsKey(name)) _store.Remove(name);
            _store.Add(name, state);
        }

        public T Get<T>(string name, T def = default(T)) {
            if (!_store.ContainsKey(name)) return def;
            var obj = _store[name];
            return obj is T ? (T)obj : def;
        }
    }
}
