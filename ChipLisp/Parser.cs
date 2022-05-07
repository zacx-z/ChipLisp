using System;

namespace Nela.ChipLisp {
    public class Parser {
        public Obj ReadExpr(Lexer lexer) {
            switch (lexer.head) {
            case '\0':
                return null;
            case '(':
            case '[':
            case '{':
                return ReadList(lexer);
            case '\'':
                return ReadQuote(lexer);
            default:
                return lexer.ReadObj();
            }
        }

        private Obj ReadList(Lexer lexer) {
            var sourcePos = lexer.GetCurrentSourcePos();
            lexer.Next();
            Obj head = Obj.nil;
            while (true) {
                switch (lexer.head) {
                case ')':
                case ']':
                case '}':
                    {
                        lexer.Next();
                        var ret = VM.Reverse(head);
                        ret.sourcePos = sourcePos;
                        return ret;
                    }
                case '.':
                    if (!lexer.reader.PeekChar(out var ch) || !char.IsDigit(ch))
                    {
                        lexer.Next();
                        var last = ReadExpr(lexer);
                        lexer.Consume(')');
                        var ret = VM.Reverse(head);
                        (head as CellObj).cdr = last;
                        ret.sourcePos = sourcePos;
                        return ret;
                    }

                    break;
                }

                var obj = ReadExpr(lexer);
                if (obj == null)
                    throw new Exception("unclosed parenthesis");

                head = VM.Cons(obj, head);
            }
        }

        private Obj ReadQuote(Lexer lexer) {
            var sourcePos = lexer.GetCurrentSourcePos();
            lexer.Next();
            var sym = VM.Intern("quote");
            var o = VM.Cons(sym, VM.Cons(ReadExpr(lexer), Obj.nil));
            o.sourcePos = sourcePos;
            return o;
        }
    }
}