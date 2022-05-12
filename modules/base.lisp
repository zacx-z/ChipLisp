(defun id (x) x)

(defun $ (f . args)
    (lambda a (eval (cons f (concat args a)))))

; logical

(defun not (b)
    (if b () t))

(defun or (a b)
    (if a a (if b b ())))

(defun and (a b)
    (if a (if b b ()) ()))

; list

(define empty ())
(defun empty? (a) (eq a ()))

(define rest cdr)
(define first car)
(defun second (lst) (car (cdr lst)))

(defun prepend (lst e)
    (cons e lst)
)

(defun append (lst e)
    (if (not lst) (list e) (cons (first lst) (list-append (rest lst) e)))
)

(defun concat (lst1 lst2)
    (if (not lst1) lst2 (cons (first lst1) (concat (rest lst1) lst2)))
)

(defun iter (f lst)
    (if (eq lst ())
        ()
        (progn (f (first lst)) (iter f (rest lst)))
    )
)

(defun iter/index (f lst)
    (defun iter/internal (f i lst)
        (if (eq lst ())
            ()
            (progn (f (first lst) i) (iter/internal f (+ i 1) (rest lst)))
        )
    )
    (iter/internal f 0 lst)
)

; (for/list (i lst) body...)
(defmacro for/list (var . body)
    (list 'iter/index (list 'lambda (list (first var)) (cons 'progn body)) (second var))
)

(defun map (f lst)
    (if (eq lst())
        ()
        (cons (f (first lst)) (map f (rest lst)))
    )
)

(defun filter (f lst)
    (if (eq lst ())
        ()
        (if (f (first lst))
            (cons (first lst) (filter f (rest lst)))
            (filter f (rest lst)))
    )
)

(defun reverse (lst)
    (defun reverse/internal (lst res)
        (if (eq lst ())
            res
            (reverse/internal (rest lst) (cons (first lst) res))
        )
    )
    (reverse/internal lst ())
)

(defun take (n lst)
    (if (or (eq lst ()) (eql n 0))
        ()
        (cons (first lst) (take (- n 1) (rest lst)))
    )
)

(defun skip (n lst)
    (if (or (eq lst ()) (eql n 0))
        lst
        (skip (- n 1) (rest lst))
    )
)

(defun take-last (n lst)
    (defun take/reverse (n lst res)
        (if (or (eq lst ()) (eql n 0))
            res
            (take/reverse (- n 1) (rest lst) (cons (first lst) res))
        )
    )
    (defun take-last/internal (n lst res)
        (if (eq lst ())
            (take/reverse n res ())
            (take-last/internal n (rest lst) (cons (first lst) res))
        )
    )
    (take-last/internal n lst ())
)

(defun skip-last (n lst)
    (defun skip-last/internal (n lst res)
        (if (eq lst ())
            (reverse (skip n res))
            (skip-last/internal n (rest lst) (cons (first lst) res))
        )
    )
    (skip-last/internal n lst ())
)

; module
; used as (module definition_statement1 definition_statement2 ... (export sym1 sym2 ...))

(defmacro module body
    (eval (list (list 'lambda () (cons 'progn body)))))

(define export ((lambda ()
    (defmacro export/1 (sym)
        (list 'list-append (list 'quote (list 'define sym)) sym)
    )
    (defmacro export/internal lst
        (if lst
            (cons (eval (list 'export/1 (first lst))) (eval (list 'macroexpand (cons 'export/internal (rest lst)))))
            ()
        )
    )
    (defmacro export lst
        (cons 'progn (eval (list 'macroexpand (cons 'export/internal lst))))
    )
)))