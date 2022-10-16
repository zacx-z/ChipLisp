using System;
using System.IO;

namespace Nela.ChipLisp {

    public abstract class Obj {
        public static NilObj nil = new NilObj();

        public (int, int) sourcePos = (-1, -1);

        public virtual Obj OnEval(VM vm, Env env) {
            throw new InvalidObjException(this);
        }

        public virtual int GetValueHash() {
            return GetHashCode();
        }

        public virtual bool OnEql(Obj other) {
            return this.Equals(other); // default implementation
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

        public override int GetValueHash() {
            return 0;
        }

        public override void Print(TextWriter writer) {
            writer.Write("()");
        }
    }

    public interface IValueObj {
        object untypedValue { get; }
    }

    public abstract class ValueObj : Obj, IValueObj {
        public abstract Type type { get; }
        public abstract object untypedValue { get; }

        public override Obj OnEval(VM vm, Env env) {
            return this;
        }

        public static ValueObj<T> Create<T>(T val) {
            return new ValueObj<T>(val);
        }
    }

    public interface IValueObj<out T> : IValueObj {
        T value { get; }
    }

    public class ValueObj<T> : ValueObj, IValueObj<T> {
        public override Type type => typeof(T);
        public override object untypedValue => value;
        T IValueObj<T>.value => value;
        public T value;

        public ValueObj(T value) {
            this.value = value;
        }

        public override int GetValueHash() {
            return value.GetHashCode();
        }

        public override bool OnEql(Obj other) {
            if (other is IValueObj<T> o) {
                return value.Equals(o.value);
            }

            return false;
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

        public override int GetValueHash() {
            return car.GetValueHash() ^ cdr.GetValueHash();
        }

        public override bool OnEql(Obj other) {
            if (other is CellObj o) {
                return car.OnEql(o.car) && cdr.OnEql(o.cdr);
            }

            return false;
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

        public override int GetValueHash() {
            return env.GetHashCode() ^ pmtrs.GetValueHash() ^ body.GetValueHash();
        }

        public override bool OnEql(Obj other) {
            if (other is FuncObj o) {
                return env == o.env && pmtrs.OnEql(o.pmtrs) && body.OnEql(o.body);
            }

            return false;
        }

        public override void Print(TextWriter writer) {
            writer.Write("(lambda ");
            pmtrs.Print(writer);
            var enumerator = body.GetListEnumerator();
            while (enumerator.GetNext(out var p)) {
                writer.Write("\n\t");
                p.Print(writer);
            }
            writer.Write("\n)");
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

        public Obj Execute(VM vm) {
            return vm.EvalToTailCall(targetEnv, evalTarget);
        }

        public override void Print(TextWriter writer) {
            writer.Write("<tail call:");
            evalTarget.Print(writer);
            writer.Write(">");
        }
    }
}