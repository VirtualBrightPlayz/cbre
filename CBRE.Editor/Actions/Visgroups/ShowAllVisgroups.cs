using CBRE.Common.Mediator;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Actions.Visgroups {
    public class ShowAllVisgroups : IAction {
        public bool SkipInStack { get { return CBRE.Settings.Select.SkipVisibilityInUndoStack; } }
        public bool ModifiesState { get { return false; } }

        private List<MapObject> _shown;
        private List<int> _hiddenGroups;

        public void Dispose() {
            _shown = null;
            _hiddenGroups = null;
        }

        public void Reverse(Document document) {
            _shown.ForEach(x => x.IsVisgroupHidden = true);
            _hiddenGroups.Select(x => document.Map.Visgroups.FirstOrDefault(v => v.ID == x))
                .Where(x => x != null).ToList()
                .ForEach(x => x.Visible = false);

            Mediator.Publish(EditorMediator.VisgroupsChanged);
            Mediator.Publish(EditorMediator.DocumentTreeStructureChanged);

            _shown = null;
        }

        public void Perform(Document document) {
            _hiddenGroups = document.Map.Visgroups.Where(x => !x.Visible).Select(x => x.ID).ToList();
            _shown = document.Map.WorldSpawn.GetSelfAndAllChildren()
                .Where(x => x.IsVisgroupHidden).ToList();
            _shown.ForEach(x => x.IsVisgroupHidden = false);
            document.Map.Visgroups.ForEach(x => x.Visible = true);

            Mediator.Publish(EditorMediator.VisgroupsChanged);
            Mediator.Publish(EditorMediator.DocumentTreeStructureChanged);
        }
    }
}