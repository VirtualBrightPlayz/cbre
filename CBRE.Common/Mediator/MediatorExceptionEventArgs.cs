using System;

namespace CBRE.Common.Mediator {
    public class MediatorExceptionEventArgs : EventArgs {
        public Enum Message { get; set; }
        public Exception Exception { get; set; }

        public MediatorExceptionEventArgs(Enum message, Exception exception) {
            Exception = exception;
            Message = message;
        }
    }
}
