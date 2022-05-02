using System;

namespace NelaSystem.ChipLisp {
    public class InterpreterException : Exception {
        public Exception inner { get; private set; }

        public InterpreterException(Obj expr, Exception inner)
            : base($"Exception thrown while evaluating {expr} at ({expr.sourcePos.Item1}:{expr.sourcePos.Item2})\n{inner.Message}") {
            this.inner = inner;
        }
    }

    public class LexerException : Exception {
        private static string MakeMessage(Lexer lexer, string restMessage) {
            var (sourceLine, sourceColumn) = lexer.GetCurrentSourcePos();
            return $"Lexing Error at {sourceLine}:{sourceColumn}\n{restMessage}";
        }

        public readonly string restMessage;
        public readonly (int, int) sourcePos;

        public LexerException(Lexer lexer, string message)
            : base(MakeMessage(lexer, message)) {
            this.sourcePos = lexer.GetCurrentSourcePos();
            this.restMessage = message;
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