using System;

namespace NelaSystem.ChipLisp {
    public class RuntimeException : Exception {
        public RuntimeException(string message) : base(message) {}
    }

    public class InvalidCallException : Exception {}

    public class NotListException : Exception {
        public Obj obj { get; private set; }

        public NotListException(Obj obj) {
            this.obj = obj;
        }
    }
}