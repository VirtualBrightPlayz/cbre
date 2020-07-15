using CBRE.Editor.Documents;
using CBRE.Graphics.Renderables;
using CBRE.UI;

namespace CBRE.Editor.Rendering.Helpers {
    public class HelperRenderable : IRenderable {
        private readonly Document _document;

        public HelperRenderable(Document document) {
            _document = document;
        }

        public void Render(object sender) {
            var vp = sender as ViewportBase;
            _document.HelperManager.Render(vp);
        }
    }
}