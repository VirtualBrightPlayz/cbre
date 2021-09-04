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
            
        }
    }
}
