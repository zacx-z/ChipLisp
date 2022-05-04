using System;
using System.IO;
using System.Collections.Generic;

namespace Nela.ChipLisp {
    public class VM {
        public static VM vm = new VM();

        private Dictionary<string, SymObj> symbols = new Dictionary<string, SymObj>();

        public SymObj Intern(string name) {
            if (symbols.TryGetValue(name, out var ret)) {
                return ret;
            }
            ret = new SymObj() { name = name };
            symbols.Add(name, ret);
            return ret;
        }

        public Obj Eval(Env env, Obj obj) {
            if (obj == null) return null;

            try {
                return obj.OnEval(this, env);
            }
            catch (Exception e) {
                throw new InterpreterException(obj, e);
            }
        }

        public bool MacroExpand(Env env, Obj obj, out Obj expanded) {
            if (!(obj is CellObj cell && cell.car is SymObj sym)) {
                expanded = obj;
                return false;
            }

            var bind = env.Find(sym);
            if (bind == null || !(bind.cdr is MacroObj macro)) {
                expanded = Obj.nil;
                return false;
            }

            var args = cell.cdr;
            expanded = ApplyFunc(env, macro, args);
            return true;
        }

        public Obj ReadExpr(TextReader reader) => ReadExpr(new Lexer(this, reader));

        public Obj ReadExpr(Lexer lexer) {
            while (true) {
                if (lexer.isEnd) return null;
                switch (lexer.head) {
                case ' ':
                case '\r':
                case '\t':
                    lexer.NextChar();
                    break;
                case '\n':
                    lexer.NextChar();
                    lexer.OnNextLine();
                    break;
                case ';':
                    lexer.SkipLine();
                    break;
                case '(':
                case '[':
                case '{':
                    return ReadList(lexer);
                case ')':
                case ']':
                case '}':
                    return lexer.ReadAs(CparenObj.cparen);
                case '.':
                    if (!lexer.reader.PeekChar(out var ch) || !char.IsDigit(ch))
                        return lexer.ReadAs(DotObj.dot);
                    goto default;
                case '\'':
                    return ReadQuote(lexer);
                default:
                    return lexer.ReadObj();
                }
            }
        }

        public Obj Apply(Env env, Obj fn, Obj args) {
            if (fn is PrimObj prim) {
                try {
                    return prim.func(this, env, args);
                }
                catch (NotListException e) {
                    if (e.obj == args) {
                        throw new ArgumentException($"arguments must be a list but got {args}");
                    }

                    throw;
                }
                catch (RuntimeException e) {
                    throw new PrimitiveRuntimeException(fn, $"{e.Message}\n{e.StackTrace}");
                }
            }

            if (fn is FuncObj func) {
                Obj argValues;
                try {
                    argValues = EvalListExt(env, args);
                }
                catch (NotListException) {
                    throw new ArgumentException($"arguments must be a list but got {args}");
                }
                return ApplyFunc(env, func, argValues);
            }

            throw new InvalidCallException(fn);
        }

        public Obj ApplyFunc(Env obj, FuncObj fn, Obj args) {
            var pmtrs = fn.pmtrs;
            var env = PushEnv(fn.env, pmtrs, args);
            var body = fn.body;
            return Progn(env, body);
        }

        public Obj EvalList(Env env, Obj list) {
            Obj head = Obj.nil;
            var lp = list;
            for (; lp is CellObj cell;lp = cell.cdr) {
                var expr = cell.car;
                var res = Eval(env, expr);
                head = Cons(res, head);
            }

            if (lp != Obj.nil) throw new NotListException(list);

            return Reverse(head);
        }

        /// list could be in the form of (a . b). It will evaluates b and expand it, and repeat until getting a list.
        /// useful for applying rest parameters, e.g. (defun f (a . b) (if b (+ a (f . b)) a))
        public Obj EvalListExt(Env env, Obj list) {
            Obj head = Obj.nil;
            var lp = list;
            while (lp is CellObj || (lp = Eval(env, lp)) is CellObj) {
                var cell = lp as CellObj;
                var expr = cell.car;
                var res = Eval(env, expr);
                head = Cons(res, head);
                lp = cell.cdr;
            }

            if (lp != Obj.nil) throw new NotListException(list);

            return Reverse(head);
        }

        public Obj Progn(Env env, Obj list) {
            Obj r = Obj.nil;

            var lp = list;
            for (; lp is CellObj cell;lp = cell.cdr) {
                r = cell.car;
                r = Eval(env, r);
            }

            if (lp != Obj.nil) throw new NotListException(list);
            return r;
        }

        public Env PushEnv(Env env, Obj vars, Obj vals) {
            var map = new List<CellObj>();
            for (; vars is CellObj cvars && vals is CellObj cvals; vars = cvars.cdr, vals = cvals.cdr) {
                var sym = cvars.car;
                var val = cvals.car;
                map.Add(new CellObj(sym, val));
            }

            if (vars is CellObj) {
                throw new ArgumentNumberMismatchException(vars, vals);
            }

            if (vars != Obj.nil) {
                map.Add(new CellObj(vars, vals));
            }

            return new Env(map, env);
        }

        // Destructively reverses the given list
        public Obj Reverse(Obj p) {
            Obj ret = Obj.nil;
            while (p != Obj.nil) {
                var head = p as CellObj;
                p = head.cdr;
                head.cdr = ret;
                ret = head;
            }

            return ret;
        }

        public CellObj Cons(Obj car, Obj cdr) {
            return new CellObj(car, cdr);
        }

        public Obj Error(string errorMessage) {
            throw new RuntimeException(errorMessage);
        }

        private Obj ReadList(Lexer lexer) {
            var sourcePos = lexer.GetCurrentSourcePos();
            lexer.NextChar();
            Obj head = Obj.nil;
            while (true) {
                var obj = ReadExpr(lexer);
                if (obj == null)
                    throw new Exception("unclosed parenthesis");
                if (obj == CparenObj.cparen) {
                    var ret = Reverse(head);
                    ret.sourcePos = sourcePos;
                    return ret;
                }

                if (obj == DotObj.dot) {
                    var last = ReadExpr(lexer);
                    if (ReadExpr(lexer) != CparenObj.cparen)
                        throw new Exception("Closed parenthesis expected after dot");
                    var ret = Reverse(head);
                    (head as CellObj).cdr = last;
                    ret.sourcePos = sourcePos;
                    return ret;
                }

                head = Cons(obj, head);
            }
        }

        private Obj ReadQuote(Lexer lexer) {
            var sourcePos = lexer.GetCurrentSourcePos();
            lexer.NextChar();
            var sym = Intern("quote");
            var o = Cons(sym, Cons(ReadExpr(lexer), Obj.nil));
            o.sourcePos = sourcePos;
            return o;
        }
    }
}