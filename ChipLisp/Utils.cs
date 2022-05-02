using System.Collections;
using System.Collections.Generic;
using System.IO;

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