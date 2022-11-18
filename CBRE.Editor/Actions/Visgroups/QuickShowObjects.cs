using CBRE.Common.Mediator;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Actions.Visgroups {
    public class QuickShowObjects : IAction {
        public bool SkipInStack { get { return CBRE.Settings.Select.SkipVisibilityInUndoStack; } }
        public bool ModifiesState { get { return false; } }

        private List<MapObject> _objects;
        private int _removed;

        public QuickShowObjects(IEnumerable<MapObject> objects) {
            _objects = objects.SelectMany(x => x.GetSelfAndAllChildren()).Distinct().Where(x => x.IsVisgroupHidden).ToList();
        }

        public void Dispose() {
            _objects = null;
        }

        public void Reverse(Document document) {
            foreach (var mapObject in _objects) {
                var o = mapObject;
                if (!o.AutoVisgroups.Contains(_removed)) {
                    o.AutoVisgroups.Add(_removed);
                    o.Visgroups.Add(_removed);
                }
                o.IsVisgroupHidden = true;
                document.ObjectRenderer.RemoveMapObject(o);
            }
            Mediator.Publish(EditorMediator.DocumentTreeStructureChanged);
            Mediator.Publish(EditorMediator.VisgroupsChanged);
        }

        public void Perform(Document document) {
            var autohide = document.Map.GetAllVisgroups().First(x => x.Name == "Autohide");
            _removed = autohide.ID;
            foreach (var mapObject in _objects) {
                var o = mapObject;
                o.AutoVisgroups.Remove(_removed);
                o.Visgroups.Remove(_removed);
                o.IsVisgroupHidden = false;
                document.ObjectRenderer.AddMapObject(o);
            }
            Mediator.Publish(EditorMediator.DocumentTreeStructureChanged);
            Mediator.Publish(EditorMediator.VisgroupsChanged);
        }
    }
}
