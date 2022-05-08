using System.IO;

namespace Nela.ChipLisp.LangExtensions {
    public class BackQuoteObj : Obj {
        public Obj inner;

        public BackQuoteObj(Obj inner) {
            this.inner = inner;
        }

        public override Obj OnEval(VM vm, Env env) {
            if (inner is CellObj list) {
                Obj head = Obj.nil;
                Obj lp = list;

                for (; lp is CellObj cell;lp = cell.cdr) {
                    var expr = cell.car;
                    var res = expr;
                    if (expr is CommaObj) {
                        res = vm.Eval(env, expr);
                    }
                    head = VM.Cons(res, head);
                }

                if (lp != Obj.nil) throw new NotListException(list);

                return VM.Reverse(head);
            }
            return inner;
        }

        public override void Print(TextWriter writer) {
            writer.Write("`");
            inner.Print(writer);
        }
    }

    public class CommaObj : Obj {
        public Obj inner;

        public CommaObj(Obj inner) {
            this.inner = inner;
        }

        public override Obj OnEval(VM vm, Env env) {
            return inner.OnEval(vm, env);
        }

        public override void Print(TextWriter writer) {
            writer.Write(",");
            inner.Print(writer);
        }
    }
}
