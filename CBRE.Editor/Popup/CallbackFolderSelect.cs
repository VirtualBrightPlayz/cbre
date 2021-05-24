using System;

namespace CBRE.Editor.Popup {
    public class CallbackFolderSelect : FolderSelectPopup {
        private Action<string> _callback;
        public CallbackFolderSelect(string title, string path, Action<string> callback) : base(title, path) {
            _callback = callback;
        }

        protected override void FileSelected(string file) {
            _callback?.Invoke(file);
        }
    }
}