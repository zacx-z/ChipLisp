using System;

namespace NelaSystem.ChipLisp {
    public class InterpreterException : Exception {
        public Exception exception { get; private set; }

        public InterpreterException(Obj expr, Exception exception)
            : base($"Exception thrown while evaluating {expr} at ({expr.sourcePos.Item1}:{expr.sourcePos.Item2})\n{exception.Message}") {
            this.exception = exception;
        }
    }

    public class RuntimeException : Exception {
        public RuntimeException(string message) : base(message) {}
    }

    public class InvalidCallException : Exception {
        public Obj obj { get; private set; }

        public InvalidCallException(Obj obj) {
            this.obj = obj;
        }
    }

    public class NotListException : Exception {
        public Obj obj { get; private set; }

        public NotListException(Obj obj) {
            this.obj = obj;
        }
    }

    public class PrimitiveRuntimeException : Exception {
        public PrimitiveRuntimeException(Obj p, string message)
            : base($"Error when running primitive function: {p}\nMessage: {message}") {}
    }
}