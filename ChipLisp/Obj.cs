using System;
using System.IO;

namespace Nela.ChipLisp {

    public abstract class Obj {
        public static NilObj nil = new NilObj();

        public (int, int) sourcePos = (-1, -1);

        public virtual Obj OnEval(VM vm, Env env) {
            throw new InvalidObjException(this);
        }

        public abstract void Print(TextWriter writer);

        public override string ToString() {
            var writer = new StringWriter();
            Print(writer);
            return writer.ToString();
        }
    }

    public class NilObj : Obj {
        public override Obj OnEval(VM vm, Env env) {
            return this;
        }

        public override void Print(TextWriter writer) {
            writer.Write("()");
        }
    }

    public abstract class ValueObj : Obj {
        public override Obj OnEval(VM vm, Env env) {
            return this;
        }
    }

    public interface IValueObj<out T> {
        T value { get; }
    }

    public class ValueObj<T> : ValueObj, IValueObj<T> {
        T IValueObj<T>.value => value;
        public T value;

        public ValueObj(T value) {
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

        public override Obj OnEval(VM vm, Env env) {
            if (vm.MacroExpand(env, this, out var expanded)) {
                return vm.Eval(env, expanded);
            }

            var fn = vm.Eval(env, this.car);
            var args = this.cdr;

            try {
                return vm.Apply(env, fn, args);
            }
            catch (InvalidCallException) {
                throw new Exception($"Invalid Call: Expected a function for {this.car} but got {fn}");
            }
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

        public override Obj OnEval(VM vm, Env env) {
            return env.Find(this);
        }

        public override void Print(TextWriter writer) {
            writer.Write(name);
        }
    }

    public class PrimObj : Obj {
        public PrimitiveFunc func;
        private string name;

        public PrimObj(PrimitiveFunc func, string funcName = null) {
            this.func = func;
            this.name = funcName ?? func.Method.Name;
        }

        public override Obj OnEval(VM vm, Env env) {
            return this;
        }

        public override void Print(TextWriter writer) {
            writer.Write($"<primitive {name}>");
        }
    }

    public class FuncObj : Obj {
        public Obj pmtrs;
        public Obj body;
        public Env env;

        public override Obj OnEval(VM vm, Env env) {
            return this;
        }

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

    public delegate Obj PrimitiveFunc(VM vm, Env env, Obj args);

    // for tail call optimization
    internal class TailCallObj : Obj {
        public Obj evalTarget;
        public Env targetEnv;

        public TailCallObj(Obj evalTarget, Env targetEnv) {
            this.evalTarget = evalTarget;
            this.targetEnv = targetEnv;
        }

        public override Obj OnEval(VM vm, Env env) {
            return vm.EvalToTailCall(targetEnv, evalTarget);
        }

        public override void Print(TextWriter writer) {
            writer.Write("<tail call:");
            evalTarget.Print(writer);
            writer.Write(">");
        }
    }
}