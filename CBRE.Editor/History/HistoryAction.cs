using CBRE.Editor.Actions;
using CBRE.Editor.Documents;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.History {
    public sealed class HistoryAction : IHistoryItem {
        private readonly List<IAction> actions;

        public string Name { get; }
        public bool SkipInStack { get; }
        public bool ModifiesState { get; }

        public HistoryAction(string name, params IAction[] actions) {
            Name = name;
            this.actions = actions.ToList();
            SkipInStack = actions.All(x => x.SkipInStack);
            ModifiesState = actions.Any(x => x.ModifiesState);
        }

        public void Undo(Document document) {
            for (var i = actions.Count - 1; i >= 0; i--) {
                actions[i].Reverse(document);
            }
        }

        public void Redo(Document document) {
            actions.ForEach(x => x.Perform(document));
        }

        public void Dispose() {
            actions.ForEach(x => x.Dispose());
            actions.Clear();
        }
    }
}
