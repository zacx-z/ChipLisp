using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NelaSystem.ChipLisp {
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

        public static Obj ExpectOr(this VM vm, Obj obj, params Expect.ConditionBranch[] group) {
            foreach (var g in group) {
                var ret = g.processor(obj);
                if (ret != null)
                    return ret;
            }
            
            vm.Error($"Expected type among {string.Join(", ", group.Select(g => Utils.GetObjTypeName(g.type)))} but got {obj}");
            return null;
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
            if (enumerator.GetNext(out var r1) && enumerator.GetNext(out var r2) && !enumerator.MoveNext()) {
                return (r1, r2);
            }
            vm.Error($"Expected list of 2 but got {list}");
            return (null, null);
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
            return typeof(NativeObj).IsAssignableFrom(t) ? t.GenericTypeArguments[0].Name : t.Name;
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