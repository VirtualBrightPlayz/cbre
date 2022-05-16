using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CBRE.Common.Mediator;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Documents;
using CBRE.Providers.Map;
using CBRE.Common.Extensions;
using CBRE.Settings;
using static CBRE.Editor.GameMain;

namespace CBRE.Editor;

class RecentMenu : Menu, IMediatorListener {
    private const int MAX_COUNT = 10;

    public static readonly RecentMenu Instance = new();

    private static IEnumerable<string> History {
        get => Instance.Items.Skip(1).Select(i => i.Name);
        set {
            Instance.Items.Remove(1..);
            Instance.Items.AddRange(value.Select(s => Instance.ItemFromString(s)));
        }
    }

    static RecentMenu() {
        History = Recent.RecentFiles;
        Recent.RecentFilesAccessors = new(
            () => History.ToList(),
            files => History = files
        );
    }

    private RecentMenu() : base("Recent files") {
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

        Items.Insert(1, ItemFromString(doc.MapFile));
        Items.Remove((MAX_COUNT + 1)..);
    }

    internal MenuItem ItemFromString(string str) {
        MenuItem newItem = new(str, "");
        newItem.Action = () => {
            if (!File.Exists(str)) {
                PostponedActions.Add(() => {
                    Items.Remove(newItem);
                });
                return;
            }
            Map _map = MapProvider.GetMapFromFile(str);
            DocumentManager.AddAndSwitch(new Document(str, _map));
        };
        return newItem;
    }
}
