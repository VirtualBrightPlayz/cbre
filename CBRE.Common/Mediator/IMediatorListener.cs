using System;

namespace CBRE.Common.Mediator {
    public interface IMediatorListener {
        void Notify(Enum message, object data);
    }
}
