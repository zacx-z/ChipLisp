using System;
using System.IO;
using Xunit;
using Nela.ChipLisp;
using Nela.ChipLisp.LangExtensions;
using Nela.ChipLisp.Libs;

namespace Test {
    public class Tests {
        [Fact]
        public void TestAdd() {
            var state = new State();
            state.LoadPreludeLib();
            Assert.Equal(ValueObj.Create(3), state.Eval("(+ 1 2)"));
        }

        [Fact]
        public void TestRecursion() {
            var state = new State();
            state.LoadPreludeLib();
            Assert.Equal(ValueObj.Create(10), state.Eval("(progn (defun sum (a . b) (if b (+ a (sum . b)) a)) (sum 1 2 3 4))"));
            state.Eval("(defun fib (n) (if (< n 2) 1 (+ (fib (- n 1)) (fib (- n 2)))))");
            Assert.Equal(ValueObj.Create(8), state.Eval("(fib 5)"));
        }

        [Fact]
        public void TestExtensions() {
            var state = new State();
            state.LoadPreludeLib();
            var parser = new ExtendedParser();
            Assert.Equal(ValueObj.Create(10), state.Eval(parser.ReadExpr(new Lexer(new StringReader(
@"(progn
(defmacro plus-all args
    (if (cdr args)
        `(+, (car args), (eval `(macroexpand, (cons 'plus-all (cdr args)))))
        (car args))
)
(eval (plus-all 1 2 3 4))
)")))));
        }

        [Fact]
        public void TestCLR() {
            var state = new State();
            state.LoadPreludeLib();
            state.LoadLib(ClrLib.Load);
            Assert.Equal(ValueObj.Create(typeof(string)), state.Eval("(progn (clr-using \"System\") (clr-type \"String\"))"));
            Assert.Equal(ValueObj.Create(typeof(string)), state.Eval("(clr-type \"System.String\")"));
            Assert.Equal(ValueObj.Create("is"), state.Eval("(clr-call-member \"There is nothing.\" 'Substring 6 2)"));
            Assert.Equal(ValueObj.Create(StringSplitOptions.None), state.Eval("(clr-coerce (clr-type \"StringSplitOptions\") 0)"));
        }
    }
}