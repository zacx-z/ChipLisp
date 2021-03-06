using System;

namespace Nela.ChipLisp {
    public class InterpreterException : Exception {
        public Exception inner { get; }
        private Obj expr;

        public override string Message => $"Error while evaluating {expr} at ({expr.sourcePos.Item1}:{expr.sourcePos.Item2})\n{inner.Message}";

        public override string StackTrace => $"{inner.StackTrace}\n{base.StackTrace}";

        public InterpreterException(Obj expr, Exception inner) {
            this.inner = inner;
            this.expr = expr;
        }
    }

    public class LexerException : Exception {
        private static string MakeMessage(ILexer lexer, string restMessage) {
            var (sourceLine, sourceColumn) = lexer.GetCurrentSourcePos();
            return $"Lexing Error at {sourceLine}:{sourceColumn}\n{restMessage}";
        }

        public readonly string restMessage;
        public readonly (int, int) sourcePos;

        public LexerException(ILexer lexer, string message)
            : base(MakeMessage(lexer, message)) {
            this.sourcePos = lexer.GetCurrentSourcePos();
            this.restMessage = message;
        }
    }

    public class ParserException : Exception {
        public ParserException(string message) : base(message) {}
    }

    public class RuntimeException : Exception {
        public RuntimeException(string message) : base(message) {}
    }

    public class InvalidCallException : Exception {
        public Obj obj { get; }

        public InvalidCallException(Obj obj) {
            this.obj = obj;
        }
    }

    public class InvalidObjException : Exception {
        public InvalidObjException(Obj obj)
            : base($"Unknown type: {obj} at {obj.sourcePos.Item1}:{obj.sourcePos.Item2}") {
        }
    }

    public class NotListException : Exception {
        public Obj obj { get; }

        public NotListException(Obj obj) {
            this.obj = obj;
        }
    }

    public class SymbolNotFoundException : Exception {
        public SymbolNotFoundException(SymObj sym, Env env)
            : base ($"Symbol '{sym}' not found") {}
    }

    public class ArgumentNumberMismatchException : Exception {
        public ArgumentNumberMismatchException(Obj vars, Obj vals)
            : base ($"Argument number mismatches. Expected {vars} but got {vals}") {}
    }

    public class PrimitiveRuntimeException : Exception {
        public PrimitiveRuntimeException(Obj p, string message)
            : base($"Error when running primitive function: {p}\nMessage: {message}") {}
    }
}