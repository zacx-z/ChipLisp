using System;
using System.Collections.Generic;
using System.IO;

namespace NelaSystem.ChipLisp {
    public class State {
        public delegate void LibLoader(State state);

        public VM vm { get; }
        public Env env { get; private set; }
        private Stack<Env> envStack = new Stack<Env>();

        public State(VM vm = null) {
            this.vm = vm ?? VM.vm;
            envStack.Push(env = new Env(null, null));
            LoadLib(CoreLib.Load);
        }

        public Obj Eval(string expr) => Eval(new StringReader(expr));
        public Obj Eval(Lexer lexer) => Eval(VM.vm.ReadExpr(lexer));
        public Obj Eval(TextReader reader) => Eval(VM.vm.ReadExpr(reader));
        public Obj Eval(Obj expr) => VM.vm.Eval(env, expr);

        public void PushEnv() {
            envStack.Push(env = new Env(null, env));
        }

        public void PushEnv(Env env) {
            envStack.Push(this.env = env);
        }

        public Env PopEnv() {
            if (envStack.Count > 1) {
                var ret = envStack.Pop();
                env = envStack.Peek();
                return ret;
            }

            throw new Exception("Already in the top env!");
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
            env.AddVariable(vm.Intern(sym), new PrimObj((vm, env, list) => func(vm, env, vm.EvalListExt(env, list)), func.Method.Name));
        }

        public void AddFunction(string sym, PrimitiveFunc func, string funcName) {
            env.AddVariable(vm.Intern(sym), new PrimObj((vm, env, list) => func(vm, env, vm.EvalListExt(env, list)), funcName));
        }

        public void AddPrimitive(string sym, PrimitiveFunc func) {
            env.AddVariable(vm.Intern(sym), new PrimObj(func));
        }
    }
}