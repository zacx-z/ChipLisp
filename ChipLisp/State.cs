using System;
using System.IO;

namespace NelaSystem.ChipLisp {
    public class State {
        public delegate void LibLoader(State state);

        public VM vm { get; private set; }
        private Env env;

        public State(VM vm = null) {
            this.vm = vm ?? VM.vm;
            env = new Env(null, null);
            LoadLib(CoreLib.Load);
        }

        public Obj Eval(string expr) => Eval(new StringReader(expr));
        public Obj Eval(TextReader reader) => Eval(VM.vm.ReadExpr(reader));
        public Obj Eval(Obj expr) => VM.vm.Eval(env, expr);

        public void PushEnv() {
            env = new Env(null, env);
        }

        public void PopEnv() {
            if (env.up != null) {
                env = env.up;
            }
            else {
                throw new Exception("Already in the top env!");
            }
        }

        public void LoadPreludeLib() {
            LoadLib(PreludeLib.Load);
        }

        public void LoadLib(LibLoader libLoader) {
            libLoader(this);
        }

        public void AddVariable(string sym, Obj val) {
            env.AddVariable(vm.Intern(sym), val);
        }

        public void AddFunction(string sym, PrimitiveFunc func) {
            env.AddVariable(vm.Intern(sym), new PrimObj((vm, env, list) => func(vm, env, vm.EvalList(env, list))));
        }

        public void AddMacro(string sym, PrimitiveFunc func) {
            env.AddVariable(vm.Intern(sym), new PrimObj(func));
        }
    }
}