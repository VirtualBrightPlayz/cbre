using System;
using System.IO;
using System.Linq;
using CBRE.Common.Mediator;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Providers.Map;
using static CBRE.Editor.GameMain;

namespace CBRE.Editor;

class RecentMenu : Menu, IMediatorListener {
    public RecentMenu() : base("Recent files") {
        Mediator.Subscribe(EditorMediator.DocumentClosed, this);
        Items.Add(new MenuItem("Clear recent", "", null, () => {
            foreach (MenuItem item in Items.Skip(1)) {
                ItemsToRemove.Add(item);
            }
        }));
    }

    public void Notify(Enum message, object data) {
        Document doc = data as Document;
        if (doc.MapFile == null) { return; }

        int i = Items.FindIndex(x => x.Name == doc.MapFile);
        if (i != -1) {
            // Bring to front.
            MenuItem item = Items[i];
            Items.RemoveAt(i);
            Items.Insert(1, item);
            return;
        }

        MenuItem newItem = new(doc.MapFile, "");
        newItem.Action = () => {
            if (!File.Exists(doc.MapFile)) {
                ItemsToRemove.Add(newItem);
                return;
            }
            Map _map = MapProvider.GetMapFromFile(doc.MapFile);
            DocumentManager.AddAndSwitch(new Document(doc.MapFile, _map));
        };
        Items.Insert(1, newItem);
    }

    private void Remove(MenuItem recent) {
        Items.Remove(recent);
    }
}
