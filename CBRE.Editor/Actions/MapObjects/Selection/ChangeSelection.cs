using CBRE.Common.Mediator;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CBRE.Editor.Actions.MapObjects.Selection {
    public class ChangeSelection : IAction {
        public bool SkipInStack { get { return CBRE.Settings.Select.SkipSelectionInUndoStack; } }
        public bool ModifiesState { get { return false; } }

        private long[] _selected;
        private long[] _deselected;

        public ChangeSelection(IEnumerable<MapObject> selected, IEnumerable<MapObject> deselected) {
            _selected = selected.Select(x => x.ID).ToArray();
            _deselected = deselected.Select(x => x.ID).ToArray();
        }

        public ChangeSelection(IEnumerable<long> selected, IEnumerable<long> deselected) {
            _selected = selected.ToArray();
            _deselected = deselected.ToArray();
        }

        public void Dispose() {
            _selected = _deselected = null;
        }

        public void Reverse(Document document) {
            var sel = _selected.Select(x => document.Map.WorldSpawn.FindByID(x)).Where(x => x != null).ToArray();
            var desel = _deselected.Select(x => document.Map.WorldSpawn.FindByID(x)).Where(x => x != null && x.BoundingBox != null).ToArray();

            document.Selection.Deselect(sel);
            document.Selection.Select(desel);

            Mediator.Publish(EditorMediator.DocumentTreeSelectedObjectsChanged, sel.Union(desel));
            Mediator.Publish(EditorMediator.SelectionChanged);
        }

        public void Perform(Document document) {
            var desel = _deselected.Select(x => document.Map.WorldSpawn.FindByID(x)).Where(x => x != null).ToArray();
            var sel = _selected.Select(x => document.Map.WorldSpawn.FindByID(x)).Where(x => x != null && x.BoundingBox != null).ToArray();

            document.Selection.Deselect(desel);
            document.Selection.Select(sel);

            Mediator.Publish(EditorMediator.DocumentTreeSelectedObjectsChanged, sel.Union(desel));
            Mediator.Publish(EditorMediator.SelectionChanged);
        }
    }
}
