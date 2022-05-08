namespace Nela.ChipLisp.LangExtensions {
    public class ExtendedParser : Parser, IParser {
        public override Obj ReadExpr(ILexer lexer) {
            switch (lexer.head) {
            case '`':
                return ReadBackQuote(lexer);
            case ',':
                return ReadComma(lexer);
            }
            return base.ReadExpr(lexer);
        }

        private Obj ReadBackQuote(ILexer lexer) {
            lexer.Next();
            return new BackQuoteObj(ReadExpr(lexer));
        }

        private Obj ReadComma(ILexer lexer) {
            lexer.Next();
            return new CommaObj(ReadExpr(lexer));
        }
    }
}