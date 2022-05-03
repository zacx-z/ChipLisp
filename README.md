# ChipLisp
ChipLisp is a lightweight lisp interpreter written in C# primarily designed for interop usage.
ChipLisp is aimed for minimal implementation and flexiblity to be interacted from C# side. It runs a subset of traditional lisp code with a few modifications.
Adopting Lua's philosophy, it reserves total control for C# code to make it easy to create lisp sandboxes only exposing what you need. With the powerful macro syntax, it won't be hard to create a DSL with some customization.

ChipLisp's implementation refers to [minilisp](https://github.com/rui314/minilisp).


## Language

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

Define a recursive macro, which is more complex:
```lisp
(defmacro plus-all args
    (if (cdr args)
        (list '+ (car args) (eval (list 'macroexpand (cons 'plus-all (cdr args)))))
        (car args))
)

> (macroexpand (plus-all a b c d))
(+ a (+ b (+ c d)))
```
