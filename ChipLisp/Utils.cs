using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Nela.ChipLisp {
    public static class Extensions {
        public static int GetListLength(this Obj self) {
            int len = 0;
            var o = self;
            while (o is CellObj c) {
                len++;
                o = c.cdr;
            }

            return o == Obj.nil ? len : -1;
        }

        public static ListEnumerator GetListEnumerator(this Obj self) {
            return new ListEnumerator(self);
        }

        public static T Expect<T>(this VM vm, Obj obj) where T : Obj {
            if (obj is T o) {
                return o;
            }

            vm.Error($"Expected type of {Utils.GetObjTypeName<T>()} but got {obj}");
            return null;
        }

        public static T ExpectValue<T>(this VM vm, Obj obj) {
            if (obj is IValueObj<T> v) {
                return v.value;
            }

            vm.Error($"Expected type assignable to {typeof(T).Name} but got {obj}");
            return default(T);
        }

        public static Obj ExpectOr(this VM vm, Obj obj, params Expect.ConditionBranch[] group) {
            foreach (var g in group) {
                var ret = g.processor(obj);
                if (ret != null)
                    return ret;
            }
            
            vm.Error($"Expected type among {string.Join(", ", group.Select(g => Utils.GetObjTypeName(g.type)))} but got {obj}");
            return null;
        }

        public static void ExpectList0(this VM vm, Obj list) {
            var enumerator = list.GetListEnumerator();
            if (!enumerator.MoveNext()) {
                return;
            }

            vm.Error($"Expected list of 0 but got {list}");
        }

        public static Obj ExpectList1(this VM vm, Obj list) {
            var enumerator = list.GetListEnumerator();
            if (enumerator.GetNext(out var r) && !enumerator.MoveNext()) {
                return r;
            }
            vm.Error($"Expected list of 1 but got {list}");
            return null;
        }

        public static (Obj, Obj) ExpectList2(this VM vm, Obj list) {
            var enumerator = list.GetListEnumerator();
            if (enumerator.GetNext(out var r1)
             && enumerator.GetNext(out var r2)
             && !enumerator.MoveNext()) {
                return (r1, r2);
            }
            vm.Error($"Expected list of 2 but got {list}");
            return (null, null);
        }

        public static (Obj, Obj, Obj) ExpectList3(this VM vm, Obj list) {
            var enumerator = list.GetListEnumerator();
            if (enumerator.GetNext(out var r1)
             && enumerator.GetNext(out var r2)
             && enumerator.GetNext(out var r3)
             && !enumerator.MoveNext()) {
                return (r1, r2, r3);
            }
            vm.Error($"Expected list of 3 but got {list}");
            return (null, null, null);
        }

        public static (Obj, Obj, Obj, Obj) ExpectList4(this VM vm, Obj list) {
            var enumerator = list.GetListEnumerator();
            if (enumerator.GetNext(out var r1)
             && enumerator.GetNext(out var r2)
             && enumerator.GetNext(out var r3)
             && enumerator.GetNext(out var r4)
             && !enumerator.MoveNext()) {
                return (r1, r2, r3, r4);
            }
            vm.Error($"Expected list of 4 but got {list}");
            return (null, null, null, null);
        }

        public static (Obj, Obj, Obj, Obj, Obj) ExpectList5(this VM vm, Obj list) {
            var enumerator = list.GetListEnumerator();
            if (enumerator.GetNext(out var r1)
             && enumerator.GetNext(out var r2)
             && enumerator.GetNext(out var r3)
             && enumerator.GetNext(out var r4)
             && enumerator.GetNext(out var r5)
             && !enumerator.MoveNext()) {
                return (r1, r2, r3, r4, r5);
            }
            vm.Error($"Expected list of 5 but got {list}");
            return (null, null, null, null, null);
        }

        public static void AddCSharpFunction(this State state, string sym, Action func) {
            state.AddFunction(sym, (vm, env, args) => {
                vm.ExpectList0(args);
                func();
                return Obj.nil;
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<TR>(this State state, string sym, Func<TR> func) {
            state.AddFunction(sym, (vm, env, args) => {
                vm.ExpectList0(args);
                return new ValueObj<TR>(func());
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<T1>(this State state, string sym, Action<T1> func) {
            state.AddFunction(sym, (vm, env, args) => {
                func(vm.ExpectValue<T1>(vm.ExpectList1(args)));
                return Obj.nil;
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<T1, TR>(this State state, string sym, Func<T1, TR> func) {
            state.AddFunction(sym, (vm, env, args) => new ValueObj<TR>(
                func(vm.ExpectValue<T1>(vm.ExpectList1(args)))
            ), func.Method.Name);
        }

        public static void AddCSharpFunction<T1, T2>(this State state, string sym, Action<T1, T2> func) {
            state.AddFunction(sym, (vm, env, args) => {
                var (a1, a2) = vm.ExpectList2(args);
                func(vm.ExpectValue<T1>(a1), vm.ExpectValue<T2>(a2));
                return Obj.nil;
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<T1, T2, TR>(this State state, string sym, Func<T1, T2, TR> func) {
            state.AddFunction(sym, (vm, env, args) => {
                var (a1, a2) = vm.ExpectList2(args);
                return new ValueObj<TR>(func(vm.ExpectValue<T1>(a1), vm.ExpectValue<T2>(a2)));
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<T1, T2, T3>(this State state, string sym, Action<T1, T2, T3> func) {
            state.AddFunction(sym, (vm, env, args) => {
                var (a1, a2, a3) = vm.ExpectList3(args);
                func(vm.ExpectValue<T1>(a1), vm.ExpectValue<T2>(a2), vm.ExpectValue<T3>(a3));
                return Obj.nil;
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<T1, T2, T3, TR>(this State state, string sym, Func<T1, T2, T3, TR> func) {
            state.AddFunction(sym, (vm, env, args) => {
                var (a1, a2, a3) = vm.ExpectList3(args);
                return new ValueObj<TR>(func(vm.ExpectValue<T1>(a1), vm.ExpectValue<T2>(a2), vm.ExpectValue<T3>(a3)));
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<T1, T2, T3, T4>(this State state, string sym, Action<T1, T2, T3, T4> func) {
            state.AddFunction(sym, (vm, env, args) => {
                var (a1, a2, a3, a4) = vm.ExpectList4(args);
                func(vm.ExpectValue<T1>(a1), vm.ExpectValue<T2>(a2), vm.ExpectValue<T3>(a3), vm.ExpectValue<T4>(a4));
                return Obj.nil;
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<T1, T2, T3, T4, TR>(this State state, string sym, Func<T1, T2, T3, T4, TR> func) {
            state.AddFunction(sym, (vm, env, args) => {
                var (a1, a2, a3, a4) = vm.ExpectList4(args);
                return new ValueObj<TR>(func(vm.ExpectValue<T1>(a1), vm.ExpectValue<T2>(a2), vm.ExpectValue<T3>(a3), vm.ExpectValue<T4>(a4)));
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<T1, T2, T3, T4, T5>(this State state, string sym, Action<T1, T2, T3, T4, T5> func) {
            state.AddFunction(sym, (vm, env, args) => {
                var (a1, a2, a3, a4, a5) = vm.ExpectList5(args);
                func(vm.ExpectValue<T1>(a1), vm.ExpectValue<T2>(a2), vm.ExpectValue<T3>(a3), vm.ExpectValue<T4>(a4), vm.ExpectValue<T5>(a5));
                return Obj.nil;
            }, func.Method.Name);
        }

        public static void AddCSharpFunction<T1, T2, T3, T4, T5, TR>(this State state, string sym, Func<T1, T2, T3, T4, T5, TR> func) {
            state.AddFunction(sym, (vm, env, args) => {
                var (a1, a2, a3, a4, a5) = vm.ExpectList5(args);
                return new ValueObj<TR>(func(vm.ExpectValue<T1>(a1), vm.ExpectValue<T2>(a2), vm.ExpectValue<T3>(a3), vm.ExpectValue<T4>(a4), vm.ExpectValue<T5>(a5)));
            }, func.Method.Name);
        }

        public static void Consume(this ILexer lexer, char c) {
            var head = lexer.head;
            if (c != head)
                throw new LexerException(lexer, $"Expected {c} but got {head}(ANSI {(int)head})");
            lexer.Next();
        }

        public static bool ReadChar(this TextReader reader, out char c) {
            int b = reader.Read();
            if (b == -1) {
                c = (char)0;
                return false;
            }
            c = (char) b;
            return true;
        }

        public static bool PeekChar(this TextReader reader, out char c) {
            int b = reader.Peek();
            if (b == -1) {
                c = (char)0;
                return false;
            }
            c = (char) b;
            return true;
        }
    }

    public static class Utils {
        public static string GetObjTypeName<T>() where T : Obj {
            return GetObjTypeName(typeof(T));
        }

        public static string GetObjTypeName(Type t) {
            return typeof(ValueObj).IsAssignableFrom(t) ? t.GenericTypeArguments[0].Name : t.Name;
        }
    }

    public static class Expect {
        public struct ConditionBranch {
            public delegate Obj ExpectProcessor(Obj obj);
            public Type type;
            public ExpectProcessor processor;
        }
        public static ConditionBranch On<T>(Func<T, Obj> callback) where T : Obj {
            return new ConditionBranch() {
                type = typeof(T),
                processor = obj => {
                    if (obj is T o) {
                        return callback(o);
                    }

                    return null;
                }
            };
        }
    }

    public class ListEnumerator : IEnumerator<Obj> {
        private Obj o;
        private Obj cur;

        public ListEnumerator(Obj list) {
            o = cur = list;
        }

        public bool GetNext(out Obj val) {
            if (cur == Obj.nil) {
                val = cur;
                return false;
            }

            if (cur is CellObj c) {
                val = c.car;
                cur = c.cdr;
                return true;
            }

            throw new NotListException(o);
        }

#region implements IEnumerator<Obj>
        object IEnumerator.Current => Current;
        public Obj Current { get; private set; }

        public bool MoveNext() {
            var ret = GetNext(out var c);
            Current = c;
            return ret;
        }

        public void Reset() {
            cur = o;
        }

        public void Dispose() {}
#endregion
    }
}