using System;
using System.IO;
using Nela.ChipLisp.LangExtensions;

namespace Nela.ChipLisp.CommandLine {
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
            string mainFile = null;
            bool useExtension = false;

            foreach (var arg in args) {
                if (arg.StartsWith("--")) {
                    switch (arg) {
                    case "--ext":
                        useExtension = true;
                        break;
                    }
                } else {
                    mainFile = arg;
                }
            }

            var parser = useExtension ? new ExtendedParser() : new Parser();

            if (mainFile != null) {
                var reader = new StreamReader(args[0]);
                var lexer = new Lexer(reader);
                while (lexer.head != 0) {
                    state.Eval(parser.ReadExpr(lexer));
                }
            }
            else {
                // repl
                Console.CancelKeyPress += OnCancel;
                while (true) {
                    Console.Write("> ");
                    try {
                        var expr = parser.ReadExpr(new Lexer(Console.In));
                        Console.WriteLine(state.Eval(expr));
                        Console.In.ReadLine();
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