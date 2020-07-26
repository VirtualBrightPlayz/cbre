using CBRE.Editor.Documents;
using CBRE.Providers.Model;

namespace CBRE.Editor.Compiling {
    class GenericExport {
        public static void SaveToFile(string filename, Document document, string format) {
            AssimpProvider.SaveToFile(filename, document.Map, format);
        }
    }
}
