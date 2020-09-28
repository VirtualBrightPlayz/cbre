using CBRE.DataStructures.Geometric;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace CBRE.Editor.Rendering {
    public abstract class ViewportBase {
        private Stopwatch _stopwatch;
        public bool IsFocused { get; private set; }
        private int UnfocusedUpdateCounter { get; set; }

        private object _inputLock;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public MouseCursor Cursor { get; set; }

        public bool PrevMouseOver;
        public int PrevMouseX;
        public int PrevMouseY;

        public bool IsUnlocked(object context) {
            return _inputLock == null || _inputLock == context;
        }

        public bool AquireInputLock(object context) {
            if (_inputLock == null) _inputLock = context;
            return _inputLock == context;
        }

        public bool ReleaseInputLock(object context) {
            if (_inputLock == context) _inputLock = null;
            return _inputLock == null;
        }

        public delegate void RenderExceptionEventHandler(object sender, Exception exception);
        public event RenderExceptionEventHandler RenderException;
        protected void OnRenderException(Exception ex) {
            if (RenderException != null) {
                var st = new StackTrace();
                var frames = st.GetFrames() ?? new StackFrame[0];
                var msg = "Rendering exception: " + ex.Message;
                foreach (var frame in frames) {
                    var method = frame.GetMethod();
                    msg += "\r\n    " + method.ReflectedType.FullName + "." + method.Name;
                }
                RenderException(this, new Exception(msg, ex));
            }
        }

        public delegate void ListenerExceptionEventHandler(object sender, Exception exception);
        public event ListenerExceptionEventHandler ListenerException;
        protected void OnListenerException(Exception ex) {
            if (ListenerException != null) {
                var st = new StackTrace();
                var frames = st.GetFrames() ?? new StackFrame[0];
                var msg = "Listener exception: " + ex.Message;
                foreach (var frame in frames) {
                    var method = frame.GetMethod();
                    msg += "\r\n    " + method.ReflectedType.FullName + "." + method.Name;
                }
                ListenerException(this, new Exception(msg, ex));
            }
        }

        protected ViewportBase()  {
            _stopwatch = new Stopwatch();
        }

        public virtual Microsoft.Xna.Framework.Matrix GetViewportMatrix() {
            return Microsoft.Xna.Framework.Matrix.Identity;
        }

        public virtual Microsoft.Xna.Framework.Matrix GetCameraMatrix() {
            return Microsoft.Xna.Framework.Matrix.Identity;
        }

        public virtual Microsoft.Xna.Framework.Matrix GetModelViewMatrix() {
            return Microsoft.Xna.Framework.Matrix.Identity;
        }

        public virtual void FocusOn(Box box) {
            FocusOn(box.Center);
        }

        public virtual void FocusOn(Vector3 coordinate) {
            // Virtual
        }

        protected virtual void UpdateAfterLoadIdentity() {

        }

        protected virtual void UpdateBeforeSetViewport() {

        }

        protected virtual void UpdateBeforeClearViewport() {

        }

        protected virtual void UpdateBeforeRender() {

        }

        protected virtual void UpdateAfterRender() {

        }

        public abstract void Render();
    }
}
