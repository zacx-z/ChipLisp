using System;
using System.IO;

namespace NelaSystem.ChipLisp.CommandLine {
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
            if (args.Length > 0) {
                var reader = new StreamReader(args[0]);
                var lexer = new Lexer(VM.vm, reader);
                while (!reader.EndOfStream) {
                    state.Eval(lexer);
                }
            }
            else {
                // repl
                Console.CancelKeyPress += OnCancel;
                while (true) {
                    Console.Write("> ");
                    try {
                        Console.WriteLine(state.Eval(Console.In));
                    }
                    catch (InterpreterException e) {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        private static void OnCancel(object sender, ConsoleCancelEventArgs args) {
            Console.WriteLine("quitting");
            args.Cancel = false;
        }
    }
}