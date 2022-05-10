using System;
using System.Collections.Generic;
using System.IO;
using Nela.ChipLisp.LangExtensions;
using Nela.ChipLisp.Libs;

namespace Nela.ChipLisp.CommandLine {
    internal class Program {
        public static void Main(string[] args) {
            var state = new State();
            state.LoadPreludeLib();
            state.AddFunction("display", (vm, env, list) => {
                var e = list.GetListEnumerator();
                while (e.GetNext(out var arg)) {
                    arg.Print(Console.Out);
                }
                Console.WriteLine();
                return Obj.nil;
            }, "Display");
            var scriptFiles = new List<string>();
            bool useExtension = false;
            bool repl = false;

            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];
                if (arg.StartsWith("--")) {
                    switch (arg) {
                    case "--ext":
                        useExtension = true;
                        break;
                    case "--repl":
                        repl = true;
                        break;
                    case "--lib":
                        i++;
                        if (i < args.Length) {
                            switch (args[i].ToLower()) {
                            case "clr":
                                state.LoadLib(ClrLib.Load);
                                break;
                            }
                        }

                        break;
                    }
                } else {
                    scriptFiles.Add(arg);
                }
            }

            var parser = useExtension ? new ExtendedParser() : new Parser();

            if (scriptFiles.Count > 0) {
                foreach (var f in scriptFiles) {
                    var reader = new StreamReader(f);
                    var lexer = new Lexer(reader);
                    while (lexer.head != 0) {
                        state.Eval(parser.ReadExpr(lexer));
                    }
                }
            }
            if (scriptFiles.Count == 0 || repl) {
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