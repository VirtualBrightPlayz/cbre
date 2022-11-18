using CBRE.Common.Mediator;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Actions.MapObjects.Operations {
    public class ReplaceObjects : IAction {
        public bool SkipInStack { get { return false; } }
        public bool ModifiesState { get { return true; } }

        private readonly Dictionary<long, MapObject> _perform;
        private readonly Dictionary<long, MapObject> _reverse;

        public ReplaceObjects(IEnumerable<MapObject> before, IEnumerable<MapObject> after) {
            _perform = before.ToDictionary(x => x.ID, x => after.FirstOrDefault(y => y.ID == x.ID));
            _reverse = new Dictionary<long, MapObject>();
        }

        public void Dispose() {
            _perform.Clear();
            _reverse.Clear();
        }

        public void Reverse(Document document) {
            var root = document.Map.WorldSpawn;
            foreach (var kv in _reverse) {
                var obj = root.FindByID(kv.Key);
                if (obj == null) { return; }

                // Unclone will reset children, need to reselect them if needed
                var deselect = obj.GetSelfAndAllChildren().Where(x => x.IsSelected).ToList();
                document.Selection.Deselect(deselect);

                SwapObjects(document, obj, kv.Value);

                var select = obj.GetSelfAndAllChildren().Where(x => deselect.Any(y => x.ID == y.ID));
                document.Selection.Select(select);

                document.Map.UpdateAutoVisgroups(obj, true);
            }

            Mediator.Publish(EditorMediator.DocumentTreeStructureChanged, _reverse.Select(x => document.Map.WorldSpawn.FindByID(x.Key)));
            Mediator.Publish(EditorMediator.SelectionChanged);
            Mediator.Publish(EditorMediator.VisgroupsChanged);

            _reverse.Clear();
        }

        private void SwapObjects(Document document, MapObject prev, MapObject newObj) {
            document.ObjectRenderer.RemoveMapObject(prev);
            prev.Unclone(newObj);
            document.ObjectRenderer.AddMapObject(prev);
        }

        public void Perform(Document document) {
            var root = document.Map.WorldSpawn;
            _reverse.Clear();
            foreach (var kv in _perform) {
                var obj = root.FindByID(kv.Key);
                if (obj == null) { return; }

                _reverse.Add(kv.Key, obj.Clone());

                // Unclone will reset children, need to reselect them if needed
                var deselect = obj.GetSelfAndAllChildren().Where(x => x.IsSelected).ToList();
                document.Selection.Deselect(deselect);

                SwapObjects(document, obj, kv.Value);

                var select = obj.GetSelfAndAllChildren().Where(x => deselect.Any(y => x.ID == y.ID));
                document.Selection.Select(select);

                document.Map.UpdateAutoVisgroups(obj, true);
            }

            Mediator.Publish(EditorMediator.DocumentTreeStructureChanged, _perform.Select(x => document.Map.WorldSpawn.FindByID(x.Key)));
            Mediator.Publish(EditorMediator.SelectionChanged);
            Mediator.Publish(EditorMediator.VisgroupsChanged);
        }
    }
}
