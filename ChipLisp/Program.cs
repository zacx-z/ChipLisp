using System;

namespace NelaSystem.ChipLisp {
    internal class Program {
        public static void Main(string[] args) {
            var state = new State();
            state.LoadPreludeLib();
            Console.WriteLine(state.Eval("(defun test (x) (- x 2))"));
            Console.WriteLine(state.Eval("(test 3.3)"));
        }
    }
}