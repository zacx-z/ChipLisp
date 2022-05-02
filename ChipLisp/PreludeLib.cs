using System;

namespace NelaSystem.ChipLisp {
    public class PreludeLib {
        public static void Load(State state) {
            state.AddFunction("+", Prim_Plus);
            state.AddFunction("-", Prim_Minus);
            state.AddMacro("define", Prim_Define);
            state.AddMacro("defun", Prim_Defun);
            state.AddMacro("defmacro", Prim_Defmacro);
            state.AddMacro("lambda", Prim_Lambda);
        }

        private static Obj Prim_Plus(VM vm, Env env, Obj list) {
            var enumerator = list.GetListEnumerator();
            if (!enumerator.GetNext(out var first)) {
                vm.Error("no arguments");
                return null;
            }

            if (first is NativeObj<int> it) {
                int sum = it.value;

                while (enumerator.GetNext(out var arg)) {
                    if (arg is NativeObj<int> i) {
                        sum += i.value;
                    }
                    else {
                        vm.Error("+ takes only numbers");
                    }
                }

                return new NativeObj<int>(sum);
            }

            if (first is NativeObj<float> fl) {
                float sum = fl.value;

                while (enumerator.GetNext(out var arg)) {
                    if (arg is NativeObj<float> f) {
                        sum += f.value;
                    }
                    else {
                        vm.Error("+ takes only numbers");
                    }
                }

                return new NativeObj<float>(sum);
            }

            vm.Error("+ takes only numbers");
            return null;
        }

        private static Obj Prim_Minus(VM vm, Env env, Obj list) {
            var enumerator = list.GetListEnumerator();
            if (!enumerator.GetNext(out var first)) {
                vm.Error("no arguments");
                return null;
            }

            if (first is NativeObj<int> it) {
                int ret = it.value;
                int l = 1;

                while (enumerator.GetNext(out var arg)) {
                    if (arg is NativeObj<int> i) {
                        ret -= i.value;
                        l++;
                    }
                    else {
                        vm.Error("- takes only numbers");
                    }
                }

                return new NativeObj<int>(l == 1 ? -ret : ret);
            }

            if (first is NativeObj<float> fl) {
                float ret = fl.value;
                int l = 1;

                while (enumerator.GetNext(out var arg)) {
                    if (arg is NativeObj<float> f) {
                        ret -= f.value;
                        l++;
                    }
                    else {
                        vm.Error("- takes only numbers");
                    }
                }

                return new NativeObj<float>(l == 1 ? -ret : ret);
            }

            vm.Error("- takes only numbers");
            return null;
        }

        private static Obj Prim_Define(VM vm, Env env, Obj list) {
            if (!(list is CellObj cell && list.GetListLength() == 2 && cell.car is SymObj sym && cell.cdr is CellObj valueCell)) {
                vm.Error("malformed define");
                return null;
            }

            var value = vm.Eval(env, valueCell.car);
            env.AddVariable(sym, value);
            return value;
        }

        private static Obj Prim_Defun(VM vm, Env env, Obj list) {
            if (!(list is CellObj cell && cell.car is SymObj sym)) {
                vm.Error("malformed defun");
                return null;
            }

            var fn = HandleFunction<FuncObj>(vm, env, cell.cdr);
            env.AddVariable(sym, fn);
            return fn;
        }

        private static Obj Prim_Defmacro(VM vm, Env env, Obj list) {
            if (!(list is CellObj cell && cell.car is SymObj sym)) {
                vm.Error("malformed defmacro");
                return null;
            }

            var fn = HandleFunction<MacroObj>(vm, env, cell.cdr);
            env.AddVariable(sym, fn);
            return fn;
        }

        private static Obj Prim_Lambda(VM vm, Env env, Obj list) {
            return HandleFunction<FuncObj>(vm, env, list);
        }

        private static Obj HandleFunction<T>(VM vm, Env env, Obj list) where T : FuncObj, new() {
            if (!(list is CellObj cell)) {
                vm.Error("malformed lambda");
                return null;
            }

            var argEnum = cell.car.GetListEnumerator();
            while (argEnum.GetNext(out var arg)) {
                if (!(arg is SymObj))
                    vm.Error("parameter must be a symbol");
            }

            return new T() {
                pmtrs = cell.car,
                body = cell.cdr,
                env = env
            };
        }
    }
}