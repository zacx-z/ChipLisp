# ChipLisp
[![.NET](https://github.com/zacx-z/ChipLisp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/zacx-z/ChipLisp/actions/workflows/dotnet.yml)

ChipLisp is a lightweight lisp interpreter written in C# primarily designed for embedding usage.

ChipLisp is aimed for flexibility to be interacted from C# side with minimal implementation. It runs a subset of traditional lisp code with a few modifications.
Adopting Lua's philosophy, it reserves total control from C# code to make it easy to create lisp sandboxes only exposing what you need. With the powerful macro syntax, it won't be hard to create a DSL with some customization.

ChipLisp is implemented referring to [minilisp](https://github.com/rui314/minilisp), but adds complete error reporting behaviors.

## Getting Started

1. Build the project.

2. Add the reference `ChipLisp.dll` to your project, or copy `ChipLisp.dll` to your Unity project.

You can also run `ichiplisp` to run lisp code interactively or in a file by `ichiplisp <filename>`.

## How to Run

ChipLisp's API is similar to Lua's. To run lisp code, create a `State` and selectively load libraries.

```c#
var state = new State();
state.LoadPreludeLib();
Console.WriteLine(state.Eval("(+ 1 2)"));
```

`state.Eval()` also accepts `TextReader` as input.

Scopes are managed by `Env`. `state.PushEnv()` adds a local scope on the stack, which all subsequent operations run in. Call `state.PopEnv()` to leave the local scope.

If you want to create a DSL and restrict its access:

```c#
state.LoadPreludeLib();
state.PushEnv();
state.Eval(dslDefinitionLib);
state.PushEnv(Env.FromMapping(state.env.vars));
state.Eval(dslCode);
```

In this way, you may write lisp code in `dslDefinitionLib` to create necessary definitions for the DSL. Because `Env.FromMapping()` only copies variable definitions, prelude library is being precluded when `dslCode` is running.

## How to Extend

You can extend ChipLisp with C# functions so that you can customize it to your needs.

### Add Variables

```c#
state.AddVariable("my-int", new ValueObj<int>(100));
```

You can use any type in C# with `ValueObj<T>` and pass it to your custom functions.

### Add Functions

#### The Low-Level Way

The low-level and powerful way is to use `state.AddVariable` and `PrimObj`.

```c#
state.AddVariable("my-fun", new PrimObj(MyFunc));

...

Obj MyFunc(VM vm, Env env, Obj list) {
    vm.EvalListExt(env, list);
    // do your work
    ...
}
```

`env`: the scope information of the function implemented.
`list`: the rest elements when being called.

Implement the function with the help of `VM` class and `Utils.cs`.

ChipLisp also provides two helper functions to define C# functions:

 - `state.AddFunction(name, func)`
 - `state.AddMacro(name, macrofunc)`

To learn more, one way is to read the functions implemented Prelude.cs.

#### The High-Level Way

ChipLisp provides helper functions to fast export C# functions with ease. It automatically converts the arguments and return values for each side.

Signature: `state.AddCSharpFunction<T1, T2 ...>(name, csharpfunc)`

For example:
```c#
state.AddCSharpFunction<float, float>("exp", Math.Abs);
```

### Extending the Parser

You can provide new syntax by extending `Parser` class to override `ReadExpr()` method.

```c#
class MyParser : Parser {
    // ...
}
```

Override `Parser.ReadExpr()` to add new expressions to parse.

```c#
public override Obj ReadExpr(ILexer lexer) {
    switch (lexer.head) {
    case '`':
        return ReadBackQuote(lexer);
    case ',':
        return ReadComma(lexer);
    }
    return base.ReadExpr(lexer);
}
```

Then use your new parser to read expr:

```c#
var parser = new MyParser();
state.Eval(parser.ReadExpr(new Lexer(textReader)));
```

### Extending the Lexer

You can support more literals by extending `Lexer` class to override its `ReadObj()` method.

```c#
class MyLexer : Lexer {
    // ...
}
```

Override `Lexer.ReadObj` method:

```c#
protected override Obj ReadObj() {
    switch (head) {
    case '@':
        return ReadNewLiteral();
    default:
        return base.ReadObj();
    }
}
```

Then use your new lexer to read from the source:
```c#
var lexer = new MyLexer(textReader);
state.Eval(lexer);
```

### Manipulating the variable scope

You can register a handler to `onMissing` delegate. Whenever the VM looking for a symbol that is not created, it will be called and you can customize what should be returned for that symbol. This is useful to create DSLs.

```c#
env.onMissing = o => new PrimObj(objCreateFunc);
```

## Lisp Language

There are two reserved symbols: `t` and `quote`. They are not supposed to be redefined.

ChipLisp treats `[` `{` the same as `(`, and `]` `}` the same as `)`.

**Supported literals:**
- int: `123`
- float: `3.14`
- string: `"brown fox"`
- char: `\'a'`

**Primitives "Prelude" lib provides:**
```lisp
cons
car
cdr
list
+
-
*
/
<
= ; compare integers
to-i ; convert float to int
to-f ; convert int to float
eq ; check if two object references are the same
eql ; check if two objects have the same value
eval
define
defun
defmacro
lambda
macroexpand
progn
if
while
let ; bind local variables
```
You may refers to [minilisp](https://github.com/rui314/minilisp) for their usages.

### Let

Syntax:
`(let ([<var> <val>] ...) <body> ...)`

Similar to `let` in Racket, it binds local variable and run `<body>` with them.

Example:
```lisp
(let ([x 1]
      [y 2])
     (+ x y))
```

### Extensions

The Extensions project provides some extended syntax by extending the parser.

**Back-quote and comma:**

Back-quote provides an easier way to write macros:

```lisp
(defmacro unless (condition expr)
    `(if, condition, (), expr)
)
```

Similar to Common Lisp, the backquote (`) character signals that in the expression that follows, every subexpression not preceded by a comma is to be quoted, and every subexpression preceded by a comma is to be evaluated.
it quote. It is equivalent to:

```lisp
(defmacro unless (condition expr)
    (list 'if condition () expr)
)
```

However, unlike in Common Lisp, it only unwraps one layer, so you need to nest back-quoted expressions in some cases:

```lisp
(defmacro plus-all args
    (if (cdr args)
        `(+, (car args), (eval `(macroexpand, (cons 'plus-all (cdr args)))))
        (car args))
)
```

## Advanced Topics

On function calls, if the last cell of the arguments is not ended by `nil`, the ending element will be evaluated and the result will be spliced into the argument list, like `,@` in Common Lisp. It will repeat evaluating the ending element of the returned result until getting a value other than a cell value.

This feature is useful to apply rest arguments to functions with varied-length parameters.

Define a recursive function which takes a variable number of numbers:
```lisp
(defun sum (a . b) (if b (+ a (sum . b)) a))

> (sum 1 2 3 4)
10
```

Define a similar recursive macro, which is more complex:
```lisp
(defmacro plus-all args
    (if (cdr args)
        (list '+ (car args) (eval (list 'macroexpand (cons 'plus-all (cdr args)))))
        (car args))
)

> (macroexpand (plus-all a b c d))
(+ a (+ b (+ c d)))
```

There are some cases where this gets quirky. For example:

```lisp
(define pair '(1 2))
(+ 1 . (cdr pair))
```

It raises an error "Can't evaluate (+ 1 cdr pair)", because `(+ 1 . (cdr pair))` is expanded as `(+ . (1 . (cdr . (pair . ()))))` in which cdr is treated as a list element. To avoid this issue, you might have to bind `(cdr pair)` to a local variable.
