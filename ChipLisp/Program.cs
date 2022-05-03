using System;
using System.IO;

namespace NelaSystem.ChipLisp {
    internal class Program {
        public static void Main(string[] args) {
            var state = new State();
            state.LoadPreludeLib();
            state.AddFunction("put", (vm, env, list) => {
                var e = list.GetListEnumerator();
                while (e.GetNext(out var arg)) {
                    arg.Print(Console.Out);
                }
                Console.WriteLine();
                return Obj.nil;
            });
            var reader = new StreamReader("./main.lisp");
            while (!reader.EndOfStream) {
                state.Eval(reader);
            }
        }
    }
}