using CBRE.DataStructures.MapObjects;
using CBRE.Providers;
using CBRE.Providers.Map;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class SaveMap : FileSelectPopup
    {
        private Map _map;

        public SaveMap(string path, Map map) : base("Save Map", path)
        {
            _map = map;
        }

        protected override void FileSelected(string file) {
            try
            {
                MapProvider.SaveMapToFile(file, _map);
            }
            catch (ProviderNotFoundException e)
            {
                new MessagePopup("Error", e.Message, new ImColor() { Value = new Num.Vector4(1f, 0f, 0f, 1f) });
            }
        }
    }
}