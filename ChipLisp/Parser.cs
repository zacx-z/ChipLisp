using System;

namespace Nela.ChipLisp {
    public interface IParser {
        Obj ReadExpr(ILexer lexer);
    }

    public class Parser : IParser {
        public virtual Obj ReadExpr(ILexer lexer) {
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
                return lexer.Read();
            }
        }

        private Obj ReadList(ILexer lexer) {
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
                    if (!lexer.PeekChar(out var ch) || !char.IsDigit(ch))
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
                    throw new ParserException("unclosed parenthesis");

                head = VM.Cons(obj, head);
            }
        }

        private Obj ReadQuote(ILexer lexer) {
            var sourcePos = lexer.GetCurrentSourcePos();
            lexer.Next();
            var sym = VM.Intern("quote");
            var o = VM.Cons(sym, VM.Cons(ReadExpr(lexer), Obj.nil));
            o.sourcePos = sourcePos;
            return o;
        }
    }
}