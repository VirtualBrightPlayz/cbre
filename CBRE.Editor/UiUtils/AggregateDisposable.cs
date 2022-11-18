using System;
using System.Collections.Immutable;

namespace CBRE.Editor {
    public struct AggregateDisposable : IDisposable {
        private ImmutableArray<IDisposable> disposables;

        public AggregateDisposable(params IDisposable[] disposables) {
            this.disposables = disposables.ToImmutableArray();
        }

        public void Dispose() {
            for (int i = disposables.Length - 1; i >= 0; i--) {
                disposables[i].Dispose();
            }
        }
    }
}
