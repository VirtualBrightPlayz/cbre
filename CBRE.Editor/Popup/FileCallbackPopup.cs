using System;
using System.IO;
using System.Text;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class FileCallbackPopup : FileSelectPopup {
        private Action<string> _callback;

        public FileCallbackPopup(string title, string path, Action<string> callback) : base(title, path) {
            _callback = callback;
        }

        protected override void FileSelected(string file)
        {
            _callback?.Invoke(file);
        }
    }
}