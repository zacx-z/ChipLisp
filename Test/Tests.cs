using System.IO;
using Xunit;
using Nela.ChipLisp;
using Nela.ChipLisp.LangExtensions;

namespace Test {
    public class Tests {
        [Fact]
        public void TestAdd() {
            var state = new State();
            state.LoadPreludeLib();
            Assert.Equal(state.Eval("(+ 1 2)"), ValueObj.Create(3));
        }

        [Fact]
        public void TestRecursion() {
            var state = new State();
            state.LoadPreludeLib();
            Assert.Equal(state.Eval("(progn (defun sum (a . b) (if b (+ a (sum . b)) a)) (sum 1 2 3 4))"), ValueObj.Create(10));
        }

        [Fact]
        public void TestExtensions() {
            var state = new State();
            state.LoadPreludeLib();
            var parser = new ExtendedParser();
            Assert.Equal(state.Eval(parser.ReadExpr(new Lexer(new StringReader(
@"(progn
(defmacro plus-all args
    (if (cdr args)
        `(+, (car args), (eval `(macroexpand, (cons 'plus-all (cdr args)))))
        (car args))
)
(eval (plus-all 1 2 3 4))
)")))), ValueObj.Create(10));
        }
    }
}