# ChipLisp
ChipLisp is a lightweight lisp interpreter written in C# primarily designed for interop usage.

ChipLisp is aimed for minimal implementation and flexibility to be interacted from C# side. It runs a subset of traditional lisp code with a few modifications.
Adopting Lua's philosophy, it reserves total control for C# code to make it easy to create lisp sandboxes only exposing what you need. With the powerful macro syntax, it won't be hard to create a DSL with some customization.

ChipLisp is implemented referring to [minilisp](https://github.com/rui314/minilisp), but adds complete error reporting.

## Run Interpreter

ChipLisp's API is similar to Lua's. To run lisp code, create a `State` and selectively load library.

```c#
var state = new State();
state.LoadPreludeLib();
Console.WriteLine(state.Eval("(+ 1 2)"));
```

`state.Eval()` also accepts `TextReader` as input.

Scope is managed by `Env`. `state.PushEnv()` adds a local scope on the stack, which all subsequent operations run in. Call `state.PopEnv()` to leave the local scope.

If you want to create a DSL and restrict its access:

```c#
state.LoadPreludeLib();
state.PushEnv();
state.Eval(dslDefinitionLib);
state.PushEnv(Env.FromMapping(state.env.vars));
state.Eval(dslCode);
```

In this way, you may write lisp code in `dslDefinitionLib` to create necessary definitions for the DSL. Because `Env.FromMapping()` only copies variable definitions, prelude library is being precluded when `dslCode` is running.

## Lisp Language

There are two reserved symbols: `t` and `quote`. They are not supposed to be redefined.

Prelude lib provides:
```lisp
cons
car
cdr
list
+
-
<
=
eq
eval
define
defun
defmacro
lambda
macroexpand
if
while
let
```

In ChipLisp, when calling functions, if the last cell of the arguments is not ended by `nil`, the ending element will be evaluated and the result will be spliced into the result, like `,@` in Common Lisp. It will repeat evaluating the ending element of the returned result until not getting a cell value or reaching `nil`. It is useful to apply rest arguments to functions with varied-length parameters.

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
