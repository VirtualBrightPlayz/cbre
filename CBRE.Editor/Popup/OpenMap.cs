using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Providers;
using CBRE.Providers.Map;
using ImGuiNET;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class OpenMap : FileSelectPopup
    {
        public OpenMap(string path) : base("Open Map", path)
        {
        }

        protected override void FileSelected(string file) {
            try
            {
                Map _map = MapProvider.GetMapFromFile(file);
                DocumentManager.AddAndSwitch(new Document(file, _map));
            }
            catch (ProviderException e)
            {
                new MessagePopup("Error", e.Message, new ImColor() { Value = new Num.Vector4(1f, 0f, 0f, 1f) });
            }
        }
    }
}