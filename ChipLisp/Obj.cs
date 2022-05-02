using System.IO;

namespace NelaSystem.ChipLisp {

    public abstract class Obj {
        public static NilObj nil = new NilObj();

        public (int, int) sourcePos = (-1, -1);

        public abstract void Print(TextWriter writer);

        public override string ToString() {
            var writer = new StringWriter();
            Print(writer);
            return writer.ToString();
        }
    }
    public delegate Obj PrimitiveFunc(VM vm, Env env, Obj args);

    public class NilObj : Obj {
        public override void Print(TextWriter writer) {
            writer.Write("()");
        }
    }
    
    public abstract class NativeObj : Obj {}

    public class NativeObj<T> : NativeObj {
        public T value;

        public NativeObj(T value) {
            this.value = value;
        }

        public override void Print(TextWriter writer) {
            writer.Write(value.ToString());
        }
    }

    public class CellObj : Obj {
        public Obj car;
        public Obj cdr;

        public CellObj(Obj car, Obj cdr) {
            this.car = car;
            this.cdr = cdr;
        }

        public override void Print(TextWriter writer) {
            writer.Write("(");
            var o = this;
            while (true) {
                o.car.Print(writer);
                if (o.cdr == nil)
                    break;
                if (o.cdr is CellObj cell) {
                    writer.Write(" ");
                    o = cell;
                }
                else {
                    writer.Write(" . ");
                    o.cdr.Print(writer);
                    break;
                }
            }
            writer.Write(")");
        }
    }

    public class SymObj : Obj {
        public string name;

        public override void Print(TextWriter writer) {
            writer.Write(name);
        }
    }

    public class PrimObj : Obj {
        public PrimitiveFunc func;

        public PrimObj(PrimitiveFunc func) {
            this.func = func;
        }

        public override void Print(TextWriter writer) {
            writer.Write($"<primitive {func}>");
        }
    }

    public class FuncObj : Obj {
        public Obj pmtrs;
        public Obj body;
        public Env env;

        public override void Print(TextWriter writer) {
            writer.Write("<function>");
        }
    }
    
    public class MacroObj : FuncObj {}

    public class TrueObj : Obj {
        public static TrueObj t = new TrueObj();
        private TrueObj() {}

        public override void Print(TextWriter writer) {
            writer.Write("t");
        }
    }

    public class DotObj : Obj {
        public static DotObj dot = new DotObj();
        private DotObj() {}

        public override void Print(TextWriter writer) {
            writer.Write(".");
        }
    }

    public class CparenObj : Obj {
        public static CparenObj cparen = new CparenObj();
        private CparenObj() {}
        public override void Print(TextWriter writer) {
            writer.Write(")");
        }
    }
}