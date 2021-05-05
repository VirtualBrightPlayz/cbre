using System.IO;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Providers;
using CBRE.Providers.Map;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class SavePopup : PopupGame {
        private string _message;
        private Map _map;
        private string _path;
        private int radioIndex;

        public SavePopup(string title, string message, string path, Map map) : base(title) {
            _message = message;
            _path = path;
            _map = map;
        }

        protected override bool ImGuiLayout() {
            ImGui.Text(_message);
            ImGui.NewLine();
            string[] files = Directory.GetFiles(_path);
            for (int i = 0; i < files.Length; i++)
            {
                ImGui.RadioButton(files[i], ref radioIndex, i);
            }
            if (ImGui.Button("Don't Save")) {
                return false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Save")) {
                try
                {
                    MapProvider.SaveMapToFile(_path, _map);
                }
                catch (ProviderNotFoundException e)
                {
                    new MessagePopup("Error", e.Message);
                }
                return false;
            }
            return true;
        }
    }
}