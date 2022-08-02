using System;
using CBRE.Common.Mediator;
using CBRE.Editor.Documents;
using DiscordRPC;
using DiscordRPC.Logging;

namespace CBRE.Editor;

class DiscordManager : IMediatorListener, IDisposable {
    private DiscordRpcClient client;

    public DiscordManager() {
        client = new("965075413346951189");
        client.Logger = new ConsoleLogger(LogLevel.Warning);
        client.Initialize();

        Mediator.Subscribe(EditorMediator.CompileStarted, this);
        Mediator.Subscribe(EditorMediator.CompileFinished, this);
        Mediator.Subscribe(EditorMediator.CompileFailed, this);
        Mediator.Subscribe(EditorMediator.DocumentActivated, this);
        Mediator.Subscribe(EditorMediator.DocumentAllClosed, this);
        client.SetPresence(_basicPresence);

        var curDoc = DocumentManager.CurrentDocument;
        if (curDoc == null) {
            Notify(EditorMediator.DocumentAllClosed, null);
        } else {
            Notify(EditorMediator.DocumentActivated, curDoc);
        }
    }

    ~DiscordManager() {
        Dispose();
    }

    public void Dispose() {
        if (client != null) {
            Mediator.UnsubscribeAll(this);
            client.ClearPresence();
            client.Dispose();
            client = null;
        }
    }

    private readonly RichPresence _basicPresence = new() {
        Assets = new() {
            LargeImageKey = "icon",
            LargeImageText = VersionUtil.Version
        },
        Timestamps = Timestamps.Now,
        Buttons = new [] {
            new Button {
                Label = "Website",
                Url = "https://scp-cbn.github.io/cbre/"
            },
            new Button {
                Label = "GitHub",
                Url = "https://github.com/SCP-CBN/cbre"
            }
        }
    };

    public void Notify(Enum message, object data) {
        switch (message) {
            case EditorMediator.CompileStarted:
                {
                    var doc = data as Document;
                    client.UpdateDetails($"Compiling \"{doc!.MapFileName}\"");
                }
                break;
            case EditorMediator.CompileFinished:
            case EditorMediator.CompileFailed:
            case EditorMediator.DocumentActivated:
                {
                    var doc = data as Document;
                    client.UpdateDetails($"Editing \"{doc!.MapFileName}\"");
                }
                break;
            case EditorMediator.DocumentAllClosed:
                client.UpdateDetails("Contemplating life choices");
                break;
        }
    }
}
