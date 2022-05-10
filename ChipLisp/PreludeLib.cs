namespace Nela.ChipLisp {
    public class PreludeLib {
        public static void Load(State state) {
            state.AddFunction("cons", Prim_Cons);
            state.AddFunction("car", Prim_Car);
            state.AddFunction("cdr", Prim_Cdr);
            state.AddPrimitive("list", Prim_List);
            state.AddFunction("+", Prim_Plus);
            state.AddFunction("-", Prim_Minus);
            state.AddFunction("*", Prim_Multiply);
            state.AddFunction("/", Prim_Divide);
            state.AddFunction("to-i", Prim_ToInt);
            state.AddFunction("to-f", Prim_ToFloat);
            state.AddFunction("<", Prim_Lt);
            state.AddFunction("=", Prim_NumEq);
            state.AddFunction("eq", Prim_Eq);
            state.AddFunction("eval", Prim_Eval);
            state.AddPrimitive("define", Prim_Define);
            state.AddPrimitive("defun", Prim_Defun);
            state.AddPrimitive("defmacro", Prim_Defmacro);
            state.AddPrimitive("lambda", Prim_Lambda);
            state.AddPrimitive("macroexpand", Prim_MacroExpand);
            state.AddPrimitive("progn", Prim_Progn);
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

        // (defun list args args)
        private static Obj Prim_List(VM vm, Env env, Obj list) {
            var args = vm.EvalListExt(env, list);
            return args;
        }

        private static Obj Prim_Plus(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();
            if (!enumerator.GetNext(out var first)) {
                return vm.Error("no arguments");
            }

            if (first is ValueObj<int> it) {
                int sum = it.value;

                while (enumerator.GetNext(out var arg)) {
                    sum += vm.Expect<ValueObj<int>>(arg).value;
                }

                return new ValueObj<int>(sum);
            }

            if (first is ValueObj<float> fl) {
                float sum = fl.value;

                while (enumerator.GetNext(out var arg)) {
                    sum += vm.Expect<ValueObj<float>>(arg).value;
                }

                return new ValueObj<float>(sum);
            }

            return vm.Error("+ takes only numbers");
        }

        private static Obj Prim_Minus(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();
            if (!enumerator.GetNext(out var first)) {
                return vm.Error("no arguments");
            }

            if (first is ValueObj<int> it) {
                int ret = it.value;
                int l = 1;

                while (enumerator.GetNext(out var arg)) {
                    ret -= vm.Expect<ValueObj<int>>(arg).value;
                    l++;
                }

                return new ValueObj<int>(l == 1 ? -ret : ret);
            }

            if (first is ValueObj<float> fl) {
                float ret = fl.value;
                int l = 1;

                while (enumerator.GetNext(out var arg)) {
                    ret -= vm.Expect<ValueObj<float>>(arg).value;
                    l++;
                }

                return new ValueObj<float>(l == 1 ? -ret : ret);
            }

            return vm.Error("- takes only numbers");
        }

        private static Obj Prim_Multiply(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();
            if (!enumerator.GetNext(out var first)) {
                return vm.Error("no arguments");
            }

            if (first is ValueObj<int> it) {
                int product = it.value;

                while (enumerator.GetNext(out var arg)) {
                    product *= vm.Expect<ValueObj<int>>(arg).value;
                }

                return new ValueObj<int>(product);
            }

            if (first is ValueObj<float> fl) {
                float product = fl.value;

                while (enumerator.GetNext(out var arg)) {
                    product *= vm.Expect<ValueObj<float>>(arg).value;
                }

                return new ValueObj<float>(product);
            }

            return vm.Error("* takes only numbers");
        }

        private static Obj Prim_Divide(VM vm, Env env, Obj args) {
            var (lhs, rhs) = vm.ExpectList2(args);
            return vm.ExpectOr(lhs
                , Expect.On<ValueObj<float>, Obj>(f => new ValueObj<float>(f.value / vm.Expect<ValueObj<float>>(rhs).value))
                , Expect.On<ValueObj<int>, Obj>(f => new ValueObj<int>(f.value / vm.Expect<ValueObj<int>>(rhs).value))
                );
        }

        private static Obj Prim_ToInt(VM vm, Env env, Obj args) {
            var f = vm.Expect<ValueObj<float>>(vm.ExpectList1(args));
            return new ValueObj<int>((int) f.value);
        }

        private static Obj Prim_ToFloat(VM vm, Env env, Obj args) {
            var i = vm.Expect<ValueObj<int>>(vm.ExpectList1(args));
            return new ValueObj<float>(i.value);
        }

        private static Obj Prim_Lt(VM vm, Env env, Obj args) {
            var (lhs, rhs) = vm.ExpectList2(args);
            if (lhs is ValueObj<int> lhsInt) {
                return lhsInt.value < vm.Expect<ValueObj<int>>(rhs).value ? (Obj)TrueObj.t : Obj.nil;
            }

            if (lhs is ValueObj<float> lhsFloat) {
                return lhsFloat.value < vm.Expect<ValueObj<float>>(rhs).value ? (Obj) TrueObj.t : Obj.nil;
            }

            return vm.Error("< takes only numbers");
        }

        // only on integers
        private static Obj Prim_NumEq(VM vm, Env env, Obj args) {
            var (lhs, rhs) = vm.ExpectList2(args);
            return vm.Expect<ValueObj<int>>(lhs).value == vm.Expect<ValueObj<int>>(rhs).value ? (Obj)TrueObj.t : Obj.nil;
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
                return vm.Error("malformed define");
            }

            var value = vm.Eval(env, valueCell.car);
            env.AddVariable(sym, value);
            return value;
        }

        private static Obj Prim_Defun(VM vm, Env env, Obj list) {
            if (!(list is CellObj cell && cell.car is SymObj sym)) {
                return vm.Error("malformed defun");
            }

            var fn = HandleFunction<FuncObj>(vm, env, cell.cdr);
            env.AddVariable(sym, fn);
            return fn;
        }

        private static Obj Prim_Defmacro(VM vm, Env env, Obj list) {
            if (!(list is CellObj cell && cell.car is SymObj sym)) {
                return vm.Error("malformed defmacro");
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
            return vm.Error("malformed macroexpand");
        }

        private static Obj Prim_Progn(VM vm, Env env, Obj list) {
            return vm.Progn(env, list);
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
                return vm.Error("malformed lambda");
            }

            Obj p = cell.car;
            for (; p is CellObj c; p = c.cdr) {
                if (!(c.car is SymObj))
                    return vm.Error("parameter must be a symbol");
            }

            if (p != Obj.nil && !(p is SymObj)) {
                return vm.Error("parameter must be a symbol");
            }

            return new T() {
                pmtrs = cell.car,
                body = cell.cdr,
                env = env
            };
        }
    }
}