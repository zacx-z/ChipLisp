using System;

namespace NelaSystem.ChipLisp {
    public class PreludeLib {
        public static void Load(State state) {
            state.AddFunction("cons", Prim_Cons);
            state.AddFunction("car", Prim_Car);
            state.AddFunction("cdr", Prim_Cdr);
            state.AddFunction("+", Prim_Plus);
            state.AddFunction("-", Prim_Minus);
            state.AddFunction("<", Prim_Lt);
            state.AddFunction("=", Prim_NumEq);
            state.AddFunction("eq", Prim_Eq);
            state.AddFunction("eval", Prim_Eval);
            state.AddPrimitive("define", Prim_Define);
            state.AddPrimitive("defun", Prim_Defun);
            state.AddPrimitive("defmacro", Prim_Defmacro);
            state.AddPrimitive("lambda", Prim_Lambda);
            state.AddPrimitive("macroexpand", Prim_MacroExpand);
            state.AddPrimitive("if", Prim_If);
            state.AddPrimitive("while", Prim_While);
            state.AddPrimitive("let", Prim_Let);
        }

        private static Obj Prim_Cons(VM vm, Env env, Obj args) {
            var (_, rhs) = vm.ExpectList2(args);
            (args as CellObj).cdr = rhs;
            return args;
        }

        private static Obj Prim_Car(VM vm, Env env, Obj args) {
            return vm.Expect<CellObj>(vm.ExpectList1(args)).car;
        }

        private static Obj Prim_Cdr(VM vm, Env env, Obj args) {
            return vm.Expect<CellObj>(vm.ExpectList1(args)).cdr;
        }

        private static Obj Prim_Plus(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();
            if (!enumerator.GetNext(out var first)) {
                vm.Error("no arguments");
                return null;
            }

            if (first is NativeObj<int> it) {
                int sum = it.value;

                while (enumerator.GetNext(out var arg)) {
                    sum += vm.Expect<NativeObj<int>>(arg).value;
                }

                return new NativeObj<int>(sum);
            }

            if (first is NativeObj<float> fl) {
                float sum = fl.value;

                while (enumerator.GetNext(out var arg)) {
                    sum += vm.Expect<NativeObj<float>>(arg).value;
                }

                return new NativeObj<float>(sum);
            }

            vm.Error("+ takes only numbers");
            return null;
        }

        private static Obj Prim_Minus(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();
            if (!enumerator.GetNext(out var first)) {
                vm.Error("no arguments");
                return null;
            }

            if (first is NativeObj<int> it) {
                int ret = it.value;
                int l = 1;

                while (enumerator.GetNext(out var arg)) {
                    ret -= vm.Expect<NativeObj<int>>(arg).value;
                    l++;
                }

                return new NativeObj<int>(l == 1 ? -ret : ret);
            }

            if (first is NativeObj<float> fl) {
                float ret = fl.value;
                int l = 1;

                while (enumerator.GetNext(out var arg)) {
                    ret -= vm.Expect<NativeObj<float>>(arg).value;
                    l++;
                }

                return new NativeObj<float>(l == 1 ? -ret : ret);
            }

            vm.Error("- takes only numbers");
            return null;
        }

        private static Obj Prim_Lt(VM vm, Env env, Obj args) {
            var (lhs, rhs) = vm.ExpectList2(args);
            if (lhs is NativeObj<int> lhsInt) {
                return lhsInt.value < vm.Expect<NativeObj<int>>(rhs).value ? (Obj)TrueObj.t : Obj.nil;
            }

            if (lhs is NativeObj<float> lhsFloat) {
                return lhsFloat.value < vm.Expect<NativeObj<float>>(rhs).value ? (Obj) TrueObj.t : Obj.nil;
            }

            vm.Error("< takes only numbers");
            return null;
        }

        private static Obj Prim_NumEq(VM vm, Env env, Obj args) {
            var (lhs, rhs) = vm.ExpectList2(args);
            return vm.Expect<NativeObj<int>>(lhs).value == vm.Expect<NativeObj<int>>(rhs).value ? (Obj)TrueObj.t : Obj.nil;
        }

        private static Obj Prim_Eq(VM vm, Env env, Obj args) {
            var (lhs, rhs) = vm.ExpectList2(args);
            return lhs == rhs ? (Obj)TrueObj.t : Obj.nil;
        }

        private static Obj Prim_Eval(VM vm, Env env, Obj args) {
            var arg = vm.ExpectList1(args);
            return vm.Eval(env, arg);
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

        private static Obj Prim_MacroExpand(VM vm, Env env, Obj list) {
            var enumerator = list.GetListEnumerator();
            if (enumerator.GetNext(out var body) && !enumerator.MoveNext()) {
                vm.MacroExpand(env, body, out var ret);
                return ret;
            }
            vm.Error("malformed macroexpand");
            return null;
        }

        private static Obj Prim_If(VM vm, Env env, Obj list) {
            var cell = vm.Expect<CellObj>(list);
            var cond = cell.car;
            var thenGroup = vm.Expect<CellObj>(cell.cdr);
            cond = vm.Eval(env, cond);
            if (cond != Obj.nil) {
                return vm.Eval(env, thenGroup.car);
            }

            var els = thenGroup.cdr;
            return els == Obj.nil ? Obj.nil : vm.Progn(env, els);
        }

        private static Obj Prim_While(VM vm, Env env, Obj list) {
            var cell = vm.Expect<CellObj>(list);
            var cond = cell.car;
            var body = vm.Expect<CellObj>(cell.cdr);
            while (vm.Eval(env, cond) != Obj.nil) {
                vm.EvalList(env, body);
            }
            return Obj.nil;
        }

        // (let ([x 10]) (+ x 10))
        private static Obj Prim_Let(VM vm, Env env, Obj list) {
            var cell = vm.Expect<CellObj>(list);
            var newEnv = new Env(null, env);
            var valExprs = cell.car;
            var valEnumerator = valExprs.GetListEnumerator();
            while (valEnumerator.GetNext(out var valExpr)) {
                var (symObj, val) = vm.ExpectList2(valExpr);
                var sym = vm.Expect<SymObj>(symObj);
                newEnv.AddVariable(sym, vm.Eval(newEnv, val));
            }

            var body = vm.Expect<CellObj>(cell.cdr);
            return vm.Progn(newEnv, body);
        }

        private static Obj HandleFunction<T>(VM vm, Env env, Obj list) where T : FuncObj, new() {
            if (!(list is CellObj cell)) {
                vm.Error("malformed lambda");
                return null;
            }

            Obj p = cell.car;
            for (; p is CellObj c; p = c.cdr) {
                if (!(c.car is SymObj))
                    vm.Error("parameter must be a symbol");
            }

            if (p != Obj.nil && !(p is SymObj)) {
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