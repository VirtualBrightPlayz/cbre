using System;
using System.IO;
using CBRE.Common.Mediator;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Providers.Map;
using CBRE.Common.Extensions;
using static CBRE.Editor.GameMain;

namespace CBRE.Editor;

class RecentMenu : Menu, IMediatorListener {
    private const int MAX_COUNT = 10;

    public RecentMenu() : base("Recent files") {
        Mediator.Subscribe(EditorMediator.DocumentOpened, this);
        Mediator.Subscribe(EditorMediator.DocumentSaved, this);
        Items.Add(new MenuItem("Clear recent", "", null, () => {
            PostponedActions.Add(() => {
                Items.Remove(1..);
            });
        }));
    }

    public void Notify(Enum message, object data) {
        Document doc = data as Document;
        if (doc.MapFile == null) { return; }

        if (Items.Find(x => x.Name == doc.MapFile) != null) {
            PostponedActions.Add(() => {
                int i = Items.FindIndex(x => x.Name == doc.MapFile);
                if (i != -1) {
                    // Bring to front.
                    MenuItem item = Items[i];
                    Items.RemoveAt(i);
                    Items.Insert(1, item);
                }
            });
            return;
        }

        MenuItem newItem = new(doc.MapFile, "");
        newItem.Action = () => {
            if (!File.Exists(doc.MapFile)) {
                PostponedActions.Add(() => {
                    Items.Remove(newItem);
                });
                return;
            }
            Map _map = MapProvider.GetMapFromFile(doc.MapFile);
            DocumentManager.AddAndSwitch(new Document(doc.MapFile, _map));
        };
        Items.Insert(1, newItem);
        Items.Remove((MAX_COUNT + 1)..);
    }

    private void Remove(MenuItem recent) {
        Items.Remove(recent);
    }
}
